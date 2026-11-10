using UnityEngine;

namespace EverythingWorld.World
{
    /// Modern bluegrass and the classics (part of BluegrassKit):
    /// - Billy Strings: a big night-show stage in the hollow, a black deck under a truss, a wall of glowing panels
    ///   cycling blue, violet, amber and teal, a giant glowing banjo hung in the middle and two rings of light
    ///   swirling round it (faster with the music); beside it a little stone jailhouse with one lit, barred window
    ///   ("Dust in a Baggie"), a dark mire pool glowing at the edge with stepping stones leading away from it
    ///   ("Away From The Mire"), and trees with crowns of crumpled tinfoil glinting ("Turmoil & Tinfoil"); each
    ///   glows brighter while its song plays.
    /// - The classics: Bill Monroe's mandolin gets a little round stage with a vintage microphone and footlights
    ///   ("Uncle Pen"); the Blue Moon of Kentucky hangs over a horse paddock; Doc Watson's black mountain gets a
    ///   cabin porch with a guitar on a rocker and a lit window ("Black Mountain Rag"); Tony Rice's chapel a round
    ///   window over the door ("Church Street Blues"); mist lies round Ricky Skaggs, Del McCoury and Flatt &amp;
    ///   Scruggs.
    public partial class BluegrassKit
    {
        public const string AwayFromTheMire = "6oPDKGwBri4OLQX5IYu02T", DustInABaggie = "2Lk8XTulavagQOrYqFpGB8", TurmoilTinfoil = "3kipE3F3ghfvZHPxDvbNQF",
            UnclePen = "5jNVwNjg6cDCHUUpNV7ven", BlackMountainRag = "5Ys3TeaxdroLXxWWjI2Z68", UnclePenSkaggs = "70OFRF0sbCGQRQR0iAIZSX",
            Vincent = "3ve9f8UWOEZOPbIasGDVqr", ChurchStreet = "6fbgWiXF0bNlihr7s1eIrX";

        /// A glow material that brightens and breathes while its song plays (BluegrassLife).
        Material SongGlow(string song, Color c, float s = 1.6f)
        {
            var m = new Material(GlowM(c, s)) { name = "Song glow" }; life.songGlows.Add((song, m, c * (s * .65f))); return m;
        }

        void Billy(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var c = z.At(-10f, 0); var ctx = z.ctx;
            var black = WorldMaterials.Solid(new Color(.07f, .07f, .09f)); var truss = WorldMaterials.Solid(new Color(.55f, .56f, .6f), .8f);
            Color blue = new Color(.3f, .5f, 1f), violet = new Color(.62f, .35f, 1f), amber = new Color(1f, .72f, .2f), teal = new Color(.2f, .9f, .9f);
            // the deck, steps, the truss
            z.mk.Box(black, c + up * .6f, new Vector3(14f, 1.2f, 7f), rot); z.solid.Box(black, c + up * .6f, new Vector3(14f, 1.2f, 7f), rot);
            z.mk.Box(black, c + z.front * 3.9f + up * .3f, new Vector3(3f, .6f, .8f), rot); z.solid.Box(black, c + z.front * 3.9f + up * .3f, new Vector3(3f, .6f, .8f), rot);
            foreach (var s in new[] { -1f, 1f })
            {
                var tw = c + z.side * s * 7.4f + z.front * 2.6f;
                foreach (var a in new[] { -.25f, .25f }) foreach (var b in new[] { -.25f, .25f }) z.mk.Box(truss, tw + z.side * a + z.front * b + up * 4.6f, new Vector3(.08f, 9.2f, .08f), rot);
                for (var y = .5f; y < 9f; y += .9f) z.mk.Box(truss, tw + up * y, new Vector3(.55f, .06f, .55f), rot);
                ArtistBits.PostCollider(z.root, tw, 9f, .4f);
            }
            foreach (var b in new[] { -.25f, .25f }) foreach (var y in new[] { 9f, 9.5f }) z.mk.Box(truss, c + z.front * (2.6f + b) + up * y, new Vector3(15.3f, .08f, .08f), rot);
            for (var x = -7f; x <= 7f; x += 1f) z.mk.Box(truss, c + z.front * 2.6f + z.side * x + up * 9.25f, new Vector3(.06f, .5f, .55f), rot);
            // stage lights on the truss
            for (var k = 0; k < 6; k++)
            {
                var lp = c + z.front * 2.6f + z.side * (-5f + k * 2f) + up * 8.7f; var col = k % 2 == 0 ? blue : amber;
                z.mk.Prism(black, lp, -up, .22f, .5f, 8); z.mk.Prism(GlowM(col, 3f), lp - up * .5f, -up, .2f, .03f, 8); halos.Add(lp - up * .6f, 1f, col, .7f);
            }
            // the panel wall, cycling colours (BluegrassLife)
            var back = c - z.front * 3.2f;
            z.mk.Box(black, back + up * 4.8f, new Vector3(13.4f, 7.2f, .3f), rot); z.solid.Box(black, back + up * 4.8f, new Vector3(13.4f, 7.2f, .3f), rot);
            Color[] cols = { blue, violet, amber, teal }; var mats = new Material[4];
            for (var k = 0; k < 4; k++) { mats[k] = new Material(GlowM(cols[k], 1.4f)) { name = "Billy panel" }; life.panels.Add((mats[k], cols[k] * .45f, k * 1.6f)); }
            for (var col = 0; col < 6; col++) for (var row = 0; row < 4; row++)
                z.mk.Box(mats[(col + row * 3) % 4], back + z.front * .17f + z.side * (-5.5f + col * 2.2f) + up * (2f + row * 1.6f), new Vector3(2.05f, 1.45f, .04f), rot);
            // the glowing banjo and the swirls of light round it
            var bc = c - z.front * 1.6f + up * 6.2f;
            Banjo(z.mk, bc, rot * Quaternion.Euler(0, 0, -14f), 2.2f, true);
            halos.Add(bc + z.front * .4f, 4f, blue, .45f);
            for (var k = 0; k < 2; k++)
            {
                var sk = new MeshKit(); var m = GlowM(k == 0 ? new Color(.4f, .65f, 1f) : amber, 3f);
                for (var i = 0; i < 40; i++) { var a = i * 9f * Mathf.Deg2Rad; var r = 3f + k * .9f + Mathf.Sin(i * .45f + k) * .35f; sk.Blob(m, new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0), Vector3.one * (.1f + .05f * (i % 3)), ctx.Rng, 0f); }
                var go = sk.BuildSingle("Billy swirl", life.transform, false); go.transform.SetPositionAndRotation(bc + z.front * (.6f + k * .3f), rot);
                life.swirls.Add((go.transform, k == 0 ? 1f : -1.3f));
            }
            // amps, monitors, mic stands
            foreach (var s in new[] { -1f, 1f })
            {
                var ap = c + z.side * s * 5.4f - z.front * 1.8f + up * 1.2f;
                z.mk.Box(black, ap + up * 1f, new Vector3(1.4f, 2f, .9f), rot); z.mk.Box(WorldMaterials.Solid(new Color(.2f, .2f, .22f)), ap + up * 1f + z.front * .47f, new Vector3(1.2f, 1.7f, .02f), rot);
                z.solid.Box(black, ap + up * 1f, new Vector3(1.4f, 2f, .9f), rot);
            }
            for (var k = -1; k <= 1; k++)
            {
                var mp = c + z.side * k * 3f + z.front * 1.8f + up * 1.2f; z.mk.Prism(truss, mp, up, .03f, 1.5f, 6); z.mk.Blob(black, mp + up * 1.55f, Vector3.one * .07f, ctx.Rng, 0f);
                z.mk.Box(black, mp + z.front * 1.1f + up * .2f, new Vector3(.8f, .4f, .5f), rot * Quaternion.Euler(-25f, 0, 0));
            }
            var key = ArtistBits.PointLight(z.root, c + up * 6f + z.front * 7f, blue, 22f, 2.2f); z.area.glowLight = key;
            ArtistBits.PointLight(z.root, c + up * 3f + z.front * 5f + z.side * 5f, amber, 14f, 1.4f);
            ArtistBits.PointLight(z.root, c + up * 3f + z.front * 5f - z.side * 5f, violet, 14f, 1.4f);
            var mark = new GameObject("Shot: Billy Strings' stage").transform; mark.SetParent(ctx.Root, false);
            var cam = z.At(8f, 2f); mark.SetPositionAndRotation(cam, Quaternion.LookRotation((bc - up * 1.5f - cam - up * 1.5f).normalized));

            // Dust in a Baggie: the jailhouse with one lit, barred window
            var j = z.At(-1f, -14f); var jr = rot; var jf = z.front;
            z.mk.Box(stone, j + up * 1.7f, new Vector3(4.4f, 3.4f, 3.8f), jr); z.solid.Box(stone, j + up * 1.7f, new Vector3(4.4f, 3.4f, 3.8f), jr);
            z.mk.Box(roofM, j + up * 3.5f, new Vector3(4.8f, .25f, 4.2f), jr);
            for (var y = .4f; y < 3.3f; y += .5f) z.mk.Box(WorldMaterials.Solid(new Color(.5f, .49f, .46f)), j + up * y + jf * 1.91f, new Vector3(4.4f, .04f, .02f), jr);
            z.mk.Box(metal, j + jf * 1.92f - z.side * 1f + up * 1.05f, new Vector3(1f, 2.1f, .05f), jr);
            var win = j + jf * 1.92f + z.side * .9f + up * 2.1f;
            z.mk.Box(dark, win, new Vector3(1.1f, .9f, .03f), jr); z.mk.Box(SongGlow(DustInABaggie, amber, 2f), win + jf * .01f, new Vector3(.9f, .7f, .03f), jr);
            for (var b = -2; b <= 2; b++) z.mk.Box(dark, win + jf * .05f + z.side * b * .18f, new Vector3(.05f, .8f, .05f), jr);
            halos.Add(win + jf * .4f, 1.2f, amber, .5f);
            z.mk.Box(cream, j + jf * 1.93f - z.side * 1f + up * 2.6f, new Vector3(1.4f, .45f, .03f), jr);
            ArtistBits.Label(z.labels, "JAIL", j + jf * 1.96f - z.side * 1f + up * 2.6f, jf, .3f, new Color(.15f, .12f, .1f), TextAnchor.MiddleCenter);
            ArtistBits.PointLight(z.root, win + jf * 2f, amber, 8f, 1f);

            // Away From The Mire: a dark pool glowing at its edge, stepping stones leading away
            var mc = z.At(-2f, 14f); var mireGlow = SongGlow(AwayFromTheMire, new Color(.2f, .85f, .65f), 1.2f);
            var mire = new Material(WorldMaterials.Glow(new Color(.03f, .06f, .05f), .05f)) { name = "Mire" }; mire.SetFloat("_Smoothness", .95f);
            for (var k = 0; k < 24; k++)
            {
                float a0 = k * Mathf.PI / 12f, a1 = (k + 1) * Mathf.PI / 12f;
                Vector3 E(float a, float r) => z.side * Mathf.Cos(a) * r * 1.3f + z.front * Mathf.Sin(a) * r;
                z.mk.Tri(mireGlow, mc + up * .04f, mc + up * .04f + E(a0, 3.1f), mc + up * .04f + E(a1, 3.1f), mc - up);
                z.mk.Tri(mire, mc + up * .07f, mc + up * .07f + E(a0, 2.6f), mc + up * .07f + E(a1, 2.6f), mc - up);
            }
            for (var k = 0; k < 4; k++) { var sp = z.At(1f + k * 1.4f, 14f - k * .6f); z.mk.Prism(stone, sp - up * .1f, up, .45f, .28f, 7); }
            for (var k = 0; k < 12; k++) { var a = k * 30f * Mathf.Deg2Rad; var rp = mc + z.side * Mathf.Cos(a) * 4.4f + z.front * Mathf.Sin(a) * 3.3f; rp = ctx.OnGround(rp.x, rp.z); crowns.Box(WorldMaterials.Solid(new Color(.28f, .4f, .22f)), rp + up * .7f, new Vector3(.06f, 1.4f, .06f), Quaternion.Euler(ctx.Range(-12f, 12f), 0, ctx.Range(-12f, 12f))); }
            halos.Add(mc + up * .4f, 4f, new Color(.2f, .85f, .65f), .3f); life.mists.Add(mc);

            // Turmoil & Tinfoil: trees with crumpled tinfoil crowns
            var foil = WorldMaterials.Solid(new Color(.86f, .88f, .93f), .97f); var glint = SongGlow(TurmoilTinfoil, new Color(.85f, .92f, 1f), 1.8f);
            foreach (var tp in new[] { z.At(-15f, -9f), z.At(-15.5f, 9f), z.At(-8f, -18f), z.At(-8f, 18.5f) })
            {
                var h = ctx.Range(4.5f, 6f);
                z.mk.Prism(bark, tp - up * .3f, up, .35f, h, 7, .6f); ArtistBits.PostCollider(z.root, tp, h, .4f);
                for (var k = 0; k < 6; k++) { var d = Quaternion.Euler(0, k * 60f + ctx.Range(-15f, 15f), 0) * Vector3.forward; crowns.Blob(foil, tp + up * (h + ctx.Range(-.4f, .8f)) + d * 1.6f, new Vector3(1.6f, 1.3f, 1.6f), ctx.Rng, .6f); }
                crowns.Blob(foil, tp + up * (h + 1.8f), new Vector3(1.8f, 1.4f, 1.8f), ctx.Rng, .6f);
                for (var k = 0; k < 9; k++) { var d = Quaternion.Euler(ctx.Range(-60f, 30f), ctx.Range(0f, 360f), 0) * Vector3.forward; z.mk.Box(glint, tp + up * (h + 1f) + d * 2.3f, Vector3.one * .16f, Quaternion.Euler(ctx.Range(0f, 90f), ctx.Range(0f, 90f), 0)); }
                halos.Add(tp + up * (h + 1f), 2.4f, new Color(.85f, .92f, 1f), .2f);
            }
        }

        /// A horse, standing or grazing.
        void Horse(Plot z, Vector3 at, Vector3 dir, Color coat, bool grazing)
        {
            var up = Vector3.up; dir.y = 0; dir.Normalize(); var rot = Quaternion.LookRotation(dir); var side = Vector3.Cross(up, dir);
            var m = WorldMaterials.Solid(coat); var hair = WorldMaterials.Solid(coat * .35f); var hoof = WorldMaterials.Solid(new Color(.12f, .1f, .09f));
            z.mk.Box(m, at + up * 1.35f, new Vector3(.62f, .7f, 1.6f), rot);
            z.mk.Blob(m, at + up * 1.4f + dir * .75f, new Vector3(.33f, .4f, .38f), z.ctx.Rng, 0f); z.mk.Blob(m, at + up * 1.4f - dir * .75f, new Vector3(.34f, .38f, .38f), z.ctx.Rng, 0f);
            foreach (var s in new[] { -.2f, .2f }) foreach (var f in new[] { -.68f, .7f }) { z.mk.Box(m, at + side * s + dir * f + up * .55f, new Vector3(.14f, 1.1f, .16f), rot); z.mk.Box(hoof, at + side * s + dir * f + up * .05f, new Vector3(.16f, .1f, .2f), rot); }
            var nd = grazing ? (dir * .7f - up * .7f).normalized : (dir * .5f + up * .85f).normalized; var nb = at + up * 1.6f + dir * .85f;
            z.mk.Box(m, nb + nd * .45f, new Vector3(.24f, .3f, 1f), Quaternion.LookRotation(nd, up)); z.mk.Box(hair, nb + nd * .45f - Vector3.Cross(side, nd) * .17f, new Vector3(.06f, .08f, .95f), Quaternion.LookRotation(nd, up));
            var hd = grazing ? (-up * .9f + dir * .45f).normalized : (dir * .85f - up * .5f).normalized; var hp = nb + nd * .95f;
            z.mk.Box(m, hp + hd * .28f, new Vector3(.2f, .24f, .58f), Quaternion.LookRotation(hd, up));
            foreach (var s in new[] { -.07f, .07f }) z.mk.Box(m, hp + side * s - hd * .05f + Vector3.Cross(hd, side) * -.18f, new Vector3(.05f, .14f, .05f), Quaternion.LookRotation(hd, up));
            z.mk.Box(hair, at + up * 1.2f - dir * 1.05f, new Vector3(.1f, .8f, .12f), rot * Quaternion.Euler(-15f, 0, 0));
            z.solid.Box(m, at + up * 1f, new Vector3(.8f, 2f, 2.4f), rot);
        }

        /// Bill Monroe: a little round stage before the mandolin with a vintage microphone and two footlights.
        void MonroeMic(Plot z)
        {
            var up = Vector3.up; var st = z.At(1.6f, 0); var chrome = Tone(new Color(.8f, .8f, .83f), .95f);
            z.mk.Prism(plank, st - up * .1f, up, 1.6f, .4f, 16); z.solid.Prism(plank, st - up * .1f, up, 1.6f, .4f, 16);
            z.mk.Prism(dark, st + up * .3f, up, .25f, .05f, 10); z.mk.Prism(chrome, st + up * .3f, up, .03f, 1.35f, 6);
            var head = st + up * 1.75f; z.mk.Blob(chrome, head, new Vector3(.11f, .17f, .09f), z.ctx.Rng, 0f); z.mk.Box(dark, head, new Vector3(.24f, .05f, .2f), z.Rot);
            var foot = SongGlow(UnclePen, Warm, 2.2f);
            foreach (var s in new[] { -1f, 1f }) { var fp = st + z.front * 1.2f + z.side * s * .8f + up * .3f; z.mk.Box(dark, fp, new Vector3(.4f, .25f, .3f), z.Rot * Quaternion.Euler(30f, 0, 0)); z.mk.Box(foot, fp + z.front * .05f + up * .05f, new Vector3(.3f, .16f, .03f), z.Rot * Quaternion.Euler(30f, 0, 0)); }
            ArtistBits.PostCollider(z.root, st, 1.8f, .3f);
        }

        /// Doc Watson: a cabin porch at the foot of the black mountain, a guitar on a rocker, a lit window.
        void DocPorch(Plot z)
        {
            var up = Vector3.up; var p = z.At(.5f, 3.8f); var rot = z.Rot; var logs = WorldMaterials.Solid(new Color(.5f, .38f, .27f));
            z.mk.Box(plank, p + up * .15f, new Vector3(4.2f, .3f, 2.6f), rot); z.solid.Box(plank, p + up * .15f, new Vector3(4.2f, .3f, 2.6f), rot);
            var wall = p - z.front * 1.45f; z.mk.Box(logs, wall + up * 1.7f, new Vector3(4.2f, 3f, .35f), rot); z.solid.Box(logs, wall + up * 1.7f, new Vector3(4.2f, 3f, .35f), rot);
            for (var k = 0; k < 6; k++) z.mk.Box(WorldMaterials.Solid(new Color(.4f, .3f, .22f)), wall + up * (.45f + k * .5f) + z.front * .18f, new Vector3(4.2f, .07f, .03f), rot);
            z.mk.Box(dark, wall + z.front * .19f - z.side * .9f + up * 1.3f, new Vector3(.9f, 1.9f, .03f), rot);
            var win = wall + z.front * .19f + z.side * 1f + up * 1.8f; z.mk.Box(trim, win, new Vector3(1f, .9f, .03f), rot); z.mk.Box(SongGlow(BlackMountainRag, Warm, 1.8f), win + z.front * .01f, new Vector3(.8f, .7f, .03f), rot);
            foreach (var s in new[] { -1.95f, 1.95f }) { z.mk.Box(wood, p + z.front * 1.15f + z.side * s + up * 1.5f, new Vector3(.15f, 2.4f, .15f), rot); ArtistBits.PostCollider(z.root, p + z.front * 1.15f + z.side * s, 2.7f, .12f); }
            z.mk.Box(roofM, p + up * 2.85f, new Vector3(4.6f, .1f, 3.2f), rot * Quaternion.Euler(10f, 0, 0));
            Rocker(z.mk, z.solid, z.ctx, p + up * .3f + z.side * .4f, z.front);
            Guitar(z.mk, p + up * .95f + z.front * .3f + z.side * 1.1f, rot * Quaternion.Euler(-14f, 0, 12f), .5f);
            var lan = p + z.front * 1.1f + up * 2.3f; z.mk.Prism(GlowM(Warm, 2.6f), lan, up, .12f, .25f, 8); halos.Add(lan + up * .12f, .9f, Warm, .8f);
        }
    }
}
