using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Meadows' artist areas: small daydream scenes off the loop, facing it, each with a plaque, a board of
    /// covers and a light. Fabric wears a soft version of the colour of whoever added the artist. Clairo: a
    /// giant swing hanging from a great tree full of fairy lights. beabadoobee: a blanket fort you can walk into,
    /// glowing inside. Everyone else: a hammock, a beanbag and lamp, a heap of pillows, a swing, a paper-star
    /// mobile or a big plush bear. The older-generation artists' scenes are faded, like an older dream.
    public partial class MeadowsKit
    {
        static readonly HashSet<string> OlderDream = new HashSet<string> { "The Postal Service", "fun.", "Foster The People", "The Apples In Stereo" };
        readonly System.Random fairyRng = new System.Random(11);

        static int Hash(string s) { var h = 17; foreach (var ch in s ?? "") h = h * 31 + ch; return h & 0x7fffffff; }

        Color Soft(IslandWorldData.Artist a, int i)
        {
            bool n = !string.IsNullOrEmpty(a.addedBy) && a.addedBy.Contains("npgallagher"), s = !string.IsNullOrEmpty(a.addedBy) && a.addedBy.Contains("Pyrosolstice");
            var c = n && s ? (i % 2 == 0 ? NoahBlue : SeanGreen) : s ? SeanGreen : n ? NoahBlue : new Color(.9f, .6f, .7f);
            c = Color.Lerp(c, Cream, .55f);
            if (OlderDream.Contains(a.name)) c = Color.Lerp(c, new Color(.82f, .76f, .66f), .55f);   // faded, like an old photo
            return c;
        }

        class Area
        {
            public LandContext ctx; public Transform root, labels; public MeshKit mk, solid, pictures;
            public IslandWorldData.Artist a; public Vector3 center, front, side; public List<Renderer> glows; public ArtistArea area;
            public Vector3 At(float forward, float right) { var p = center + front * forward + side * right; return ctx.OnGround(p.x, p.z); }
        }

        public override void BuildArtist(LandContext ctx, IslandWorldData.Artist a)
        {
            if (wood == null) MakeMaterials();
            var root = new GameObject("Artist: " + a.name).transform; root.SetParent(ctx.Root, false);
            var front = facing.TryGetValue(a.name, out var f) ? f : new Vector3(center.x - a.x, 0, center.z - a.z).normalized;
            if (front.sqrMagnitude < .01f) front = Vector3.forward;
            var mk = new MeshKit(); var solid = new MeshKit(); var pictures = new MeshKit();
            var labels = new GameObject("Labels").transform; labels.SetParent(root, false);
            var area = root.gameObject.AddComponent<ArtistArea>();
            area.artistName = a.name; area.stationId = a.stationId; area.radius = a.r; area.glowColor = FairyWarm;
            var glows = new List<Renderer>();
            var z = new Area { ctx = ctx, root = root, mk = mk, solid = solid, pictures = pictures, labels = labels, a = a, center = ctx.OnGround(a.x, a.z), front = front, side = Vector3.Cross(Vector3.up, front), glows = glows, area = area };
            if (a.name == "Clairo") Swing(z, true);
            else if (a.name == "beabadoobee") Fort(z);
            else switch (Hash(a.name) % 6)
            {
                case 0: Hammock(z); break;
                case 1: Beanbag(z); break;
                case 2: Pillows(z); break;
                case 3: Swing(z, false); break;
                case 4: Mobile(z); break;
                default: Plush(z); break;
            }
            if (!(a.name == "Clairo" || a.name == "beabadoobee")) { CoverBoard(z, z.At(-1.6f, -2.6f), front); Plaque(z, 2.4f, 2.4f); SmallSongPiece(z); }
            glows.Add(ArtistBits.LanternPost(mk, root, z.At(1.4f, -3.6f), z.side, darkWood, iron, FairyWarm, 2.6f));
            // a warm light over every daydream (brighter for the radio stations), swelling while the artist plays
            var big = a.stationId != 0;
            var lamp = ArtistBits.PointLight(root, z.At(.5f, -1.5f) + Vector3.up * (big ? 4f : 2.8f), FairyWarm, big ? 18f : 12f, big ? 1.3f : .9f);
            if (area.glowLight == null) area.glowLight = lamp;
            foreach (var g in glows) if (g != null) halos.Add(g.transform.position, 1.1f, FairyWarm, .8f);
            area.glowRenderers = glows.ToArray();
            mk.Build("Area", root, false);
            solid.BuildCollider("Walls", root);
            pictures.Build("Covers", root, false);
        }

        void Plaque(Area z, float forward, float right) =>
            ArtistBits.Plaque(z.mk, z.solid, z.labels, z.a, z.At(forward, right), z.front, darkWood, board, Cream, 1.5f);

        void CoverBoard(Area z, Vector3 ground, Vector3 facingDir, float size = .8f)
        {
            facingDir = Vector3.ProjectOnPlane(facingDir, Vector3.up).normalized;
            var rot = Quaternion.LookRotation(facingDir);
            var side = Vector3.Cross(Vector3.up, facingDir);
            var n = Mathf.Max(1, Mathf.Min(3, z.a.songList?.Length ?? 1));
            var w = n * size * 1.25f + .3f;
            foreach (var s in new[] { -1f, 1f })
            {
                z.mk.Prism(darkWood, ground + side * s * (w / 2f - .1f) - facingDir * .05f, Vector3.up, .06f, 2.1f, 5);
                ArtistBits.PostCollider(z.root, ground + side * s * (w / 2f - .1f) - facingDir * .05f, 2.1f, .1f);
            }
            var c = ground + Vector3.up * 1.6f;
            z.mk.Box(board, c - Vector3.up * .1f, new Vector3(w, size + .55f, .06f), rot);
            z.solid.Box(board, c - Vector3.up * .1f, new Vector3(w, size + .55f, .15f), rot);
            ArtistBits.CoverWall(z.pictures, z.labels, z.ctx, z.a, c + facingDir * .035f + Vector3.up * .08f, facingDir, n, size, n, true, Cream);
        }

        void BigTree(Area z, Vector3 at, float scale, bool lights)
        {
            var h = 5f * scale; var r = 4.2f * scale;
            z.mk.Prism(bark, at, Vector3.up, .45f * scale, h + 1.5f, 8, .65f);
            z.mk.Blob(leafB, at + Vector3.up * (h + r * .5f), new Vector3(r, r * .8f, r), z.ctx.Rng, .14f);
            z.mk.Blob(leafA, at + Vector3.up * (h + r * .3f) + new Vector3(r * .5f, 0, r * .3f), new Vector3(r * .7f, r * .6f, r * .7f), z.ctx.Rng, .14f);
            ArtistBits.PostCollider(z.root, at, h, .5f * scale);
            if (!lights) return;
            for (var k = 0; k < 4; k++)
            {
                var a0 = k * Mathf.PI / 2f; var a1 = a0 + Mathf.PI * .7f;
                var p0 = at + Vector3.up * (h + .4f) + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * r * .85f;
                var p1 = at + Vector3.up * (h + .9f) + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * r * .85f;
                FairyLights(z.mk, p0, p1, 1.4f, k % 2 == 0 ? FairyWarm : Firefly);
            }
        }

        // Clairo (and some smaller ones): a swing hanging from a great tree
        void Swing(Area z, bool grand)
        {
            var scale = grand ? 1.8f : 1f;
            var tree = z.At(-3f * scale, 0);
            if (grand) BigTree(z, tree, scale, true);
            else
            {
                // an A-frame for the smaller ones
                foreach (var s in new[] { -1f, 1f })
                foreach (var t in new[] { -1f, 1f })
                {
                    var foot = z.At(-1.2f + t * .9f, s * 1.6f); var top = z.At(-1.2f, s * 1.6f) + Vector3.up * 3.6f; var d = top - foot;
                    z.mk.Box(wood, (foot + top) / 2f, new Vector3(.14f, d.magnitude, .14f), Quaternion.LookRotation(z.side, d.normalized));
                    z.solid.Box(wood, (foot + top) / 2f, new Vector3(.2f, d.magnitude, .2f), Quaternion.LookRotation(z.side, d.normalized));
                }
                var l = z.At(-1.2f, -1.7f) + Vector3.up * 3.6f; var r = z.At(-1.2f, 1.7f) + Vector3.up * 3.6f;
                z.mk.Box(wood, (l + r) / 2f, new Vector3(.18f, .18f, Vector3.Distance(l, r)), Quaternion.LookRotation(r - l));
            }
            var branchY = grand ? 5f * scale + .6f : 3.6f;
            var hang = z.At(grand ? -1f : -1.2f, 0) ;
            var seat = hang + Vector3.up * (grand ? 1.1f : .6f);
            var width = grand ? 2.6f : 1.2f;
            foreach (var s in new[] { -1f, 1f })
            {
                var a = seat + z.side * s * width / 2f; var b = new Vector3(a.x, hang.y + branchY, a.z);
                z.mk.Box(rope, (a + b) / 2f, new Vector3(.05f, b.y - a.y, .05f), Quaternion.identity);
                z.solid.Box(rope, (a + b) / 2f, new Vector3(.12f, b.y - a.y, .12f), Quaternion.identity);
            }
            z.mk.Box(WorldMaterials.Solid(Soft(z.a, 0)), seat, new Vector3(width + .3f, .12f, grand ? 1.1f : .5f), Quaternion.LookRotation(z.front));
            z.solid.Box(wood, seat, new Vector3(width + .3f, .3f, grand ? 1.1f : .5f), Quaternion.LookRotation(z.front));
            if (grand)
            {
                CoverBoard(z, z.At(2.2f, -4.5f), z.front, 1f);
                Plaque(z, 3f, 4.2f);
                var blanket = WorldMaterials.Solid(Soft(z.a, 1));
                z.mk.Box(blanket, z.At(2.5f, 1.5f) + Vector3.up * .03f, new Vector3(3.4f, .05f, 2.6f), Quaternion.LookRotation(z.front) * Quaternion.Euler(0, 14, 0));
            }
        }

        // beabadoobee: a blanket fort big enough to walk into, glowing inside
        void Fort(Area z)
        {
            var c = z.At(-2f, 0);
            var rot = Quaternion.LookRotation(z.front);
            const float W = 8f, D = 7f, H = 3.6f;
            var sheetA = WorldMaterials.Solid(Soft(z.a, 0)); var sheetB = WorldMaterials.Solid(new Color(.95f, .86f, .9f));
            // a ridge pole on two chair-backs, sheets draped either side down to the ground
            foreach (var s in new[] { -1f, 1f })
            {
                z.mk.Box(wood, c + z.front * s * (D / 2f + (s < 0 ? .2f : -.2f)) + Vector3.up * H / 2f, new Vector3(.25f, H, .25f), rot);
                z.solid.Box(wood, c + z.front * s * (D / 2f + (s < 0 ? .2f : -.2f)) + Vector3.up * H / 2f, new Vector3(.3f, H, .3f), rot);
            }
            z.mk.Box(wood, c + Vector3.up * H, new Vector3(.2f, .2f, D + .4f), rot);
            foreach (var s in new[] { -1f, 1f })
            {
                Vector3 top = c + Vector3.up * H, bottom = c + z.side * s * W / 2f + Vector3.up * .05f;
                var mid = (top + bottom) / 2f - z.side * s * .25f;
                var d = top - bottom;
                var r = Quaternion.LookRotation(z.front, Vector3.Cross(z.front, d).normalized * -s);
                z.mk.Box(s < 0 ? sheetA : sheetB, mid, new Vector3(d.magnitude + .3f, .06f, D), Quaternion.LookRotation(z.front) * Quaternion.Euler(0, 0, Mathf.Atan2(H, W / 2f) * Mathf.Rad2Deg * -s));
                z.solid.Box(sheetA, mid, new Vector3(d.magnitude, .3f, D), Quaternion.LookRotation(z.front) * Quaternion.Euler(0, 0, Mathf.Atan2(H, W / 2f) * Mathf.Rad2Deg * -s));
            }
            // the back is a hanging sheet; inside, cushions, covers and fairy lights along the ridge
            z.mk.Box(sheetB, c - z.front * (D / 2f) + Vector3.up * H / 2f, new Vector3(W * .9f, H, .06f), rot);
            z.solid.Box(sheetB, c - z.front * (D / 2f) + Vector3.up * H / 2f, new Vector3(W * .9f, H, .2f), rot);
            for (var i = 0; i < 4; i++)
            {
                z.mk.Blob(WorldMaterials.Solid(Pastel[i % Pastel.Length]), c + z.side * (i - 1.5f) * 1.3f - z.front * 1.8f + Vector3.up * .35f, new Vector3(.7f, .4f, .6f), z.ctx.Rng, .1f);
                z.solid.Box(fabric, c + z.side * (i - 1.5f) * 1.3f - z.front * 1.8f + Vector3.up * .35f, new Vector3(1.3f, .75f, 1.1f), rot);
            }
            ArtistBits.CoverWall(z.pictures, z.labels, z.ctx, z.a, c - z.front * (D / 2f - .08f) + Vector3.up * 1.9f, z.front, 3, .75f, 3, true, Ink);
            FairyLights(z.mk, c + Vector3.up * (H - .15f) - z.front * D / 2f, c + Vector3.up * (H - .15f) + z.front * D / 2f, .3f, FairyWarm);
            z.area.glowLight = ArtistBits.PointLight(z.root, c + Vector3.up * 2.2f, new Color(1f, .75f, .6f), 9f, 1.6f);
            Plaque(z, 3.2f, 3.4f);
        }

        void Hammock(Area z)
        {
            var a = z.At(-1.5f, -3f); var b = z.At(-1.5f, 3f);
            foreach (var p in new[] { a, b }) BigTree(z, p - z.side * Mathf.Sign(Vector3.Dot(p - z.center, z.side)) * 0f, .8f, false);
            var cloth = WorldMaterials.Solid(Soft(z.a, 0));
            var pa = a + Vector3.up * 1.7f; var pb = b + Vector3.up * 1.7f;
            for (var i = 0; i < 10; i++)
            {
                float t0 = i / 10f, t1 = (i + 1) / 10f;
                Vector3 P(float t) => Vector3.Lerp(pa, pb, t) - Vector3.up * 1.1f * 4f * t * (1f - t);
                var p0 = P(t0); var p1 = P(t1);
                z.mk.Box(cloth, (p0 + p1) / 2f, new Vector3(1.1f, .06f, Vector3.Distance(p0, p1) + .02f), Quaternion.LookRotation(p1 - p0));
            }
            z.solid.Box(cloth, Vector3.Lerp(pa, pb, .5f) - Vector3.up * .8f, new Vector3(1.2f, .8f, 5.6f), Quaternion.LookRotation(pb - pa));
        }

        void Beanbag(Area z)
        {
            var c = z.At(-.5f, 1f);
            z.mk.Blob(WorldMaterials.Solid(Soft(z.a, 0)), c + Vector3.up * .45f, new Vector3(.9f, .55f, .9f), z.ctx.Rng, .2f);
            z.solid.Box(fabric, c + Vector3.up * .4f, new Vector3(1.6f, .8f, 1.6f), Quaternion.identity);
            var lamp = z.At(-1f, 2.4f);
            z.mk.Prism(iron, lamp, Vector3.up, .05f, 1.9f, 6);
            ArtistBits.PostCollider(z.root, lamp, 2.1f, .4f);
            z.mk.Prism(WorldMaterials.Glow(new Color(1f, .82f, .62f), 1.8f), lamp + Vector3.up * 1.7f, Vector3.up, .38f, .45f, 10, .65f);
            halos.Add(lamp + Vector3.up * 1.9f, 1.4f, FairyWarm, .8f);
        }

        void Pillows(Area z)
        {
            for (var i = 0; i < 6; i++)
            {
                var p = z.At(-.5f + (i % 3 - 1) * .4f, (i - 2.5f) * .7f) + Vector3.up * (.35f + (i / 3) * .5f);
                z.mk.Blob(WorldMaterials.Solid(i % 2 == 0 ? Soft(z.a, i) : Pastel[i % Pastel.Length]), p, new Vector3(.8f, .35f, .55f), z.ctx.Rng, .12f);
            }
            z.solid.Box(fabric, z.At(-.5f, 0) + Vector3.up * .6f, new Vector3(2.4f, 1.2f, 4.6f), Quaternion.LookRotation(z.front));
        }

        void Mobile(Area z)
        {
            var post = z.At(-.8f, 1.2f);
            z.mk.Prism(darkWood, post, Vector3.up, .07f, 3.4f, 6);
            ArtistBits.PostCollider(z.root, post, 3.4f, .1f);
            var top = post + Vector3.up * 3.3f;
            var paper = WorldMaterials.Glow(Color.Lerp(Soft(z.a, 0), Color.white, .4f), .9f);
            for (var k = 0; k < 6; k++)
            {
                var a = k * Mathf.PI / 3f;
                var arm = top + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * .9f;
                z.mk.Box(rope, (top + arm) / 2f, new Vector3(.02f, .02f, .9f), Quaternion.LookRotation(arm - top));
                var star = arm - Vector3.up * (.6f + (k % 3) * .3f);
                z.mk.Box(rope, (arm + star) / 2f, new Vector3(.01f, Vector3.Distance(arm, star), .01f), Quaternion.identity);
                z.mk.Box(paper, star, new Vector3(.3f, .3f, .04f), Quaternion.Euler(0, k * 30f, 45f));
                z.mk.Box(paper, star, new Vector3(.3f, .3f, .04f), Quaternion.Euler(0, k * 30f, 0f));
            }
        }

        void Plush(Area z)
        {
            var c = z.At(-.8f, 1.2f);
            var fur = WorldMaterials.Solid(Color.Lerp(Soft(z.a, 0), new Color(.85f, .72f, .62f), .5f));
            z.mk.Blob(fur, c + Vector3.up * .9f, new Vector3(1f, 1f, .85f), z.ctx.Rng, .08f);
            z.mk.Blob(fur, c + Vector3.up * 2.2f + z.front * .1f, new Vector3(.75f, .7f, .7f), z.ctx.Rng, .06f);
            foreach (var s in new[] { -1f, 1f })
            {
                z.mk.Blob(fur, c + Vector3.up * 2.8f + z.side * s * .5f, new Vector3(.25f, .25f, .15f), z.ctx.Rng, .05f);
                z.mk.Blob(fur, c + Vector3.up * .3f + z.side * s * .6f + z.front * .6f, new Vector3(.32f, .3f, .45f), z.ctx.Rng, .05f);
                z.mk.Blob(board, c + Vector3.up * 2.3f + z.side * s * .25f + z.front * .68f, Vector3.one * .07f, z.ctx.Rng, 0f);
            }
            z.solid.Box(fur, c + Vector3.up * 1.4f, new Vector3(2f, 2.8f, 1.8f), Quaternion.LookRotation(z.front));
        }
    }
}
