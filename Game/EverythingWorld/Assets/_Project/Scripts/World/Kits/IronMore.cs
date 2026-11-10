using UnityEngine;

namespace EverythingWorld.World
{
    /// The Iron Range's deep pass (part of IronKit): every artist's songs as things that glow while they play, and a
    /// tall burning beacon at every artist so you can find them across the dark range (the dark stays).
    /// - Metallica: a hospital bed under a single lamp inside a ring of barbed wire ("One"), a black obelisk whose
    ///   light drains away ("Fade To Black"), a ring of candles ("Nothing Else Matters"), a whiskey barrel with a
    ///   glowing jar ("Whiskey In The Jar").
    /// - Black Sabbath: a wizard's staff with a glowing orb ("The Wizard"), pairs of eyes watching from the dead trees
    ///   ("Paranoid").
    /// - Elena Siegman and Kevin Sherwood, the zombies corner: a weapon-upgrade machine glowing violet ("Beauty Of
    ///   Annihilation"), a teleporter pad crackling with electricity, hands reaching up out of the ground in the
    ///   mist, tally marks glowing red on the bunker wall ("The One"); Kevin Sherwood: a tall music box with a little
    ///   skeleton dancer turning on it and a teddy bear sitting by it ("Lullaby Of A Deadman").
    /// - Ozzy Osbourne: a little cabin with its porch light on ("Mama, I'm Coming Home").
    /// - The rest glow up for their one song.
    public partial class IronKit
    {
        public const string One = "0LAcM6I7ijW4VVW0aytl1t", FadeToBlack = "0dqGfCMAGyDgpUAgLNOjWd", NothingElse = "2CtemffYhT0DJWcT1XW047", Whiskey = "77gY3WsOnEsq9JOWgBmdLj",
            Orion = "4xyWJM4kscTROV2RESdwpd", TheWizard = "6sjTzevtstOxOMsFsypsEm", Paranoid = "1Y373MqadDRtclJNdnUXVc", ElectricFuneral = "7As8h8LJTMIritB8QwSmqr",
            Beauty = "05XukYTsS9JIrzxNeQt05B", TheOne = "5LcuhvZlZK1k1OYarrt177", Lullaby = "6BrBmkdx076OXG7BalDQhR", MamaHome = "0S3gpZzlT9Hb7CCSV2owX7",
            HolyDiver = "57fqmDaokbxZ3TaB0jp39q", Trooper = "1Ab3hhOw1TJWDgO10vlxNZ", BreakingLaw = "2RaA6kIcvomt77qlIgGhCT", Mother = "7txxAtOMwLLnQTpKeBL6bp",
            Stargazer = "4xhFGedBnZDqccPwisb55d", JaguarSong = "6vI3tiGBkbgSkM8iS8evlp", SuckingLove = "4NGB1XLq6zLYXg2KBDEMee", KrovZaKrov = "3f8zZrQpvmvIagtFHOlnVy",
            Mystification = "5AFRQAsGT0CWzgR9UPsqN1", RunAway = "3E8cgWzXSWJthJiXBieSg4", EdgeOfPerfection = "3lLxol06VgDfappIPxBQU3";

        Material SongGlow(string song, Color c, float s = 1.6f)
        {
            var m = new Material(GlowM(c, s)) { name = "Song glow" }; life.songGlows.Add((song, m, c * (s * .65f))); return m;
        }

        /// A tall iron beacon with a fire on top, seen from far across the range.
        void Beacon(Plot z, Vector3 g)
        {
            var up = Vector3.up;
            z.mk.Prism(iron, g - up * .3f, up, .22f, 7.3f, 6, .7f); z.mk.Prism(iron, g + up * 7f, up, .7f, .5f, 8, 1.3f);
            for (var k = 0; k < 4; k++) z.mk.Box(iron, g + up * 6.5f + Quaternion.Euler(0, k * 90f, 0) * Vector3.forward * .4f, new Vector3(.08f, 1.2f, .08f), Quaternion.Euler(0, k * 90f, 0) * Quaternion.Euler(25f, 0, 0));
            z.mk.Blob(GlowM(Molten, 3.2f), g + up * 7.7f, new Vector3(.5f, .6f, .5f), z.ctx.Rng, .2f);
            halos.Add(g + up * 7.9f, 3f, Molten, .7f); life.fires.Add(g + up * 7.6f); life.beacons.Add(g + up * 8f);
            ArtistBits.PostCollider(z.root, g, 7f, .3f);
        }

        void MetallicaMore(Plot z)
        {
            var up = Vector3.up; var ctx = z.ctx; var rot = z.Rot;
            // One: a bed under a single lamp inside a ring of barbed wire
            var b = z.At(6f, -13f); var sheet = WorldMaterials.Solid(new Color(.85f, .85f, .82f));
            z.mk.Box(iron, b + up * .45f, new Vector3(1f, .1f, 2.1f), rot); z.mk.Box(sheet, b + up * .55f, new Vector3(.95f, .14f, 2f), rot); z.mk.Box(sheet, b + up * .7f - z.front * .8f, new Vector3(.7f, .15f, .4f), rot);
            foreach (var s in new[] { -.45f, .45f }) foreach (var f in new[] { -1f, 1f }) z.mk.Box(iron, b + z.side * s + z.front * f + up * .25f, new Vector3(.05f, .5f, .05f), rot);
            z.solid.Box(iron, b + up * .4f, new Vector3(1.1f, .8f, 2.2f), rot);
            var lampPos = b + up * 3.2f; z.mk.Box(iron, b + up * 1.6f + z.side * .8f, new Vector3(.06f, 3.2f, .06f), rot); z.mk.Box(iron, lampPos + z.side * .4f, new Vector3(.8f, .05f, .05f), rot);
            z.mk.Cone(iron, lampPos - up * .1f, .3f, .3f, 10); z.mk.Blob(SongGlow(One, new Color(1f, .95f, .8f), 2.4f), lampPos - up * .15f, Vector3.one * .12f, ctx.Rng, 0f);
            halos.Add(lampPos - up * .3f, 1.6f, new Color(1f, .95f, .85f), .5f);
            for (var k = 0; k < 12; k++) { var a0 = k * Mathf.PI / 6f; var a1 = (k + 1) * Mathf.PI / 6f; var p0 = ctx.OnGround(b.x + Mathf.Cos(a0) * 2.6f, b.z + Mathf.Sin(a0) * 2.6f); var p1 = ctx.OnGround(b.x + Mathf.Cos(a1) * 2.6f, b.z + Mathf.Sin(a1) * 2.6f); foreach (var y in new[] { .4f, .8f }) z.mk.Box(chain, (p0 + p1) / 2f + up * y, new Vector3(.03f, .03f, Vector3.Distance(p0, p1)), Quaternion.LookRotation(p1 - p0)); if (k % 3 == 0) { z.mk.Box(timber, p0 + up * .5f, new Vector3(.08f, 1f, .08f), rot); ArtistBits.PostCollider(z.root, p0, 1f, .1f); } }
            // Fade To Black: a black obelisk whose light drains away
            var o = z.At(-14f, 10f); z.mk.Prism(dark, o - up * .2f, up, .9f, 7f, 4, .35f, 45f); z.solid.Box(dark, o + up * 3f, new Vector3(1.3f, 6f, 1.3f), Quaternion.identity);
            z.mk.Box(SongGlow(FadeToBlack, new Color(.6f, .65f, 1f), 1.2f), o + up * 3f + z.front * .58f, new Vector3(.1f, 4f, .02f), rot);
            // Nothing Else Matters: a ring of candles
            var nc = z.At(6f, 13f); var candle = SongGlow(NothingElse, new Color(1f, .75f, .35f), 2.6f); var wax = WorldMaterials.Solid(new Color(.9f, .88f, .8f));
            for (var k = 0; k < 10; k++) { var a = k * Mathf.PI / 5f; var p = ctx.OnGround(nc.x + Mathf.Cos(a) * 1.5f, nc.z + Mathf.Sin(a) * 1.5f); var h = .3f + (k % 3) * .15f; z.mk.Prism(wax, p, up, .07f, h, 6); z.mk.Blob(candle, p + up * (h + .06f), new Vector3(.04f, .07f, .04f), ctx.Rng, 0f); }
            halos.Add(nc + up * .5f, 2.2f, new Color(1f, .75f, .35f), .5f);
            // Whiskey In The Jar: a barrel and a glowing jar
            var wb = z.At(-6f, -14f); z.mk.Prism(timber, wb, up, .6f, 1.2f, 12, .92f); for (var k = 0; k < 3; k++) z.mk.Prism(iron, wb + up * (.15f + k * .45f), up, .62f, .06f, 12);
            z.mk.Prism(SongGlow(Whiskey, new Color(1f, .6f, .2f), 1.4f), wb + up * 1.25f, up, .25f, .4f, 10, .8f); ArtistBits.PostCollider(z.root, wb, 1.2f, .65f);
            // Orion: its stars brighten for its own song
            foreach (var sp in life.stars) z.mk.Blob(SongGlow(Orion, new Color(.8f, .88f, 1f), 1.5f), sp, Vector3.one * .5f, ctx.Rng, 0f);
        }

        void SabbathMore(Plot z)
        {
            var up = Vector3.up; var ctx = z.ctx;
            var st = z.At(3f, -9f); z.mk.Prism(timber, st, (up + z.side * .1f).normalized, .07f, 3.2f, 6); z.mk.Prism(timber, st + up * 3.1f, z.side, .05f, .3f, 6);
            z.mk.Blob(SongGlow(TheWizard, new Color(.6f, .8f, 1f), 2.4f), st + up * 3.4f, Vector3.one * .28f, ctx.Rng, 0f); halos.Add(st + up * 3.4f, 1.6f, new Color(.6f, .8f, 1f), .6f); ArtistBits.PostCollider(z.root, st, 3f, .1f);
            var eyes = SongGlow(Paranoid, new Color(1f, .85f, .2f), 1.6f);
            foreach (var p in new[] { z.At(-4f, -6f), z.At(-6f, -3f), z.At(-2f, -7f), z.At(-9f, -6f) }) foreach (var s in new[] { -.12f, .12f }) z.mk.Blob(eyes, p + up * 2.6f + z.side * s + z.front * .3f, new Vector3(.06f, .04f, .03f), ctx.Rng, 0f);
            var funeral = SongGlow(ElectricFuneral, new Color(.6f, .5f, 1f), 1f); z.mk.Prism(funeral, z.At(1f, 5f) + up * .03f, up, 2.2f, .03f, 16);
        }

        void ZombiesMore(Plot z)
        {
            var up = Vector3.up; var ctx = z.ctx; var rot = z.Rot;
            // the weapon-upgrade machine
            var pm = z.At(5f, 3f); var body = WorldMaterials.Solid(new Color(.2f, .2f, .26f), .7f); var violet = SongGlow(Beauty, new Color(.6f, .3f, 1f), 2f);
            z.mk.Box(body, pm + up * 1f, new Vector3(2.2f, 2f, 1.4f), rot); z.mk.Box(body, pm + up * 2.3f - z.front * .2f, new Vector3(1.6f, .6f, 1f), rot);
            z.mk.Box(violet, pm + up * 1.3f + z.front * .71f, new Vector3(1.4f, .9f, .02f), rot); for (var k = 0; k < 4; k++) z.mk.Prism(violet, pm + up * 2.7f + z.side * ((k - 1.5f) * .4f), up, .07f, .4f, 6);
            z.mk.Box(dark, pm + up * .55f + z.front * .72f, new Vector3(1.6f, .25f, .02f), rot); z.solid.Box(body, pm + up * 1.2f, new Vector3(2.2f, 2.6f, 1.4f), rot);
            halos.Add(pm + up * 1.5f + z.front, 2.6f, new Color(.6f, .3f, 1f), .45f); ArtistBits.PointLight(z.root, pm + up * 2f + z.front * 2f, new Color(.6f, .35f, 1f), 9f, 1.4f);
            // the teleporter pad
            var tp = z.At(-5f, 10.5f); z.mk.Prism(iron, tp - up * .1f, up, 2f, .4f, 16); z.mk.Prism(GlowM(new Color(.4f, .7f, 1f), 1.6f), tp + up * .31f, up, 1.6f, .02f, 16);
            foreach (var k in new[] { 0, 1, 2 }) { var p = tp + Quaternion.Euler(0, k * 120f, 0) * Vector3.forward * 1.9f; z.mk.Prism(iron, p, up, .12f, 3.4f, 6); z.mk.Blob(chain, p + up * 3.5f, Vector3.one * .25f, ctx.Rng, 0f); ArtistBits.PostCollider(z.root, p, 3.4f, .15f); life.teleporter.Add(p + up * 3.5f); }
            // hands reaching up out of the ground
            var skin = WorldMaterials.Solid(new Color(.45f, .5f, .42f));
            for (var k = 0; k < 7; k++)
            {
                var p = z.At(5.5f + ctx.Range(-1.2f, 1.2f), -8f + k * 1.2f + ctx.Range(-.4f, .4f)); var lean = Quaternion.Euler(ctx.Range(-25f, 25f), ctx.Range(0f, 360f), ctx.Range(-25f, 25f));
                z.mk.Box(skin, p + lean * (up * .35f), new Vector3(.12f, .7f, .1f), lean); z.mk.Box(skin, p + lean * (up * .78f), new Vector3(.2f, .2f, .07f), lean);
                for (var f = 0; f < 4; f++) z.mk.Box(skin, p + lean * (up * 1f + Vector3.right * (f - 1.5f) * .05f), new Vector3(.03f, .2f, .03f), lean * Quaternion.Euler(0, 0, (f - 1.5f) * 10f));
                z.mk.Blob(darkRock, p + up * .05f, new Vector3(.3f, .08f, .3f), ctx.Rng, .4f);
            }
            life.mists.Add(z.At(5f, -5f)); life.mists.Add(z.At(-4f, 9f));
            // tally marks glowing red on the bunker wall
            var wall = z.At(-8f, -2f) + z.front * 3.02f + up * 3.2f + z.side * -.9f; var red = SongGlow(TheOne, new Color(1f, .15f, .1f), 1.8f);
            for (var g = 0; g < 2; g++) { for (var k = 0; k < 4; k++) z.mk.Box(red, wall + z.side * (g * .9f + k * .15f), new Vector3(.04f, .5f, .01f), rot); z.mk.Box(red, wall + z.side * (g * .9f + .22f), new Vector3(.7f, .04f, .01f), rot * Quaternion.Euler(0, 0, 30f)); }
        }

        void Sherwood(Plot z)
        {
            var up = Vector3.up; var ctx = z.ctx; var c = z.At(-3f, 0); var rot = z.Rot; var wood = WorldMaterials.Solid(new Color(.3f, .2f, .14f), .6f);
            z.mk.Box(wood, c + up * .9f, new Vector3(1.4f, 1.8f, 1.4f), rot); z.solid.Box(wood, c + up * .9f, new Vector3(1.4f, 1.8f, 1.4f), rot);
            var pale = SongGlow(Lullaby, new Color(.6f, .8f, 1f), 1.6f);
            z.mk.Box(pale, c + up * .9f + z.front * .71f, new Vector3(1f, 1.2f, .02f), rot); z.mk.Prism(iron, c + up * 1.8f, up, .65f, .08f, 16);
            var dk = new MeshKit(); var bone = WorldMaterials.Solid(new Color(.85f, .85f, .78f));
            dk.Box(bone, up * .45f, new Vector3(.12f, .5f, .08f), Quaternion.identity); dk.Blob(bone, up * .82f, new Vector3(.1f, .12f, .1f), new System.Random(3), 0f);
            dk.Box(bone, up * .62f, new Vector3(.5f, .04f, .04f), Quaternion.Euler(0, 0, 25f)); foreach (var s in new[] { -.05f, .05f }) dk.Box(bone, up * .12f + Vector3.right * s, new Vector3(.04f, .3f, .04f), Quaternion.identity);
            var d = dk.BuildSingle("Music box dancer", life.transform, false).transform; d.position = c + up * 1.88f; life.spinners.Add(d);
            var bear = WorldMaterials.Solid(new Color(.55f, .4f, .28f)); var bp = c + z.side * 1.4f + z.front * .6f;
            z.mk.Blob(bear, bp + up * .3f, new Vector3(.3f, .32f, .26f), ctx.Rng, 0f); z.mk.Blob(bear, bp + up * .75f, Vector3.one * .2f, ctx.Rng, 0f);
            foreach (var s in new[] { -.13f, .13f }) { z.mk.Blob(bear, bp + up * .92f + z.side * s, Vector3.one * .07f, ctx.Rng, 0f); z.mk.Blob(bear, bp + up * .12f + z.side * s * 1.4f + z.front * .2f, new Vector3(.08f, .08f, .14f), ctx.Rng, 0f); }
            halos.Add(c + up * 2.1f, 1.8f, new Color(.6f, .8f, 1f), .5f); life.mists.Add(c);
        }

        void OzzyMore(Plot z)
        {
            var up = Vector3.up; var c = z.At(-9f, -8.5f); var rot = Quaternion.LookRotation(z.front);
            z.mk.Box(timber, c + up * 1.4f, new Vector3(4f, 2.8f, 3.4f), rot); z.solid.Box(timber, c + up * 1.4f, new Vector3(4f, 2.8f, 3.4f), rot);
            z.mk.GableRoof(darkRock, c + up * 2.83f, new Vector3(4.4f, 0, 3.8f), 1.4f, rot * Quaternion.Euler(0, 90f, 0));
            var front = rot * Vector3.forward; z.mk.Box(dark, c + up * 1f + front * 1.71f, new Vector3(.9f, 1.9f, .03f), rot);
            var warm = SongGlow(MamaHome, new Color(1f, .75f, .4f), 1.8f); z.mk.Box(warm, c + up * 1.6f + front * 1.71f + Vector3.Cross(up, front) * 1.2f, new Vector3(.8f, .7f, .03f), rot);
            z.mk.Blob(warm, c + up * 2.3f + front * 1.9f - Vector3.Cross(up, front) * .7f, Vector3.one * .1f, z.ctx.Rng, 0f); halos.Add(c + up * 2.3f + front * 2f, 1.2f, new Color(1f, .75f, .4f), .6f);
        }
    }
}
