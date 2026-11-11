using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// Garage Rock Row's deep pass (part of GarageKit): songs as late-night places in the back lots and hills round
    /// the town, each under a tall neon sign with its name that you can see over the rooftops, each glowing while
    /// its song plays.
    /// - Arctic Monkeys: a phone booth with the receiver hanging ("Do I Wanna Know?"), a motorbike under a giant
    ///   neon question mark ("R U Mine?").
    /// - Cage the Elephant: a car on an overlook, headlights on toward the town ("Cigarette Daydreams"), a busker's
    ///   pallet stage with a mic, a spotlight and an open guitar case ("Ain't No Rest for the Wicked").
    /// - Silversun Pickups: a stack of old TVs glowing blue ("Lazy Eye"), a big red button on a striped pedestal with
    ///   warning lights ("Panic Switch").
    /// - The Kooks: an ice-cream van with a neon cone ("Naive"), a light-up dance floor under a mirror ball ("She
    ///   Moves In Her Own Way").
    /// - The Strokes: a corner of a ball field, home plate, backstop and a stadium light ("Ode To The Mets").
    /// - The Voidz: an arcade cabinet glitching on a pallet ("Did My Best").
    public partial class GarageKit
    {
        readonly List<Vector3> placeSpots = new List<Vector3>();
        static readonly Color Cyan = new Color(.2f, .95f, 1f), Yellow = new Color(1f, .88f, .25f), Blue = new Color(.35f, .55f, 1f), Hot = new Color(1f, .4f, .9f), Red = new Color(1f, .2f, .15f);

        Material SongGlow(string song, Color c, float s = 2f)
        {
            var m = new Material(WorldMaterials.Glow(c, s)) { name = "Song glow" };
            life.songGlows.Add((song, m, c * s));
            return m;
        }

        void Lit(LandContext ctx, Vector3 at, Color col, float r = 2.6f)
        {
            ArtistBits.PointLight(ctx.Root, at, col, 13f, 2f);
            halos.Add(at, r, col, .5f);
        }

        bool Gentle(LandContext ctx, float x, float z, float r, float drop)
        {
            float lo = float.MaxValue, hi = float.MinValue;
            for (var k = 0; k < 12; k++) foreach (var f in new[] { .5f, 1f }) { var a = k * Mathf.PI / 6f; var h = ctx.Ground(x + Mathf.Cos(a) * r * f, z + Mathf.Sin(a) * r * f); lo = Mathf.Min(lo, h); hi = Mathf.Max(hi, h); }
            return hi - lo < drop;
        }

        bool InRing(LandContext ctx, Vector3 q, float pad)
        {
            foreach (var a in ctx.Artists) if (new Vector2(q.x - a.x, q.z - a.z).magnitude < a.r + pad) return true;
            return false;
        }

        /// Out behind the town (off the street's strip), on fairly level ground, apart from each other.
        Vector3 PlaceSpot(LandContext ctx, float clear)
        {
            var side = Vector3.Cross(Vector3.up, streetDir);
            for (var tries = 0; tries < 1500; tries++)
            {
                var t = ctx.Range(-10f, streetLen + 20f); var off = ctx.Range(44f + clear, tries > 900 ? 160f : 110f) * (ctx.Range(0f, 1f) < .5f ? -1f : 1f);
                var q = streetA + streetDir * t + side * off; float px = q.x, pz = q.z;
                if (!ctx.IsLand(px, pz, clear + 20f) || !ctx.IsFree(px, pz, clear) || OnTown(px, pz, clear) || InNoGrass(px, pz, clear) || InRing(ctx, q, clear + 2f)) continue;
                if (!Gentle(ctx, px, pz, clear + 2f, tries > 1100 ? clear * .5f : clear * .3f)) continue;
                var ok = true; foreach (var s in placeSpots) if (Vector2.Distance(new Vector2(s.x, s.z), new Vector2(px, pz)) < clear + 22f) { ok = false; break; }
                if (!ok) continue;
                var p = ctx.OnGround(px, pz); placeSpots.Add(p); ctx.AddKeepout(p.x, p.z, clear + 3f); noGrass.Add(new Vector3(p.x, clear + 6f, p.z)); return p;
            }
            return Vector3.positiveInfinity;
        }

        /// The tall neon sign with the song's name (seen over the rooftops) and the tour's mark. Returns the way the
        /// place faces (toward the street).
        Vector3 PlaceSign(LandContext ctx, MeshKit mk, Vector3 c, float clear, string sign, string mark, Color col)
        {
            var side = Vector3.Cross(Vector3.up, streetDir);
            var rel = new Vector3(c.x - streetA.x, 0, c.z - streetA.z); var across = Vector3.Dot(rel, side);
            var face = (across >= 0f ? -side : side).normalized; var sd = Vector3.Cross(Vector3.up, face);
            var sp = ctx.OnGround(c.x + face.x * (clear - .5f) + sd.x * 3f, c.z + face.z * (clear - .5f) + sd.z * 3f); var rot = Quaternion.LookRotation(face);
            mk.Prism(metal, sp - Vector3.up * .3f, Vector3.up, .12f, 7.3f, 6);
            var w = Mathf.Max(3f, sign.Length * .32f + .8f);
            mk.Box(dark, sp + Vector3.up * 6.4f, new Vector3(w, 1.2f, .18f), rot);
            mk.Box(WorldMaterials.Glow(col, 2.4f), sp + Vector3.up * 6.4f + face * .1f, new Vector3(w - .2f, .06f, .03f), rot);
            mk.Box(WorldMaterials.Glow(col, 2.4f), sp + Vector3.up * 5.85f + face * .1f, new Vector3(w - .2f, .06f, .03f), rot);
            ArtistBits.Label(ctx.Root, sign, sp + Vector3.up * 6.55f + face * .1f, face, .34f, col, TextAnchor.MiddleCenter);
            ArtistBits.Label(ctx.Root, sign, sp + Vector3.up * 6.55f - face * .1f, -face, .34f, col, TextAnchor.MiddleCenter);
            ArtistBits.PostCollider(ctx.Root, sp, 3f, .2f);
            halos.Add(sp + Vector3.up * 6.4f + face * .8f, 2.2f, col, .5f);
            var m = new GameObject("Shot: " + mark).transform; m.SetParent(ctx.Root, false);
            var cam = c + face * (clear + 13f) - sd * 3f;   // far enough back that the tall sign is in the picture
            foreach (var (fo, so) in new[] { (clear + 13f, -3f), (clear + 11f, 3f), (clear + 8f, -4f), (-(clear + 9f), 3f), (clear + 4f, 0f) })
            { var q = c + face * fo + sd * so; if (ctx.IsLand(q.x, q.z, 6f) && !InRing(ctx, q, 2f)) { cam = q; break; } }
            cam = ctx.OnGround(cam.x, cam.z) + Vector3.up * 1.3f;
            m.SetPositionAndRotation(cam, Quaternion.LookRotation((c + Vector3.up * 1.6f - cam).normalized));
            return face;
        }

        void SongPlaces(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            if (!hasStreet) return;
            placeSpots.Clear();
            var placed = 0;
            void Place(float clear, string sign, string mark, Color col, System.Action<Vector3, Vector3> build)
            {
                var c = PlaceSpot(ctx, clear); if (float.IsInfinity(c.x)) { Debug.Log("Garage Rock Row: no room for " + mark); return; }
                build(c, PlaceSign(ctx, mk, c, clear, sign, mark, col)); placed++;
            }
            Place(5f, "DO I WANNA KNOW?", "the phone booth", Red, (c, f) => PhoneBooth(ctx, mk, solid, c, f, DoIWannaKnow));
            Place(6f, "R U MINE?", "the question mark", Cyan, (c, f) => QuestionMark(ctx, mk, solid, c, f, "2AT8iROs4FQueDv2c8q2KE"));
            Place(6f, "CIGARETTE DAYDREAMS", "the overlook car", Yellow, (c, f) => OverlookCar(ctx, mk, solid, c, f, "2tznHmp70DxMyr2XhWLOW0"));
            Place(5f, "AIN'T NO REST FOR THE WICKED", "the busker's stage", Yellow, (c, f) => BuskerStage(ctx, mk, solid, c, f, "3Pzh926pXggbMe2ZpXyMV7"));
            Place(5f, "LAZY EYE", "the stack of TVs", Blue, (c, f) => TvStack(ctx, mk, solid, c, f, "3l24Vnw1YbWv8UEKaZLjCE"));
            Place(5f, "PANIC SWITCH", "the panic button", Red, (c, f) => PanicButton(ctx, mk, solid, c, f, "6lbSAmO1adMTCdcZlCf44e"));
            Place(6f, "NAIVE", "the ice-cream van", Hot, (c, f) => IceCreamVan(ctx, mk, solid, c, f, "2RUhtNBh43RtSg0WBPPq3m"));
            Place(6f, "SHE MOVES IN HER OWN WAY", "the dance floor", Hot, (c, f) => DanceFloor(ctx, mk, solid, c, f, "2SGBEDwsOAOAHrrdAd304i"));
            Place(7f, "ODE TO THE METS", "the ball field", Blue, (c, f) => BallField(ctx, mk, solid, c, f, "1BLOVHYYlH4JUHQGcpt75R"));
            Place(5f, "DID MY BEST", "the arcade cabinet", Cyan, (c, f) => Arcade(ctx, mk, solid, c, f, "5dSdDZlkvDUHitZxSRJrq7"));
            Debug.Log($"Garage Rock Row: {placed} song places");
        }

        // ---------------------------------------------------------------- Arctic Monkeys
        void PhoneBooth(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            var red = WorldMaterials.Solid(new Color(.6f, .1f, .1f)); var pane = WorldMaterials.Solid(new Color(.3f, .35f, .4f), .9f);
            mk.Box(curb, c + up * .05f, new Vector3(1.6f, .1f, 1.6f), rot);
            foreach (var s in new[] { -1f, 1f }) foreach (var t in new[] { -1f, 1f }) mk.Box(red, c + side * s * .58f + face * t * .58f + up * 1.25f, new Vector3(.14f, 2.4f, .14f), rot);
            mk.Box(pane, c - face * .58f + up * 1.2f, new Vector3(1.02f, 2f, .05f), rot);
            foreach (var s in new[] { -1f, 1f }) mk.Box(pane, c + side * s * .58f + up * 1.2f, new Vector3(.05f, 2f, 1.02f), rot);
            mk.Box(red, c + up * 2.55f, new Vector3(1.4f, .25f, 1.4f), rot);
            mk.Box(SongGlow(id, Red, 2.6f), c + up * 2.55f + face * .71f, new Vector3(.9f, .14f, .03f), rot);
            mk.Box(SongGlow(id, new Color(1f, .85f, .6f), 1.6f), c + up * 2.35f, new Vector3(.5f, .05f, .5f), rot);
            solid.Box(red, c - face * .1f + up * 1.2f, new Vector3(1.3f, 2.4f, 1.1f), rot);
            mk.Box(dark, c - face * .5f + up * 1.3f + side * .2f, new Vector3(.3f, .45f, .12f), rot);
            mk.Box(dark, c + face * .05f + up * .75f + side * .2f, new Vector3(.08f, .22f, .08f), rot * Quaternion.Euler(20f, 0, 0));   // the receiver, hanging
            mk.Box(dark, c - face * .2f + up * 1.05f + side * .2f, new Vector3(.02f, .02f, .7f), rot * Quaternion.Euler(-35f, 0, 0));
            Lit(ctx, c + up * 2f + face * 1.2f, Red);
        }

        void QuestionMark(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var side = Vector3.Cross(up, face); var neon = SongGlow(id, Cyan, 2.6f);
            var q = c - face * 1.5f; mk.Prism(metal, q, up, .08f, 3.4f, 6); ArtistBits.PostCollider(ctx.Root, q, 3.4f, .12f);
            var o = q + up * 5f; var prev = Vector3.zero;
            for (var k = 0; k <= 12; k++)
            {
                var a = Mathf.PI * (1.15f - k * 1.45f / 12f); var p = o + (side * Mathf.Cos(a) + up * Mathf.Sin(a)) * 1f;
                if (k > 0) mk.Box(neon, (prev + p) / 2f, new Vector3(.12f, .12f, Vector3.Distance(prev, p) + .1f), Quaternion.LookRotation(p - prev, face));
                prev = p;
            }
            mk.Box(neon, prev - up * .45f, new Vector3(.12f, .8f, .12f), Quaternion.LookRotation(face));
            mk.Blob(neon, o - up * 2.15f, Vector3.one * .16f, ctx.Rng, 0f);
            // the motorbike under it
            var b = c + face * .8f; var br = Quaternion.LookRotation(side);
            foreach (var s in new[] { -1f, 1f }) mk.Prism(dark, b + side * s * .7f + up * .35f - face * .08f, face, .34f, .16f, 12);
            mk.Box(metal, b + up * .65f, new Vector3(.25f, .3f, 1.2f), br); mk.Box(dark, b + up * .85f - side * .2f, new Vector3(.26f, .1f, .6f), br);
            mk.Box(metal, b + up * 1f + side * .55f, new Vector3(.7f, .04f, .04f), br * Quaternion.Euler(0, 90f, 0));
            mk.Prism(SongGlow(id, Yellow, 2.4f), b + up * .85f + side * .72f, side, .1f, .04f, 10);
            solid.Box(metal, b + up * .55f, new Vector3(.5f, 1.1f, 1.8f), br);
            Lit(ctx, o + face * 1.2f - up * 1f, Cyan, 3f);
        }

        // ---------------------------------------------------------------- Cage the Elephant
        void OverlookCar(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            var paint = WorldMaterials.Solid(new Color(.25f, .35f, .5f));
            mk.Box(paint, c + up * .65f, new Vector3(1.9f, .6f, 4.6f), rot); mk.Box(paint, c + up * 1.15f - face * .3f, new Vector3(1.7f, .5f, 2.2f), rot);
            mk.Box(SongGlow(id, new Color(1f, .8f, .45f), 1f), c + up * 1.15f - face * .3f, new Vector3(1.74f, .38f, 2f), rot);
            foreach (var s in new[] { -1f, 1f }) foreach (var t in new[] { -1f, 1f }) mk.Prism(dark, c + side * s * .98f + face * t * 1.5f + up * .35f, side * s, .35f, .2f, 10);
            foreach (var s in new[] { -1f, 1f }) mk.Box(SongGlow(id, Yellow, 3f), c + face * 2.31f + side * s * .65f + up * .7f, new Vector3(.3f, .16f, .03f), rot);
            solid.Box(paint, c + up * .7f, new Vector3(1.9f, 1.4f, 4.6f), rot);
            ArtistBits.PointLight(ctx.Root, c + face * 4f + up * .8f, Yellow, 16f, 2.2f);
            halos.Add(c + face * 2.6f + up * .7f, 1.2f, Yellow, .6f);
            var stub = c + side * 1.4f + face * 1.6f; mk.Box(curb, ctx.OnGround(stub.x, stub.z) + up * .03f, new Vector3(.12f, .05f, .12f), rot);
        }

        void BuskerStage(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            for (var k = 0; k < 3; k++) mk.Box(wood, c + side * (k - 1f) * 1.1f + up * .08f, new Vector3(1.05f, .16f, 2.4f), rot);
            solid.Box(wood, c + up * .08f, new Vector3(3.3f, .16f, 2.4f), rot);
            var mic = c + up * .16f; mk.Prism(metal, mic, up, .03f, 1.55f, 6); mk.Box(dark, mic + up * 1.6f, new Vector3(.1f, .2f, .1f), rot); ArtistBits.PostCollider(ctx.Root, mic, 1.6f, .15f);
            var amp = c - side * 1.1f - face * .6f + up * .16f; mk.Box(dark, amp + up * .4f, new Vector3(.7f, .8f, .4f), rot); mk.Box(SongGlow(id, Red, 2.4f), amp + up * .7f + face * .21f, new Vector3(.08f, .08f, .02f), rot);
            var case_ = c + face * 1.8f + side * .8f; case_ = ctx.OnGround(case_.x, case_.z);
            mk.Box(dark, case_ + up * .06f, new Vector3(.45f, .1f, 1.1f), rot * Quaternion.Euler(0, 20f, 0));
            mk.Box(WorldMaterials.Solid(new Color(.5f, .15f, .2f)), case_ + up * .1f, new Vector3(.38f, .03f, 1f), rot * Quaternion.Euler(0, 20f, 0));
            for (var k = 0; k < 6; k++) mk.Prism(SongGlow(id, Yellow, 1.6f), case_ + up * .12f + side * (k % 2 - .5f) * .15f + face * (k * .1f - .25f), up, .03f, .01f, 8);
            foreach (var s in new[] { -1f, 1f }) { var p = c - face * 1.6f + side * s * 2f; mk.Prism(metal, p, up, .06f, 3.6f, 6); ArtistBits.PostCollider(ctx.Root, p, 3.6f, .1f); }
            mk.Box(metal, c - face * 1.6f + up * 3.6f, new Vector3(4.2f, .1f, .1f), rot);
            mk.Prism(SongGlow(id, Yellow, 2.8f), c - face * 1.4f + up * 3.4f, (face - up).normalized, .22f, .3f, 10, 1.4f);
            ArtistBits.PointLight(ctx.Root, c + up * 2.2f, Yellow, 9f, 2.2f);
            halos.Add(c + up * 1.4f + face * .4f, 1.4f, Yellow, .5f);
        }

        // ---------------------------------------------------------------- Silversun Pickups
        void TvStack(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var side = Vector3.Cross(up, face); var shell = WorldMaterials.Solid(new Color(.3f, .27f, .25f));
            for (var row = 0; row < 3; row++)
                for (var col = 0; col < 4 - row; col++)
                {
                    var w = .9f - row * .05f; var p = c + side * ((col - (3 - row) / 2f) * 1f) + up * (.4f + row * .78f);
                    var tilt = Quaternion.LookRotation(face) * Quaternion.Euler(0, (col * 7 + row * 5) % 11 - 5f, 0);
                    mk.Box(shell, p, new Vector3(w, .74f, .7f), tilt);
                    var bright = (row + col) % 3 == 1 ? 3f : 1.8f;
                    mk.Box(SongGlow(id, (row + col) % 2 == 0 ? Blue : Cyan, bright), p + tilt * Vector3.forward * .36f, new Vector3(w - .22f, .5f, .03f), tilt);
                }
            solid.Box(shell, c + up * 1.2f, new Vector3(4.2f, 2.4f, .8f), Quaternion.LookRotation(face));
            Lit(ctx, c + up * 1.6f + face * 1.4f, Blue, 3f);
        }

        void PanicButton(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var yel = WorldMaterials.Solid(new Color(.95f, .8f, .15f));
            for (var k = 0; k < 6; k++) mk.Box(k % 2 == 0 ? yel : dark, c + up * (.1f + k * .18f), new Vector3(.8f, .18f, .8f), rot);
            solid.Box(dark, c + up * .55f, new Vector3(.8f, 1.1f, .8f), rot);
            mk.Box(metal, c + up * 1.13f, new Vector3(.9f, .06f, .9f), rot);
            mk.Prism(SongGlow(id, Red, 2.6f), c + up * 1.16f, up, .28f, .14f, 16, .9f);
            foreach (var s in new[] { -1f, 1f }) { var p = c + Vector3.Cross(up, face) * s * 1.8f; mk.Prism(metal, p, up, .05f, 2.2f, 6); mk.Prism(SongGlow(id, Yellow, 3f), p + up * 2.2f, up, .14f, .2f, 10, .8f); ArtistBits.PostCollider(ctx.Root, p, 2.2f, .1f); }
            Lit(ctx, c + up * 2f + face * .8f, Red);
        }

        // ---------------------------------------------------------------- The Kooks
        void IceCreamVan(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var side = Vector3.Cross(up, face); var rot = Quaternion.LookRotation(side);
            var white = WorldMaterials.Solid(new Color(.92f, .9f, .88f)); var pink = WorldMaterials.Solid(new Color(.9f, .55f, .7f));
            mk.Box(white, c + up * 1.4f, new Vector3(2.1f, 2.2f, 4.8f), rot); mk.Box(pink, c + up * .5f, new Vector3(2.12f, .3f, 4.82f), rot);
            mk.Box(white, c + side * 2.7f + up * .9f, new Vector3(2f, 1.2f, 1.2f), rot);
            foreach (var s in new[] { -1f, 1f }) foreach (var t in new[] { -1f, 1f }) mk.Prism(dark, c + face * s * 1.06f + side * t * 1.6f + up * .38f, face * s, .38f, .2f, 10);
            mk.Box(SongGlow(id, new Color(1f, .9f, .7f), 1.6f), c + face * 1.07f + up * 1.6f, new Vector3(.03f, .9f, 2f), rot);   // the serving hatch
            mk.Box(pink, c + face * 1.25f + up * 1.05f, new Vector3(.4f, .06f, 2.2f), rot);
            solid.Box(white, c + up * 1.2f, new Vector3(2.1f, 2.4f, 6f), rot);
            var cone = c + up * 2.5f + side * -.8f;
            mk.Prism(SongGlow(id, Yellow, 2.2f), cone + up * 1.1f, Vector3.down, .4f, 1f, 8, .1f);
            mk.Blob(SongGlow(id, Hot, 2.6f), cone + up * 1.35f, new Vector3(.5f, .4f, .5f), ctx.Rng, .1f);
            Lit(ctx, c + face * 2f + up * 2.2f, Hot);
        }

        void DanceFloor(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            for (var i = 0; i < 5; i++) for (var j = 0; j < 5; j++)
            {
                var col = (i + j) % 3 == 0 ? Hot : (i + j) % 3 == 1 ? Cyan : Yellow;
                mk.Box(SongGlow(id, col, 1.4f), c + side * (i - 2f) * 1.02f + face * (j - 2f) * 1.02f + up * .06f, new Vector3(1f, .12f, 1f), rot);
            }
            foreach (var s in new[] { -1f, 1f }) foreach (var t in new[] { -1f, 1f }) { var p = c + side * s * 3f + face * t * 3f; mk.Prism(metal, p, up, .06f, 4.4f, 6); ArtistBits.PostCollider(ctx.Root, p, 4.4f, .1f); }
            foreach (var s in new[] { -1f, 1f }) { mk.Box(metal, c + side * s * 3f + up * 4.4f, new Vector3(.08f, .08f, 6f), rot); mk.Box(metal, c + face * s * 3f + up * 4.4f, new Vector3(6f, .08f, .08f), rot); }
            mk.Box(metal, c + up * 4.05f, new Vector3(.02f, .7f, .02f), rot);
            mk.Blob(WorldMaterials.Solid(new Color(.85f, .88f, .95f), .95f), c + up * 3.5f, Vector3.one * .45f, ctx.Rng, 0f);
            halos.Add(c + up * 3.5f, 1.4f, new Color(.9f, .95f, 1f), .7f);
            ArtistBits.PointLight(ctx.Root, c + up * 2.6f, Hot, 12f, 2f);
        }

        // ---------------------------------------------------------------- The Strokes
        void BallField(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            var dirt = WorldMaterials.Solid(new Color(.45f, .33f, .25f)); var white = WorldMaterials.Solid(new Color(.95f, .95f, .93f));
            mk.Prism(dirt, c + up * .02f, up, 4.4f, .03f, 16);
            var plate = c - face * 2.4f; mk.Box(white, plate + up * .06f, new Vector3(.45f, .02f, .45f), rot * Quaternion.Euler(0, 45f, 0));
            foreach (var (a, d) in new[] { (45f, 3f), (-45f, 3f) }) { var q = plate + Quaternion.Euler(0, a, 0) * face * d; mk.Box(white, q + up * .07f, new Vector3(.35f, .06f, .35f), rot); }
            for (var k = -3; k <= 3; k++) { var p = plate - face * 1.8f + side * k * .9f; mk.Prism(metal, p, up, .04f, 3.2f, 6); }
            mk.Box(WorldMaterials.Solid(new Color(.4f, .42f, .45f), .3f), plate - face * 1.8f + up * 1.6f, new Vector3(5.6f, 3.2f, .03f), rot);
            solid.Box(metal, plate - face * 1.8f + up * 1.6f, new Vector3(5.6f, 3.2f, .2f), rot);
            var bench = c + side * 4.4f; mk.Box(wood, bench + up * .45f, new Vector3(.45f, .06f, 2.4f), rot); solid.Box(wood, bench + up * .25f, new Vector3(.45f, .5f, 2.4f), rot);
            const float PH = 6.5f; var pole = c + side * -4.6f - face * 1f; mk.Prism(metal, pole, up, .14f, PH, 8); ArtistBits.PostCollider(ctx.Root, pole, PH, .2f);
            mk.Box(dark, pole + up * PH, new Vector3(2f, 1.2f, .3f), rot);
            for (var i = 0; i < 3; i++) for (var j = 0; j < 2; j++) mk.Box(SongGlow(id, new Color(.9f, .95f, 1f), 3f), pole + up * (PH - .3f + j * .55f) + side * (i - 1f) * .6f + face * .16f, new Vector3(.45f, .4f, .03f), rot);
            var flag = WorldMaterials.Solid(new Color(.95f, .5f, .15f)); mk.Box(flag, bench + up * 1.4f, new Vector3(.03f, .35f, .9f), rot * Quaternion.Euler(0, 0, 0)); mk.Box(SongGlow(id, Blue, 2f), bench + up * 1.4f + side * .02f, new Vector3(.02f, .12f, .7f), rot);
            mk.Prism(wood, bench + up * .5f - face * .6f, up, .03f, 1.1f, 6);
            ArtistBits.PointLight(ctx.Root, pole + up * (PH - 1f) + face * 2f + side * 2f, new Color(.9f, .95f, 1f), 20f, 3f);
            ArtistBits.PointLight(ctx.Root, c + up * 3f, new Color(.9f, .95f, 1f), 12f, 1.6f);
            halos.Add(pole + up * PH + face * .8f, 1.8f, new Color(.9f, .95f, 1f), .6f);
        }

        // ---------------------------------------------------------------- The Voidz
        void Arcade(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            mk.Box(wood, c + up * .08f, new Vector3(1.4f, .16f, 1.2f), rot);
            var body = WorldMaterials.Solid(new Color(.15f, .12f, .22f));
            mk.Box(body, c + up * 1.05f, new Vector3(.9f, 1.8f, .8f), rot); solid.Box(body, c + up * 1f, new Vector3(1f, 2f, 1f), rot);
            mk.Box(SongGlow(id, Cyan, 2f), c + up * 1.45f + face * .38f, new Vector3(.65f, .55f, .04f), rot * Quaternion.Euler(-12f, 0, 0));
            for (var k = 0; k < 5; k++) mk.Box(SongGlow(id, k % 2 == 0 ? Hot : Yellow, 2.4f), c + up * (1.3f + k * .1f) + face * .42f + side * ((k * 37 % 7) / 7f - .45f) * .5f, new Vector3(.18f + (k % 3) * .1f, .025f, .02f), rot * Quaternion.Euler(-12f, 0, 0));
            mk.Box(dark, c + up * .95f + face * .5f, new Vector3(.9f, .08f, .35f), rot);
            mk.Prism(SongGlow(id, Red, 2.4f), c + up * 1f + face * .5f - side * .2f, up, .05f, .12f, 8);
            mk.Box(SongGlow(id, Hot, 2.6f), c + up * 2.05f + face * .3f, new Vector3(.85f, .25f, .04f), rot);
            mk.Box(metal, c + up * 2.4f - side * .3f, new Vector3(.02f, .5f, .02f), rot * Quaternion.Euler(0, 0, 25f));
            Lit(ctx, c + up * 1.8f + face * 1.2f, Cyan);
        }
    }
}
