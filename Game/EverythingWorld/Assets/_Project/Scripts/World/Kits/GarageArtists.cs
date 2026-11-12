using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// Garage Rock Row's garages: one per artist down both sides of the street, door rolled up, the band's gear
    /// set up inside (a drum kit, amps with their lights on, guitars on stands, a mic, a rug, a lamp, string
    /// lights), their covers on the back wall, their name in neon over the door, a plaque by it. And each their
    /// own touch: Arctic Monkeys (the biggest, nearest the casino) a giant neon soundwave across the front and a
    /// muscle car outside; Kings of Leon burn barrels; The Killers a frame of Vegas bulbs; The Black Keys two giant
    /// neon piano keys; Silversun Pickups a half-closed neon eye on the roof; The Kooks a camper van; The Strokes a
    /// Mets pennant; The Voidz a glitching sign; Cage the Elephant a getaway car with its doors open.
    public partial class GarageKit
    {
        public override void BuildArtist(LandContext ctx, IslandWorldData.Artist a)
        {
            if (asphalt == null) MakeMaterials();
            var root = new GameObject("Artist: " + a.name).transform; root.SetParent(ctx.Root, false);
            var labels = new GameObject("Labels").transform; labels.SetParent(root, false);
            var area = root.gameObject.AddComponent<ArtistArea>();
            area.artistName = a.name; area.stationId = a.stationId; area.radius = a.r; area.glowColor = Sodium;
            var mk = new MeshKit(); var solid = new MeshKit(); var pictures = new MeshKit();
            var up = Vector3.up; var sideV = Vector3.Cross(Vector3.up, streetDir);
            var pos = new Vector3(a.x, 0, a.z);
            var s = Vector3.Dot(pos - streetA, sideV) >= 0f ? 1f : -1f;
            var face = hasStreet ? -sideV * s : new Vector3(center.x - a.x, 0, center.z - a.z).normalized;
            var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(Vector3.up, face);
            var am = a.name == "Arctic Monkeys";
            float W = am ? 13f : 9f, D = am ? 12f : 10f, H = am ? 6f : 4.6f, T = .3f, doorW = W - 2f, doorH = am ? 4.2f : 3.4f;
            var c = ctx.OnGround(a.x, a.z) + face * 2f;
            if (hasStreet) c.y = StreetY(Vector3.Dot(c - streetA, streetDir)) + .05f;
            var wallM = WorldMaterials.Solid(Paint[Mathf.Abs(a.name.GetHashCode()) % Paint.Length]);
            var concrete = WorldMaterials.Solid(new Color(.45f, .45f, .46f));
            void Wall(Vector3 p, Vector3 size) { mk.Box(wallM, p, size, rot); solid.Box(wallM, p, size, rot); }
            // the floor, walls (the sides fit between front and back), the roof, the header over the door
            mk.Box(concrete, c + up * .05f, new Vector3(W, .1f, D), rot);
            Wall(c - face * (D / 2f - T / 2f) + up * H / 2f, new Vector3(W, H, T));
            foreach (var x in new[] { -1f, 1f }) Wall(c + side * x * (W / 2f - T / 2f) + up * H / 2f, new Vector3(T, H, D - 2f * T));
            var fz = c + face * (D / 2f - T / 2f);
            foreach (var x in new[] { -1f, 1f }) Wall(fz + side * x * (doorW / 2f + (W - doorW) / 4f) + up * H / 2f, new Vector3((W - doorW) / 2f, H, T));
            Wall(fz + up * (doorH + (H - doorH) / 2f), new Vector3(doorW, H - doorH, T));
            mk.Box(dark, c + up * (H + .15f), new Vector3(W + .3f, .3f, D + .3f), rot);
            // the roll-up door, rolled up under the ceiling
            mk.Box(WorldMaterials.Solid(new Color(.6f, .6f, .62f)), fz - face * .4f + up * (doorH - .25f), new Vector3(doorW, .5f, .7f), rot);
            IndoorSpaces.Add(c + up * H / 2f, rot, new Vector3(W, H, D));
            // inside: covers on the back wall, the band's gear on a rug
            ArtistBits.CoverWall(pictures, labels, ctx, a, c - face * (D / 2f - T - .03f) + up * (H * .62f), face, Mathf.Min(4, a.songList?.Length ?? 1), .85f, 8, true, Ink);
            Gear(ctx, mk, solid, c, face, side, Mathf.Abs(a.name.GetHashCode()));
            // string lights across the ceiling, a warm light inside
            for (var k = 0; k < 2; k++)
            {
                var y = H - .4f; var a0 = c + side * (W / 2f - .4f) + face * ((k - .5f) * D * .5f) + up * y; var a1 = a0 - side * (W - .8f);
                for (var j = 0; j <= 12; j++) { var p = Vector3.Lerp(a0, a1, j / 12f) - up * Mathf.Sin(j / 12f * Mathf.PI) * .3f; mk.Blob(GlowM(new Color(1f, .8f, .5f), 2.4f), p, Vector3.one * .06f, ctx.Rng, 0f); }
            }
            var glow = ArtistBits.PointLight(root, c + up * (H - .8f), new Color(1f, .92f, .75f), am ? 18f : 14f, 2.4f, true);
            ArtistBits.PointLight(root, fz + face * 3f + up * 3f, new Color(.7f, .95f, 1f), 12f, 1.6f);          // a cool work light spilling out of the door
            area.glowLight = glow;
            halos.Add(c + up * (H - .7f), 1.2f, new Color(1f, .72f, .45f), .5f);           // small and up under the ceiling: a big one by the door hazed the whole view
            // the band's name in neon over the door
            var col = NeonC[Mathf.Abs(a.name.GetHashCode() / 5) % NeonC.Length];
            var signAt = fz + face * .25f + up * (doorH + (H - doorH) / 2f);
            ArtistBits.Label(labels, a.name, signAt + face * .05f, face, am ? .55f : .42f, col, TextAnchor.MiddleCenter);
            mk.Box(GlowM(col, 2.4f), signAt - up * ((H - doorH) / 2f - .1f) + face * .02f, new Vector3(doorW, .06f, .04f), rot);
            halos.Add(signAt + face * 1.4f, 1.8f, col, .8f);
            // the plaque by the door
            ArtistBits.Plaque(mk, solid, labels, a, ctx.OnGround(fz.x + face.x * 2.4f + side.x * (W / 2f + 1.2f), fz.z + face.z * 2.4f + side.z * (W / 2f + 1.2f)), face, wood, dark, Ink, 1.5f);
            var outside = fz + face * 3.2f; outside.y = c.y;
            switch (a.name)
            {
                case "Arctic Monkeys": Soundwave(ctx, mk, fz + face * .2f + up * (H + 1.8f), face, side, W); Car(ctx, mk, solid, outside + side * (W / 2f + 1f) + face * 1f, streetDir, new Color(.08f, .08f, .1f), true); JacketHook(ctx, mk, c - face * (D / 2f - .5f) + side * (W / 2f - 1.2f) + up * 1.7f, face); break;
                case "Kings of Leon": foreach (var x in new[] { -1f, 1f }) BurnBarrel(ctx, mk, outside + side * x * (W / 2f + .8f)); break;
                case "The Killers": VegasFrame(ctx, mk, fz + face * .2f, face, side, doorW, doorH); break;
                case "The Black Keys": PianoKeys(ctx, mk, fz + face * .2f + up * (H + 1.6f), face, side); break;
                case "Silversun Pickups": NeonEye(ctx, mk, c + up * (H + 1.8f) + face * (D / 2f - 1f), face, side); break;
                case "The Kooks": Van(ctx, mk, solid, outside + side * (W / 2f + 1.5f), streetDir); break;
                case "The Strokes": Pennant(ctx, mk, fz + face * .2f + side * (W / 2f - .8f) + up * (H - .6f), face, side); break;
                case "The Voidz": Glitch(ctx, labels, mk, signAt + up * 1.6f, face, a.name); break;
                case "Cage The Elephant": Car(ctx, mk, solid, outside + side * (W / 2f + 1f), streetDir, new Color(.55f, .12f, .1f), true); break;
                default: if (Mathf.Abs(a.name.GetHashCode()) % 2 == 0) Couch(ctx, mk, solid, outside - side * (W / 2f + .5f), face); else BurnBarrel(ctx, mk, outside + side * (W / 2f + .8f)); break;
            }
            area.glowRenderers = new Renderer[0];
            mk.Build("Garage", root, false);
            solid.BuildCollider("Garage walls", root);
            pictures.Build("Covers", root, false);
            noGrass.Add(new Vector3(c.x, Mathf.Max(W, D) * .8f, c.z));
        }

        /// A drum kit, two amps with their lights on, guitars on stands, a mic stand and a rug.
        void Gear(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, Vector3 side, int seed)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face);
            var rugCol = new[] { new Color(.5f, .15f, .15f), new Color(.2f, .25f, .45f), new Color(.45f, .35f, .2f) }[seed % 3];
            mk.Box(WorldMaterials.Solid(rugCol), c + up * .11f - face * 1f, new Vector3(5f, .02f, 4f), rot);
            var kit = c - face * 2.4f + up * .1f;
            var shell = WorldMaterials.Solid(new[] { new Color(.75f, .1f, .12f), new Color(.1f, .1f, .12f), new Color(.85f, .82f, .75f) }[seed % 3], .7f);
            mk.Prism(shell, kit + up * .45f - side * .3f, side, .45f, .6f, 14);
            mk.Prism(WorldMaterials.Solid(Ink), kit + up * .45f + side * .31f, side, .42f, .01f, 14);
            foreach (var (x, h) in new[] { (-.7f, .85f), (.7f, .9f), (1f, .55f) }) mk.Prism(shell, kit + side * x + face * .3f + up * h, up, .25f, .25f, 10);
            var brass = WorldMaterials.Solid(new Color(.85f, .7f, .3f), .8f);
            foreach (var x in new[] { -1.2f, 1.3f }) { var p = kit + side * x - face * .2f; mk.Prism(metal, p, up, .02f, 1.3f, 5); mk.Prism(brass, p + up * 1.3f, up, .4f, .02f, 12); ArtistBits.PostCollider(ctx.Root, p, 1.35f, .35f); }   // the cymbal at chest height
            solid.Box(shell, kit + up * .5f, new Vector3(2.6f, 1f, 1.4f), Quaternion.LookRotation(face));
            foreach (var x in new[] { -1f, 1f })
            {
                var amp = c + side * x * 2.6f - face * 1.6f;
                mk.Box(dark, amp + up * .55f, new Vector3(1f, 1.1f, .6f), rot);
                mk.Box(WorldMaterials.Solid(new Color(.3f, .28f, .26f)), amp + up * .5f + face * .31f, new Vector3(.85f, .75f, .02f), rot);
                mk.Box(GlowM(new Color(1f, .2f, .15f), 2.4f), amp + up * 1f + face * .31f + side * .35f, Vector3.one * .05f, rot);
                solid.Box(dark, amp + up * .55f, new Vector3(1f, 1.1f, .6f), rot);
                // a guitar on a stand beside it
                var g = amp + face * 1.2f + up * .1f;
                var wood2 = WorldMaterials.Solid(new[] { new Color(.75f, .5f, .25f), new Color(.15f, .15f, .2f), new Color(.8f, .15f, .2f), new Color(.85f, .85f, .8f) }[(seed + (x > 0 ? 1 : 0)) % 4], .7f);
                mk.Blob(wood2, g + up * .4f, new Vector3(.24f, .32f, .08f), ctx.Rng, 0f);
                mk.Box(dark, g + up * 1f, new Vector3(.06f, .75f, .05f), rot * Quaternion.Euler(-8f, 0, 0));
                solid.Box(dark, g + up * .7f, new Vector3(.6f, 1.4f, .4f), rot);
            }
            var mic = c + face * .5f; mk.Prism(metal, mic + up * .1f, up, .02f, 1.5f, 5); mk.Blob(dark, mic + up * 1.65f, Vector3.one * .07f, ctx.Rng, 0f);
            solid.Box(metal, mic + up * .9f, new Vector3(.3f, 1.7f, .3f), rot);
            var lamp = c - side * 1.6f + face * .8f;
            mk.Prism(metal, lamp + up * .1f, up, .03f, 1.4f, 5);
            mk.Prism(WorldMaterials.Solid(new Color(.9f, .8f, .6f)), lamp + up * 1.45f, up, .25f, .3f, 10, .6f);
            mk.Blob(GlowM(new Color(1f, .8f, .5f), 2.4f), lamp + up * 1.5f, Vector3.one * .12f, ctx.Rng, 0f);
            solid.Box(metal, lamp + up * .85f, new Vector3(.5f, 1.7f, .5f), rot);
        }

        void Soundwave(LandContext ctx, MeshKit mk, Vector3 at, Vector3 face, Vector3 side, float width)
        {
            var white = GlowM(new Color(.95f, .97f, 1f), 2.6f); var rot = Quaternion.LookRotation(face);
            mk.Box(dark, at, new Vector3(width + 1f, 3f, .2f), rot);
            var n = 41;
            for (var k = 0; k < n; k++)
            {
                var t = k / (n - 1f);
                var h = (.15f + Mathf.Abs(Mathf.Sin(t * 19f) * Mathf.Sin(t * 5.3f + .6f)) * 1.2f) * Mathf.Sin(t * Mathf.PI);
                mk.Box(white, at + side * ((t - .5f) * width) + face * .12f, new Vector3(.12f, h * 2f + .06f, .04f), rot);
            }
            halos.Add(at + face * 2f, 4f, new Color(.9f, .95f, 1f), .7f);
            life.wave = at + face * .5f;
        }

        void JacketHook(LandContext ctx, MeshKit mk, Vector3 at, Vector3 face)
        {
            var leather = WorldMaterials.Solid(new Color(.08f, .07f, .07f), .6f);
            mk.Box(metal, at + Vector3.up * .45f, new Vector3(.06f, .06f, .2f), Quaternion.LookRotation(face));
            mk.Box(leather, at, new Vector3(.75f, .9f, .18f), Quaternion.LookRotation(face));
            foreach (var s in new[] { -1f, 1f }) mk.Box(leather, at + Vector3.Cross(Vector3.up, face) * s * .45f - Vector3.up * .1f, new Vector3(.18f, .75f, .16f), Quaternion.LookRotation(face));
        }

        void BurnBarrel(LandContext ctx, MeshKit mk, Vector3 at)
        {
            var up = Vector3.up; at = ctx.OnGround(at.x, at.z);
            mk.Prism(WorldMaterials.Solid(new Color(.35f, .22f, .16f)), at, up, .42f, 1f, 10);
            mk.Blob(GlowM(new Color(1f, .5f, .15f), 2.6f), at + up * 1.05f, new Vector3(.32f, .22f, .32f), ctx.Rng, .3f);
            halos.Add(at + up * 1.4f, 1.6f, new Color(1f, .5f, .15f), 1f);
            var l = ArtistBits.PointLight(ctx.Root, at + up * 1.6f, new Color(1f, .55f, .2f), 8f, 1.2f);
            l.gameObject.AddComponent<FireFlicker>().baseIntensity = 1.2f;
            ArtistBits.PostCollider(ctx.Root, at, 1f, .45f);
            life.barrels.Add(at + up * 1.05f);
        }

        void VegasFrame(LandContext ctx, MeshKit mk, Vector3 at, Vector3 face, Vector3 side, float w, float h)
        {
            var up = Vector3.up; var bulbs = GlowM(new Color(1f, .85f, .45f), 2.6f);
            for (var x = -w / 2f; x <= w / 2f; x += .4f) foreach (var y in new[] { .05f, h + .15f }) mk.Blob(bulbs, at + side * x + up * y, Vector3.one * .08f, ctx.Rng, 0f);
            for (var y = 0f; y <= h; y += .4f) foreach (var x in new[] { -w / 2f, w / 2f }) mk.Blob(bulbs, at + side * x + up * y, Vector3.one * .08f, ctx.Rng, 0f);
            var star = at + up * (h + 2.6f);
            for (var k = 0; k < 5; k++) { var d = Quaternion.AngleAxis(k * 72f, face) * up; mk.Prism(GlowM(new Color(1f, .3f, .4f), 2.6f), star, d, .25f, 1f, 5, .05f); }
            halos.Add(star + face, 2f, new Color(1f, .4f, .5f), 1f);
        }

        void PianoKeys(LandContext ctx, MeshKit mk, Vector3 at, Vector3 face, Vector3 side)
        {
            var rot = Quaternion.LookRotation(face);
            for (var k = 0; k < 7; k++) mk.Box(GlowM(new Color(.95f, .95f, 1f), 2f), at + side * ((k - 3f) * .75f) + face * .1f, new Vector3(.65f, 2.4f, .05f), rot);
            foreach (var k in new[] { -2.25f, -1.5f, 0f, .75f, 1.5f }) mk.Box(dark, at + side * k + Vector3.up * .45f + face * .15f, new Vector3(.4f, 1.5f, .06f), rot);
            halos.Add(at + face * 2f, 3f, new Color(.9f, .95f, 1f), .6f);
        }

        void NeonEye(LandContext ctx, MeshKit mk, Vector3 at, Vector3 face, Vector3 side)
        {
            var up = Vector3.up; var tube = GlowM(new Color(.3f, .9f, 1f), 2.6f);
            Vector3 L(float t, float top) { var x = (t - .5f) * 4f; var y = (1f - (2f * t - 1f) * (2f * t - 1f)) * (top > 0 ? .55f : -1.1f); return at + side * x + up * y; }
            foreach (var top in new[] { 1f, -1f }) for (var k = 0; k < 16; k++) { var a = L(k / 16f, top); var b = L((k + 1) / 16f, top); mk.Box(tube, (a + b) / 2f, new Vector3(.1f, .1f, Vector3.Distance(a, b) + .02f), Quaternion.LookRotation(b - a)); }
            mk.Prism(GlowM(new Color(1f, .3f, .6f), 2.6f), at - up * .15f - face * .05f, face, .4f, .05f, 14);
            for (var s = -1; s <= 1; s += 2) mk.Prism(metal, at + side * s * 1.6f - up * 1.8f, up, .06f, 1.6f, 5);
            halos.Add(at + face * 1.5f, 2.6f, new Color(.4f, .9f, 1f), .9f);
        }

        void Van(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at, Vector3 dir)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(dir); var side = Vector3.Cross(Vector3.up, dir);
            var top = WorldMaterials.Solid(new Color(.95f, .94f, .88f)); var bottom = WorldMaterials.Solid(new Color(.4f, .65f, .75f));
            mk.Box(bottom, at + up * .8f, new Vector3(2f, 1f, 4.4f), rot);
            mk.Box(top, at + up * 1.75f, new Vector3(2f, .9f, 4.4f), rot);
            mk.Box(WorldMaterials.Solid(new Color(.15f, .2f, .25f), .9f), at + up * 1.75f + dir * 2.21f, new Vector3(1.7f, .6f, .02f), rot);
            foreach (var s in new[] { -1f, 1f }) foreach (var z in new[] { -1.4f, 1.4f }) mk.Prism(dark, at + side * s * 1f + dir * z + up * .38f - side * s * .12f, side * s, .38f, .25f, 12);
            mk.Box(WorldMaterials.Solid(new Color(1f, .8f, .3f)), at + up * 2.3f, new Vector3(.5f, .08f, 3.4f), rot * Quaternion.Euler(0, 0, 4f));
            solid.Box(top, at + up * 1.2f, new Vector3(2f, 2.4f, 4.4f), rot);
        }

        void Pennant(LandContext ctx, MeshKit mk, Vector3 at, Vector3 face, Vector3 side)
        {
            var blue = WorldMaterials.Solid(new Color(.1f, .25f, .6f)); var orange = WorldMaterials.Solid(new Color(1f, .5f, .1f));
            var down = -Vector3.up;
            mk.Tri(blue, at, at + down * 1.2f, at - side * 3f + down * .6f, at - face);
            mk.Tri(orange, at + face * .01f + down * .2f, at + face * .01f + down * 1f, at - side * 2.2f + down * .6f + face * .01f, at - face);
            ArtistBits.Label(ctx.Root, "NY", at - side * .8f + down * .6f + face * .03f, face, .25f, Ink, TextAnchor.MiddleCenter);
        }

        void Glitch(LandContext ctx, Transform labels, MeshKit mk, Vector3 at, Vector3 face, string name)
        {
            var side = Vector3.Cross(Vector3.up, face);
            ArtistBits.Label(labels, name.ToUpper(), at + face * .02f + side * .06f, face, .5f, new Color(1f, .2f, .3f), TextAnchor.MiddleCenter);
            ArtistBits.Label(labels, name.ToUpper(), at + face * .03f - side * .06f + Vector3.up * .03f, face, .5f, new Color(.2f, 1f, 1f), TextAnchor.MiddleCenter);
            for (var k = 0; k < 6; k++) mk.Box(GlowM(k % 2 == 0 ? new Color(1f, .2f, .3f) : new Color(.2f, 1f, 1f), 2.4f), at + side * ctx.Range(-1.6f, 1.6f) + Vector3.up * ctx.Range(-.5f, .5f) + face * .04f, new Vector3(ctx.Range(.3f, 1.2f), .05f, .02f), Quaternion.LookRotation(face));
            halos.Add(at + face * 1.2f, 2f, new Color(.6f, .6f, 1f), .7f);
        }

        void Couch(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at, Vector3 face)
        {
            var up = Vector3.up; at = ctx.OnGround(at.x, at.z); var rot = Quaternion.LookRotation(face);
            var fabric = WorldMaterials.Solid(new Color(.45f, .3f, .25f));
            mk.Box(fabric, at + up * .3f, new Vector3(2f, .5f, .9f), rot);
            mk.Box(fabric, at + up * .75f - face * .38f, new Vector3(2f, .6f, .2f), rot);
            foreach (var s in new[] { -.95f, .95f }) mk.Box(fabric, at + Vector3.Cross(Vector3.up, face) * s + up * .5f, new Vector3(.2f, .45f, .9f), rot);
            solid.Box(fabric, at + up * .5f, new Vector3(2.1f, 1f, .9f), rot);
        }
    }
}
