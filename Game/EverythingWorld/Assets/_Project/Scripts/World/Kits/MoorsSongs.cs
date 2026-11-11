using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Moors' second pass (part of MoorsKit): every song of every artist has a light of its own in that artist's
    /// scene, brightening and breathing while the song plays (the first pass gave song moments mostly to Radiohead
    /// and the Chili Peppers).
    public partial class MoorsKit
    {
        public const string SunsetInJuly = "0DaXPLskMeTu01eqx7Di1i", ChildPsychology = "7Bj4wIqRzM6vx278EFYoBh", Song2 = "1FTSo4v6BOZH9QxKc3MbVM", ElephantSong = "5OpwZViQUOos3cSrEo5qXv",
            VivaLaVida = "1mea3bSkSGXuIRvnydlB5b", Yellow = "3AJwUDP919kvQ9QcozQPxg", BlueCarsSong = "6B618H5CuCdEzcVs0NKTlt", Afterglow = "0gtQpIxMYqLxtSQn52m0Wx", FatalAttraction = "5631C1irkmhGmNPeyKp7av",
            ClintEastwood = "7yMiX7n9SBvadzox8T5jzT", MelancholyHill = "0q6LuUqGLUiCPP1cbdwFs3", TheMountain = "3MebYJnEDK4XdnCj3VMLjL", PlasticGuru = "68nwsIBVoGDO38VKhtvKzD",
            HoldMyHand = "4WnYuVFXe6Si4fZqkxbnvS", LetHerCry = "6NGi23FFKq9tH5NR1NcTw2", OnlyWannaBe = "1OFKUn2VLafrHj7ybnap0Q", DriveSong = "7nnWIPM5hwE3DaUBkvOIpy", FlyAway = "1OxcIUqVmVYxT6427tbhDW",
            Cydonia = "7ouMYWpwJ422jRcDASZB7P", Madness = "0c4IEciLCDdXEhhKxj4ThA", YouGetWhatYouGive = "1Cwsd5xI8CajJz795oy4XF", JustAGirl = "5lWRaa0fBxDE5yU91npPq7", DontLookBack = "7ppPZa3TRUSGKaks9wH7VT",
            Medicine = "3r8aVZkRiUrAwXK3aGsqy1", CommonPeople = "2fXKyAyPrEa24c6PJyqznF", Disco2000 = "48nfjq644cod3jFR0gpSuU", TwoPlusTwo = "4xkcGfpM9RwB4IiQ7yx2dB", WakeUp = "2QiqwOVUctPRVggO9G1Zs5",
            AllStar = "3cfOd4CMv2snFaKAnMdnvK", Believer = "5M5mzimlah21xlC1rAuIuY", AveMaria = "6a9sOyyOUmfT0ZbWWjesZu", Dreams = "4JGKZS7h4Qa16gOU3oNETV", Linger = "0gEyKnHvgkrkBM6fbeHdwK", Zombie = "7EZC6E7UjZe63f1jRmkWxt",
            IfYouCouldOnlySee = "4KoNBTm9a55KgLMtEaf3n6", Applefish = "4Givd6JZUnj2wa9iT9qN3F", BugInTheCake = "0eqW8uWYZD19N46werkSTF", PlasticCouch = "60SVJh6cdcjYh7CgQ5UkS0",
            BuddyHolly = "5XkVQZWvsVwedk0Jv54SVH", IslandInTheSun = "2MLHyLy5z5l5YRp7momlgw", NoScrubs = "3wQxUfquHLpgHoU6fo8Z50", SayItAintSo = "6VoIBz0VhCyz7OdEoRYDiA";

        /// A glowing material that brightens while the given song plays.
        Material SongGlow(string song, Color c, float s = 1.6f)
        {
            var m = new Material(GlowM(c, s)) { name = "Song glow" }; life.songGlows.Add((song, m, c * (s * .65f))); return m;
        }

        /// The extra pieces for songs whose scene had nothing to light for them, added after each artist's scene.
        void SongPieces(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot;
            switch (z.a.name)
            {
                case "Gorillaz":
                {   // On Melancholy Hill: a lantern on the bench; The Mountain: a little peak beside the hill with a glowing
                    // cairn on top; The Plastic Guru: a ring of glowing plastic flowers round the hill's foot
                    var hill = z.At(-1f, 7.5f); var top = hill + up * 1.5f;
                    var lan = top + up * .55f + z.side * .7f; z.mk.Box(SongGlow(MelancholyHill, new Color(.55f, .8f, 1f), 2.2f), lan, new Vector3(.16f, .22f, .16f), rot); halos.Add(lan, 1.2f, new Color(.55f, .8f, 1f), .6f);
                    var pk = z.At(-6f, 12.5f); var rock = WorldMaterials.Solid(new Color(.42f, .42f, .46f)); var snow = WorldMaterials.Solid(new Color(.95f, .96f, 1f));
                    z.mk.Cone(rock, pk - up * .2f, 2.6f, 4.6f, 6); z.mk.Cone(snow, pk + up * 2.9f, 1.05f, 1.55f, 6); z.solid.Prism(rock, pk - up * .2f, up, 2f, 3f, 8);
                    var cairn = pk + up * 4.5f; z.mk.Blob(SongGlow(TheMountain, new Color(1f, .7f, .3f), 2.4f), cairn, Vector3.one * .25f, z.ctx.Rng, .2f); halos.Add(cairn, 1.8f, new Color(1f, .7f, .3f), .6f);
                    var petal = SongGlow(PlasticGuru, new Color(1f, .4f, .8f), 2f);
                    for (var k = 0; k < 12; k++) { var a = k * Mathf.PI * 2f / 12f; var f = hill + (z.side * Mathf.Cos(a) + z.front * Mathf.Sin(a)) * 3.9f; f = z.ctx.OnGround(f.x, f.z); z.mk.Prism(WorldMaterials.Solid(new Color(.3f, .55f, .3f)), f, up, .02f, .35f, 4); z.mk.Prism(petal, f + up * .35f, up, .14f, .05f, 5, 1.6f); }
                    break;
                }
                case "Hootie & The Blowfish":
                {   // Let Her Cry: a glowing tear under the pufferfish's eye; Hold My Hand: a lamp at the foot of the owl's post
                    var c = z.At(-4f, -2f) + up * 3.2f;
                    z.mk.Blob(SongGlow(LetHerCry, new Color(.4f, .7f, 1f), 2.2f), c + z.front * 2.3f + z.side * .8f - up * .1f, new Vector3(.12f, .2f, .12f), z.ctx.Rng, 0f);
                    var post = z.At(-2f, 5f); var lp = post + up * 1.6f + z.front * .3f; z.mk.Box(dark, lp + up * .25f - z.front * .15f, new Vector3(.04f, .04f, .3f), rot);
                    z.mk.Box(SongGlow(HoldMyHand, Lamp, 2.4f), lp, new Vector3(.18f, .24f, .18f), rot); halos.Add(lp, 1.2f, Lamp, .6f);
                    break;
                }
                case "Violet Grohl":
                {   // Plastic Couch: a standard lamp beside the couch; Bug In The Cake: candles on the cake
                    var c = z.At(-2f, -3f); var lp = c + z.side * 2.1f; z.mk.Prism(steel, lp, up, .03f, 1.6f, 6); z.mk.Prism(SongGlow(PlasticCouch, new Color(1f, .82f, .55f), 1.6f), lp + up * 1.6f, up, .35f, .4f, 10, .6f); halos.Add(lp + up * 1.8f, 1.4f, new Color(1f, .82f, .55f), .5f); ArtistBits.PostCollider(z.root, lp, 1.8f, .2f);
                    var cake = z.At(-4f, 4f); var flame = SongGlow(BugInTheCake, new Color(1f, .7f, .3f), 2.6f);
                    for (var k = 0; k < 7; k++) { var a = k * Mathf.PI * 2f / 7f; var cp = cake + up * 3.32f + (z.side * Mathf.Cos(a) + z.front * Mathf.Sin(a)) * .95f; z.mk.Prism(white, cp, up, .04f, .3f, 6); z.mk.Blob(flame, cp + up * .36f, Vector3.one * .05f, z.ctx.Rng, 0f); }
                    halos.Add(cake + up * 3.8f, 1.8f, new Color(1f, .7f, .3f), .5f);
                    break;
                }
                case "Muse":
                {   // Madness: the horse's eyes burn red
                    var h = z.At(-1f, 3f); foreach (var s in new[] { -.12f, .12f }) z.mk.Blob(SongGlow(Madness, new Color(1f, .15f, .1f), 2.6f), h + up * 2.45f + z.side * 1.35f + z.front * s, Vector3.one * .06f, z.ctx.Rng, 0f);
                    break;
                }
                case "Coldplay":
                {   // Viva La Vida: jewels on the cracked crown
                    var cp = z.At(-7f, 5f); var jewel = SongGlow(VivaLaVida, new Color(.4f, .5f, 1f), 2.4f);
                    for (var k = 0; k < 5; k++) z.mk.Blob(jewel, cp + up * 1.45f + Quaternion.Euler(0, k * 72f + 36f, 0) * Vector3.forward * .72f, Vector3.one * .09f, z.ctx.Rng, 0f);
                    halos.Add(cp + up * 1.6f, 1.4f, new Color(.4f, .5f, 1f), .5f);
                    break;
                }
                case "Pulp":
                {   // Common People: a supermarket sign over the trolley
                    var tr = z.At(1.5f, 5f); var sp = tr + z.side * 1.4f; z.mk.Prism(steel, sp, up, .06f, 2.6f, 6); ArtistBits.PostCollider(z.root, sp, 2.6f, .1f);
                    z.mk.Box(SongGlow(CommonPeople, new Color(1f, .9f, .4f), 1.8f), sp + up * 2.75f - z.side * .6f, new Vector3(1.6f, .45f, .08f), rot);
                    ArtistBits.Label(z.labels, "OPEN", sp + up * 2.75f - z.side * .6f + z.front * .05f, z.front, .16f, new Color(.2f, .1f, .05f), TextAnchor.MiddleCenter);
                    break;
                }
                case "Weezer":
                {   // Island In The Sun: a lantern at the palm's top; No Scrubs: a jukebox by the diner door
                    var pond = z.At(2.5f, 8f); z.mk.Blob(SongGlow(IslandInTheSun, new Color(1f, .8f, .35f), 2.2f), pond + up * 3.2f + z.side * .5f, Vector3.one * .2f, z.ctx.Rng, 0f); halos.Add(pond + up * 3.2f, 1.8f, new Color(1f, .8f, .35f), .5f);
                    var jb = z.At(-1.6f, -7.5f); z.mk.Box(WorldMaterials.Solid(new Color(.5f, .25f, .2f), .8f), jb + up * .8f, new Vector3(1f, 1.6f, .6f), rot); z.solid.Box(dark, jb + up * .8f, new Vector3(1f, 1.6f, .6f), rot);
                    z.mk.Prism(SongGlow(NoScrubs, new Color(1f, .45f, .8f), 2f), jb + up * 1.35f + z.front * .31f, z.front, .45f, .03f, 14, 1f); z.mk.Box(SongGlow(NoScrubs, new Color(.4f, .9f, 1f), 1.6f), jb + up * .6f + z.front * .31f, new Vector3(.7f, .5f, .02f), rot);
                    halos.Add(jb + up * 1.2f + z.front * .6f, 1.4f, new Color(1f, .45f, .8f), .5f);
                    break;
                }
                case "Black Box Recorder":
                {   // Child Psychology: a desk lamp on the school desk
                    var d = z.At(-1f, 4f); var lp = d + up * .78f + z.side * .35f; z.mk.Prism(steel, lp, up, .02f, .4f, 6);
                    z.mk.Prism(SongGlow(ChildPsychology, new Color(1f, .9f, .6f), 2f), lp + up * .4f, up, .14f, .14f, 8, .5f); halos.Add(lp + up * .45f, 1f, new Color(1f, .9f, .6f), .5f);
                    break;
                }
                case "Dishwalla":
                {   // Counting Blue Cars: every car's headlights on
                    var head = SongGlow(BlueCarsSong, new Color(1f, .95f, .8f), 2.4f);
                    for (var k = 0; k < 5; k++) { var p = z.At(-3f, (k - 2f) * 2.6f); foreach (var s in new[] { -.5f, .5f }) z.mk.Prism(head, p + z.side * s + z.front * 1.8f + up * .6f, z.front, .12f, .04f, 10); }
                    halos.Add(z.At(-1f, 0) + up * .7f, 3f, new Color(1f, .95f, .8f), .3f);
                    break;
                }
                default:
                    if (z.a.name.StartsWith("b") && z.a.name.EndsWith("a"))
                    {   // bôa, Elephant: a lantern-lit saddle cloth on the elephant's back
                        var c = z.At(-3f, 0); var cloth = SongGlow(ElephantSong, new Color(1f, .5f, .2f), 1.2f);
                        z.mk.Box(cloth, c + up * 3.42f, new Vector3(1.6f, .1f, 1.8f), rot);
                        foreach (var s in new[] { -1f, 1f }) z.mk.Box(cloth, c + up * 2.6f + z.side * s * 1.62f, new Vector3(.04f, .9f, 1.6f), rot);
                        halos.Add(c + up * 3.6f, 2f, new Color(1f, .5f, .2f), .4f);
                    }
                    break;
            }
        }
    }

    public partial class MoorsLife
    {
        public List<(string song, Material m, Color c)> songGlows = new List<(string, Material, Color)>();
        readonly Dictionary<string, float> songAmount = new Dictionary<string, float>();

        void SongGlowUpdate(float dt, float t, string id, bool here)
        {
            foreach (var (song, m, c) in songGlows)
            {
                if (m == null) continue;
                songAmount.TryGetValue(song, out var k);
                k = Mathf.MoveTowards(k, here && id == song ? 1f : 0f, dt / 2f); songAmount[song] = k;
                m.SetColor("_EmissionColor", c * (1f + k * (2.5f + Mathf.Sin(t * 2.2f) * .6f)));
            }
        }
    }
}
