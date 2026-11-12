using UnityEngine;

namespace EverythingWorld.World
{
    /// The Math Rock Escarpment's other artists, each with a piece of its song.
    /// Below the escarpment: Explosions In The Sky two stone pillars leaning in until they touch, light in the seam
    /// ("Your Hand In Mine"); This Will Destroy You a still dark pond with a tall pale banner over it ("Quiet", "I Believe
    /// in Your Victory"); Mogwai a wall of amps standing alone in the long grass facing the sea ("Coolverine"); God Is
    /// An Astronaut a telescope on a tripod with a few stars hung above it ("New Years End"); 65daysofstatic a huge moon
    /// half risen behind a bank of earth ("Supermoon": a giant moon climbs the sky); and the rest a standing stone and a
    /// lantern.
    /// Above it: American Football a porch, steps, a lit window upstairs, a bike leaning on the rail ("Wake Her Up");
    /// Covet a little zebra crossing with a blinking signal ("shibuya"); Chon glass bubbles rising and drifting
    /// ("Bubble Dream"); Ichika Nito a crescent moon on a stand beside a guitar on its stand ("The Moon's Elbow");
    /// Elephant Gym a stone elephant under a paper umbrella with a rain cloud over it ("春雨 Spring Rain": rain falls);
    /// LITE a stone well glowing deep down ("Deep Inside"); the rest a bench and a wind chime on their terrace.
    public partial class MathKit
    {
        void Others(Plot z)
        {
            var up = Vector3.up; var ctx = z.ctx; var rot = z.Rot; var c = z.At(-3f, 0);
            string first = null; if (SongIds.TryGetValue(z.a.name, out var l) && l.Length > 0) first = l[0].id;
            Material G(Color col, float s = 1.6f) { if (first != null) z.lit.Add(first); return SongGlow(first, col, s); }
            switch (z.a.name)
            {
                case "Explosions In The Sky":
                {
                    var seam = G(Gold, 2f);
                    foreach (var e in new[] { -1f, 1f }) { var b = c + z.side * e * 2.2f; z.mk.Box(stone, b + up * 3f + z.side * -e * .7f, new Vector3(1.1f, 6.4f, 1f), rot * Quaternion.Euler(0, 0, e * 13f)); ArtistBits.PostCollider(z.root, b, 4f, .6f); }
                    z.mk.Box(seam, c + up * 6.2f, new Vector3(.12f, .8f, .9f), rot);
                    halos.Add(c + up * 6.2f + z.front * .6f, 2.4f, Gold, .6f); ArtistBits.PointLight(z.root, c + up * 5f + z.front * 2f, Gold, 14f, 1.8f);
                    break;
                }
                case "This Will Destroy You":
                {
                    var quiet = Song(z, "Quiet"); var victory = Song(z, "I Believe");
                    z.mk.Prism(stone, c - up * .25f, up, 3.4f, .33f, 16); z.mk.Prism(SongGlow(quiet, new Color(.3f, .4f, .55f), .4f), c + up * .09f, up, 3.1f, .02f, 20);
                    BoxCol(z.root, c, new Vector3(6.6f, .7f, 6.6f), rot);
                    var pole = c - z.front * 3.8f; z.mk.Prism(woodDark, pole - up * .2f, up, .1f, 8f, 6); ArtistBits.PostCollider(z.root, pole, 7.5f, .12f);
                    z.mk.Box(SongGlow(victory, new Color(.95f, .93f, .88f), .9f), pole + up * 6.2f + z.side * .9f, new Vector3(1.7f, 2.6f, .04f), rot);
                    halos.Add(pole + up * 6.2f + z.side * .9f, 2.4f, Mist, .35f);
                    break;
                }
                case "Mogwai":
                {
                    var toSea = new Vector3(sea2.x, 0, sea2.y); var mr = Quaternion.LookRotation(toSea); var msd = Vector3.Cross(up, toSea);
                    var amp = WorldMaterials.Solid(new Color(.12f, .12f, .13f)); var grille = WorldMaterials.Solid(new Color(.22f, .21f, .2f));
                    for (var i = 0; i < 4; i++) for (var j = 0; j < 3; j++) { var p = c + msd * ((i - 1.5f) * 1.5f) + up * (.75f + j * 1.45f); z.mk.Box(amp, p, new Vector3(1.4f, 1.4f, .8f), mr); z.mk.Box(grille, p + toSea * .41f, new Vector3(1.2f, 1.2f, .02f), mr); }
                    z.solid.Box(amp, c + up * 2.2f, new Vector3(6f, 4.4f, .9f), mr);
                    var lamp = G(new Color(1f, .35f, .25f), 2.4f); for (var i = 0; i < 4; i++) z.mk.Blob(lamp, c + msd * ((i - 1.5f) * 1.5f) + up * 4.65f + toSea * .3f, Vector3.one * .07f, ctx.Rng, 0f);
                    halos.Add(c + up * 2.4f + toSea * 1.5f, 3.4f, Warm, .2f);
                    break;
                }
                case "God Is An Astronaut":
                {
                    var tp = c; foreach (var a in new[] { 0f, 120f, 240f }) z.mk.Box(woodDark, tp + up * .7f + Quaternion.Euler(0, a, 0) * Vector3.forward * .35f, new Vector3(.06f, 1.5f, .06f), Quaternion.Euler(0, a, 0) * Quaternion.Euler(15f, 0, 0));
                    var td = (z.front * -.4f + up).normalized; z.mk.Prism(metal, tp + up * 1.3f - td * .9f, td, .16f, 2f, 10, .7f); ArtistBits.PostCollider(z.root, tp, 1.6f, .5f);
                    var star = G(new Color(.9f, .93f, 1f), 2.4f);
                    for (var k = 0; k < 6; k++) { var p = c + up * (5f + (k % 3) * 1.1f) + z.side * ((k - 2.5f) * 1.1f) - z.front * (k % 2) * 1.2f; z.mk.Box(cloth, p + up * .6f, new Vector3(.01f, 1.2f, .01f), rot); z.mk.Prism(star, p, z.front, .25f, .04f, 4, 1f, k * 20f); halos.Add(p, .7f, Mist, .55f); }
                    var hang = c + up * 7.4f; foreach (var e in new[] { -1f, 1f }) { var pp = c + z.side * e * 3.6f; z.mk.Box(woodDark, pp + up * 3.7f, new Vector3(.12f, 7.6f, .12f), Quaternion.identity); ArtistBits.PostCollider(z.root, pp, 7f, .1f); }
                    z.mk.Box(woodDark, hang, new Vector3(7.4f, .08f, .08f), rot);
                    break;
                }
                case "65daysofstatic":
                {
                    var moon = G(new Color(1f, .93f, .75f), 1.3f);
                    var m = c - z.front * 3f; z.mk.Prism(moon, m + up * .5f - z.front * .3f, z.front, 3.6f, .4f, 32);
                    z.mk.Blob(WorldMaterials.Solid(new Color(.38f, .4f, .22f)), m + z.front * .8f + up * .3f, new Vector3(5f, 1.8f, 1.5f), ctx.Rng, .2f);
                    BoxCol(z.root, m + up * 1.2f, new Vector3(7f, 2.6f, 2.4f), rot);
                    halos.Add(m + up * 2f + z.front * .6f, 4.4f, new Color(1f, .93f, .75f), .4f);
                    break;
                }
                case "American Football":
                {
                    var h = c - z.front * 1.5f; var siding = WorldMaterials.Solid(new Color(.85f, .82f, .74f));
                    z.mk.Box(siding, h + up * 2.8f - z.front * 1.2f, new Vector3(6f, 5.6f, .4f), rot); z.solid.Box(siding, h + up * 2.8f - z.front * 1.2f, new Vector3(6f, 5.6f, .5f), rot);
                    var win = G(new Color(1f, .82f, .5f), 1.8f);
                    z.mk.Box(win, h + up * 4.3f - z.front * .98f + z.side * 1.4f, new Vector3(1.1f, 1.2f, .04f), rot); z.mk.Box(WorldMaterials.Solid(new Color(.15f, .14f, .13f)), h + up * 4.3f - z.front * .98f - z.side * 1.4f, new Vector3(1.1f, 1.2f, .04f), rot);
                    z.mk.Box(woodDark, h + up * 1.1f - z.front * .98f, new Vector3(1f, 2.1f, .04f), rot);
                    z.mk.Box(wood, h + up * .35f, new Vector3(5f, .2f, 2.2f), rot); z.solid.Box(wood, h + up * .2f, new Vector3(5f, .5f, 2.2f), rot);
                    for (var k = 0; k < 3; k++) z.mk.Box(wood, h + up * (.1f + k * .1f) + z.front * (1.4f - k * .3f), new Vector3(1.6f, .12f, .4f), rot);
                    foreach (var e in new[] { -1f, 1f }) { z.mk.Box(cloth, h + z.side * e * 2.3f + z.front * .95f + up * 1.6f, new Vector3(.14f, 2.6f, .14f), rot); ArtistBits.PostCollider(z.root, h + z.side * e * 2.3f + z.front * .95f, 2.6f, .1f); }
                    z.mk.Box(woodDark, h + up * 2.95f + z.front * .2f, new Vector3(5.4f, .12f, 2.6f), rot * Quaternion.Euler(10f, 0, 0));
                    z.mk.Box(cloth, h + up * .95f + z.front * 1f, new Vector3(4.4f, .06f, .06f), rot);
                    halos.Add(h + up * 4.3f - z.front * .4f + z.side * 1.4f, 1.6f, Warm, .6f);
                    ArtistBits.PointLight(z.root, h + up * 2.4f + z.front * 1.5f, Warm, 10f, 1.4f);
                    Bike(ctx, z.mk, ctx.OnGround((h + z.side * 3.3f + z.front * 1.4f).x, (h + z.side * 3.3f + z.front * 1.4f).z));
                    break;
                }
                case "Covet":
                {
                    var asph = WorldMaterials.Solid(new Color(.22f, .22f, .24f)); z.mk.Box(asph, c + up * .02f, new Vector3(6f, .05f, 6f), rot);
                    for (var k = 0; k < 6; k++) { z.mk.Box(cloth, c + up * .05f + z.side * ((k - 2.5f) * .9f), new Vector3(.45f, .02f, 2.4f), rot); z.mk.Box(cloth, c + up * .05f + z.front * ((k - 2.5f) * .9f) + z.side * 2.4f, new Vector3(.45f, .02f, 1.2f), rot * Quaternion.Euler(0, 90f, 0)); }
                    var sig = c + z.side * -3.4f + z.front * 2.6f; z.mk.Prism(metal, sig - up * .2f, up, .07f, 3f, 6); ArtistBits.PostCollider(z.root, sig, 2.8f, .1f);
                    z.mk.Box(WorldMaterials.Solid(new Color(.15f, .15f, .15f)), sig + up * 2.6f, new Vector3(.35f, .7f, .25f), rot);
                    var green = G(new Color(.35f, 1f, .6f), 2.4f); z.mk.Box(green, sig + up * 2.75f + z.front * .13f, new Vector3(.22f, .22f, .02f), rot);
                    z.mk.Box(GlowM(new Color(1f, .3f, .25f), .6f), sig + up * 2.45f + z.front * .13f, new Vector3(.22f, .22f, .02f), rot);
                    life.signal = green; halos.Add(sig + up * 2.7f + z.front * .3f, 1f, new Color(.35f, 1f, .6f), .5f);
                    break;
                }
                case "Chon":
                {
                    z.mk.Prism(stone, c - up * .2f, up, .9f, .7f, 10); var bub = G(new Color(.7f, .9f, 1f), .6f);
                    for (var k = 0; k < 9; k++)
                    {
                        var bk = new MeshKit(); bk.Blob(bub, Vector3.zero, Vector3.one * ctx.Range(.25f, .55f), ctx.Rng, 0f);
                        var go = bk.BuildSingle("Bubble", life.transform, false); if (go == null) continue;
                        var p = c + new Vector3(ctx.Range(-1.5f, 1.5f), ctx.Range(1f, 5f), ctx.Range(-1.5f, 1.5f)); go.transform.position = p; life.bubbles.Add(go.transform); life.bubbleAt.Add(c);
                    }
                    ArtistBits.PostCollider(z.root, c, .7f, .9f); halos.Add(c + up * 3f, 3f, new Color(.7f, .9f, 1f), .3f);
                    break;
                }
                case "Ichika Nito":
                {
                    var moon = G(new Color(1f, .93f, .75f), 1.8f); var m = c + up * 3.4f;
                    z.mk.Prism(woodDark, c - up * .2f, up, .07f, 3f, 6); ArtistBits.PostCollider(z.root, c, 2.8f, .1f);
                    for (var k = 0; k < 10; k++) { var a0 = Mathf.Lerp(-1.2f, 1.6f, k / 10f); var a1 = Mathf.Lerp(-1.2f, 1.6f, (k + 1) / 10f); var p0 = m + (z.side * Mathf.Cos(a0) + up * Mathf.Sin(a0)) * 1f; var p1 = m + (z.side * Mathf.Cos(a1) + up * Mathf.Sin(a1)) * 1f; z.mk.Box(moon, (p0 + p1) / 2f, new Vector3(.25f - Mathf.Abs(k - 4.5f) * .03f, .25f - Mathf.Abs(k - 4.5f) * .03f, Vector3.Distance(p0, p1) + .05f), Quaternion.LookRotation(p1 - p0, z.front)); }
                    halos.Add(m, 1.8f, new Color(1f, .93f, .75f), .5f);
                    var gs = c + z.side * 2f; var gbody = WorldMaterials.Solid(new Color(.75f, .5f, .28f));
                    z.mk.Box(metal, gs + up * .3f, new Vector3(.5f, .6f, .4f), rot); z.mk.Blob(gbody, gs + up * .9f, new Vector3(.45f, .55f, .15f), ctx.Rng, 0f); z.mk.Box(woodDark, gs + up * 1.7f, new Vector3(.08f, 1.1f, .05f), rot);
                    ArtistBits.PostCollider(z.root, gs, 1.5f, .35f);
                    break;
                }
                case "Elephant Gym":
                {
                    var grey = WorldMaterials.Solid(new Color(.6f, .6f, .62f)); var e0 = c;
                    z.mk.Blob(grey, e0 + up * 1.3f, new Vector3(1.4f, .9f, .9f), ctx.Rng, .1f); z.mk.Blob(grey, e0 + up * 1.7f + z.side * 1.2f, new Vector3(.6f, .6f, .55f), ctx.Rng, .1f);
                    z.mk.Prism(grey, e0 + up * 1.6f + z.side * 1.65f, (z.side * .3f - up).normalized, .14f, 1.3f, 6, .6f);
                    foreach (var e in new[] { -1f, 1f }) { z.mk.Box(grey, e0 + up * 1.8f + z.side * 1.1f + z.front * e * .45f, new Vector3(.4f, .55f, .06f), rot); foreach (var f in new[] { -.6f, .6f }) z.mk.Prism(grey, e0 + z.side * f + z.front * e * .45f - up * .1f, up, .2f, .9f, 6); }
                    BoxCol(z.root, e0 + up * 1f, new Vector3(3f, 2f, 1.6f), rot);
                    var um = c + z.side * -1.8f; z.mk.Prism(woodDark, um - up * .1f, up, .04f, 2.4f, 6); z.mk.Prism(WorldMaterials.Solid(new Color(.85f, .3f, .25f)), um + up * 2.3f, up, 1.1f, .5f, 10, .05f); ArtistBits.PostCollider(z.root, um, 2.2f, .08f);
                    var cloud = G(new Color(.8f, .85f, .95f), .5f); var cl = c + up * 5.6f;
                    for (var k = 0; k < 4; k++) z.mk.Blob(cloud, cl + z.side * ((k - 1.5f) * .8f) + up * (k % 2) * .3f, new Vector3(.7f, .5f, .6f), ctx.Rng, .2f);
                    life.rainCloud = cl; halos.Add(cl, 2.4f, Mist, .3f);
                    break;
                }
                case "LITE":
                {
                    var deep = G(new Color(.4f, .7f, 1f), 2f);
                    z.mk.Prism(stone, c - up * .2f, up, 1.2f, 1f, 12); z.mk.Prism(deep, c + up * .78f, up, .9f, .02f, 12);
                    foreach (var e in new[] { -1f, 1f }) z.mk.Box(woodDark, c + z.side * e * 1.1f + up * 1.6f, new Vector3(.12f, 2f, .12f), rot);
                    z.mk.Box(woodDark, c + up * 2.6f, new Vector3(2.5f, .12f, .12f), rot); z.mk.Box(cloth, c + up * 1.9f, new Vector3(.02f, 1.4f, .02f), rot); z.mk.Prism(wood, c + up * 1.1f, up, .18f, .3f, 8);
                    ArtistBits.PostCollider(z.root, c, 1f, 1.2f); halos.Add(c + up * 1f, 1.8f, new Color(.4f, .7f, 1f), .55f);
                    ArtistBits.PointLight(z.root, c + up * .5f, new Color(.4f, .7f, 1f), 6f, 2f);
                    break;
                }
                default:
                {
                    if (z.below) { StandingStone(ctx, z.mk, c); Lantern(ctx, z.mk, c + z.side * 1.6f); }
                    else AutumnTree(ctx, z.mk, z.At(-Foot(z.a) * .55f, -2f), ctx.Range(5f, 7f));
                    break;
                }
            }
        }
    }
}
