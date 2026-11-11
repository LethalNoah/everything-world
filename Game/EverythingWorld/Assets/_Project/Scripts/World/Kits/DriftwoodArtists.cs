using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Driftwood Shore's artist areas, each a small bittersweet scene facing the boardwalk, with a plaque, its
    /// covers on a weathered board (backed, with titles) and a lantern. Jack Johnson: a grey shingled beach house on
    /// stilts with a porch, a hammock, a guitar and a fire pit. Agnes Obel: a black piano standing in a pool of still
    /// water, candles burning on it. Angus & Julia Stone: two chairs a little apart facing the sea, a silver coin
    /// shining in the sand between them. Tracy Chapman: an old car on the sand, headlights on into the mist.
    /// Israel Kamakawiwo'ole: a ukulele on a bench by a little pool. Nick Drake: a rowing boat under a pink moon
    /// lantern. And for everyone else something left on the shore that fits their song.
    public partial class DriftwoodKit
    {
        class Area
        {
            public LandContext ctx; public Transform root, labels; public MeshKit mk, solid, pictures;
            public IslandWorldData.Artist a; public Vector3 center, front, side, sea; public List<Renderer> glows; public ArtistArea area;
            public Vector3 At(float forward, float right) { var p = center + front * forward + side * right; return ctx.OnGround(p.x, p.z); }
        }

        public override void BuildArtist(LandContext ctx, IslandWorldData.Artist a)
        {
            if (drift == null) MakeMaterials();
            var root = new GameObject("Artist: " + a.name).transform; root.SetParent(ctx.Root, false);
            var front = facing.TryGetValue(a.name, out var f) ? f : new Vector3(center.x - a.x, 0, center.z - a.z).normalized;
            if (front.sqrMagnitude < .01f) front = Vector3.forward;
            var mk = new MeshKit(); var solid = new MeshKit(); var pictures = new MeshKit();
            var labels = new GameObject("Labels").transform; labels.SetParent(root, false);
            var area = root.gameObject.AddComponent<ArtistArea>();
            area.artistName = a.name; area.stationId = a.stationId; area.radius = a.r; area.glowColor = Ember;
            var glows = new List<Renderer>();
            var c = ctx.OnGround(a.x, a.z);
            var z = new Area { ctx = ctx, root = root, mk = mk, solid = solid, pictures = pictures, labels = labels, a = a, center = c, front = front, side = Vector3.Cross(Vector3.up, front), sea = SeaDir(ctx, c, out _), glows = glows, area = area };
            switch (a.name)
            {
                case "Jack Johnson": BeachHouse(z); break;
                case "Agnes Obel": Piano(z); break;
                case "Angus & Julia Stone": TwoChairs(z); break;
                case "Tracy Chapman": Car(z); break;
                case "Israel Kamakawiwo'ole": Ukulele(z); break;
                case "Nick Drake": PinkMoon(z); break;
                case "Ben Harper": TidePost(z); break;
                case "Sparky Deathcap": ClosedHut(z); break;
                case "Ryley Walker": WindChimes(z); break;
                case "unclepat": Windsock(z); break;              // kept for him, wherever his land ends up
                case "Paris Paloma": Silhouette(z); break;
                case "OCS": Figurehead(z); break;
                case "Rodrigo y Gabriela": TwoGuitars(z); break;
                case "Shearwater": Seabirds(z); break;
                case "Jonathan Wilson": Raven(z); break;
                case "Folk Bitch Trio": ThreeCrates(z); break;
                case "Amélie Farren": Lemonade(z); break;
                case "hey, nothing": Lifebuoy(z); break;
                case "Norma Tanega": Marker(z); break;
                default: GuitarStand(z); break;
            }
            if (a.name != "Jack Johnson" && HasStation(a))
            {
                // the radio-station scenes are bigger: their board, plaque and lantern stand by the end of the walk
                CoverBoard(z, z.At(4.5f, -7.5f), front);
                ArtistBits.Plaque(mk, solid, labels, a, z.At(5.5f, 6f), front, driftDark, board, Ink, 1.5f);
                glows.Add(ArtistBits.LanternPost(mk, root, z.At(6.5f, -3.4f), z.side, driftDark, iron, Ember, 2.6f));
            }
            else if (a.name != "Jack Johnson")
            {
                CoverBoard(z, z.At(-2.2f, -3f), front);
                ArtistBits.Plaque(mk, solid, labels, a, z.At(2.6f, 3f), front, driftDark, board, Ink, 1.5f);
                glows.Add(ArtistBits.LanternPost(mk, root, z.At(1.6f, -4.4f), z.side, driftDark, iron, Ember, 2.6f));
            }
            foreach (var g in glows) if (g != null) halos.Add(g.transform.position, 1.2f, Ember, .8f);
            area.glowRenderers = glows.ToArray();
            mk.Build("Area", root, false);
            solid.BuildCollider("Walls", root);
            pictures.Build("Covers", root, false);
        }

        /// A weathered board of covers with their titles, backed so they don't vanish from behind.
        void CoverBoard(Area z, Vector3 ground, Vector3 facingDir, float size = .8f)
        {
            facingDir = Vector3.ProjectOnPlane(facingDir, Vector3.up).normalized;
            var rot = Quaternion.LookRotation(facingDir);
            var side = Vector3.Cross(Vector3.up, facingDir);
            var n = Mathf.Max(1, Mathf.Min(3, z.a.songList?.Length ?? 1));
            var w = n * size * 1.25f + .3f;
            foreach (var s in new[] { -1f, 1f }) z.mk.Prism(driftDark, ground + side * s * (w / 2f - .1f) - facingDir * .05f, Vector3.up, .07f, 2.1f, 5);
            var c = ground + Vector3.up * 1.6f;
            z.mk.Box(drift, c - Vector3.up * .1f, new Vector3(w, size + .55f, .08f), rot);
            z.solid.Box(drift, c - Vector3.up * .1f, new Vector3(w, size + .55f, .16f), rot);
            ArtistBits.CoverWall(z.pictures, z.labels, z.ctx, z.a, c + facingDir * .045f + Vector3.up * .08f, facingDir, n, size, n, true, new Color(.18f, .16f, .15f));
        }

        // ---------------------------------------------------------------- Jack Johnson: the beach house
        void BeachHouse(Area z)
        {
            var ctx = z.ctx;
            const float W = 12f, D = 9f, H = 4.2f, t = .3f, stilt = 1.8f, deckD = 3.5f;
            var front = z.front; var side = z.side; var rot = Quaternion.LookRotation(front);
            var c = z.center - front * 3f; c.y = 0;
            float lo = float.MaxValue, hi = float.MinValue;
            for (var i = -2; i <= 2; i++) for (var j = -2; j <= 3; j++) { var p = c + side * i * W / 4f + front * j * D / 4f; var g = ctx.Ground(p.x, p.z); lo = Mathf.Min(lo, g); hi = Mathf.Max(hi, g); }
            var fy = hi + stilt;
            var shingle = WorldMaterials.Solid(new Color(.6f, .6f, .6f)); var trim = WorldMaterials.Solid(new Color(.88f, .86f, .82f));
            var up = Vector3.up;
            // stilts, floor and deck
            for (var i = -2; i <= 2; i++) for (var j = -1; j <= 2; j++)
            {
                var p = c + side * i * (W / 4f) + front * j * (D / 3f); var g = ctx.Ground(p.x, p.z);
                z.mk.Prism(driftDark, new Vector3(p.x, g - .3f, p.z), up, .18f, fy - g + .3f, 6);
            }
            z.mk.Box(planks, c + up * (fy - .1f) + front * deckD / 2f, new Vector3(W, .2f, D + deckD), rot);
            z.solid.Box(planks, c + up * (fy - .3f) + front * deckD / 2f, new Vector3(W, .6f, D + deckD), rot);
            void Wall(Vector3 p, Vector3 s) { z.mk.Box(shingle, p, s, rot); z.solid.Box(shingle, p, s, rot); }
            Wall(c - front * (D / 2f - t / 2f) + up * (fy + H / 2f), new Vector3(W, H, t));
            foreach (var s in new[] { -1f, 1f })
            {
                Wall(c + side * s * (W / 2f - t / 2f) + up * (fy + H / 2f), new Vector3(t, H, D - 2f * t));
                var win = c + side * s * (W / 2f + .02f) + up * (fy + 2f);
                z.glows.Add(ArtistBits.GlassBox(z.root, win, new Vector3(.06f, 1.2f, 1.6f), new Color(1f, .74f, .6f), rot));
            }
            var fz = c + front * (D / 2f - t / 2f);
            var door = HumanScale.DoorWidth; var pier = (W - door) / 2f;
            foreach (var s in new[] { -1f, 1f })
            {
                Wall(fz + side * s * (door / 2f + pier / 2f) + up * (fy + H / 2f), new Vector3(pier, H, t));
                var win = fz + side * s * (door / 2f + pier / 2f) + front * .17f + up * (fy + 2f);
                z.glows.Add(ArtistBits.GlassBox(z.root, win, new Vector3(1.8f, 1.3f, .05f), new Color(1f, .74f, .6f), rot));
                z.mk.Box(trim, win - up * .72f + front * .05f, new Vector3(2f, .1f, .15f), rot);
            }
            Wall(fz + up * (fy + HumanScale.DoorHeight + (H - HumanScale.DoorHeight) / 2f), new Vector3(door, H - HumanScale.DoorHeight, t));
            z.mk.GableRoof(WorldMaterials.Solid(new Color(.42f, .43f, .45f)), c + up * (fy + H), new Vector3(D + 1f, 0, W + 1f), 3f, Quaternion.LookRotation(side));
            for (var y = .4f; y < H; y += .4f) foreach (var s in new[] { -1f, 1f }) z.mk.Box(trim, c + side * s * (W / 2f + .005f) + up * (fy + y), new Vector3(.015f, .02f, D), rot);
            // deck rail, stairs down, a hammock, a guitar by the door, a fire pit below
            var deckEdge = c + front * (D / 2f + deckD);
            foreach (var s in new[] { -1f, 1f })
            {
                var r0 = deckEdge + side * s * (W / 2f) + up * (fy + .9f); var r1 = c + side * s * (W / 2f) + front * (D / 2f) + up * (fy + .9f);
                z.mk.Box(drift, (r0 + r1) / 2f, new Vector3(.08f, .08f, deckD), rot);
                z.solid.Box(drift, (r0 + r1) / 2f - up * .45f, new Vector3(.1f, 1f, deckD), rot);
            }
            z.mk.Box(drift, deckEdge + side * (W / 4f + 1.5f) + up * (fy + .9f), new Vector3(W / 2f - 3f, .08f, .08f), rot);
            z.mk.Box(drift, deckEdge - side * (W / 4f + 1.5f) + up * (fy + .9f), new Vector3(W / 2f - 3f, .08f, .08f), rot);
            z.solid.Box(drift, deckEdge + side * (W / 4f + 1.5f) + up * (fy + .45f), new Vector3(W / 2f - 3f, 1f, .1f), rot);
            z.solid.Box(drift, deckEdge - side * (W / 4f + 1.5f) + up * (fy + .45f), new Vector3(W / 2f - 3f, 1f, .1f), rot);
            var stairTop = deckEdge + up * fy; var stairFoot = deckEdge + front * (stilt * 2.2f); stairFoot.y = ctx.Ground(stairFoot.x, stairFoot.z);
            var sd = stairTop - stairFoot;
            z.mk.Box(planks, (stairTop + stairFoot) / 2f, new Vector3(2.6f, .2f, sd.magnitude + .3f), Quaternion.LookRotation(sd));
            z.solid.Box(planks, (stairTop + stairFoot) / 2f, new Vector3(2.6f, .2f, sd.magnitude + .3f), Quaternion.LookRotation(sd));
            var h0 = c + front * (D / 2f + .6f) - side * (W / 2f - .6f) + up * (fy + 1.6f); var h1 = h0 + front * (deckD - 1f) + side * 1.2f;
            var cloth2 = WorldMaterials.Solid(new Color(.78f, .58f, .56f));
            for (var i = 0; i < 8; i++)
            {
                float t0 = i / 8f, t1 = (i + 1) / 8f;
                Vector3 P(float tt) => Vector3.Lerp(h0, h1, tt) - up * .9f * 4f * tt * (1f - tt);
                z.mk.Box(cloth2, (P(t0) + P(t1)) / 2f, new Vector3(.9f, .05f, Vector3.Distance(P(t0), P(t1)) + .02f), Quaternion.LookRotation(P(t1) - P(t0)));
            }
            var gtr = fz + front * .5f + side * 1.8f + up * fy;
            var wood = WorldMaterials.Solid(new Color(.7f, .5f, .3f), .6f);
            z.mk.Blob(wood, gtr + up * .3f, new Vector3(.22f, .3f, .08f), ctx.Rng, 0f);
            z.mk.Box(board, gtr + up * .75f, new Vector3(.05f, .6f, .04f), rot);
            var pit = z.At(D / 2f - 3f + deckD + 7f, -5f);
            Bonfire(ctx, z.mk, z.solid, pit, z.sea);
            // inside: his covers on the back wall, two armchairs, a record player
            ArtistBits.CoverWall(z.pictures, z.labels, ctx, z.a, c - front * (D / 2f - t - .03f) + up * (fy + 2.6f), front, 3, .9f, 6, true, Ink);
            foreach (var s in new[] { -1f, 1f })
            {
                var ch = c + side * s * 2.5f + up * fy;
                z.mk.Box(WorldMaterials.Solid(new Color(.55f, .45f, .42f)), ch + up * .3f, new Vector3(1.1f, .6f, 1f), rot);
                z.mk.Box(WorldMaterials.Solid(new Color(.55f, .45f, .42f)), ch + up * .75f - front * .45f, new Vector3(1.1f, .9f, .2f), rot);
                z.solid.Box(cloth, ch + up * .5f, new Vector3(1.1f, 1f, 1f), rot);
            }
            var rp = c - front * (D / 2f - 1f) + side * 4.3f + up * fy;
            z.mk.Box(WorldMaterials.Solid(new Color(.4f, .3f, .25f)), rp + up * .4f, new Vector3(1.2f, .8f, .7f), rot);
            z.mk.Prism(board, rp + up * .82f, up, .3f, .02f, 16);
            z.solid.Box(cloth, rp + up * .4f, new Vector3(1.2f, .8f, .7f), rot);
            ArtistBits.PointLight(z.root, c + up * (fy + H - .6f), new Color(1f, .74f, .6f), 12f, 1.4f, true);
            IndoorSpaces.Add(c + up * (fy + H / 2f), rot, new Vector3(W, H, D));
            ArtistBits.Plaque(z.mk, z.solid, z.labels, z.a, ctx.OnGround(stairFoot.x + side.x * 3f + front.x * 1.5f, stairFoot.z + side.z * 3f + front.z * 1.5f), front, driftDark, board, Ink, 1.6f);
            z.area.glowLight = ArtistBits.PointLight(z.root, deckEdge + up * (fy + 2.5f), new Color(1f, .74f, .6f), 14f, 1.2f);
            JackYard(z, c, fy, W, D, deckD);
        }

        /// Round Jack Johnson's house: string lights swagged round the deck, surfboards leaning by the door, a
        /// telescope on the deck for "Constellations", a rain barrel for "Monsoon", a washing line with towels, an
        /// outdoor shower, and a path of stepping planks.
        void JackYard(Area z, Vector3 c, float fy, float W, float D, float deckD)
        {
            var ctx = z.ctx; var front = z.front; var side = z.side; var up = Vector3.up; var rot = Quaternion.LookRotation(front);
            var deckEdge = c + front * (D / 2f + deckD);
            var corners = new[] { c + side * (W / 2f) + front * (D / 2f), deckEdge + side * (W / 2f - .1f), deckEdge - side * (W / 2f - .1f), c - side * (W / 2f) + front * (D / 2f) };
            for (var i = 0; i < corners.Length; i++) corners[i].y = fy;
            foreach (var k in new[] { 1, 2 }) z.mk.Prism(driftDark, corners[k], up, .07f, 2.7f, 6);
            for (var i = 0; i + 1 < corners.Length; i++)
            {
                var a = corners[i] + up * 2.6f; var b = corners[i + 1] + up * 2.6f;
                var n = Mathf.Max(3, Mathf.RoundToInt(Vector3.Distance(a, b) / .7f));
                var prev = a;
                for (var k = 0; k <= n; k++)
                {
                    var t = k / (float)n; var p = Vector3.Lerp(a, b, t) - up * Mathf.Sin(t * Mathf.PI) * .45f;
                    if (k > 0) z.mk.Box(iron, (prev + p) / 2f, new Vector3(.015f, .015f, Vector3.Distance(prev, p) + .01f), Quaternion.LookRotation(p - prev));
                    z.mk.Blob(bulb, p - up * .08f, Vector3.one * .07f, ctx.Rng, 0f);
                    if (k % 3 == 1) halos.Add(p - up * .08f, .7f, Bulb, .8f);
                    prev = p;
                }
            }
            // surfboards leaning on the front wall, right of the window
            Color[] boardCols = { new Color(.82f, .58f, .56f), new Color(.5f, .78f, .74f), new Color(.92f, .9f, .82f) };
            for (var k = 0; k < 3; k++)
            {
                var foot = c + front * (D / 2f + .75f) + side * (W / 2f - .5f - k * .62f); foot.y = fy;
                var top = c + front * (D / 2f + .12f) + side * (W / 2f - .55f - k * .62f); top.y = fy + 2.3f;
                var d = top - foot;
                z.mk.Box(WorldMaterials.Solid(boardCols[k]), (foot + top) / 2f, new Vector3(.5f, d.magnitude, .07f), Quaternion.LookRotation(front, d.normalized));
                z.mk.Box(WorldMaterials.Solid(new Color(.3f, .3f, .32f)), (foot + top) / 2f + front * .04f, new Vector3(.05f, d.magnitude * .9f, .01f), Quaternion.LookRotation(front, d.normalized));
            }
            // the telescope on the deck, pointed up at the sky
            var scope = deckEdge - side * (W / 2f - 1.4f) - front * 1.2f; scope.y = fy;
            for (var k = 0; k < 3; k++)
            {
                var a = k * Mathf.PI * 2f / 3f; var leg = scope + (side * Mathf.Cos(a) + front * Mathf.Sin(a)) * .45f;
                var head = scope + up * 1.3f;
                z.mk.Box(board, (leg + head) / 2f, new Vector3(.04f, Vector3.Distance(leg, head), .04f), Quaternion.FromToRotation(up, (head - leg).normalized));
            }
            var tubeDir = (front * .6f + up).normalized;
            z.mk.Prism(WorldMaterials.Solid(new Color(.72f, .6f, .38f), .8f), scope + up * 1.3f - tubeDir * .5f, tubeDir, .09f, 1.3f, 10, 1.3f);
            // a rain barrel under the stilts, and an outdoor shower on the other side
            var barrel = c + side * (W / 2f + .9f) - front * (D / 2f - 1.2f); barrel.y = ctx.Ground(barrel.x, barrel.z);
            z.mk.Prism(WorldMaterials.Solid(new Color(.45f, .36f, .3f)), barrel, up, .45f, 1.1f, 10);
            foreach (var h in new[] { .2f, .85f }) z.mk.Prism(iron, barrel + up * h, up, .47f, .06f, 10);
            z.mk.Prism(water, barrel + up * 1.06f, up, .4f, .05f, 10);
            z.solid.Prism(iron, barrel, up, .47f, 1.1f, 8);
            var shower = c - side * (W / 2f + 1.3f) + front * (D / 2f - 1f); shower.y = ctx.Ground(shower.x, shower.z);
            z.mk.Box(planks, shower + up * .06f, new Vector3(1.2f, .12f, 1.2f), rot);
            z.mk.Prism(iron, shower, up, .05f, 2.4f, 6);
            z.mk.Box(iron, shower + up * 2.35f + side * .25f, new Vector3(.5f, .04f, .04f), rot);
            z.mk.Prism(iron, shower + up * 2.2f + side * .5f, up, .14f, .12f, 8, .4f);
            ArtistBits.PostCollider(z.root, shower, 2.4f, .1f);
            // a washing line with towels in the wind
            var l0 = c - side * (W / 2f + 4f) + front * 4f; l0.y = ctx.Ground(l0.x, l0.z); var l1 = l0 - front * 7f; l1.y = ctx.Ground(l1.x, l1.z);
            foreach (var p in new[] { l0, l1 }) { z.mk.Prism(driftDark, p, up, .07f, 2.1f, 6); ArtistBits.PostCollider(z.root, p, 2f, .1f); }
            Color[] towels = { new Color(.82f, .5f, .48f), new Color(.5f, .74f, .78f), new Color(.92f, .86f, .7f), new Color(.86f, .66f, .4f) };
            var lineTop0 = l0 + up * 1.95f; var lineTop1 = l1 + up * 1.95f;
            z.mk.Box(rope, (lineTop0 + lineTop1) / 2f, new Vector3(.02f, .02f, Vector3.Distance(lineTop0, lineTop1)), Quaternion.LookRotation(lineTop1 - lineTop0));
            for (var k = 0; k < 4; k++)
            {
                var at = Vector3.Lerp(lineTop0, lineTop1, .15f + k * .23f) - up * .35f - z.sea * .1f;
                z.mk.Box(WorldMaterials.Solid(towels[k]), at, new Vector3(.03f, .7f, .9f), Quaternion.LookRotation(side) * Quaternion.Euler(0, 0, ctx.Range(-12f, -4f)));
            }
            // stepping planks from the stairs to the boardwalk
            var stairFoot = deckEdge + front * 4f;
            for (var k = 0; k < 4; k++)
            {
                var p = stairFoot + front * (1.2f + k * 1.1f); p.y = ctx.Ground(p.x, p.z) + .04f;
                z.mk.Box(planks, p, new Vector3(1.6f, .1f, .7f), rot * Quaternion.Euler(0, (k % 2 == 0 ? 4f : -5f), 0));
            }
        }

        // ---------------------------------------------------------------- Agnes Obel: the piano in the water
        void Piano(Area z)
        {
            var c = z.At(-.5f, 0);
            var rot = Quaternion.LookRotation(-z.front);         // the keys and the bench toward the walk
            z.mk.Prism(water, c + Vector3.up * .04f, Vector3.up, 6f, .03f, 24);
            noGrass.Add(new Vector3(c.x, 7f, c.z));
            var black = WorldMaterials.Solid(new Color(.06f, .06f, .07f), .9f);
            // a grand piano: the body, the lid raised, three legs, the keys
            Vector3 L(float x, float y, float zz) => c + rot * new Vector3(x, y, zz);
            var body = new[] { new Vector2(-.75f, -.7f), new Vector2(.75f, -.7f), new Vector2(.75f, .3f), new Vector2(.4f, 1.1f), new Vector2(-.1f, 1.3f), new Vector2(-.6f, .9f), new Vector2(-.75f, .2f) };
            for (var i = 0; i < body.Length; i++)
            {
                var a = body[i]; var b = body[(i + 1) % body.Length];
                z.mk.Quad(black, L(a.x, .65f, a.y), L(b.x, .65f, b.y), L(b.x, 1f, b.y), L(a.x, 1f, a.y), L(0, .8f, .2f));
                z.mk.Tri(black, L(0, 1f, .2f), L(a.x, 1f, a.y), L(b.x, 1f, b.y), L(0, .5f, .2f));
                z.mk.Tri(black, L(0, .65f, .2f), L(a.x, .65f, a.y), L(b.x, .65f, b.y), L(0, 1.2f, .2f));
            }
            z.mk.Tri(black, L(.75f, 1f, -.6f), L(.4f, 1f, 1.1f), L(-.4f, 1.9f, .9f), L(.7f, 2f, .3f));
            foreach (var p in new[] { L(-.6f, 0, -.55f), L(.6f, 0, -.55f), L(0, 0, 1f) }) z.mk.Box(black, p + Vector3.up * .33f, new Vector3(.12f, .66f, .12f), rot);
            z.mk.Box(WorldMaterials.Solid(new Color(.95f, .94f, .9f)), L(0, .98f, -.78f), new Vector3(1.4f, .04f, .16f), rot);
            for (var k = 0; k < 14; k++) z.mk.Box(black, L(-.62f + k * .095f, 1.0f, -.74f), new Vector3(.04f, .03f, .09f), rot);
            z.solid.Box(black, L(0, .7f, .3f), new Vector3(1.6f, 1.4f, 2f), rot);
            z.mk.Box(black, L(0, .45f, -1.25f), new Vector3(.8f, .1f, .35f), rot);   // the bench
            // three candles burning on the lid, ember-gold
            var flame = WorldMaterials.Glow(new Color(1f, .62f, .25f), 3f);
            for (var k = 0; k < 3; k++)
            {
                var p = L(-.3f + k * .25f, 1f, .6f + (k % 2) * .15f);
                z.mk.Prism(WorldMaterials.Solid(new Color(.92f, .9f, .82f)), p, Vector3.up, .035f, .2f + k * .06f, 6);
                z.mk.Blob(flame, p + Vector3.up * (.25f + k * .06f), new Vector3(.025f, .05f, .025f), z.ctx.Rng, 0f);
                halos.Add(p + Vector3.up * (.25f + k * .06f), .5f, new Color(1f, .62f, .25f), .9f);
            }
            var cl = ArtistBits.PointLight(z.root, L(0, 1.6f, .4f), new Color(1f, .6f, .3f), 9f, 1.6f);
            cl.gameObject.AddComponent<FireFlicker>().baseIntensity = 1.6f;
            z.area.glowLight = cl;
            AgnesShore(z, c);
        }

        /// Round Agnes Obel's pool: a ring of candles on stones at the water's edge, stepping stones out to the
        /// piano bench, dark reeds behind and three bare, pale birches.
        void AgnesShore(Area z, Vector3 c)
        {
            var ctx = z.ctx; var up = Vector3.up;
            var flameCol = new Color(1f, .62f, .25f);
            var flame = WorldMaterials.Glow(flameCol, 3f); var wax = WorldMaterials.Solid(new Color(.92f, .9f, .82f));
            for (var k = 0; k < 24; k++)
            {
                var a = k * Mathf.PI * 2f / 24f + ctx.Range(-.05f, .05f);
                var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                if (Vector3.Dot(d, z.front) > .9f) continue;                        // leave the way in open
                var p = ctx.OnGround(c.x + d.x * 6.6f, c.z + d.z * 6.6f);
                z.mk.Blob(stone, p + up * .08f, new Vector3(.3f, .12f, .28f), ctx.Rng, .2f);
                var h = ctx.Range(.15f, .5f);
                z.mk.Prism(wax, p + up * .15f, up, .05f, h, 6);
                z.mk.Blob(flame, p + up * (.21f + h), new Vector3(.03f, .06f, .03f), ctx.Rng, 0f);
                if (k % 2 == 0) halos.Add(p + up * (.21f + h), .7f, flameCol, .9f);
            }
            ArtistBits.PointLight(z.root, c + up * .6f + z.front * 3f, flameCol, 10f, 1f);
            for (var k = 0; k < 4; k++)
            {
                var p = c + z.front * (2.3f + k * 1.15f) + z.side * (k % 2 == 0 ? .15f : -.15f); p.y = c.y + .02f;
                z.mk.Prism(stone, p, up, .42f, .12f, 8);
            }
            var reed = WorldMaterials.Solid(new Color(.16f, .2f, .15f), .5f);
            for (var k = 0; k < 40; k++)
            {
                var a = ctx.Range(0, Mathf.PI * 2f); var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                if (Vector3.Dot(d, z.front) > -.15f) continue;
                var r = ctx.Range(7.2f, 8.6f);
                var p = ctx.OnGround(c.x + d.x * r, c.z + d.z * r);
                for (var j = 0; j < 3; j++)
                {
                    var hh = ctx.Range(1.2f, 2.2f);
                    z.mk.Box(reed, p + new Vector3(ctx.Range(-.25f, .25f), hh / 2f, ctx.Range(-.25f, .25f)), new Vector3(.04f, hh, .04f), Quaternion.Euler(ctx.Range(-10f, 10f), 0, ctx.Range(-10f, 10f)));
                }
            }
            var birch = WorldMaterials.Solid(new Color(.88f, .87f, .83f)); var mark = WorldMaterials.Solid(new Color(.12f, .12f, .13f));
            foreach (var (fw, rt) in new[] { (-10f, -4f), (-11f, 1.5f), (-9f, 6f) })
            {
                var p = z.At(fw, rt); var h = ctx.Range(6f, 8.5f);
                var lean = new Vector3(ctx.Range(-.08f, .08f), 1f, ctx.Range(-.08f, .08f)).normalized;
                z.mk.Prism(birch, p - up * .3f, lean, .2f, h, 7, .45f);
                for (var k = 0; k < 6; k++) z.mk.Box(mark, p + lean * ctx.Range(.5f, h * .8f), new Vector3(.42f, .05f, .1f), Quaternion.Euler(0, ctx.Range(0, 360f), 0));
                for (var k = 0; k < 5; k++)
                {
                    var from = p + lean * ctx.Range(h * .45f, h * .9f);
                    var d = new Vector3(ctx.Range(-1f, 1f), ctx.Range(.4f, 1f), ctx.Range(-1f, 1f)).normalized;
                    z.mk.Prism(birch, from, d, .06f, ctx.Range(1f, 2.2f), 5, .3f);
                }
                ArtistBits.PostCollider(z.root, p, h * .7f, .25f);
            }
        }

        // ---------------------------------------------------------------- Angus & Julia Stone: two chairs
        /// Angus & Julia Stone: two chairs a little apart on an old rug, facing the sea, under a canopy of fairy
        /// lights on driftwood poles; a crate between with two mugs and a paper plane ("Big Jet Plane"), a guitar
        /// case open on the sand, and a silver coin shining in front of them.
        void TwoChairs(Area z)
        {
            var ctx = z.ctx; var up = Vector3.up;
            var look = Vector3.ProjectOnPlane(z.sea, up).normalized; var ls = Vector3.Cross(up, look);
            var cc = z.At(-2f, 0);
            var rug = Quaternion.LookRotation(look);
            z.mk.Box(WorldMaterials.Solid(new Color(.62f, .38f, .36f)), cc + up * .02f, new Vector3(4.6f, .02f, 3f), rug);
            z.mk.Box(WorldMaterials.Solid(new Color(.86f, .7f, .5f)), cc + up * .036f, new Vector3(4f, .02f, 2.4f), rug);   // 5 mm above the red rug's (grown) top
            z.mk.Box(WorldMaterials.Solid(new Color(.5f, .3f, .3f)), cc + up * .04f, new Vector3(3.4f, .02f, 1.8f), rug);
            foreach (var s in new[] { -1f, 1f }) Chair(z, ctx.OnGround(cc.x + ls.x * s * 1.6f, cc.z + ls.z * s * 1.6f), (look + ls * s * .08f).normalized);
            // the crate between them
            var crate = cc - look * .5f;
            z.mk.Box(driftDark, crate + up * .3f, new Vector3(.6f, .6f, .5f), rug);
            z.solid.Box(driftDark, crate + up * .3f, new Vector3(.6f, .6f, .5f), rug);
            foreach (var s in new[] { -.15f, .15f }) z.mk.Prism(WorldMaterials.Solid(new Color(.86f, .84f, .78f)), crate + up * .6f + ls * s, up, .05f, .11f, 8);
            var paper = WorldMaterials.Solid(new Color(.96f, .96f, .94f));
            var plane = crate + up * .62f - look * .1f;
            z.mk.Tri(paper, plane + look * .16f, plane - look * .1f + ls * .12f, plane - look * .1f, plane + up);
            z.mk.Tri(paper, plane + look * .16f, plane - look * .1f - ls * .12f, plane - look * .1f, plane + up);
            // the canopy of fairy lights
            var corners = new Vector3[4];
            for (var k = 0; k < 4; k++)
            {
                var sx = k == 0 || k == 3 ? -1f : 1f; var sz = k < 2 ? -1f : 1f;
                var p = cc + ls * sx * 2.8f + look * sz * 2.2f; p = ctx.OnGround(p.x, p.z);
                z.mk.Prism(driftDark, p - up * .2f, up, .07f, 2.9f, 6);
                ArtistBits.PostCollider(z.root, p, 2.7f, .1f);
                corners[k] = p + up * 2.6f;
            }
            for (var k = 0; k < 4; k++)
            {
                var a = corners[k]; var b = corners[(k + 1) % 4];
                var n = Mathf.Max(3, Mathf.RoundToInt(Vector3.Distance(a, b) / .55f)); var prev = a;
                for (var j = 0; j <= n; j++)
                {
                    var t = j / (float)n; var p = Vector3.Lerp(a, b, t) - up * Mathf.Sin(t * Mathf.PI) * .5f;
                    if (j > 0) z.mk.Box(iron, (prev + p) / 2f, new Vector3(.015f, .015f, Vector3.Distance(prev, p) + .01f), Quaternion.LookRotation(p - prev));
                    z.mk.Blob(bulb, p - up * .07f, Vector3.one * .06f, ctx.Rng, 0f);
                    if (j % 3 == 1) halos.Add(p - up * .07f, .6f, Bulb, .8f);
                    prev = p;
                }
            }
            ArtistBits.PointLight(z.root, cc + up * 2.2f, Bulb, 9f, 1.1f);
            // a guitar case open on the sand behind them
            var gc = cc - look * 2.4f - ls * 2.6f; gc = ctx.OnGround(gc.x, gc.z);
            var caseRot = Quaternion.LookRotation(ls) * Quaternion.Euler(0, 15f, 0);
            z.mk.Box(board, gc + up * .08f, new Vector3(.45f, .16f, 1.1f), caseRot);
            z.mk.Box(WorldMaterials.Solid(new Color(.55f, .25f, .3f)), gc + up * .165f, new Vector3(.38f, .01f, 1.02f), caseRot);
            z.mk.Box(board, gc + up * .3f + caseRot * Vector3.left * .3f, new Vector3(.05f, .45f, 1.1f), caseRot * Quaternion.Euler(0, 0, -20f));
            // a silver coin catching the light in the sand in front of them
            var coin = cc + look * .9f; coin.y = ctx.Ground(coin.x, coin.z) + .05f;
            z.mk.Prism(WorldMaterials.Glow(new Color(.85f, .88f, .92f), 2.4f), coin, up, .06f, .01f, 14);
            halos.Add(coin + up * .05f, .45f, new Color(.85f, .9f, 1f), .9f);
            noGrass.Add(new Vector3(cc.x, 3.6f, cc.z));
        }

        void Chair(Area z, Vector3 at, Vector3 look)
        {
            var rot = Quaternion.LookRotation(look);
            var w = WorldMaterials.Solid(new Color(.66f, .62f, .56f));
            z.mk.Box(w, at + Vector3.up * .45f, new Vector3(.6f, .06f, .55f), rot);
            z.mk.Box(w, at + Vector3.up * .85f - look * .27f, new Vector3(.6f, .75f, .06f), rot * Quaternion.Euler(-8f, 0, 0));
            foreach (var (x, y) in new[] { (-.25f, -.22f), (.25f, -.22f), (-.25f, .22f), (.25f, .22f) }) z.mk.Box(w, at + rot * new Vector3(x, .22f, y), new Vector3(.05f, .45f, .05f), rot);
            z.solid.Box(w, at + Vector3.up * .5f, new Vector3(.6f, 1f, .6f), rot);
        }

        // ---------------------------------------------------------------- Tracy Chapman: Fast Car
        void Car(Area z)
        {
            var look = z.sea; var rot = Quaternion.LookRotation(look); var side = Vector3.Cross(Vector3.up, look);
            var c = z.At(-.5f, .5f);
            var paint = WorldMaterials.Solid(new Color(.42f, .5f, .55f), .4f); var rust = WorldMaterials.Solid(new Color(.5f, .34f, .26f));
            z.mk.Box(paint, c + Vector3.up * .7f, new Vector3(1.9f, .6f, 4.4f), rot);
            z.mk.Box(paint, c + Vector3.up * 1.25f - look * .3f, new Vector3(1.7f, .55f, 2.2f), rot);
            z.mk.Box(WorldMaterials.Solid(new Color(.2f, .24f, .28f), .8f), c + Vector3.up * 1.27f - look * .3f, new Vector3(1.72f, .4f, 2.1f), rot);
            z.mk.Blob(rust, c + Vector3.up * .65f + side * .96f + look * .8f, new Vector3(.05f, .2f, .4f), z.ctx.Rng, .3f);
            foreach (var s in new[] { -1f, 1f }) foreach (var k in new[] { -1.35f, 1.35f })
                z.mk.Prism(WorldMaterials.Solid(new Color(.1f, .1f, .1f)), c + side * s * .9f + look * k + Vector3.up * .38f - side * .12f, side, .38f, .24f, 12);
            var lamp = WorldMaterials.Glow(new Color(1f, .92f, .75f), 3f);
            foreach (var s in new[] { -1f, 1f })
            {
                var hl = c + Vector3.up * .78f + look * 2.21f + side * s * .65f;
                z.mk.Box(lamp, hl, new Vector3(.3f, .2f, .04f), rot);
                halos.Add(hl + look * .2f, 1.1f, new Color(1f, .92f, .75f), .9f);
            }
            var beam = new GameObject("Headlights").AddComponent<Light>();
            beam.transform.SetParent(z.root, false);
            beam.type = LightType.Spot; beam.spotAngle = 55f; beam.range = 30f; beam.intensity = 2.5f; beam.color = new Color(1f, .92f, .75f);
            beam.transform.SetPositionAndRotation(c + Vector3.up * .8f + look * 2.3f, Quaternion.LookRotation(look - Vector3.up * .12f));
            z.solid.Box(paint, c + Vector3.up * .9f, new Vector3(1.9f, 1.6f, 4.4f), rot);
        }

        // ---------------------------------------------------------------- Israel: a ukulele on a bench by a pool
        void Ukulele(Area z)
        {
            var c = z.At(-.5f, 0);
            z.mk.Prism(water, c + Vector3.up * .04f + z.front * 2.6f, Vector3.up, 2.2f, .03f, 18);
            var rot = Quaternion.LookRotation(z.front);
            z.mk.Box(drift, c + Vector3.up * .45f, new Vector3(1.8f, .08f, .45f), rot);
            foreach (var s in new[] { -1f, 1f }) z.mk.Box(driftDark, c + z.side * s * .75f + Vector3.up * .22f, new Vector3(.08f, .45f, .4f), rot);
            z.solid.Box(drift, c + Vector3.up * .45f, new Vector3(1.8f, .9f, .5f), rot);
            var uke = c + Vector3.up * .5f + z.side * .3f;
            var koa = WorldMaterials.Solid(new Color(.72f, .46f, .24f), .6f);
            z.mk.Blob(koa, uke + Vector3.up * .03f, new Vector3(.13f, .04f, .18f), z.ctx.Rng, 0f);
            z.mk.Box(board, uke + Vector3.up * .05f - z.front * .3f, new Vector3(.04f, .02f, .3f), rot);
            Color[] petals = { new Color(1f, .6f, .7f), new Color(1f, .85f, .4f), new Color(.95f, .95f, .9f) };
            for (var k = 0; k < 10; k++) z.mk.Blob(WorldMaterials.Solid(petals[k % 3]), c + Vector3.up * .5f - z.side * .4f + new Vector3(Mathf.Cos(k * .63f) * .2f, 0, Mathf.Sin(k * .63f) * .2f), Vector3.one * .05f, z.ctx.Rng, .2f);
        }

        // ---------------------------------------------------------------- Nick Drake: a rowing boat under a pink moon
        void PinkMoon(Area z)
        {
            var c = z.At(-.5f, .5f); var look = z.sea; var rot = Quaternion.LookRotation(look); var side = Vector3.Cross(Vector3.up, look);
            var hull = WorldMaterials.Solid(new Color(.55f, .6f, .62f));
            for (var i = 0; i < 6; i++)
            {
                var t = i / 5f - .5f; var w = Mathf.Lerp(.75f, .2f, Mathf.Abs(t) * 2f);
                z.mk.Box(hull, c + look * t * 3.6f + Vector3.up * .25f, new Vector3(w * 2f, .5f, .62f), rot);
            }
            z.mk.Box(drift, c + Vector3.up * .45f, new Vector3(1.3f, .06f, .3f), rot);
            z.solid.Box(hull, c + Vector3.up * .3f, new Vector3(1.5f, .6f, 3.6f), rot);
            var post = c + side * 1.8f; post.y = z.ctx.Ground(post.x, post.z);
            z.mk.Prism(driftDark, post, Vector3.up, .07f, 3.2f, 6);
            var moon = WorldMaterials.Glow(new Color(1f, .62f, .75f), 2.4f);
            z.mk.Blob(moon, post + Vector3.up * 3.4f, Vector3.one * .35f, z.ctx.Rng, 0f);
            halos.Add(post + Vector3.up * 3.4f, 2.4f, new Color(1f, .62f, .75f), 1f);
            ArtistBits.PostCollider(z.root, post, 3.2f, .12f);
        }

        // ---------------------------------------------------------------- everyone else
        void TidePost(Area z)
        {
            var p = z.At(-.3f, .8f);
            for (var k = 0; k < 8; k++) z.mk.Box(WorldMaterials.Solid(k % 2 == 0 ? new Color(.9f, .9f, .88f) : new Color(.25f, .3f, .4f)), p + Vector3.up * (k * .4f + .2f), new Vector3(.25f, .4f, .25f), Quaternion.identity);
            var bell = WorldMaterials.Solid(new Color(.72f, .56f, .3f), .7f);
            z.mk.Prism(bell, p + Vector3.up * 3.5f, Vector3.down, .25f, .35f, 10, 1.4f);
            ArtistBits.PostCollider(z.root, p, 3.6f, .2f);
        }

        void ClosedHut(Area z)
        {
            var p = z.At(-1f, .5f); var rot = Quaternion.LookRotation(z.front);
            var paint = WorldMaterials.Solid(HutPaint[0]);
            z.mk.Box(paint, p + Vector3.up * 1.7f, new Vector3(2.6f, 3f, 2.4f), rot);
            z.solid.Box(paint, p + Vector3.up * 1.7f, new Vector3(2.6f, 3f, 2.4f), rot);
            z.mk.GableRoof(WorldMaterials.Solid(new Color(.86f, .85f, .82f)), p + Vector3.up * 3.2f, new Vector3(2.8f, 0, 3f), 1.1f, Quaternion.LookRotation(z.side));
            for (var k = 0; k < 7; k++) z.mk.Box(driftDark, p + z.front * 1.22f + Vector3.up * (.8f + k * .25f), new Vector3(1.3f, .14f, .04f), rot);   // boarded-up door
            var leaf = WorldMaterials.Solid(new Color(.7f, .45f, .25f));
            for (var k = 0; k < 20; k++) z.mk.Box(leaf, z.At(z.ctx.Range(-.5f, 2f), z.ctx.Range(-2f, 2f)) + Vector3.up * .02f, new Vector3(.12f, .01f, .08f), Quaternion.Euler(0, z.ctx.Range(0, 360f), 0));
        }

        void Windsock(Area z)
        {
            var p = z.At(-.5f, 1f);
            z.mk.Prism(iron, p, Vector3.up, .06f, 4.2f, 6);
            var dir = new Vector3(z.sea.z, 0, -z.sea.x);
            for (var k = 0; k < 5; k++)
                z.mk.Prism(WorldMaterials.Solid(k % 2 == 0 ? new Color(.85f, .45f, .3f) : new Color(.92f, .9f, .86f)), p + Vector3.up * 4f + dir * k * .32f, dir, .25f - k * .03f, .33f, 10, .88f);
            ArtistBits.PostCollider(z.root, p, 4.2f, .12f);
        }

        /// Ryley Walker, "The West Wind": wind chimes of shells, sea glass and hollow driftwood hanging from a
        /// driftwood frame, all blown the same way.
        void WindChimes(Area z)
        {
            var ctx = z.ctx; var up = Vector3.up; var p = z.At(-.8f, 0); var side = z.side;
            foreach (var s in new[] { -1f, 1f })
            {
                var f = p + side * s * 1.4f; f.y = ctx.Ground(f.x, f.z);
                z.mk.Prism(driftDark, f - up * .2f, up, .1f, 3f, 6);
                ArtistBits.PostCollider(z.root, f, 2.8f, .12f);
            }
            var bar0 = p - side * 1.6f + up * 2.75f; var bar1 = p + side * 1.6f + up * 2.75f;
            z.mk.Prism(drift, bar0, side, .09f, 3.2f, 6);
            var lean = -Vector3.ProjectOnPlane(z.sea, up).normalized * .25f;   // blown in off the sea
            var shell = WorldMaterials.Solid(new Color(.93f, .89f, .82f));
            for (var k = 0; k < 9; k++)
            {
                var top = Vector3.Lerp(bar0, bar1, (k + .5f) / 9f);
                var len = .9f + (k * 37 % 7) * .12f;
                var dir = (-up + lean).normalized; var bottom = top + dir * len;
                z.mk.Box(rope, (top + bottom) / 2f, new Vector3(.015f, len, .015f), Quaternion.FromToRotation(up, -dir));
                for (var j = 1; j <= 3; j++)
                {
                    var at = top + dir * (len * j / 3f);
                    var kind = (k + j) % 3;
                    if (kind == 0) z.mk.Prism(driftDark, at - dir * .1f, dir, .05f, .32f, 6);
                    else if (kind == 1) z.mk.Blob(shell, at, new Vector3(.08f, .06f, .03f), ctx.Rng, .2f);
                    else
                    {
                        var col = SeaGlass[(k + j) % SeaGlass.Length];
                        z.mk.Box(WorldMaterials.Glow(col, 2f), at, new Vector3(.08f, .1f, .02f), Quaternion.LookRotation(z.front));
                        if (k % 2 == 0) halos.Add(at, .5f, col, .8f);
                    }
                }
            }
        }

        void Silhouette(Area z)
        {
            var p = z.At(-1.5f, 0);
            z.mk.Blob(WorldMaterials.Solid(DuneSand), p, new Vector3(3f, 1f, 3f), z.ctx.Rng, .1f);
            var dark = WorldMaterials.Solid(new Color(.12f, .12f, .14f));
            var top = p + Vector3.up * .9f;
            var rot = Quaternion.LookRotation(z.sea);
            z.mk.Box(dark, top + Vector3.up * .8f, new Vector3(.5f, 1.2f, .06f), rot);
            z.mk.Blob(dark, top + Vector3.up * 1.65f, new Vector3(.17f, .21f, .05f), z.ctx.Rng, 0f);
            z.mk.Box(dark, top + Vector3.up * .2f, new Vector3(.36f, .5f, .06f), rot);
            z.solid.Box(dark, p + Vector3.up * 1.2f, new Vector3(1.6f, 2.4f, 1.6f), rot);
        }

        void Figurehead(Area z)
        {
            var p = z.At(-.8f, .5f);
            var wood = WorldMaterials.Solid(new Color(.55f, .45f, .38f));
            var lean = Quaternion.LookRotation(z.front) * Quaternion.Euler(-25f, 0, 0);
            z.mk.Prism(wood, p - Vector3.up * .3f, lean * Vector3.up, .45f, 1.6f, 8, .7f);
            z.mk.Blob(wood, p + lean * Vector3.up * 1.7f, new Vector3(.3f, .38f, .3f), z.ctx.Rng, .05f);
            z.mk.Blob(WorldMaterials.Solid(new Color(.75f, .6f, .35f)), p + lean * Vector3.up * 1.85f - z.front * .1f, new Vector3(.34f, .2f, .34f), z.ctx.Rng, .15f);
            ArtistBits.PostCollider(z.root, p, 1.8f, .5f);
        }

        void TwoGuitars(Area z)
        {
            var log = z.At(-.8f, 0); var along = z.side;
            z.mk.Prism(drift, log + Vector3.up * .3f - along * 1.2f, along, .32f, 2.4f, 7);
            z.solid.Box(drift, log + Vector3.up * .3f, new Vector3(2.4f, .6f, .6f), Quaternion.LookRotation(along));
            var wood = WorldMaterials.Solid(new Color(.75f, .55f, .32f), .6f);
            foreach (var s in new[] { -.5f, .5f })
            {
                var g = log + along * s + z.front * .35f;
                z.mk.Blob(wood, g + Vector3.up * .35f, new Vector3(.2f, .28f, .07f), z.ctx.Rng, 0f);
                z.mk.Box(board, g + Vector3.up * .8f - z.front * .05f, new Vector3(.05f, .6f, .04f), Quaternion.LookRotation(z.front) * Quaternion.Euler(-12f, 0, 0));
            }
        }

        void Seabirds(Area z)
        {
            var white = WorldMaterials.Solid(new Color(.95f, .95f, .93f)); var grey = WorldMaterials.Solid(new Color(.4f, .42f, .46f));
            var rock = z.At(-.8f, .5f);
            z.mk.Blob(stone, rock + Vector3.up * .6f, new Vector3(1.8f, 1.1f, 1.5f), z.ctx.Rng, .25f);
            z.solid.Box(stone, rock + Vector3.up * .6f, new Vector3(3f, 1.4f, 2.6f), Quaternion.identity);
            for (var k = 0; k < 5; k++)
            {
                var b = rock + Vector3.up * 1.6f + new Vector3(z.ctx.Range(-1f, 1f), 0, z.ctx.Range(-.8f, .8f));
                var look = Quaternion.Euler(0, z.ctx.Range(0, 360f), 0) * Vector3.forward;
                z.mk.Blob(white, b, new Vector3(.12f, .12f, .22f), z.ctx.Rng, .05f);
                z.mk.Box(grey, b + Vector3.up * .06f - look * .05f, new Vector3(.2f, .04f, .18f), Quaternion.LookRotation(look));
                z.mk.Blob(white, b + Vector3.up * .12f + look * .15f, Vector3.one * .07f, z.ctx.Rng, 0f);
            }
        }

        void Raven(Area z)
        {
            var post = z.At(-.6f, .8f);
            z.mk.Prism(drift, post, Vector3.up, .14f, 2f, 6);
            var dark = WorldMaterials.Solid(new Color(.08f, .08f, .1f), .5f);
            z.mk.Blob(dark, post + Vector3.up * 2.2f, new Vector3(.16f, .18f, .28f), z.ctx.Rng, .05f);
            z.mk.Blob(dark, post + Vector3.up * 2.42f + z.front * .18f, Vector3.one * .1f, z.ctx.Rng, 0f);
            z.mk.Box(dark, post + Vector3.up * 2.4f + z.front * .3f, new Vector3(.03f, .03f, .12f), Quaternion.LookRotation(z.front));
            ArtistBits.PostCollider(z.root, post, 2f, .14f);
        }

        void ThreeCrates(Area z)
        {
            var c = z.At(-.5f, 0);
            for (var k = 0; k < 3; k++)
            {
                var a = k * Mathf.PI * 2f / 3f; var p = c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 1.3f; p.y = z.ctx.Ground(p.x, p.z);
                z.mk.Box(driftDark, p + Vector3.up * .25f, new Vector3(.6f, .5f, .5f), Quaternion.Euler(0, a * Mathf.Rad2Deg, 0));
                z.solid.Box(driftDark, p + Vector3.up * .25f, new Vector3(.6f, .5f, .5f), Quaternion.Euler(0, a * Mathf.Rad2Deg, 0));
            }
            z.mk.Box(WorldMaterials.Solid(new Color(.3f, .25f, .22f)), c + Vector3.up * .15f, new Vector3(.45f, .3f, .25f), Quaternion.identity);   // an old radio
            z.mk.Box(WorldMaterials.Glow(Ember, 1.6f), c + Vector3.up * .2f + Vector3.forward * .13f, new Vector3(.2f, .08f, .01f), Quaternion.identity);
        }

        void Lemonade(Area z)
        {
            var c = z.At(-.6f, .3f); var rot = Quaternion.LookRotation(z.front);
            z.mk.Box(drift, c + Vector3.up * .8f, new Vector3(1.6f, .08f, .7f), rot);
            foreach (var s in new[] { -1f, 1f }) z.mk.Box(driftDark, c + z.side * s * .7f + Vector3.up * .4f, new Vector3(.08f, .8f, .6f), rot);
            z.solid.Box(drift, c + Vector3.up * .45f, new Vector3(1.6f, .9f, .7f), rot);
            var glass = WorldMaterials.Solid(new Color(.95f, .9f, .55f), .8f);
            z.mk.Prism(glass, c + Vector3.up * .84f - z.side * .4f, Vector3.up, .1f, .3f, 10);
            for (var k = 0; k < 3; k++) z.mk.Prism(glass, c + Vector3.up * .84f + z.side * (.1f + k * .2f), Vector3.up, .05f, .14f, 8);
        }

        void Lifebuoy(Area z)
        {
            var post = z.At(-.6f, .8f);
            z.mk.Prism(driftDark, post, Vector3.up, .1f, 1.8f, 6);
            var red = WorldMaterials.Solid(new Color(.82f, .3f, .26f)); var white = WorldMaterials.Solid(new Color(.92f, .92f, .9f));
            for (var k = 0; k < 16; k++)
            {
                var a0 = k * Mathf.PI / 8f; var a1 = (k + 1) * Mathf.PI / 8f;
                Vector3 P(float a) => post + Vector3.up * 1.3f + z.front * .14f + (z.side * Mathf.Cos(a) + Vector3.up * Mathf.Sin(a)) * .35f;
                z.mk.Box((k / 2) % 2 == 0 ? red : white, (P(a0) + P(a1)) / 2f, new Vector3(.14f, .14f, Vector3.Distance(P(a0), P(a1)) + .02f), Quaternion.LookRotation(P(a1) - P(a0), z.front));
            }
            ArtistBits.PostCollider(z.root, post, 1.8f, .15f);
        }

        void Marker(Area z)
        {
            var p = z.At(-.7f, .6f);
            z.mk.Box(drift, p + Vector3.up * .7f, new Vector3(.14f, 1.4f, .12f), Quaternion.LookRotation(z.front));
            z.mk.Box(drift, p + Vector3.up * 1.05f, new Vector3(.7f, .12f, .12f), Quaternion.LookRotation(z.front));
            var lamp = WorldMaterials.Glow(Ember, 2f);
            z.mk.Box(lamp, p + Vector3.up * .15f + z.front * .4f, new Vector3(.18f, .26f, .18f), Quaternion.identity);
            halos.Add(p + Vector3.up * .2f + z.front * .4f, .9f, Ember, .8f);
            ArtistBits.PostCollider(z.root, p, 1.4f, .15f);
        }

        void GuitarStand(Area z)
        {
            var p = z.At(-.6f, .8f);
            z.mk.Blob(WorldMaterials.Solid(new Color(.75f, .55f, .32f), .6f), p + Vector3.up * .4f, new Vector3(.22f, .3f, .08f), z.ctx.Rng, 0f);
            z.mk.Box(board, p + Vector3.up * .95f, new Vector3(.05f, .65f, .04f), Quaternion.LookRotation(z.front) * Quaternion.Euler(-10f, 0, 0));
            ArtistBits.PostCollider(z.root, p, 1.2f, .3f);
            TidePool(z.ctx, z.mk, z.At(1f, -2.5f), z.sea);
        }
    }
}
