using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Indie Downs' artist areas. Each faces the path it's reached by, and wears the colour of whoever added
    /// it (both colours where both did). Death Cab for Cutie: the record shop on a little high street. Florence
    /// Road: an open-air stage on the lawn where the two paths meet. Rainbow Kitten Surprise: a rainbow bus stop on
    /// a lane to nowhere. Pavement: the crossroads, with a fingerpost for each path. Everyone else gets a small
    /// summer moment: a picnic, a market stall, deckchairs, a viewpoint, bikes by a fence, a phone box.
    public partial class IndieDownsKit
    {
        static int Hash(string s) { var h = 17; foreach (var ch in s ?? "") h = h * 31 + ch; return h & 0x7fffffff; }

        Color AccentOf(IslandWorldData.Artist a, int i)
        {
            bool n = By(a, Noah), s = By(a, Sean);
            if (n && s) return i % 2 == 0 ? NoahBlue : SeanGreen;
            return n ? NoahBlue : s ? SeanGreen : new Color(.85f, .55f, .3f);
        }

        public override void BuildArtist(LandContext ctx, IslandWorldData.Artist a)
        {
            if (chalk == null) MakeMaterials();
            var root = new GameObject("Artist: " + a.name).transform;
            root.SetParent(ctx.Root, false);
            var center = ctx.OnGround(a.x, a.z);
            var front = arrival.TryGetValue(a.name, out var f) ? f : new Vector3(ctx.Plaza.x - a.x, 0, ctx.Plaza.z - a.z).normalized;
            if (front.sqrMagnitude < .01f) front = Vector3.forward;
            var mk = new MeshKit(); var solid = new MeshKit(); var pictures = new MeshKit();
            var labels = new GameObject("Labels").transform; labels.SetParent(root, false);
            var area = root.gameObject.AddComponent<ArtistArea>();
            area.artistName = a.name; area.stationId = a.stationId; area.radius = a.r; area.glowColor = Lamp;
            var glows = new List<Renderer>();
            var ctxA = new Area { ctx = ctx, root = root, mk = mk, solid = solid, pictures = pictures, labels = labels, a = a, center = center, front = front, side = Vector3.Cross(Vector3.up, front), glows = glows, area = area };

            if (a.name == "Death Cab for Cutie") RecordShop(ctxA);
            else if (a.name == "Florence Road") Stage(ctxA);
            else if (a.name == "Rainbow Kitten Surprise") BusStop(ctxA);
            else if (a.name == "Pavement") Crossroads(ctxA);
            else if (a.tier != "shrine") Moment(ctxA, Hash(a.name) % 6);
            else Small(ctxA, Hash(a.name) % 3);

            foreach (var g in glows) if (g != null) halos.Add(g.transform.position, 1.1f, Lamp, .7f);
            area.glowRenderers = glows.ToArray();
            mk.Build("Area", root, false);
            solid.BuildCollider("Walls", root);
            pictures.Build("Covers", root, false);
        }

        class Area
        {
            public LandContext ctx; public Transform root, labels; public MeshKit mk, solid, pictures;
            public IslandWorldData.Artist a; public Vector3 center, front, side; public List<Renderer> glows; public ArtistArea area;
            public Vector3 At(float forward, float right) { var p = center + front * forward + side * right; return ctx.OnGround(p.x, p.z); }
        }

        void Plaque(Area z, float forward, float right) =>
            ArtistBits.Plaque(z.mk, z.solid, z.labels, z.a, z.At(forward, right), z.front, darkWood, board, Cream, 1.5f);

        /// A small board on two posts holding covers, facing 'facing'.
        void CoverBoard(Area z, Vector3 ground, Vector3 facing, int count, float size = .8f)
        {
            facing = Vector3.ProjectOnPlane(facing, Vector3.up).normalized;
            var rot = Quaternion.LookRotation(facing);
            var side = Vector3.Cross(Vector3.up, facing);
            var n = Mathf.Max(1, Mathf.Min(count, z.a.songList?.Length ?? 1));
            var w = n * size * 1.25f + .3f;
            foreach (var s in new[] { -1f, 1f }) z.mk.Prism(darkWood, ground + side * s * (w / 2f - .1f) - facing * .05f, Vector3.up, .06f, 2.1f, 5);
            var c = ground + Vector3.up * 1.6f;
            z.mk.Box(board, c - Vector3.up * .1f, new Vector3(w, size + .55f, .06f), rot);
            z.solid.Box(board, c - Vector3.up * .1f, new Vector3(w, size + .55f, .15f), rot);
            ArtistBits.CoverWall(z.pictures, z.labels, z.ctx, z.a, c + facing * .035f + Vector3.up * .08f, facing, n, size, n, true, Cream);
        }

        void StripedPanel(MeshKit mk, Vector3 center, Vector3 right, Vector3 up, float w, float h, int stripes, Color a, Color b, float thick = .05f)
        {
            var n = Vector3.Cross(right, up).normalized;
            var rot = Quaternion.LookRotation(n, up);
            for (var i = 0; i < stripes; i++)
            {
                var x = (i + .5f) / stripes - .5f;
                mk.Box(WorldMaterials.Solid(i % 2 == 0 ? a : b), center + right.normalized * x * w, new Vector3(w / stripes + .002f, h, thick), rot);
            }
        }

        // ---------------------------------------------------------------- Death Cab for Cutie: the record shop
        void RecordShop(Area z)
        {
            var ctx = z.ctx;
            const float w = 12f, d = 11f, wallH = 5f, t = .35f;
            var front = z.front; var side = z.side;
            var rot = Quaternion.LookRotation(front);
            var c = z.center - front * 2f; c.y = 0f;           // heights below are absolute
            var (lo, hi) = Footprint(ctx, c, rot, w, d);
            var floorY = hi + .15f;
            var accent = AccentOf(z.a, 0);
            var facade = WorldMaterials.Solid(Color.Lerp(accent, Cream, .45f));
            var floor = WorldMaterials.Solid(new Color(.55f, .4f, .3f));
            Floor(z.mk, z.solid, floor, stone, c, rot, w, d, floorY, lo);
            void Wall(Material m, float x0, float x1, float y0, float y1, float zc, float thick, Quaternion r)
            {
                var p = c + rot * new Vector3((x0 + x1) / 2f, 0, zc) + Vector3.up * (floorY + (y0 + y1) / 2f);
                var size = new Vector3(x1 - x0, y1 - y0, thick);
                z.mk.Box(m, p, size, r); z.solid.Box(m, p, size, r);
            }
            // back and sides in brick
            Wall(brick, -w / 2f, w / 2f, 0, wallH, -d / 2f + t / 2f, t, rot);
            var sideRot = Quaternion.LookRotation(side);
            foreach (var s in new[] { -1f, 1f })
            {
                var p = c + side * s * (w / 2f - t / 2f) + Vector3.up * (floorY + wallH / 2f);
                z.mk.Box(brick, p, new Vector3(d - 2f * t, wallH, t), sideRot); z.solid.Box(brick, p, new Vector3(d, wallH, t), sideRot);
            }
            // the painted shopfront: a wide door between two big windows
            float zf = d / 2f - t / 2f, doorHalf = HumanScale.DoorWidth / 2f;
            Wall(facade, -w / 2f, -5f, 0, wallH, zf, t, rot); Wall(facade, 5f, w / 2f, 0, wallH, zf, t, rot);
            Wall(facade, -2f, -doorHalf, 0, wallH, zf, t, rot); Wall(facade, doorHalf, 2f, 0, wallH, zf, t, rot);
            Wall(facade, -doorHalf, doorHalf, HumanScale.DoorHeight, wallH, zf, t, rot);
            foreach (var s in new[] { -1f, 1f })
            {
                Wall(facade, s < 0 ? -5f : 2f, s < 0 ? -2f : 5f, 0, .9f, zf, t, rot);
                Wall(facade, s < 0 ? -5f : 2f, s < 0 ? -2f : 5f, 3.2f, wallH, zf, t, rot);
                z.mk.Box(cream, c + rot * new Vector3(s * 3.5f, 0, zf + .1f) + Vector3.up * (floorY + .92f), new Vector3(3.1f, .1f, .35f), rot);   // sill
            }
            z.mk.Box(slate, c + Vector3.up * (floorY + wallH + .1f), new Vector3(w + .4f, .2f, d + .4f), rot);   // roof
            // the sign and a striped awning
            var signAt = c + front * (d / 2f + .06f) + Vector3.up * (floorY + 4.2f);
            z.mk.Box(board, signAt, new Vector3(6.5f, .9f, .1f), rot);
            ArtistBits.Label(z.labels, "RECORDS", signAt + front * .07f + Vector3.up * .3f, front, .5f, Cream);
            for (var i = 0; i < 12; i++)
            {
                var x = (i + .5f) / 12f * (w - .4f) - (w - .4f) / 2f;
                var col = WorldMaterials.Solid(i % 2 == 0 ? accent : Cream);
                var top = c + rot * new Vector3(x, 0, d / 2f) + Vector3.up * (floorY + 3.5f);
                var tilt = Quaternion.LookRotation(front) * Quaternion.Euler(28f, 0, 0);
                z.mk.Box(col, top + front * .8f - Vector3.up * .42f, new Vector3((w - .4f) / 12f + .01f, .05f, 1.8f), tilt);
            }
            // inside: covers on the back wall, two record bins, a counter, a listening booth
            ArtistBits.CoverWall(z.pictures, z.labels, ctx, z.a, c - front * (d / 2f - t - .03f) + Vector3.up * (floorY + 2.7f), front, 5, .95f, 10, true, Cream);
            var pic = WorldMaterials.Picture(ctx.CoverAtlas);
            var songs = z.a.songList ?? new IslandWorldData.Song[0];
            foreach (var s in new[] { -1f, 1f })
            {
                var bin = c + side * s * 2.4f + front * .5f + Vector3.up * floorY;
                z.mk.Box(wood, bin + Vector3.up * .45f, new Vector3(1.2f, .9f, 3.2f), rot);
                z.solid.Box(wood, bin + Vector3.up * .45f, new Vector3(1.2f, .9f, 3.2f), rot);
                var lean = 35f * Mathf.Deg2Rad;
                var normal = front * Mathf.Cos(lean) + Vector3.up * Mathf.Sin(lean);
                var upv = Vector3.up * Mathf.Cos(lean) - front * Mathf.Sin(lean);
                var r = Vector3.Cross(normal, Vector3.up).normalized;
                for (var k = 0; k < 6 && songs.Length > 0; k++)
                {
                    var song = songs[(k + (s > 0 ? 3 : 0)) % songs.Length];
                    if (song.cover < 0) continue;
                    var p = bin + Vector3.up * 1.12f + front * (1.25f - k * .5f);
                    z.pictures.Picture(pic, p, r * .3f, upv * .3f, normal, ctx.CoverRect(song.cover));
                    z.mk.Box(board, p - normal * .015f, new Vector3(.6f, .6f, .02f), Quaternion.LookRotation(normal, upv));
                }
            }
            var counter = c + side * 3.6f - front * 3.2f + Vector3.up * floorY;
            z.mk.Box(darkWood, counter + Vector3.up * .55f, new Vector3(1f, 1.1f, 2.6f), rot);
            z.solid.Box(darkWood, counter + Vector3.up * .55f, new Vector3(1f, 1.1f, 2.6f), rot);
            z.mk.Box(iron, counter + Vector3.up * 1.25f, new Vector3(.5f, .3f, .4f), rot);
            var booth = c - side * 4.2f - front * 3.4f + Vector3.up * floorY;
            z.mk.Box(glassDark, booth + side * 1.1f + Vector3.up * 1.3f, new Vector3(.08f, 2.6f, 2.2f), rot);
            z.mk.Box(glassDark, booth + front * 1.1f + Vector3.up * 2.2f, new Vector3(2.2f, .8f, .08f), rot);
            z.solid.Box(glassDark, booth + side * 1.1f + Vector3.up * 1.3f, new Vector3(.15f, 2.6f, 2.2f), rot);
            z.solid.Box(glassDark, booth + front * 1.1f + Vector3.up * 2.2f, new Vector3(2.2f, .8f, .15f), rot);
            z.solid.Box(darkWood, booth + Vector3.up * .35f, new Vector3(.5f, .7f, .5f), rot);
            z.mk.Prism(iron, booth, Vector3.up, .2f, .62f, 8);
            z.mk.Box(darkWood, booth + Vector3.up * .66f, new Vector3(.5f, .08f, .5f), rot);
            z.mk.Box(board, booth - side * .6f + Vector3.up * 1.6f, new Vector3(.08f, .3f, .35f), rot);   // headphones on the wall
            var light = ArtistBits.PointLight(z.root, c + Vector3.up * (floorY + 4f), new Color(1f, .78f, .55f), 14f, 1.4f, true);
            z.area.glowLight = light;
            IndoorSpaces.Add(c + Vector3.up * (floorY + wallH / 2f), rot, new Vector3(w, wallH, d));
            Ramp(ctx, z.mk, z.solid, stone, c + front * (d / 2f), front, floorY, HumanScale.DoorWidth + .4f);
            ArtistBits.Plaque(z.mk, z.solid, z.labels, z.a, ctx.OnGround(c.x + front.x * (d / 2f + 2.6f) + side.x * 4.2f, c.z + front.z * (d / 2f + 2.6f) + side.z * 4.2f), front, darkWood, board, Cream, 1.5f);
            // the rest of the little high street: a cottage either side, a bike and a lamp post
            foreach (var s in new[] { -1f, 1f })
            {
                var at = c + side * s * (w / 2f + 5.5f) - front * .5f;
                if (ctx.IsLand(at.x, at.z, 4f))
                    House(ctx, z.mk, z.solid, ctx.OnGround(at.x, at.z), front, 8f, 7.5f, 5f, Pastels[s < 0 ? 1 : 2], Doors[s < 0 ? 0 : 3], s < 0 ? slate : terracotta);
            }
            Bike(z.mk, z.At(d / 2f - 2f + .9f, -w / 2f + 1.2f), z.side, Pastels[3]);
            LampPost(ctx, z.mk, z.pictures, z.At(d / 2f - 2f + 2.6f, w / 2f - .8f), front, 2);
        }

        /// A floor you can stand on at floorY, on a foundation that reaches down to the ground.
        static void Floor(MeshKit mk, MeshKit solid, Material top, Material foundation, Vector3 c, Quaternion rot, float w, float d, float floorY, float groundLo)
        {
            var h = floorY - groundLo + .5f;
            mk.Box(foundation, new Vector3(c.x, floorY - .1f - h / 2f, c.z), new Vector3(w + .4f, h, d + .4f), rot);
            mk.Box(top, new Vector3(c.x, floorY - .05f, c.z), new Vector3(w, .1f, d), rot);
            solid.Box(top, new Vector3(c.x, floorY - .25f, c.z), new Vector3(w, .5f, d), rot);
        }

        /// A gentle ramp from a doorway (or a stage edge) down to the ground.
        static void Ramp(LandContext ctx, MeshKit mk, MeshKit solid, Material m, Vector3 threshold, Vector3 outward, float floorY, float width)
        {
            outward = Vector3.ProjectOnPlane(outward, Vector3.up).normalized;
            var g = ctx.Ground(threshold.x + outward.x * 1.5f, threshold.z + outward.z * 1.5f);
            if (floorY - g < .22f) return;
            var len = Mathf.Max(1.6f, (floorY - g) / Mathf.Tan(18f * Mathf.Deg2Rad));
            var far = threshold + outward * len; far.y = ctx.Ground(far.x, far.z) - .05f;
            var near = new Vector3(threshold.x, floorY - .02f, threshold.z);
            var r = Quaternion.LookRotation(near - far);
            var size = new Vector3(width, .2f, Vector3.Distance(near, far) + .4f);
            mk.Box(m, (near + far) / 2f, size, r);
            solid.Box(m, (near + far) / 2f, size, r);
        }

        // ---------------------------------------------------------------- Florence Road: the stage
        void Stage(Area z)
        {
            var ctx = z.ctx;
            const float W = 12f, D = 7f;
            var front = z.front; var side = z.side;
            var rot = Quaternion.LookRotation(front);
            var c = z.center - front * 5f; c.y = 0f;
            var (lo, hi) = Footprint(ctx, c, rot, W, D);
            var floorY = hi + 1.1f;
            var deck = WorldMaterials.Solid(new Color(.42f, .33f, .26f));
            Floor(z.mk, z.solid, deck, darkWood, c, rot, W, D, floorY, lo);
            Ramp(ctx, z.mk, z.solid, deck, c + side * (W / 2f) + front * 1.5f, side, floorY, 2.4f);
            // back wall with the covers, big
            var back = c - front * (D / 2f - .15f) + Vector3.up * (floorY + 3f);
            z.mk.Box(board, back, new Vector3(W, 6f, .3f), rot);
            z.solid.Box(board, back, new Vector3(W, 6f, .3f), rot);
            ArtistBits.CoverWall(z.pictures, z.labels, ctx, z.a, back + front * .17f + Vector3.up * .3f, front, 3, 1.5f, 6, true, Cream);
            // the truss over the front of the stage, with lights in both their colours
            var trussY = floorY + 7f;
            foreach (var s in new[] { -1f, 1f })
            {
                var foot = c + side * s * (W / 2f + .3f) + front * (D / 2f - .3f); foot.y = ctx.Ground(foot.x, foot.z);
                z.mk.Box(iron, new Vector3(foot.x, (foot.y + trussY) / 2f, foot.z), new Vector3(.4f, trussY - foot.y, .4f), rot);
                z.solid.Box(iron, new Vector3(foot.x, (foot.y + trussY) / 2f, foot.z), new Vector3(.45f, trussY - foot.y, .45f), rot);
                // speaker stacks
                var sp = c + side * s * (W / 2f + 1.8f) + front * (D / 2f - 1f); sp.y = ctx.Ground(sp.x, sp.z);
                z.mk.Box(board, sp + Vector3.up * 1.3f, new Vector3(1.4f, 2.6f, 1.2f), rot);
                z.solid.Box(board, sp + Vector3.up * 1.3f, new Vector3(1.4f, 2.6f, 1.2f), rot);
                foreach (var y in new[] { .75f, 1.85f }) z.mk.Prism(iron, sp + Vector3.up * y + front * .6f, front, .42f, .05f, 12);
            }
            var beamMid = c + front * (D / 2f - .3f); beamMid.y = trussY;
            z.mk.Box(iron, beamMid, new Vector3(W + 1f, .4f, .4f), rot);
            for (var i = 0; i < 6; i++)
            {
                var x = (i + .5f) / 6f * W - W / 2f;
                var col = i % 3 == 2 ? Lamp : (i % 3 == 0 ? NoahBlue : SeanGreen);
                z.glows.Add(ArtistBits.GlassBox(z.root, beamMid + side * x - Vector3.up * .45f, new Vector3(.4f, .4f, .4f), col));
            }
            // the lawn: festoon lights to two poles, hay bales to sit on, bunting in both colours
            var poleL = z.At(D / 2f + 13f - 5f, -7f); var poleR = z.At(D / 2f + 13f - 5f, 7f);
            foreach (var p in new[] { poleL, poleR }) { z.mk.Prism(darkWood, p, Vector3.up, .12f, 5.5f, 6); ArtistBits.PostCollider(z.root, p, 5.5f, .16f); }
            var bulb = WorldMaterials.Glow(Lamp, 2.2f);
            void Festoon(Vector3 a, Vector3 b)
            {
                var n = Mathf.Max(3, Mathf.FloorToInt(Vector3.Distance(a, b) / 1.3f));
                for (var i = 0; i <= n; i++)
                {
                    var t = i / (float)n;
                    var p = Vector3.Lerp(a, b, t) - Vector3.up * 1.2f * 4f * t * (1f - t);
                    z.mk.Blob(bulb, p, Vector3.one * .1f, ctx.Rng, 0f);
                    if (i % 2 == 0) halos.Add(p, .45f, Lamp, .6f);
                }
            }
            var trussL = c - side * (W / 2f + .3f) + front * (D / 2f - .3f); trussL.y = trussY - .3f;
            var trussR = c + side * (W / 2f + .3f) + front * (D / 2f - .3f); trussR.y = trussY - .3f;
            Festoon(trussL, poleL + Vector3.up * 5.3f); Festoon(trussR, poleR + Vector3.up * 5.3f); Festoon(poleL + Vector3.up * 5.3f, poleR + Vector3.up * 5.3f);
            Festoon(trussL, poleR + Vector3.up * 5.3f); Festoon(trussR, poleL + Vector3.up * 5.3f);
            var both = new[] { NoahBlue, Cream, SeanGreen, Cream };
            Bunting(z.mk, poleL + Vector3.up * 3.2f, trussL - Vector3.up * 3.5f, both, 0, .8f);
            Bunting(z.mk, poleR + Vector3.up * 3.2f, trussR - Vector3.up * 3.5f, both, 1, .8f);
            var hay = WorldMaterials.Solid(new Color(.86f, .76f, .46f));
            for (var i = 0; i < 6; i++)
            {
                var p = z.At(D / 2f - 5f + 5f + (i / 3) * 3.4f, (i % 3 - 1) * 3.6f + ((i / 3) % 2) * 1.2f);
                var r = Quaternion.LookRotation(front) * Quaternion.Euler(0, (i * 37) % 20 - 10, 0);
                z.mk.Box(hay, p + Vector3.up * .3f, new Vector3(1.5f, .6f, .8f), r);
                z.solid.Box(hay, p + Vector3.up * .3f, new Vector3(1.5f, .6f, .8f), r);
            }
            z.area.glowLight = ArtistBits.PointLight(z.root, c + front * 4f + Vector3.up * (floorY + 6f), Lamp, 18f, 1.2f);
            Plaque(z, D / 2f - 5f + 2.5f, -(W / 2f + 2.5f));
        }

        // ---------------------------------------------------------------- Rainbow Kitten Surprise: the bus stop
        void BusStop(Area z)
        {
            var ctx = z.ctx;
            var front = z.front; var side = z.side;
            var rot = Quaternion.LookRotation(front);
            // a short lane that comes from nowhere and goes nowhere
            var lane = new List<Vector3>();
            for (var x = -18f; x <= 18f; x += 2f) lane.Add(z.At(3.6f, x));
            Ribbon(ctx, z.mk, WorldMaterials.Solid(new Color(.45f, .45f, .47f)), lane, 4f, .09f);
            var c = z.At(-.5f, 0);
            const float W = 5f, H = 2.7f, D = 1.8f;
            Color[] rainbow = { new Color(.9f, .3f, .3f), new Color(.95f, .58f, .25f), new Color(.95f, .85f, .35f), new Color(.45f, .78f, .42f), new Color(.35f, .6f, .9f), new Color(.55f, .42f, .8f), new Color(.85f, .45f, .7f) };
            for (var i = 0; i < rainbow.Length; i++)
            {
                var y = (i + .5f) / rainbow.Length * H;
                z.mk.Box(WorldMaterials.Solid(rainbow[i]), c - front * (D / 2f) + Vector3.up * y, new Vector3(W, H / rainbow.Length + .01f, .12f), rot);
            }
            z.solid.Box(cream, c - front * (D / 2f) + Vector3.up * H / 2f, new Vector3(W, H, .2f), rot);
            foreach (var s in new[] { -1f, 1f })
            {
                z.mk.Box(glassDark, c + side * s * (W / 2f) + Vector3.up * (H / 2f), new Vector3(.06f, H, D), rot);
                z.solid.Box(glassDark, c + side * s * (W / 2f) + Vector3.up * (H / 2f), new Vector3(.12f, H, D), rot);
            }
            z.mk.Box(cream, c + Vector3.up * (H + .08f), new Vector3(W + .4f, .16f, D + .5f), rot);
            for (var i = 0; i < rainbow.Length; i++)
                z.mk.Box(WorldMaterials.Solid(rainbow[i]), c + front * (D / 2f + .25f) + Vector3.up * (H + .08f) + side * ((i + .5f) / rainbow.Length * (W + .4f) - (W + .4f) / 2f), new Vector3((W + .4f) / rainbow.Length + .01f, .2f, .06f), rot);
            ArtistBits.CoverWall(z.pictures, z.labels, ctx, z.a, c - front * (D / 2f - .08f) + Vector3.up * 1.75f, front, 3, .7f, 3, true, Cream);
            Bench(z.mk, z.solid, c - front * .35f, front);
            // the stop sign and a timetable
            var flag = z.At(2.4f, W / 2f + 1f);
            z.mk.Prism(iron, flag, Vector3.up, .06f, 3f, 6);
            z.mk.Prism(red, flag + Vector3.up * 2.75f - front * .03f, front, .4f, .06f, 14);
            z.mk.Prism(cream, flag + Vector3.up * 2.75f + front * .01f, front, .3f, .04f, 14);
            ArtistBits.Label(z.labels, "BUS\nSTOP", flag + Vector3.up * 2.87f + front * .07f, front, .11f, Ink);
            ArtistBits.PostCollider(z.root, flag, 3f, .1f);
            z.mk.Box(board, flag + Vector3.up * 1.5f + front * .06f, new Vector3(.5f, .6f, .04f), rot);
            Plaque(z, 1.5f, -(W / 2f + 2f));
            var lamp = ArtistBits.LanternPost(z.mk, z.root, z.At(1.2f, -(W / 2f + 3.6f)), side, darkWood, iron, Lamp, 3f);
            z.glows.Add(lamp);
        }

        // ---------------------------------------------------------------- Pavement: the crossroads
        void Crossroads(Area z)
        {
            var ctx = z.ctx;
            Bench(z.mk, z.solid, z.At(-3f, 2f), z.front);
            CoverBoard(z, z.At(-3.5f, -2.2f), z.front, 2);
            Plaque(z, 3.2f, 2.4f);
            var a = z.At(4f, -5f); var b = z.At(4f, 5f);
            foreach (var p in new[] { a, b }) { z.mk.Prism(darkWood, p, Vector3.up, .08f, 2.8f, 6); ArtistBits.PostCollider(z.root, p, 2.8f, .12f); }
            Bunting(z.mk, a + Vector3.up * 2.7f, b + Vector3.up * 2.7f, new[] { NoahBlue, Cream, SeanGreen, Cream }, 0, .6f);
        }

        // ---------------------------------------------------------------- everyone else: summer moments
        void Moment(Area z, int kind)
        {
            var ctx = z.ctx;
            var accent = AccentOf(z.a, 0); var accent2 = AccentOf(z.a, 1);
            var front = z.front; var side = z.side;
            switch (kind)
            {
                case 0: // a picnic under a tree
                {
                    var c = z.At(0, 0);
                    var rug = Quaternion.LookRotation(front) * Quaternion.Euler(0, 12, 0);
                    for (var i = 0; i < 4; i++)
                    for (var j = 0; j < 3; j++)
                        z.mk.Box(WorldMaterials.Solid((i + j) % 2 == 0 ? accent : Cream), c + rug * new Vector3((i - 1.5f) * .7f, .03f, (j - 1f) * .7f), new Vector3(.7f, .04f, .7f), rug);
                    z.mk.Box(wood, c + rug * new Vector3(1.6f, .25f, .9f), new Vector3(.6f, .4f, .4f), rug);
                    z.mk.Box(darkWood, c + rug * new Vector3(1.6f, .55f, .9f), new Vector3(.5f, .05f, .05f), rug);
                    Tree(z, z.At(-4.5f, -3f), 1.1f);
                    CoverBoard(z, z.At(-2.5f, 3.5f), front, 2);
                    Plaque(z, 3.2f, -2.8f);
                    break;
                }
                case 1: // a market stall
                {
                    var c = z.At(-1f, 0);
                    var rot = Quaternion.LookRotation(front);
                    z.mk.Box(wood, c + Vector3.up * .9f, new Vector3(3.4f, .1f, 1.3f), rot);
                    z.solid.Box(wood, c + Vector3.up * .5f, new Vector3(3.4f, 1f, 1.3f), rot);
                    foreach (var sx in new[] { -1f, 1f })
                    foreach (var sz in new[] { -1f, 1f })
                        z.mk.Prism(darkWood, c + side * sx * 1.6f + front * sz * .55f, Vector3.up, .05f, sz > 0 ? 2.3f : 2.6f, 5);
                    StripedPanel(z.mk, c + Vector3.up * 2.5f, side, (front * -.45f + Vector3.up * .2f).normalized * 1f, 3.6f, 1.6f, 8, accent2, Cream);
                    var pic = WorldMaterials.Picture(ctx.CoverAtlas);
                    var songs = z.a.songList ?? new IslandWorldData.Song[0];
                    for (var i = 0; i < songs.Length && i < 3; i++)
                    {
                        if (songs[i].cover < 0) continue;
                        var lean = 25f * Mathf.Deg2Rad;
                        var n = front * Mathf.Cos(lean) + Vector3.up * Mathf.Sin(lean);
                        var u = Vector3.up * Mathf.Cos(lean) - front * Mathf.Sin(lean);
                        var at = c + Vector3.up * 1.3f + side * (i - 1f) * 1f;
                        z.pictures.Picture(pic, at, Vector3.Cross(n, Vector3.up).normalized * .4f, u * .4f, n, ctx.CoverRect(songs[i].cover));
                        z.mk.Box(board, at - n * .015f, new Vector3(.8f, .8f, .02f), Quaternion.LookRotation(n, u));
                    }
                    for (var i = 0; i < 3; i++) z.mk.Box(wood, c - front * 1.2f + side * (i - 1f) * .9f + Vector3.up * .25f, new Vector3(.7f, .5f, .5f), Quaternion.LookRotation(front) * Quaternion.Euler(0, i * 17, 0));
                    Plaque(z, 2.5f, -3f);
                    break;
                }
                case 2: // a red phone box on the hill, and a bench
                {
                    PhoneBox(z.mk, z.solid, z.At(-1f, -1.6f), front);
                    Bench(z.mk, z.solid, z.At(0f, 1.6f), front);
                    CoverBoard(z, z.At(-1.2f, 3.8f), front, 2);
                    Plaque(z, 2.6f, -2.6f);
                    break;
                }
                case 3: // a viewpoint: a bench and a telescope looking out
                {
                    var out_ = -front;
                    Bench(z.mk, z.solid, z.At(-1f, -1f), out_);
                    var tri = z.At(-1.5f, 1.4f);
                    for (var i = 0; i < 3; i++)
                    {
                        var a = i * Mathf.PI * 2f / 3f;
                        var foot = tri + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * .45f; foot.y = ctx.Ground(foot.x, foot.z);
                        var top = tri + Vector3.up * 1.3f; var d = top - foot;
                        z.mk.Box(darkWood, (foot + top) / 2f, new Vector3(.05f, d.magnitude, .05f), Quaternion.LookRotation(front, d.normalized));
                    }
                    z.mk.Prism(WorldMaterials.Solid(new Color(.75f, .6f, .3f), .5f), tri + Vector3.up * 1.4f - out_ * .5f, (out_ + Vector3.up * .25f).normalized, .1f, 1.2f, 8, .8f);
                    CoverBoard(z, z.At(1.2f, 3.2f), front, 2);
                    Plaque(z, 2.6f, -2.8f);
                    break;
                }
                case 4: // deckchairs and a striped windbreak
                {
                    for (var i = 0; i < 2; i++) DeckChair(z, z.At(-.5f, (i - .5f) * 1.8f), -front, i == 0 ? accent : accent2);
                    for (var i = 0; i < 4; i++)
                    {
                        var p = z.At(-2.6f, (i - 1.5f) * 1.4f);
                        z.mk.Prism(darkWood, p, Vector3.up, .04f, 1.6f, 5);
                    }
                    StripedPanel(z.mk, z.At(-2.6f, 0) + Vector3.up * .8f, side, Vector3.up, 4.2f, 1.4f, 6, accent, Cream, .03f);
                    CoverBoard(z, z.At(1.4f, 3.4f), front, 2);
                    Plaque(z, 2.8f, -2.8f);
                    break;
                }
                default: // bikes against a fence and a lamp post with gig posters
                {
                    for (var i = 0; i < 5; i++) z.mk.Prism(darkWood, z.At(-1.6f, (i - 2f) * 1.5f), Vector3.up, .06f, 1.1f, 5);
                    for (var y = 0; y < 2; y++)
                    {
                        var a = z.At(-1.6f, -3f) + Vector3.up * (.45f + y * .4f); var b = z.At(-1.6f, 3f) + Vector3.up * (.45f + y * .4f);
                        z.mk.Box(wood, (a + b) / 2f, new Vector3(.06f, .1f, Vector3.Distance(a, b)), Quaternion.LookRotation(b - a));
                    }
                    z.solid.Box(wood, z.At(-1.6f, 0) + Vector3.up * .55f, new Vector3(6f, 1.1f, .2f), Quaternion.LookRotation(front));
                    Bike(z.mk, z.At(-1.1f, -1.3f), side, Color.Lerp(accent, Cream, .3f));
                    Bike(z.mk, z.At(-1.1f, 1.5f), side, Pastels[4]);
                    LampPost(ctx, z.mk, z.pictures, z.At(1.2f, 3.4f), front, 2);
                    CoverBoard(z, z.At(.5f, -3.6f), front, 2);
                    Plaque(z, 2.6f, 0f);
                    break;
                }
            }
        }

        /// Artists with one song: a gig poster on a lamp post with a bike, a bench by a cover, or a deckchair.
        void Small(Area z, int kind)
        {
            var accent = AccentOf(z.a, 0);
            switch (kind)
            {
                case 0:
                    LampPostCover(z, z.At(-.5f, 1.2f));
                    Bike(z.mk, z.At(-.6f, -.4f), z.side, Color.Lerp(accent, Cream, .25f));
                    break;
                case 1:
                    CoverBoard(z, z.At(-1.2f, 0), z.front, 1);
                    Bench(z.mk, z.solid, z.At(.8f, 1.6f), z.front);
                    break;
                default:
                    DeckChair(z, z.At(0f, 1.4f), z.front, accent);
                    CoverBoard(z, z.At(-1.4f, -.6f), z.front, 1);
                    break;
            }
            Plaque(z, 2f, -1.6f);
        }

        /// A lamp post with this artist's own cover as a gig poster on it.
        void LampPostCover(Area z, Vector3 at)
        {
            z.mk.Prism(iron, at, Vector3.up, .1f, 4f, 8);
            ArtistBits.PostCollider(z.root, at, 4f, .15f);
            z.mk.Box(iron, at + Vector3.up * 4f, new Vector3(.5f, .12f, .5f), Quaternion.identity);
            z.glows.Add(ArtistBits.GlassBox(z.root, at + Vector3.up * 3.75f, new Vector3(.34f, .4f, .34f), Lamp));
            var song = z.a.songList != null && z.a.songList.Length > 0 ? z.a.songList[0] : null;
            if (song == null || song.cover < 0) return;
            var n = z.front; var r = Vector3.Cross(n, Vector3.up);
            z.pictures.Picture(WorldMaterials.Picture(z.ctx.CoverAtlas), at + Vector3.up * 1.6f + n * .12f, r * .4f, Vector3.up * .4f, n, z.ctx.CoverRect(song.cover));
        }

        void DeckChair(Area z, Vector3 at, Vector3 facing, Color stripe)
        {
            facing = Vector3.ProjectOnPlane(facing, Vector3.up).normalized;
            var rot = Quaternion.LookRotation(facing);
            var side = Vector3.Cross(Vector3.up, facing);
            foreach (var s in new[] { -1f, 1f })
            {
                var a = at + side * s * .3f + facing * .45f; var b = at + side * s * .3f - facing * .4f + Vector3.up * .95f;
                var d = b - a;
                z.mk.Box(wood, (a + b) / 2f, new Vector3(.04f, d.magnitude, .04f), Quaternion.LookRotation(side, d.normalized));
            }
            var seatA = at + facing * .4f + Vector3.up * .35f; var seatB = at - facing * .35f + Vector3.up * .95f;
            var up = (seatB - seatA).normalized;
            StripedPanel(z.mk, (seatA + seatB) / 2f, side, up, .56f, Vector3.Distance(seatA, seatB), 4, stripe, Cream, .02f);
            z.solid.Box(wood, at + Vector3.up * .45f, new Vector3(.7f, .9f, .9f), rot);
        }

        void Tree(Area z, Vector3 at, float scale)
        {
            z.mk.Prism(bark, at, Vector3.up, .3f * scale, 4.5f * scale, 7, .7f);
            var r = 3.2f * scale;
            z.mk.Blob(leafA, at + Vector3.up * (4f * scale + r * .5f), new Vector3(r, r * .85f, r), z.ctx.Rng, .12f);
            z.mk.Blob(leafB, at + Vector3.up * (3.6f * scale + r * .4f) + new Vector3(r * .5f, 0, r * .3f), new Vector3(r * .7f, r * .6f, r * .7f), z.ctx.Rng, .12f);
            ArtistBits.PostCollider(z.root, at, 4f * scale, .35f * scale);
        }
    }
}
