using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The J-Rock Isles at dusk, rushing like an anime opening:
    /// - cherry petals drift down round you; big clouds lit orange and pink; the windows and lanterns glow, brighter
    ///   with the music; Hoshikuma Minami's screens cycle colours on the beat;
    /// - a train runs along the coast and back (where it is comes from the shared clock, so friends see the same
    ///   train), the crossing lights blinking as it nears;
    /// - Sithu Aye's kite flies over the school on a long string;
    /// - "Bad Apple!!": the whole land drains to black and white. "COLORS": the sky cycles through every colour.
    ///   "FIRE BIRD": the sky burns orange and the phoenix blazes. "Skye": the sky clears to a bright blue and the kite
    ///   climbs. "OXYGEN": bubbles rise round the stage. Every song's own light brightens while it plays.
    public class JRockLife : MonoBehaviour
    {
        const string Land = "The J-Rock Isles";
        public Vector3 center, shrineAt, schoolRoof, crossingAt, oxygenAt, phoenix, kiteAt, kiteString;
        public Material lanterns, windows, windowsCool, vending;
        public Material[] crossing = new Material[0], screens = new Material[0];
        public Vector3[] track = new Vector3[0];
        public Transform kite; public string oxygenSong, kiteSong;
        public List<Vector3> blossomAt = new List<Vector3>(), neon = new List<Vector3>();
        public List<(string song, Material m, Color c)> songGlows = new List<(string, Material, Color)>();
        readonly Dictionary<string, float> songAmount = new Dictionary<string, float>();

        CloudLayer clouds; ParticleSystem petals, bubbles; Transform train; LineRenderer kiteLine;
        Color lanternBase, windowBase, coolBase, vendBase; readonly List<Color> screenBase = new List<Color>(); Color[] crossBase = new Color[0];
        float mono, colors, fire, skye, nextPetal, nextBubble;
        float trackLen; readonly List<float> trackAt = new List<float>();

        void Start()
        {
            clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 70; clouds.span = 2600f; clouds.key = "jrock";
            clouds.minHeight = 140f; clouds.maxHeight = 240f; clouds.size = 3.4f; clouds.speed = 6f; clouds.cover = .55f;
            clouds.color = new Color(1f, .62f, .55f);
            Color E(Material m) => m != null ? m.GetColor("_EmissionColor") : Color.black;
            lanternBase = E(lanterns); windowBase = E(windows); coolBase = E(windowsCool); vendBase = E(vending);
            foreach (var s in screens) screenBase.Add(E(s));
            crossBase = new Color[crossing.Length]; for (var k = 0; k < crossing.Length; k++) crossBase[k] = E(crossing[k]);
            petals = Particles("Cherry petals", 900, new Color(1f, .78f, .88f, 1f), .08f, .14f, false);
            var pm = petals.main; pm.startLifetime = new ParticleSystem.MinMaxCurve(5f, 8f); pm.startSpeed = new ParticleSystem.MinMaxCurve(.1f, .5f); pm.gravityModifier = .02f;
            var pr = petals.rotationOverLifetime; pr.enabled = true; pr.z = new ParticleSystem.MinMaxCurve(-4f, 4f);
            bubbles = Particles("Oxygen bubbles", 500, new Color(.75f, .95f, 1f, .8f), .2f, .5f, true);
            var bm = bubbles.main; bm.startLifetime = new ParticleSystem.MinMaxCurve(3f, 5f); bm.startSpeed = new ParticleSystem.MinMaxCurve(1f, 2.4f); bm.gravityModifier = -.05f;
            var bs = bubbles.shape; bs.shapeType = ParticleSystemShapeType.Cone; bs.angle = 25f; bs.radius = 4f; bs.rotation = new Vector3(-90f, 0, 0);
            // the train: three cars with lit windows
            if (track.Length > 1)
            {
                trackAt.Add(0f); for (var k = 1; k < track.Length; k++) { trackLen += Vector3.Distance(track[k - 1], track[k]); trackAt.Add(trackLen); }
                var tk = new MeshKit(); var body = WorldMaterials.Solid(new Color(.88f, .9f, .92f)); var stripe = WorldMaterials.Solid(new Color(.15f, .55f, .35f)); var lit = WorldMaterials.Glow(new Color(1f, .9f, .7f), 1.2f);
                for (var c = 0; c < 3; c++)
                {
                    var o = Vector3.forward * (c * 12.6f - 12.6f);
                    tk.Box(body, o + Vector3.up * 1.9f, new Vector3(2.8f, 3f, 12f), Quaternion.identity);
                    tk.Box(stripe, o + Vector3.up * 1f, new Vector3(2.84f, .35f, 12.02f), Quaternion.identity);
                    foreach (var s in new[] { -1f, 1f }) for (var w = 0; w < 5; w++) tk.Box(lit, o + new Vector3(s * 1.42f, 2.3f, (w - 2f) * 2.2f), new Vector3(.03f, 1f, 1.5f), Quaternion.identity);
                }
                tk.Box(lit, Vector3.forward * 18.62f + Vector3.up * 1.4f, new Vector3(1.6f, .3f, .05f), Quaternion.identity);
                var go = tk.BuildSingle("Coast train", transform, false); if (go != null) train = go.transform;
            }
            if (kite != null)
            {
                var lgo = new GameObject("Kite string"); lgo.transform.SetParent(transform, false); kiteLine = lgo.AddComponent<LineRenderer>();
                kiteLine.positionCount = 2; kiteLine.widthMultiplier = .03f; kiteLine.useWorldSpace = true; kiteLine.sharedMaterial = WorldMaterials.Solid(new Color(.95f, .95f, .95f)); kiteLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        static Material Additive(string name)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = name };
            m.SetColor("_BaseColor", Color.white); m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 2f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            m.SetFloat("_ZWrite", 0f); m.SetFloat("_Cull", 0f); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            return m;
        }

        ParticleSystem Particles(string name, int max, Color col, float s0, float s1, bool add)
        {
            var go = new GameObject(name); go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.loop = false; main.playOnAwake = false; main.maxParticles = max;
            main.simulationSpace = ParticleSystemSimulationSpace.World; main.startSize = new ParticleSystem.MinMaxCurve(s0, s1); main.startColor = col;
            var e = ps.emission; e.rateOverTime = 0f;
            var cl = ps.colorOverLifetime; cl.enabled = true;
            var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, .1f), new GradientAlphaKey(0f, 1f) });
            cl.color = g;
            var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (var y = 0; y < 32; y++) for (var x = 0; x < 32; x++) { var d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(16, 16)) / 16f; tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Pow(Mathf.Clamp01(1f - d), 1.5f))); }
            tex.Apply();
            var r = go.GetComponent<ParticleSystemRenderer>();
            var m = add ? Additive(name) : new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = name };
            if (!add) { m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f); m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha); m.SetFloat("_ZWrite", 0f); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent; }
            m.SetTexture("_BaseMap", tex); r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return ps;
        }

        static bool Playing(string id, string artist, string title)
        {
            if (id == null || !JRockSongs.Ids(artist, out var l)) return false;
            foreach (var (t, i) in l) if (i == id && t.StartsWith(title)) return true;
            return false;
        }

        Vector3 TrackPoint(float s, out Vector3 dir)
        {
            s = Mathf.Clamp(s, 0f, trackLen); var k = 1; while (k < trackAt.Count - 1 && trackAt[k] < s) k++;
            var t = Mathf.InverseLerp(trackAt[k - 1], trackAt[k], s); dir = (track[k] - track[k - 1]).normalized;
            return Vector3.Lerp(track[k - 1], track[k], t);
        }

        void Update()
        {
            var dt = Time.deltaTime; var t = Time.time; var now = SharedChance.Now;
            var here = SkyDirector.Instance != null && SkyDirector.Instance.CurrentLand == Land;
            var d = MusicDirector.Instance; var playing = d != null && d.CurrentSlot.IsValid;
            var energy = SurpriseDirector.Energy;
            var id = MusicDirector.ForcedTrackId ?? (playing ? d.CurrentSlot.track.id : null);

            mono = Mathf.MoveTowards(mono, here && Playing(id, "nomico", "Bad Apple") ? 1f : 0f, dt / 2f);
            colors = Mathf.MoveTowards(colors, here && Playing(id, "FLOW", "COLORS") ? 1f : 0f, dt / 2f);
            fire = Mathf.MoveTowards(fire, here && Playing(id, "Roselia", "FIRE BIRD") ? 1f : 0f, dt / 3f);
            skye = Mathf.MoveTowards(skye, here && Playing(id, "Sithu Aye", "Skye") ? 1f : 0f, dt / 4f);

            foreach (var (s, _, _) in songGlows) if (!songAmount.ContainsKey(s)) songAmount[s] = 0f;
            var keys = new List<string>(songAmount.Keys);
            foreach (var s in keys) songAmount[s] = Mathf.MoveTowards(songAmount[s], here && id == s ? 1f : 0f, dt / 2f);
            foreach (var (s, m, c) in songGlows) if (m != null) m.SetColor("_EmissionColor", c * (1f + songAmount[s] * (2.5f + Mathf.Sin(t * 2.2f) * .6f)));

            // the lights: warm, brighter with the music; drained of colour in "Bad Apple!!"
            Color Drain(Color c) { var g = (c.r + c.g + c.b) / 3f; return Color.Lerp(c, new Color(g, g, g), mono); }
            var pulse = 1f + energy * .5f;
            if (lanterns != null) lanterns.SetColor("_EmissionColor", Drain(lanternBase) * (pulse + .1f * Mathf.Sin(t * 2f)));
            if (windows != null) windows.SetColor("_EmissionColor", Drain(windowBase) * pulse);
            if (windowsCool != null) windowsCool.SetColor("_EmissionColor", Drain(coolBase) * pulse);
            if (vending != null) vending.SetColor("_EmissionColor", vendBase * (1f + .05f * Mathf.Sin(t * 9f)));
            var beat = Mathf.Repeat(t * (1.2f + energy * 1.8f), 3f);
            for (var k = 0; k < screens.Length; k++) if (screens[k] != null) { var hue = Mathf.Repeat(t * .08f + k * .33f + (colors > 0f ? t * .4f : 0f), 1f); var col = Color.HSVToRGB(hue, .75f, 1f); screens[k].SetColor("_EmissionColor", Drain(Color.Lerp(screenBase[k], col, .7f)) * (.6f + (Mathf.FloorToInt(beat) == k ? energy * 1f : 0f))); }

            // the sky: a dramatic dusk; grey in "Bad Apple!!", every colour in "COLORS", burning in "FIRE BIRD", clear blue in "Skye"
            var tint = new Color(1f, .55f, .45f); var amount = .16f;
            if (colors > 0f) { tint = Color.Lerp(tint, Color.HSVToRGB(Mathf.Repeat(t * .15f, 1f), .7f, 1f), colors); amount = Mathf.Lerp(amount, .45f, colors); }
            if (fire > 0f) { tint = Color.Lerp(tint, new Color(1f, .35f, .1f), fire); amount = Mathf.Lerp(amount, .5f, fire); }
            if (skye > 0f) { tint = Color.Lerp(tint, new Color(.45f, .75f, 1f), skye); amount = Mathf.Lerp(amount, .45f, skye); }
            if (mono > 0f) { tint = Color.Lerp(tint, new Color(.7f, .7f, .7f), mono); amount = Mathf.Lerp(amount, .75f, mono); }
            SkyDirector.SetSongTint(Land, tint, amount);
            if (here) { var w = clouds != null ? clouds.Wind : new Vector2(1, 0); Shader.SetGlobalFloat("_EW_Wind", .4f + .3f * energy); Shader.SetGlobalVector("_EW_WindDir", new Vector4(w.x, 0, w.y, 0)); }

            // the train: out along the coast and back, a pause at each end
            var nearCrossing = false;
            if (train != null && trackLen > 20f)
            {
                const float speed = 14f; var run = trackLen / speed; var cycle = run * 2f + 20f; var ph = (float)(now % cycle);
                float s; var back = false;
                if (ph < run) s = ph * speed; else if (ph < run + 10f) s = trackLen; else if (ph < run * 2f + 10f) { s = trackLen - (ph - run - 10f) * speed; back = true; } else s = 0f;
                var p = TrackPoint(s, out var dir); if (back) dir = -dir;
                train.SetPositionAndRotation(p + Vector3.up * .5f, Quaternion.LookRotation(new Vector3(dir.x, 0, dir.z)));
                nearCrossing = crossingAt != Vector3.zero && Vector3.Distance(p, crossingAt) < 60f;
            }
            for (var k = 0; k < crossing.Length; k++) if (crossing[k] != null) crossing[k].SetColor("_EmissionColor", crossBase[k] * (nearCrossing ? ((Mathf.Repeat(t * 1.6f, 1f) < .5f) == (k == 0) ? 1.6f : .05f) : .15f));

            // the kite
            if (kite != null)
            {
                var kp = kiteAt + new Vector3(Mathf.Sin(t * .4f) * 3f, Mathf.Sin(t * .7f) * 1.5f + skye * 10f, Mathf.Cos(t * .3f) * 2f);
                kite.SetPositionAndRotation(kp, Quaternion.Euler(0, t * 6f, Mathf.Sin(t * 1.3f) * 15f));
                if (kiteLine != null) { kiteLine.SetPosition(0, kiteString); kiteLine.SetPosition(1, kp - Vector3.up * 1f); }
            }

            if (!here) return;
            var cam = Camera.main != null ? Camera.main.transform.position : center;
            nextPetal -= dt;
            if (nextPetal <= 0f) { nextPetal = .05f; petals.transform.position = cam + new Vector3(Random.Range(-25f, 25f), Random.Range(5f, 12f), Random.Range(-25f, 25f)); petals.Emit(1); foreach (var b in blossomAt) if ((b - cam).sqrMagnitude < 40f * 40f && Random.value < .05f) { petals.transform.position = b + Random.insideUnitSphere * 1.5f; petals.Emit(1); } }
            nextBubble -= dt;
            if (oxygenSong != null && id == oxygenSong && nextBubble <= 0f) { nextBubble = .05f; bubbles.transform.SetPositionAndRotation(oxygenAt, Quaternion.identity); bubbles.Emit(2); }
        }
    }

    /// Looks up the J-Rock Isles' songs from outside the kit.
    public static class JRockSongs
    {
        public static bool Ids(string artist, out (string title, string id)[] list) => JRockKit.SongTable.TryGetValue(artist, out list);
    }
}
