using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Kingdom's countryside between the legends: big oaks, hedgerows, circles of standing stones shaped like
    /// records (grooves and a label), jukeboxes in lay-bys by the road glowing warm, vintage cars parked up,
    /// sheep grazing, haystacks, a few stone cottages, and long grass with buttercups and poppies.
    public partial class ClassicKit
    {
        void Country(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            int oaks = 0, hedges = 0, circles = 0, jukes = 0, cars = 0, flocks = 0, stacks = 0, cottages = 0;
            const float cell = 40f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + ctx.Range(0f, cell), pz = z + ctx.Range(0f, cell);
                if (!Open(ctx, px, pz, 6f)) continue;
                var p = ctx.OnGround(px, pz);
                var nearRoad = NearPath(px, pz, 14f);
                var roll = ctx.Range(0f, 1f);
                if (nearRoad && jukes < 6 && roll < .25f) { Jukebox(ctx, mk, solid, p); jukes++; }
                else if (nearRoad && cars < 8 && roll < .45f) { VintageCar(ctx, mk, solid, p, Paint[cars % Paint.Length]); cars++; }
                else if (roll < .3f) { for (var k = 0; k < ctx.Rng.Next(1, 3); k++) Oak(ctx, mk, ctx.OnGround(px + ctx.Range(-6f, 6f), pz + ctx.Range(-6f, 6f)), ctx.Range(.9f, 1.4f)); oaks++; }
                else if (roll < .45f) { Hedgerow(ctx, mk, solid, p); hedges++; }
                else if (roll < .5f && circles < 3) { RecordStones(ctx, mk, solid, p); circles++; }
                else if (roll < .6f) { Sheep(ctx, mk, p); flocks++; }
                else if (roll < .68f) { Haystacks(ctx, mk, solid, p); stacks++; }
                else if (roll < .72f && cottages < 5) { Cottage(ctx, mk, solid, p); cottages++; }
                else continue;
                noGrass.Add(new Vector3(px, 4f, pz));
            }
            Debug.Log($"Kingdom of Classic Rock: {oaks} oak groups, {hedges} hedgerows, {circles} record stone circles, {jukes} jukeboxes, {cars} vintage cars, {flocks} flocks, {stacks} haystacks, {cottages} cottages");
        }

        void Hedgerow(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at)
        {
            var dir = Quaternion.Euler(0, ctx.Range(0, 180f), 0) * Vector3.forward; var n = ctx.Rng.Next(5, 11);
            for (var k = 0; k < n; k++)
            {
                var p = ctx.OnGround(at.x + dir.x * (k - n / 2f) * 2.2f, at.z + dir.z * (k - n / 2f) * 2.2f);
                soft.Blob(hedge, p + Vector3.up * .9f, new Vector3(1.4f, 1f, 1f), ctx.Rng, .3f);
                if (ctx.Chance(.2f)) soft.Blob(WorldMaterials.Solid(new Color(.95f, .92f, .85f)), p + Vector3.up * 1.6f + Vector3.Cross(Vector3.up, dir) * .6f, Vector3.one * .12f, ctx.Rng, .2f);
            }
            solid.Box(hedge, at + Vector3.up * .9f, new Vector3(1.6f, 1.8f, n * 2.2f), Quaternion.LookRotation(dir));
        }

        /// A circle of standing stones, each a great black record on edge: grooves, a red label, a hole.
        void RecordStones(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at)
        {
            var up = Vector3.up; var vinyl = WorldMaterials.Solid(new Color(.08f, .08f, .09f), .7f); var groove = WorldMaterials.Solid(new Color(.18f, .18f, .2f), .8f);
            Color[] labels = { new Color(.85f, .2f, .2f), new Color(.95f, .8f, .3f), new Color(.3f, .5f, .85f), new Color(.95f, .95f, .9f) };
            for (var k = 0; k < 7; k++)
            {
                var a = k * Mathf.PI * 2f / 7f; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                var p = ctx.OnGround(at.x + d.x * 9f, at.z + d.z * 9f); var r = ctx.Range(1.6f, 2.2f);
                var c = p + up * (r - .4f);
                mk.Prism(vinyl, c - d * .12f, d, r, .24f, 24);
                mk.Prism(vinyl, c + d * .12f, -d, r, .24f, 24);
                foreach (var s in new[] { 1f, -1f }) { mk.Prism(groove, c + d * s * .125f, d * s, r * .75f, .01f, 24); mk.Prism(WorldMaterials.Solid(labels[k % labels.Length]), c + d * s * .138f, d * s, r * .32f, .01f, 16); mk.Prism(dark, c + d * s * .151f, d * s, .06f, .01f, 8); }
                solid.Box(vinyl, c, new Vector3(r * 1.6f, r * 2f, .4f), Quaternion.LookRotation(d));
            }
            mk.Prism(stoneM, at - up * .2f, up, 1.4f, .7f, 10);
            solid.Prism(stoneM, at - up * .2f, up, 1.4f, .7f, 10);
            noGrass.Add(new Vector3(at.x, 12f, at.z));
            life.landmarks.Add(at);
        }

        /// A jukebox in a lay-by: rounded top glowing in bands of colour, a little lamp.
        void Jukebox(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at)
        {
            var up = Vector3.up; var face = new Vector3(center.x - at.x, 0, center.z - at.z).normalized; var rot = Quaternion.LookRotation(face);
            mk.Box(WorldMaterials.Solid(new Color(.55f, .3f, .2f), .6f), at + up * .9f, new Vector3(1.2f, 1.8f, .8f), rot);
            mk.Prism(WorldMaterials.Solid(new Color(.55f, .3f, .2f), .6f), at + up * 1.8f - face * .4f, face, .6f, .8f, 16);
            Color[] bands = { new Color(1f, .4f, .2f), new Color(1f, .85f, .3f), new Color(.3f, .8f, 1f) };
            for (var b = 0; b < 3; b++) mk.Box(GlowM(bands[b], 2.4f), at + up * (.5f + b * .45f) + face * .41f, new Vector3(1f, .12f, .02f), rot);
            mk.Box(GlowM(new Color(1f, .9f, .7f), 1.8f), at + up * 1.75f + face * .41f, new Vector3(.8f, .35f, .02f), rot);
            halos.Add(at + up * 1.2f + face * 1.2f, 1.6f, new Color(1f, .7f, .4f), .8f);
            solid.Box(dark, at + up * 1.1f, new Vector3(1.2f, 2.2f, .8f), rot);
            life.jukeboxes.Add(at + up * 1.2f);
        }

        /// A fifties car: a long low body, a glassy cabin set back, tail fins, chrome bumpers, whitewall tyres.
        void VintageCar(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at, Color paint, Vector3? facing = null)
        {
            var up = Vector3.up; var dir = facing ?? Quaternion.Euler(0, ctx.Range(0, 360f), 0) * Vector3.forward; dir.y = 0; dir.Normalize();
            var rot = Quaternion.LookRotation(dir); var side = Vector3.Cross(up, dir);
            var body = WorldMaterials.Solid(paint, .85f); var glass = WorldMaterials.Solid(new Color(.2f, .28f, .35f), .95f); var chrome = WorldMaterials.Solid(new Color(.85f, .86f, .88f), .95f);
            mk.Box(body, at + up * .72f, new Vector3(2f, .62f, 5f), rot);                                   // the body
            mk.Box(body, at + up * 1.06f + dir * 1.5f, new Vector3(1.9f, .12f, 1.8f), rot * Quaternion.Euler(4f, 0, 0));   // bonnet
            mk.Box(body, at + up * 1.3f - dir * .4f, new Vector3(1.75f, .55f, 2f), rot);                  // cabin
            mk.Box(body, at + up * 1.62f - dir * .4f, new Vector3(1.8f, .1f, 2.1f), rot);                 // roof
            mk.Box(glass, at + up * 1.3f - dir * .4f, new Vector3(1.78f, .42f, 1.9f), rot);               // windows all round
            mk.Box(glass, at + up * 1.3f + dir * .62f, new Vector3(1.6f, .45f, .06f), rot * Quaternion.Euler(-30f, 0, 0));
            foreach (var s in new[] { -1f, 1f })
            {
                mk.Tri(body, at + side * s * .9f + up * 1.03f - dir * 2.5f, at + side * s * .9f + up * 1.03f - dir * 1.3f, at + side * s * .9f + up * 1.45f - dir * 2.55f, side * s);     // tail fin
                mk.Tri(body, at + side * s * .9f + up * 1.03f - dir * 2.5f, at + side * s * .9f + up * 1.45f - dir * 2.55f, at + side * s * .9f + up * 1.03f - dir * 1.3f, -side * s);
                mk.Box(chrome, at + side * s * 1.005f + up * .78f, new Vector3(.02f, .06f, 4.6f), rot);                    // chrome trim
                mk.Box(GlowM(new Color(1f, .2f, .15f), 1.6f), at + side * s * .7f + up * .85f - dir * 2.51f, new Vector3(.3f, .14f, .02f), rot);
                mk.Prism(GlowM(new Color(1f, .95f, .8f), 1.4f), at + up * .82f + dir * 2.48f + side * s * .7f, dir, .15f, .05f, 10);
                foreach (var z in new[] { -1.6f, 1.55f })
                {
                    var w = at + side * s * .92f + dir * z + up * .38f;
                    mk.Prism(dark, w - side * s * .14f, side * s, .38f, .28f, 14);
                    mk.Prism(white, w + side * s * .141f, side * s, .26f, .01f, 14);
                    mk.Prism(chrome, w + side * s * .145f, side * s, .13f, .01f, 10);
                }
            }
            foreach (var z in new[] { -2.55f, 2.55f }) mk.Box(chrome, at + up * .48f + dir * z, new Vector3(2.05f, .16f, .12f), rot);
            mk.Box(chrome, at + up * .7f + dir * 2.52f, new Vector3(1.2f, .25f, .04f), rot);               // grille
            solid.Box(body, at + up * .85f, new Vector3(2.1f, 1.7f, 5.2f), rot);
        }

        void Sheep(LandContext ctx, MeshKit mk, Vector3 at)
        {
            var wool = WorldMaterials.Solid(new Color(.95f, .94f, .9f)); var face = WorldMaterials.Solid(new Color(.15f, .15f, .15f));
            for (var k = 0; k < ctx.Rng.Next(3, 8); k++)
            {
                var p = ctx.OnGround(at.x + ctx.Range(-8f, 8f), at.z + ctx.Range(-8f, 8f)); var d = Quaternion.Euler(0, ctx.Range(0, 360f), 0) * Vector3.forward;
                mk.Blob(wool, p + Vector3.up * .7f, new Vector3(.45f, .4f, .65f), ctx.Rng, .2f);
                mk.Blob(face, p + Vector3.up * .75f + d * .65f, new Vector3(.15f, .18f, .2f), ctx.Rng, 0f);
                foreach (var (x, z) in new[] { (-.2f, -.3f), (.2f, -.3f), (-.2f, .3f), (.2f, .3f) }) mk.Box(face, p + Quaternion.LookRotation(d) * new Vector3(x, .2f, z), new Vector3(.08f, .4f, .08f), Quaternion.LookRotation(d));
            }
        }

        void Haystacks(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at)
        {
            var hay = WorldMaterials.Solid(new Color(.88f, .75f, .4f));
            for (var k = 0; k < ctx.Rng.Next(2, 5); k++)
            {
                var p = ctx.OnGround(at.x + ctx.Range(-7f, 7f), at.z + ctx.Range(-7f, 7f)); var d = Quaternion.Euler(0, ctx.Range(0, 360f), 0) * Vector3.right;
                mk.Prism(hay, p + Vector3.up * .8f - d * .7f, d, .8f, 1.4f, 12);
                solid.Box(hay, p + Vector3.up * .8f, new Vector3(1.4f, 1.6f, 1.6f), Quaternion.LookRotation(Vector3.Cross(Vector3.up, d)));
            }
        }

        void Cottage(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at)
        {
            var up = Vector3.up; var face = new Vector3(center.x - at.x, 0, center.z - at.z).normalized; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            const float W = 7f, D = 5f, H = 3f;
            mk.Box(stoneM, at + up * H / 2f, new Vector3(W, H, D), rot); solid.Box(stoneM, at + up * H / 2f, new Vector3(W, H, D), rot);
            mk.GableRoof(WorldMaterials.Solid(new Color(.75f, .65f, .4f)), at + up * H, new Vector3(D + .8f, 0, W + .6f), 2.2f, Quaternion.LookRotation(side));
            mk.Box(WorldMaterials.Solid(new Color(.3f, .45f, .3f)), at + up * 1f + face * (D / 2f + .02f), new Vector3(1f, 2f, .05f), rot);
            foreach (var s in new[] { -1.9f, 1.9f }) mk.Box(GlowM(new Color(1f, .88f, .6f), 1.4f), at + up * 1.6f + face * (D / 2f + .02f) + side * s, new Vector3(1f, .8f, .04f), rot);
            mk.Box(stoneM, at + up * (H + 2f) + side * (W / 2f - .6f), new Vector3(.8f, 1.6f, .8f), rot);
            for (var k = 0; k < 8; k++) soft.Blob(WorldMaterials.Solid(k % 2 == 0 ? new Color(.9f, .3f, .4f) : new Color(.95f, .85f, .3f)), ctx.OnGround(at.x + face.x * (D / 2f + 1f) + side.x * (k - 3.5f) * .8f, at.z + face.z * (D / 2f + 1f) + side.z * (k - 3.5f) * .8f) + up * .3f, Vector3.one * .25f, ctx.Rng, .3f);
            noGrass.Add(new Vector3(at.x, 6f, at.z));
        }

        /// Long grass on the hills, buttercups and poppies through it, short near the roads.
        void GroundCover(LandContext ctx)
        {
            var mat = WindMesh.Material(.3f, 1f, Grass);
            var meshes = new Dictionary<(int, int), WindMesh>();
            var rng = new System.Random(1969);
            const float step = 1.9f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += step)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += step)
            {
                var px = x + ((float)rng.NextDouble() - .5f) * step; var pz = z + ((float)rng.NextDouble() - .5f) * step;
                if (!ctx.IsLand(px, pz, 1f) || NearPath(px, pz, 3.6f) || InNoGrass(px, pz, 0f) || InFootprint(ctx, px, pz, 0f, .7f) || InPond(px, pz, 1f)) continue;
                var n = Mathf.PerlinNoise(px * .02f + 3f, pz * .02f + 8f);
                if (n < .3f) continue;
                var key = (Mathf.FloorToInt(px / 70f), Mathf.FloorToInt(pz / 70f));
                if (!meshes.TryGetValue(key, out var wm)) meshes[key] = wm = new WindMesh();
                var col = Color.Lerp(new Color(.38f, .58f, .28f), new Color(.65f, .68f, .34f), (float)rng.NextDouble());
                var at = new Vector3(px, ctx.Ground(px, pz) - .04f, pz);
                wm.Tuft(at, .35f + (float)rng.NextDouble() * .45f, col, Color.Lerp(col, new Color(.95f, .9f, .6f), .3f), rng, 4, .07f);
                var f = rng.NextDouble();
                if (f < .07) wm.Flower(at + new Vector3(.2f, 0, .1f), .35f, new Color(1f, .85f, .2f), rng);
                else if (f < .1) wm.Flower(at + new Vector3(-.15f, 0, .2f), .4f, new Color(.9f, .2f, .15f), rng);
            }
            foreach (var kv in meshes) kv.Value.Build("Kingdom grass", ctx.Root, mat);
        }
    }
}
