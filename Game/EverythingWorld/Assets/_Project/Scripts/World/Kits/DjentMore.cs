using UnityEngine;

namespace EverythingWorld.World
{
    /// The Djent Spires' smaller artists, each with a piece of their own from their song, behind the covers board:
    /// Opeth a tall window of crystal panes ("Windowpane"); Coheed and Cambria a doorway with a lamp lit in it
    /// ("Welcome Home"); Caligula's Horse a gold crystal flower ("Marigold"); StarSystems a moon with crystal rain
    /// falling round it ("It's Raining on the Moon"); Arch Echo a wheel of coloured glass ("Color Wheel"); David Maxim
    /// Micic an armchair and a lamp on a floating slab ("Living Room"); Angel Vivaldi a figure-eight loop of light
    /// ("Eight"); Anima Tempo twin crystals, mirror images ("Primal Symmetry"); DGM two doors, one open ("Second
    /// Chance"); DVSR a pit stepping down six tiers ("Six Figures Deep"); Destiny Potato a little maze of low walls
    /// ("Walls of Thoughts"); Fernando Molinari a console of glowing buttons and faders ("Control"); Liquid Tension
    /// Experiment two gyroscope rings crossing ("Biaxident"); Outrun The Sunlight a red crystal bird on a perch ("Red
    /// Bird"); The Omnific jewelled eggs in a glass case ("Objets de Vertu"); Unprocessed broken crystal columns
    /// ("Ruins").
    public partial class DjentKit
    {
        void Small(Plot z)
        {
            var up = Vector3.up; var ctx = z.ctx; var c = z.At(-3.2f, 0); var rot = z.Rot;
            string first = null; if (SongIds.TryGetValue(z.a.name, out var l) && l.Length > 0) { first = l[0].id; z.lit.Add(first); }
            Material G(Color col, float s = 1.8f) => SongGlow(first, col, s);
            switch (z.a.name)
            {
                case "Opeth":
                {
                    var pane = G(new Color(.8f, .9f, 1f), 1.2f);
                    z.mk.Box(slate, c + up * 2.6f, new Vector3(3.2f, 5.4f, .3f), rot); z.solid.Box(slate, c + up * 2.6f, new Vector3(3.2f, 5.4f, .4f), rot);
                    for (var i = 0; i < 2; i++) for (var j = 0; j < 4; j++) z.mk.Box(pane, c + up * (.9f + j * 1.2f) + z.side * ((i - .5f) * 1.25f) + z.front * .17f, new Vector3(1.1f, 1.05f, .03f), rot);
                    z.mk.Box(goldM, c + up * 5.4f, new Vector3(3.4f, .2f, .5f), rot);
                    halos.Add(c + up * 2.6f + z.front * 1f, 2.6f, Ice, .45f); break;
                }
                case "Coheed and Cambria":
                {
                    var lamp = G(Gold, 2f);
                    foreach (var s in new[] { -1f, 1f }) { z.mk.Box(stoneLight, c + z.side * s * 1.1f + up * 1.6f, new Vector3(.5f, 3.2f, .6f), rot); z.solid.Box(stoneLight, c + z.side * s * 1.1f + up * 1.6f, new Vector3(.5f, 3.2f, .6f), rot); }
                    z.mk.Box(stoneLight, c + up * 3.4f, new Vector3(2.8f, .5f, .6f), rot);
                    z.mk.Box(G(new Color(1f, .85f, .55f), 1.2f), c + up * 1.5f - z.front * .1f, new Vector3(1.7f, 3f, .05f), rot);
                    z.mk.Box(metal, c + up * 3.1f + z.front * .5f, new Vector3(.05f, .4f, .05f), rot); z.mk.Box(lamp, c + up * 2.8f + z.front * .5f, new Vector3(.25f, .3f, .25f), rot);
                    halos.Add(c + up * 2f + z.front * .8f, 2.2f, Gold, .5f); break;
                }
                case "Caligula's Horse":
                {
                    var petal = G(Gold, 1.8f);
                    z.mk.Prism(WorldMaterials.Solid(new Color(.4f, .6f, .45f)), c - up * .2f, up, .12f, 3.4f, 6);
                    for (var k = 0; k < 10; k++) { var a = k * 36f; var d = Quaternion.AngleAxis(a, up) * Quaternion.Euler(55f, 0, 0) * up; Crystal(z.mk, petal, c + up * 3.2f, d, .26f, 1.6f); }
                    z.mk.Blob(goldGlow, c + up * 3.3f, Vector3.one * .35f, ctx.Rng, 0f);
                    ArtistBits.PostCollider(z.root, c, 3.4f, .3f); halos.Add(c + up * 3.3f, 2.4f, Gold, .55f); break;
                }
                case "StarSystems":
                {
                    var moon = G(Pearl, 1.4f);
                    z.mk.Prism(slate, c - up * .2f, up, .2f, 3f, 6); ArtistBits.PostCollider(z.root, c, 3f, .3f);
                    z.mk.Blob(moon, c + up * 4.6f, Vector3.one * 1.5f, ctx.Rng, .08f);
                    for (var k = 0; k < 18; k++) { var a = k * 2.4f; var r = 1.8f + (k % 3) * .4f; var p = c + up * (1.2f + (k * 7 % 11) * .3f) + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * r; Crystal(z.mk, iceGlow, p, -up, .05f, .35f); }
                    halos.Add(c + up * 4.6f, 3f, Pearl, .5f); life.rainMoon = c + up * 4.6f; break;
                }
                case "Arch Echo":
                {
                    Color[] cols = { new Color(1f, .3f, .3f), new Color(1f, .75f, .25f), new Color(.35f, .95f, .45f), new Color(.3f, .7f, 1f), new Color(.6f, .4f, 1f), new Color(1f, .45f, .8f) };
                    var wk = new MeshKit();
                    for (var k = 0; k < 6; k++) { var a = k * 60f; var d = Quaternion.AngleAxis(a, Vector3.forward) * Vector3.up; wk.Box(G(cols[k], 1.6f), d * 1.1f, new Vector3(1.1f, 1.8f, .06f), Quaternion.AngleAxis(a, Vector3.forward)); }
                    wk.Blob(goldGlow, Vector3.zero, Vector3.one * .35f, ctx.Rng, 0f);
                    var go = wk.BuildSingle("Colour wheel", life.transform, false);
                    if (go != null) { go.transform.SetPositionAndRotation(c + up * 3.2f, rot); life.wheels.Add(go.transform); }
                    z.mk.Prism(slate, c - up * .2f, up, .2f, 3.2f, 6); ArtistBits.PostCollider(z.root, c, 3f, .3f);
                    halos.Add(c + up * 3.2f, 2.6f, Pearl, .45f); break;
                }
                case "David Maxim Micic":
                {
                    var slab = c + up * 1.6f;
                    z.mk.Box(stoneLight, slab, new Vector3(3.4f, .3f, 2.6f), rot); Crystal(z.mk, crystal, slab - up * .15f, -up, .5f, 1.6f);
                    var chair = WorldMaterials.Solid(new Color(.55f, .35f, .3f));
                    z.mk.Box(chair, slab + up * .55f, new Vector3(1.2f, .5f, 1f), rot); z.mk.Box(chair, slab + up * 1.05f - z.front * .45f, new Vector3(1.2f, 1f, .25f), rot);
                    foreach (var s in new[] { -1f, 1f }) z.mk.Box(chair, slab + up * .8f + z.side * s * .55f, new Vector3(.2f, .5f, 1f), rot);
                    var lp = slab + z.side * 1.3f; z.mk.Prism(metal, lp, up, .04f, 1.6f, 6); z.mk.Prism(G(new Color(1f, .85f, .55f), 1.6f), lp + up * 1.5f, up, .3f, .4f, 8, .6f);
                    z.solid.Box(stoneLight, slab, new Vector3(3.4f, .3f, 2.6f), rot); BoxCol(z.root, c + up * .7f, new Vector3(1f, 1.4f, 1f), rot);
                    halos.Add(lp + up * 1.6f, 1.6f, Gold, .55f); life.floaters.Add(slab); break;
                }
                case "Angel Vivaldi":
                {
                    var loop = G(Ice, 2.2f);
                    for (var k = 0; k < 48; k++)
                    {
                        float t0 = k * Mathf.PI * 2f / 48f, t1 = (k + 1) * Mathf.PI * 2f / 48f;
                        Vector3 P(float t) => c + up * (3f + Mathf.Sin(t) * Mathf.Cos(t) * 2.4f) + z.side * Mathf.Sin(t) * 2.6f;
                        z.mk.Box(loop, (P(t0) + P(t1)) / 2f, new Vector3(.14f, .14f, Vector3.Distance(P(t0), P(t1)) + .04f), Quaternion.LookRotation(P(t1) - P(t0)));
                    }
                    z.mk.Prism(slate, c - up * .2f, up, .2f, 3f, 6); ArtistBits.PostCollider(z.root, c, 3f, .3f);
                    halos.Add(c + up * 3f, 3f, Ice, .5f); break;
                }
                case "Anima Tempo":
                {
                    foreach (var s in new[] { -1f, 1f }) { Crystal(z.mk, G(s < 0 ? Ice : Gold, 1.4f), c + z.side * s * 1.3f - up * .3f, (up + z.side * s * .25f).normalized, .5f, 5f, s * 20f); ArtistBits.PostCollider(z.root, c + z.side * s * 1.3f, 4f, .55f); }
                    z.mk.Box(pearlM, c + up * .05f, new Vector3(.06f, .02f, 3f), rot);
                    halos.Add(c + up * 3f, 2.6f, Pearl, .45f); break;
                }
                case "DGM":
                {
                    foreach (var s in new[] { -1f, 1f })
                    {
                        var d = c + z.side * s * 1.6f;
                        foreach (var e in new[] { -1f, 1f }) z.mk.Box(stoneLight, d + z.side * e * .75f + up * 1.4f, new Vector3(.25f, 2.8f, .4f), rot);
                        z.mk.Box(stoneLight, d + up * 2.9f, new Vector3(1.8f, .3f, .4f), rot); z.solid.Box(stoneLight, d + up * 1.5f, new Vector3(1.8f, 3f, .4f), rot);
                        if (s < 0) z.mk.Box(slate, d + up * 1.35f + z.front * .1f, new Vector3(1.25f, 2.6f, .08f), rot);
                        else { z.mk.Box(slate, d + up * 1.35f + z.side * .62f + z.front * .6f, new Vector3(.08f, 2.6f, 1.2f), rot); z.mk.Box(G(Gold, 1.4f), d + up * 1.35f, new Vector3(1.25f, 2.6f, .04f), rot); halos.Add(d + up * 1.4f + z.front * .6f, 1.8f, Gold, .55f); }
                    }
                    break;
                }
                case "DVSR":
                {
                    for (var k = 0; k < 6; k++) z.mk.Prism(k % 2 == 0 ? stone : stoneLight, c - up * (k * .45f + .3f), up, 3.6f - k * .5f, .35f, 6, 1f, 30f);
                    z.mk.Prism(G(Cyan, 2f), c - up * 2.9f, up, .6f, .05f, 6, 1f, 30f);
                    halos.Add(c - up * 2f, 2.2f, Cyan, .5f); ArtistBits.PointLight(z.root, c - up * 1.5f, Cyan, 8f, 2f);
                    for (var k = 0; k < 6; k++) { var a = k * Mathf.PI / 3f; var p = c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 3.9f; z.mk.Prism(goldM, ctx.OnGround(p.x, p.z) - up * .2f, up, .06f, 1.2f, 6); ArtistBits.PostCollider(z.root, ctx.OnGround(p.x, p.z), 1f, .1f); }
                    break;
                }
                case "Destiny Potato":
                {
                    var wall = G(new Color(.55f, .7f, 1f), .9f);
                    (float x, float y, float l, bool along)[] walls = { (-2f, 0, 4.2f, true), (2f, 0, 4.2f, true), (0, -2f, 2.8f, false), (0, 2f, 2.8f, false), (-.8f, .6f, 2f, true), (.8f, -.6f, 2f, true), (0, 0, 1.4f, false) };
                    foreach (var (x, y, len, along) in walls)
                    {
                        var p = c + z.side * x + z.front * y + up * .55f; var size = along ? new Vector3(.25f, 1.1f, len) : new Vector3(len, 1.1f, .25f);
                        z.mk.Box(stoneLight, p, size, rot); z.solid.Box(stoneLight, p, size, rot);
                        z.mk.Box(wall, p + up * .58f, new Vector3(size.x, .05f, size.z), rot);
                    }
                    halos.Add(c + up * 1.2f, 3f, Ice, .3f); break;
                }
                case "Fernando Molinari":
                {
                    var desk = c + up * .5f; var deskRot = rot * Quaternion.Euler(-20f, 0, 0);
                    z.mk.Box(slate, c + up * .45f, new Vector3(3.2f, .9f, 1.2f), rot); z.solid.Box(slate, c + up * .5f, new Vector3(3.2f, 1f, 1.2f), rot);
                    z.mk.Box(dark, desk + up * .55f, new Vector3(3f, .1f, 1.1f), deskRot);
                    for (var k = 0; k < 8; k++) { z.mk.Box(G(k % 3 == 0 ? Gold : Ice, 2f), desk + up * .62f + z.side * ((k - 3.5f) * .34f) - z.front * .2f, new Vector3(.18f, .05f, .18f), deskRot); z.mk.Box(metal, desk + up * .62f + z.side * ((k - 3.5f) * .34f) + z.front * .25f, new Vector3(.06f, .08f, .4f), deskRot); }
                    halos.Add(desk + up * 1f, 1.8f, Ice, .45f); break;
                }
                case "Liquid Tension Experiment":
                {
                    var gk = new MeshKit(); var ringA = G(Ice, 2f); var ringB = G(Gold, 2f);
                    for (var k = 0; k < 32; k++)
                    {
                        float a0 = k * Mathf.PI / 16f, a1 = (k + 1) * Mathf.PI / 16f;
                        var p0 = new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0) * 1.8f; var p1 = new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0) * 1.8f;
                        gk.Box(ringA, (p0 + p1) / 2f, new Vector3(.14f, .14f, Vector3.Distance(p0, p1) + .02f), Quaternion.LookRotation(p1 - p0));
                        var q0 = new Vector3(0, Mathf.Sin(a0), Mathf.Cos(a0)) * 1.5f; var q1 = new Vector3(0, Mathf.Sin(a1), Mathf.Cos(a1)) * 1.5f;
                        gk.Box(ringB, (q0 + q1) / 2f, new Vector3(.12f, .12f, Vector3.Distance(q0, q1) + .02f), Quaternion.LookRotation(q1 - q0));
                    }
                    gk.Blob(pearlM, Vector3.zero, Vector3.one * .3f, ctx.Rng, 0f);
                    var go = gk.BuildSingle("Gyroscope", life.transform, false); if (go != null) { go.transform.position = c + up * 3.2f; life.gyros.Add(go.transform); }
                    z.mk.Prism(goldM, c - up * .2f, up, .9f, .6f, 6); z.mk.Prism(metal, c + up * .4f, up, .06f, 1f, 6); ArtistBits.PostCollider(z.root, c, 1f, .9f);
                    halos.Add(c + up * 3.2f, 2.6f, Ice, .45f); break;
                }
                case "Outrun The Sunlight":
                {
                    var red = G(new Color(1f, .25f, .2f), 1.8f);
                    z.mk.Prism(stoneLight, c - up * .2f, up, .15f, 3f, 6); z.mk.Box(stoneLight, c + up * 2.8f, new Vector3(1.4f, .12f, .12f), Quaternion.LookRotation(z.side));
                    var b = c + up * 3.25f;
                    z.mk.Box(red, b, new Vector3(.4f, .45f, .75f), Quaternion.LookRotation(z.side)); z.mk.Box(red, b + up * .32f + z.side * .3f, new Vector3(.3f, .3f, .3f), Quaternion.LookRotation(z.side));
                    z.mk.Prism(goldM, b + up * .32f + z.side * .45f, z.side, .07f, .25f, 4, .05f);
                    foreach (var s in new[] { -1f, 1f }) z.mk.Box(red, b + z.front * s * .4f + up * .05f, new Vector3(.12f, .06f, .9f), Quaternion.LookRotation(z.side) * Quaternion.Euler(0, s * 50f, 20f));
                    ArtistBits.PostCollider(z.root, c, 3f, .25f); halos.Add(b, 1.6f, new Color(1f, .3f, .25f), .55f); break;
                }
                case "The Omnific":
                {
                    var glassM = WorldMaterials.Solid(new Color(.8f, .9f, 1f), .95f);
                    z.mk.Box(goldM, c + up * .5f, new Vector3(3f, 1f, 1.4f), rot); z.solid.Box(goldM, c + up * 1.1f, new Vector3(3f, 2.2f, 1.4f), rot);
                    foreach (var s in new[] { -1f, 1f }) z.mk.Box(glassM, c + up * 1.6f + z.front * s * .68f, new Vector3(3f, 1.2f, .03f), rot);
                    z.mk.Box(goldM, c + up * 2.25f, new Vector3(3.1f, .1f, 1.5f), rot);
                    Color[] gems = { new Color(.4f, .7f, 1f), Gold, new Color(1f, .4f, .5f) };
                    for (var k = 0; k < 3; k++) { var e = c + up * 1.45f + z.side * ((k - 1f) * .95f); z.mk.Prism(stoneLight, e - up * .4f, up, .15f, .2f, 8); z.mk.Blob(G(gems[k], 1.6f), e, new Vector3(.24f, .32f, .24f), ctx.Rng, 0f); halos.Add(e, .8f, gems[k], .55f); }
                    break;
                }
                case "Unprocessed":
                {
                    for (var k = 0; k < 4; k++)
                    {
                        var p = c + z.side * ((k - 1.5f) * 1.7f) + z.front * ((k % 2) * .9f); var h = new[] { 3.6f, 1.6f, 4.4f, 2.2f }[k];
                        z.mk.Prism(stoneLight, ctx.OnGround(p.x, p.z) - up * .2f, up, .5f, h, 6, 1f, k * 12f);
                        z.mk.Prism(k == 2 ? G(Ice, 1.6f) : crystal, ctx.OnGround(p.x, p.z) + up * (h - .25f), (up + z.front * .4f).normalized, .45f, .7f, 6, .2f);
                        ArtistBits.PostCollider(z.root, ctx.OnGround(p.x, p.z), h, .55f);
                    }
                    z.mk.Prism(stoneLight, c + z.front * 2.2f + up * .4f, z.side, .5f, 2.4f, 6);
                    halos.Add(c + up * 3f, 2.4f, Ice, .4f); break;
                }
                default:
                {
                    // anyone new: a crystal cluster on a gold base
                    z.mk.Prism(goldM, c - up * .2f, up, 1.2f, .5f, 6);
                    for (var k = 0; k < 5; k++) Crystal(z.mk, k == 0 ? G(Ice, 1.6f) : crystal, c + new Vector3(ctx.Range(-.6f, .6f), 0, ctx.Range(-.6f, .6f)), (up + new Vector3(ctx.Range(-.3f, .3f), 0, ctx.Range(-.3f, .3f))).normalized, .3f, ctx.Range(2f, 4.5f));
                    ArtistBits.PostCollider(z.root, c, 3f, 1.2f); halos.Add(c + up * 2.5f, 2.4f, Ice, .45f); break;
                }
            }
            ArtistBits.PointLight(z.root, c + up * 3.5f + z.front * 2.5f, new Color(.9f, .95f, 1f), 12f, 1.6f);
        }
    }
}
