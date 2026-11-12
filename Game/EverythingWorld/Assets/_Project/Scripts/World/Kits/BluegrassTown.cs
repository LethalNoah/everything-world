using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The holler and its mountain town (part of BluegrassKit): steep green ridges rise on both sides of the creek
    /// bottom; a gravel main street runs along the creek past the picking circle, with a clapboard general store
    /// whose porch is full of pickers, a white church with a steeple, a fiddlers' convention stage with bunting and
    /// hay-bale seats, a post office, a gas pump and power poles. Up on a ridge, a coal mine's timber portal with a
    /// cart track; over the creek, a timber railroad trestle with coal hoppers; moonshine stills smoking in the woods.
    public partial class BluegrassKit
    {
        Vector2 creekDir2 = Vector2.right;
        readonly List<Vector3> street = new List<Vector3>();   // the main street's middle line (y unused)
        Material clap, clapBlue, clapYellow, brick, denim, coal, rail;

        void TownMaterials()
        {
            clap = WorldMaterials.Solid(new Color(.93f, .9f, .82f)); clapBlue = WorldMaterials.Solid(new Color(.55f, .66f, .78f)); clapYellow = WorldMaterials.Solid(new Color(.92f, .8f, .45f));
            brick = WorldMaterials.Solid(new Color(.58f, .3f, .22f)); denim = WorldMaterials.Solid(new Color(.25f, .33f, .5f)); coal = WorldMaterials.Solid(new Color(.06f, .06f, .07f), .8f);
            rail = WorldMaterials.Solid(new Color(.35f, .3f, .28f), .7f);
        }

        // ---------------------------------------------------------------- shaping: the ridges and the street
        /// Ridges rise on both sides of the creek bottom (up to about 20 m, lumpy with spurs and side hollows),
        /// fading out toward the land's edges so the neighbours' ground still meets ours.
        void Ridges(IslandWorldData data, int index)
        {
            int c0 = data.Nx, c1 = 0, r0 = data.Nz, r1 = 0;
            for (var r = 0; r < data.Nz; r++) for (var c = 0; c < data.Nx; c++) if (data.islandIds[r * data.Nx + c] == index) { c0 = Mathf.Min(c0, c); c1 = Mathf.Max(c1, c); r0 = Mathf.Min(r0, r); r1 = Mathf.Max(r1, r); }
            if (c1 < c0) return;
            c0 = Mathf.Max(0, c0 - 2); r0 = Mathf.Max(0, r0 - 2); c1 = Mathf.Min(data.Nx - 1, c1 + 2); r1 = Mathf.Min(data.Nz - 1, r1 + 2);
            int w = c1 - c0 + 1, h = r1 - r0 + 1; var dist = new float[w * h]; var step = data.Step; var diag = step * 1.414f;
            for (var r = 0; r < h; r++) for (var c = 0; c < w; c++) dist[r * w + c] = data.islandIds[(r + r0) * data.Nx + c + c0] == index ? 1e6f : 0f;
            for (var r = 0; r < h; r++) for (var c = 0; c < w; c++)
            {
                var i = r * w + c; var d = dist[i]; if (d == 0f) continue;
                d = Mathf.Min(d, c > 0 ? dist[i - 1] + step : 0f, r > 0 ? dist[i - w] + step : 0f);
                d = Mathf.Min(d, r > 0 && c > 0 ? dist[i - w - 1] + diag : 0f, r > 0 && c < w - 1 ? dist[i - w + 1] + diag : 0f); dist[i] = d;
            }
            for (var r = h - 1; r >= 0; r--) for (var c = w - 1; c >= 0; c--)
            {
                var i = r * w + c; var d = dist[i]; if (d == 0f) continue;
                d = Mathf.Min(d, c < w - 1 ? dist[i + 1] + step : 0f, r < h - 1 ? dist[i + w] + step : 0f);
                d = Mathf.Min(d, r < h - 1 && c < w - 1 ? dist[i + w + 1] + diag : 0f, r < h - 1 && c > 0 ? dist[i + w - 1] + diag : 0f); dist[i] = d;
            }
            for (var r = 0; r < h; r++) for (var c = 0; c < w; c++)
            {
                var b = dist[r * w + c]; if (b <= 0f) continue;
                var x = (c + c0) * step; var z = data.layout.depth - (r + r0) * step;
                var dc = CreekFar(x, z); if (dc < 26f) continue;
                var lump = .5f + Mathf.PerlinNoise(x * .011f + 3.3f, z * .011f + 5.1f) * 1f;
                var rise = 20f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(26f, 160f, dc)) * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 80f, b)) * lump;
                data.heights[(r + r0) * data.Nx + c + c0] += rise;
            }
        }

        /// Distance from (x, z) to the creek's middle line, without the near-only cut-off CreekDist uses.
        float CreekFar(float x, float z)
        {
            var best = float.MaxValue; var p = new Vector2(x, z);
            for (var k = 0; k + 1 < creek.Count; k++) best = Mathf.Min(best, SegDist(p, new Vector2(creek[k].x, creek[k].z), new Vector2(creek[k + 1].x, creek[k + 1].z), out _));
            return best;
        }

        /// The main street: a line along the creek 12 m from where you arrive (toward the creek), as far as it can
        /// go each way (up to 96 m) without reaching the water or the land's edge; its ground is levelled smoothly.
        void StreetLine(IslandWorldData data, int index, System.Func<float, float, int> id)
        {
            street.Clear();
            var o = heart2 + perp2 * 12f; var side = new Vector2(-creekDir2.y, creekDir2.x);
            bool Ok(Vector2 q) => id(q.x, q.y) == index && id(q.x + side.x * 22f, q.y + side.y * 22f) == index && id(q.x - side.x * 22f, q.y - side.y * 22f) == index && !InCreek(q.x, q.y, 9f);
            var fwd = new List<Vector3>(); var back = new List<Vector3>();
            for (var t = 0f; t <= 96f; t += 6f) { var q = o + creekDir2 * t; if (!Ok(q)) break; fwd.Add(new Vector3(q.x, 0, q.y)); }
            for (var t = 6f; t <= 96f; t += 6f) { var q = o - creekDir2 * t; if (!Ok(q)) break; back.Add(new Vector3(q.x, 0, q.y)); }
            back.Reverse(); street.AddRange(back); street.AddRange(fwd);
            if (street.Count < 4) { street.Clear(); return; }
            var hs = new float[street.Count]; for (var i = 0; i < hs.Length; i++) hs[i] = data.HeightAt(street[i].x, street[i].z);
            for (var pass = 0; pass < 6; pass++) for (var i = 1; i + 1 < hs.Length; i++) hs[i] = (hs[i - 1] + hs[i] * 2f + hs[i + 1]) / 4f;
            const float flat = 16f, blend = 28f;
            for (var r = 0; r < data.Nz; r++)
            {
                var z = data.layout.depth - r * data.Step;
                for (var c = 0; c < data.Nx; c++)
                {
                    var i = r * data.Nx + c; if (data.islandIds[i] != index) continue;
                    var x = c * data.Step; var p = new Vector2(x, z); var best = float.MaxValue; var target = 0f;
                    for (var k = 0; k + 1 < street.Count; k++)
                    {
                        var a = street[k]; if (Mathf.Abs(a.x - x) > 40f || Mathf.Abs(a.z - z) > 40f) continue;
                        var d = SegDist(p, new Vector2(a.x, a.z), new Vector2(street[k + 1].x, street[k + 1].z), out var t);
                        if (d < best) { best = d; target = Mathf.Lerp(hs[k], hs[k + 1], t); }
                    }
                    if (best > blend || InCreek(x, z, 6f)) continue;
                    data.heights[i] = best < flat ? target : Mathf.Lerp(target, data.heights[i], Mathf.SmoothStep(0f, 1f, (best - flat) / (blend - flat)));
                }
            }
        }

        // ---------------------------------------------------------------- the street
        void Street(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            if (street.Count < 4) return;
            TownMaterials();
            var pts = new List<Vector3>(); foreach (var p in street) pts.Add(ctx.OnGround(p.x, p.z));
            Strip(mk, pts, WorldMaterials.Solid(new Color(.6f, .58f, .54f)), 6f, .18f, ctx);
            foreach (var p in pts) Remember(p, 0);
            // power poles down one side, every other one with a street lamp, wires sagging between them
            var dir = new Vector3(creekDir2.x, 0, creekDir2.y); var away = new Vector3(perp2.x, 0, perp2.y);
            var poles = new List<Vector3>(); var lights = 0;
            for (var i = 0; i < pts.Count; i += 3)
            {
                var g = pts[i] + away * 4.4f; if (Vector2.Distance(new Vector2(g.x, g.z), heart2) < 14f || InCreek(g.x, g.z, 1.5f) || TrailNear(g.x, g.z, 2f)) continue;
                g = ctx.OnGround(g.x, g.z); poles.Add(g);
                mk.Prism(bark, g - Vector3.up * .3f, Vector3.up, .14f, 7.6f, 7, .8f); mk.Box(wood, g + Vector3.up * 6.8f, new Vector3(.12f, .12f, 2.2f), Quaternion.LookRotation(dir));
                foreach (var s in new[] { -.9f, .9f }) mk.Prism(trim, g + Vector3.up * 6.86f + dir * s, Vector3.up, .05f, .14f, 6);
                ArtistBits.PostCollider(ctx.Root, g, 7f, .18f);
                if (poles.Count % 2 == 1)
                {
                    var arm = g + Vector3.up * 5.4f; mk.Box(metal, arm - away * .7f, new Vector3(.08f, .08f, 1.4f), Quaternion.LookRotation(away));
                    var bulb = arm - away * 1.35f - Vector3.up * .15f; mk.Cone(metal, bulb, .32f, .2f, 8); mk.Blob(GlowM(Warm, 3f), bulb - Vector3.up * .05f, Vector3.one * .14f, ctx.Rng, 0f);
                    halos.Add(bulb - Vector3.up * .1f, 1.2f, Warm, .8f);
                    if (lights++ < 3) ArtistBits.PointLight(ctx.Root, bulb - Vector3.up * .5f, Warm, 14f, 1.1f);
                }
            }
            for (var i = 0; i + 1 < poles.Count; i++)
                foreach (var s in new[] { -.9f, .9f })
                {
                    Vector3 a = poles[i] + Vector3.up * 6.9f + dir * s, b = poles[i + 1] + Vector3.up * 6.9f + dir * s, m = (a + b) / 2f - Vector3.up * .6f;
                    mk.Box(dark, (a + m) / 2f, new Vector3(.03f, .03f, Vector3.Distance(a, m)), Quaternion.LookRotation(m - a)); mk.Box(dark, (m + b) / 2f, new Vector3(.03f, .03f, Vector3.Distance(m, b)), Quaternion.LookRotation(b - m));
                }
        }

        bool TrailNear(float x, float z, float within)
        {
            foreach (var (p, w) in walkPoints) { if (w < 1 || w >= 400) continue; float dx = p.x - x, dz = p.z - z; if (dx * dx + dz * dz < within * within) return true; }
            return false;
        }

        /// The first free spot beside the main street for a building of radius r (nearest the middle first), and
        /// the way it should face (toward the street).
        bool Slot(LandContext ctx, float r, out Vector3 at, out Vector3 face)
        {
            at = face = default; if (street.Count < 4) return false;
            var away = new Vector3(perp2.x, 0, perp2.y); var o = new Vector3(heart2.x, 0, heart2.y) + away * 12f; var dir = new Vector3(creekDir2.x, 0, creekDir2.y);
            Vector3 first = street[0], last = street[street.Count - 1]; float tMin = Vector3.Dot(first - o, dir), tMax = Vector3.Dot(last - o, dir);
            foreach (var t in new[] { 22f, -22f, 36f, -36f, 50f, -50f, 64f, -64f, 78f, -78f, 92f, -92f })
            {
                if (t < tMin + 2f || t > tMax - 2f) continue;
                foreach (var s in new[] { -1f, 1f })
                {
                    var p = o + dir * t + away * s * (r + 4.5f);
                    if (!ctx.IsLand(p.x, p.z, r) || InFootprint(ctx, p.x, p.z, r + 2f) || InCreek(p.x, p.z, r + 2f) || TrailNear(p.x, p.z, r + 1.5f) || InNoGrass(p.x, p.z, r)) continue;
                    at = ctx.OnGround(p.x, p.z); face = -away * s; noGrass.Add(new Vector3(p.x, r + 1f, p.z)); return true;
                }
            }
            return false;
        }

        void Town(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            if (street.Count < 4) return;
            if (Slot(ctx, 8f, out var at, out var face)) GeneralStore(ctx, mk, solid, at, face);
            if (Slot(ctx, 7.5f, out at, out face)) Church(ctx, mk, solid, at, face);
            if (Slot(ctx, 7.5f, out at, out face)) ContestStage(ctx, mk, solid, at, face);
            if (Slot(ctx, 5f, out at, out face)) PostOffice(ctx, mk, solid, at, face);
            if (Slot(ctx, 5f, out at, out face)) GasPump(ctx, mk, solid, at, face);
            var colours = new[] { clapBlue, clapYellow, clap };
            for (var k = 0; k < 3; k++) if (Slot(ctx, 5f, out at, out face)) TownHouse(ctx, mk, solid, at, face, colours[k]);
            var mark = new GameObject("Shot: main street").transform; mark.SetParent(ctx.Root, false);
            var mid = ctx.OnGround(street[street.Count / 2].x, street[street.Count / 2].z); var cam = street[Mathf.Max(0, street.Count / 2 - 7)]; cam = ctx.OnGround(cam.x, cam.z);
            var look = mid - cam; look.y = 0; mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized + Vector3.up * .04f));
        }

        Vector3 Side(Vector3 face) => Vector3.Cross(Vector3.up, face);

        /// Clapboard lines across a wall face.
        void Clapboard(MeshKit mk, Vector3 wallCenter, Vector3 normal, float width, float height)
        {
            var rot = Quaternion.LookRotation(normal); var shade = WorldMaterials.Solid(new Color(.8f, .77f, .7f));
            for (var y = -height / 2f + .35f; y < height / 2f; y += .35f) mk.Box(shade, wallCenter + Vector3.up * y + normal * .01f, new Vector3(width, .04f, .02f), rot);
        }

        /// The general store: a clapboard store with a tall false front and a sign, a deep porch where four pickers
        /// sit in rockers with a banjo, a fiddle, a guitar and a mandolin, and a bass player stands at the end;
        /// crates, a barrel, a drinks cooler, lit windows.
        void GeneralStore(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at, Vector3 face)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Side(face); const float W = 11f, D = 8f, H = 4.2f;
            var c = at - face * 1.8f;
            mk.Box(clap, c + up * H / 2f, new Vector3(W, H, D), rot); solid.Box(clap, c + up * H / 2f, new Vector3(W, H, D), rot);
            Clapboard(mk, c + up * H / 2f + face * D / 2f, face, W, H);
            mk.GableRoof(roofM, c + up * (H + .03f), new Vector3(W + .4f, 0, D + .4f), 1.6f, Quaternion.LookRotation(side));
            var ff = c + face * (D / 2f + .1f); mk.Box(clap, ff + up * (H + 1.1f), new Vector3(W, 2.4f, .25f), rot); mk.Box(trim, ff + up * (H + 2.35f), new Vector3(W + .3f, .25f, .4f), rot);
            mk.Box(WorldMaterials.Solid(new Color(.25f, .35f, .3f)), ff + up * (H + 1.05f) + face * .14f, new Vector3(W - 1.6f, 1.2f, .05f), rot);
            ArtistBits.Label(ctx.Root, "GENERAL STORE", ff + up * (H + 1.05f) + face * .18f, face, .62f, new Color(1f, .93f, .7f), TextAnchor.MiddleCenter);
            mk.Box(dark, ff + up * 1.15f + face * .02f, new Vector3(1.4f, 2.3f, .05f), rot);
            foreach (var s in new[] { -3.2f, 3.2f })
            {
                mk.Box(trim, ff + up * 1.7f + side * s + face * .02f, new Vector3(2.6f, 1.9f, .05f), rot);
                mk.Box(GlowM(Warm, 1.7f), ff + up * 1.7f + side * s + face * .05f, new Vector3(2.3f, 1.6f, .03f), rot);
                for (var k = 0; k < 3; k++) mk.Box(WorldMaterials.Solid(new Color(.7f, .3f + k * .15f, .2f)), ff + up * (1.05f + k * .45f) + side * (s + (k - 1) * .5f) + face * .08f, new Vector3(.35f, .3f, .03f), rot);
            }
            // the porch
            var deck = c + face * (D / 2f + 1.9f);
            mk.Box(plank, deck + up * .25f, new Vector3(W, .5f, 3.6f), rot); solid.Box(plank, deck + up * .25f, new Vector3(W, .5f, 3.6f), rot);
            mk.Box(plank, deck + face * 2.1f + up * .12f, new Vector3(2.4f, .25f, .6f), rot);
            foreach (var s in new[] { -1f, 1f }) { mk.Box(wood, deck + side * s * (W / 2f - .2f) + face * 1.6f + up * 2f, new Vector3(.2f, 3.1f, .2f), rot); ArtistBits.PostCollider(ctx.Root, deck + side * s * (W / 2f - .2f) + face * 1.6f, 3.4f, .15f); }
            mk.Box(roofM, deck + up * 3.55f + face * .1f, new Vector3(W + .4f, .12f, 4f), rot * Quaternion.Euler(9f, 0, 0));
            for (var i = 0; i < 12; i++) { var t = i / 11f; var bp = deck + side * Mathf.Lerp(-W / 2f + .3f, W / 2f - .3f, t) + face * 1.6f + up * (3.15f - Mathf.Sin(t * Mathf.PI) * .35f); mk.Blob(GlowM(i % 2 == 0 ? Warm : new Color(1f, .6f, .25f), 3f), bp, Vector3.one * .09f, ctx.Rng, 0f); if (i % 3 == 1) halos.Add(bp, .6f, Warm, .7f); }
            var seat = deck + up * .5f;
            Picker(ctx, mk, solid, seat - side * 3.6f, face, 0, new Color(.75f, .3f, .25f), new Color(.85f, .66f, .5f));
            Picker(ctx, mk, solid, seat - side * 1.2f, face, 1, new Color(.3f, .45f, .65f), new Color(.45f, .3f, .22f));
            Picker(ctx, mk, solid, seat + side * 1.2f, face, 2, new Color(.92f, .9f, .82f), new Color(.7f, .52f, .38f));
            Picker(ctx, mk, solid, seat + side * 3.6f, face, 3, new Color(.4f, .55f, .35f), new Color(.92f, .76f, .62f));
            Stander(ctx, mk, seat + side * 4.9f - face * .6f, face, new Color(.55f, .4f, .65f), new Color(.6f, .42f, .3f));
            // crates, a barrel, a drinks cooler
            var cr = c + face * (D / 2f + .5f) - side * (W / 2f + .9f); mk.Box(wood, cr + up * .3f, new Vector3(.7f, .6f, .7f), rot); mk.Box(wood, cr + up * .9f + side * .1f, new Vector3(.6f, .6f, .6f), rot * Quaternion.Euler(0, 15f, 0)); solid.Box(wood, cr + up * .6f, new Vector3(.9f, 1.2f, .9f), rot);
            var bl = c + face * (D / 2f - 1.2f) - side * (W / 2f + .8f); mk.Prism(wood, ctx.OnGround(bl.x, bl.z), up, .4f, 1f, 10, .92f); ArtistBits.PostCollider(ctx.Root, ctx.OnGround(bl.x, bl.z), 1f, .45f);
            var cool = deck + up * .5f + side * (W / 2f - .7f) - face * 1.1f; mk.Box(WorldMaterials.Solid(new Color(.8f, .15f, .12f), .6f), cool + up * .45f, new Vector3(1f, .9f, .6f), rot);
            mk.Box(GlowM(new Color(.6f, .85f, 1f), 1.6f), cool + up * .55f + face * .31f, new Vector3(.7f, .3f, .02f), rot); solid.Box(dark, cool + up * .45f, new Vector3(1f, .9f, .6f), rot);
            ArtistBits.PointLight(ctx.Root, deck + up * 2.8f + face * 1f, Warm, 14f, 1.5f);
            var mark = new GameObject("Shot: the general store porch").transform; mark.SetParent(ctx.Root, false);
            var cam = deck + face * 11f + side * 4f; cam = ctx.OnGround(cam.x, cam.z) + up * .2f; mark.SetPositionAndRotation(cam, Quaternion.LookRotation((deck + up * 1.2f - cam - up * 1.5f).normalized));
        }

        /// A picker in a rocking chair: denim legs, a shirt, a hat, playing a banjo (0), fiddle (1), guitar (2) or
        /// mandolin (3).
        void Picker(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 seat, Vector3 dir, int kind, Color shirt, Color skinC)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(dir); var side = Side(dir);
            Rocker(mk, solid, ctx, seat, dir);
            var shirtM = WorldMaterials.Solid(shirt); var skin = WorldMaterials.Solid(skinC); var hat = WorldMaterials.Solid(kind % 2 == 0 ? new Color(.35f, .25f, .18f) : new Color(.85f, .78f, .6f));
            foreach (var s in new[] { -.1f, .1f }) { mk.Box(denim, seat + side * s + up * .6f + dir * .12f, new Vector3(.16f, .16f, .5f), rot); mk.Box(denim, seat + side * s + up * .32f + dir * .38f, new Vector3(.15f, .55f, .15f), rot); mk.Box(dark, seat + side * s + up * .06f + dir * .45f, new Vector3(.15f, .1f, .28f), rot); }
            mk.Box(shirtM, seat + up * 1f - dir * .14f, new Vector3(.44f, .62f, .26f), rot * Quaternion.Euler(-6f, 0, 0));
            var head = seat + up * 1.5f - dir * .1f; mk.Blob(skin, head, new Vector3(.12f, .14f, .12f), ctx.Rng, 0f);
            mk.Prism(hat, head + up * .08f, up, .24f, .03f, 12); mk.Prism(hat, head + up * .08f, up, .13f, .14f, 10);
            switch (kind)
            {
                case 0: Banjo(mk, seat + up * .95f + dir * .26f, rot * Quaternion.Euler(-15f, 0, 62f), .5f, false); break;
                case 1: Fiddle(mk, seat + up * 1.3f + dir * .1f + side * .12f, rot * Quaternion.Euler(-70f, 0, 70f), .5f); break;
                case 2: Guitar(mk, seat + up * .95f + dir * .28f, rot * Quaternion.Euler(-12f, 0, 64f), .48f); break;
                default: Mandolin(mk, seat + up * 1.05f + dir * .26f, rot * Quaternion.Euler(-15f, 0, 55f), .55f); break;
            }
            foreach (var s in new[] { -.26f, .26f }) mk.Box(shirtM, seat + up * 1f + side * s + dir * .08f, new Vector3(.12f, .12f, .45f), rot * Quaternion.Euler(20f, 0, 0));
        }

        /// A standing bass player with an upright bass.
        void Stander(LandContext ctx, MeshKit mk, Vector3 floor, Vector3 dir, Color shirt, Color skinC)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(dir); var side = Side(dir); var skin = WorldMaterials.Solid(skinC);
            foreach (var s in new[] { -.11f, .11f }) mk.Box(denim, floor + side * s + up * .45f, new Vector3(.16f, .9f, .16f), rot);
            mk.Box(WorldMaterials.Solid(shirt), floor + up * 1.22f, new Vector3(.46f, .66f, .26f), rot);
            var head = floor + up * 1.72f; mk.Blob(skin, head, new Vector3(.12f, .14f, .12f), ctx.Rng, 0f);
            mk.Prism(WorldMaterials.Solid(new Color(.2f, .2f, .22f)), head + up * .08f, up, .22f, .03f, 12); mk.Prism(WorldMaterials.Solid(new Color(.2f, .2f, .22f)), head + up * .08f, up, .12f, .13f, 10);
            UprightBass(mk, floor + up * .95f + dir * .45f + side * .15f, rot * Quaternion.Euler(-8f, 0, 10f), .9f);
            ArtistBits.PostCollider(ctx.Root, floor, 1.8f, .45f);
        }

        /// A white clapboard church with a steeple and a bell, lit tall windows and a red door.
        void Church(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at, Vector3 face)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Side(face); const float W = 6.4f, D = 11f, H = 4.6f;
            var c = at - face * 1.5f;
            mk.Box(clap, c + up * H / 2f, new Vector3(W, H, D), rot); solid.Box(clap, c + up * H / 2f, new Vector3(W, H, D), rot);
            mk.GableRoof(roofM, c + up * (H + .03f), new Vector3(W + .6f, 0, D + .6f), 2.6f, rot);
            foreach (var zz in new[] { -1f, 1f }) mk.Tri(clap, c + up * H + side * W / 2f + face * zz * D / 2f, c + up * (H + 2.55f) + face * zz * D / 2f, c + up * H - side * W / 2f + face * zz * D / 2f, c + up * (H + 1f));
            Clapboard(mk, c + up * H / 2f + face * D / 2f, face, W, H);
            var fr = c + face * (D / 2f + .02f);
            mk.Box(WorldMaterials.Solid(new Color(.6f, .12f, .1f)), fr + up * 1.2f, new Vector3(1.5f, 2.4f, .05f), rot);
            mk.Prism(GlowM(new Color(1f, .82f, .45f), 2f), fr + up * 3.6f, face, .55f, .04f, 14);
            foreach (var s in new[] { -1f, 1f }) for (var k = 0; k < 3; k++)
            {
                var wp = c + side * s * (W / 2f + .02f) + face * (-3.2f + k * 3.2f) + up * 2.3f;
                mk.Box(trim, wp, new Vector3(.04f, 2.4f, 1.1f), rot); mk.Box(GlowM(new Color(1f, .85f, .5f), 1.8f), wp + side * s * .02f, new Vector3(.03f, 2.1f, .8f), rot);
            }
            // the steeple over the front: a square tower, a belfry with a bell, a tall spire
            var tw = c + face * (D / 2f - 1.2f);
            mk.Box(clap, tw + up * (H + 2.2f), new Vector3(2f, 4.4f, 2f), rot);
            mk.Box(trim, tw + up * (H + 4.6f), new Vector3(2.3f, .3f, 2.3f), rot);
            foreach (var s in new[] { -1f, 1f }) foreach (var zz in new[] { -1f, 1f }) mk.Box(clap, tw + up * (H + 5.6f) + side * s * .85f + face * zz * .85f, new Vector3(.3f, 1.8f, .3f), rot);
            mk.Box(trim, tw + up * (H + 6.6f), new Vector3(2.3f, .3f, 2.3f), rot);
            mk.Cone(metal, tw + up * (H + 5.9f), .45f, .7f, 10); mk.Prism(metal, tw + up * (H + 5.2f), up, .5f, .2f, 10);
            mk.Cone(roofM, tw + up * (H + 6.75f), 1.35f, 5.5f, 4, 45f);
            mk.Prism(GlowM(new Color(1f, .9f, .6f), 2.2f), tw + up * (H + 12.2f), up, .05f, 1f, 6); mk.Box(GlowM(new Color(1f, .9f, .6f), 2.2f), tw + up * (H + 12.85f), new Vector3(.6f, .08f, .08f), rot);
            halos.Add(tw + up * (H + 12.6f), 1.4f, new Color(1f, .9f, .6f), .6f);
            mk.Box(stone, fr + face * .6f + up * .12f, new Vector3(2.4f, .24f, 1.2f), rot);
            ArtistBits.PointLight(ctx.Root, fr + face * 3f + up * 3f, new Color(1f, .85f, .55f), 12f, 1.2f);
            var mark = new GameObject("Shot: the church").transform; mark.SetParent(ctx.Root, false);
            var cam = fr + face * 15f - side * 5f; cam = ctx.OnGround(cam.x, cam.z); mark.SetPositionAndRotation(cam, Quaternion.LookRotation((tw + up * 5f - cam - up * 1.5f).normalized));
        }

        /// The fiddlers' convention stage: a plank stage with a backdrop and a banner, bunting, string lights, a
        /// fiddle on a stand by the mic, rows of hay bales to sit on.
        void ContestStage(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at, Vector3 face)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Side(face); const float W = 9f, D = 5f;
            var c = at - face * 2.5f;
            mk.Box(plank, c + up * .5f, new Vector3(W, 1f, D), rot); solid.Box(plank, c + up * .5f, new Vector3(W, 1f, D), rot);
            mk.Box(plank, c + face * (D / 2f + .4f) + up * .25f, new Vector3(2f, .5f, .8f), rot);
            var back = c - face * (D / 2f - .15f);
            mk.Box(barnRed, back + up * 2.9f, new Vector3(W, 3.8f, .3f), rot); solid.Box(barnRed, back + up * 2.9f, new Vector3(W, 3.8f, .3f), rot);
            mk.Box(cream, back + up * 4f + face * .17f, new Vector3(W - 1.4f, 1.1f, .04f), rot);
            ArtistBits.Label(ctx.Root, "OLD-TIME FIDDLERS' CONVENTION", back + up * 4f + face * .2f, face, .34f, new Color(.55f, .12f, .1f), TextAnchor.MiddleCenter);
            foreach (var s in new[] { -1f, 1f }) { mk.Box(wood, c + side * s * (W / 2f - .15f) + face * (D / 2f - .2f) + up * 2.7f, new Vector3(.2f, 3.4f, .2f), rot); ArtistBits.PostCollider(ctx.Root, c + side * s * (W / 2f - .15f) + face * (D / 2f - .2f), 4.4f, .15f); }
            Color[] flags = { new Color(.85f, .15f, .12f), new Color(.95f, .95f, .92f), new Color(.2f, .35f, .8f), new Color(1f, .8f, .2f) };
            for (var i = 0; i < 15; i++)
            {
                var t = (i + .5f) / 15f; var p = c + face * (D / 2f - .2f) + side * Mathf.Lerp(-W / 2f, W / 2f, t) + up * (4.35f - Mathf.Sin(t * Mathf.PI) * .5f);
                mk.Tri(WorldMaterials.Solid(flags[i % flags.Length]), p - side * .25f, p + side * .25f, p - up * .55f, p + face);
                mk.Tri(WorldMaterials.Solid(flags[i % flags.Length]), p + side * .25f, p - side * .25f, p - up * .55f, p - face);
                if (i % 2 == 0) { mk.Blob(GlowM(Warm, 3f), p + up * .1f, Vector3.one * .09f, ctx.Rng, 0f); if (i % 4 == 0) halos.Add(p + up * .1f, .6f, Warm, .7f); }
            }
            var mic = c + up * 1f + face * 1f; mk.Prism(dark, mic, up, .03f, 1.5f, 6); mk.Prism(metal, mic + up * 1.5f, face, .06f, .18f, 8);
            Fiddle(mk, mic + side * 1.3f + up * .7f, rot * Quaternion.Euler(-10f, 0, 0), .5f); mk.Prism(dark, mic + side * 1.3f, up, .03f, .4f, 6);
            var rows = c + face * (D / 2f + 3.5f);
            for (var row = 0; row < 2; row++) for (var k = -1; k <= 1; k++) { var hp = rows + face * row * 2.4f + side * k * 2.6f; hp = ctx.OnGround(hp.x, hp.z); mk.Box(hay, hp + up * .35f, new Vector3(1.8f, .7f, .8f), rot); solid.Box(hay, hp + up * .35f, new Vector3(1.8f, .7f, .8f), rot); }
            ArtistBits.PointLight(ctx.Root, c + up * 4f + face * 4f, new Color(1f, .8f, .5f), 14f, 1.6f);
            noGrass.Add(new Vector3(rows.x, 4f, rows.z));
        }

        void PostOffice(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at, Vector3 face)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Side(face); var c = at - face * 1f;
            mk.Box(brick, c + up * 1.8f, new Vector3(6f, 3.6f, 5.5f), rot); solid.Box(brick, c + up * 1.8f, new Vector3(6f, 3.6f, 5.5f), rot);
            mk.Box(trim, c + up * 3.7f, new Vector3(6.3f, .25f, 5.8f), rot); mk.Box(roofM, c + up * 3.9f, new Vector3(6f, .2f, 5.5f), rot);
            var fr = c + face * 2.77f; mk.Box(dark, fr + up * 1.1f, new Vector3(1.2f, 2.2f, .04f), rot);
            foreach (var s in new[] { -1.9f, 1.9f }) mk.Box(GlowM(Warm, 1.5f), fr + up * 1.8f + side * s, new Vector3(1.2f, 1.2f, .04f), rot);
            mk.Box(cream, fr + up * 3.05f + face * .02f, new Vector3(3.6f, .6f, .04f), rot);
            ArtistBits.Label(ctx.Root, "POST OFFICE", fr + up * 3.05f + face * .05f, face, .32f, new Color(.2f, .2f, .3f), TextAnchor.MiddleCenter);
            var pole = c + face * 4f + side * 2.5f; pole = ctx.OnGround(pole.x, pole.z); mk.Prism(metal, pole, up, .06f, 6f, 6);
            for (var k = 0; k < 4; k++) mk.Box(WorldMaterials.Solid(k % 2 == 0 ? new Color(.8f, .15f, .15f) : new Color(.95f, .95f, .95f)), pole + up * (5.7f - k * .15f) + side * .75f, new Vector3(1.4f, .15f, .02f), rot * Quaternion.Euler(0, 20f, 0));
            ArtistBits.PostCollider(ctx.Root, pole, 6f, .1f);
            var box = c + face * 4f - side * 2f; box = ctx.OnGround(box.x, box.z); mk.Box(WorldMaterials.Solid(new Color(.2f, .3f, .55f), .6f), box + up * .65f, new Vector3(.6f, 1.3f, .6f), rot); ArtistBits.PostCollider(ctx.Root, box, 1.3f, .4f);
        }

        /// An old red gas pump with a lit globe on top, a little tin shed, a pickup waiting.
        void GasPump(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at, Vector3 face)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Side(face);
            var shed = at - face * 2f; mk.Box(WorldMaterials.Solid(new Color(.6f, .62f, .6f), .6f), shed + up * 1.4f, new Vector3(4f, 2.8f, 3.2f), rot); solid.Box(dark, shed + up * 1.4f, new Vector3(4f, 2.8f, 3.2f), rot);
            mk.Box(rail, shed + up * 2.9f, new Vector3(4.4f, .15f, 3.6f), rot * Quaternion.Euler(-6f, 0, 0)); mk.Box(dark, shed + face * 1.61f + up * 1f, new Vector3(1f, 2f, .04f), rot);
            var pump = at + face * 1.4f + side * 1f; pump = ctx.OnGround(pump.x, pump.z);
            mk.Box(stone, pump + up * .1f, new Vector3(1.4f, .2f, 1f), rot);
            mk.Box(WorldMaterials.Solid(new Color(.8f, .12f, .1f), .6f), pump + up * 1.05f, new Vector3(.7f, 1.7f, .5f), rot);
            mk.Box(cream, pump + up * 1.35f + face * .26f, new Vector3(.45f, .45f, .02f), rot);
            var globe = pump + up * 2.15f; mk.Blob(GlowM(new Color(1f, .95f, .8f), 2.6f), globe, new Vector3(.26f, .3f, .14f), ctx.Rng, 0f); halos.Add(globe, .9f, Warm, .8f);
            mk.Box(dark, pump + up * 1.1f + side * .38f, new Vector3(.06f, .5f, .1f), rot);
            ArtistBits.PostCollider(ctx.Root, pump, 2.2f, .5f);
            Pickup(ctx, mk, solid, ctx.OnGround(at.x + face.x * 1f - side.x * 3f, at.z + face.z * 1f - side.z * 3f));
        }

        void TownHouse(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at, Vector3 face, Material paint)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Side(face); var c = at - face * 1.2f;
            mk.Box(paint, c + up * 1.6f, new Vector3(6f, 3.2f, 5f), rot); solid.Box(paint, c + up * 1.6f, new Vector3(6f, 3.2f, 5f), rot);
            mk.GableRoof(roofM, c + up * 3.23f, new Vector3(6.6f, 0, 5.6f), 1.9f, Quaternion.LookRotation(side));
            var fr = c + face * 2.52f; mk.Box(dark, fr + up * 1.1f, new Vector3(1f, 2.1f, .04f), rot);
            foreach (var s in new[] { -1.9f, 1.9f }) { mk.Box(trim, fr + up * 1.75f + side * s, new Vector3(1.1f, 1.1f, .04f), rot); mk.Box(GlowM(Warm, 1.6f), fr + up * 1.75f + side * s + face * .02f, new Vector3(.85f, .85f, .03f), rot); }
            mk.Box(plank, fr + face * 1.1f + up * .2f, new Vector3(6f, .4f, 2.2f), rot); solid.Box(plank, fr + face * 1.1f + up * .2f, new Vector3(6f, .4f, 2.2f), rot);
            foreach (var s in new[] { -2.8f, 2.8f }) { mk.Box(wood, fr + face * 2f + side * s + up * 1.6f, new Vector3(.15f, 2.4f, .15f), rot); ArtistBits.PostCollider(ctx.Root, fr + face * 2f + side * s, 2.8f, .12f); }
            mk.Box(roofM, fr + face * 1.2f + up * 2.85f, new Vector3(6.4f, .1f, 2.6f), rot * Quaternion.Euler(10f, 0, 0));
            Rocker(mk, solid, ctx, fr + face * 1f + up * .4f + side * 1.2f, face);
            mk.Prism(GlowM(Warm, 2.6f), fr + face * 2f + up * 2.3f, up, .12f, .25f, 8); halos.Add(fr + face * 2f + up * 2.42f, .9f, Warm, .8f);
            var ch = c - side * 2.4f + up * 3.8f; mk.Box(brick, ch, new Vector3(.8f, 2.6f, .8f), rot); life.chimneys.Add(ch + up * 1.4f);
        }

        // ---------------------------------------------------------------- the trestle, the mine, the stills
        /// A timber railroad trestle across the creek: bents of splayed posts with X braces, a deck with ties and
        /// rails, three coal hoppers standing on it, and the track running on onto the banks to a bumper.
        void Trestle(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up;
            for (var i = 8; i < creek.Count - 8; i += 3)
            {
                var p = creek[i]; var dd = Vector2.Distance(new Vector2(p.x, p.z), heart2); if (dd < 100f || dd > 260f) continue;
                var dir = creek[i + 1] - creek[i - 1]; dir.y = 0; dir.Normalize(); var across = Vector3.Cross(up, dir);
                if (millAt != Vector3.zero && Vector3.Distance(new Vector3(p.x, 0, p.z), new Vector3(millAt.x, 0, millAt.z)) < 50f) continue;
                if (NearPath(p.x, p.z, 26f)) continue;
                var bed = p.y; var deckY = Mathf.Max(bed + 5.5f, Mathf.Max(ctx.Ground(p.x + across.x * 24f, p.z + across.z * 24f), ctx.Ground(p.x - across.x * 24f, p.z - across.z * 24f)) + .5f);
                float Reach(float s) { var t = 8f; while (t < 34f && ctx.Ground(p.x + across.x * t * s, p.z + across.z * t * s) < deckY - .5f) t += 2f; return t; }
                float ra = Reach(1f), rb = Reach(-1f);
                var ok = true;
                for (var t = -rb - 10f; t <= ra + 10f && ok; t += 2f) { var q = p + across * t; if (!ctx.IsLand(q.x, q.z, 1f) && !InCreek(q.x, q.z, 2f) || InFootprint(ctx, q.x, q.z, 3f) || NearPath(q.x, q.z, 3f) || InNoGrass(q.x, q.z, 1f)) ok = false; }
                if (!ok) continue;
                var timber = WorldMaterials.Solid(new Color(.32f, .24f, .18f)); var rot = Quaternion.LookRotation(across);
                var a = new Vector3(p.x, deckY, p.z) - across * rb; var b = new Vector3(p.x, deckY, p.z) + across * ra; var mid = (a + b) / 2f; var len = ra + rb;
                mk.Box(timber, mid, new Vector3(3.2f, .5f, len), rot); solid.Box(timber, mid, new Vector3(3.2f, .5f, len), rot);
                for (var t = 0f; t <= len; t += .7f) mk.Box(timber, a + across * t + up * .3f, new Vector3(2.6f, .12f, .25f), rot);
                foreach (var s in new[] { -.72f, .72f }) mk.Box(rail, mid + dir * s + up * .42f, new Vector3(.1f, .14f, len), rot);
                for (var t = 2f; t < len - 1f; t += 4f)
                {
                    var bp = a + across * t; var g = InCreek(bp.x, bp.z, 1f) ? bed - .3f : ctx.Ground(bp.x, bp.z) - .3f; var hgt = deckY - g; if (hgt < .6f) continue;
                    foreach (var s in new[] { -1f, 1f })
                    {
                        var top = bp + dir * s * 1.2f; var foot = new Vector3(bp.x, g, bp.z) + dir * s * (1.2f + hgt * .12f);
                        mk.Box(timber, (top + foot) / 2f, new Vector3(.3f, Vector3.Distance(top, foot), .3f), Quaternion.LookRotation(across, top - foot));
                        ArtistBits.PostCollider(ctx.Root, foot + up * .3f, hgt, .25f); BoxCol(ctx.Root, (top + foot) / 2f, new Vector3(.35f, Vector3.Distance(top, foot), .35f), Quaternion.LookRotation(across, top - foot));   // the leg slants
                    }
                    for (var y = g + 2.5f; y < deckY - 1f; y += 3f)
                        foreach (var k in new[] { -1f, 1f }) { var cc = new Vector3(bp.x, y + 1.25f, bp.z); var span = dir * 3f + up * 2.5f * k; mk.Box(timber, cc, new Vector3(.18f, .18f, span.magnitude), Quaternion.LookRotation(span)); }
                }
                // the track on onto both banks, ending at a bumper
                foreach (var s in new[] { -1f, 1f })
                {
                    var start = s > 0 ? b : a; Vector3 last = start;
                    for (var t = .7f; t < 12f; t += .7f) { var q = start + across * s * t; q = ctx.OnGround(q.x, q.z); mk.Box(timber, q + up * .08f, new Vector3(2.6f, .12f, .25f), rot); last = q; }
                    foreach (var r2 in new[] { -.72f, .72f }) { var r0 = start + dir * r2; var r1 = last + dir * r2 + up * .2f; mk.Box(rail, (r0 + r1) / 2f + up * .1f, new Vector3(.1f, .14f, Vector3.Distance(r0, r1)), Quaternion.LookRotation(r1 - r0)); }
                    mk.Box(barnRed, last + across * s * .5f + up * .5f, new Vector3(2f, .8f, .4f), rot); solid.Box(barnRed, last + across * s * .5f + up * .5f, new Vector3(2f, .8f, .4f), rot);
                    noGrass.Add(new Vector3((start.x + last.x) / 2f, 7f, (start.z + last.z) / 2f));
                }
                // three coal hoppers
                var hopper = WorldMaterials.Solid(new Color(.22f, .2f, .2f), .5f);
                for (var k = -1; k <= 1; k++)
                {
                    var hc = mid + across * (k * 4.2f) + up * .5f;
                    foreach (var zz in new[] { -1.2f, 1.2f }) foreach (var s in new[] { -.75f, .75f }) mk.Prism(dark, hc + across * zz + dir * s * .9f + up * .35f, dir * s, .35f, .15f, 10);
                    mk.Box(hopper, hc + up * 1.7f, new Vector3(2.6f, 1.8f, 3.6f), rot); mk.Box(hopper, hc + up * .9f, new Vector3(1.6f, .6f, 2.4f), rot);
                    for (var l = 0; l < 4; l++) mk.Blob(coal, hc + up * 2.65f + across * (l * .8f - 1.2f), new Vector3(.6f, .35f, .5f), ctx.Rng, .4f);
                    solid.Box(hopper, hc + up * 1.4f, new Vector3(2.6f, 2.8f, 3.6f), rot);
                }
                Remember(new Vector3(mid.x, ctx.Ground(mid.x, mid.z), mid.z), 403);
                var mark = new GameObject("Shot: the trestle").transform; mark.SetParent(ctx.Root, false);
                var cam = p + dir * 30f + across * 8f; cam = ctx.OnGround(cam.x, cam.z); mark.SetPositionAndRotation(cam, Quaternion.LookRotation((mid - up * 2f - cam).normalized));
                return;
            }
        }
        Vector3 millAt;

        /// A coal mine up on the highest open ridge: a timber portal into the hillside, a cart track out of it with
        /// a loaded coal cart, a lantern, a coal heap, and an old sign.
        void Mine(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var best = Vector3.zero; var bestH = float.MinValue;
            for (var x = ctx.MinX; x < ctx.MaxX; x += 12f)
                for (var z = ctx.MinZ; z < ctx.MaxZ; z += 12f)
                {
                    var d = Vector2.Distance(new Vector2(x, z), heart2); if (d < 90f || d > 300f) continue;
                    if (!Open(ctx, x, z, 14f) || TrailNear(x, z, 16f)) continue;
                    var h = ctx.Ground(x, z); if (h > bestH) { bestH = h; best = new Vector3(x, h, z); }
                }
            if (bestH == float.MinValue) return;
            // face downhill
            var gx = ctx.Ground(best.x + 4f, best.z) - ctx.Ground(best.x - 4f, best.z); var gz = ctx.Ground(best.x, best.z + 4f) - ctx.Ground(best.x, best.z - 4f);
            var face = new Vector3(-gx, 0, -gz); if (face.sqrMagnitude < .001f) face = new Vector3(heart2.x - best.x, 0, heart2.y - best.z); face.Normalize();
            var rot = Quaternion.LookRotation(face); var side = Side(face); var c = best;
            var rock = WorldMaterials.Solid(new Color(.38f, .37f, .35f)); var timber = WorldMaterials.Solid(new Color(.34f, .26f, .19f));
            // the hillside it runs into
            // (blobs are not turned, so each is round in plan and kept far enough back to leave the portal clear)
            mk.Blob(rock, c - face * 7.6f + up * 2.5f, new Vector3(6.2f, 5.5f, 6.2f), ctx.Rng, .25f); mk.Blob(rock, c - face * 5.5f + side * 6.5f + up * 1.5f, new Vector3(4f, 3.5f, 4f), ctx.Rng, .3f); mk.Blob(rock, c - face * 5.5f - side * 6.8f + up * 1.2f, new Vector3(3.8f, 3f, 3.8f), ctx.Rng, .3f);
            mk.Box(rock, c - face * 2.6f + up * 2.1f, new Vector3(5.4f, 4.2f, 2.4f), rot);
            crowns.Blob(leaves[2], c - face * 8f + up * 7.4f, new Vector3(4.5f, 1.2f, 4.5f), ctx.Rng, .3f);
            solid.Box(rock, c - face * 6f + up * 2.5f, new Vector3(14f, 5f, 9f), rot);
            // the portal
            var mouth = c - face * .9f;
            mk.Box(dark, mouth + up * 1.5f - face * .3f, new Vector3(2.8f, 3f, .2f), rot);
            foreach (var s in new[] { -1f, 1f }) mk.Box(timber, mouth + side * s * 1.6f + up * 1.6f, new Vector3(.4f, 3.2f, .4f), rot);
            mk.Box(timber, mouth + up * 3.3f, new Vector3(4.2f, .45f, .5f), rot);
            mk.Box(cream, mouth + up * 4f + face * .1f, new Vector3(2.6f, .6f, .06f), rot);
            ArtistBits.Label(ctx.Root, "MINE No. 9", mouth + up * 4f + face * .15f, face, .3f, new Color(.15f, .12f, .1f), TextAnchor.MiddleCenter);
            var lamp = mouth + side * 1.6f + up * 2.6f + face * .35f; mk.Prism(GlowM(Warm, 2.8f), lamp - up * .15f, up, .13f, .3f, 8); halos.Add(lamp, 1f, Warm, .8f);
            ArtistBits.PointLight(ctx.Root, mouth + face * 2.5f + up * 2.5f, Warm, 12f, 1.3f);
            // the cart track and a loaded cart
            Vector3 prev = default;
            for (var t = 0f; t < 16f; t += .8f) { var q = mouth + face * t; q = ctx.OnGround(q.x, q.z); mk.Box(timber, q + up * .06f, new Vector3(1.4f, .1f, .2f), rot); if (t > 0f) foreach (var s in new[] { -.4f, .4f }) mk.Box(rail, (prev + q) / 2f + side * s + up * .17f, new Vector3(.06f, .08f, Vector3.Distance(prev, q) + .05f), Quaternion.LookRotation(q - prev)); prev = q; }
            var cart = ctx.OnGround(mouth.x + face.x * 5f, mouth.z + face.z * 5f);
            foreach (var zz in new[] { -.5f, .5f }) foreach (var s in new[] { -.4f, .4f }) mk.Prism(dark, cart + side * s + face * zz + up * .25f, side * Mathf.Sign(s), .22f, .08f, 10);
            mk.Prism(rail, cart + up * .45f, up, .75f, .8f, 4, 1.25f, 45f); for (var l = 0; l < 4; l++) mk.Blob(coal, cart + up * 1.3f + side * (l % 2 - .5f) * .5f + face * (l / 2 - .5f) * .5f, new Vector3(.35f, .25f, .35f), ctx.Rng, .4f);
            ArtistBits.PostCollider(ctx.Root, cart, 1.4f, .8f);
            var heap = ctx.OnGround(mouth.x + face.x * 7f + side.x * 4f, mouth.z + face.z * 7f + side.z * 4f);
            // a proper heap: rooted at the lowest ground round it and peaked well above the highest, lumps of coal
            // spilled round its foot, a touch lighter than the cart's coal, so it reads as a pile, not a flat black blot
            float hlo = heap.y, hhi = heap.y; for (var k = 0; k < 8; k++) { var a = k * Mathf.PI / 4f; var g = ctx.Ground(heap.x + Mathf.Cos(a) * 2.4f, heap.z + Mathf.Sin(a) * 2.4f); hlo = Mathf.Min(hlo, g); hhi = Mathf.Max(hhi, g); }
            var heapM = WorldMaterials.Solid(new Color(.15f, .145f, .16f), .85f); var lumps = new System.Random(9);   // own random, so nothing after the mine moves
            mk.Cone(heapM, new Vector3(heap.x, hlo - .3f, heap.z), 2.4f, hhi - hlo + 2.3f, 9);
            for (var k = 0; k < 7; k++) { var a = k * 51f * Mathf.Deg2Rad + .3f; var lp = new Vector3(heap.x + Mathf.Cos(a) * 2.6f, 0, heap.z + Mathf.Sin(a) * 2.6f); lp.y = ctx.Ground(lp.x, lp.z) + .1f; mk.Blob(heapM, lp, new Vector3(.35f, .22f, .3f), lumps, .4f); }
            solid.Box(coal, heap + up * .5f, new Vector3(3f, 1f, 3f), rot);
            noGrass.Add(new Vector3(mouth.x + face.x * 6f, 9f, mouth.z + face.z * 6f));
            var mark = new GameObject("Shot: the coal mine").transform; mark.SetParent(ctx.Root, false);
            var cam = mouth + face * 14f + side * 5f; cam = ctx.OnGround(cam.x, cam.z) + up * 3.5f; mark.SetPositionAndRotation(cam, Quaternion.LookRotation((mouth + up * 1.5f - cam - up * 1.6f).normalized));
            life.mists.Add(mouth + face * 4f);
        }

        /// A moonshine still hidden in the woods: a copper pot on a stone fire, a cooling barrel, jugs, smoke.
        void Moonshine(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at)
        {
            var up = Vector3.up; var copper = WorldMaterials.Solid(new Color(.8f, .45f, .25f), .8f); var d = Quaternion.Euler(0, ctx.Range(0, 360f), 0) * Vector3.forward; var side = Side(d);
            for (var k = 0; k < 7; k++) mk.Blob(stone, at + Quaternion.Euler(0, k * 51f, 0) * Vector3.forward * .8f + up * .2f, new Vector3(.3f, .25f, .3f), ctx.Rng, .3f);
            mk.Prism(copper, at + up * .45f, up, .65f, .9f, 12, .55f); mk.Prism(copper, at + up * 1.35f, up, .14f, .4f, 8); mk.Prism(copper, at + up * 1.7f, (side + up * .15f).normalized, .05f, 1.5f, 6);
            var keg = at + side * 1.7f; mk.Prism(wood, keg, up, .45f, 1f, 10); for (var k = 0; k < 3; k++) mk.Prism(copper, keg + up * (.2f + k * .3f), up, .47f, .05f, 10);
            for (var k = 0; k < 3; k++) { var j = at - side * 1.2f + d * (k * .4f - .4f); mk.Prism(WorldMaterials.Solid(new Color(.85f, .82f, .72f)), j, up, .16f, .3f, 8, .5f); mk.Prism(WorldMaterials.Solid(new Color(.5f, .35f, .2f)), j + up * .3f, up, .04f, .1f, 6); }
            solid.Box(copper, at + up * .8f + side * .6f, new Vector3(3.6f, 1.6f, 1.6f), Quaternion.LookRotation(side));
            life.fires.Add(at + up * .1f); life.chimneys.Add(at + up * 2f);
            var fl = ArtistBits.PointLight(ctx.Root, at + up * .6f, new Color(1f, .55f, .2f), 7f, 1.2f); fl.gameObject.AddComponent<FireFlicker>().baseIntensity = 1.2f;
            noGrass.Add(new Vector3(at.x, 2.5f, at.z));
        }
    }
}
