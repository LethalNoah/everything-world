using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Ridge's bands, each a big earnest set piece on its own lit pad, with a plaque, covers on a board and a
    /// lamp:
    /// - Blue Öyster Cult: a giant hooded figure with a scythe on a rock, eyes that glow in "(Don't Fear) The
    ///   Reaper", a giant cowbell hung on a frame beside it, a fire pit ("Burnin' for You").
    /// - Boston: a guitar-shaped spaceship hovering over a domed pad (it flies a loop in "More Than a Feeling").
    /// - Journey: a giant spoked wheel turning high in the sky on a tall frame ("Wheel in the Sky"), a stretch of
    ///   track with a little midnight train that runs in "Don't Stop Believin'", streetlights along it.
    /// - Styx: a sailing ship in a cradle that rises into the sky, glowing, in "Come Sail Away".
    /// - Survivor: a boxing ring under a giant tiger's eye that lights up in "Eye of the Tiger".
    /// - Cheap Trick: a giant checkered guitar. Heart: a big neon heart and a barracuda. Kansas: a slowly turning
    ///   tornado funnel. Night Ranger: a hot rod with flames. REO Speedwagon: an old speedwagon. The Edgar Winter
    ///   Group: a mad-science tower with sparking coils ("Frankenstein").
    public partial class ArenaKit
    {
        class Plot
        {
            public LandContext ctx; public Transform root, labels; public MeshKit mk, solid, pictures;
            public IslandWorldData.Artist a; public Vector3 c, front, side; public List<Renderer> glows; public ArtistArea area;
            public Vector3 At(float fw, float rt) { var p = c + front * fw + side * rt; return ctx.OnGround(p.x, p.z); }
        }

        public override void BuildArtist(LandContext ctx, IslandWorldData.Artist a)
        {
            if (stone == null) MakeMaterials();
            var root = new GameObject("Artist: " + a.name).transform; root.SetParent(ctx.Root, false);
            var front = facing.TryGetValue(a.name, out var f) ? f : new Vector3(center.x - a.x, 0, center.z - a.z).normalized;
            if (front.sqrMagnitude < .01f) front = Vector3.forward;
            var labels = new GameObject("Labels").transform; labels.SetParent(root, false);
            var area = root.gameObject.AddComponent<ArtistArea>();
            area.artistName = a.name; area.stationId = a.stationId; area.radius = a.r; area.glowColor = Orange;
            var z = new Plot { ctx = ctx, root = root, labels = labels, mk = new MeshKit(), solid = new MeshKit(), pictures = new MeshKit(), a = a, c = ctx.OnGround(a.x, a.z), front = front, side = Vector3.Cross(Vector3.up, front), glows = new List<Renderer>(), area = area };
            // a round lit pad under every set piece
            z.mk.Prism(WorldMaterials.Solid(new Color(.3f, .3f, .34f)), z.c - Vector3.up * .3f, Vector3.up, Foot(a) * .75f, .4f, 24);
            for (var k = 0; k < 16; k++) { var d = Quaternion.Euler(0, k * 22.5f, 0) * Vector3.forward; z.mk.Box(GlowM(Orange, 2f), z.c + Vector3.up * .12f + d * (Foot(a) * .75f), new Vector3(.4f, .1f, .4f), Quaternion.LookRotation(d)); }
            switch (a.name)
            {
                case "Blue Öyster Cult": ReaperSet(z); ReaperMore(z); break;
                case "Boston": GuitarShip(z); BostonMore(z); break;
                case "Journey": Wheel(z); JourneyMore(z); break;
                case "Styx": Ship(z); break;
                case "Survivor": Ring(z); break;
                case "Cheap Trick": Checkered(z); break;
                case "Heart": HeartFish(z); break;
                case "Kansas": Tornado(z); break;
                case "Night Ranger": HotRod(z); break;
                case "REO Speedwagon": Speedwagon(z); break;
                case "The Edgar Winter Group": Lab(z); break;
                case "Starship": CityNight(z); break;
                case "U2": WhiteFlag(z); break;
                default: Mic(z); break;
            }
            var reach = Foot(a);
            Board(z, z.At(reach * .85f, -reach * .5f - 2.5f), front);
            ArtistBits.Plaque(z.mk, z.solid, labels, a, z.At(reach * .85f + 1.5f, reach * .5f + 2.5f), front, metal, dark, Ink, 1.6f);
            z.glows.Add(Lamp(ctx, z.mk, root, z.At(reach * .85f, reach * .5f + 5f)));
            if (z.area.glowLight == null) z.area.glowLight = ArtistBits.PointLight(root, z.c + Vector3.up * 6f + front * 5f, Spot, HasStation(a) ? 26f : 19f, 2.6f);
            area.glowRenderers = z.glows.ToArray();
            z.mk.Build("Area", root, false);
            z.solid.BuildCollider("Walls", root);
            z.pictures.Build("Covers", root, false);
            noGrass.Add(new Vector3(z.c.x, reach, z.c.z));
        }

        void Board(Plot z, Vector3 ground, Vector3 dir)
        {
            dir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized; var rot = Quaternion.LookRotation(dir); var side = Vector3.Cross(Vector3.up, dir);
            var n = Mathf.Max(1, Mathf.Min(4, z.a.songList?.Length ?? 1)); const float size = .9f; var w = n * size * 1.25f + .4f;
            foreach (var s in new[] { -1f, 1f }) z.mk.Prism(metal, ground + side * s * (w / 2f - .1f) - dir * .05f, Vector3.up, .07f, 2.4f, 6);
            var c = ground + Vector3.up * 1.8f;
            z.mk.Box(dark, c - Vector3.up * .1f, new Vector3(w, size + .6f, .1f), rot); z.solid.Box(dark, c - Vector3.up * .1f, new Vector3(w, size + .6f, .2f), rot);
            ArtistBits.CoverWall(z.pictures, z.labels, z.ctx, z.a, c + dir * .055f + Vector3.up * .08f, dir, n, size, n, true, Ink);
        }

        Transform Mover(Plot z, string name, MeshKit k, Vector3 at, Quaternion rot)
        {
            var t = new GameObject(name).transform; t.SetParent(z.root, false);
            k.BuildSingle(name, t, false); t.SetPositionAndRotation(at, rot); return t;
        }

        // ---------------------------------------------------------------- Blue Öyster Cult
        void ReaperSet(Plot z)
        {
            var up = Vector3.up; var ctx = z.ctx; var robe = WorldMaterials.Solid(new Color(.06f, .06f, .08f));
            var rockAt = z.At(-4f, 2f);
            z.mk.Blob(stone, rockAt + up * 2f, new Vector3(4f, 3f, 3.6f), ctx.Rng, .3f); z.solid.Box(stone, rockAt + up * 2f, new Vector3(6f, 4f, 5f), Quaternion.identity);
            var f = rockAt + up * 4.2f;
            z.mk.Prism(robe, f, up, 1.6f, 6f, 10, .35f);                     // the robe
            z.mk.Prism(robe, f + up * 5.6f, up, .9f, 1.6f, 10, .5f);          // the hood
            z.mk.Blob(dark, f + up * 6.3f + z.front * .35f, new Vector3(.55f, .65f, .3f), ctx.Rng, 0f);
            var eyes = new Material(GlowM(new Color(.5f, .8f, 1f), .5f)) { name = "Reaper eyes" };
            foreach (var s in new[] { -.2f, .2f }) z.mk.Blob(eyes, f + up * 6.4f + z.front * .6f + z.side * s, Vector3.one * .1f, ctx.Rng, 0f);
            life.reaperEyes = eyes; life.reaper = f + up * 6.4f + z.front * 2f;
            // the scythe
            var pole = f + z.side * 1.6f; z.mk.Box(wood, pole + up * 3.6f, new Vector3(.15f, 8f, .15f), Quaternion.LookRotation(z.front) * Quaternion.Euler(0, 0, -8f));
            z.mk.Box(metal, pole + up * 7.4f + z.front * 1.2f + z.side * .3f, new Vector3(.1f, .5f, 3f), Quaternion.LookRotation(z.front) * Quaternion.Euler(-15f, 0, 0));
            // a giant cowbell hung on a frame
            var cb = z.At(6f, -9f);
            foreach (var s in new[] { -1f, 1f }) { z.mk.Box(dark, cb + z.side * s * 2.4f + up * 3f, new Vector3(.3f, 6f, .3f), Quaternion.LookRotation(z.front)); ArtistBits.PostCollider(z.root, cb + z.side * s * 2.4f, 6f, .2f); }
            z.mk.Box(dark, cb + up * 6f, new Vector3(5.2f, .3f, .3f), Quaternion.LookRotation(z.front));
            var bell = WorldMaterials.Solid(new Color(.8f, .65f, .35f), .9f);
            z.mk.Prism(bell, cb + up * 2.4f, up, 1.5f, 3.2f, 4, .6f, 45f);
            z.mk.Prism(dark, cb + up * 2.35f, up, 1.2f, .05f, 4, 1f, 45f);
            z.mk.Box(metal, cb + up * 5.8f, new Vector3(.15f, .5f, .15f), Quaternion.identity);
            ArtistBits.Label(z.labels, "MORE", cb + up * 4.2f + z.front * 1.05f, z.front, .3f, new Color(.15f, .1f, .05f), TextAnchor.MiddleCenter);
            // a fire pit
            var pit = z.At(5f, 8f);
            for (var k = 0; k < 9; k++) z.mk.Blob(stone, pit + Quaternion.Euler(0, k * 40f, 0) * Vector3.forward * 1.2f + up * .2f, new Vector3(.35f, .25f, .3f), ctx.Rng, .3f);
            var fl = ArtistBits.PointLight(z.root, pit + up * 1f, Flame, 12f, 2f); fl.gameObject.AddComponent<FireFlicker>().baseIntensity = 2f;
            ArtistBits.PostCollider(z.root, pit, .6f, 1.3f);
            life.fires.Add(pit + up * .3f);
        }

        // ---------------------------------------------------------------- Boston
        void GuitarShip(Plot z)
        {
            var up = Vector3.up; var dome = z.At(-2f, 0);
            z.mk.Blob(WorldMaterials.Solid(new Color(.75f, .78f, .85f), .9f), dome + up * .5f, new Vector3(5f, 1.6f, 5f), z.ctx.Rng, 0f);
            z.solid.Prism(stone, dome - up * .2f, up, 4.6f, 1.8f, 12);
            var k = new MeshKit(); var hull = WorldMaterials.Solid(new Color(.55f, .4f, .25f), .8f); var neck = WorldMaterials.Solid(new Color(.3f, .22f, .15f));
            k.Blob(hull, new Vector3(0, 0, -2f), new Vector3(3.2f, .9f, 3.6f), new System.Random(5), 0f);
            k.Blob(hull, new Vector3(0, 0, 1.5f), new Vector3(2.4f, .85f, 2.4f), new System.Random(6), 0f);
            k.Box(neck, new Vector3(0, 0, 6.5f), new Vector3(.8f, .4f, 7f), Quaternion.identity);
            k.Box(neck, new Vector3(0, 0, 10.5f), new Vector3(1.4f, .5f, 1.8f), Quaternion.identity);
            k.Blob(WorldMaterials.Glow(new Color(.5f, .85f, 1f), 2.4f), new Vector3(0, .9f, -1.5f), new Vector3(1.3f, .6f, 1.3f), new System.Random(7), 0f);
            for (var e = 0; e < 3; e++) k.Prism(WorldMaterials.Glow(new Color(1f, .6f, .25f), 2.6f), new Vector3((e - 1) * 1.2f, 0, -5.4f), Vector3.back, .4f, .4f, 10);
            var ship = Mover(z, "Guitar ship", k, dome + up * 9f, Quaternion.LookRotation(z.side));
            life.guitarShip = ship; life.guitarShipHome = ship.position;
            halos.Add(dome + up * 9f, 6f, new Color(.5f, .85f, 1f), .4f);
        }

        // ---------------------------------------------------------------- Journey
        void Wheel(Plot z)
        {
            var up = Vector3.up; var c = z.At(-4f, 0); var hub = c + up * 15f; var rot = Quaternion.LookRotation(z.front);
            foreach (var s in new[] { -1f, 1f }) { z.mk.Box(metal, c + z.side * s * 5f + up * 7.5f, new Vector3(.5f, 15.5f, .5f), rot * Quaternion.Euler(0, 0, s * 18f)); ArtistBits.PostCollider(z.root, c + z.side * s * 7.4f, 4f, .4f); }
            var k = new MeshKit(); var rimM = WorldMaterials.Glow(new Color(.55f, .75f, 1f), 1.6f);
            for (var i = 0; i < 24; i++)
            {
                float a0 = i * Mathf.PI * 2f / 24f, a1 = (i + 1) * Mathf.PI * 2f / 24f;
                var p0 = new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0) * 11f; var p1 = new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0) * 11f;
                k.Box(rimM, (p0 + p1) / 2f, new Vector3(.4f, .4f, Vector3.Distance(p0, p1) + .05f), Quaternion.LookRotation(p1 - p0, Vector3.forward));
                if (i % 2 == 0) k.Box(WorldMaterials.Solid(new Color(.8f, .82f, .86f), .8f), p0 / 2f, new Vector3(.15f, .15f, 11f), Quaternion.LookRotation(p0, Vector3.forward));
                if (i % 3 == 0) k.Blob(WorldMaterials.Glow(new Color(1f, .9f, .6f), 2.6f), p0, Vector3.one * .45f, new System.Random(i), 0f);
            }
            k.Prism(metal, new Vector3(0, 0, -.5f), Vector3.forward, 1f, 1f, 12);
            life.wheel = Mover(z, "Wheel in the Sky", k, hub, rot);
            halos.Add(hub + z.front * 2f, 9f, new Color(.55f, .75f, 1f), .35f);
            // a stretch of track with streetlights and a little midnight train
            var t0 = z.At(6f, -14f); var t1 = z.At(6f, 14f);
            var sleepers = (int)(Vector3.Distance(t0, t1) / 1.1f);
            for (var i = 0; i <= sleepers; i++) { var p = Vector3.Lerp(t0, t1, i / (float)sleepers); p = z.ctx.OnGround(p.x, p.z); z.mk.Box(wood, p + up * .08f, new Vector3(.3f, .16f, 2.6f), Quaternion.LookRotation(z.front)); }
            foreach (var s in new[] { -.75f, .75f }) z.mk.Box(metal, (t0 + t1) / 2f + z.front * s + up * .25f, new Vector3(.1f, .12f, Vector3.Distance(t0, t1)), Quaternion.LookRotation(t1 - t0));
            for (var i = 0; i < 3; i++) { var lp = Vector3.Lerp(t0, t1, (i + .5f) / 3f) + z.front * 3f; lp = z.ctx.OnGround(lp.x, lp.z); z.mk.Prism(dark, lp, up, .08f, 4.5f, 6); z.mk.Box(GlowM(new Color(1f, .8f, .45f), 2.4f), lp + up * 4.5f, new Vector3(.4f, .3f, .4f), Quaternion.identity); halos.Add(lp + up * 4.5f, 1.4f, new Color(1f, .8f, .45f), .8f); ArtistBits.PostCollider(z.root, lp, 4.5f, .1f); }
            var tk = new MeshKit(); var eng = WorldMaterials.Solid(new Color(.15f, .15f, .18f), .7f);
            tk.Box(eng, new Vector3(0, 1f, 0), new Vector3(1.6f, 1.6f, 4f), Quaternion.identity); tk.Prism(eng, new Vector3(0, 1.1f, -1.6f), Vector3.forward, .75f, 3.2f, 12);
            tk.Prism(eng, new Vector3(0, 1.8f, 1.2f), Vector3.up, .25f, 1f, 8);
            tk.Prism(WorldMaterials.Glow(new Color(1f, .95f, .7f), 2.6f), new Vector3(0, 1.3f, 2.01f), Vector3.forward, .3f, .05f, 10);
            for (var w = 0; w < 4; w++) tk.Box(WorldMaterials.Glow(new Color(1f, .85f, .5f), 1.6f), new Vector3(.81f, 1.4f, -1.2f + w * .8f), new Vector3(.02f, .4f, .5f), Quaternion.identity);
            tk.Box(WorldMaterials.Solid(new Color(.5f, .1f, .12f), .6f), new Vector3(0, 1.1f, -4.6f), new Vector3(1.6f, 1.6f, 4f), Quaternion.identity);
            for (var w = 0; w < 4; w++) tk.Box(WorldMaterials.Glow(new Color(1f, .85f, .5f), 1.6f), new Vector3(.81f, 1.4f, -6f + w * .8f), new Vector3(.02f, .5f, .5f), Quaternion.identity);
            life.train = Mover(z, "Midnight train", tk, t0 + up * .3f, Quaternion.LookRotation(t1 - t0));
            life.trainA = t0 + up * .3f; life.trainB = t1 + up * .3f;
        }

        // ---------------------------------------------------------------- Styx
        void Ship(Plot z)
        {
            var up = Vector3.up; var c = z.At(-3f, 0);
            foreach (var s in new[] { -1f, 1f }) foreach (var b in new[] { -1f, 1f }) z.mk.Box(wood, c + z.side * s * 1.6f + z.front * b * 3.5f + up * 1.2f, new Vector3(.4f, 2.4f, .4f), Quaternion.LookRotation(z.front) * Quaternion.Euler(0, 0, s * 20f));
            z.solid.Box(wood, c + up * 1.2f, new Vector3(3.6f, 2.4f, 8f), Quaternion.LookRotation(z.front));
            var k = new MeshKit(); var hull = WorldMaterials.Solid(new Color(.45f, .3f, .2f)); var sail = WorldMaterials.Solid(new Color(.92f, .9f, .82f));
            k.Box(hull, new Vector3(0, .8f, 0), new Vector3(3f, 1.6f, 11f), Quaternion.identity);
            k.Prism(hull, new Vector3(0, .8f, 5.5f), Vector3.forward, 1.5f, 2.4f, 4, .1f, 45f);
            k.Box(hull, new Vector3(0, 2f, -4.6f), new Vector3(3f, 1.2f, 2f), Quaternion.identity);
            foreach (var mz in new[] { -2f, 2.4f })
            {
                k.Box(wood, new Vector3(0, 7f, mz), new Vector3(.3f, 11f, .3f), Quaternion.identity);
                k.Box(sail, new Vector3(0, 7.3f, mz - 1.8f), new Vector3(.06f, 6.5f, 3.4f), Quaternion.identity);
                k.Box(wood, new Vector3(0, 4.2f, mz - 1.8f), new Vector3(.15f, .15f, 3.8f), Quaternion.identity);
            }
            k.Tri(sail, new Vector3(0, 12f, 2.4f), new Vector3(0, 2.6f, 2.4f), new Vector3(0, 2.6f, 7.5f), new Vector3(1, 7, 4));
            k.Tri(sail, new Vector3(0, 12f, 2.4f), new Vector3(0, 2.6f, 7.5f), new Vector3(0, 2.6f, 2.4f), new Vector3(-1, 7, 4));
            k.Box(WorldMaterials.Glow(new Color(.6f, .8f, 1f), 1.4f), new Vector3(0, 1f, -5.51f), new Vector3(2.4f, .5f, .02f), Quaternion.identity);
            var ship = Mover(z, "Sailing ship", k, c + up * 2.4f, Quaternion.LookRotation(z.side));
            life.ship = ship; life.shipHome = ship.position;
        }

        // ---------------------------------------------------------------- Survivor
        void Ring(Plot z)
        {
            var up = Vector3.up; var c = z.At(-2f, 0); var rot = Quaternion.LookRotation(z.front);
            z.mk.Box(WorldMaterials.Solid(new Color(.85f, .85f, .88f)), c + up * .8f, new Vector3(9f, 1.6f, 9f), rot); z.solid.Box(dark, c + up * .8f, new Vector3(9f, 1.6f, 9f), rot);
            var ropeM = WorldMaterials.Solid(new Color(.8f, .15f, .15f));
            foreach (var s in new[] { -1f, 1f }) foreach (var b in new[] { -1f, 1f }) { z.mk.Box(dark, c + z.side * s * 4.3f + z.front * b * 4.3f + up * 2.4f, new Vector3(.3f, 1.8f, .3f), rot); ArtistBits.PostCollider(z.root, c + z.side * s * 4.3f + z.front * b * 4.3f + up * 1.6f, 1.8f, .2f); }
            for (var y = 0; y < 3; y++) foreach (var s in new[] { -1f, 1f }) { z.mk.Box(ropeM, c + up * (2f + y * .45f) + z.side * s * 4.3f, new Vector3(.06f, .06f, 8.6f), rot); z.mk.Box(ropeM, c + up * (2f + y * .45f) + z.front * s * 4.3f, new Vector3(8.6f, .06f, .06f), rot); }
            // the tiger's eye on a frame behind the ring
            var eye = c - z.front * 7f + up * 9f;
            z.mk.Box(dark, eye - up * 4.5f, new Vector3(.4f, 9f, .4f), rot); ArtistBits.PostCollider(z.root, eye - up * 9f, 9f, .3f);
            var iris = new Material(GlowM(new Color(1f, .7f, .1f), .8f)) { name = "Tiger eye" };
            z.mk.Blob(WorldMaterials.Solid(new Color(.1f, .08f, .05f)), eye, new Vector3(4.4f, 2.4f, .4f), z.ctx.Rng, 0f);
            z.mk.Blob(iris, eye + z.front * .25f, new Vector3(3.6f, 2f, .2f), z.ctx.Rng, 0f);
            z.mk.Box(dark, eye + z.front * .45f, new Vector3(.5f, 2f, .05f), rot);
            life.tigerEye = iris; life.ring = c + up * 1.6f;
            var spot = new GameObject("Ring spotlight").AddComponent<Light>(); spot.transform.SetParent(z.root, false);
            spot.type = LightType.Spot; spot.spotAngle = 45f; spot.range = 26f; spot.intensity = .4f; spot.color = Spot;
            spot.transform.SetPositionAndRotation(c + up * 16f, Quaternion.LookRotation(Vector3.down)); life.ringSpot = spot;
        }

        void Checkered(Plot z)
        {
            var up = Vector3.up; var c = z.At(-3f, 0); var rot = Quaternion.LookRotation(z.front);
            var b = WorldMaterials.Solid(new Color(.08f, .08f, .08f), .7f); var w = WorldMaterials.Solid(new Color(.95f, .95f, .95f), .7f);
            for (var x = 0; x < 5; x++) for (var y = 0; y < 6; y++)
            {
                var p = c + up * (1.2f + y * .9f) + z.side * ((x - 2f) * .9f);
                var r = Mathf.Abs(x - 2f) * .9f; var yy = y * .9f - 2.4f; if (r * r / 6f + yy * yy / 9f > 1f) continue;
                z.mk.Box((x + y) % 2 == 0 ? b : w, p, new Vector3(.9f, .9f, .6f), rot);
            }
            z.mk.Box(WorldMaterials.Solid(new Color(.3f, .2f, .12f)), c + up * 9f, new Vector3(.5f, 6f, .4f), rot);
            z.mk.Box(b, c + up * 12.4f, new Vector3(.9f, 1.2f, .4f), rot);
            z.solid.Box(b, c + up * 3.5f, new Vector3(4.4f, 6f, 1f), rot);
            halos.Add(c + up * 4f + z.front * 3f, 3f, Spot, .35f);
        }

        void HeartFish(Plot z)
        {
            var up = Vector3.up; var c = z.At(-3f, 0); var red = SongGlow(Barracuda, new Color(1f, .15f, .3f), 2.4f);
            for (var k = 0; k < 24; k++)
            {
                float a0 = k / 24f * Mathf.PI * 2f, a1 = (k + 1) / 24f * Mathf.PI * 2f;
                Vector3 P(float a) => c + up * 7f + z.side * (16f * Mathf.Pow(Mathf.Sin(a), 3f) * .22f) + up * ((13f * Mathf.Cos(a) - 5f * Mathf.Cos(2f * a) - 2f * Mathf.Cos(3f * a) - Mathf.Cos(4f * a)) * .22f);
                z.mk.Box(red, (P(a0) + P(a1)) / 2f, new Vector3(.25f, .25f, Vector3.Distance(P(a0), P(a1)) + .05f), Quaternion.LookRotation(P(a1) - P(a0), z.front));
            }
            z.mk.Box(dark, c + up * 2.5f, new Vector3(.3f, 5f, .3f), Quaternion.LookRotation(z.front)); ArtistBits.PostCollider(z.root, c, 5f, .25f);
            halos.Add(c + up * 7f + z.front, 4f, new Color(1f, .2f, .35f), .5f);
            var fish = WorldMaterials.Solid(new Color(.6f, .65f, .7f), .9f); var f = z.At(4f, 6f) + up * 1.6f; var fr = Quaternion.LookRotation(z.side);
            z.mk.Blob(fish, f, new Vector3(.6f, .7f, 3.5f), z.ctx.Rng, 0f);
            z.mk.Tri(fish, f + z.side * -3.2f, f + z.side * -4.4f + up * 1f, f + z.side * -4.4f - up * 1f, f + z.front);
            z.mk.Tri(fish, f + z.side * -3.2f, f + z.side * -4.4f - up * 1f, f + z.side * -4.4f + up * 1f, f - z.front);
            z.mk.Box(dark, f - up * 1.1f, new Vector3(.2f, 1.2f, .2f), fr); z.solid.Box(dark, f, new Vector3(1.4f, 2.4f, 7f), fr);
        }

        void Tornado(Plot z)
        {
            // a warm dusty funnel ("Dust in the Wind"), its pale bands lit from within: in the same blue-grey as the
            // stadium stands it merged into the wall behind it at night and looked like it cut into it
            var up = Vector3.up; var c = z.At(-3f, 0); var k = new MeshKit(); var grey = WorldMaterials.Solid(new Color(.62f, .55f, .44f)); var pale = GlowM(new Color(1f, .85f, .6f), .9f);
            // a smooth funnel of many thin twisted bands, debris whirling round it
            for (var i = 0; i < 30; i++) { var y = i * .5f; var r = .4f + y * y * .028f; k.Prism(i % 3 == 0 ? pale : grey, new Vector3(Mathf.Sin(y * .45f) * .7f, y, Mathf.Cos(y * .45f) * .7f), Vector3.up, r, .52f, 18, 1.04f, i * 13f); }
            for (var i = 0; i < 14; i++) { var a = i * 2.4f; var y = 1f + i * .9f; var r = 1.4f + y * y * .03f; k.Box(i % 2 == 0 ? wood : dark, new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r), new Vector3(.6f, .12f, .25f), Quaternion.Euler(i * 40f, a * 57f, i * 25f)); }
            life.tornado = Mover(z, "Tornado", k, c, Quaternion.identity);
            z.solid.Prism(grey, c, up, 1.5f, 6f, 8);
            var sign = z.At(4f, 6f); z.mk.Prism(wood, sign, up, .08f, 2.4f, 6); z.mk.Box(wood, sign + up * 2.2f - z.side * .6f, new Vector3(.05f, .45f, 1.6f), Quaternion.LookRotation(z.side));
            ArtistBits.Label(z.labels, "WAYWARD", sign + up * 2.2f - z.side * .6f + z.front * .04f, z.front, .16f, Ink, TextAnchor.MiddleCenter);
        }

        void HotRod(Plot z)
        {
            var up = Vector3.up; var at = z.At(-2f, 0); var dir = z.side; var rot = Quaternion.LookRotation(dir); var side = Vector3.Cross(up, dir);
            var body = WorldMaterials.Solid(new Color(.1f, .1f, .12f), .9f); var chrome = metal;
            z.mk.Box(body, at + up * .9f, new Vector3(1.8f, .7f, 4.4f), rot); z.mk.Box(body, at + up * 1.5f - dir * .6f, new Vector3(1.7f, .7f, 1.6f), rot);
            z.mk.Box(chrome, at + up * 1.4f + dir * 1.4f, new Vector3(.8f, .5f, 1.2f), rot);
            foreach (var s in new[] { -1f, 1f }) { foreach (var zz in new[] { -1.5f, 1.6f }) z.mk.Prism(dark, at + side * s * 1f + dir * zz + up * .55f, side * s, zz < 0 ? .6f : .45f, .4f, 14); for (var k = 0; k < 4; k++) z.mk.Tri(GlowM(new Color(1f, .5f, .1f), 2f), at + side * s * .91f + up * .8f + dir * (1.8f - k * .5f), at + side * s * .91f + up * 1.2f + dir * (1.6f - k * .5f), at + side * s * .91f + up * .9f + dir * (.9f - k * .5f), at - side * s); }
            z.solid.Box(body, at + up * 1f, new Vector3(2f, 2f, 4.6f), rot);
        }

        void Speedwagon(Plot z)
        {
            var up = Vector3.up; var at = z.At(-2f, 0); var dir = z.side; var rot = Quaternion.LookRotation(dir); var side = Vector3.Cross(up, dir);
            var red = WorldMaterials.Solid(new Color(.7f, .12f, .1f), .6f);
            z.mk.Box(red, at + up * 1.2f + dir * 1.5f, new Vector3(1.6f, 1.2f, 1.6f), rot); z.mk.Box(dark, at + up * 2.2f + dir * 1.5f, new Vector3(1.7f, .1f, 1.7f), rot);
            z.mk.Box(wood, at + up * 1f - dir * 1f, new Vector3(2f, .6f, 3.4f), rot);
            foreach (var s in new[] { -1f, 1f }) foreach (var zz in new[] { -2f, 1.8f }) { z.mk.Prism(dark, at + side * s * 1f + dir * zz + up * .6f, side * s, .6f, .15f, 12); for (var k = 0; k < 6; k++) z.mk.Box(wood, at + side * s * 1.08f + dir * zz + up * .6f, new Vector3(.02f, 1.1f, .06f), Quaternion.LookRotation(dir) * Quaternion.Euler(k * 30f, 0, 0)); }
            z.mk.Prism(GlowM(new Color(1f, .9f, .6f), 2f), at + up * 1.4f + dir * 2.35f + side * .55f, dir, .18f, .1f, 10);
            z.solid.Box(red, at + up * 1.1f, new Vector3(2.1f, 2.2f, 5f), rot);
        }

        void Lab(Plot z)
        {
            var up = Vector3.up; var c = z.At(-3f, 0); var rot = Quaternion.LookRotation(z.front);
            z.mk.Prism(stone, c, up, 2.4f, 9f, 8, .8f); z.solid.Prism(stone, c, up, 2.4f, 9f, 8);
            z.mk.Prism(dark, c + up * 9f, up, 2.2f, .8f, 8); z.mk.Box(GlowM(new Color(.6f, 1f, .5f), 2f), c + up * 4f + z.front * 2.2f, new Vector3(.8f, 1.2f, .05f), rot);
            for (var k = 0; k < 2; k++)
            {
                var coil = c + z.side * (k == 0 ? -5f : 5f);
                z.mk.Prism(metal, coil, up, .3f, 4f, 8); for (var r = 0; r < 6; r++) z.mk.Prism(WorldMaterials.Solid(new Color(.75f, .45f, .2f), .8f), coil + up * (.5f + r * .55f), up, .5f, .3f, 12);
                z.mk.Blob(metal, coil + up * 4.6f, Vector3.one * .8f, z.ctx.Rng, 0f);
                life.coils.Add(coil + up * 4.6f); ArtistBits.PostCollider(z.root, coil, 4.6f, .5f);
            }
            halos.Add(c + up * 5f + z.front * 3f, 3f, new Color(.6f, 1f, .5f), .4f);
        }

        void Mic(Plot z)
        {
            var up = Vector3.up; var c = z.At(-2f, 0);
            z.mk.Prism(metal, c, up, .12f, 5f, 8); z.mk.Blob(metal, c + up * 5.4f, new Vector3(.6f, .8f, .6f), z.ctx.Rng, 0f);
            ArtistBits.PostCollider(z.root, c, 5f, .2f);
        }

        /// Starship: We Built This City, a little night skyline of lit windows, a radio mast with a blinking light on top.
        void CityNight(Plot z)
        {
            var up = Vector3.up; var c = z.At(-4f, 0); var rot = Quaternion.LookRotation(z.front); var wall = WorldMaterials.Solid(new Color(.2f, .2f, .28f));
            for (var k = 0; k < 8; k++)
            {
                var h = 4f + (k * 37 % 7) * 1.2f; var p = c + z.side * ((k - 3.5f) * 1.9f) - z.front * (k % 2) * 1f;
                z.mk.Box(wall, p + up * h / 2f, new Vector3(1.7f, h, 1.7f), rot);
                for (var w = 1; w < h - .5f; w += 1) foreach (var x in new[] { -.4f, .4f }) if ((k * 7 + w * 3 + (x > 0 ? 1 : 0)) % 4 != 0) z.mk.Box(GlowM(new Color(1f, .85f, .55f), 1.8f), p + up * w + z.side * x + z.front * .86f, new Vector3(.45f, .35f, .02f), rot);
            }
            var mast = c + z.side * 1f + up * 12f; z.mk.Prism(metal, c + z.side * 1f + up * 9f, up, .1f, 4f, 6); z.mk.Blob(GlowM(new Color(1f, .15f, .1f), 2.6f), mast + up * 1.1f, Vector3.one * .25f, z.ctx.Rng, 0f);
            z.solid.Box(dark, c + up * 5f, new Vector3(15f, 10f, 3f), rot);
            ArtistBits.PointLight(z.root, c + up * 4f + z.front * 5f, new Color(1f, .8f, .55f), 16f, 1.4f);
        }

        /// U2: Sunday Bloody Sunday, a low stage with a big white flag on a tall pole and a marching drum, under a red glow.
        void WhiteFlag(Plot z)
        {
            var up = Vector3.up; var c = z.At(-4f, 0); var rot = Quaternion.LookRotation(z.front);
            z.mk.Box(dark, c + up * .5f, new Vector3(9f, 1f, 5f), rot); z.solid.Box(dark, c + up * .5f, new Vector3(9f, 1f, 5f), rot);
            var pole = c + z.side * 2.5f + up * 1f; z.mk.Prism(metal, pole, up, .08f, 6.2f, 8); ArtistBits.PostCollider(z.root, pole - up * 1f, 7f, .15f);
            for (var k = 0; k < 6; k++) { var t = k / 6f; z.mk.Box(white, pole + up * 4.6f + z.side * (.5f + t * 3.4f) + up * Mathf.Sin(t * 4f) * .3f, new Vector3(.6f, 2.4f, .05f), rot * Quaternion.Euler(0, Mathf.Sin(t * 5f) * 25f, 0)); }
            var drum = c + up * 1.6f - z.side * 1.5f; z.mk.Prism(WorldMaterials.Solid(new Color(.85f, .85f, .82f)), drum - z.side * .5f, z.side, .65f, 1f, 16); z.mk.Prism(WorldMaterials.Solid(new Color(.6f, .1f, .1f)), drum - z.side * .55f, z.side, .67f, .15f, 16);
            ArtistBits.PointLight(z.root, c + up * 6f + z.front * 3f, new Color(1f, .25f, .2f), 18f, 2f); halos.Add(c + up * 5f, 6f, new Color(1f, .25f, .2f), .3f);
        }
    }
}
