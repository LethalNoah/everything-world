using UnityEngine;

namespace EverythingWorld.World
{
    /// The string band, built upright (neck along +y, face toward +z) round origin o, turned by r, scaled by s: used
    /// both for the instruments floating over the picking circle and for the big statues in the artist areas.
    public partial class BluegrassKit
    {
        static Material Tone(Color c, float gloss = .55f) => WorldMaterials.Solid(c, gloss);

        static void Part(MeshKit mk, Material m, Vector3 o, Quaternion r, float s, Vector3 at, Vector3 size, Quaternion turn) => mk.Box(m, o + r * (at * s), size * s, r * turn);
        static void Disc(MeshKit mk, Material m, Vector3 o, Quaternion r, float s, Vector3 at, float radius, float depth, int sides = 16) => mk.Prism(m, o + r * ((at - Vector3.forward * depth / 2f) * s), r * Vector3.forward, radius * s, depth * s, sides);

        static void Strings(MeshKit mk, Vector3 o, Quaternion r, float s, int n, float from, float to, float z, float spread)
        {
            var m = Tone(new Color(.85f, .85f, .82f), .8f);
            for (var i = 0; i < n; i++) { var x = (n == 1 ? 0 : (i / (n - 1f) - .5f)) * spread; Part(mk, m, o, r, s, new Vector3(x, (from + to) / 2f, z), new Vector3(.006f, to - from, .006f), Quaternion.identity); }
        }

        /// A five-string banjo: a round pot with a pale head, a long neck with a dark fingerboard and a peghead.
        void Banjo(MeshKit mk, Vector3 o, Quaternion r, float s, bool electric)
        {
            var rim = Tone(electric ? new Color(.2f, .35f, .75f) : new Color(.7f, .45f, .2f), .7f); var head = electric ? WorldMaterials.Glow(new Color(.5f, .8f, 1f), .9f) : Tone(Cream, .3f);
            var chrome = Tone(new Color(.75f, .75f, .78f), .9f); var neck = Tone(new Color(.5f, .3f, .15f)); var board = Tone(new Color(.12f, .1f, .1f));
            Disc(mk, rim, o, r, s, new Vector3(0, 0, -.02f), .52f, .2f, 20); Disc(mk, chrome, o, r, s, new Vector3(0, 0, .085f), .5f, .02f, 20); Disc(mk, head, o, r, s, new Vector3(0, 0, .1f), .46f, .01f, 20);
            Part(mk, neck, o, r, s, new Vector3(0, 1f, .02f), new Vector3(.1f, 1.2f, .07f), Quaternion.identity);
            Part(mk, board, o, r, s, new Vector3(0, 1f, .06f), new Vector3(.08f, 1.2f, .01f), Quaternion.identity);
            Part(mk, neck, o, r, s, new Vector3(0, 1.7f, .02f), new Vector3(.2f, .26f, .06f), Quaternion.identity);
            for (var i = 0; i < 4; i++) Part(mk, chrome, o, r, s, new Vector3(i % 2 == 0 ? -.13f : .13f, 1.62f + (i / 2) * .12f, .02f), new Vector3(.07f, .03f, .03f), Quaternion.identity);
            Part(mk, chrome, o, r, s, new Vector3(0, -.32f, .12f), new Vector3(.12f, .14f, .02f), Quaternion.identity);
            Strings(mk, o, r, s, 5, -.3f, 1.6f, .07f, .06f);
        }

        /// A fiddle: two bouts and a waist, f-holes, a dark fingerboard, a scroll, and its bow alongside.
        void Fiddle(MeshKit mk, Vector3 o, Quaternion r, float s)
        {
            var body = Tone(new Color(.65f, .3f, .1f), .75f); var board = Tone(new Color(.1f, .08f, .08f));
            Disc(mk, body, o, r, s, new Vector3(0, -.12f, 0), .2f, .09f); Disc(mk, body, o, r, s, new Vector3(0, .16f, 0), .16f, .09f); Part(mk, body, o, r, s, new Vector3(0, .02f, 0), new Vector3(.22f, .16f, .09f), Quaternion.identity);
            foreach (var x in new[] { -.08f, .08f }) Part(mk, board, o, r, s, new Vector3(x, -.02f, .05f), new Vector3(.015f, .12f, .01f), Quaternion.identity);
            Part(mk, board, o, r, s, new Vector3(0, .3f, .055f), new Vector3(.05f, .4f, .02f), Quaternion.identity);
            Part(mk, body, o, r, s, new Vector3(0, .5f, .02f), new Vector3(.05f, .14f, .05f), Quaternion.identity);
            mk.Prism(body, o + r * (new Vector3(-.03f, .6f, .02f) * s), r * Vector3.right, .045f * s, .06f * s, 8);
            Strings(mk, o, r, s, 4, -.16f, .5f, .065f, .03f);
            var bow = Tone(new Color(.3f, .18f, .1f)); var hair = Tone(new Color(.95f, .93f, .88f), .2f);
            Part(mk, bow, o, r, s, new Vector3(.32f, .2f, .02f), new Vector3(.015f, .9f, .015f), Quaternion.Euler(0, 0, -12f));
            Part(mk, hair, o, r, s, new Vector3(.29f, .2f, .02f), new Vector3(.01f, .85f, .02f), Quaternion.Euler(0, 0, -12f));
        }

        /// A mandolin: a teardrop body with a round soundhole, a short neck, a sunburst finish.
        void Mandolin(MeshKit mk, Vector3 o, Quaternion r, float s)
        {
            var body = Tone(new Color(.6f, .28f, .08f), .75f); var face = Tone(new Color(.9f, .62f, .25f), .75f); var board = Tone(new Color(.1f, .08f, .08f));
            Disc(mk, body, o, r, s, new Vector3(0, -.05f, 0), .26f, .1f, 18); Disc(mk, face, o, r, s, new Vector3(0, -.05f, .052f), .2f, .01f, 18); Disc(mk, body, o, r, s, new Vector3(0, .17f, 0), .14f, .1f, 12);
            Disc(mk, board, o, r, s, new Vector3(0, .05f, .06f), .06f, .01f, 12);
            Part(mk, board, o, r, s, new Vector3(0, .4f, .055f), new Vector3(.06f, .42f, .02f), Quaternion.identity);
            Part(mk, body, o, r, s, new Vector3(0, .66f, .02f), new Vector3(.14f, .14f, .04f), Quaternion.identity);
            Strings(mk, o, r, s, 4, -.25f, .62f, .07f, .04f);
        }

        /// A dreadnought guitar: big lower bout, upper bout, a dark soundhole, a long neck.
        void Guitar(MeshKit mk, Vector3 o, Quaternion r, float s)
        {
            var body = Tone(new Color(.85f, .62f, .32f), .7f); var side = Tone(new Color(.45f, .25f, .12f)); var board = Tone(new Color(.1f, .08f, .08f));
            Disc(mk, side, o, r, s, new Vector3(0, -.2f, 0), .38f, .14f, 18); Disc(mk, side, o, r, s, new Vector3(0, .2f, 0), .3f, .14f, 16);
            Disc(mk, body, o, r, s, new Vector3(0, -.2f, .075f), .37f, .01f, 18); Disc(mk, body, o, r, s, new Vector3(0, .2f, .075f), .29f, .01f, 16);
            Disc(mk, board, o, r, s, new Vector3(0, .12f, .085f), .1f, .01f, 14);
            Part(mk, side, o, r, s, new Vector3(0, .85f, .04f), new Vector3(.1f, .85f, .06f), Quaternion.identity); Part(mk, board, o, r, s, new Vector3(0, .85f, .075f), new Vector3(.08f, .85f, .01f), Quaternion.identity);
            Part(mk, side, o, r, s, new Vector3(0, 1.38f, .04f), new Vector3(.16f, .22f, .05f), Quaternion.identity);
            Part(mk, board, o, r, s, new Vector3(0, -.42f, .085f), new Vector3(.24f, .05f, .02f), Quaternion.identity);
            Strings(mk, o, r, s, 6, -.42f, 1.3f, .085f, .07f);
        }

        /// An upright bass, taller than a person, with its endpin.
        void UprightBass(MeshKit mk, Vector3 o, Quaternion r, float s)
        {
            var body = Tone(new Color(.55f, .25f, .1f), .75f); var board = Tone(new Color(.1f, .08f, .08f));
            Disc(mk, body, o, r, s, new Vector3(0, -.35f, 0), .55f, .3f, 18); Disc(mk, body, o, r, s, new Vector3(0, .3f, 0), .42f, .3f, 16); Part(mk, body, o, r, s, new Vector3(0, -.02f, 0), new Vector3(.6f, .4f, .3f), Quaternion.identity);
            foreach (var x in new[] { -.16f, .16f }) Part(mk, board, o, r, s, new Vector3(x, -.05f, .16f), new Vector3(.03f, .3f, .01f), Quaternion.identity);
            Part(mk, board, o, r, s, new Vector3(0, .95f, .14f), new Vector3(.09f, 1.3f, .03f), Quaternion.identity);
            Part(mk, body, o, r, s, new Vector3(0, 1.7f, .08f), new Vector3(.1f, .3f, .1f), Quaternion.identity);
            Part(mk, board, o, r, s, new Vector3(0, -1f, 0), new Vector3(.03f, .3f, .03f), Quaternion.identity);
            Strings(mk, o, r, s, 4, -.6f, 1.6f, .16f, .07f);
        }
    }
}
