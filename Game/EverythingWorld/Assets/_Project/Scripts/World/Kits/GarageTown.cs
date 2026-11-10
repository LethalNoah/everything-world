using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// Garage Rock Row's town: between the garages a pub, a diner, a gas station, a laundromat, a record shop and
    /// a motel (room 505 lit, next to Arctic Monkeys), each with a neon sign; back alleys with dumpsters, fire
    /// escapes, burn barrels and steam; muscle cars parked at the kerb; the neon casino closing off the far end.
    /// Round the town, the hills: a water tower, lit billboards, a junkyard of old cars, a radio mast with a red
    /// light, power lines, dark trees and chain-link fences.
    public partial class GarageKit
    {
        void Town(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var side = Vector3.Cross(Vector3.up, streetDir);
            string[] kinds = { "PUB", "DINER", "GAS", "LAUNDROMAT", "RECORDS", "BAR", "PAWN", "MOTEL" };
            var made = 0;
            IslandWorldData.Artist am = null;
            foreach (var a in ctx.Artists) if (a.name == "Arctic Monkeys") am = a;
            for (var t = 22f; t < streetLen - 40f; t += 21f)
                foreach (var s in new[] { -1, 1 })
                {
                    var lot = OnStreet(t, s * Setback);
                    var taken = false;
                    foreach (var a in ctx.Artists) if (Vector2.Distance(new Vector2(a.x, a.z), new Vector2(lot.x, lot.z)) < 17f) taken = true;
                    if (taken || !ctx.IsLand(lot.x, lot.z, 6f)) continue;
                    var kind = kinds[made % kinds.Length];
                    if (am != null && kind != "MOTEL" && made > 0 && Vector2.Distance(new Vector2(am.x, am.z), new Vector2(lot.x, lot.z)) < 40f && !motelMade) kind = "MOTEL";
                    if (kind == "MOTEL" && motelMade) kind = "BAR";
                    Building(ctx, mk, solid, lot, -side * s, kind, made);
                    made++;
                    // a back alley behind every other one
                    if (made % 2 == 0) Alley(ctx, mk, solid, lot + side * s * 9f, streetDir);
                }
            // muscle cars parked at the kerb
            for (var t = 30f; t < streetLen - 20f; t += ctx.Range(18f, 34f))
            {
                var s = ctx.Chance(.5f) ? 1f : -1f;
                Car(ctx, mk, solid, OnStreet(t, s * (StreetHalf - 1.4f)), streetDir * (ctx.Chance(.7f) ? 1f : -1f), Paint[ctx.Rng.Next(Paint.Length)], false);
            }
        }

        bool motelMade;

        /// One building on the Row: walls, a flat roof with a parapet, lit windows, a door, an awning and a neon
        /// sign saying what it is.
        void Building(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at, Vector3 face, string kind, int i)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(Vector3.up, face);
            var W = kind == "GAS" ? 12f : kind == "MOTEL" ? 16f : 10f; const float D = 9f;
            var H = kind == "MOTEL" ? 7f : ctx.Range(5f, 8.5f);
            var wall = kind == "PUB" || kind == "BAR" ? brick : WorldMaterials.Solid(Paint[i % Paint.Length]);
            var c = at + face * 1f; c.y = StreetY(Vector3.Dot(at - streetA, streetDir));
            if (kind == "GAS")
            {
                // a canopy on posts over two pumps, a little kiosk behind
                foreach (var (x, z) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) }) { var p = c + side * x * 5f + face * z * 3f; mk.Box(metal, p + up * 2.6f, new Vector3(.3f, 5.2f, .3f), rot); ArtistBits.PostCollider(ctx.Root, p, 5f, .2f); }
                mk.Box(WorldMaterials.Solid(new Color(.85f, .2f, .2f)), c + up * 5.4f, new Vector3(W, .6f, 7f), rot);
                mk.Box(GlowM(new Color(1f, .95f, .85f), 2.2f), c + up * 5.08f, new Vector3(W - 1f, .05f, 6f), rot);
                ArtistBits.PointLight(ctx.Root, c + up * 4.5f, new Color(1f, .95f, .85f), 16f, 1.6f);
                foreach (var s in new[] { -2f, 2f }) { mk.Box(WorldMaterials.Solid(new Color(.9f, .88f, .82f)), c + side * s + up * .8f, new Vector3(.7f, 1.6f, .5f), rot); solid.Box(metal, c + side * s + up * .8f, new Vector3(.7f, 1.6f, .5f), rot); }
                var kiosk = c - face * 6f;
                mk.Box(wall, kiosk + up * 1.6f, new Vector3(6f, 3.2f, 4f), rot); solid.Box(wall, kiosk + up * 1.6f, new Vector3(6f, 3.2f, 4f), rot);
                mk.Box(glass, kiosk + up * 1.4f + face * 2.02f, new Vector3(4f, 1.6f, .05f), rot);
                Sign(ctx, mk, c + up * 6.4f + face * 3.6f, face, "GAS", NeonC[5], 1.1f);
                noGrass.Add(new Vector3(c.x, 10f, c.z));
                return;
            }
            mk.Box(wall, c + up * H / 2f, new Vector3(W, H, D), rot);
            solid.Box(wall, c + up * H / 2f, new Vector3(W, H, D), rot);
            mk.Box(dark, c + up * (H + .3f), new Vector3(W + .2f, .6f, D + .2f), rot);
            var front = c + face * (D / 2f + .02f);
            // windows (most lit), a door, an awning
            var lit = new[] { GlowM(new Color(1f, .9f, .6f), 2.2f), GlowM(new Color(.7f, .92f, 1f), 2.2f), GlowM(new Color(1f, .95f, .85f), 2f) };
            var floors = Mathf.FloorToInt((H - 1f) / 3f);
            for (var f = 0; f < floors; f++)
                for (var w = -1; w <= 1; w++)
                {
                    if (f == 0 && w == 0) continue;
                    mk.Box(lit[(i + f + w + 3) % 3], front + up * (1.6f + f * 3f) + side * w * (W / 3.2f), new Vector3(W / 4.5f, 1.4f, .05f), rot);
                }
            mk.Box(dark, front + up * 1.15f, new Vector3(1.4f, 2.3f, .06f), rot);
            mk.Box(WorldMaterials.Solid(NeonC[i % NeonC.Length] * .6f), front + up * 2.9f + face * .8f, new Vector3(W * .7f, .1f, 1.6f), rot * Quaternion.Euler(-14f, 0, 0));
            if (kind == "MOTEL")
            {
                motelMade = true;
                // a row of numbered doors; 505 lit red (for the song)
                for (var k = 0; k < 4; k++)
                {
                    var d = front + side * ((k - 1.5f) * 3.6f) + up * 1.15f;
                    var is505 = k == 2;
                    mk.Box(is505 ? GlowM(new Color(1f, .2f, .25f), 2f) : WorldMaterials.Solid(new Color(.35f, .3f, .3f)), d, new Vector3(1f, 2.2f, .07f), rot);
                    ArtistBits.Label(ctx.Root, (502 + k).ToString(), d + up * 1.35f + face * .06f, face, .22f, is505 ? new Color(1f, .5f, .5f) : Ink, TextAnchor.MiddleCenter);
                    if (is505) { halos.Add(d + face * 1.2f, 1.6f, new Color(1f, .25f, .3f), 1f); life.room505 = d + face * .5f; }
                }
                Sign(ctx, mk, front + up * (H + 2.4f), face, "MOTEL", NeonC[0], 1.4f);
                life.neonRedSigns.Add(front + up * (H + 2.4f));
            }
            else Sign(ctx, mk, front + up * (H - 1f) + face * .25f, face, kind, NeonC[(i + 1) % NeonC.Length], 1f);
            if (kind == "PUB")
            {
                // a hanging pub sign
                var arm = front + up * 3.6f + side * (W / 2f - .6f);
                mk.Box(metal, arm + face * .7f, new Vector3(.06f, .06f, 1.4f), rot);
                mk.Box(WorldMaterials.Solid(new Color(.2f, .25f, .2f)), arm + face * 1.2f - up * .6f, new Vector3(.08f, 1f, .9f), rot);
            }
            ArtistBits.PointLight(ctx.Root, front + face * 2f + up * 2.5f, new Color(1f, .9f, .65f), 12f, 1.6f);
            noGrass.Add(new Vector3(c.x, 8f, c.z));
        }

        /// A neon sign: the word lit on a dark backing board, edged with glowing tube.
        void Sign(LandContext ctx, MeshKit mk, Vector3 at, Vector3 face, string text, Color col, float size)
        {
            var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(Vector3.up, face);
            var w = text.Length * .75f * size + 1f; var h = 1.3f * size;
            mk.Box(dark, at, new Vector3(w, h, .2f), rot);
            var tube = GlowM(col, 2.6f);
            foreach (var y in new[] { -h / 2f, h / 2f }) mk.Box(tube, at + Vector3.up * y + face * .12f, new Vector3(w, .08f, .05f), rot);
            foreach (var x in new[] { -w / 2f, w / 2f }) mk.Box(tube, at + side * x + face * .12f, new Vector3(.08f, h, .05f), rot);
            ArtistBits.Label(ctx.Root, text, at + face * .13f, face, .8f * size, col, TextAnchor.MiddleCenter);
            halos.Add(at + face * 1.4f, w * .6f + .5f, col, .8f);
            life.neon.Add((at + face * .5f, col));
        }

        void Alley(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at, Vector3 along)
        {
            var up = Vector3.up; at = ctx.OnGround(at.x, at.z);
            var dump = WorldMaterials.Solid(new Color(.2f, .35f, .25f));
            mk.Box(dump, at + up * .7f, new Vector3(2f, 1.4f, 1.2f), Quaternion.LookRotation(along));
            mk.Box(WorldMaterials.Solid(new Color(.15f, .25f, .18f)), at + up * 1.45f, new Vector3(2.1f, .1f, 1.3f), Quaternion.LookRotation(along) * Quaternion.Euler(-8f, 0, 0));
            solid.Box(dump, at + up * .7f, new Vector3(2f, 1.4f, 1.2f), Quaternion.LookRotation(along));
            var barrel = at + along * 3f;
            mk.Prism(WorldMaterials.Solid(new Color(.35f, .25f, .2f)), barrel, up, .4f, .95f, 10);
            mk.Blob(GlowM(new Color(1f, .5f, .15f), 2.4f), barrel + up * 1f, new Vector3(.3f, .2f, .3f), ctx.Rng, .3f);
            halos.Add(barrel + up * 1.3f, 1.4f, new Color(1f, .5f, .15f), 1f);
            ArtistBits.PostCollider(ctx.Root, barrel, 1f, .45f);
            life.barrels.Add(barrel + up * 1f);
            for (var k = 0; k < 4; k++) mk.Box(WorldMaterials.Solid(new Color(.3f, .25f, .2f)), ctx.OnGround(at.x + ctx.Range(-2f, 2f), at.z + ctx.Range(-2f, 2f)) + up * .2f, new Vector3(.5f, .4f, .4f), Quaternion.Euler(0, ctx.Range(0, 90f), 0));
            life.steam.Add(at - along * 3f + up * .1f);
        }

        /// A muscle car: long low body, a cabin set back, fat wheels, chrome, headlights.
        void Car(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at, Vector3 dir, Color paint, bool lightsOn)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(dir); var side = Vector3.Cross(Vector3.up, dir);
            var body = WorldMaterials.Solid(paint, .85f); var chrome = WorldMaterials.Solid(new Color(.8f, .8f, .82f), .9f);
            mk.Box(body, at + up * .65f, new Vector3(2f, .6f, 4.8f), rot);
            mk.Box(body, at + up * 1.15f - dir * .3f, new Vector3(1.8f, .5f, 2.2f), rot);
            mk.Box(WorldMaterials.Solid(new Color(.1f, .12f, .15f), .9f), at + up * 1.17f - dir * .3f, new Vector3(1.82f, .38f, 2.1f), rot);
            mk.Box(WorldMaterials.Solid(new Color(.95f, .95f, .95f)), at + up * .96f + dir * .7f, new Vector3(.4f, .02f, 2.2f), rot);
            foreach (var z in new[] { -2.42f, 2.42f }) mk.Box(chrome, at + up * .45f + dir * z, new Vector3(2.05f, .15f, .1f), rot);
            foreach (var s in new[] { -1f, 1f }) foreach (var z in new[] { -1.5f, 1.5f })
                mk.Prism(dark, at + side * s * 1f + dir * z + up * .4f - side * s * .14f, side * s, .42f, .3f, 12);
            foreach (var s in new[] { -.65f, .65f }) mk.Box(lightsOn ? GlowM(new Color(1f, .95f, .8f), 2.4f) : WorldMaterials.Solid(new Color(.85f, .85f, .8f)), at + up * .7f + dir * 2.41f + side * s, new Vector3(.3f, .2f, .04f), rot);
            foreach (var s in new[] { -.7f, .7f }) mk.Box(GlowM(new Color(1f, .15f, .15f), 1.4f), at + up * .7f - dir * 2.41f + side * s, new Vector3(.35f, .12f, .04f), rot);
            solid.Box(body, at + up * .8f, new Vector3(2f, 1.4f, 4.8f), rot);
        }

        // ---------------------------------------------------------------- the casino
        void Casino(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var face = -streetDir; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(Vector3.up, face);
            var c = OnStreet(streetLen, 0) + streetDir * 22f; c.y = StreetY(streetLen);
            const float W = 40f, D = 18f, H = 13f;
            var wall = WorldMaterials.Solid(new Color(.3f, .12f, .2f));
            mk.Box(wall, c + up * H / 2f, new Vector3(W, H, D), rot);
            solid.Box(wall, c + up * H / 2f, new Vector3(W, H, D), rot);
            var front = c + face * (D / 2f + .02f);
            // the marquee canopy, edged with chasing bulbs (three sets, lit in turn: GarageLife)
            mk.Box(dark, front + up * 5.5f + face * 3f, new Vector3(24f, .8f, 6f), rot);
            var bulbs = new Material[3];
            for (var i = 0; i < 3; i++) bulbs[i] = new Material(GlowM(new Color(1f, .85f, .45f), 2.4f)) { name = "Marquee bulbs" };
            life.marquee = bulbs;
            var b = 0;
            for (var x = -12f; x <= 12f; x += .6f, b++)
                foreach (var y in new[] { 5.1f, 5.9f })
                    mk.Blob(bulbs[b % 3], front + up * y + face * 6.05f + side * x, Vector3.one * .1f, ctx.Rng, 0f);
            for (var x = -11f; x <= 11f; x += .8f, b++) mk.Blob(bulbs[b % 3], front + up * 1f + side * x + face * .1f, Vector3.one * .1f, ctx.Rng, 0f);
            // glowing gold doors and the name
            mk.Box(GlowM(new Color(1f, .78f, .35f), 2f), front + up * 2f, new Vector3(10f, 4f, .06f), rot);
            halos.Add(front + up * 2f + face * 3f, 5f, new Color(1f, .78f, .35f), .8f);
            Sign(ctx, mk, front + up * (H - 2.5f) + face * .3f, face, "CASINO", NeonC[0], 3f);
            life.neonRedSigns.Add(front + up * (H - 2.5f));
            // the sign tower with a star on top
            var tower = c + side * (W / 2f + 3f) + face * (D / 2f - 2f);
            mk.Box(dark, tower + up * 13f, new Vector3(2.4f, 26f, 2.4f), rot);
            solid.Box(dark, tower + up * 13f, new Vector3(2.4f, 26f, 2.4f), rot);
            for (var y = 2f; y < 24f; y += 1.5f) mk.Box(GlowM(NeonC[Mathf.RoundToInt(y) % NeonC.Length], 2.4f), tower + up * y + face * 1.25f, new Vector3(1.8f, .4f, .05f), rot);
            var star = tower + up * 28f;
            for (var k = 0; k < 5; k++) { var d = Quaternion.AngleAxis(k * 72f, face) * up; mk.Prism(GlowM(new Color(1f, .9f, .3f), 2.6f), star, d, .6f, 2.6f, 5, .05f); }
            halos.Add(star + face * 1f, 5f, new Color(1f, .9f, .3f), 1f);
            ArtistBits.PointLight(ctx.Root, front + face * 8f + up * 6f, new Color(1f, .5f, .6f), 34f, 2f);
            ArtistBits.PointLight(ctx.Root, star, new Color(1f, .9f, .4f), 24f, 1.4f);
            life.casino = front + face * 2f;
            noGrass.Add(new Vector3(c.x, 30f, c.z));
            // cars parked in front, two with their lights on
            for (var k = 0; k < 5; k++) Car(ctx, mk, solid, OnStreet(streetLen - 4f, (k - 2f) * 3.2f) + streetDir * 8f, side * (k % 2 == 0 ? 1f : -1f), Paint[(k + 2) % Paint.Length], k % 3 == 0);
        }

        // ---------------------------------------------------------------- the outskirts
        void Outskirts(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var tree = WorldMaterials.Solid(new Color(.16f, .22f, .17f)); var trunk = WorldMaterials.Solid(new Color(.22f, .18f, .16f));
            var tower = false; var mast = false; var junk = false; var boards = 0;
            const float cell = 38f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + ctx.Range(0f, cell), pz = z + ctx.Range(0f, cell);
                if (!ctx.IsLand(px, pz, 6f) || OnTown(px, pz, 8f) || InNoGrass(px, pz, 4f)) continue;
                var near = false; foreach (var a in ctx.Artists) if (Vector2.Distance(new Vector2(a.x, a.z), new Vector2(px, pz)) < a.r + 6f) near = true;
                if (near) continue;
                var p = ctx.OnGround(px, pz);
                var roll = ctx.Range(0f, 1f);
                if (!tower && p.y > 25f && roll < .3f) { WaterTower(ctx, mk, p); tower = true; }
                else if (!mast && roll < .05f) { Mast(ctx, mk, p); mast = true; }
                else if (!junk && roll < .1f) { Junkyard(ctx, mk, solid, p); junk = true; }
                else if (boards < 6 && roll < .18f) { Billboard(ctx, mk, p, boards++); }
                else if (roll < .65f)
                {
                    for (var k = 0; k < ctx.Rng.Next(1, 4); k++)
                    {
                        var q = ctx.OnGround(px + ctx.Range(-5f, 5f), pz + ctx.Range(-5f, 5f)); var h = ctx.Range(4f, 8f);
                        mk.Prism(trunk, q - up * .3f, up, .25f, h * .5f, 6);
                        mk.Blob(tree, q + up * h * .65f, new Vector3(2f, h * .45f, 2f), ctx.Rng, .25f);
                        ArtistBits.PostCollider(ctx.Root, q, h * .5f, .3f);
                    }
                }
                else if (roll < .78f)
                {
                    // a run of chain-link fence on posts
                    var dir = Quaternion.Euler(0, ctx.Range(0, 180f), 0) * Vector3.forward;
                    for (var k = 0; k < 5; k++)
                    {
                        var a0 = ctx.OnGround(px + dir.x * k * 3f, pz + dir.z * k * 3f); var a1 = ctx.OnGround(px + dir.x * (k + 1) * 3f, pz + dir.z * (k + 1) * 3f);
                        mk.Prism(metal, a0, up, .05f, 2f, 5);
                        mk.Quad(WorldMaterials.Solid(new Color(.5f, .52f, .55f), .3f), a0 + up * .2f, a1 + up * .2f, a1 + up * 1.9f, a0 + up * 1.9f, a0 + Vector3.Cross(up, dir));
                        mk.Quad(WorldMaterials.Solid(new Color(.5f, .52f, .55f), .3f), a0 + up * .2f, a1 + up * .2f, a1 + up * 1.9f, a0 + up * 1.9f, a0 - Vector3.Cross(up, dir));
                        solid.Box(metal, (a0 + a1) / 2f + up, new Vector3(.1f, 2f, 3f), Quaternion.LookRotation(a1 - a0));
                    }
                }
                else continue;
                noGrass.Add(new Vector3(px, 4f, pz));
            }
            // power lines along both sides of the town, out to where you arrive
            var side = Vector3.Cross(Vector3.up, streetDir);
            if (hasStreet)
                foreach (var s in new[] { -1f, 1f })
                {
                    Vector3? prev = null;
                    for (var t = -10f; t < streetLen + 10f; t += 32f)
                    {
                        var p = OnStreet(Mathf.Clamp(t, 0, streetLen), s * 34f);
                        if (!ctx.IsLand(p.x, p.z, 2f)) { prev = null; continue; }
                        p.y = ctx.Ground(p.x, p.z);
                        mk.Prism(wood, p - up * .3f, up, .14f, 9.3f, 6);
                        mk.Box(wood, p + up * 8.6f, new Vector3(2.4f, .15f, .15f), Quaternion.LookRotation(side));
                        ArtistBits.PostCollider(ctx.Root, p, 9f, .16f);
                        var top = p + up * 8.6f;
                        if (prev.HasValue)
                            foreach (var o in new[] { -1f, 1f })
                            {
                                var a = prev.Value + side * o; var b = top + side * o; var last = a;
                                for (var k = 1; k <= 6; k++) { var q = Vector3.Lerp(a, b, k / 6f) - up * Mathf.Sin(k / 6f * Mathf.PI) * .9f; mk.Box(dark, (last + q) / 2f, new Vector3(.03f, .03f, Vector3.Distance(last, q)), Quaternion.LookRotation(q - last)); last = q; }
                            }
                        prev = top;
                    }
                }
        }

        void WaterTower(LandContext ctx, MeshKit mk, Vector3 at)
        {
            var up = Vector3.up; var white = WorldMaterials.Solid(new Color(.75f, .76f, .78f));
            foreach (var (x, z) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) }) { var f = at + new Vector3(x, 0, z) * 3f; mk.Prism(metal, f - up * .3f, (up - new Vector3(x, 0, z) * .08f).normalized, .2f, 14.5f, 6); ArtistBits.PostCollider(ctx.Root, f, 12f, .25f); }
            mk.Prism(white, at + up * 14f, up, 4.2f, 5f, 14);
            mk.Cone(white, at + up * 19f, 4.4f, 2f, 14);
            var face = new Vector3(center.x - at.x, 0, center.z - at.z).normalized;
            ArtistBits.Label(ctx.Root, "THE ROW", at + up * 16.5f + face * 4.25f, face, 1.1f, new Color(.2f, .2f, .25f), TextAnchor.MiddleCenter);
            mk.Blob(GlowM(new Color(1f, .2f, .2f), 2.6f), at + up * 21.2f, Vector3.one * .25f, ctx.Rng, 0f);
            halos.Add(at + up * 21.2f, 1.6f, new Color(1f, .2f, .2f), 1f);
            noGrass.Add(new Vector3(at.x, 6f, at.z));
        }

        void Mast(LandContext ctx, MeshKit mk, Vector3 at)
        {
            var up = Vector3.up;
            for (var y = 0f; y < 34f; y += 4f) { mk.Box(metal, at + up * (y + 2f), new Vector3(.12f, 4f, .12f), Quaternion.identity); mk.Box(metal, at + up * y, new Vector3(1.2f - y * .03f, .08f, 1.2f - y * .03f), Quaternion.identity); }
            var red = new Material(GlowM(new Color(1f, .1f, .1f), 2.6f)) { name = "Mast light" };
            mk.Blob(red, at + up * 34.5f, Vector3.one * .4f, ctx.Rng, 0f);
            life.mastLight = red;
            halos.Add(at + up * 34.5f, 3f, new Color(1f, .15f, .15f), 1f);
            ArtistBits.PostCollider(ctx.Root, at, 6f, .8f);
        }

        void Junkyard(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at)
        {
            for (var k = 0; k < 9; k++)
            {
                var p = ctx.OnGround(at.x + ctx.Range(-9f, 9f), at.z + ctx.Range(-9f, 9f)) + Vector3.up * ((k % 3 == 2) ? 1.3f : 0f);
                var dir = Quaternion.Euler(ctx.Range(-8f, 8f), ctx.Range(0, 360f), ctx.Range(-12f, 12f)) * Vector3.forward;
                Car(ctx, mk, solid, p, dir, Color.Lerp(Paint[k % Paint.Length], new Color(.45f, .3f, .2f), .5f), false);
            }
            noGrass.Add(new Vector3(at.x, 12f, at.z));
        }

        void Billboard(LandContext ctx, MeshKit mk, Vector3 at, int i)
        {
            var up = Vector3.up; var face = new Vector3(center.x - at.x, 0, center.z - at.z).normalized; var side = Vector3.Cross(Vector3.up, face); var rot = Quaternion.LookRotation(face);
            foreach (var s in new[] { -3f, 3f }) { mk.Prism(metal, at + side * s - up * .3f, up, .15f, 7.3f, 6); ArtistBits.PostCollider(ctx.Root, at + side * s, 7f, .2f); }
            var col = NeonC[i % NeonC.Length];
            mk.Box(WorldMaterials.Solid(Color.Lerp(col, new Color(.9f, .88f, .8f), .5f)), at + up * 8.5f, new Vector3(9f, 4f, .2f), rot);
            mk.Box(WorldMaterials.Solid(col * .8f), at + up * 8.5f + face * .11f, new Vector3(5f, 2.4f, .02f), rot);
            string[] words = { "ROCK ON", "OPEN LATE", "LIVE TONIGHT", "DRIVE SAFE", "NO SLEEP", "TURN IT UP" };
            ArtistBits.Label(ctx.Root, words[i % words.Length], at + up * 8.5f + face * .13f, face, .9f, Ink, TextAnchor.MiddleCenter);
            foreach (var s in new[] { -2.5f, 2.5f }) { mk.Box(GlowM(new Color(1f, .95f, .8f), 2.2f), at + up * 10.8f + side * s + face * .6f, new Vector3(.4f, .15f, .3f), rot); }
            halos.Add(at + up * 9f + face * 2f, 4f, new Color(1f, .95f, .8f), .5f);
        }

        // ---------------------------------------------------------------- weeds and grass
        void GroundCover(LandContext ctx)
        {
            var mat = WindMesh.Material(.3f, 1f, new Color(.3f, .38f, .26f));
            var meshes = new Dictionary<(int, int), WindMesh>();
            var rng = new System.Random(505);
            const float step = 2f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += step)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += step)
            {
                var px = x + ((float)rng.NextDouble() - .5f) * step; var pz = z + ((float)rng.NextDouble() - .5f) * step;
                if (!ctx.IsLand(px, pz, 1f) || InNoGrass(px, pz, 0f)) continue;
                if (hasStreet)
                {
                    var rel = new Vector3(px - streetA.x, 0, pz - streetA.z);
                    var t = Vector3.Dot(rel, streetDir); var off = Mathf.Abs(Vector3.Dot(rel, Vector3.Cross(Vector3.up, streetDir)));
                    if (t > -25f && t < streetLen + 10f && off < StreetHalf + Walk + 1.5f) continue;
                }
                var near = false; foreach (var a in ctx.Artists) if (Vector2.Distance(new Vector2(a.x, a.z), new Vector2(px, pz)) < 8f) near = true;
                if (near) continue;
                var n = Mathf.PerlinNoise(px * .03f, pz * .03f + 4f);
                if (n < .32f) continue;
                var key = (Mathf.FloorToInt(px / 70f), Mathf.FloorToInt(pz / 70f));
                if (!meshes.TryGetValue(key, out var wm)) meshes[key] = wm = new WindMesh();
                var col = Color.Lerp(new Color(.24f, .32f, .22f), new Color(.38f, .4f, .28f), (float)rng.NextDouble());
                wm.Tuft(new Vector3(px, ctx.Ground(px, pz) - .04f, pz), .3f + (float)rng.NextDouble() * .5f, col, Color.Lerp(col, new Color(.6f, .55f, .4f), .3f), rng, 4, .07f);
            }
            foreach (var kv in meshes) kv.Value.Build("Weeds", ctx.Root, mat);
        }
    }
}
