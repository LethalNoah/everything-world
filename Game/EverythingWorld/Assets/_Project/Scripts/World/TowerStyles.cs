using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// Each realm's fast-travel tower, built for its music (the Everything Mast keeps its plain shape, since the hub's
    /// departures board hangs on it). Every tower keeps its footprint within about 6 m of its centre and its door on
    /// the south side, where travellers arrive. Built in the tower's own space; TowerLife moves the parts.
    /// - The Old Kingdoms: a stack of giant records on a spindle, each turning, a tonearm swinging over the top.
    /// - Riffland: amp cabinets stacked smaller and smaller, speakers pumping, a giant guitar neck, lightning.
    /// - The Indie Isles: a pastel lighthouse wrapped in fairy lights, a cassette for a lantern room, a warm beam.
    /// - The Bass Reaches: a dark tower of subwoofers pumping with the bass, neon rings, a laser into the sky.
    /// - The Chart Isles: a stepped gold trophy tower with gold records and marquee lights, a glitter ball on top.
    /// - The Groovelands: stacked bandstands with string lights, a brass saxophone winding up to its bell.
    /// - The Boroughs: a brick tower block with graffiti and a fire escape, a water tower and a boombox on the roof.
    /// - The Isles of Story: a theatre with red curtains and a gold arch, a spire wrapped in a turning filmstrip,
    ///   a clapperboard, a conductor's baton at the top.
    public static class TowerStyles
    {
        class B
        {
            public Transform t; public MeshKit mk = new MeshKit(), solid = new MeshKit(); public TowerLife life; public System.Random rng = new System.Random(7);
            public Vector3 W(Vector3 local) => t.TransformPoint(local);
            public Light Light(Vector3 local, Color c, float range, float intensity) => ArtistBits.PointLight(t, W(local), c, range, intensity);
            public Transform Part(MeshKit m, string name, Vector3 local, Quaternion rot)
            {
                var go = m.BuildSingle(name, t, false); if (go == null) return null; go.isStatic = false; go.transform.localPosition = local; go.transform.localRotation = rot; return go.transform;
            }
            public Material Pulse(Color c, float s, float phase = 0f) { var m = new Material(WorldMaterials.Glow(c, s)) { name = "Tower pulse" }; life.pulse.Add((m, m.GetColor("_EmissionColor"), phase)); return m; }
            public void Post(Vector3 local, float h, float r) => ArtistBits.PostCollider(t, W(local), h, r);
            public void Sign(string text, Vector3 local, float h, Color c) => ArtistBits.Label(t, text, W(local), Vector3.back, h, c, TextAnchor.MiddleCenter);
            /// A speaker built facing +y (surround, cone, glowing dust cap), mounted facing 'dir' and pumping on the beat.
            public void Speaker(Vector3 local, Vector3 dir, float r, Material cap, float amount = .18f)
            {
                var m = new MeshKit(); m.Prism(G.Rubber, Vector3.zero, Vector3.up, r, .22f, 24); m.Cone(G.ConeM, Vector3.up * .22f, r * .84f, r * .3f, 24); m.Blob(cap, Vector3.up * (.22f + r * .3f), Vector3.one * r * .2f, rng, 0f);
                var p = Part(m, "Speaker", local, Quaternion.FromToRotation(Vector3.up, dir)); if (p != null) life.pump.Add((p, local, dir, amount));
            }
        }

        static class G
        {
            public static readonly Material Rubber = WorldMaterials.Solid(new Color(.1f, .1f, .11f)), ConeM = WorldMaterials.Solid(new Color(.16f, .16f, .18f), .3f), Dark = WorldMaterials.Solid(new Color(.05f, .05f, .06f));
        }

        public static bool Build(Transform tower, IslandWorldData.Tower data)
        {
            var b = new B { t = tower }; b.life = tower.gameObject.AddComponent<TowerLife>();
            switch (data.realm)
            {
                case "The Old Kingdoms": Records(b); break;
                case "Riffland": Amps(b); break;
                case "The Indie Isles": Lighthouse(b); break;
                case "The Bass Reaches": Subs(b); break;
                case "The Chart Isles": Trophy(b); break;
                case "The Groovelands": Bandstand(b); break;
                case "The Boroughs": Block(b); break;
                case "The Isles of Story": Theatre(b); break;
                default: Object.Destroy(b.life); return false;
            }
            foreach (var (name, from, at) in new[] { ("the tower", new Vector3(12f, 2f, -44f), new Vector3(0, 20f, 0)), ("the tower door", new Vector3(3f, 1.8f, -13f), new Vector3(0, 3f, 0)) })
            {
                var m = new GameObject("Shot: " + name).transform; m.SetParent(tower, false); m.position = b.W(from); m.rotation = Quaternion.LookRotation(b.W(at) - b.W(from));
            }
            b.mk.Build("Tower", tower, false);
            b.solid.BuildCollider("Tower walls", tower);
            return true;
        }

        static Material S(Color c, float sm = .08f) => WorldMaterials.Solid(c, sm);
        static Material Gl(Color c, float s) => WorldMaterials.Glow(c, s);
        static readonly Vector3 up = Vector3.up;

        // ---------------------------------------------------------------- the Old Kingdoms: a stack of records
        static void Records(B b)
        {
            b.life.bpm = 100f; var mk = b.mk;
            var stone = S(new Color(.62f, .56f, .48f)); var black = S(new Color(.09f, .09f, .1f), .9f); var silver = S(new Color(.8f, .8f, .82f), .7f); var warm = new Color(1f, .75f, .42f);
            Color[] labels = { new Color(.85f, .2f, .15f), new Color(.95f, .88f, .7f), new Color(.2f, .35f, .8f), new Color(1f, .55f, .15f), new Color(.2f, .6f, .3f), new Color(.55f, .3f, .75f) };
            Color[] sleeves = { new Color(.8f, .62f, .24f), new Color(.24f, .48f, .5f), new Color(.6f, .26f, .18f), new Color(.86f, .8f, .66f), new Color(.42f, .46f, .24f) };
            mk.Prism(stone, Vector3.zero, up, 5.4f, 4f, 32); b.solid.Prism(stone, Vector3.zero, up, 5.4f, 4f, 32);
            mk.Box(G.Dark, new Vector3(0, 1.5f, -5.3f), new Vector3(2.2f, 3f, .3f)); mk.Box(Gl(warm, 1.6f), new Vector3(0, 3.3f, -5.42f), new Vector3(.5f, .3f, .1f)); b.Light(new Vector3(0, 3.4f, -6.4f), warm, 9f, 1.2f);
            b.Sign("THE OLD KINGDOMS", new Vector3(0, 3.75f, -5.47f), .32f, new Color(.2f, .14f, .1f));
            var y = 4f;
            for (var k = 0; k < 14; k++)
            {
                var r = 5f - k * .16f;
                mk.Box(S(sleeves[k % sleeves.Length]), new Vector3(0, y + .12f, 0), new Vector3(r * 1.25f, .24f, r * 1.25f), Quaternion.Euler(0, k * 23f, 0)); y += .24f;
                var rm = new MeshKit(); rm.Prism(black, Vector3.zero, up, r, .3f, 40); for (var g = 1; g <= 3; g++) rm.Prism(Gl(new Color(.5f, .5f, .58f), .25f), up * (.3f + g * .001f), up, r * (1f - g * .14f), .004f, 40, 1f, 0f, true);
                rm.Prism(Gl(labels[k % labels.Length], .8f), up * .306f, up, r * .3f, .02f, 24);
                rm.Prism(Gl(new Color(1f, .75f, .42f), .9f), Vector3.zero, up, r + .05f, .06f, 40, 1f, 0f, false);
                var p = b.Part(rm, "Record", new Vector3(0, y, 0), Quaternion.identity); if (p != null) b.life.spin.Add((p, up, (k % 2 == 0 ? 1f : -1f) * (8f + k)));
                y += .3f + 1.1f;
            }
            b.Post(new Vector3(0, 4f, 0), y - 4f, 5f);
            mk.Prism(silver, new Vector3(0, 4f, 0), up, .45f, y - 4f + 4f, 16); mk.Blob(Gl(warm, 2.4f), new Vector3(0, y + 4.3f, 0), Vector3.one * .8f, b.rng, 0f);
            // the tonearm, from a post at the side, swinging over the top record
            var px = new Vector3(4.6f, 0, 2.4f); mk.Prism(silver, new Vector3(px.x, 4f, px.z), up, .28f, y - 3.2f, 12); b.Post(new Vector3(px.x, 0, px.z), 6f, .35f); mk.Prism(silver, new Vector3(px.x, y + .8f, px.z), up, .55f, .3f, 16);
            var am = new MeshKit(); var toward = (new Vector3(0, 0, 0) - px).normalized; am.Box(silver, toward * 2.2f, new Vector3(.18f, .18f, 4.4f), Quaternion.LookRotation(toward)); am.Box(S(new Color(.2f, .2f, .22f)), toward * 4.5f - up * .15f, new Vector3(.5f, .2f, .7f), Quaternion.LookRotation(toward)); am.Blob(Gl(new Color(1f, .3f, .2f), 2f), toward * 4.5f - up * .3f, Vector3.one * .12f, b.rng, 0f);
            am.Blob(silver, -toward * .6f, Vector3.one * .4f, b.rng, 0f);
            var arm = b.Part(am, "Tonearm", new Vector3(px.x, y + 1.2f, px.z), Quaternion.identity); if (arm != null) b.life.swing.Add((arm, Quaternion.identity, up, 9f, .25f));
            b.Light(new Vector3(0, y + 4f, 0), warm, 45f, 2.2f);
        }

        // ---------------------------------------------------------------- Riffland: a stack of amps and a guitar neck
        static void Amps(B b)
        {
            b.life.bpm = 140f; var mk = b.mk;
            var tolex = S(new Color(.06f, .06f, .07f), .3f); var grille = S(new Color(.24f, .22f, .2f)); var piping = S(new Color(.85f, .68f, .3f), .5f); var chrome = S(new Color(.8f, .8f, .82f), .8f);
            var hot = new Color(1f, .45f, .15f); var cap = b.Pulse(hot, 1.6f);
            var cabs = new[] { new Vector3(9f, 5f, 7f), new Vector3(8f, 4.6f, 6.4f), new Vector3(7f, 4.2f, 5.6f), new Vector3(6f, 3.8f, 4.8f), new Vector3(5f, 3.4f, 4f) };
            var y = 0f;
            for (var k = 0; k < cabs.Length; k++)
            {
                var s = cabs[k]; var c = new Vector3(0, y + s.y / 2f, 0);
                mk.Box(tolex, c, s); b.solid.Box(tolex, c, s);
                foreach (var f in new[] { -1f, 1f })
                {
                    var fz = f * (s.z / 2f + .02f);
                    mk.Box(grille, new Vector3(0, c.y, fz), new Vector3(s.x - .5f, s.y - .5f, .04f));
                    foreach (var e in new[] { -1f, 1f }) mk.Box(piping, new Vector3(0, c.y + e * (s.y / 2f - .16f), f * (s.z / 2f + .05f)), new Vector3(s.x - .3f, .07f, .05f));
                    var rs = Mathf.Min(s.y * .2f, 1.1f);
                    for (var i = 0; i < 2; i++) for (var j = 0; j < 2; j++)
                    {
                        var sx = (i - .5f) * s.x * .48f; var sy = c.y + (j - .5f) * s.y * .46f;
                        if (k == 0 && f < 0 && Mathf.Abs(sx) < 2f) continue;   // the door
                        b.Speaker(new Vector3(sx, sy, f * (s.z / 2f + .05f)), Vector3.forward * f, rs, cap);
                    }
                    mk.Box(Gl(new Color(.95f, .9f, .8f), .6f), new Vector3(s.x * .32f, c.y + s.y / 2f - .45f, f * (s.z / 2f + .06f)), new Vector3(1.2f, .3f, .04f));
                }
                y += s.y;
            }
            mk.Box(G.Dark, new Vector3(0, 1.6f, -3.56f), new Vector3(2.4f, 3.2f, .1f)); mk.Box(Gl(hot, 1.4f), new Vector3(0, 3.35f, -3.58f), new Vector3(2.6f, .08f, .06f));
            b.Sign("RIFFLAND", new Vector3(0, 3.75f, -3.6f), .36f, new Color(.95f, .85f, .6f)); b.Light(new Vector3(0, 3.4f, -5f), hot, 9f, 1.3f);
            // the amp heads: control panels with knobs and a red jewel
            foreach (var (hs, hy) in new[] { (new Vector3(4.4f, 1.6f, 3f), y), (new Vector3(3.6f, 1.4f, 2.6f), y + 1.6f) })
            {
                mk.Box(tolex, new Vector3(0, hy + hs.y / 2f, 0), hs); b.solid.Box(tolex, new Vector3(0, hy + hs.y / 2f, 0), hs);
                foreach (var f in new[] { -1f, 1f })
                {
                    mk.Box(piping, new Vector3(0, hy + hs.y / 2f, f * (hs.z / 2f + .02f)), new Vector3(hs.x - .3f, hs.y - .4f, .04f));
                    for (var i = 0; i < 6; i++) mk.Prism(G.Dark, new Vector3(-hs.x / 2f + .7f + i * .5f, hy + hs.y / 2f, f * (hs.z / 2f + .04f)), Vector3.forward * f, .14f, .14f, 10);
                    mk.Blob(b.Pulse(new Color(1f, .15f, .1f), 2.2f, .5f), new Vector3(hs.x / 2f - .5f, hy + hs.y / 2f, f * (hs.z / 2f + .08f)), Vector3.one * .13f, b.rng, 0f);
                }
            }
            y += 3f;
            // the guitar neck, frets glowing, and the headstock with its tuning pegs
            var wood = S(new Color(.5f, .32f, .18f), .3f); var board = S(new Color(.14f, .09f, .06f));
            mk.Box(wood, new Vector3(0, y + 7f, 0), new Vector3(1.3f, 14f, .6f)); mk.Box(board, new Vector3(0, y + 7f, -.33f), new Vector3(1.2f, 14f, .08f));
            for (var i = 0; i < 13; i++) { mk.Box(Gl(new Color(.9f, .9f, .95f), .7f), new Vector3(0, y + .6f + i * 1.05f, -.38f), new Vector3(1.2f, .05f, .03f)); if (i % 2 == 1) mk.Blob(Gl(hot, 1.4f), new Vector3(0, y + 1.1f + i * 1.05f, -.39f), new Vector3(.12f, .12f, .02f), b.rng, 0f); }
            var hy2 = y + 15.4f; mk.Box(wood, new Vector3(0, hy2, 0), new Vector3(2f, 3f, .5f), Quaternion.Euler(-12f, 0, 0));
            for (var i = 0; i < 6; i++) { var sd = i < 3 ? -1f : 1f; mk.Prism(chrome, new Vector3(sd * 1f, hy2 - 1f + (i % 3) * .9f, 0), Vector3.right * sd, .1f, .5f, 8); mk.Blob(chrome, new Vector3(sd * 1.55f, hy2 - 1f + (i % 3) * .9f, 0), new Vector3(.12f, .2f, .06f), b.rng, 0f); }
            // lightning crackling round the top
            for (var k = 0; k < 4; k++)
            {
                var lm = new MeshKit(); var p = Vector3.zero; var d = Quaternion.Euler(0, k * 90f + 30f, 0) * Vector3.forward; var bolt = Gl(new Color(.8f, .85f, 1f), 3f);
                for (var s = 0; s < 6; s++) { var q = p + d * 1.4f + up * (.8f + (float)b.rng.NextDouble() * .8f) + Vector3.Cross(up, d) * ((float)b.rng.NextDouble() - .5f) * 1.6f; lm.Box(bolt, (p + q) / 2f, new Vector3(.12f, .12f, (q - p).magnitude), Quaternion.LookRotation(q - p)); p = q; }
                var part = b.Part(lm, "Lightning", new Vector3(0, hy2 + 1.4f, 0), Quaternion.identity); if (part != null) b.life.flicker.Add(part.gameObject);
            }
            b.Light(new Vector3(0, hy2 + 3f, 0), new Color(.8f, .85f, 1f), 40f, 2f); b.Light(new Vector3(0, 10f, -7f), hot, 18f, 1.4f);
        }

        // ---------------------------------------------------------------- the Indie Isles: the lo-fi lighthouse
        static void Lighthouse(B b)
        {
            b.life.bpm = 84f; var mk = b.mk;
            Color[] bands = { new Color(.96f, .92f, .84f), new Color(.95f, .66f, .72f), new Color(.96f, .92f, .84f), new Color(.6f, .84f, .76f), new Color(.96f, .92f, .84f), new Color(.62f, .74f, .95f) };
            const float H = 26f; float R(float y) => Mathf.Lerp(4.2f, 2.8f, y / H);
            for (var k = 0; k < bands.Length; k++) { var y0 = k * H / bands.Length; var h = H / bands.Length; mk.Prism(S(bands[k]), new Vector3(0, y0, 0), up, R(y0), h, 28, R(y0 + h) / R(y0)); }
            b.Post(Vector3.zero, H, 4f);
            var white = S(new Color(.95f, .95f, .93f)); var warm = new Color(1f, .78f, .48f);
            mk.Prism(white, new Vector3(0, H, 0), up, 3.8f, .3f, 28); for (var k = 0; k < 20; k++) { var a = k * Mathf.PI * 2f / 20f; var p = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 3.6f + up * (H + .3f); mk.Box(white, p + up * .5f, new Vector3(.08f, 1f, .08f)); mk.Box(white, p + up * 1f, new Vector3(1.15f, .07f, .07f), Quaternion.Euler(0, -a * Mathf.Rad2Deg + 90f, 0)); }
            // the cassette lantern room: reels turning in its window
            var shell = S(new Color(.93f, .88f, .78f), .3f); var cy = H + .3f + 1.9f;
            mk.Box(shell, new Vector3(0, cy, 0), new Vector3(6f, 3.8f, 1.4f)); mk.Box(S(new Color(.8f, .76f, .68f)), new Vector3(0, cy - 1.6f, 0), new Vector3(4.4f, .7f, 1.5f));
            foreach (var f in new[] { -1f, 1f })
            {
                mk.Box(b.Pulse(new Color(.98f, .6f, .66f), .6f), new Vector3(0, cy + 1.05f, f * .72f), new Vector3(5.2f, 1f, .03f));
                mk.Box(G.Dark, new Vector3(0, cy - .05f, f * .72f), new Vector3(3.8f, 1.4f, .03f));
                foreach (var e in new[] { -1.25f, 1.25f })
                {
                    var rm = new MeshKit(); rm.Prism(S(new Color(.35f, .22f, .14f)), Vector3.back * .02f, Vector3.forward, .62f, .04f, 24); rm.Prism(white, Vector3.back * .04f, Vector3.forward, .34f, .08f, 16); for (var i = 0; i < 6; i++) rm.Box(G.Dark, Quaternion.Euler(0, 0, i * 60f) * Vector3.up * .22f + Vector3.back * .09f, new Vector3(.08f, .14f, .02f), Quaternion.Euler(0, 0, i * 60f));
                    var reel = b.Part(rm, "Reel", new Vector3(e, cy - .05f, f * .76f), f < 0 ? Quaternion.identity : Quaternion.Euler(0, 180f, 0)); if (reel != null) b.life.spin.Add((reel, Vector3.forward, 50f));
                }
            }
            b.solid.Box(shell, new Vector3(0, cy, 0), new Vector3(6f, 3.8f, 1.4f));
            // the lamp on top and its slow warm beam
            var top = cy + 1.9f; mk.Prism(S(new Color(.75f, .58f, .3f), .6f), new Vector3(0, top, 0), up, .7f, .5f, 16); mk.Blob(Gl(warm, 2.6f), new Vector3(0, top + .9f, 0), Vector3.one * .6f, b.rng, 0f); mk.Cone(S(new Color(.75f, .58f, .3f), .6f), new Vector3(0, top + 1.4f, 0), .8f, .8f, 16);
            var bm = new MeshKit(); foreach (var d in new[] { Vector3.forward, Vector3.back }) bm.Prism(Gl(warm, .35f), Vector3.zero, d, .25f, 70f, 10, 10f, 0f, false);
            var beam = b.Part(bm, "Beam", new Vector3(0, top + .9f, 0), Quaternion.identity); if (beam != null) { b.life.spin.Add((beam, up, 14f)); foreach (var r in beam.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; }
            // fairy lights spiralling down the tower
            var fairy = new[] { b.Pulse(new Color(1f, .85f, .55f), 2f), b.Pulse(new Color(1f, .6f, .75f), 2f, .5f), b.Pulse(new Color(.6f, .9f, .85f), 2f, .25f) };
            for (var i = 0; i < 150; i++) { var a = i * .33f; var yy = .8f + i * .165f; if (yy > H - .3f) break; var r = R(yy) + .07f; mk.Blob(fairy[i % 3], new Vector3(Mathf.Cos(a) * r, yy, Mathf.Sin(a) * r), Vector3.one * .09f, b.rng, 0f); }
            // the door, a bike and potted plants
            var wood = S(new Color(.5f, .34f, .22f)); mk.Box(wood, new Vector3(0, 1.5f, -4.13f), new Vector3(1.6f, 3f, .3f), Quaternion.identity); mk.Prism(Gl(warm, 1.4f), new Vector3(0, 3.6f, -4.06f), Vector3.back, .4f, .12f, 16);
            b.Sign("THE INDIE ISLES", new Vector3(0, 4.5f, -4.05f), .3f, new Color(.25f, .2f, .2f)); b.Light(new Vector3(0, 3.6f, -5.4f), warm, 9f, 1.2f);
            var bike = new Vector3(-2.6f, 0, -4.6f); var frame = S(new Color(.3f, .6f, .7f), .4f);
            foreach (var e in new[] { -.55f, .55f }) { mk.Prism(G.Dark, bike + new Vector3(e, .36f, -.03f), Vector3.forward, .36f, .06f, 16); }
            mk.Box(frame, bike + new Vector3(0, .62f, 0), new Vector3(1f, .06f, .06f), Quaternion.Euler(0, 0, -8f)); mk.Box(frame, bike + new Vector3(.25f, .55f, 0), new Vector3(.06f, .5f, .06f), Quaternion.Euler(0, 0, 18f)); mk.Box(G.Dark, bike + new Vector3(-.4f, .95f, 0), new Vector3(.3f, .06f, .14f)); b.solid.Box(frame, bike + new Vector3(0, .5f, 0), new Vector3(1.6f, 1f, .3f));
            for (var i = 0; i < 3; i++) { var pp = new Vector3(1.6f + i * .8f, 0, -4.4f + i * .3f); mk.Prism(S(new Color(.72f, .38f, .24f)), pp, up, .3f, .5f, 10, 1.25f); mk.Blob(S(new Color(.3f, .55f, .3f)), pp + up * .85f, new Vector3(.45f, .5f, .45f), b.rng, .2f); b.Post(pp, .9f, .35f); }
            b.Light(new Vector3(0, top + 1f, 0), warm, 40f, 2f);
        }

        // ---------------------------------------------------------------- the Bass Reaches: the subwoofer spire
        static void Subs(B b)
        {
            b.life.bpm = 140f; var mk = b.mk;
            const float H = 30f; float R(float y) => Mathf.Lerp(4.4f, 3f, y / H);
            var body = S(new Color(.07f, .07f, .09f), .5f); mk.Prism(body, Vector3.zero, up, 4.4f, H, 32, 3f / 4.4f); b.Post(Vector3.zero, H, 4.2f);
            var cyan = new Color(.2f, .85f, 1f); var magenta = new Color(1f, .25f, .85f);
            var capC = b.Pulse(cyan, 2.2f); var capM = b.Pulse(magenta, 2.2f, .5f);
            for (var k = 0; k < 6; k++)
            {
                var y = 2.6f + k * 4.6f; var rr = R(y); var rs = Mathf.Lerp(1.6f, 1.15f, k / 5f);
                for (var j = 0; j < 3; j++)
                {
                    var d = Quaternion.Euler(0, k * 60f + j * 120f, 0) * Vector3.forward;
                    if (k == 0 && Vector3.Angle(d, Vector3.back) < 50f) continue;
                    b.Speaker(d * (rr - .05f) + up * y, d, rs, k % 2 == 0 ? capC : capM, .28f);
                }
                var ring = b.Pulse(k % 2 == 0 ? cyan : magenta, 1.6f, k * .125f); var ry = y + 2.3f; var rr2 = R(ry) + .1f;
                for (var s = 0; s < 32; s++) { var a = (s + .5f) * Mathf.PI * 2f / 32f; mk.Box(ring, new Vector3(Mathf.Cos(a) * rr2, ry, Mathf.Sin(a) * rr2), new Vector3(.12f, .14f, rr2 * Mathf.PI * 2f / 32f + .02f), Quaternion.Euler(0, -a * Mathf.Rad2Deg, 0)); }
            }
            mk.Box(G.Dark, new Vector3(0, 1.6f, -4.35f), new Vector3(2.2f, 3.2f, .3f)); foreach (var e in new[] { -1.15f, 1.15f }) mk.Box(capC, new Vector3(e, 1.6f, -4.42f), new Vector3(.08f, 3.3f, .06f)); mk.Box(capC, new Vector3(0, 3.27f, -4.42f), new Vector3(2.38f, .08f, .06f));
            b.Sign("THE BASS REACHES", new Vector3(0, 3.8f, -4.45f), .3f, cyan); b.Light(new Vector3(0, 3f, -6f), cyan, 10f, 1.3f);
            mk.Prism(body, new Vector3(0, H, 0), up, 3f, .6f, 32, .6f); mk.Prism(capM, new Vector3(0, H + .6f, 0), up, 1f, 1.2f, 16, .4f);
            var lm = new MeshKit(); lm.Prism(b.Pulse(cyan, 1.4f), Vector3.zero, up, .22f, 400f, 8, 1f, 0f, false); var laser = b.Part(lm, "Laser", new Vector3(0, H + 1.8f, 0), Quaternion.identity);
            if (laser != null) foreach (var r in laser.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            b.Light(new Vector3(0, H + 2f, 0), magenta, 40f, 2f); b.Light(new Vector3(0, 12f, 0), cyan, 24f, 1.4f);
        }

        // ---------------------------------------------------------------- the Chart Isles: the trophy tower
        static void Trophy(B b)
        {
            b.life.bpm = 118f; var mk = b.mk;
            var gold = S(new Color(1f, .9f, .6f), .6f); var band = S(new Color(.12f, .08f, .1f)); var red = Gl(new Color(1f, .25f, .3f), 1f);
            var bulbA = b.Pulse(new Color(1f, .9f, .6f), 2.2f); var bulbB = b.Pulse(new Color(1f, .9f, .6f), 2.2f, .5f);
            var tiers = new[] { (3.8f, 5f), (3.2f, 5f), (2.6f, 4.6f), (2f, 4.2f), (1.5f, 3.6f) };
            string[] words = { "#1", "TOP 40", "HIT PARADE", "NEW ENTRY" };
            var y = 0f;
            for (var k = 0; k < tiers.Length; k++)
            {
                var (a, h) = tiers[k];
                mk.Box(gold, new Vector3(0, y + h / 2f, 0), new Vector3(2f * a, h, 2f * a)); b.solid.Box(gold, new Vector3(0, y + h / 2f, 0), new Vector3(2f * a, h, 2f * a));
                for (var f = 0; f < 4; f++)
                {
                    var n = Quaternion.Euler(0, f * 90f, 0) * Vector3.forward; var side = Vector3.Cross(up, n);
                    if (!(k == 0 && f == 2)) { mk.Prism(gold, n * (a + .01f) + up * (y + h * .5f), n, a * .62f, .14f, 36); mk.Prism(G.Dark, n * (a + .15f) + up * (y + h * .5f), n, a * .36f, .01f, 30); mk.Prism(red, n * (a + .16f) + up * (y + h * .5f), n, a * .2f, .02f, 20); }
                    // the marquee band of bulbs at the top of each tier
                    mk.Box(band, n * (a + .06f) + up * (y + h - .45f), new Vector3(2f * a + .12f, .7f, .1f), Quaternion.LookRotation(n));
                    var count = Mathf.RoundToInt(2f * a / .45f); for (var i = 0; i <= count; i++) { var bp = n * (a + .13f) + side * (-a + i * (2f * a / count)) + up * (y + h - .45f); mk.Blob(i % 2 == 0 ? bulbA : bulbB, bp + up * .2f, Vector3.one * .08f, b.rng, 0f); mk.Blob(i % 2 == 0 ? bulbB : bulbA, bp - up * .2f, Vector3.one * .08f, b.rng, 0f); }
                    if (k == 1) ArtistBits.Label(b.t, words[f], b.W(n * (a + .16f) + up * (y + h - .45f)), n, .32f, new Color(1f, .85f, .5f), TextAnchor.MiddleCenter);
                }
                y += h;
            }
            // the red carpet and the door
            mk.Box(G.Dark, new Vector3(0, 1.6f, -3.82f), new Vector3(2.2f, 3.2f, .1f)); mk.Box(S(new Color(.7f, .1f, .14f)), new Vector3(0, .03f, -6.6f), new Vector3(2f, .06f, 5.5f));
            foreach (var e in new[] { -1.4f, 1.4f }) for (var i = 0; i < 3; i++) { var pp = new Vector3(e, 0, -4.6f - i * 1.8f); mk.Prism(gold, pp, up, .07f, 1f, 8); mk.Blob(gold, pp + up * 1.05f, Vector3.one * .12f, b.rng, 0f); b.Post(pp, 1f, .1f); }
            b.Sign("THE CHART ISLES", new Vector3(0, 3.7f, -3.86f), .3f, new Color(1f, .85f, .5f));
            // the glitter ball
            var gm = new MeshKit(); gm.Blob(S(new Color(.78f, .8f, .86f), .9f), Vector3.zero, Vector3.one * 2.6f, b.rng, 0f); var tile = Gl(new Color(.95f, .97f, 1f), 1.8f);
            for (var i = 0; i < 90; i++) { var d = Random.onUnitSphere; gm.Box(tile, d * 2.6f, new Vector3(.36f, .36f, .04f), Quaternion.LookRotation(d)); }
            mk.Prism(gold, new Vector3(0, y, 0), up, .2f, 1.4f, 8);
            var ball = b.Part(gm, "Glitter ball", new Vector3(0, y + 4f, 0), Quaternion.identity); if (ball != null) b.life.spin.Add((ball, up, 22f));
            // spotlight beams swinging across the sky from the top tier
            for (var k = 0; k < 4; k++)
            {
                var d = Quaternion.Euler(0, k * 90f + 45f, 0) * Vector3.forward; var sm = new MeshKit(); sm.Prism(Gl(new Color(1f, .9f, .7f), .35f), Vector3.zero, up, .25f, 120f, 8, 6f, 0f, false);
                var home = Quaternion.AngleAxis(25f, Vector3.Cross(up, d)); var sp = b.Part(sm, "Spotlight", d * 1.8f + up * (y - 3.6f), home);
                if (sp != null) { b.life.swing.Add((sp, home, Vector3.Cross(up, d), 18f, .4f + k * .07f)); foreach (var r in sp.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; }
            }
            b.Light(new Vector3(0, y + 4f, 0), new Color(1f, .8f, .9f), 40f, 2.2f); b.Light(new Vector3(0, 4f, -7f), new Color(1f, .85f, .5f), 16f, 1.4f);
        }

        // ---------------------------------------------------------------- the Groovelands: stacked bandstands and a saxophone
        static void Bandstand(B b)
        {
            b.life.bpm = 112f; var mk = b.mk;
            var cream = S(new Color(.94f, .9f, .8f)); var brass = S(new Color(.88f, .66f, .28f), .7f); var roof = S(new Color(.3f, .56f, .48f), .3f); var warm = new Color(1f, .8f, .5f);
            var bulbA = b.Pulse(warm, 2.2f); var bulbB = b.Pulse(new Color(1f, .55f, .4f), 2.2f, .5f);
            var tiers = new[] { (5.4f, 6.2f), (4.4f, 5.6f), (3.4f, 5f) }; var y = 0f;
            for (var k = 0; k < tiers.Length; k++)
            {
                var (r, h) = tiers[k]; var floor = k == 0 ? .8f : .5f;
                mk.Prism(cream, new Vector3(0, y, 0), up, r, floor, 24); b.solid.Prism(cream, new Vector3(0, y, 0), up, r, floor, 24);
                var top = y + h - 1.2f;
                for (var c = 0; c < 8; c++) { var a = (c + .5f) * Mathf.PI / 4f; var cp = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (r - .45f); mk.Prism(brass, cp + up * (y + floor), up, .14f, top - y - floor, 10); if (k == 0) b.Post(cp + up * y, top, .2f); }
                mk.Prism(cream, new Vector3(0, top, 0), up, r + .3f, .6f, 24);
                // string lights hung between the columns
                for (var c = 0; c < 8; c++)
                {
                    var a0 = (c + .5f) * Mathf.PI / 4f; var a1 = (c + 1.5f) * Mathf.PI / 4f; var p0 = new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * (r - .2f) + up * top; var p1 = new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * (r - .2f) + up * top;
                    for (var i = 1; i < 6; i++) { var t = i / 6f; mk.Blob(i % 2 == 0 ? bulbA : bulbB, Vector3.Lerp(p0, p1, t) - up * Mathf.Sin(t * Mathf.PI) * .5f, Vector3.one * .09f, b.rng, 0f); }
                }
                if (k < tiers.Length - 1) { mk.Cone(roof, new Vector3(0, top + .6f, 0), r + .6f, 1f, 24); y = top + .6f + .6f; }
                else { mk.Cone(roof, new Vector3(0, top + .6f, 0), r + .6f, 3.4f, 24); y = top + 4f; }
            }
            // steps up into the bottom bandstand, music stands and a drum inside
            for (var i = 0; i < 3; i++) mk.Box(cream, new Vector3(0, (i + 1) * .135f, -5.9f + i * .45f), new Vector3(2.4f, (i + 1) * .27f, .45f));
            b.solid.Box(cream, new Vector3(0, .3f, -5.4f), new Vector3(2.4f, .6f, .8f), Quaternion.Euler(-30f, 0, 0));
            for (var i = 0; i < 4; i++) { var sp = new Vector3(-2.2f + i * 1.45f, .8f, 1.5f); mk.Prism(G.Dark, sp, up, .03f, 1.1f, 5); mk.Box(G.Dark, sp + up * 1.15f, new Vector3(.5f, .35f, .03f), Quaternion.Euler(-20f, 0, 0)); }
            mk.Prism(S(new Color(.75f, .15f, .15f), .4f), new Vector3(0, .8f, 3f), up, .7f, .7f, 20); mk.Prism(S(new Color(.95f, .93f, .88f)), new Vector3(0, 1.5f, 3f), up, .7f, .02f, 20); b.Post(new Vector3(0, .8f, 3f), .7f, .7f);
            b.Sign("THE GROOVELANDS", new Vector3(0, 5.4f, -5.75f), .32f, new Color(.25f, .18f, .1f));
            // the saxophone winding up the tower to its bell
            var pts = new List<Vector3>(); for (var i = 0; i <= 60; i++) { var t = i / 60f; var a = -Mathf.PI / 2f + .6f + t * Mathf.PI * 3f; var rr = Mathf.Lerp(6.1f, 4.6f, t); pts.Add(new Vector3(Mathf.Cos(a) * rr, 1.2f + t * (y - 4f), Mathf.Sin(a) * rr)); }
            for (var i = 0; i + 1 < pts.Count; i++) { var w = Mathf.Lerp(.28f, .7f, i / (float)pts.Count); var d = pts[i + 1] - pts[i]; mk.Box(brass, (pts[i] + pts[i + 1]) / 2f, new Vector3(w, w, d.magnitude + .1f), Quaternion.LookRotation(d)); if (i % 5 == 2) mk.Prism(S(new Color(.98f, .86f, .5f), .8f), pts[i] + Vector3.Cross(d.normalized, up).normalized * w * .6f, Vector3.Cross(d.normalized, up).normalized, w * .35f, .08f, 12); }
            mk.Prism(G.Dark, pts[0], (pts[0] - pts[1]).normalized, .14f, .6f, 8, .6f); b.Post(pts[0] - up * 1.2f, 2f, .4f);
            var end = pts[pts.Count - 1]; var bd = ((pts[pts.Count - 1] - pts[pts.Count - 2]).normalized + up * 1.4f).normalized;
            mk.Prism(brass, end, bd, .7f, 2.2f, 24, 2.6f, 0f, false); mk.Prism(b.Pulse(new Color(1f, .8f, .4f), 2f), end + bd * 1.6f, bd, 1.4f, .05f, 24);
            // the golden bell on the very top
            mk.Prism(brass, new Vector3(0, y - .5f, 0), up, .08f, 1f, 6); mk.Prism(S(new Color(1f, .8f, .3f), .9f), new Vector3(0, y + .3f, 0), up, .7f, 1f, 16, .45f); mk.Blob(S(new Color(1f, .8f, .3f), .9f), new Vector3(0, y + 1.35f, 0), Vector3.one * .3f, b.rng, 0f);
            b.Light(new Vector3(0, 4f, 0), warm, 14f, 1.4f); b.Light(end + up * 2f, new Color(1f, .75f, .4f), 36f, 2f);
        }

        // ---------------------------------------------------------------- the Boroughs: the tower block and the boombox
        static void Block(B b)
        {
            b.life.bpm = 92f; var mk = b.mk;
            var brick = S(new Color(.52f, .25f, .18f)); var stone = S(new Color(.62f, .58f, .52f)); var iron = S(new Color(.12f, .12f, .14f), .3f);
            const float H = 24f, A = 4f; var warm = new Color(1f, .78f, .45f);
            mk.Box(brick, new Vector3(0, H / 2f, 0), new Vector3(2f * A, H, 2f * A)); b.solid.Box(brick, new Vector3(0, H / 2f, 0), new Vector3(2f * A, H, 2f * A));
            var lit = Gl(warm, 1.2f); var litB = Gl(new Color(.6f, .75f, 1f), .9f); var dark = S(new Color(.1f, .1f, .13f), .5f);
            for (var f = 0; f < 4; f++)
            {
                var n = Quaternion.Euler(0, f * 90f, 0) * Vector3.forward; var side = Vector3.Cross(up, n); var rot = Quaternion.LookRotation(n);
                for (var k = 0; k < 7; k++)
                {
                    var wy = 4.4f + k * 2.8f; mk.Box(stone, n * (A + .06f) + up * (wy - .95f), new Vector3(2f * A + .1f, .14f, .12f), rot);
                    for (var c = 0; c < 3; c++) { if (f == 1 && c == 2) continue; var r = b.rng.NextDouble(); mk.Box(r < .55 ? lit : r < .7 ? litB : dark, n * (A + .02f) + side * (c - 1f) * 2.4f + up * wy, new Vector3(1.1f, 1.4f, .04f), rot); }
                }
            }
            // graffiti round the bottom
            Color[] paint = { new Color(1f, .3f, .5f), new Color(.2f, .8f, 1f), new Color(1f, .85f, .2f), new Color(.5f, 1f, .4f), new Color(.7f, .45f, 1f) };
            string[] tags = { "BOOM", "BAP", "RHYME", "FLOW", "BEATS", "CREW" };
            for (var f = 0; f < 4; f++)
            {
                var n = Quaternion.Euler(0, f * 90f, 0) * Vector3.forward; var side = Vector3.Cross(up, n); var rot = Quaternion.LookRotation(n);
                for (var i = 0; i < 4; i++) { var gp = n * (A + .03f) + side * (-2.8f + i * 1.9f + (float)b.rng.NextDouble() * .4f) + up * (1.1f + (float)b.rng.NextDouble() * 1.2f); if (f == 2 && Mathf.Abs(Vector3.Dot(gp, side)) < 1.6f) continue; mk.Box(Gl(paint[(i + f) % paint.Length], .5f), gp, new Vector3(1.4f + (float)b.rng.NextDouble(), .7f + (float)b.rng.NextDouble() * .6f, .03f), rot * Quaternion.Euler(0, 0, ((float)b.rng.NextDouble() - .5f) * 16f)); }
                ArtistBits.Label(b.t, tags[f], b.W(n * (A + .05f) + side * ((f % 2 == 0 ? 1f : -1f) * 1.4f) + up * 2.9f), n, .7f, paint[(f + 2) % paint.Length], TextAnchor.MiddleCenter);
            }
            // the door
            mk.Box(S(new Color(.25f, .3f, .32f), .5f), new Vector3(0, 1.5f, -A - .04f), new Vector3(1.8f, 3f, .1f)); mk.Box(stone, new Vector3(0, .15f, -A - .6f), new Vector3(2.6f, .3f, 1.2f)); b.solid.Box(stone, new Vector3(0, .15f, -A - .6f), new Vector3(2.6f, .3f, 1.2f));
            mk.Box(Gl(warm, 1.8f), new Vector3(0, 3.3f, -A - .15f), new Vector3(.5f, .2f, .2f)); b.Light(new Vector3(0, 3.2f, -A - 1.2f), warm, 9f, 1.3f);
            b.Sign("THE BOROUGHS", new Vector3(0, 3.8f, -A - .06f), .34f, new Color(.95f, .9f, .8f));
            // the fire escape zigzagging up the east side
            for (var k = 0; k < 7; k++)
            {
                var py = 3.4f + k * 2.8f; var pc = new Vector3(A + .75f, py, 0);
                mk.Box(iron, pc, new Vector3(1.5f, .08f, 4.2f)); mk.Box(iron, pc + new Vector3(.72f, .5f, 0), new Vector3(.05f, .05f, 4.2f)); for (var i = 0; i < 5; i++) mk.Box(iron, pc + new Vector3(.72f, .25f, -2f + i), new Vector3(.04f, .5f, .04f));
                if (k < 6) { var s = k % 2 == 0 ? 1f : -1f; var a = pc + new Vector3(0, .05f, -s * 1.6f); var c2 = new Vector3(A + .75f, py + 2.8f, s * 1.6f); var d = c2 - a; mk.Box(iron, (a + c2) / 2f, new Vector3(.8f, .06f, d.magnitude), Quaternion.LookRotation(d)); }
            }
            // the roof: a parapet, a water tower and the boombox
            foreach (var f in new[] { 0f, 90f, 180f, 270f }) { var n = Quaternion.Euler(0, f, 0) * Vector3.forward; mk.Box(stone, n * (A - .1f) + up * (H + .35f), new Vector3(2f * A, .7f, .2f), Quaternion.LookRotation(n)); }
            var wt = new Vector3(2.4f, H, 2.4f); var wood = S(new Color(.48f, .34f, .22f));
            foreach (var e in new[] { new Vector3(-.8f, 0, -.8f), new Vector3(.8f, 0, -.8f), new Vector3(-.8f, 0, .8f), new Vector3(.8f, 0, .8f) }) mk.Box(iron, wt + e + up * 1.2f, new Vector3(.12f, 2.4f, .12f));
            mk.Prism(wood, wt + up * 2.4f, up, 1.3f, 2.6f, 16); mk.Cone(iron, wt + up * 5f, 1.45f, 1f, 16);
            var bx = new Vector3(-1.2f, H, -.6f); var silver = S(new Color(.74f, .75f, .78f), .7f); var cap = b.Pulse(new Color(1f, .5f, .2f), 1.8f);
            mk.Box(silver, bx + up * 1.9f, new Vector3(6.4f, 3.4f, 1.8f)); b.solid.Box(silver, bx + up * 1.9f, new Vector3(6.4f, 3.4f, 1.8f));
            foreach (var f in new[] { -1f, 1f })
            {
                var fz = f * .92f; mk.Box(G.Dark, bx + new Vector3(0, 1.9f, fz), new Vector3(6f, 3f, .04f));
                foreach (var e in new[] { -2f, 2f }) b.Speaker(bx + new Vector3(e, 1.8f, fz + f * .03f), Vector3.forward * f, 1.15f, cap, .2f);
                mk.Box(Gl(new Color(.6f, .85f, 1f), .9f), bx + new Vector3(0, 2.3f, fz + f * .03f), new Vector3(1.4f, .8f, .03f));
                for (var i = 0; i < 5; i++) mk.Box(b.Pulse(i < 3 ? new Color(.4f, 1f, .5f) : new Color(1f, .4f, .3f), 1.6f, i * .1f), bx + new Vector3(-.5f + i * .25f, 1.35f + i * .05f, fz + f * .03f), new Vector3(.16f, .3f + i * .1f, .03f));
            }
            mk.Box(silver, bx + up * 4.1f, new Vector3(4.6f, .25f, .3f)); foreach (var e in new[] { -2.2f, 2.2f }) mk.Box(silver, bx + new Vector3(e, 3.85f, 0), new Vector3(.25f, .7f, .3f));
            mk.Prism(silver, bx + new Vector3(2.6f, 3.6f, 0), (up + Vector3.right * .4f).normalized, .04f, 3.2f, 6);
            b.Light(bx + up * 4f + Vector3.back * 2f, new Color(1f, .6f, .3f), 30f, 1.8f);
        }

        // ---------------------------------------------------------------- the Isles of Story: the theatre spire
        static void Theatre(B b)
        {
            b.life.bpm = 96f; var mk = b.mk;
            var cream = S(new Color(.93f, .88f, .78f)); var gold = S(new Color(1f, .9f, .62f), .6f); var velvet = S(new Color(.6f, .07f, .1f), .25f); var warm = new Color(1f, .8f, .5f);
            mk.Box(cream, new Vector3(0, 3.5f, 0), new Vector3(9f, 7f, 8.4f)); b.solid.Box(cream, new Vector3(0, 3.5f, 0), new Vector3(9f, 7f, 8.4f));
            mk.Box(gold, new Vector3(0, 7.1f, 0), new Vector3(9.4f, .3f, 8.8f)); mk.Box(gold, new Vector3(0, .2f, 0), new Vector3(9.3f, .4f, 8.7f));
            // the gold proscenium arch with red curtains drawn back from a dark doorway
            var fz = -4.22f;
            foreach (var e in new[] { -1f, 1f }) mk.Box(gold, new Vector3(e * 2.6f, 2.6f, fz - .1f), new Vector3(.5f, 5.2f, .3f));
            for (var i = 0; i <= 12; i++) { var a = Mathf.PI * i / 12f; mk.Box(gold, new Vector3(Mathf.Cos(a) * 2.6f, 5.2f + Mathf.Sin(a) * 1.1f, fz - .1f), new Vector3(.5f, .5f, .3f), Quaternion.Euler(0, 0, a * Mathf.Rad2Deg)); }
            mk.Box(G.Dark, new Vector3(0, 2.5f, fz + .02f), new Vector3(4.6f, 5f, .04f));
            foreach (var e in new[] { -1f, 1f }) for (var i = 0; i < 4; i++) mk.Box(velvet, new Vector3(e * (1.9f - i * .2f), 2.6f, fz - .14f - (i % 2) * .06f), new Vector3(.36f, 5f, .1f));
            for (var i = 0; i < 9; i++) mk.Blob(velvet, new Vector3(-2.2f + i * .55f, 5.05f, fz - .2f), new Vector3(.3f, .22f, .1f), b.rng, 0f);
            for (var i = 0; i < 14; i++) { var a = Mathf.PI * (i + .5f) / 14f; mk.Blob(b.Pulse(warm, 2.2f, i % 2 * .5f), new Vector3(Mathf.Cos(a) * 3.1f, 5.2f + Mathf.Sin(a) * 1.5f, fz - .28f), Vector3.one * .1f, b.rng, 0f); }
            b.Sign("THE ISLES OF STORY", new Vector3(0, 6.55f, fz - .02f), .34f, new Color(.45f, .1f, .1f)); b.Light(new Vector3(0, 4.5f, fz - 2.5f), warm, 12f, 1.5f);
            // the clapperboard on the east wall, its arm snapping
            var cb = new Vector3(4.52f, 3.6f, 0); mk.Box(G.Dark, cb, new Vector3(.1f, 1.8f, 2.4f)); for (var i = 0; i < 3; i++) mk.Box(S(new Color(.95f, .95f, .95f)), cb + new Vector3(.06f, -.3f + i * .5f, 0), new Vector3(.02f, .06f, 2.2f));
            var am = new MeshKit(); for (var i = 0; i < 6; i++) am.Box(i % 2 == 0 ? G.Dark : S(new Color(.95f, .95f, .95f)), new Vector3(0, .2f, -1.0f + i * .4f), new Vector3(.12f, .4f, .4f));
            var arm = b.Part(am, "Clapper", cb + new Vector3(0, .9f, -1.2f) + Vector3.forward * 0f, Quaternion.identity); if (arm != null) b.life.swing.Add((arm, Quaternion.Euler(-12f, 0, 0), Vector3.right, 12f, 1.1f));
            // the spire and the filmstrip turning round it
            const float S0 = 7.3f, SH = 28f; float R(float y) => Mathf.Lerp(3.4f, .4f, (y - S0) / SH);
            mk.Prism(gold, new Vector3(0, S0, 0), up, 3.4f, SH, 8, .4f / 3.4f); b.Post(new Vector3(0, S0, 0), SH * .6f, 3f);
            var fm = new MeshKit(); var film = S(new Color(.08f, .08f, .1f), .4f); var frame = Gl(new Color(1f, .92f, .75f), .7f); var hole = S(new Color(.9f, .9f, .9f));
            for (var i = 0; i < 70; i++)
            {
                var t = i / 70f; var a = t * Mathf.PI * 2f * 2.5f; var yy = S0 + 1.5f + t * (SH - 8f); var r = R(yy) + .5f; var p = new Vector3(Mathf.Cos(a) * r, yy, Mathf.Sin(a) * r); var tan = new Vector3(-Mathf.Sin(a), .12f, Mathf.Cos(a)).normalized; var n = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                fm.Box(film, p, new Vector3(1.05f, 1f, .04f), Quaternion.LookRotation(n)); fm.Box(frame, p + n * .03f, new Vector3(.7f, .6f, .02f), Quaternion.LookRotation(n)); foreach (var e in new[] { -.42f, .42f }) fm.Box(hole, p + n * .03f + up * e, new Vector3(.12f, .08f, .02f), Quaternion.LookRotation(n));
            }
            var strip = b.Part(fm, "Filmstrip", Vector3.zero, Quaternion.identity); if (strip != null) b.life.spin.Add((strip, up, 6f));
            // the conductor's baton at the top
            var by = S0 + SH; mk.Blob(S(new Color(.75f, .6f, .42f)), new Vector3(0, by + .4f, 0), new Vector3(.32f, .6f, .32f), b.rng, 0f); mk.Prism(S(new Color(.97f, .96f, .92f), .5f), new Vector3(0, by + .9f, 0), up, .1f, 6f, 8, .3f); mk.Blob(b.Pulse(new Color(1f, .9f, .6f), 2.6f), new Vector3(0, by + 7f, 0), Vector3.one * .3f, b.rng, 0f);
            b.Light(new Vector3(0, by + 6f, 0), warm, 36f, 2f); b.Light(new Vector3(6f, 10f, 0), new Color(1f, .5f, .5f), 16f, 1.2f);
        }
    }
}
