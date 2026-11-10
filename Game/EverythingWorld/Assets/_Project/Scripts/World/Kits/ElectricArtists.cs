using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Electric Fields' stages and tents. Each artist has a plaque by the walkway, their covers on a board
    /// and a light pole; everything is built big enough to read from across a field.
    /// - MGMT: the Big Top, a huge striped tent with a mirrored disco ball bursting through the roof, a stage of
    ///   synths and lights inside; outside a carousel ("Congratulations") and a neon eel arching over the way in
    ///   ("Electric Feel").
    /// - Santigold: the main stage, black and yellow, an LED wall, speaker towers, a lighting truss and flame
    ///   cannons that fire in "Disparate Youth" (ElectricLife).
    /// - LCD Soundsystem: a silver dome stage with a huge mirror ball, dim and waiting until one of their songs
    ///   plays, when it comes fully alive; a ring of camp chairs outside ("All My Friends").
    /// - Empire of the Sun: a great golden sunburst on a hill ("Walking On A Dream"). Phoenix: a bandstand with a
    ///   piano and a chandelier ("Lisztomania"). Passion Pit: a chill-out tent with hammocks ("Sleepyhead"). Two
    ///   Door Cinema Club: an outdoor cinema with two doors standing in the field. Django Django: a comet on poles
    ///   ("Hail Bop"). Imogen Heap: a microphone in a ring of light ("Headlock").
    public partial class ElectricKit
    {
        class Spot
        {
            public LandContext ctx; public Transform root, labels; public MeshKit mk, solid, pictures;
            public IslandWorldData.Artist a; public Vector3 c, front, side; public List<Renderer> glows; public ArtistArea area;
            public Vector3 At(float fw, float rt) { var p = c + front * fw + side * rt; return ctx.OnGround(p.x, p.z); }
        }

        public override void BuildArtist(LandContext ctx, IslandWorldData.Artist a)
        {
            if (wood == null) MakeMaterials();
            var root = new GameObject("Artist: " + a.name).transform; root.SetParent(ctx.Root, false);
            var front = facing.TryGetValue(a.name, out var f) ? f : new Vector3(center.x - a.x, 0, center.z - a.z).normalized;
            if (front.sqrMagnitude < .01f) front = Vector3.forward;
            var labels = new GameObject("Labels").transform; labels.SetParent(root, false);
            var area = root.gameObject.AddComponent<ArtistArea>();
            area.artistName = a.name; area.stationId = a.stationId; area.radius = a.r; area.glowColor = Sunset;
            var z = new Spot { ctx = ctx, root = root, labels = labels, mk = new MeshKit(), solid = new MeshKit(), pictures = new MeshKit(), a = a, c = ctx.OnGround(a.x, a.z), front = front, side = Vector3.Cross(Vector3.up, front), glows = new List<Renderer>(), area = area };
            switch (a.name)
            {
                case "MGMT": BigTop(z); break;
                case "Santigold": MainStage(z); break;
                case "LCD Soundsystem": Dome(z); break;
                case "Empire Of The Sun": Sunburst(z); break;
                case "Phoenix": Bandstand(z); break;
                case "Passion Pit": ChillTent(z); break;
                case "Two Door Cinema Club": Cinema(z); break;
                case "Django Django": Comet(z); break;
                case "Imogen Heap": MicRing(z); break;
                default: SmallStage(z); break;
            }
            var reach = Foot(a);
            Board(z, z.At(reach * .8f, -reach * .55f - 2f), front);
            ArtistBits.Plaque(z.mk, z.solid, labels, a, z.At(reach * .8f + 1.5f, reach * .55f + 2f), front, wood, dark, Ink, 1.6f);
            LightPole(ctx, z.mk, z.At(reach * .8f, reach * .55f + 4.5f), Neon[Mathf.Abs(a.name.GetHashCode()) % Neon.Length]);
            if (z.area.glowLight == null) z.area.glowLight = ArtistBits.PointLight(root, z.c + Vector3.up * 5f, Sunset, HasStation(a) ? 24f : 14f, HasStation(a) ? 1.6f : 1.1f);
            area.glowRenderers = z.glows.ToArray();
            z.mk.Build("Area", root, false);
            z.solid.BuildCollider("Walls", root);
            z.pictures.Build("Covers", root, false);
            noGrass.Add(new Vector3(z.c.x, reach * .9f, z.c.z));
        }

        void Board(Spot z, Vector3 ground, Vector3 dir)
        {
            dir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized;
            var rot = Quaternion.LookRotation(dir); var side = Vector3.Cross(Vector3.up, dir);
            var n = Mathf.Max(1, Mathf.Min(4, z.a.songList?.Length ?? 1)); const float size = .9f;
            var w = n * size * 1.25f + .4f;
            foreach (var s in new[] { -1f, 1f }) z.mk.Prism(metal, ground + side * s * (w / 2f - .1f) - dir * .05f, Vector3.up, .06f, 2.4f, 6);
            var c = ground + Vector3.up * 1.8f;
            z.mk.Box(dark, c - Vector3.up * .1f, new Vector3(w, size + .6f, .1f), rot);
            z.solid.Box(dark, c - Vector3.up * .1f, new Vector3(w, size + .6f, .2f), rot);
            ArtistBits.CoverWall(z.pictures, z.labels, z.ctx, z.a, c + dir * .055f + Vector3.up * .08f, dir, n, size, n, true, Ink);
        }

        // ---------------------------------------------------------------- MGMT: the Big Top
        void BigTop(Spot z)
        {
            var ctx = z.ctx; var up = Vector3.up; var c = z.c;
            const float R = 17f, wallH = 5f, peak = 15f; const int n = 24;
            Color[] stripes = { new Color(.92f, .3f, .38f), new Color(.98f, .95f, .88f) };
            Vector3 Rim(int k, float r, float y) { var a = k * Mathf.PI * 2f / n; return c + new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r); }
            var door = Mathf.RoundToInt(Mathf.Repeat(Mathf.Atan2(z.front.z, z.front.x) / (Mathf.PI * 2f) * n, n));
            for (var k = 0; k < n; k++)
            {
                var m = WorldMaterials.Solid(stripes[k % 2]);
                var isDoor = Mathf.Abs(Mathf.DeltaAngle(k * 360f / n, door * 360f / n)) < 20f;
                // the wall (open where the door is), the roof sloping up to the peak, in stripes
                if (!isDoor)
                {
                    z.mk.Quad(m, Rim(k, R, 0), Rim(k + 1, R, 0), Rim(k + 1, R, wallH), Rim(k, R, wallH), c + up * 2f);
                    z.mk.Quad(m, Rim(k, R, 0), Rim(k + 1, R, 0), Rim(k + 1, R, wallH), Rim(k, R, wallH), c + up * 2f + (Rim(k, R, 0) - c) * 2f);
                    var mid = (Rim(k, R, 0) + Rim(k + 1, R, 0)) / 2f;
                    z.solid.Box(canvas, mid + up * wallH / 2f, new Vector3(.3f, wallH, Vector3.Distance(Rim(k, R, 0), Rim(k + 1, R, 0)) + .2f), Quaternion.LookRotation(Rim(k + 1, R, 0) - Rim(k, R, 0)));
                }
                // the roof: one sloped band from the eaves up to a ring round the disco ball (seen from both sides)
                z.mk.Quad(m, Rim(k, R + .8f, wallH - .3f), Rim(k + 1, R + .8f, wallH - .3f), Rim(k + 1, 3.2f, peak - 1f), Rim(k, 3.2f, peak - 1f), c);
                z.mk.Quad(m, Rim(k, R + .8f, wallH - .3f), Rim(k + 1, R + .8f, wallH - .3f), Rim(k + 1, 3.2f, peak - 1f), Rim(k, 3.2f, peak - 1f), c + up * 40f);
                // a pennant on the rim
                if (k % 2 == 0)
                {
                    var p = Rim(k, R + .9f, wallH - .2f);
                    z.mk.Prism(metal, p, up, .04f, 2.2f, 5);
                    z.mk.Tri(WorldMaterials.Solid(Bunting[k % Bunting.Length]), p + up * 2.2f, p + up * 1.6f, p + up * 1.9f + (Rim(k + 1, R + .9f, 0) - Rim(k, R + .9f, 0)).normalized * 1f, p + (p - c));
                    z.mk.Tri(WorldMaterials.Solid(Bunting[k % Bunting.Length]), p + up * 2.2f, p + up * 1.6f, p + up * 1.9f + (Rim(k + 1, R + .9f, 0) - Rim(k, R + .9f, 0)).normalized * 1f, p - (p - c));
                }
            }
            // the king pole and the disco ball bursting through the roof
            z.mk.Prism(metal, c, up, .35f, peak, 8);
            ArtistBits.PostCollider(z.root, c, peak, .4f);
            var ball = new GameObject("Disco ball").transform; ball.SetParent(z.root, false);
            var bk = new MeshKit();
            var mirror = new Material(GlowM(new Color(.85f, .9f, 1f), 1.4f)) { name = "Mirror tiles" };
            mirror.SetFloat("_Smoothness", 1f);
            const float BR = 3.6f;
            for (var i = 1; i < 10; i++)
            {
                var lat = Mathf.PI * i / 10f - Mathf.PI / 2f; var ring = Mathf.Max(6, Mathf.RoundToInt(Mathf.Cos(lat) * 22f));
                for (var j = 0; j < ring; j++)
                {
                    var lon = j * Mathf.PI * 2f / ring;
                    var nrm = new Vector3(Mathf.Cos(lat) * Mathf.Cos(lon), Mathf.Sin(lat), Mathf.Cos(lat) * Mathf.Sin(lon));
                    bk.Box((i + j) % 5 == 0 ? GlowM(Neon[(i + j) % Neon.Length], 2.6f) : mirror, nrm * BR, new Vector3(.55f, .55f, .06f), Quaternion.LookRotation(nrm));
                }
            }
            bk.Blob(dark, Vector3.zero, Vector3.one * (BR - .1f), ctx.Rng, 0f);
            bk.Build("Ball", ball, false);
            ball.position = c + up * (peak + 1.2f);
            life.discoBall = ball; life.mirrorTiles = mirror;
            halos.Add(c + up * (peak + 1.2f) + z.front * 5f, 6f, Electric, .8f);
            // the stage inside, at the back: synths on stands, a light rig
            var stage = c - z.front * (R - 6f);
            z.mk.Box(dark, stage + up * .5f, new Vector3(12f, 1f, 6f), Quaternion.LookRotation(z.front));
            z.solid.Box(dark, stage + up * .5f, new Vector3(12f, 1f, 6f), Quaternion.LookRotation(z.front));
            for (var k = 0; k < 4; k++)
            {
                var s = stage + z.side * ((k - 1.5f) * 2.6f) + up * 1f;
                z.mk.Box(dark, s + up * .9f, new Vector3(1.4f, .12f, .5f), Quaternion.LookRotation(z.front) * Quaternion.Euler(-12f, 0, 0));
                for (var key = 0; key < 8; key++) z.mk.Box(GlowM(Neon[(k + key) % Neon.Length], 2.4f), s + up * .97f + z.side * ((key - 3.5f) * .15f) + z.front * .1f, new Vector3(.1f, .03f, .2f), Quaternion.LookRotation(z.front));
                z.mk.Prism(metal, s, up, .04f, .9f, 5);
            }
            for (var k = 0; k < 7; k++)
            {
                var l = stage + z.side * ((k - 3f) * 1.8f) + up * 7f + z.front * 2f;
                z.mk.Box(dark, l, new Vector3(.4f, .4f, .5f), Quaternion.LookRotation(z.front - up * .6f));
                z.mk.Prism(GlowM(Neon[k % Neon.Length], 3f), l + z.front * .25f - up * .15f, (z.front - up * .6f).normalized, .15f, .05f, 8);
            }
            z.mk.Box(metal, stage + up * 7.2f + z.front * 2f, new Vector3(13f, .25f, .25f), Quaternion.LookRotation(z.front));
            for (var k = 0; k < 4; k++) ArtistBits.PointLight(z.root, stage + up * 6f + z.front * (3f + k * 2.5f) + z.side * ((k % 2) * 6f - 3f), Neon[k % Neon.Length], 16f, 1.4f);
            z.area.glowLight = ArtistBits.PointLight(z.root, c + up * 9f, new Color(1f, .7f, .5f), 30f, 1.4f);
            // Congratulations: a carousel beside the tent
            Carousel(z, z.At(R * .5f, R + 9f));
            // Electric Feel: a neon eel arching over the way in
            var gate = z.At(R + 5f, 0);
            var eel = new Material(GlowM(Electric, 2.6f)) { name = "Electric eel" };
            life.eel = eel;
            Vector3 E(float t) => gate + z.side * Mathf.Lerp(-5f, 5f, t) + up * (Mathf.Sin(t * Mathf.PI) * 6f + .2f) + z.front * Mathf.Sin(t * Mathf.PI * 4f) * .6f;
            for (var k = 0; k < 30; k++)
            {
                var a0 = E(k / 30f); var a1 = E((k + 1) / 30f);
                z.mk.Prism(eel, a0, (a1 - a0).normalized, Mathf.Lerp(.32f, .12f, Mathf.Abs(k / 30f - .35f) * 1.6f), Vector3.Distance(a0, a1) + .05f, 7);
            }
            z.mk.Blob(GlowM(Lime, 3f), E(.33f) + up * .25f + z.front * .3f, Vector3.one * .12f, ctx.Rng, 0f);
            halos.Add(E(.5f) + z.front * 1.5f, 3f, Electric, .9f);
            foreach (var s in new[] { 0f, 1f }) ArtistBits.PostCollider(z.root, E(s), 1f, .3f);
        }

        void Carousel(Spot z, Vector3 at)
        {
            var ctx = z.ctx; var up = Vector3.up;
            var go = new GameObject("Carousel"); go.transform.SetParent(z.root, false);
            var k = new MeshKit();
            const float R = 5f;
            k.Prism(WorldMaterials.Solid(new Color(.95f, .85f, .7f)), Vector3.zero, up, R, .5f, 20);
            k.Prism(WorldMaterials.Solid(new Color(.85f, .35f, .45f)), up * .5f, up, .5f, 4f, 10);
            k.Cone(WorldMaterials.Solid(new Color(.92f, .3f, .38f)), up * 4.5f, R + .3f, 2.4f, 20);
            for (var i = 0; i < 20; i++)
            {
                var a = i * Mathf.PI * 2f / 20f; var p = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (R + .2f) + up * 4.45f;
                k.Blob(GlowM(i % 2 == 0 ? new Color(1f, .85f, .5f) : Hot, 2.6f), p, Vector3.one * .1f, ctx.Rng, 0f);
            }
            for (var i = 0; i < 8; i++)
            {
                var a = i * Mathf.PI * 2f / 8f; var p = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 3.6f;
                k.Prism(WorldMaterials.Solid(new Color(.9f, .8f, .4f), .8f), p + up * .5f, up, .05f, 4f, 6);
                var horse = p + up * (1.4f + (i % 2) * .4f);
                k.Blob(WorldMaterials.Solid(Bunting[i % Bunting.Length]), horse, new Vector3(.25f, .3f, .7f), ctx.Rng, .1f);
                k.Blob(WorldMaterials.Solid(Bunting[i % Bunting.Length]), horse + up * .35f + new Vector3(-Mathf.Sin(a), 0, Mathf.Cos(a)) * .55f, new Vector3(.15f, .22f, .25f), ctx.Rng, .1f);
            }
            k.Build("Carousel", go.transform, false);
            go.transform.position = at;
            life.carousel = go.transform;
            halos.Add(at + up * 4.6f + (z.c - at).normalized * -1f, 3f, new Color(1f, .8f, .5f), .8f);
            ArtistBits.PointLight(z.root, at + up * 3.5f, new Color(1f, .78f, .5f), 12f, 1.4f);
            ArtistBits.PostCollider(z.root, at, 4f, R);
        }

        // ---------------------------------------------------------------- Santigold: the main stage
        void MainStage(Spot z)
        {
            var ctx = z.ctx; var up = Vector3.up; var rot = Quaternion.LookRotation(z.front);
            var c = z.At(-6f, 0); var yellow = WorldMaterials.Solid(new Color(1f, .82f, .15f));
            const float W = 22f, D = 10f;
            z.mk.Box(dark, c + up * .8f, new Vector3(W, 1.6f, D), rot);
            z.solid.Box(dark, c + up * .8f, new Vector3(W, 1.6f, D), rot);
            for (var k = 0; k < 11; k++) z.mk.Box(k % 2 == 0 ? yellow : dark, c + up * .8f + z.front * (D / 2f + .01f) + z.side * ((k - 5f) * 2f), new Vector3(2f, 1.6f, .02f), rot);
            // the roof on its towers, the LED wall at the back
            foreach (var s in new[] { -1f, 1f })
                foreach (var d in new[] { -1f, 1f })
                {
                    var p = c + z.side * s * (W / 2f) + z.front * d * (D / 2f);
                    z.mk.Box(metal, p + up * 6f, new Vector3(.5f, 12f, .5f), rot);
                    ArtistBits.PostCollider(z.root, p, 12f, .3f);
                }
            z.mk.Box(dark, c + up * 12.4f, new Vector3(W + 1f, .8f, D + 1f), rot);
            var led = new Material(GlowM(new Color(1f, .82f, .2f), 2f)) { name = "LED wall" };
            life.ledWall = led;
            for (var x = 0; x < 9; x++)
                for (var y = 0; y < 4; y++)
                    z.mk.Box((x + y) % 3 == 0 ? led : (x + y) % 3 == 1 ? GlowM(Hot, 2f) : GlowM(Electric, 2f), c - z.front * (D / 2f - .3f) + z.side * ((x - 4f) * 2.1f) + up * (3f + y * 2.1f), new Vector3(2f, 2f, .1f), rot);
            z.solid.Box(dark, c - z.front * (D / 2f - .2f) + up * 6.5f, new Vector3(W, 10f, .4f), rot);
            // speaker towers either side, lights on the truss
            foreach (var s in new[] { -1f, 1f })
            {
                var sp = c + z.side * s * (W / 2f + 2.5f) + z.front * (D / 2f - 1f);
                for (var k = 0; k < 4; k++) { z.mk.Box(dark, sp + up * (1f + k * 2f), new Vector3(2.2f, 1.9f, 1.6f), rot); z.mk.Prism(WorldMaterials.Solid(new Color(.2f, .2f, .22f)), sp + up * (1f + k * 2f) + z.front * .81f, z.front, .6f, .02f, 14); }
                z.solid.Box(dark, sp + up * 4f, new Vector3(2.2f, 8f, 1.6f), rot);
                life.flames.Add(sp + up * 8.2f);
            }
            for (var k = 0; k < 10; k++)
            {
                var l = c + z.side * ((k - 4.5f) * 2.2f) + up * 11.7f + z.front * (D / 2f - .5f);
                z.mk.Prism(GlowM(Neon[k % Neon.Length], 3f), l, (z.front - up * .8f).normalized, .2f, .3f, 8);
            }
            life.mainStage = c + up * 11f + z.front * (D / 2f);
            life.mainStageFront = z.front;
            for (var k = 0; k < 3; k++) ArtistBits.PointLight(z.root, c + up * 8f + z.front * (D / 2f + 4f) + z.side * ((k - 1) * 7f), Neon[k], 22f, 1.6f);
            halos.Add(c + up * 7f + z.front * (D / 2f + 3f), 6f, new Color(1f, .82f, .3f), .7f);
            // Run the Road: a bold painted road running up to the crowd barrier
            var road = WorldMaterials.Solid(new Color(.18f, .18f, .2f));
            for (var d = D / 2f + 2f; d < D / 2f + 16f; d += 2f)
            {
                var p = c + z.front * d; p = ctx.OnGround(p.x, p.z);
                z.mk.Box(road, p + up * .04f, new Vector3(4f, .06f, 2.02f), rot);
                if ((int)d % 4 == 0) z.mk.Box(yellow, p + up * .08f, new Vector3(.25f, .02f, 1.2f), rot);
            }
            var barrier = c + z.front * (D / 2f + 2.5f);
            for (var k = 0; k < 9; k++) { var b = barrier + z.side * ((k - 4f) * 2.2f); b = ctx.OnGround(b.x, b.z); z.mk.Box(metal, b + up * .6f, new Vector3(2.1f, 1.2f, .1f), rot); }
            z.solid.Box(metal, barrier + up * .6f, new Vector3(20f, 1.2f, .3f), rot);
        }

        // ---------------------------------------------------------------- LCD Soundsystem: the dome
        void Dome(Spot z)
        {
            var ctx = z.ctx; var up = Vector3.up; var c = z.c;
            const float R = 11f; const int n = 16;
            var strut = WorldMaterials.Solid(new Color(.8f, .82f, .86f), .7f);
            // a geodesic-ish dome: rings of struts, the front third open as the stage mouth
            Vector3 P(int ring, int k) { var lat = ring * Mathf.PI / 2f / 4f; var lon = k * Mathf.PI * 2f / n; return c + new Vector3(Mathf.Cos(lat) * Mathf.Cos(lon) * R, Mathf.Sin(lat) * R, Mathf.Cos(lat) * Mathf.Sin(lon) * R); }
            var mouth = Mathf.Atan2(z.front.z, z.front.x);
            for (var ring = 0; ring < 4; ring++)
                for (var k = 0; k < n; k++)
                {
                    var lon = k * Mathf.PI * 2f / n;
                    var open = Mathf.Abs(Mathf.DeltaAngle(lon * Mathf.Rad2Deg, mouth * Mathf.Rad2Deg)) < 50f && ring < 2;
                    void S(Vector3 a, Vector3 b) { z.mk.Box(strut, (a + b) / 2f, new Vector3(.12f, .12f, Vector3.Distance(a, b) + .1f), Quaternion.LookRotation(b - a)); }
                    S(P(ring, k), P(ring, k + 1));
                    if (!open) { S(P(ring, k), P(ring + 1, k)); S(P(ring, k), P(ring + 1, k + 1)); }
                    if (!open && ring < 3) z.mk.Quad(WorldMaterials.Solid(new Color(.7f, .72f, .78f)), P(ring, k), P(ring, k + 1), P(ring + 1, k + 1), P(ring + 1, k), c);
                }
            for (var k = 0; k < n; k++)
            {
                var lon = k * Mathf.PI * 2f / n;
                if (Mathf.Abs(Mathf.DeltaAngle(lon * Mathf.Rad2Deg, mouth * Mathf.Rad2Deg)) < 50f) continue;
                var a = P(0, k); var b = P(0, k + 1);
                z.solid.Box(strut, (a + b) / 2f + up * 2f, new Vector3(.4f, 4f, Vector3.Distance(a, b) + .3f), Quaternion.LookRotation(b - a));
            }
            // the stage and the great mirror ball
            var stage = c - z.front * 4f;
            z.mk.Box(dark, stage + up * .4f, new Vector3(9f, .8f, 4f), Quaternion.LookRotation(z.front));
            z.solid.Box(dark, stage + up * .4f, new Vector3(9f, .8f, 4f), Quaternion.LookRotation(z.front));
            var ball = new GameObject("LCD mirror ball").transform; ball.SetParent(z.root, false);
            var bk = new MeshKit();
            var tiles = new Material(GlowM(new Color(.9f, .92f, 1f), .5f)) { name = "LCD mirror" };
            tiles.SetFloat("_Smoothness", 1f);
            const float BR = 1.8f;
            for (var i = 1; i < 8; i++)
            {
                var lat = Mathf.PI * i / 8f - Mathf.PI / 2f; var ringN = Mathf.Max(5, Mathf.RoundToInt(Mathf.Cos(lat) * 16f));
                for (var j = 0; j < ringN; j++)
                {
                    var lon = j * Mathf.PI * 2f / ringN; var nrm = new Vector3(Mathf.Cos(lat) * Mathf.Cos(lon), Mathf.Sin(lat), Mathf.Cos(lat) * Mathf.Sin(lon));
                    bk.Box(tiles, nrm * BR, new Vector3(.38f, .38f, .05f), Quaternion.LookRotation(nrm));
                }
            }
            bk.Blob(dark, Vector3.zero, Vector3.one * (BR - .05f), ctx.Rng, 0f);
            bk.Build("Ball", ball, false);
            ball.position = c + up * 7f;
            z.mk.Box(rope, c + up * 9f, new Vector3(.04f, 4f, .04f), Quaternion.identity);
            life.lcdBall = ball; life.lcdTiles = tiles;
            var lcdLights = new List<Light>();
            for (var k = 0; k < 4; k++) lcdLights.Add(ArtistBits.PointLight(z.root, c + up * 5f + Quaternion.Euler(0, k * 90f, 0) * z.front * 4f, Neon[k], 16f, .3f));
            life.lcdLights = lcdLights;
            life.lcdAt = c;
            z.area.glowLight = lcdLights[0];
            // All My Friends: a ring of camp chairs and a cooler on the grass outside
            var ringC = z.At(R + 6f, R * .6f);
            for (var k = 0; k < 6; k++)
            {
                var a = k * Mathf.PI * 2f / 6f; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                var p = ctx.OnGround(ringC.x + d.x * 2.4f, ringC.z + d.z * 2.4f); var r = Quaternion.LookRotation(-d);
                var col = WorldMaterials.Solid(Bunting[k % Bunting.Length]);
                z.mk.Box(col, p + up * .45f, new Vector3(.55f, .05f, .5f), r);
                z.mk.Box(col, p + up * .8f + d * .25f, new Vector3(.55f, .6f, .05f), r * Quaternion.Euler(12f, 0, 0));
                foreach (var (x, y) in new[] { (-.25f, -.2f), (.25f, -.2f), (-.25f, .22f), (.25f, .22f) }) z.mk.Box(metal, p + r * new Vector3(x, .22f, y), new Vector3(.03f, .45f, .03f), r);
            }
            z.mk.Box(WorldMaterials.Solid(new Color(.3f, .55f, .85f)), ringC + up * .3f, new Vector3(.7f, .5f, .45f), Quaternion.identity);
            z.solid.Box(dark, ringC + up * .5f, new Vector3(5f, 1f, 5f), Quaternion.identity);
        }

        // ---------------------------------------------------------------- the smaller stages
        void Sunburst(Spot z)
        {
            var up = Vector3.up; var c = z.At(-2f, 0) + up * 6f; var gold = GlowM(new Color(1f, .78f, .3f), 2.4f);
            z.mk.Blob(gold, c, Vector3.one * 1.6f, z.ctx.Rng, .05f);
            for (var k = 0; k < 24; k++)
            {
                var d = Quaternion.AngleAxis(k * 15f, z.front) * z.side;
                var len = k % 2 == 0 ? 4.2f : 2.6f;
                z.mk.Prism(gold, c + d * 1.4f, d, .22f, len, 5, .1f);
            }
            z.mk.Prism(metal, z.At(-2f, 0), up, .2f, 6f, 8);
            ArtistBits.PostCollider(z.root, z.At(-2f, 0), 6f, .3f);
            halos.Add(c + z.front * 2f, 5f, new Color(1f, .78f, .35f), 1f);
            z.area.glowLight = ArtistBits.PointLight(z.root, c + z.front * 2f, new Color(1f, .78f, .4f), 18f, 1.8f);
        }

        void Bandstand(Spot z)
        {
            var ctx = z.ctx; var up = Vector3.up; var c = z.At(-1f, 0); const int n = 8; const float R = 4.5f;
            var white = WorldMaterials.Solid(new Color(.95f, .94f, .9f));
            z.mk.Prism(white, c, up, R + .3f, .8f, n);
            z.solid.Prism(white, c, up, R + .3f, .8f, n);
            for (var k = 0; k < n; k++)
            {
                var a = k * Mathf.PI * 2f / n; var p = c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * R;
                z.mk.Prism(white, p + up * .8f, up, .12f, 3.4f, 6);
                ArtistBits.PostCollider(z.root, p, 4.2f, .14f);
            }
            z.mk.Cone(WorldMaterials.Solid(new Color(.35f, .55f, .5f)), c + up * 4.2f, R + .6f, 2.2f, n);
            var chand = c + up * 3.6f;
            for (var k = 0; k < 8; k++) { var a = k * Mathf.PI / 4f; z.mk.Blob(GlowM(new Color(1f, .85f, .55f), 2.6f), chand + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * .6f, Vector3.one * .1f, ctx.Rng, 0f); }
            halos.Add(chand - up * .3f, 2.2f, new Color(1f, .85f, .55f), .9f);
            var piano = c + up * .8f - z.front * 1f; var rot = Quaternion.LookRotation(z.side);
            z.mk.Box(dark, piano + up * .7f, new Vector3(1.6f, .35f, 2f), rot);
            z.mk.Box(WorldMaterials.Solid(Ink), piano + up * .9f + z.side * -.85f, new Vector3(1.4f, .04f, .16f), rot);
            foreach (var p in new[] { piano + z.side * .6f + z.front * .6f, piano + z.side * .6f - z.front * .6f, piano - z.side * .7f }) z.mk.Box(dark, p + up * .3f, new Vector3(.1f, .6f, .1f), rot);
            z.area.glowLight = ArtistBits.PointLight(z.root, chand, new Color(1f, .85f, .55f), 12f, 1.4f);
        }

        void ChillTent(Spot z)
        {
            var ctx = z.ctx; var up = Vector3.up; var c = z.At(-1f, 0);
            var cloth = WorldMaterials.Solid(new Color(.55f, .4f, .75f));
            const float R = 4f;
            for (var k = 0; k < 10; k++)
            {
                float a0 = k * Mathf.PI * 2f / 10f, a1 = (k + 1) * Mathf.PI * 2f / 10f;
                if (k == 0) continue;                                                     // the open side
                var p0 = c + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * R; var p1 = c + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * R;
                z.mk.Tri(WorldMaterials.Solid(k % 2 == 0 ? new Color(.55f, .4f, .75f) : new Color(.95f, .6f, .4f)), p0, p1, c + up * 5.5f, c);
                z.mk.Tri(cloth, p0, p1, c + up * 5.5f, c + (p0 - c) * 3f);
            }
            z.solid.Prism(cloth, c, up, R * .7f, 3f, 8);
            for (var k = 0; k < 2; k++)
            {
                var h0 = c + z.side * (R + 2f) + z.front * (k * 3f - 1.5f); h0 = ctx.OnGround(h0.x, h0.z); var h1 = h0 + z.side * 3f;
                foreach (var p in new[] { h0, h1 }) z.mk.Prism(wood, p, up, .08f, 1.6f, 6);
                z.solid.Box(wood, (h0 + h1) / 2f + up * .8f, new Vector3(.8f, 1.6f, 3.2f), Quaternion.LookRotation(h1 - h0));
                for (var i = 0; i < 6; i++) { float t0 = i / 6f, t1 = (i + 1) / 6f; Vector3 P(float t) => Vector3.Lerp(h0, h1, t) + up * (1.3f - Mathf.Sin(t * Mathf.PI) * .6f); z.mk.Box(WorldMaterials.Solid(Bunting[(k + i) % Bunting.Length]), (P(t0) + P(t1)) / 2f, new Vector3(.8f, .04f, Vector3.Distance(P(t0), P(t1)) + .02f), Quaternion.LookRotation(P(t1) - P(t0))); }
            }
            for (var k = 0; k < 5; k++) { var l = c + up * 4f + new Vector3(Mathf.Cos(k * 1.3f), 0, Mathf.Sin(k * 1.3f)) * 1.5f; z.mk.Blob(GlowM(Neon[k], 2.4f), l, Vector3.one * .15f, ctx.Rng, 0f); }
            halos.Add(c + up * 2f + z.front * (R + 1f), 2.4f, new Color(.8f, .55f, 1f), .8f);
            z.area.glowLight = ArtistBits.PointLight(z.root, c + up * 3f, new Color(.85f, .6f, 1f), 12f, 1.2f);
        }

        void Cinema(Spot z)
        {
            var ctx = z.ctx; var up = Vector3.up; var rot = Quaternion.LookRotation(z.front);
            var screen = z.At(-5f, 0);
            foreach (var s in new[] { -1f, 1f }) { z.mk.Box(dark, screen + z.side * s * 5.2f + up * 3.5f, new Vector3(.3f, 7f, .3f), rot); ArtistBits.PostCollider(z.root, screen + z.side * s * 5.2f, 7f, .2f); }
            var film = new Material(GlowM(new Color(.8f, .85f, 1f), 1.6f)) { name = "Cinema screen" };
            life.screen = film;
            z.mk.Box(film, screen + up * 4f, new Vector3(10f, 5.5f, .1f), rot);
            z.mk.Box(dark, screen + up * 4f - z.front * .1f, new Vector3(10.4f, 5.9f, .1f), rot);
            halos.Add(screen + up * 4f + z.front * 4f, 5f, new Color(.8f, .85f, 1f), .6f);
            // the two doors, standing on their own in the grass in front of it
            foreach (var s in new[] { -1f, 1f })
            {
                var d = z.At(1.5f, s * 2.2f);
                z.mk.Box(WorldMaterials.Solid(s < 0 ? new Color(.85f, .3f, .3f) : new Color(.3f, .5f, .85f)), d + up * 1.1f, new Vector3(1f, 2.2f, .1f), rot);
                z.mk.Box(WorldMaterials.Solid(new Color(.95f, .94f, .9f)), d + up * 1.1f, new Vector3(1.2f, 2.35f, .07f), rot);
                z.mk.Blob(WorldMaterials.Solid(new Color(.9f, .75f, .3f), .8f), d + up * 1.05f + z.side * .35f + z.front * .07f, Vector3.one * .05f, ctx.Rng, 0f);
                z.solid.Box(dark, d + up * 1.1f, new Vector3(1.2f, 2.4f, .25f), rot);
            }
            for (var k = 0; k < 6; k++)
            {
                var p = z.At(4f + (k / 3) * 2f, (k % 3 - 1) * 1.6f); var r = Quaternion.LookRotation(-z.front);
                z.mk.Box(WorldMaterials.Solid(Bunting[k % Bunting.Length]), p + up * .4f, new Vector3(.6f, .04f, .7f), r * Quaternion.Euler(-15f, 0, 0));
                z.mk.Box(wood, p + up * .2f, new Vector3(.6f, .4f, .05f), r);
            }
            z.area.glowLight = ArtistBits.PointLight(z.root, screen + up * 4f + z.front * 3f, new Color(.8f, .85f, 1f), 14f, 1.2f);
        }

        void Comet(Spot z)
        {
            var ctx = z.ctx; var up = Vector3.up;
            var head = z.At(-2f, 2f) + up * 7f; var dir = (z.side * -1f - up * .35f).normalized;
            z.mk.Blob(GlowM(new Color(.8f, .95f, 1f), 3f), head, Vector3.one * 1.1f, ctx.Rng, .15f);
            for (var k = 0; k < 14; k++)
            {
                var p = head - dir * (k * 1.1f) + new Vector3(ctx.Range(-.3f, .3f), ctx.Range(-.3f, .3f), ctx.Range(-.3f, .3f));
                z.mk.Blob(GlowM(Color.Lerp(new Color(.7f, .9f, 1f), new Color(1f, .5f, .8f), k / 14f), 2.4f), p, Vector3.one * Mathf.Lerp(.9f, .2f, k / 14f), ctx.Rng, .2f);
            }
            foreach (var p in new[] { head, head - dir * 8f })
            {
                var g = ctx.OnGround(p.x, p.z);
                z.mk.Prism(metal, g, up, .1f, p.y - g.y, 6);
                ArtistBits.PostCollider(z.root, g, p.y - g.y, .12f);
            }
            halos.Add(head, 4f, new Color(.8f, .95f, 1f), 1f);
            z.area.glowLight = ArtistBits.PointLight(z.root, head, new Color(.8f, .9f, 1f), 14f, 1.6f);
        }

        void MicRing(Spot z)
        {
            var ctx = z.ctx; var up = Vector3.up; var c = z.At(-1f, 0);
            z.mk.Prism(dark, c, up, 2.5f, .4f, 16);
            z.solid.Prism(dark, c, up, 2.5f, 4f, 12);
            z.mk.Prism(metal, c + up * .4f, up, .05f, 1.5f, 6);
            z.mk.Blob(dark, c + up * 2f, new Vector3(.12f, .18f, .12f), ctx.Rng, 0f);
            for (var k = 0; k < 12; k++)
            {
                var a = k * Mathf.PI * 2f / 12f; var p = c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 2.2f + up * .4f;
                var h = 1.5f + Mathf.Abs(Mathf.Sin(k * 1.7f)) * 2.5f;
                z.mk.Box(GlowM(Neon[k % Neon.Length], 2.6f), p + up * h / 2f, new Vector3(.12f, h, .12f), Quaternion.identity);
            }
            halos.Add(c + up * 2f, 3.4f, new Color(.8f, .6f, 1f), .9f);
            z.area.glowLight = ArtistBits.PointLight(z.root, c + up * 2.5f, new Color(.8f, .6f, 1f), 12f, 1.4f);
        }

        /// A small open stage with a striped canopy, a drum riser, amps and two lights.
        void SmallStage(Spot z)
        {
            var ctx = z.ctx; var up = Vector3.up; var rot = Quaternion.LookRotation(z.front); var c = z.At(-2f, 0);
            var col = WorldMaterials.Solid(Bunting[Mathf.Abs(z.a.name.GetHashCode()) % Bunting.Length]);
            z.mk.Box(dark, c + up * .5f, new Vector3(8f, 1f, 5f), rot);
            z.solid.Box(dark, c + up * .5f, new Vector3(8f, 1f, 5f), rot);
            foreach (var s in new[] { -1f, 1f }) { z.mk.Box(metal, c + z.side * s * 3.8f + up * 3f, new Vector3(.2f, 6f, .2f), rot); ArtistBits.PostCollider(z.root, c + z.side * s * 3.8f, 6f, .15f); }
            for (var k = 0; k < 6; k++) z.mk.Box(k % 2 == 0 ? col : WorldMaterials.Solid(Ink), c + up * 6f + z.side * ((k - 2.5f) * 1.4f), new Vector3(1.4f, .3f, 5.4f), rot * Quaternion.Euler(-6f, 0, 0));
            z.mk.Box(dark, c + up * 1.3f - z.front * 1.2f, new Vector3(2.4f, .6f, 2f), rot);
            z.mk.Prism(WorldMaterials.Solid(new Color(.85f, .2f, .25f)), c + up * 1.6f - z.front * 1.2f, z.front, .5f, .5f, 14);
            foreach (var s in new[] { -1f, 1f }) z.mk.Box(dark, c + z.side * s * 2.6f + up * 1.6f - z.front * 1.5f, new Vector3(1f, 1.2f, .6f), rot);
            foreach (var s in new[] { -1f, 1f }) z.mk.Prism(GlowM(Neon[(Mathf.Abs(z.a.name.GetHashCode()) + (s > 0 ? 1 : 0)) % Neon.Length], 3f), c + z.side * s * 3.2f + up * 5.6f + z.front * .5f, (z.front - up).normalized, .2f, .3f, 8);
            halos.Add(c + up * 4f + z.front * 3f, 3f, Sunset, .7f);
        }
    }
}
