using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The festival round the stages: a Ferris wheel turning on the highest open hill, campsites of bright tents
    /// with bunting and a fire, food trucks with lit hatches and fairy lights, hay bales with synths set up on
    /// them, giant inflatables (a flower, a smiling sun, a rainbow arch, a mushroom), light towers, a row of
    /// portable toilets, and glowsticks dropped in the grass.
    public partial class ElectricKit
    {
        void Festival(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            FerrisWheel(ctx, mk, solid);
            int camps = 0, trucks = 0, bales = 0, inflatables = 0, towers = 0, loos = 0;
            const float cell = 42f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + ctx.Range(0f, cell), pz = z + ctx.Range(0f, cell);
                if (!Open(ctx, px, pz, 7f)) continue;
                var p = ctx.OnGround(px, pz);
                var toGate = new Vector3(center.x - px, 0, center.z - pz).normalized;
                var nearWalk = NearWalk(px, pz, 45f);
                var roll = ctx.Range(0f, 1f);
                if (roll < .15f) { Campsite(ctx, mk, solid, p, camps); camps++; }
                else if (roll < .3f && nearWalk) { FoodTruck(ctx, mk, solid, p, toGate); trucks++; }
                else if (roll < .55f && bales < 40) { HayBales(ctx, mk, solid, p, toGate); bales++; }
                else if (roll < .63f) { Inflatable(ctx, mk, solid, p, inflatables++); }
                else if (roll < .72f) { LightTower(ctx, mk, solid, p); towers++; }
                else if (roll < .78f && loos < 3 && nearWalk) { Loos(ctx, mk, solid, p, toGate); loos++; }
                else if (roll < .9f) Glowsticks(ctx, mk, p);
                else continue;
                noGrass.Add(new Vector3(px, 5f, pz));
            }
            Debug.Log($"The Electric Fields: {camps} campsites, {trucks} food trucks, {bales} hay-bale synths, {inflatables} inflatables, {towers} light towers, {loos} loo rows");
        }

        bool NearWalk(float x, float z, float within)
        {
            foreach (var (p, _) in walkPoints) { float dx = p.x - x, dz = p.z - z; if (dx * dx + dz * dz < within * within) return true; }
            return false;
        }

        // ---------------------------------------------------------------- the Ferris wheel
        void FerrisWheel(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            Vector3 best = default; var bestH = float.MinValue;
            for (var t = 0; t < 400; t++)
            {
                float x = ctx.Range(ctx.MinX, ctx.MaxX), z = ctx.Range(ctx.MinZ, ctx.MaxZ);
                if (!Open(ctx, x, z, 16f)) continue;
                var d = Vector2.Distance(new Vector2(x, z), new Vector2(center.x, center.z));
                if (d < 90f || d > 320f) continue;
                var h = ctx.Ground(x, z) - d * .02f;
                if (h > bestH) { bestH = h; best = ctx.OnGround(x, z); }
            }
            if (bestH == float.MinValue) return;
            var face = new Vector3(center.x - best.x, 0, center.z - best.z).normalized; var side = Vector3.Cross(Vector3.up, face); var up = Vector3.up;
            const float R = 14f;
            var hub = best + up * (R + 2.5f);
            foreach (var s in new[] { -1f, 1f })
                foreach (var f in new[] { -1f, 1f })
                {
                    var foot = best + side * f * 7f + face * s * 1.6f; foot.y = ctx.Ground(foot.x, foot.z) - .3f;
                    var d = hub + face * s * 1.6f - foot;
                    mk.Box(metal, (foot + hub + face * s * 1.6f) / 2f, new Vector3(.4f, .4f, d.magnitude), Quaternion.LookRotation(d));
                    ArtistBits.PostCollider(ctx.Root, foot, 3f, .4f);
                }
            mk.Box(dark, best + up * .6f, new Vector3(5f, 1.2f, 3f), Quaternion.LookRotation(face));
            solid.Box(dark, best + up * .6f, new Vector3(5f, 1.2f, 3f), Quaternion.LookRotation(face));
            // the wheel itself turns (ElectricLife): rims, spokes, light bulbs, and gondolas that hang level
            var wheel = new GameObject("Ferris wheel").transform; wheel.SetParent(ctx.Root, false);
            wheel.SetPositionAndRotation(hub, Quaternion.LookRotation(face));
            var wk = new MeshKit(); var white = WorldMaterials.Solid(new Color(.95f, .94f, .9f));
            const int n = 32;
            var bulbs = new Material(GlowM(new Color(1f, .8f, .5f), 2.4f)) { name = "Wheel bulbs" };
            for (var k = 0; k < n; k++)
            {
                float a0 = k * Mathf.PI * 2f / n, a1 = (k + 1) * Mathf.PI * 2f / n;
                foreach (var zf in new[] { -.8f, .8f })
                {
                    var p0 = new Vector3(Mathf.Cos(a0) * R, Mathf.Sin(a0) * R, zf); var p1 = new Vector3(Mathf.Cos(a1) * R, Mathf.Sin(a1) * R, zf);
                    wk.Box(white, (p0 + p1) / 2f, new Vector3(.25f, .25f, Vector3.Distance(p0, p1) + .05f), Quaternion.LookRotation(p1 - p0));
                    wk.Blob(bulbs, p0 * 1.02f, Vector3.one * .14f, ctx.Rng, 0f);
                }
                if (k % 2 == 0)
                {
                    var rim = new Vector3(Mathf.Cos(a0) * R, Mathf.Sin(a0) * R, 0);
                    wk.Box(white, rim / 2f, new Vector3(.15f, .15f, R), Quaternion.LookRotation(rim));
                }
            }
            wk.Prism(dark, new Vector3(0, 0, -1.6f), Vector3.forward, .8f, 3.2f, 10);
            wk.Build("Wheel", wheel, false);
            var gondolas = new List<Transform>();
            for (var k = 0; k < 12; k++)
            {
                var g = new GameObject("Gondola").transform; g.SetParent(wheel, false);
                var a = k * Mathf.PI * 2f / 12f; g.localPosition = new Vector3(Mathf.Cos(a) * R, Mathf.Sin(a) * R, 0);
                var gk = new MeshKit(); var col = WorldMaterials.Solid(Bunting[k % Bunting.Length]);
                gk.Box(col, new Vector3(0, -1.4f, 0), new Vector3(1.6f, 1.4f, 1.4f), Quaternion.identity);
                gk.Box(white, new Vector3(0, -.5f, 0), new Vector3(1.8f, .15f, 1.6f), Quaternion.identity);
                gk.Box(metal, new Vector3(0, -.25f, 0), new Vector3(.08f, .5f, .08f), Quaternion.identity);
                gk.Build("Car", g, false);
                gondolas.Add(g);
            }
            life.ferris = wheel; life.gondolas = gondolas; life.wheelBulbs = bulbs;
            halos.Add(hub + face * 3f, 8f, new Color(1f, .78f, .5f), .6f);
            ArtistBits.PointLight(ctx.Root, hub + face * 4f, new Color(1f, .78f, .5f), 30f, 1.4f);
            noGrass.Add(new Vector3(best.x, 12f, best.z));
        }

        // ---------------------------------------------------------------- the rest
        void Campsite(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at, int index)
        {
            var up = Vector3.up; var n = ctx.Rng.Next(3, 7);
            for (var k = 0; k < n; k++)
            {
                var p = ctx.OnGround(at.x + ctx.Range(-7f, 7f), at.z + ctx.Range(-7f, 7f));
                var rot = Quaternion.Euler(0, ctx.Range(0, 360f), 0); var fwd = rot * Vector3.forward; var rt = rot * Vector3.right;
                var col = WorldMaterials.Solid(Bunting[ctx.Rng.Next(Bunting.Length)]);
                var l = ctx.Range(2.2f, 3f); var w = ctx.Range(1.6f, 2.2f); var h = ctx.Range(1.1f, 1.5f);
                if (ctx.Chance(.5f))
                {
                    // a ridge tent
                    var a = p - fwd * l / 2f; var b = p + fwd * l / 2f;
                    foreach (var s in new[] { -1f, 1f }) mk.Quad(col, a + rt * s * w / 2f, b + rt * s * w / 2f, b + up * h, a + up * h, p - rt * s);
                    foreach (var e in new[] { a, b }) mk.Tri(col, e - rt * w / 2f, e + rt * w / 2f, e + up * h, p);
                }
                else
                {
                    // a dome tent
                    mk.Blob(col, p + up * .2f, new Vector3(w * .55f, h, l * .5f), ctx.Rng, 0f);
                }
                solid.Box(col, p + up * h / 2f, new Vector3(w, h, l), rot);
            }
            // a little fire with logs and a flag on a pole
            mk.Blob(GlowM(new Color(1f, .55f, .2f), 3f), at + up * .2f, new Vector3(.35f, .2f, .35f), ctx.Rng, .3f);
            halos.Add(at + up * .5f, 1.6f, new Color(1f, .55f, .2f), 1f);
            if (index % 3 == 0)                                                    // a real light at every third fire only (lights are costly)
            {
                var fire = ArtistBits.PointLight(ctx.Root, at + up * .8f, new Color(1f, .6f, .3f), 8f, 1.4f);
                fire.gameObject.AddComponent<FireFlicker>().baseIntensity = 1.4f;
            }
            var pole = ctx.OnGround(at.x + 3f, at.z + 2f);
            mk.Prism(wood, pole, up, .05f, 5f, 5);
            var fc = WorldMaterials.Solid(Bunting[ctx.Rng.Next(Bunting.Length)]);
            soft.Tri(fc, pole + up * 5f, pole + up * 4f, pole + up * 4.5f + Vector3.right * 1.4f, pole + Vector3.forward);
            soft.Tri(fc, pole + up * 5f, pole + up * 4f, pole + up * 4.5f + Vector3.right * 1.4f, pole - Vector3.forward);
        }

        void FoodTruck(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at, Vector3 face)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(Vector3.Cross(Vector3.up, face)); var side = face;
            var col = WorldMaterials.Solid(Bunting[ctx.Rng.Next(Bunting.Length)]);
            mk.Box(col, at + up * 1.7f, new Vector3(2.4f, 2.6f, 5.5f), rot);
            mk.Box(WorldMaterials.Solid(Ink), at + up * 3.1f, new Vector3(2.5f, .2f, 5.6f), rot);
            foreach (var z in new[] { -1.7f, 1.7f }) mk.Prism(dark, at + rot * new Vector3(-1.25f, .45f, z), rot * Vector3.right, .45f, 2.5f, 10);
            var hatch = at + up * 1.9f + side * 1.22f;
            var lit = GlowM(new Color(1f, .78f, .45f), 1.1f);                       // warm, not blown out to white
            mk.Box(lit, hatch, new Vector3(.05f, 1f, 3f), Quaternion.LookRotation(side) * Quaternion.Euler(0, 90f, 0));
            mk.Box(col, hatch + up * .75f + side * .5f, new Vector3(1f, .06f, 3.2f), Quaternion.LookRotation(side) * Quaternion.Euler(0, 90f, 0) * Quaternion.Euler(0, 0, -20f));
            halos.Add(hatch + side * 1.4f, 2f, new Color(1f, .85f, .55f), .8f);
            ArtistBits.PointLight(ctx.Root, hatch + side * 1.5f, new Color(1f, .85f, .55f), 9f, 1.2f);
            solid.Box(col, at + up * 1.7f, new Vector3(2.4f, 3f, 5.5f), rot);
            // a couple of picnic tables in front
            for (var k = 0; k < 2; k++)
            {
                var t = at + side * 4f + Vector3.Cross(Vector3.up, side) * (k * 3f - 1.5f); t = ctx.OnGround(t.x, t.z);
                var tr = Quaternion.LookRotation(side); var along = Vector3.Cross(Vector3.up, side);
                mk.Box(wood, t + up * .75f, new Vector3(.8f, .06f, 1.8f), tr);
                foreach (var s in new[] { -.65f, .65f }) mk.Box(wood, t + up * .45f + along * s, new Vector3(.3f, .05f, 1.8f), tr);
                // A-frame legs at both ends, from the ground up through the seats to the top
                foreach (var e in new[] { -.75f, .75f })
                    foreach (var s in new[] { -1f, 1f })
                    {
                        var foot = t + side * e + along * s * .75f; var top = t + side * e + up * .72f + along * s * .15f;
                        mk.Box(wood, (foot + top) / 2f, new Vector3(.08f, Vector3.Distance(foot, top), .08f), Quaternion.FromToRotation(Vector3.up, (top - foot).normalized));
                    }
                solid.Box(wood, t + up * .4f, new Vector3(1.8f, .8f, 1.8f), Quaternion.LookRotation(side));
            }
        }

        void HayBales(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at, Vector3 face)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(Vector3.up, face);
            for (var k = 0; k < 3; k++)
            {
                var p = at + side * ((k - 1) * 1.6f);
                mk.Box(straw, p + up * .45f, new Vector3(1.5f, .9f, .9f), rot * Quaternion.Euler(0, ctx.Range(-4f, 4f), 0));
                solid.Box(straw, p + up * .45f, new Vector3(1.5f, .9f, .9f), rot);
            }
            mk.Box(straw, at + up * 1.35f, new Vector3(1.5f, .9f, .9f), rot * Quaternion.Euler(0, 8f, 0));
            // synths on top, glowing keys, a lamp and a cable snaking off
            foreach (var s in new[] { -1.6f, 1.6f })
            {
                var syn = at + side * s + up * .95f;
                mk.Box(dark, syn, new Vector3(1.2f, .12f, .45f), rot);
                for (var key = 0; key < 7; key++) mk.Box(GlowM(Neon[(key + (s > 0 ? 2 : 0)) % Neon.Length], 2.2f), syn + up * .07f + side * ((key - 3f) * .15f) + face * .08f, new Vector3(.1f, .02f, .18f), rot);
            }
            var lamp = at + up * 1.85f;
            mk.Prism(GlowM(new Color(1f, .8f, .5f), 2.4f), lamp, up, .18f, .4f, 8);
            halos.Add(lamp + up * .2f, 1.6f, new Color(1f, .8f, .5f), .8f);
        }

        void Inflatable(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at, int which)
        {
            var up = Vector3.up; var shiny = .6f;
            switch (which % 4)
            {
                case 0: // a giant flower
                    mk.Prism(WorldMaterials.Solid(new Color(.4f, .75f, .35f), shiny), at, up, .5f, 7f, 10);
                    for (var k = 0; k < 8; k++) { var d = Quaternion.Euler(0, k * 45f, 0) * Vector3.forward; mk.Blob(WorldMaterials.Solid(new Color(1f, .45f, .65f), shiny), at + up * 7.5f + d * 1.9f, new Vector3(1.2f, .5f, 1.2f), ctx.Rng, 0f); }
                    mk.Blob(WorldMaterials.Solid(new Color(1f, .85f, .3f), shiny), at + up * 7.6f, new Vector3(1.4f, .7f, 1.4f), ctx.Rng, 0f);
                    ArtistBits.PostCollider(ctx.Root, at, 7f, .6f);
                    break;
                case 1: // a smiling sun
                    var sc = at + up * 4.5f; var face = new Vector3(center.x - at.x, 0, center.z - at.z).normalized;
                    mk.Blob(WorldMaterials.Solid(new Color(1f, .8f, .2f), shiny), sc, Vector3.one * 2.6f, ctx.Rng, 0f);
                    for (var k = 0; k < 10; k++) { var d = Quaternion.AngleAxis(k * 36f, face) * Vector3.Cross(Vector3.up, face); mk.Prism(WorldMaterials.Solid(new Color(1f, .6f, .2f), shiny), sc + d * 2.4f, d, .5f, 1.5f, 6, .2f); }
                    foreach (var s in new[] { -.8f, .8f }) mk.Blob(dark, sc + face * 2.45f + Vector3.Cross(Vector3.up, face) * s + up * .5f, new Vector3(.25f, .35f, .1f), ctx.Rng, 0f);
                    for (var k = 0; k < 7; k++) { var a = Mathf.PI * (k + 1) / 8f; mk.Blob(dark, sc + face * 2.4f + Vector3.Cross(Vector3.up, face) * Mathf.Cos(a) * 1.1f - up * (.4f + Mathf.Sin(a) * .5f), Vector3.one * .12f, ctx.Rng, 0f); }
                    solid.Box(dark, at + up * 3f, new Vector3(4f, 6f, 4f), Quaternion.identity);
                    break;
                case 2: // a rainbow arch
                    var dirA = Quaternion.Euler(0, ctx.Range(0, 180f), 0) * Vector3.right;
                    Color[] bands = { new Color(.95f, .3f, .3f), new Color(1f, .65f, .2f), new Color(1f, .9f, .3f), new Color(.4f, .8f, .4f), new Color(.35f, .55f, 1f), new Color(.65f, .4f, .9f) };
                    for (var b = 0; b < bands.Length; b++)
                        for (var k = 0; k < 14; k++)
                        {
                            float a0 = Mathf.PI * k / 14f, a1 = Mathf.PI * (k + 1) / 14f, r = 6f - b * .55f;
                            var p0 = at + dirA * Mathf.Cos(a0) * r + up * Mathf.Sin(a0) * r; var p1 = at + dirA * Mathf.Cos(a1) * r + up * Mathf.Sin(a1) * r;
                            mk.Prism(WorldMaterials.Solid(bands[b], shiny), p0, (p1 - p0).normalized, .3f, Vector3.Distance(p0, p1) + .1f, 8);
                        }
                    foreach (var s in new[] { -1f, 1f }) solid.Box(dark, at + dirA * s * 4.6f + up * 1.6f, new Vector3(3.4f, 3.2f, .8f), Quaternion.LookRotation(Vector3.Cross(Vector3.up, dirA)));
                    break;
                default: // a mushroom
                    mk.Prism(WorldMaterials.Solid(new Color(.95f, .92f, .85f), shiny), at, up, 1f, 4f, 10, .8f);
                    mk.Blob(WorldMaterials.Solid(new Color(.9f, .25f, .3f), shiny), at + up * 4.6f, new Vector3(3.6f, 1.8f, 3.6f), ctx.Rng, 0f);
                    for (var k = 0; k < 9; k++) { var d = Quaternion.Euler(0, k * 40f, 0) * Vector3.forward; mk.Blob(WorldMaterials.Solid(Ink), at + up * (5.2f + (k % 3) * .3f) + d * (1.6f + (k % 2) * .9f), new Vector3(.4f, .15f, .4f), ctx.Rng, 0f); }
                    ArtistBits.PostCollider(ctx.Root, at, 4f, 1.1f);
                    break;
            }
        }

        void LightTower(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at)
        {
            var up = Vector3.up; const float H = 10f;
            foreach (var (x, z) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) }) mk.Box(metal, at + new Vector3(x, 0, z) * .7f + up * H / 2f, new Vector3(.12f, H, .12f), Quaternion.identity);
            for (var y = 1.5f; y < H; y += 2f) mk.Box(metal, at + up * y, new Vector3(1.5f, .08f, 1.5f), Quaternion.identity);
            solid.Box(metal, at + up * H / 2f, new Vector3(1.6f, H, 1.6f), Quaternion.identity);
            var col = Neon[ctx.Rng.Next(Neon.Length)];
            for (var k = 0; k < 4; k++)
            {
                var d = Quaternion.Euler(0, k * 90f, 0) * Vector3.forward;
                mk.Prism(GlowM(col, 3f), at + up * (H + .3f) + d * .5f, (d - up * .4f).normalized, .25f, .35f, 8);
            }
            halos.Add(at + up * (H + .3f), 3f, col, .9f);
            life.towerLights.Add(ArtistBits.PointLight(ctx.Root, at + up * (H - 1f), col, 20f, 1.4f));
            life.towers.Add(at + up * (H + .5f));
        }

        void Loos(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at, Vector3 face)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(Vector3.up, face);
            for (var k = 0; k < 5; k++)
            {
                var p = at + side * ((k - 2f) * 1.35f); p = ctx.OnGround(p.x, p.z);
                var col = WorldMaterials.Solid(k % 2 == 0 ? new Color(.3f, .55f, .85f) : new Color(.35f, .7f, .45f));
                mk.Box(col, p + up * 1.15f, new Vector3(1.2f, 2.3f, 1.2f), rot);
                mk.Box(WorldMaterials.Solid(Ink), p + up * 2.35f, new Vector3(1.25f, .12f, 1.25f), rot);
                mk.Box(WorldMaterials.Solid(k == 2 ? new Color(.85f, .3f, .3f) : new Color(.4f, .8f, .4f)), p + up * 1.5f + face * .61f + side * .35f, new Vector3(.18f, .08f, .02f), rot);
            }
            solid.Box(dark, at + up * 1.15f, new Vector3(6.8f, 2.3f, 1.2f), rot);
        }

        void Glowsticks(LandContext ctx, MeshKit mk, Vector3 at)
        {
            for (var k = 0; k < 9; k++)
            {
                var p = ctx.OnGround(at.x + ctx.Range(-4f, 4f), at.z + ctx.Range(-4f, 4f)) + Vector3.up * .03f;
                var col = Neon[ctx.Rng.Next(Neon.Length)];
                mk.Box(GlowM(col, 3f), p, new Vector3(.04f, .04f, .25f), Quaternion.Euler(0, ctx.Range(0, 360f), 0));
                if (k % 3 == 0) halos.Add(p + Vector3.up * .1f, .5f, col, .8f);
            }
        }
    }
}
