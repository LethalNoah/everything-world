using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The ruined cathedral at the heart of the Shoegaze Fens: a long nave of tall pointed windows filled with
    /// glowing stained glass (pink, violet, blue), its roof gone except for a few stone ribs against the sky, a
    /// great rose window over the open west door, one tall broken tower with its belfry lit, and a rounded east
    /// end. Inside: rows of columns (two fallen), a long still pool down the middle with candles along it, and
    /// a glowing orb over the altar. The glass breathes with the music (FensLife).
    public partial class FensKit
    {
        void Cathedral(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var c = ctx.OnGround(heart.x, heart.y);
            var f = Quaternion.Euler(0, 24f, 0) * Vector3.forward; var s = Vector3.Cross(Vector3.up, f); var up = Vector3.up;
            var rot = Quaternion.LookRotation(f);
            const float L = 40f, W = 14f, H = 12f, T = 1.2f, Sill = 2.6f, WinH = 6f;
            var west = c - f * (L / 2f); var east = c + f * (L / 2f);
            var glassCols = new[] { HotPink, Violet, new Color(.45f, .6f, 1f) };
            var glass = new Material[3];
            for (var i = 0; i < 3; i++) glass[i] = new Material(GlowM(glassCols[i], 2.2f)) { name = "Cathedral glass" };
            life.glass = glass;
            var wallM = stoneM; var trim = WorldMaterials.Solid(new Color(.7f, .66f, .72f));
            void Block(Vector3 center, Vector3 size, Quaternion r, bool collide = true)
            {
                mk.Box(wallM, center, size, r);
                if (collide) solid.Box(wallM, center, size, r);
            }

            // the two long walls: a low base, piers between tall pointed windows, ragged tops
            const int bays = 6;
            var bay = L / bays;
            foreach (var side in new[] { -1f, 1f })
            {
                var line = c + s * side * (W / 2f);
                var wallRot = rot;
                Block(line + up * (Sill / 2f), new Vector3(T, Sill, L), wallRot);
                for (var i = 0; i <= bays; i++)
                {
                    var along = -L / 2f + i * bay;
                    var broken = (i == 2 && side > 0) || (i == 5 && side < 0);
                    var ph = broken ? ctx.Range(3f, 5f) : H - ctx.Range(0f, 2.5f);
                    Block(line + f * along + up * (ph / 2f), new Vector3(T + .3f, ph, 2.2f), wallRot);
                    if (i == bays) continue;
                    var mid = line + f * (along + bay / 2f);
                    var gap = bay - 2.2f;
                    var collapsed = (i == 3 && side > 0);
                    if (collapsed)
                    {
                        // a fallen bay: the base wall broken through, rubble spilling out
                        for (var k = 0; k < 8; k++) mk.Blob(wallM, mid + s * side * ctx.Range(-1f, 3f) + f * ctx.Range(-2f, 2f) + up * .3f, new Vector3(ctx.Range(.5f, 1f), ctx.Range(.3f, .6f), ctx.Range(.5f, .9f)), ctx.Rng, .3f);
                        continue;
                    }
                    // the window: stained glass in a pointed arch
                    var g = glass[(i + (side > 0 ? 1 : 0)) % 3];
                    var w0 = mid + up * Sill; var halfW = gap / 2f;
                    mk.Box(g, w0 + up * (WinH / 2f), new Vector3(.12f, WinH, gap), wallRot);
                    var apexY = Sill + WinH + 2.2f;
                    var l = w0 + up * WinH - f * halfW; var r = w0 + up * WinH + f * halfW; var apex = mid + up * apexY;
                    mk.Tri(g, l, r, apex, apex + s * side);
                    mk.Tri(g, l, r, apex, apex - s * side);
                    // the leading: a cross of stone tracery
                    mk.Box(trim, w0 + up * (WinH / 2f + .5f), new Vector3(.2f, WinH + 1f, .18f), wallRot);
                    mk.Box(trim, w0 + up * (WinH * .55f), new Vector3(.2f, .18f, gap), wallRot);
                    // the wall over the arch, if this bay still stands that high
                    var wallTop = H - ctx.Range(0f, 2f);
                    if (wallTop > apexY + .3f) Block(mid + up * ((apexY + wallTop) / 2f), new Vector3(T, wallTop - apexY, gap), wallRot);
                    foreach (var (a, b) in new[] { (l, apex), (r, apex) })
                    {
                        var d = b - a; mk.Box(trim, (a + b) / 2f, new Vector3(T + .1f, .3f, d.magnitude + .2f), Quaternion.LookRotation(d, s * side));
                    }
                    halos.Add(w0 + up * (WinH / 2f) + s * side * 2.6f, 2f, glassCols[(i + (side > 0 ? 1 : 0)) % 3], .8f);
                    halos.Add(w0 + up * (WinH / 2f) - s * side * 2.6f, 2f, glassCols[(i + (side > 0 ? 1 : 0)) % 3], .8f);
                }
            }

            // the west front: an open pointed doorway, the rose window over it, and the tall broken tower
            const float DoorW = 4.4f, DoorH = 6.5f;
            var pierW = (W - DoorW) / 2f;
            foreach (var side in new[] { -1f, 1f })
                Block(west + s * side * (DoorW / 2f + pierW / 2f) + up * (H + 3f) / 2f, new Vector3(pierW, H + 3f, T), rot);
            Block(west + up * (DoorH + 2f + (H + 3f - DoorH - 2f) / 2f) - f * .02f, new Vector3(DoorW, H + 3f - DoorH - 2f, T * .9f), rot, false);
            solid.Box(wallM, west + up * (DoorH + 2f + (H + 3f - DoorH - 2f) / 2f), new Vector3(DoorW, H + 3f - DoorH - 2f, T), rot);
            var dl = west + up * DoorH - s * (DoorW / 2f); var dr = west + up * DoorH + s * (DoorW / 2f); var da = west + up * (DoorH + 2f);
            mk.Tri(wallM, dl, dr, da, da - f); mk.Tri(wallM, dl, dr, da, da + f);
            var rose = west + up * (H - 1.5f) - f * (T / 2f + .05f);
            for (var k = 0; k < 16; k++)
            {
                float a0 = k * Mathf.PI / 8f, a1 = (k + 1) * Mathf.PI / 8f;
                var p0 = rose + (s * Mathf.Cos(a0) + up * Mathf.Sin(a0)) * 3f; var p1 = rose + (s * Mathf.Cos(a1) + up * Mathf.Sin(a1)) * 3f;
                var gm = glass[k % 3];
                mk.Tri(gm, rose, p0, p1, rose + f); mk.Tri(gm, rose, p0, p1, rose - f);
                mk.Box(trim, (rose + p0) / 2f - f * .04f, new Vector3(.14f, .14f, 3f), Quaternion.LookRotation(p0 - rose, f));
                mk.Box(trim, (p0 + p1) / 2f - f * .04f, new Vector3(.3f, .3f, Vector3.Distance(p0, p1) + .1f), Quaternion.LookRotation(p1 - p0, f));
            }
            mk.Prism(GlowM(Gold, 3f), rose - f * .06f, -f, .5f, .05f, 12);
            halos.Add(rose - f * 4f, 3.4f, HotPink, .9f);
            halos.Add(rose + f * 4f, 3f, Violet, .7f);

            // the tower at the north-west corner, broken off high up, its belfry glowing
            var tower = west - f * 2.5f + s * (W / 2f + 2.5f);
            const float TowerH = 27f;
            Block(tower + up * (TowerH / 2f), new Vector3(6.5f, TowerH, 6.5f), rot);
            for (var k = 0; k < 4; k++)
            {
                var n = Quaternion.Euler(0, k * 90f, 0) * f;
                var bel = tower + n * 3.3f + up * (TowerH - 6f);
                mk.Box(GlowM(Lilac, 2.6f), bel, new Vector3(n.x != 0 || n.z != 0 ? 1.6f : 1.6f, 3.4f, .1f), Quaternion.LookRotation(n));
                halos.Add(bel + n * 2.4f, 1.8f, Lilac, .9f);
                for (var j = 0; j < 3; j++) mk.Box(wallM, tower + n * 2.6f + Vector3.Cross(Vector3.up, n) * ctx.Range(-2.5f, 2.5f) + up * (TowerH + ctx.Range(0f, 2.5f) / 2f), new Vector3(ctx.Range(.8f, 1.6f), ctx.Range(1f, 3f), 1.2f), Quaternion.LookRotation(n));
            }
            var stump = west - f * 2f - s * (W / 2f + 2f);
            Block(stump + up * 4f, new Vector3(5f, 8f, 5f), rot);
            life.belfry = tower + up * (TowerH - 6f);

            // the rounded east end: seven piers in a half circle with tall windows between
            for (var k = 0; k <= 6; k++)
            {
                var a = Mathf.PI * k / 6f;
                var dir = (s * Mathf.Cos(a) + f * Mathf.Sin(a));
                var p = east + dir * (W / 2f);
                var ph = H - ctx.Range(0f, 3f);
                Block(p + up * (ph / 2f), new Vector3(1.6f, ph, 1.6f), Quaternion.LookRotation(dir));
                if (k == 6) continue;
                var a2 = Mathf.PI * (k + .5f) / 6f; var d2 = (s * Mathf.Cos(a2) + f * Mathf.Sin(a2));
                var wp = east + d2 * (W / 2f) + up * (Sill + WinH / 2f);
                Block(east + d2 * (W / 2f) + up * (Sill / 2f), new Vector3(3.5f, Sill, T), Quaternion.LookRotation(d2));
                mk.Box(glass[k % 3], wp, new Vector3(2.2f, WinH, .12f), Quaternion.LookRotation(d2));
                halos.Add(wp + d2 * 2.6f, 2f, glassCols[k % 3], .8f);
            }

            // roof ribs still spanning the nave: pointed arches against the sky
            for (var i = 1; i < bays; i += 2)
            {
                var at = c + f * (-L / 2f + i * bay);
                Vector3 P(float t) { var x = Mathf.Lerp(-1f, 1f, t); return at + s * x * (W / 2f) + up * (H + (1f - Mathf.Abs(x)) * 5f - (1f - Mathf.Abs(x)) * (1f - Mathf.Abs(x)) * 1.2f); }
                for (var k = 0; k < 8; k++) { var a = P(k / 8f); var b = P((k + 1) / 8f); mk.Box(trim, (a + b) / 2f, new Vector3(.6f, .7f, Vector3.Distance(a, b) + .15f), Quaternion.LookRotation(b - a)); }
            }

            // inside: the floor, two rows of columns (two fallen), the pool and its candles, the altar
            mk.Box(WorldMaterials.Solid(new Color(.5f, .47f, .52f)), c + up * .03f, new Vector3(W - T, .06f, L), rot);
            for (var i = 0; i < 6; i++)
                foreach (var side in new[] { -1f, 1f })
                {
                    var p = c + f * (-L / 2f + (i + .75f) * (L / 6.5f)) + s * side * 3.8f;
                    var fallen = (i == 2 && side < 0) || (i == 4 && side > 0);
                    if (fallen)
                    {
                        var dir = (s * -side + f * .3f).normalized;
                        mk.Prism(trim, p + up * .7f - dir * .5f, dir, .65f, 6.5f, 10);
                        solid.Box(trim, p + up * .7f + dir * 2.7f, new Vector3(1.3f, 1.3f, 6.5f), Quaternion.LookRotation(dir));
                        mk.Prism(trim, p, up, .75f, 1.2f, 10);
                        continue;
                    }
                    var ch = ctx.Range(6f, 10f);
                    mk.Prism(trim, p, up, .65f, ch, 10);
                    mk.Box(trim, p + up * ch, new Vector3(1.6f, .4f, 1.6f), rot);
                    ArtistBits.PostCollider(ctx.Root, p, ch, .7f);
                }
            var pool = c + f * 1f + up * .07f;
            mk.Box(water, pool, new Vector3(3f, .02f, 24f), rot);
            foreach (var side in new[] { -1f, 1f }) mk.Box(trim, pool + s * side * 1.65f, new Vector3(.3f, .12f, 24.3f), rot);
            var wax = WorldMaterials.Solid(new Color(.95f, .92f, .88f)); var flame = GlowM(new Color(1f, .7f, .4f), 3.2f);
            for (var k = 0; k < 22; k++)
            {
                var side = k % 2 == 0 ? 1f : -1f;
                var p = pool + s * side * 1.65f + f * (-11.5f + (k / 2) * 2.2f) + up * .06f;
                var h = ctx.Range(.15f, .45f);
                mk.Prism(wax, p, up, .05f, h, 6);
                mk.Blob(flame, p + up * (h + .06f), new Vector3(.03f, .06f, .03f), ctx.Rng, 0f);
                if (k % 3 == 0) halos.Add(p + up * (h + .07f), .7f, new Color(1f, .7f, .4f), .9f);
            }
            var altar = east - f * 4f;
            Block(altar + up * .6f, new Vector3(3.6f, 1.2f, 1.6f), rot);
            var orbAt = altar + up * 4.5f;
            var orb = new GameObject("Altar orb"); orb.transform.SetParent(ctx.Root, false);
            var om = new MeshKit(); om.Blob(GlowM(HotPink, 3.5f), Vector3.zero, Vector3.one * .9f, ctx.Rng, .05f); om.Build("Orb", orb.transform, false);
            orb.transform.position = orbAt; life.altarOrb = orb.transform;
            halos.Add(orbAt, 5f, HotPink, 1f);
            ArtistBits.PointLight(ctx.Root, orbAt, HotPink, 26f, 2.2f);
            ArtistBits.PointLight(ctx.Root, c - f * 8f + up * 6f, Violet, 24f, 1.6f);
            ArtistBits.PointLight(ctx.Root, c + up * 5f, new Color(1f, .7f, .45f), 20f, 1.2f);
            ArtistBits.PointLight(ctx.Root, west - f * 5f + up * 4f, Lilac, 18f, 1.4f);
            noGrass.Add(new Vector3(c.x, 26f, c.z));
            life.cathedral = c;
        }
    }
}
