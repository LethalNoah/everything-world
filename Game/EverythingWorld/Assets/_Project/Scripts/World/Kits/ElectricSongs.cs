using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Electric Fields' deep pass (part of ElectricKit): festival installations for MGMT's and Santigold's
    /// songs out in the fields, each down its own straw walkway to a flag sign, big and bright enough to read from
    /// across a field, each glowing while its song plays. ("Kids", "Electric Feel", "Congratulations" and
    /// "Disparate Youth" already have their own events and pieces.)
    /// - MGMT: a pretend mansion front propped up from behind, with a red carpet and flashbulbs ("Time to
    ///   Pretend"); a giant smiling inflatable skull with neon eyes ("When You Die"); two giant hands shaking on a
    ///   plinth ("The Handshake"); a frozen pond ringed with icy glowing pines and a sled ("Siberian Breaks").
    /// - Santigold: a loop of running track with a lit finish arch ("Run the Road"); a graffiti wall glowing with
    ///   neon spray shapes and paint cans at its foot ("L.E.S. Artistes").
    public partial class ElectricKit
    {
        readonly List<Vector3> placeSpots = new List<Vector3>();

        Material SongGlow(string song, Color c, float s = 2f)
        {
            var m = new Material(GlowM(c, s)) { name = "Song glow" };
            life.songGlows.Add((song, m, c * (s * .65f)));
            return m;
        }

        void Lit(LandContext ctx, Vector3 at, Color col, float r = 3f)
        {
            ArtistBits.PointLight(ctx.Root, at, col, 14f, 2f);
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
            foreach (var a in ctx.Artists) if ((a.stationId != 0 || a.station) && new Vector2(q.x - a.x, q.z - a.z).magnitude < a.r + pad) return true;
            return false;
        }

        Vector3 PlaceSpot(LandContext ctx, Vector3 near, float nearR, float clear)
        {
            for (var tries = 0; tries < 1400; tries++)
            {
                var wide = tries > 800; var a = ctx.Range(0f, Mathf.PI * 2f);
                var r = wide ? ctx.Range(40f, 300f) : ctx.Range(nearR + clear + 4f, nearR + clear + 60f);
                var o = wide ? center : near; float px = o.x + Mathf.Cos(a) * r, pz = o.z + Mathf.Sin(a) * r;
                var q = new Vector3(px, 0, pz);
                if (!ctx.IsLand(px, pz, clear + 25f) || !ctx.IsFree(px, pz, clear) || !Open(ctx, px, pz, clear) || InRing(ctx, q, clear + 2f)) continue;
                if (!Gentle(ctx, px, pz, clear + 2f, tries > 1100 ? clear * .45f : clear * .3f)) continue;
                var ok = true; foreach (var s in placeSpots) if (Vector2.Distance(new Vector2(s.x, s.z), new Vector2(px, pz)) < clear + 20f) { ok = false; break; }
                if (!ok) continue;
                var p = ctx.OnGround(px, pz); placeSpots.Add(p); ctx.AddKeepout(p.x, p.z, clear + 3f); noGrass.Add(new Vector3(p.x, clear + 4f, p.z)); return p;
            }
            return Vector3.positiveInfinity;
        }

        /// A straw walkway from the nearest walkway, a flag sign, the tour's mark. Returns the way the place faces.
        Vector3 PlaceWay(LandContext ctx, MeshKit mk, Vector3 c, float clear, string sign, string mark, Color col)
        {
            Vector3 join = default; var best = float.MaxValue;
            foreach (var (p, _) in walkPoints) { var d = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(c.x, c.z)); if (d < best) { best = d; join = p; } }
            var face = best < float.MaxValue ? new Vector3(join.x - c.x, 0, join.z - c.z).normalized : new Vector3(center.x - c.x, 0, center.z - c.z).normalized;
            if (face.sqrMagnitude < .1f) face = Vector3.forward;
            var side = Vector3.Cross(Vector3.up, face);
            if (best < float.MaxValue && best > clear + 3f)
            {
                var pts = new List<Vector3>(); var from = new Vector3(join.x, 0, join.z); var dir = new Vector3(c.x - join.x, 0, c.z - join.z).normalized; var len = best - clear + 1f;
                for (var d = 1f; ; d += 2f) { var dd = Mathf.Min(d, len); var q = from + dir * dd; if (!ctx.IsLand(q.x, q.z, 0f)) break; pts.Add(ctx.OnGround(q.x, q.z)); if (dd >= len) break; }
                if (pts.Count > 1) { Strip(mk, pts, WorldMaterials.Solid(Trodden), PathW * .8f, .05f); foreach (var p in pts) Remember(p, 99); }
            }
            var sp = ctx.OnGround(c.x + face.x * (clear - .5f) + side.x * 2.6f, c.z + face.z * (clear - .5f) + side.z * 2.6f); var rot = Quaternion.LookRotation(face);
            mk.Prism(wood, sp - Vector3.up * .3f, Vector3.up, .08f, 3.8f, 6);
            mk.Box(dark, sp + Vector3.up * 2.6f, new Vector3(2.8f, .6f, .07f), rot);
            mk.Box(GlowM(col, 2.4f), sp + Vector3.up * 2.26f + face * .05f, new Vector3(2.6f, .05f, .02f), rot);
            ArtistBits.Label(ctx.Root, sign, sp + Vector3.up * 2.6f + face * .045f, face, .16f, Ink, TextAnchor.MiddleCenter);
            mk.Tri(WorldMaterials.Solid(col), sp + Vector3.up * 3.5f, sp + Vector3.up * 3f, sp + Vector3.up * 3.25f + side * .7f, sp + Vector3.up * 3.25f + face);
            ArtistBits.PostCollider(ctx.Root, sp, 2.4f, .15f);
            var m = new GameObject("Shot: " + mark).transform; m.SetParent(ctx.Root, false);
            var cam = c + face * (clear + 9f) - side * 4f;
            foreach (var (fo, so) in new[] { (clear + 9f, -4f), (clear + 9f, 4f), (clear + 6f, -5f), (-(clear + 8f), 3f), (clear + 3f, 0f) })
            { var q = c + face * fo + side * so; if (ctx.IsLand(q.x, q.z, 6f) && !InRing(ctx, q, 2f)) { cam = q; break; } }
            cam = ctx.OnGround(cam.x, cam.z) + Vector3.up * 1.4f;
            m.SetPositionAndRotation(cam, Quaternion.LookRotation((c + Vector3.up * 2.2f - cam).normalized));
            return face;
        }

        void SongPlaces(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            placeSpots.Clear(); foreach (var a in ctx.Artists) placeSpots.Add(ctx.OnGround(a.x, a.z));
            var placed = 0;
            void Place(string artist, float clear, string sign, string mark, Color col, System.Action<Vector3, Vector3> build)
            {
                var a = ctx.Artists.Find(x => x.name == artist);
                var near = a != null ? ctx.OnGround(a.x, a.z) : center; var nearR = a == null ? 0f : Mathf.Max(Foot(a), a.r);
                var c = PlaceSpot(ctx, near, nearR, clear); if (float.IsInfinity(c.x)) { Debug.Log("The Electric Fields: no room for " + mark); return; }
                build(c, PlaceWay(ctx, mk, c, clear, sign, mark, col)); placed++;
            }
            Place("MGMT", 8f, "TIME TO PRETEND", "the pretend mansion", Sunset, (c, f) => PretendMansion(ctx, mk, solid, c, f, "4iG2gAwKXsOcijVaVXzRPW"));
            Place("MGMT", 6f, "WHEN YOU DIE", "the inflatable skull", Lime, (c, f) => Skull(ctx, mk, solid, c, f, "3td69vL9Py7Ai9wfXYnvji"));
            Place("MGMT", 6f, "THE HANDSHAKE", "the giant handshake", Hot, (c, f) => Handshake(ctx, mk, solid, c, f, "7d4coyGabEbHZ5tRpTbQT5"));
            Place("MGMT", 8f, "SIBERIAN BREAKS", "the frozen pond", Electric, (c, f) => FrozenPond(ctx, mk, solid, c, f, "2wNK1PjIS9NbBIchakaL7k"));
            Place("Santigold", 9f, "RUN THE ROAD", "the running track", new Color(1f, .82f, .2f), (c, f) => RunningTrack(ctx, mk, solid, c, f, "43kUtI55YuzmKZ6BBQLd4F"));
            Place("Santigold", 6f, "L.E.S. ARTISTES", "the graffiti wall", Hot, (c, f) => GraffitiWall(ctx, mk, solid, c, f, "4dmCLAZbWYT0qtcG7GVMdb"));
            Debug.Log($"The Electric Fields: {placed} song places");
        }

        // ---------------------------------------------------------------- MGMT
        void PretendMansion(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            var white = WorldMaterials.Solid(new Color(.86f, .8f, .74f)); var gold = SongGlow(id, new Color(1f, .7f, .2f), 2.2f);
            var f = c - face * 2f;
            mk.Box(white, f + up * 3.5f, new Vector3(12f, 7f, .25f), rot); solid.Box(white, f + up * 3.5f, new Vector3(12f, 7f, .4f), rot);
            mk.Tri(white, f + up * 7f + side * 6f + face * .13f, f + up * 7f - side * 6f + face * .13f, f + up * 9.2f + face * .13f, f - face);
            for (var k = -2; k <= 2; k++) if (k != 0) mk.Prism(white, f + side * k * 2.2f + face * .5f, up, .3f, 6.6f, 10);
            for (var k = -2; k <= 2; k++) foreach (var y in new[] { 2.2f, 5f }) if (!(k == 0 && y < 3f)) mk.Box(SongGlow(id, new Color(1f, .7f, .3f), 1.2f), f + side * k * 2.2f + face * .14f + up * y, new Vector3(1.1f, 1.5f, .03f), rot);
            mk.Box(gold, f + face * .15f + up * 1.5f, new Vector3(2f, 3f, .03f), rot);
            // the struts holding it up from behind: it's only a front
            foreach (var s in new[] { -4.5f, 0f, 4.5f }) mk.Box(wood, f + side * s - face * 1.6f + up * 3f, new Vector3(.18f, .18f, 7f), Quaternion.LookRotation((-face * 3f - up * 6f).normalized));
            var carpet = WorldMaterials.Solid(new Color(.75f, .1f, .14f)); mk.Box(carpet, c + face * 2f + up * .03f, new Vector3(2.2f, .04f, 8f), rot);
            foreach (var s in new[] { -1f, 1f }) for (var k = 0; k < 4; k++) { var p = c + face * (k * 2f - 1f) + side * s * 1.6f; mk.Prism(metal, p, up, .04f, 1f, 6); mk.Blob(SongGlow(id, Color.white, 3f), p + up * 1.15f, Vector3.one * .12f, ctx.Rng, 0f); ArtistBits.PostCollider(ctx.Root, p, 1f, .08f); }
            ArtistBits.PointLight(ctx.Root, f + face * 7f + up * 3f, new Color(1f, .75f, .45f), 14f, 1.2f);
        }

        void Skull(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var side = Vector3.Cross(up, face); var vinyl = WorldMaterials.Solid(new Color(.96f, .92f, .98f));
            mk.Blob(vinyl, c + up * 4f, new Vector3(3.4f, 3.2f, 3.2f), ctx.Rng, .03f);
            mk.Blob(vinyl, c + up * 1.4f + face * .5f, new Vector3(2.2f, 1.3f, 2f), ctx.Rng, .03f);
            var eye = SongGlow(id, Lime, 2.8f);
            foreach (var s in new[] { -1f, 1f }) { mk.Blob(dark, c + up * 4.2f + face * 2.75f + side * s * 1.2f, new Vector3(.8f, .9f, .5f), ctx.Rng, 0f); mk.Blob(eye, c + up * 4.2f + face * 3.12f + side * s * 1.2f, new Vector3(.35f, .4f, .2f), ctx.Rng, 0f); }
            mk.Blob(dark, c + up * 3.1f + face * 3f, new Vector3(.35f, .45f, .3f), ctx.Rng, 0f);
            for (var k = -3; k <= 3; k++) mk.Box(SongGlow(id, Hot, 2f), c + up * 1.75f + face * (2.35f - Mathf.Abs(k) * .08f) + side * k * .38f, new Vector3(.3f, .5f, .1f), Quaternion.LookRotation(face));
            solid.Box(vinyl, c + up * 3f, new Vector3(5.6f, 6f, 5.4f), Quaternion.LookRotation(face));
            foreach (var s in new[] { -1f, 1f }) mk.Box(rope, c + side * s * 3f + face * 1f + up * 1.2f, new Vector3(.03f, 2.6f, .03f), Quaternion.LookRotation(face) * Quaternion.Euler(0, 0, s * -30f));
            Lit(ctx, c + up * 4.5f + face * 4.5f, Lime, 3.6f);
        }

        void Handshake(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var side = Vector3.Cross(up, face); var skinA = WorldMaterials.Solid(new Color(.95f, .75f, .62f)); var skinB = WorldMaterials.Solid(new Color(.55f, .38f, .28f));
            mk.Box(dark, c + up * .6f, new Vector3(5f, 1.2f, 2.2f), Quaternion.LookRotation(face)); solid.Box(dark, c + up * .6f, new Vector3(5f, 1.2f, 2.2f), Quaternion.LookRotation(face));
            var clasp = c + up * 3.2f;
            foreach (var (s, skin, sleeve) in new[] { (-1f, skinA, new Color(.3f, .75f, 1f)), (1f, skinB, new Color(1f, .3f, .55f)) })
            {
                var elbow = c + up * 1.2f + side * s * 2.2f;
                mk.Prism(WorldMaterials.Solid(sleeve), elbow, (clasp - elbow).normalized, .5f, Vector3.Distance(elbow, clasp) * .6f, 10);
                mk.Prism(skin, elbow + (clasp - elbow) * .6f, (clasp - elbow).normalized, .38f, Vector3.Distance(elbow, clasp) * .35f, 10);
                mk.Blob(skin, clasp + side * s * .25f, new Vector3(.7f, .55f, .5f), ctx.Rng, .1f);
                mk.Blob(skin, clasp + side * s * .1f + up * .45f + face * .1f, new Vector3(.18f, .35f, .16f), ctx.Rng, 0f);
            }
            var spark = SongGlow(id, Hot, 2.6f);
            for (var k = 0; k < 10; k++) { var a = k * Mathf.PI / 5f; mk.Blob(spark, clasp + (up * Mathf.Sin(a) + face * Mathf.Cos(a)) * 1.3f, Vector3.one * .13f, ctx.Rng, 0f); }
            ArtistBits.PostCollider(ctx.Root, c, 4f, 1.2f);
            Lit(ctx, clasp + face * 2.5f + up * .5f, Hot, 3.6f);
        }

        void FrozenPond(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var snow = WorldMaterials.Solid(new Color(.93f, .95f, 1f));
            mk.Prism(snow, c + up * .02f, up, 7.5f, .06f, 20);
            mk.Prism(SongGlow(id, new Color(.55f, .85f, 1f), 1f), c + up * .09f, up, 4.2f, .03f, 18);
            var pine = SongGlow(id, new Color(.15f, .45f, 1f), .9f);   // a deep blue that stays blue under the bloom
            for (var k = 0; k < 9; k++)
            {
                var a = k * Mathf.PI * 2f / 9f + .3f; var p = ctx.OnGround(c.x + Mathf.Cos(a) * 6f, c.z + Mathf.Sin(a) * 6f); var h = 3f + (k % 3) * .9f;
                mk.Prism(WorldMaterials.Solid(new Color(.25f, .2f, .2f)), p, up, .14f, .7f, 6);
                for (var l = 0; l < 3; l++) mk.Cone(l == 0 ? snow : pine, p + up * (.6f + l * h * .28f), (1.1f - l * .28f) * (h / 3.5f), h * .45f, 7, k * 20f);
                ArtistBits.PostCollider(ctx.Root, p, h, .5f);
            }
            var sled = c + Vector3.Cross(up, face) * 2f + face * 1f; var sr = Quaternion.LookRotation(Vector3.Cross(up, face));
            mk.Box(WorldMaterials.Solid(new Color(.75f, .15f, .15f)), sled + up * .3f, new Vector3(.6f, .06f, 1.3f), sr);
            foreach (var s in new[] { -1f, 1f }) mk.Box(metal, sled + up * .14f + face * s * .28f, new Vector3(.05f, .05f, 1.5f), sr);
            Lit(ctx, c + up * 3f, new Color(.3f, .6f, 1f), 4f);
        }

        // ---------------------------------------------------------------- Santigold
        void RunningTrack(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var side = Vector3.Cross(up, face);
            var track = WorldMaterials.Solid(new Color(.72f, .28f, .2f)); var lane = WorldMaterials.Solid(new Color(.95f, .95f, .92f));
            const int n = 32; Vector3 P(int k, float r) { var a = k * Mathf.PI * 2f / n; var q = c + side * Mathf.Cos(a) * r * 1.4f + face * Mathf.Sin(a) * r; q.y = ctx.Ground(q.x, q.z) + .06f; return q; }
            for (var k = 0; k < n; k++)
            {
                Vector3 a0 = P(k, 6.4f), a1 = P(k + 1, 6.4f), b0 = P(k, 4.2f), b1 = P(k + 1, 4.2f);
                mk.Quad(track, a0, a1, b1, b0, (a0 + b1) / 2f - up);
                foreach (var r in new[] { 4.95f, 5.65f }) { var l0 = P(k, r); var l1 = P(k + 1, r); mk.Box(lane, (l0 + l1) / 2f + up * .01f, new Vector3(.05f, .01f, Vector3.Distance(l0, l1)), Quaternion.LookRotation(l1 - l0)); }
            }
            var fin = c + side * 7.4f; var arch = SongGlow(id, new Color(1f, .82f, .2f), 2.4f);
            foreach (var s in new[] { -1f, 1f }) { var p = ctx.OnGround((fin + face * s * 1.6f).x, (fin + face * s * 1.6f).z); mk.Prism(dark, p, up, .14f, 3.4f, 8); ArtistBits.PostCollider(ctx.Root, p, 3.4f, .18f); }
            mk.Box(dark, fin + up * 3.4f, new Vector3(.4f, .6f, 3.6f), Quaternion.LookRotation(face)); mk.Box(arch, fin + up * 3.4f + side * .21f, new Vector3(.02f, .4f, 3.3f), Quaternion.LookRotation(face));
            ArtistBits.Label(ctx.Root, "RUN THE ROAD", fin + up * 3.4f + side * .23f, side, .26f, new Color(.15f, .1f, .05f), TextAnchor.MiddleCenter);
            mk.Box(arch, P(n / 4, 5.3f) + up * .02f, new Vector3(.4f, .02f, 2.2f), Quaternion.LookRotation(side));
            Lit(ctx, fin + up * 2.6f + side * 1.5f, new Color(1f, .82f, .2f), 3.4f);
        }

        void GraffitiWall(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            var brickM = WorldMaterials.Solid(new Color(.45f, .3f, .28f));
            mk.Box(brickM, c - face * .5f + up * 2f, new Vector3(9f, 4f, .5f), rot); solid.Box(brickM, c - face * .5f + up * 2f, new Vector3(9f, 4f, .5f), rot);
            var cols = new[] { Hot, Electric, Lime, new Color(1f, .82f, .2f), new Color(.75f, .45f, 1f) };
            for (var k = 0; k < 9; k++)
            {
                var col = cols[k % cols.Length]; var p = c - face * .23f + side * ((k - 4f) * .95f) + up * (1.3f + (k * 37 % 5) * .45f);
                var r = Quaternion.LookRotation(face) * Quaternion.Euler(0, 0, k * 41f);
                mk.Box(SongGlow(id, col, 2.2f), p, new Vector3(1.1f - (k % 3) * .2f, .14f, .03f), r);
                if (k % 2 == 0) mk.Box(SongGlow(id, col, 2.2f), p + up * .25f + side * .2f, new Vector3(.14f, .7f, .03f), r);
            }
            ArtistBits.Label(ctx.Root, "L.E.S.", c - face * .21f + up * 3.3f, face, .5f, Hot, TextAnchor.MiddleCenter);
            for (var k = 0; k < 5; k++) { var p = ctx.OnGround((c + side * (k - 2f) * .5f + face * .3f).x, (c + side * (k - 2f) * .5f + face * .3f).z); mk.Prism(WorldMaterials.Solid(cols[k]), p, up, .09f, .28f, 8); }
            var lamp = c + side * 5f; mk.Prism(metal, lamp, up, .07f, 4f, 6); mk.Box(metal, lamp + up * 4f - side * .5f, new Vector3(1f, .08f, .08f), rot); mk.Box(SongGlow(id, new Color(1f, .85f, .55f), 2.6f), lamp + up * 3.9f - side * .9f, new Vector3(.4f, .1f, .25f), rot);
            ArtistBits.PostCollider(ctx.Root, lamp, 4f, .1f);
            Lit(ctx, c + face * 2.5f + up * 2.5f, Hot, 4f);
        }
    }
}
