using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The big show (part of ArenaKit): the stadium is the heart of the ridge, a live arena show that feels epic.
    /// - On stage: a band in silhouette (a singer at the mic, two guitarists, a drummer up on a riser behind his
    ///   kit), each in a spotlight; a merch tent outside the gate with glowing shirts.
    /// - On the field: a crowd of hundreds swaying in waves, lighters held up, blazing for the anthems.
    /// - Lasers fan out from the stage over the crowd and the sky; flame jets fire along the stage front on the big
    ///   beats; fireworks burst over the stands near the end of the anthems ("Don't Stop Believin'", "Carry on
    ///   Wayward Son", "Renegade", "Eye of the Tiger", "More Than a Feeling", "Wheel in the Sky", "We Built This City").
    /// - The bands' own additions: Journey's little station with a lit "ANYWHERE" board and the wheel's rim glowing
    ///   for "Wheel in the Sky"; Boston's landing field of glass domes and landing lights ("Peace of Mind"); Blue
    ///   Öyster Cult's graveyard of blue lanterns ("(Don't Fear) The Reaper") and the fire pit roaring for "Burnin'
    ///   for You"; Heart's neon heart for "Barracuda".
    public partial class ArenaKit
    {
        public const string WheelInTheSky = "6DXv7vJPrDRxo3Lnh4bacS", PeaceOfMind = "1GqlvSEtMx5xbGptxOTTyk", BurninForYou = "3fkPMWQ6cBNBLuFcPyMS8s",
            ReaperSingle = "3G3PQdcOUZTCgvhvfNGfbv", Barracuda = "4KfSdst7rW39C0sfhArdrz", CarryOn = "4DMKwE2E2iYDKY01C335Uw", Renegade = "1CQqupcyMg7176PPmIVmSj",
            BuiltThisCity = "6OnfBiiSc9RGKiBKKtZXgQ";

        Material SongGlow(string song, Color c, float s = 1.6f)
        {
            var m = new Material(GlowM(c, s)) { name = "Song glow" }; life.songGlows.Add((song, m, c * (s * .65f))); return m;
        }

        static Material SoftM(Color col, bool add)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = add ? "Arena light" : "Arena glass" };
            m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", add ? 2f : 0f); m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)(add ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
            m.SetFloat("_ZWrite", 0f); m.SetFloat("_Cull", 0f); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            m.SetColor("_BaseColor", col); return m;
        }

        static Transform SoftMesh(Transform parent, string name, List<Vector3> v, List<Color> col, List<int> tri, Material m)
        {
            var mesh = new Mesh { name = name }; mesh.SetVertices(v); mesh.SetColors(col); mesh.SetTriangles(tri, 0); mesh.RecalculateBounds();
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false; return go.transform;
        }

        /// A soft cone of light from 'top' down onto 'foot'.
        static void SpotCone(Transform parent, Vector3 top, Vector3 foot, float r, Material m)
        {
            var v = new List<Vector3> { top }; var col = new List<Color> { new Color(1, 1, 1, .05f) }; var tri = new List<int>(); const int N = 14;
            var d = (foot - top).normalized; var a1 = Vector3.Cross(d, Vector3.right).normalized; if (a1.sqrMagnitude < .1f) a1 = Vector3.Cross(d, Vector3.forward).normalized; var a2 = Vector3.Cross(d, a1);
            for (var k = 0; k < N; k++) { var a = k * Mathf.PI * 2f / N; v.Add(foot + (a1 * Mathf.Cos(a) + a2 * Mathf.Sin(a)) * r); col.Add(new Color(1, 1, 1, .55f)); tri.AddRange(new[] { 0, 1 + k, 1 + (k + 1) % N }); }
            SoftMesh(parent, "Spotlight cone", v, col, tri, m);
        }

        Material Figure => figureM != null ? figureM : (figureM = WorldMaterials.Solid(new Color(.05f, .05f, .07f)));
        Material figureM;

        void Person(MeshKit k, Vector3 feet, Vector3 face, float h, System.Random rng)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face);
            k.Box(Figure, feet + up * h * .27f, new Vector3(.32f, h * .54f, .2f), rot); k.Box(Figure, feet + up * h * .7f, new Vector3(.44f, h * .36f, .24f), rot);
            k.Blob(Figure, feet + up * h * .94f, new Vector3(.12f, .14f, .12f), rng, 0f);
        }

        void Show(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 best, Vector3 st, Vector3 gateDir, Vector3 sside, Material crowd)
        {
            var up = Vector3.up; var deck = st + up * 2.2f; var srot = Quaternion.LookRotation(gateDir); var rng = new System.Random(1984);
            // the band
            var spotM = SoftM(new Color(1f, .95f, .85f, .5f), true); life.spots = spotM;
            var riser = deck - gateDir * 3f; mk.Box(dark, riser + up * .6f, new Vector3(4f, 1.2f, 3f), srot); solid.Box(dark, riser + up * .6f, new Vector3(4f, 1.2f, 3f), srot);
            var kit = riser + up * 1.2f; var shell = WorldMaterials.Solid(new Color(.6f, .1f, .12f), .8f); var cym = WorldMaterials.Solid(new Color(.9f, .75f, .35f), .9f);
            mk.Prism(shell, kit + up * .6f + gateDir * .4f - gateDir * .4f, gateDir, .55f, .7f, 16); foreach (var s in new[] { -.6f, .6f }) mk.Prism(shell, kit + up * 1.1f + sside * s + gateDir * .5f, up, .28f, .3f, 12);
            foreach (var s in new[] { -1.3f, 1.3f }) { mk.Prism(metal, kit + sside * s, up, .03f, 1.6f, 6); mk.Prism(cym, kit + sside * s + up * 1.6f, up, .45f, .02f, 14, .2f); }
            var band = new MeshKit();
            Person(band, kit - gateDir * .6f, gateDir, 1.5f, rng);
            var singer = deck + gateDir * 3.4f; Person(band, singer, gateDir, 1.85f, rng); mk.Prism(metal, singer + gateDir * .5f, up, .03f, 1.5f, 6); mk.Blob(dark, singer + gateDir * .5f + up * 1.55f, Vector3.one * .07f, ctx.Rng, 0f);
            foreach (var s in new[] { -1f, 1f })
            {
                var g = deck + gateDir * 1.8f + sside * s * 4.5f; Person(band, g, (gateDir - sside * s * .3f).normalized, 1.85f, rng);
                band.Box(GlowM(s < 0 ? new Color(1f, .2f, .15f) : new Color(.3f, .6f, 1f), 1.2f), g + up * 1.05f + gateDir * .25f, new Vector3(.5f, .3f, .06f), srot * Quaternion.Euler(0, 0, s * 25f));
                band.Box(Figure, g + up * 1.25f + gateDir * .25f + sside * s * .45f, new Vector3(.8f, .05f, .05f), srot * Quaternion.Euler(0, 0, s * 25f));
                SpotCone(life.transform, g + up * 10.8f, g + up * .05f, 1.4f, spotM);
            }
            SpotCone(life.transform, singer + up * 10.8f - gateDir * 1f, singer + up * .05f, 1.6f, spotM); SpotCone(life.transform, kit + up * 9.6f, kit + up * .05f, 1.8f, spotM);
            band.Build("Band on stage", ctx.Root, false);
            // the crowd on the field: four swaying groups, lighters held up
            var groups = new MeshKit[4]; for (var g = 0; g < 4; g++) groups[g] = new MeshKit();
            var field = best; var count = 0;
            for (var x = -26f; x <= 26f; x += 1.5f)
                for (var z = -14f; z <= 24f; z += 1.6f)
                {
                    var p = st + gateDir * (8f + z + 6f) + sside * x + new Vector3((float)rng.NextDouble() - .5f, 0, (float)rng.NextDouble() - .5f) * .8f;
                    if (Vector2.Distance(new Vector2(p.x, p.z), new Vector2(best.x, best.z)) > StadiumR - 11f || rng.NextDouble() < .18) continue;
                    var face = (st - p); face.y = 0; face.Normalize(); p.y = best.y + .05f;
                    var g = Mathf.Clamp(Mathf.FloorToInt((x + 26f) / 13f), 0, 3); var h = 1.6f + (float)rng.NextDouble() * .3f;
                    Person(groups[g], p - best, face, h, rng);
                    if (rng.NextDouble() < .35) { var hand = p - best + up * (h + .35f) + Vector3.Cross(up, face) * .2f; groups[g].Box(Figure, hand - up * .3f, new Vector3(.08f, .6f, .08f), Quaternion.LookRotation(face)); groups[g].Box(crowd, hand + up * .1f, new Vector3(.07f, .14f, .07f), Quaternion.LookRotation(face)); }
                    count++;
                }
            for (var g = 0; g < 4; g++) { var go = groups[g].BuildSingle("Crowd", life.transform, false); go.transform.position = best; life.crowds.Add(go.transform); }
            // the lasers
            Color[] laserCol = { new Color(.2f, 1f, .4f), new Color(.3f, .7f, 1f), new Color(.7f, .35f, 1f), new Color(1f, .3f, .25f) };
            for (var k = 0; k < 8; k++)
            {
                var m = SoftM(laserCol[k % 4], true); var v = new List<Vector3>(); var col = new List<Color>(); var tri = new List<int>();
                foreach (var across in new[] { Vector3.right, Vector3.up }) { var b = v.Count; v.Add(-across * .05f); v.Add(across * .05f); v.Add(Vector3.forward * 160f + across * .6f); v.Add(Vector3.forward * 160f - across * .6f); col.Add(new Color(1, 1, 1, .9f)); col.Add(new Color(1, 1, 1, .9f)); col.Add(new Color(1, 1, 1, 0)); col.Add(new Color(1, 1, 1, 0)); tri.AddRange(new[] { b, b + 1, b + 2, b, b + 2, b + 3 }); }
                var t = SoftMesh(life.transform, "Laser", v, col, tri, m); t.position = deck + up * 9.5f + sside * ((k - 3.5f) * 2.4f); life.lasers.Add((t, m, laserCol[k % 4], k));
            }
            life.laserForward = gateDir;
            // flame jets along the stage front, fireworks over the stands
            for (var k = 0; k < 6; k++) { var p = deck + gateDir * 4.6f + sside * ((k - 2.5f) * 3.6f); mk.Box(metal, p + up * .25f, new Vector3(.6f, .5f, .6f), srot); life.pyro.Add(p + up * .6f); }
            life.fireworksAt = best + up * 40f;
            // the merch tent outside the gate
            var mt = best + gateDir * (StadiumR + 12f) + Vector3.Cross(up, gateDir) * 11f; mt = ctx.OnGround(mt.x, mt.z); var mrot = Quaternion.LookRotation(Vector3.Cross(up, gateDir));
            foreach (var s in new[] { -1f, 1f }) foreach (var b in new[] { -1f, 1f }) { var p = mt + mrot * new Vector3(s * 3f, 0, b * 1.8f); mk.Prism(metal, p, up, .06f, 3f, 6); ArtistBits.PostCollider(ctx.Root, p, 3f, .1f); }
            mk.Box(WorldMaterials.Solid(new Color(.1f, .1f, .14f)), mt + up * 3.05f, new Vector3(6.4f, .1f, 4f), mrot); mk.Box(dark, mt + up * .5f + mrot * Vector3.forward * 1.2f, new Vector3(6f, 1f, .8f), mrot); solid.Box(dark, mt + up * .5f + mrot * Vector3.forward * 1.2f, new Vector3(6f, 1f, .8f), mrot);
            for (var k = 0; k < 5; k++) { var p = mt + up * 2f - mrot * Vector3.forward * 1.6f + mrot * Vector3.right * ((k - 2f) * 1.2f); mk.Box(GlowM(Paint[k % 4] * 1.2f + new Color(.1f, .1f, .1f), 1f), p, new Vector3(.8f, .9f, .04f), mrot); }
            mk.Box(GlowM(Orange, 2f), mt + up * 3.4f + mrot * Vector3.forward * 2f, new Vector3(3f, .5f, .04f), mrot);
            ArtistBits.Label(ctx.Root, "MERCH", mt + up * 3.4f + mrot * Vector3.forward * 2.03f, mrot * Vector3.forward, .3f, new Color(.15f, .05f, .02f), TextAnchor.MiddleCenter);
            ArtistBits.PointLight(ctx.Root, mt + up * 2.5f + mrot * Vector3.forward * 2f, new Color(1f, .8f, .6f), 10f, 1.2f);
            noGrass.Add(new Vector3(mt.x, 5f, mt.z));
            var mark = new GameObject("Shot: the show").transform; mark.SetParent(ctx.Root, false);
            var cam = best + gateDir * (StadiumR - 12f) + sside * 6f + up * 4f; mark.SetPositionAndRotation(cam, Quaternion.LookRotation((deck + up * 3f - cam - up * 1.6f).normalized));
            Debug.Log($"Arena Rock Ridge: the show, {count} in the crowd");
        }

        // ---------------------------------------------------------------- the bands' additions
        void JourneyMore(Plot z)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(z.front);
            var pl = z.At(9f, 9f); z.mk.Box(stone, pl + up * .4f, new Vector3(3f, .8f, 7f), Quaternion.LookRotation(z.side)); z.solid.Box(stone, pl + up * .4f, new Vector3(3f, .8f, 7f), Quaternion.LookRotation(z.side));
            z.mk.Box(wood, pl + up * 1.25f, new Vector3(1.8f, .08f, .45f), rot); z.mk.Box(wood, pl + up * 1.55f - z.front * .2f, new Vector3(1.8f, .5f, .06f), rot);
            foreach (var s in new[] { -1.2f, 1.2f }) z.mk.Box(metal, pl + up * 2.6f + z.side * s, new Vector3(.12f, 3.6f, .12f), rot);
            var board = SongGlow(Believin, new Color(1f, .8f, .4f), 1.6f); z.mk.Box(dark, pl + up * 4.2f, new Vector3(3.4f, .9f, .2f), rot); z.mk.Box(board, pl + up * 4.2f + z.front * .11f, new Vector3(3.1f, .65f, .02f), rot);
            ArtistBits.Label(z.labels, "ANYWHERE", pl + up * 4.2f + z.front * .13f, z.front, .4f, new Color(.15f, .08f, .02f), TextAnchor.MiddleCenter);
            halos.Add(pl + up * 4.2f + z.front * .6f, 2.4f, new Color(1f, .8f, .4f), .35f);
            // the wheel's rim glows for its own song
            var hub = z.At(-4f, 0) + up * 15f; var rimGlow = SongGlow(WheelInTheSky, new Color(.55f, .75f, 1f), .9f);
            for (var i = 0; i < 24; i++) { var a = i * Mathf.PI / 12f; z.mk.Blob(rimGlow, hub + (z.side * Mathf.Cos(a) + up * Mathf.Sin(a)) * 12.2f - z.front * .4f, Vector3.one * .3f, z.ctx.Rng, 0f); }
        }

        void BostonMore(Plot z)
        {
            var up = Vector3.up; var glass = SoftM(new Color(.6f, .85f, 1f, .22f), false);
            foreach (var (fw, rt, r) in new[] { (-9f, -8f, 3.4f), (-10f, 7f, 2.6f), (2f, -10f, 2.2f) })
            {
                var c = z.At(fw, rt);
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere); Object.Destroy(go.GetComponent<Collider>()); go.name = "Glass dome"; go.transform.SetParent(z.root, false);
                go.transform.position = c; go.transform.localScale = new Vector3(r * 2f, r * 1.6f, r * 2f); var gr = go.GetComponent<Renderer>(); gr.sharedMaterial = glass; gr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                z.mk.Prism(metal, c - up * .15f, up, r + .1f, .3f, 20);
                for (var k = 0; k < 3; k++) { var tp = c + new Vector3(Mathf.Cos(k * 2.1f), 0, Mathf.Sin(k * 2.1f)) * r * .45f; z.mk.Box(GlowM(new Color(.7f, .85f, 1f), 1.2f), tp + up * (r * .3f + k * .2f), new Vector3(.5f, r * .6f + k * .4f, .5f), Quaternion.identity); }
                z.solid.Prism(metal, c - up * .2f, up, r, r * .8f, 12);
            }
            var land = SongGlow(PeaceOfMind, new Color(.5f, .9f, 1f), 2f);
            for (var k = 0; k < 20; k++) { var a = k * Mathf.PI / 10f; var p = z.At(-2f, 0) + (z.side * Mathf.Cos(a) + z.front * Mathf.Sin(a)) * 6.4f; z.mk.Box(land, p + up * .12f, new Vector3(.3f, .12f, .3f), Quaternion.identity); }
            halos.Add(z.At(-2f, 0) + up * 1f, 7f, new Color(.5f, .85f, 1f), .2f);
        }

        void ReaperMore(Plot z)
        {
            var up = Vector3.up; var ctx = z.ctx; var lantern = SongGlow(Reaper, new Color(.45f, .7f, 1f), 1.6f); life.reaperSingle = lantern;
            var grave = WorldMaterials.Solid(new Color(.35f, .35f, .4f));
            for (var k = 0; k < 9; k++)
            {
                var p = z.At(-10f + (k / 3) * 3.4f, 9f + (k % 3) * 2.2f); var r = Quaternion.LookRotation(z.front) * Quaternion.Euler(ctx.Range(-6f, 6f), ctx.Range(-10f, 10f), ctx.Range(-6f, 6f));
                z.mk.Box(grave, p + up * .55f, new Vector3(.8f, 1.1f, .2f), r); z.mk.Prism(grave, p + up * 1.1f - r * Vector3.forward * .1f, r * Vector3.forward, .4f, .2f, 10);
                z.solid.Box(grave, p + up * .6f, new Vector3(.9f, 1.3f, .4f), r);
            }
            foreach (var p in new[] { z.At(2f, -6f), z.At(2f, 6f), z.At(-1f, -9f), z.At(-6f, -9f), z.At(-11f, 4f) })
            {
                z.mk.Prism(dark, p, up, .06f, 2.2f, 6); z.mk.Prism(lantern, p + up * 2.2f, up, .18f, .35f, 8); z.mk.Prism(dark, p + up * 2.55f, up, .2f, .06f, 8);
                halos.Add(p + up * 2.4f, 1.2f, new Color(.45f, .7f, 1f), .6f); ArtistBits.PostCollider(z.root, p, 2.4f, .1f);
            }
            var fireGlow = SongGlow(BurninForYou, new Color(1f, .5f, .15f), 1.6f); var pit = z.At(5f, 8f); z.mk.Prism(fireGlow, pit + up * .05f, up, .9f, .08f, 12);
            life.burnPit = pit + up * .3f;
        }
    }
}
