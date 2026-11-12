using UnityEngine;

namespace EverythingWorld.World
{
    /// The Caldera's other artists (part of CalderaKit):
    /// - Hands Like Houses: the ribs of a great whale to walk through ("Starving To Death In The Belly Of The Whale"), a
    ///   stone lion ("Lion Skin"), strange neon plants ("Introduced Species").
    /// - Holding Absence: a doorway standing alone, full of light ("Afterlife"), rocks floating in a slow ring
    ///   ("Gravity"), a crooked lamp post ("A Crooked Melody").
    /// - Rain City Drive: a shovel in a fresh mound ("Bury a Lie"), a block of blue ice ("Frozen"), a great iron weight
    ///   ("Heavier"). Turnstile: black birds on a wire ("BIRDS"), a neon sign that keeps blacking out ("BLACKOUT"), a stack
    ///   of amps ("NEVER ENOUGH").
    /// - A few of the rest have a piece of their own (a crown for Pierce The Veil, a glowing ring for Spiritbox, neon
    ///   trees for I See Stars, a glass house for ERRA, writing glowing on a wall for Underoath, a fire for Tom Morello,
    ///   a grey orb for Bad Omens, a kneeling figure for I Prevail, a little diner sign for Wendy's); everyone else has an
    ///   obsidian shrine in a chain-link pen, its seam glowing.
    public partial class CalderaKit
    {
        void Others(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var ctx = z.ctx; var c = z.At(-3f, 0);
            switch (z.a.name)
            {
                case "Hands Like Houses":
                {
                    var bone = WorldMaterials.Solid(new Color(.8f, .78f, .72f)); var spine = z.At(-4f, 0); var prevTop = Vector3.zero;
                    for (var k = 0; k < 8; k++)
                    {
                        var p = spine + z.side * ((k - 3.5f) * 1.6f); p.y = ctx.Ground(p.x, p.z);
                        var h = 5f - Mathf.Abs(k - 3.5f) * .45f;
                        // the backbone runs along the row of ribs, joining their tops (it lay across them, cutting the arches)
                        var top = p + up * (h + .1f);
                        if (k > 0) z.mk.Box(bone, (prevTop + top) / 2f, new Vector3(.45f, .45f, Vector3.Distance(prevTop, top) + .45f), Quaternion.LookRotation(top - prevTop));
                        else z.mk.Box(bone, top - z.side * 1.2f, new Vector3(.4f, .4f, 2.4f), Quaternion.LookRotation(z.side));     // the tail end
                        prevTop = top;
                        foreach (var s in new[] { -1f, 1f }) for (var j = 0; j < 6; j++) { var a0 = j / 6f * Mathf.PI * .5f; var a1 = (j + 1) / 6f * Mathf.PI * .5f; var p0 = p + z.front * s * Mathf.Cos(a0) * 3f + up * Mathf.Sin(a0) * h; var p1 = p + z.front * s * Mathf.Cos(a1) * 3f + up * Mathf.Sin(a1) * h; z.mk.Box(bone, (p0 + p1) / 2f, new Vector3(.2f, .2f, Vector3.Distance(p0, p1) + .05f), Quaternion.LookRotation(p1 - p0)); }
                        foreach (var s in new[] { -1f, 1f }) ArtistBits.PostCollider(z.root, p + z.front * s * 2.9f, 1.5f, .25f);
                    }
                    var belly = SongGlow(z, "Starving", Cyan, 1.2f); z.mk.Prism(belly, spine + up * .04f, up, 2.6f, .04f, 18); halos.Add(spine + up * 2f, 4f, Cyan, .25f);
                    var lion = z.At(5f, -7f); var st = WorldMaterials.Solid(new Color(.35f, .33f, .35f));
                    z.mk.Box(st, lion + up * .5f, new Vector3(1.6f, 1f, 2.6f), rot); z.mk.Blob(st, lion + up * 1.4f + z.front * .7f, new Vector3(.7f, .8f, .7f), ctx.Rng, .15f); z.mk.Blob(SongGlow(z, "Lion Skin", new Color(1f, .75f, .3f), 1.4f), lion + up * 1.5f + z.front * .9f, new Vector3(1f, 1f, .5f), ctx.Rng, .3f); z.solid.Box(st, lion + up * 1f, new Vector3(1.8f, 2f, 2.8f), rot);
                    var pl = z.At(5f, 7f); var alien = SongGlow(z, "Introduced", Magenta, 1.8f);
                    for (var k = 0; k < 7; k++) { var p = pl + new Vector3(Mathf.Cos(k * 2.3f), 0, Mathf.Sin(k * 2.3f)) * (.5f + k * .2f); p.y = ctx.Ground(p.x, p.z); var hh = ctx.Range(1f, 2.2f); z.mk.Prism(WorldMaterials.Solid(new Color(.15f, .2f, .25f)), p, up, .06f, hh, 5); z.mk.Blob(alien, p + up * hh, new Vector3(.3f, .2f, .3f), ctx.Rng, .3f); ArtistBits.PostCollider(z.root, p, hh, .35f); }
                    halos.Add(pl + up * 1.6f, 2f, Magenta, .4f);
                    break;
                }
                case "Holding Absence":
                {
                    var dr = z.At(-5f, 0); var light = SongGlow(z, "Afterlife", Ink, 2.6f);
                    foreach (var s in new[] { -1f, 1f }) { z.mk.Box(obsidian, dr + z.side * s * 1.2f + up * 2f, new Vector3(.4f, 4f, .5f), rot); z.solid.Box(obsidian, dr + z.side * s * 1.2f + up * 2f, new Vector3(.4f, 4f, .5f), rot); }
                    z.mk.Box(obsidian, dr + up * 4.2f, new Vector3(2.8f, .4f, .5f), rot); z.mk.Box(light, dr + up * 2f, new Vector3(2f, 4f, .06f), rot);
                    halos.Add(dr + up * 2f + z.front * .8f, 3.4f, Ink, .6f); ArtistBits.PointLight(z.root, dr + up * 2f + z.front * 3f, new Color(.9f, .95f, 1f), 12f, 1.8f); life.afterlife = dr + up * 2f;
                    var gc = z.At(3f, -7f) + up * 2.6f; var grav = SongGlow(z, "Gravity", Violet, 1.4f);
                    for (var k = 0; k < 6; k++) { var rk = new MeshKit(); rk.Blob(k % 2 == 0 ? obsidian : grav, Vector3.zero, Vector3.one * ctx.Range(.35f, .7f), ctx.Rng, .3f); var go = rk.BuildSingle("Floating rock", life.transform, false); if (go != null) { life.floaters.Add(go.transform); life.floatAt.Add(gc); } }
                    halos.Add(gc, 2.4f, Violet, .3f);
                    var cl = z.At(3f, 7f); z.mk.Prism(dark, cl, (up + z.side * .25f).normalized, .08f, 3.4f, 6); var head = cl + (up + z.side * .25f).normalized * 3.4f;
                    z.mk.Box(SongGlow(z, "A Crooked", new Color(1f, .85f, .55f), 2.6f), head - up * .15f, new Vector3(.3f, .3f, .3f), Quaternion.Euler(0, 0, -20f)); halos.Add(head, 1.4f, new Color(1f, .85f, .55f), .6f); ArtistBits.PostCollider(z.root, cl, 3f, .15f);
                    break;
                }
                case "Rain City Drive":
                {
                    var md = z.At(-4f, -2f); z.mk.Blob(ashDark, md + up * .2f, new Vector3(1.4f, .5f, 2f), ctx.Rng, .2f);
                    z.mk.Box(steel, md + up * 1.2f + z.side * .4f, new Vector3(.06f, 1.6f, .06f), Quaternion.Euler(0, 0, -12f)); z.mk.Box(steel, md + up * .45f + z.side * .25f, new Vector3(.35f, .45f, .05f), Quaternion.Euler(0, 0, -12f));
                    z.mk.Prism(SongGlow(z, "Bury a Lie", Magenta, 1.4f), md + up * .02f, up, 1.8f, .03f, 14); z.solid.Box(ashDark, md + up * .4f, new Vector3(2.6f, .8f, 3.6f), rot);
                    var ice = z.At(3f, -7f); z.mk.Box(SongGlow(z, "Frozen", new Color(.35f, .7f, 1f), .7f), ice + up * 1.1f, new Vector3(2f, 2.2f, 2f), rot * Quaternion.Euler(0, 20f, 6f)); z.mk.Box(WorldMaterials.Solid(new Color(.75f, .9f, 1f), .95f), ice + up * 2.25f, new Vector3(2.1f, .1f, 2.1f), rot * Quaternion.Euler(0, 20f, 6f)); halos.Add(ice + up * 1.2f, 2.2f, new Color(.6f, .9f, 1f), .4f); z.solid.Box(steel, ice + up * 1.1f, new Vector3(2.4f, 2.2f, 2.4f), rot);
                    var wt = z.At(3f, 7f); var iron = WorldMaterials.Solid(new Color(.12f, .12f, .14f), .6f); z.mk.Prism(iron, wt, up, 1.4f, 2.2f, 8, .55f); z.mk.Box(iron, wt + up * 2.6f, new Vector3(.9f, .6f, .3f), rot);
                    z.mk.Box(SongGlow(z, "Heavier", Cyan, 1.6f), wt + up * 1.2f + z.front * 1.02f, new Vector3(.8f, .2f, .02f), rot); z.solid.Prism(iron, wt, up, 1.4f, 2.8f, 8);
                    break;
                }
                case "Turnstile":
                {
                    var wl = z.At(-4f, 0); foreach (var s in new[] { -1f, 1f }) { z.mk.Prism(dark, wl + z.side * s * 4f, up, .1f, 5f, 6); ArtistBits.PostCollider(z.root, wl + z.side * s * 4f, 5f, .15f); }
                    z.mk.Box(dark, wl + up * 4.6f, new Vector3(8f, .03f, .03f), Quaternion.LookRotation(z.side) * Quaternion.Euler(0, 90f, 0));
                    var bird = WorldMaterials.Solid(new Color(.04f, .04f, .05f));
                    for (var k = 0; k < 9; k++) { var b = wl + z.side * ((k - 4f) * .8f) + up * 4.85f; z.mk.Blob(bird, b, new Vector3(.15f, .2f, .25f), ctx.Rng, 0f); z.mk.Blob(bird, b + up * .22f + z.front * .08f, Vector3.one * .1f, ctx.Rng, 0f); }
                    z.mk.Box(SongGlow(z, "BIRDS", Ink, 1.2f), wl + up * 4.6f + z.front * .05f, new Vector3(8f, .02f, .02f), Quaternion.LookRotation(z.side) * Quaternion.Euler(0, 90f, 0)); life.birdsAt = wl + up * 8f;
                    var sg = z.At(3f, -7f); z.mk.Box(dark, sg + up * 2.6f, new Vector3(4f, 1.2f, .3f), rot); z.mk.Prism(steel, sg + z.side * 1.6f, up, .08f, 2f, 6); z.mk.Prism(steel, sg - z.side * 1.6f, up, .08f, 2f, 6);
                    var bo = SongGlow(z, "BLACKOUT", Cyan, 2.6f); z.mk.Box(bo, sg + up * 2.6f + z.front * .16f, new Vector3(3.6f, .9f, .02f), rot); life.blackoutSign = bo;
                    ArtistBits.Label(z.labels, "BLACKOUT", sg + up * 2.6f + z.front * .18f, z.front, .32f, new Color(.05f, .05f, .05f), TextAnchor.MiddleCenter); z.solid.Box(dark, sg + up * 1.6f, new Vector3(4f, 3.2f, .4f), rot);
                    var am = z.At(3f, 7f); for (var k = 0; k < 4; k++) { var p = am + up * (.7f + k * 1.4f); z.mk.Box(dark, p, new Vector3(1.4f, 1.4f, .8f), rot); z.mk.Box(grille, p + z.front * .41f, new Vector3(1.2f, 1.2f, .02f), rot); }
                    z.mk.Blob(SongGlow(z, "NEVER ENOUGH", Magenta, 3f), am + up * 5.5f + z.front * .42f + z.side * .5f, Vector3.one * .07f, ctx.Rng, 0f); z.solid.Box(dark, am + up * 2.8f, new Vector3(1.5f, 5.6f, .9f), rot);
                    break;
                }
                case "Pierce The Veil":
                    z.mk.Box(obsidian, c + up * .8f, new Vector3(1.4f, 1.6f, 1.4f), rot); z.solid.Box(obsidian, c + up * .8f, new Vector3(1.4f, 1.6f, 1.4f), rot);
                    { var g = SongGlow(z, "King", new Color(1f, .8f, .3f), 1.8f); z.mk.Prism(g, c + up * 1.6f, up, .55f, .35f, 12); for (var k = 0; k < 7; k++) z.mk.Cone(g, c + up * 1.95f + Quaternion.Euler(0, k * 51f, 0) * Vector3.forward * .5f, .12f, .45f, 4); halos.Add(c + up * 2f, 1.4f, new Color(1f, .8f, .3f), .5f); }
                    break;
                case "Spiritbox":
                    { var g = SongGlow(z, "Perfect", new Color(.55f, .85f, 1f), 2.2f); for (var k = 0; k < 32; k++) { float a0 = k * Mathf.PI / 16f, a1 = (k + 1) * Mathf.PI / 16f; var p0 = c + up * 2.2f + (z.side * Mathf.Cos(a0) + up * Mathf.Sin(a0)) * 1.8f; var p1 = c + up * 2.2f + (z.side * Mathf.Cos(a1) + up * Mathf.Sin(a1)) * 1.8f; z.mk.Box(g, (p0 + p1) / 2f, new Vector3(.12f, .12f, Vector3.Distance(p0, p1)), Quaternion.LookRotation(p1 - p0, z.front)); } }
                    z.mk.Box(dark, c + up * .5f, new Vector3(1f, .6f, .6f), rot); z.mk.Box(GlowM(new Color(.55f, .85f, 1f), 2f), c + up * .55f + z.front * .31f, new Vector3(.5f, .2f, .02f), rot); z.solid.Box(dark, c + up * .5f, new Vector3(1f, 1f, .6f), rot); halos.Add(c + up * 2.2f, 2.4f, new Color(.55f, .85f, 1f), .35f);
                    break;
                case "I See Stars":
                    for (var k = 0; k < 5; k++) { var p = c + new Vector3(Mathf.Cos(k * 1.3f), 0, Mathf.Sin(k * 1.3f)) * (1f + k * .5f); p.y = ctx.Ground(p.x, p.z); var g = SongGlow(z, "Electric", k % 2 == 0 ? Cyan : Magenta, 1.6f); z.mk.Prism(dark, p, up, .12f, 2.4f, 6); z.mk.Cone(g, p + up * 2f, 1f, 2.4f, 6); ArtistBits.PostCollider(z.root, p, 2f, .2f); }
                    halos.Add(c + up * 3f, 3f, Cyan, .3f);
                    break;
                case "ERRA":
                    z.mk.Box(SongGlow(z, "House of Glass", new Color(.7f, .9f, 1f), .8f), c + up * 1.6f, new Vector3(2.6f, 3.2f, 2.6f), rot); foreach (var a in new[] { -1.3f, 1.3f }) foreach (var b in new[] { -1.3f, 1.3f }) z.mk.Box(dark, c + z.side * a + z.front * b + up * 1.6f, new Vector3(.08f, 3.25f, .08f), rot);
                    z.solid.Box(dark, c + up * 1.6f, new Vector3(2.7f, 3.2f, 2.7f), rot); halos.Add(c + up * 1.6f, 2.6f, new Color(.7f, .9f, 1f), .3f);
                    break;
                case "Underoath":
                    z.mk.Box(obsidian, c + up * 1.5f, new Vector3(5f, 3f, .5f), rot); z.solid.Box(obsidian, c + up * 1.5f, new Vector3(5f, 3f, .5f), rot);
                    { var g = SongGlow(z, "Writing", Ink, 2f); for (var r = 0; r < 5; r++) for (var k = 0; k < 6; k++) z.mk.Box(g, c + up * (.7f + r * .5f) + z.side * ((k - 2.5f) * .75f) + z.front * .26f, new Vector3(ctx.Range(.25f, .6f), .06f, .02f), rot * Quaternion.Euler(0, 0, ctx.Range(-8f, 8f))); halos.Add(c + up * 1.6f + z.front * .8f, 3f, Ink, .25f); }
                    break;
                case "Tom Morello":
                    z.mk.Prism(dark, c, up, .9f, .7f, 10, 1.3f); z.mk.Blob(SongGlow(z, "Everything Burns", new Color(1f, .3f, .6f), 3f), c + up * .9f, new Vector3(.8f, .5f, .8f), ctx.Rng, .3f); life.fires.Add(c + up * 1f); ArtistBits.PostCollider(z.root, c, 1f, 1.1f); halos.Add(c + up * 1.4f, 2f, Magenta, .6f);
                    break;
                case "Bad Omens":
                    z.mk.Prism(obsidian, c, up, .8f, 1.2f, 8); z.mk.Blob(SongGlow(z, "The Grey", new Color(.75f, .75f, .8f), 1.4f), c + up * 2.2f, Vector3.one * .9f, ctx.Rng, 0f); halos.Add(c + up * 2.2f, 2f, new Color(.8f, .8f, .85f), .4f); z.solid.Prism(obsidian, c, up, .8f, 2.8f, 8);
                    break;
                case "I Prevail":
                    { var st = WorldMaterials.Solid(new Color(.3f, .29f, .32f)); z.mk.Box(st, c + up * .4f, new Vector3(.6f, .8f, 1.1f), rot); z.mk.Box(st, c + up * 1.1f - z.front * .1f, new Vector3(.6f, .9f, .4f), rot * Quaternion.Euler(30f, 0, 0)); z.mk.Blob(st, c + up * 1.5f + z.front * .25f, Vector3.one * .22f, ctx.Rng, 0f);
                      z.mk.Prism(SongGlow(z, "Bow Down", Violet, 1.8f), c + up * .03f, up, 1.4f, .04f, 14); ArtistBits.PostCollider(z.root, c, 1.6f, .6f); halos.Add(c + up * .8f, 1.6f, Violet, .4f); }
                    break;
                case "Wendy's":
                    z.mk.Prism(steel, c, up, .08f, 3f, 6); z.mk.Box(GlowM(new Color(1f, .3f, .25f), 2f), c + up * 3.3f, new Vector3(2.2f, .8f, .2f), rot); z.mk.Box(SongGlow(z, "The Best Combo", new Color(1f, .85f, .4f), 2.6f), c + up * 3.3f + z.front * .11f, new Vector3(2f, .2f, .02f), rot);
                    ArtistBits.Label(z.labels, "OPEN LATE", c + up * 3.42f + z.front * .13f, z.front, .2f, Ink, TextAnchor.MiddleCenter); ArtistBits.PostCollider(z.root, c, 3f, .2f); halos.Add(c + up * 3.3f, 2f, new Color(1f, .5f, .3f), .5f);
                    break;
                default:
                    Shrine(z, c);
                    break;
            }
        }

        /// An obsidian shrine in a chain-link pen: a faceted black monolith split by a glowing seam (the artist's first
        /// song), neon shards at its foot.
        /// A shrine for a smaller artist, one of five kinds (picked by name, so neighbours differ), lit so it reads in
        /// the dark: a black monolith split by a glowing seam; an obsidian ring standing on edge with a neon rim; a
        /// throne of big neon crystals; a boulder floating over a pool of light, chained down; a totem of amps crowned
        /// with neon. The glow is the artist's first song.
        void Shrine(Plot z, Vector3 c)
        {
            var up = Vector3.up; var rot = z.Rot; var ctx = z.ctx;
            var hash = Mathf.Abs(z.a.name.GetHashCode());
            Color[] cols = { Cyan, Violet, Magenta }; var col = cols[hash % 3];
            string first = null; if (SongIds.TryGetValue(z.a.name, out var list) && list.Length > 0) { first = list[0].id; z.lit.Add(first); }
            var glow = SongGlow(first, col, 2.4f);
            switch ((hash / 3) % 5)
            {
                case 0:   // the monolith
                {
                    var h = ctx.Range(4.2f, 5.6f);
                    z.mk.Prism(obsidian, c - up * .2f, up, 1f, h, 5, .35f, ctx.Range(0, 70f)); z.mk.Box(glow, c + up * h * .45f + z.front * .52f, new Vector3(.12f, h * .75f, .06f), rot * Quaternion.Euler(0, 0, ctx.Range(-6f, 6f)));
                    z.solid.Prism(obsidian, c, up, 1f, h, 5); halos.Add(c + up * h * .5f + z.front * 1f, 2.4f, col, .55f);
                    break;
                }
                case 1:   // the ring on edge
                {
                    var rc = c + up * 2.6f;
                    for (var k = 0; k < 20; k++) { float a0 = k * Mathf.PI / 10f, a1 = (k + 1) * Mathf.PI / 10f; var p0 = rc + (z.side * Mathf.Cos(a0) + up * Mathf.Sin(a0)) * 2.4f; var p1 = rc + (z.side * Mathf.Cos(a1) + up * Mathf.Sin(a1)) * 2.4f; z.mk.Box(obsidian, (p0 + p1) / 2f, new Vector3(.5f, .5f, Vector3.Distance(p0, p1) + .05f), Quaternion.LookRotation(p1 - p0, z.front)); if (Mathf.Sin((a0 + a1) / 2f) < -.1f) z.solid.Box(obsidian, (p0 + p1) / 2f, new Vector3(.5f, .5f, Vector3.Distance(p0, p1) + .05f), Quaternion.LookRotation(p1 - p0, z.front)); /* the low arc of the ring: knee to waist high */ var q0 = rc + (z.side * Mathf.Cos(a0) + up * Mathf.Sin(a0)) * 2.05f + z.front * .2f; var q1 = rc + (z.side * Mathf.Cos(a1) + up * Mathf.Sin(a1)) * 2.05f + z.front * .2f; z.mk.Box(glow, (q0 + q1) / 2f, new Vector3(.1f, .1f, Vector3.Distance(q0, q1) + .02f), Quaternion.LookRotation(q1 - q0, z.front)); }
                    foreach (var e in new[] { -1f, 1f }) ArtistBits.PostCollider(z.root, c + z.side * e * 2.3f, 2f, .4f);
                    z.mk.Prism(glow, c + up * .03f, up, 1.2f, .04f, 16); halos.Add(rc + z.front * .6f, 3f, col, .4f);
                    break;
                }
                case 2:   // the crystal throne
                {
                    z.mk.Box(obsidian, c + up * .5f, new Vector3(1.4f, 1f, 1.4f), rot); z.mk.Box(obsidian, c + up * 1.6f - z.front * .6f, new Vector3(1.4f, 2.2f, .3f), rot);
                    for (var k = 0; k < 7; k++) { var a = k * Mathf.PI * 2f / 7f; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)); var len = ctx.Range(2.2f, 3.8f); z.mk.Prism(glow, c + d * 1.4f - up * .2f, (up + d * .35f).normalized, .3f, len, 6, .05f); }
                    z.solid.Box(obsidian, c + up * 1f, new Vector3(3.6f, 2f, 3.6f), rot); halos.Add(c + up * 1.8f, 3f, col, .5f);
                    break;
                }
                case 3:   // the floating boulder
                {
                    var bc = c + up * (2.6f + .2f * Mathf.Sin(hash)); z.mk.Blob(obsidian, bc, new Vector3(1.2f, .9f, 1.1f), ctx.Rng, .3f);
                    z.mk.Prism(glow, c + up * .03f, up, 1.5f, .05f, 16); z.mk.Blob(glow, bc - up * .85f, new Vector3(.5f, .12f, .5f), ctx.Rng, .1f);
                    for (var k = 0; k < 3; k++) { var a = k * Mathf.PI * 2f / 3f; var g = c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 2.2f; g.y = ctx.Ground(g.x, g.z); z.mk.Box(glow, (g + bc) / 2f, new Vector3(.09f, .09f, Vector3.Distance(g, bc)), Quaternion.LookRotation(bc - g)); z.mk.Prism(obsidian, g - up * .1f, up, .2f, .4f, 6); }   // neon chains: in the dark, steel ones vanished and the boulder read as a stray shape in the air
                    ArtistBits.PostCollider(z.root, c, 1.2f, 1.6f); halos.Add(c + up * 1.3f, 2.6f, col, .5f);
                    break;
                }
                default:   // the amp totem
                {
                    for (var k = 0; k < 4; k++) { var p = c + up * (.7f + k * 1.35f); var r2 = rot * Quaternion.Euler(0, k * 17f, 0); z.mk.Box(dark, p, new Vector3(1.3f - k * .1f, 1.3f, .8f), r2); z.mk.Box(grille, p + r2 * Vector3.forward * .41f, new Vector3(1.1f - k * .1f, 1.1f, .02f), r2); z.mk.Box(glow, p + up * .66f, new Vector3(1.32f - k * .1f, .04f, .82f), r2); }
                    for (var k = 0; k < 5; k++) z.mk.Cone(glow, c + up * 6.1f + Quaternion.Euler(0, k * 72f, 0) * Vector3.forward * .4f, .14f, .7f, 4);
                    z.solid.Box(dark, c + up * 2.8f, new Vector3(1.4f, 5.6f, 1f), rot); halos.Add(c + up * 4f, 2.6f, col, .45f);
                    break;
                }
            }
        }
    }
}
