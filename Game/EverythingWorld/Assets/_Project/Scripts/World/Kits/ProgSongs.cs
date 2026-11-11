using UnityEngine;

namespace EverythingWorld.World
{
    /// More of the big prog artists' songs as things in their areas (part of ProgKit), each glowing up while its
    /// song plays:
    /// - Frank Zappa: Joe's Garage furnished (a workbench, a guitar on a stand, a couch, posters, a lit sign over
    ///   the door); "Sofa No. 1", a red velvet sofa floating and turning in the air; "Po-Jama People", striped
    ///   pajamas on a washing line; "Montana", a glowing band on the floss spool and a pair of giant tweezers;
    ///   "Peaches En Regalia", the peaches glow.
    /// - King Crimson: "Red" lights the meter's needle; "Starless", a dark pool under a cluster of small stars that
    ///   go out one by one while it plays; "Elephant Talk", a stone elephant with its trunk up and a glowing word
    ///   ring.
    /// - Rush: "2112", a glowing guitar on an altar in the temple; "The Spirit Of Radio", an old console radio
    ///   sending rings of light up into the air.
    /// - The Alan Parsons Project: "Sirius", the star over the eye blazes; "Pipeline", a chrome pipe with rings of
    ///   light chasing along it.
    public partial class ProgKit
    {
        public const string JoesGarage = "2FlAG4RhzPS7mHMAsirHs1", PeachesSong = "5uDu2qOoDou7cFXsipAo8l", SofaNo1 = "5I1VWbRoZwQK7aVoXJszi5",
            PoJama = "7Gu0jlVAOwKGLoVnAoE5Pk", Montana = "4dr49yNcSnkVXTlgXhpd13", RedSong = "7DV0TzeNMylo6rlkrYXDiP", Starless = "3bbfY8yDDwzLvvx3tFwy8i",
            ElephantTalk = "1VeYMKim09aEymk9grhXRf", Syrinx = "5H8xFHau0x3PI1CssO6EaX", SpiritOfRadio = "4e9hUiLsN4mx61ARosFi7p",
            Pipeline = "6TErrSCoOBfHFeju90jgoJ", SiriusSong = "0hSs4vOZo1OkRiAOoLafrT";

        bool FlatEnough(LandContext ctx, Vector3 p, float r)
        {
            float lo = float.MaxValue, hi = float.MinValue;
            for (var k = 0; k < 8; k++) { var a = k * Mathf.PI / 4f; var h = ctx.Ground(p.x + Mathf.Cos(a) * r, p.z + Mathf.Sin(a) * r); lo = Mathf.Min(lo, h); hi = Mathf.Max(hi, h); }
            return hi - lo < .35f && Mathf.Abs(ctx.Ground(p.x, p.z) - lo) < .3f;
        }

        void ZappaMore(Plot z)
        {
            var up = Vector3.up; var g = z.At(-8f, -5f); var rot = z.Rot; var f = z.front; var s = z.side; var ctx = z.ctx;
            // inside the garage: a workbench with tools, a guitar on a stand, a couch, posters on the back wall
            var wb = g + s * 2.85f + f * 1f; z.mk.Box(wood, wb + up * .9f, new Vector3(.8f, .1f, 2.4f), rot); foreach (var e in new[] { -1.1f, 1.1f }) z.mk.Box(wood, wb + f * e + up * .45f, new Vector3(.7f, .9f, .1f), rot);
            z.solid.Box(wood, wb + up * .5f, new Vector3(.8f, 1f, 2.4f), rot);
            for (var k = 0; k < 4; k++) z.mk.Box(k % 2 == 0 ? chrome : WorldMaterials.Solid(new Color(.8f, .3f, .15f)), wb + up * 1f + f * (-.8f + k * .5f), new Vector3(.2f, .1f, .35f), rot * Quaternion.Euler(0, k * 30f, 0));
            var gt = g - s * 1.2f + f * 1.4f; var body = WorldMaterials.Solid(new Color(.75f, .2f, .15f), .8f);
            z.mk.Box(dark, gt + up * .3f, new Vector3(.5f, .06f, .4f), rot); z.mk.Box(body, gt + up * .65f, new Vector3(.45f, .6f, .1f), rot * Quaternion.Euler(-12f, 0, 0)); z.mk.Box(dark, gt + up * 1.25f - f * .1f, new Vector3(.07f, .8f, .05f), rot * Quaternion.Euler(-12f, 0, 0));
            var couch = g - s * 2.6f + f * 1.2f; var crot = Quaternion.LookRotation(s); var plaid = WorldMaterials.Solid(new Color(.45f, .35f, .2f));
            z.mk.Box(plaid, couch + up * .3f, new Vector3(2.2f, .6f, .9f), crot); z.mk.Box(plaid, couch + up * .8f - s * .35f, new Vector3(2.2f, .7f, .25f), crot); z.solid.Box(plaid, couch + up * .5f, new Vector3(2.2f, 1f, .9f), crot);
            var glowJ = SongGlow(JoesGarage, new Color(1f, .7f, .25f), 1.4f);
            for (var k = 0; k < 3; k++) z.mk.Box(k == 1 ? glowJ : GlowM(Spectrum[k * 2 + 1], .9f), g - f * 2.78f + s * (-2f + k * 2f) + up * 2.6f, new Vector3(1f, 1.4f, .02f), rot);
            z.mk.Box(dark, g + up * .06f + f * .3f, new Vector3(3f, .02f, 2f), rot);
            var sign = g + f * 3.15f + up * 3.6f; z.mk.Box(glowJ, sign, new Vector3(4f, .6f, .02f), rot);
            ArtistBits.Label(z.labels, "JOE'S GARAGE", sign + f * .03f, f, .34f, new Color(.15f, .08f, .05f), TextAnchor.MiddleCenter);
            // Sofa No. 1: a red velvet sofa floating and turning
            var velvet = SongGlow(SofaNo1, new Color(.75f, .1f, .3f), .5f); var sk = new MeshKit();
            sk.Box(velvet, new Vector3(0, 0, 0), new Vector3(2.6f, .5f, 1f), Quaternion.identity); sk.Box(velvet, new Vector3(0, .55f, -.4f), new Vector3(2.6f, .9f, .25f), Quaternion.identity);
            foreach (var e in new[] { -1.35f, 1.35f }) sk.Box(velvet, new Vector3(e, .3f, 0), new Vector3(.25f, .7f, 1f), Quaternion.identity);
            foreach (var e in new[] { -1.1f, 1.1f }) foreach (var d in new[] { -.4f, .4f }) sk.Prism(brass, new Vector3(e, -.25f, d), Vector3.down, .06f, .3f, 6);
            var st = sk.BuildSingle("Floating sofa", life.transform, false).transform; life.sofa = st; life.sofaBase = z.At(5f, 1f) + up * 4.6f; st.position = life.sofaBase;
            halos.Add(life.sofaBase, 2.6f, new Color(.9f, .2f, .4f), .2f);
            // Po-Jama People: striped pajamas on a washing line
            var l0 = z.At(-1f, -15f); var l1 = z.At(7f, -15f);
            foreach (var p in new[] { l0, l1 }) { z.mk.Prism(wood, p - up * .2f, up, .08f, 2.6f, 6); ArtistBits.PostCollider(z.root, p, 2.4f, .12f); }
            var ld = (l1 - l0); ld.y = 0; var lrot = Quaternion.LookRotation(Vector3.Cross(up, ld.normalized));
            z.mk.Box(WorldMaterials.Solid(new Color(.9f, .9f, .9f)), (l0 + l1) / 2f + up * 2.3f, new Vector3(.03f, .03f, ld.magnitude), Quaternion.LookRotation(ld));
            Color[] stripes = { new Color(.3f, .45f, .85f), new Color(.95f, .75f, .3f), new Color(.6f, .3f, .7f) };
            for (var k = 0; k < 4; k++)
            {
                var hp = Vector3.Lerp(l0, l1, .15f + k * .23f) + up * 2.25f; var c1 = WorldMaterials.Solid(stripes[k % 3]); var white = WorldMaterials.Solid(new Color(.95f, .95f, .92f));
                if (k % 2 == 0) { z.mk.Box(c1, hp - up * .4f, new Vector3(.9f, .8f, .04f), lrot); for (var r = 0; r < 3; r++) z.mk.Box(white, hp - up * (.15f + r * .25f) + lrot * Vector3.forward * .025f, new Vector3(.9f, .08f, .01f), lrot); }
                else foreach (var e in new[] { -.2f, .2f }) { z.mk.Box(c1, hp - up * .55f + lrot * Vector3.right * e, new Vector3(.3f, 1.1f, .04f), lrot); for (var r = 0; r < 3; r++) z.mk.Box(white, hp - up * (.25f + r * .3f) + lrot * Vector3.right * e + lrot * Vector3.forward * .025f, new Vector3(.3f, .08f, .01f), lrot); }
            }
            // Montana: a glowing band round the floss spool, giant tweezers leaning on it
            var sp = z.At(3f, -7f); z.mk.Prism(SongGlow(Montana, new Color(.4f, .95f, .85f), 1.4f), sp + up * .75f, up, 1.13f, .14f, 16);
            foreach (var e in new[] { -.12f, .12f }) z.mk.Box(chrome, sp + f * 1.6f + s * e + up * 1f, new Vector3(.08f, 2.2f, .14f), rot * Quaternion.Euler(-20f, 0, e > 0 ? 4f : -4f));
        }

        void CrimsonMore(Plot z)
        {
            var up = Vector3.up; var ctx = z.ctx;
            // Starless: a dark pool under a cluster of small stars (ProgLife puts them out one by one during the song)
            var pc = z.At(.5f, 8.5f); var black = new Material(WorldMaterials.Glow(new Color(.02f, .02f, .05f), .05f)) { name = "Starless pool" }; black.SetFloat("_Smoothness", .7f);
            for (var k = 0; k < 18; k++) { float a0 = k * Mathf.PI / 9f, a1 = (k + 1) * Mathf.PI / 9f; var w = pc + up * .06f; z.mk.Tri(black, w, w + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * 2.4f, w + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * 2.4f, w - up); }
            for (var k = 0; k < 9; k++) { var a = k * 40f * Mathf.Deg2Rad; z.mk.Blob(stone, pc + new Vector3(Mathf.Cos(a), .15f, Mathf.Sin(a)) * 2.6f, new Vector3(.4f, .25f, .35f), ctx.Rng, .3f); }
            for (var k = 0; k < 12; k++)
            {
                var m = new Material(GlowM(new Color(.85f, .9f, 1f), 3f)) { name = "Starless star" }; var sp = pc + new Vector3(ctx.Range(-1.8f, 1.8f), ctx.Range(1.6f, 3.6f), ctx.Range(-1.8f, 1.8f));
                z.mk.Blob(m, sp, Vector3.one * .09f, ctx.Rng, 0f); life.stars.Add((m, new Color(.85f, .9f, 1f) * 1.95f));
            }
            halos.Add(pc + up * 2.6f, 2.6f, new Color(.8f, .85f, 1f), .3f);
            // Elephant Talk: a stone elephant, trunk raised, a glowing ring of talk at its trunk
            var e = z.At(-3f, -8.5f); var face = Quaternion.Euler(0, 40f, 0) * z.front; var rot = Quaternion.LookRotation(face); var sd = Vector3.Cross(up, face); var grey = WorldMaterials.Solid(new Color(.55f, .53f, .56f));
            // stand it on the highest of its four feet (it sat half sunk on the slope), a little taller in the leg
            foreach (var s in new[] { -.5f, .5f }) foreach (var fw in new[] { -1.1f, 1.1f }) { var fp = e + sd * s * 1.4f + face * fw; e.y = Mathf.Max(e.y, ctx.Ground(fp.x, fp.z)); }
            e += up * .5f;
            z.mk.Blob(grey, e + up * 2.2f, new Vector3(1.4f, 1.2f, 2f), ctx.Rng, .05f); var head = e + up * 2.8f + face * 2f; z.mk.Blob(grey, head, new Vector3(.9f, .95f, .8f), ctx.Rng, .05f);
            foreach (var s in new[] { -1f, 1f }) { z.mk.Box(grey, head + sd * s * 1f - face * .2f, new Vector3(.12f, 1.4f, 1.1f), rot * Quaternion.Euler(0, s * 25f, 0)); z.mk.Cone(WorldMaterials.Solid(new Color(.95f, .93f, .86f)), head + sd * s * .35f - up * .5f + face * .5f, .1f, .8f, 6); }
            var tp = head + face * .7f - up * .2f; var td = (face + up * .3f).normalized;
            for (var k = 0; k < 4; k++) { var nd = (td + up * k * .45f).normalized; z.mk.Prism(grey, tp, nd, .26f - k * .04f, .8f, 8, .85f); tp += nd * .75f; }
            foreach (var s in new[] { -.5f, .5f }) foreach (var fw in new[] { -1.1f, 1.1f }) { var fp = e + sd * s * 1.4f + face * fw; var gy = ctx.Ground(fp.x, fp.z) - .2f; z.mk.Prism(grey, new Vector3(fp.x, gy, fp.z), up, .38f, e.y + 1.4f - gy, 8); }
            var talk = SongGlow(ElephantTalk, new Color(1f, .8f, .3f), 1.6f);
            for (var k = 0; k < 16; k++) { var a0 = k * Mathf.PI / 8f; var a1 = (k + 1) * Mathf.PI / 8f; var cc = tp + td * .8f; var p0 = cc + (sd * Mathf.Cos(a0) + up * Mathf.Sin(a0)) * .7f; var p1 = cc + (sd * Mathf.Cos(a1) + up * Mathf.Sin(a1)) * .7f; z.mk.Box(talk, (p0 + p1) / 2f, new Vector3(.08f, .08f, Vector3.Distance(p0, p1)), Quaternion.LookRotation(p1 - p0)); }
            z.solid.Box(grey, e + up * 1.35f + face * .6f, new Vector3(3f, 3.7f, 4.6f), rot);
        }

        void RushMore(Plot z)
        {
            var up = Vector3.up; var ctx = z.ctx;
            // 2112: the guitar found in the temple, on an altar
            var al = z.At(-1.5f, 0); z.mk.Box(stone, al + up * .5f, new Vector3(2.4f, 1f, 1.1f), z.Rot); z.solid.Box(stone, al + up * .5f, new Vector3(2.4f, 1f, 1.1f), z.Rot);
            var gg = SongGlow(Syrinx, new Color(1f, .82f, .4f), 1.8f); var lie = z.Rot * Quaternion.Euler(-90f, 0, 70f);
            z.mk.Blob(gg, al + up * 1.1f - z.side * .35f, new Vector3(.32f, .08f, .42f), ctx.Rng, 0f); z.mk.Box(gg, al + up * 1.1f + z.side * .45f, new Vector3(1.1f, .05f, .09f), Quaternion.LookRotation(z.side));
            halos.Add(al + up * 1.3f, 1.8f, new Color(1f, .82f, .4f), .5f);
            // The Spirit Of Radio: an old console radio sending rings of light up
            var r = z.At(.5f, -9f); var cab = WorldMaterials.Solid(new Color(.45f, .28f, .16f), .7f);
            z.mk.Box(cab, r + up * .9f, new Vector3(1.6f, 1.8f, .9f), z.Rot); z.mk.Prism(cab, r + up * 1.8f, z.front, .8f, .9f, 12, 1f); z.solid.Box(cab, r + up * 1.1f, new Vector3(1.7f, 2.4f, 1f), z.Rot);
            z.mk.Box(WorldMaterials.Solid(new Color(.75f, .65f, .45f)), r + up * .7f + z.front * .46f, new Vector3(1.2f, .8f, .02f), z.Rot);
            z.mk.Prism(SongGlow(SpiritOfRadio, new Color(1f, .75f, .3f), 1.8f), r + up * 1.45f + z.front * .45f, z.front, .3f, .03f, 16);
            for (var k = 0; k < 3; k++)
            {
                var m = new Material(GlowM(new Color(.4f, .8f, 1f), 1f)) { name = "Radio ring" }; var rk = new MeshKit();
                for (var i = 0; i < 24; i++) { var a0 = i * Mathf.PI / 12f; var a1 = (i + 1) * Mathf.PI / 12f; var p0 = new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)); var p1 = new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)); rk.Box(m, (p0 + p1) / 2f, new Vector3(.06f, .06f, Vector3.Distance(p0, p1) + .01f), Quaternion.LookRotation(p1 - p0)); }
                var tr = rk.BuildSingle("Radio ring", life.transform, false).transform; tr.position = r + up * 2.8f; life.radioRings.Add((tr, m, r + up * 2.8f, k / 3f));
            }
        }

        void ParsonsMore(Plot z)
        {
            var up = Vector3.up;
            var a = z.At(3f, -7.5f); var b = z.At(-9f, -7.5f); var y = Mathf.Max(a.y, b.y) + 1.3f; a.y = b.y = y; var dir = (b - a).normalized;
            z.mk.Prism(chrome, a, dir, .38f, Vector3.Distance(a, b), 14);
            foreach (var t in new[] { .1f, .5f, .9f }) { var p = Vector3.Lerp(a, b, t); var g = z.ctx.Ground(p.x, p.z); z.mk.Box(darkStone, new Vector3(p.x, (g + y) / 2f - .2f, p.z), new Vector3(.3f, y - g, .3f), z.Rot); ArtistBits.PostCollider(z.root, new Vector3(p.x, g, p.z), y - g + .4f, .45f); }
            for (var k = 0; k < 10; k++)
            {
                var m = new Material(GlowM(new Color(1f, .8f, .35f), 1f)) { name = "Pipeline light" }; var p = Vector3.Lerp(a, b, (k + .5f) / 10f);
                z.mk.Prism(m, p - dir * .06f, dir, .42f, .12f, 14); life.pipeline.Add(m);
            }
        }
    }
}
