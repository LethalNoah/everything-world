using UnityEngine;

namespace EverythingWorld.World
{
    /// The Folk Woods' second pass (part of FolkKit): every song of the artists with more than one song gets its own
    /// light, so each of them answers when it plays, not only the first.
    /// - Fenne Lily: each room's lamp is its own song (the bedroom with the cat, the table for two, the cupboard), a
    ///   string of bulbs runs through all three rooms for "Room to Room", and flower beds by the doorstep glow for
    ///   "Uh Huh".
    /// - Yusuf / Cat Stevens: the porch lamp over the two chairs is "Father and Son"; a ring of lights round the
    ///   painted globe is "Wild World".
    /// - Gordon Lightfoot: a shack on stilts with a back stair, its door and lamp burning sunset orange ("Sundown").
    /// - John Denver: a campfire under the snowy peaks, its embers flaring for "Rocky Mountain High".
    /// - Simon & Garfunkel: a little red roadster by the stream, its lights on for "Mrs. Robinson".
    /// - Liana Flores: the silver shoes shine for "Silvershoes" (the moon already rises for "rises the moon").
    public partial class FolkKit
    {
        public const string UhHuh = "12dcVytplD6jfz8fLZazFq", ThankYouSorry = "7vN3Wka8rjbEOtFUsGudyR", Franz = "6hO23PWT4FSLZ0SNs1ARtH", CupboardSong = "2zkSXzIQ64YX4AvD7VQcUp",
            WildWorld = "7mjSHL2Eb0kAwiKbvNNyD9", FatherAndSonSong = "476V2d6iA2tWXgQboKmTtA", MrsRobinson = "6J1sj9dcEM91H80NIz1Gpo", Silvershoes = "7ep0wpZD1S5dWhPaQeMBeY";

        void SecondPass(Plot z)
        {
            switch (z.a.name)
            {
                case "Fenne Lily": CottageSongs(z); break;
                case "Yusuf / Cat Stevens": WildWorldRing(z); break;
                case "Gordon Lightfoot": SundownShack(z); break;
                case "John Denver": MountainFire(z); break;
                case "Simon & Garfunkel": Roadster(z); break;
            }
        }

        void SecondMark(Plot z, string name, Vector3 target, Vector3 from)
        {
            var m = new GameObject("Shot: " + name).transform; m.SetParent(z.ctx.Root, false);
            var cam = z.ctx.OnGround(from.x, from.z) + Vector3.up * 1.6f; var look = target - cam;
            m.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
        }

        /// Fenne Lily: the bulbs through the rooms and the doorstep flowers (the rooms' own lamps are set in Cottage).
        void CottageSongs(Plot z)
        {
            var up = Vector3.up; var c = z.At(-6f, 0); const float W = 15f, D = 5f, H = 3f;
            Vector3 P(float x, float y, float d) => c + z.side * x + up * y + z.front * d;
            // "Room to Room": a string of bulbs hung through the three rooms, dipping between the doorways
            var bulbs = SongGlow(RoomToRoom, new Color(1f, .85f, .55f), 2.2f);
            for (var k = 0; k <= 28; k++)
            {
                var t = k / 28f; var x = Mathf.Lerp(-W / 2f + .5f, W / 2f - .5f, t);
                var sag = Mathf.Abs(Mathf.Sin(t * Mathf.PI * 3f)) * .25f;
                z.mk.Blob(bulbs, P(x, H - .85f - sag, .9f), Vector3.one * .07f, z.ctx.Rng, 0f);
            }
            // "Uh Huh": flower beds either side of the doorstep, their flowers lighting with the song
            var bloom = SongGlow(UhHuh, new Color(1f, .55f, .75f), 2f); var soil = S(.32f, .22f, .15f); var leaf = S(.3f, .5f, .3f);
            foreach (var s in new[] { -1f, 1f })
            {
                var bed = P(s * (W / 2f - 1f), 0, D / 2f + .6f);
                z.mk.Box(wood, bed + up * .15f, new Vector3(1.8f, .3f, .7f), z.Rot); z.mk.Box(soil, bed + up * .31f, new Vector3(1.7f, .04f, .6f), z.Rot);
                for (var k = 0; k < 6; k++)
                {
                    var f = bed + z.side * ((k - 2.5f) * .28f) + z.front * ((k % 2) * .2f - .1f);
                    z.mk.Prism(leaf, f + up * .3f, up, .02f, .25f + (k % 3) * .06f, 4);
                    z.mk.Blob(bloom, f + up * (.58f + (k % 3) * .06f), Vector3.one * .08f, z.ctx.Rng, 0f);
                }
                halos.Add(bed + up * .7f, 1.1f, new Color(1f, .55f, .75f), .45f);
                z.solid.Box(wood, bed + up * .15f, new Vector3(1.8f, .3f, .7f), z.Rot);
            }
            SecondMark(z, "Fenne Lily's rooms", P(0, 1.6f, 0), P(-2f, 0, D / 2f + 7f));
        }

        /// Yusuf / Cat Stevens: a ring of little lights round the painted globe ("Wild World").
        void WildWorldRing(Plot z)
        {
            var up = Vector3.up; var g = z.At(2f, 6f) + up * 1.7f; var m = SongGlow(WildWorld, new Color(.45f, .8f, 1f), 2.2f);
            var tilt = Quaternion.AngleAxis(23f, z.front);
            for (var k = 0; k < 18; k++) { var a = k * Mathf.PI * 2f / 18f; z.mk.Blob(m, g + tilt * (z.side * Mathf.Cos(a) + z.front * Mathf.Sin(a)) * .95f, Vector3.one * .06f, z.ctx.Rng, 0f); }
            halos.Add(g, 1.8f, new Color(.45f, .8f, 1f), .4f);
            SecondMark(z, "Yusuf's porch and globe", z.At(-1f, 2f) + up * 1.4f, z.At(9f, 5f));
        }

        /// Gordon Lightfoot: a fisherman's shack on stilts, a back stair up to its door, door and lamp sunset orange.
        void SundownShack(Plot z)
        {
            var up = Vector3.up; var c = z.At(-6.5f, 7.5f); var rot = Quaternion.LookRotation(z.side); var fw = z.side; var sd = -z.front;
            var plank = S(.5f, .4f, .32f); var roofM = S(.3f, .26f, .25f); var sun = new Color(1f, .5f, .2f);
            foreach (var a in new[] { -1f, 1f }) foreach (var b in new[] { -1f, 1f })
            { var p = c + fw * a * 1.3f + sd * b * 1.1f; z.mk.Prism(logM, p - up * .2f, up, .1f, 1.7f, 6); }
            var floorY = 1.5f;
            z.mk.Box(plank, c + up * (floorY + 1.1f), new Vector3(2.4f, 2.2f, 2.8f), rot); z.solid.Box(plank, c + up * 1.85f, new Vector3(2.6f, 3.7f, 3f), rot);
            z.mk.GableRoof(roofM, c + up * (floorY + 2.2f), new Vector3(2.8f, 0, 3.2f), .9f, Quaternion.LookRotation(fw));
            // the back stair: up the shack's far side to a landing and the door
            var door = c - fw * 1.41f + up * (floorY + .9f) + sd * .4f;
            z.mk.Box(SongGlow(Sundown, sun, 1.6f), door, new Vector3(.8f, 1.6f, .02f), Quaternion.LookRotation(-fw));
            z.mk.Box(plank, c - fw * 1.8f + up * floorY + sd * .4f, new Vector3(1f, .1f, .8f), rot);
            for (var k = 0; k < 6; k++)
            {
                var sp = c - fw * 1.8f + sd * (1.1f + k * .45f) + up * (floorY - .1f - k * .25f);
                z.mk.Box(plank, sp, new Vector3(.4f, .08f, .9f), rot);
            }
            // two stringers under the steps, from the landing down to the ground
            foreach (var e in new[] { -.45f, .45f })
            {
                var top = c - fw * (1.8f + e) + sd * .9f + up * (floorY - .15f); var foot = c - fw * (1.8f + e) + sd * 3.75f; foot.y = z.ctx.Ground(foot.x, foot.z);
                z.mk.Box(plank, (top + foot) / 2f, new Vector3(.06f, .2f, Vector3.Distance(top, foot)), Quaternion.LookRotation(foot - top));
            }
            ArtistBits.PostCollider(z.root, c - fw * 1.8f + sd * 2.3f, 1.4f, .5f);
            var lamp = door + up * 1.05f - fw * .25f; z.mk.Blob(SongGlow(Sundown, sun, 2.6f), lamp, new Vector3(.18f, .24f, .18f), z.ctx.Rng, 0f); halos.Add(lamp, 1.6f, sun, .7f);
            ArtistBits.PointLight(z.root, lamp - fw * .6f, sun, 6f, 1f);
            foreach (var s in new[] { -1f, 1f }) z.mk.Box(SongGlow(Sundown, new Color(1f, .7f, .35f), 1.2f), c + fw * 1.41f + up * (floorY + 1.1f) + sd * s * .7f, new Vector3(.6f, .6f, .02f), Quaternion.LookRotation(fw));
            SecondMark(z, "the shack at sundown", c + up * 2f, c - fw * 7f + sd * 5f);
        }

        /// John Denver: a campfire under the peaks; its embers flare for Rocky Mountain High.
        void MountainFire(Plot z)
        {
            var up = Vector3.up; var at = z.At(-3.5f, -6.5f);
            Campfire(z.ctx, z.mk, z.solid, at);
            var ember = SongGlow(RockyMountainHigh, new Color(1f, .5f, .15f), 1.6f);
            for (var k = 0; k < 5; k++) z.mk.Blob(ember, at + up * .12f + Quaternion.Euler(0, k * 72f, 0) * Vector3.forward * .3f, new Vector3(.16f, .08f, .16f), z.ctx.Rng, .2f);
            halos.Add(at + up * .5f, 1.6f, new Color(1f, .5f, .15f), .5f);
            SecondMark(z, "the campfire under the peaks", at + up * .8f, z.At(4f, -9f));
        }

        /// Simon & Garfunkel: a little red two-seater roadster, top down, its lights on for Mrs. Robinson.
        void Roadster(Plot z)
        {
            var up = Vector3.up; var at = z.At(2.5f, -5.5f); var dir = z.side; var rot = Quaternion.LookRotation(dir); var sd = Vector3.Cross(up, dir);
            var red = WorldMaterials.Solid(new Color(.75f, .1f, .08f), .85f); var cream = S(.9f, .85f, .75f);
            z.mk.Box(red, at + up * .6f, new Vector3(1.6f, .45f, 3.8f), rot);
            z.mk.Box(red, at + up * .55f + dir * 1.5f, new Vector3(1.5f, .4f, .9f), rot * Quaternion.Euler(8f, 0, 0));
            z.mk.Box(cream, at + up * .75f - dir * .3f, new Vector3(1.3f, .3f, 1.3f), rot);   // the seats
            z.mk.Box(WorldMaterials.Solid(new Color(.6f, .75f, .85f), .95f), at + up * 1.05f + dir * .5f, new Vector3(1.4f, .4f, .04f), rot * Quaternion.Euler(-25f, 0, 0));
            foreach (var s in new[] { -1f, 1f }) foreach (var w in new[] { -1.2f, 1.2f }) z.mk.Prism(dark, at + sd * s * .82f + dir * w + up * .32f, sd * s, .32f, .2f, 12);
            var head = SongGlow(MrsRobinson, new Color(1f, .95f, .75f), 2.4f); var tail = SongGlow(MrsRobinson, new Color(1f, .15f, .1f), 2f);
            foreach (var s in new[] { -.5f, .5f })
            {
                z.mk.Prism(head, at + up * .65f + dir * 1.92f + sd * s, dir, .12f, .04f, 10);
                z.mk.Box(tail, at + up * .7f - dir * 1.91f + sd * s, new Vector3(.2f, .1f, .02f), rot);
            }
            z.solid.Box(red, at + up * .6f, new Vector3(1.8f, 1.2f, 4f), rot);
            SecondMark(z, "the roadster by the stream", at + up * .8f, z.At(8f, -9f));
        }
    }
}
