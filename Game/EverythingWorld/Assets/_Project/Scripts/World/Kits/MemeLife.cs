using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Isle of Memes, a pastel dream:
    /// - the glossy orb turns and bobs over the Homepage; desktop icons bob along the Superhighway; dolphins leap out of
    ///   their pools; bubbles rise from the fountains and drift everywhere; the screens flicker;
    /// - "Gangnam Style": everything flashes pink on the beat. "Wellerman" and "Drunken Sailor": the light goes stormy
    ///   sea-blue and the wind gets up. "A Holly Jolly Christmas": snow. "Axel F": a green flash. "The HampsterDance Song":
    ///   the colours race. "Kahoot! Lobby Music": blue and purple cycle. "Fabulous": pink glitter. "Everybody's Free (To
    ///   Wear Sunscreen)": bright sun. "Bill Nye": a lab-blue glow. "Gitchee Gitchee Goo": confetti. Every song's own
    ///   light brightens while it plays.
    public class MemeLife : MonoBehaviour
    {
        const string Land = "The Isle of Memes";
        public Vector3 center, orbAt, fountain, fountain2, metronome;
        public string gangnamSong, wellerSong, sailorSong, xmasSong, axelSong, hampSong, kahootSong, fabSong, sunSong, nyeSong, gitcheeSong;
        public Material aqua, screens, bubbles, lamps; public Transform orb; public float played = -99f;
        public List<(Transform t, Vector3 home)> icons = new List<(Transform, Vector3)>();
        public List<(Transform t, Vector3 pool, int k)> dolphins = new List<(Transform, Vector3, int)>();
        public List<(string song, Material m, Color c)> songGlows = new List<(string, Material, Color)>();
        readonly Dictionary<string, float> songAmount = new Dictionary<string, float>();

        CloudLayer clouds; ParticleSystem dots;
        Color aquaBase, screenBase, bubbleBase, lampBase;
        float gangnam, sea, xmas, axel, hamp, kahoot, fab, sun, nye, gitchee, nextFall, nextBubble;
        static readonly Color[] Confetti = { new Color(1f, .62f, .82f), new Color(.36f, .86f, .86f), new Color(.74f, .64f, 1f), new Color(1f, .88f, .5f), Color.white };

        void Start()
        {
            clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 40; clouds.span = 2400f; clouds.key = "memeisle";
            clouds.minHeight = 120f; clouds.maxHeight = 220f; clouds.size = 3.6f; clouds.speed = 1.2f; clouds.cover = .4f;
            clouds.color = new Color(1f, .86f, .96f);
            Color E(Material m) => m != null ? m.GetColor("_EmissionColor") : Color.black;
            aquaBase = E(aqua); screenBase = E(screens); bubbleBase = E(bubbles); lampBase = E(lamps);
            dots = Particles("Bubbles and confetti", 1200, Color.white, .1f, .3f, false);
            var dm = dots.main; dm.startLifetime = new ParticleSystem.MinMaxCurve(4f, 8f);
        }

        ParticleSystem Particles(string name, int max, Color col, float s0, float s1, bool additive)
        {
            var go = new GameObject(name); go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.loop = false; main.playOnAwake = false; main.maxParticles = max;
            main.simulationSpace = ParticleSystemSimulationSpace.World; main.startSize = new ParticleSystem.MinMaxCurve(s0, s1); main.startColor = col;
            var e = ps.emission; e.rateOverTime = 0f;
            var cl = ps.colorOverLifetime; cl.enabled = true;
            var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, .2f), new GradientAlphaKey(0f, 1f) });
            cl.color = g;
            var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (var y = 0; y < 32; y++) for (var x = 0; x < 32; x++) { var d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(16, 16)) / 16f; tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Pow(Mathf.Clamp01(1f - d), 1.5f))); }
            tex.Apply();
            var m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = name };
            m.SetColor("_BaseColor", Color.white); m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", additive ? 2f : 0f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)(additive ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
            m.SetFloat("_ZWrite", 0f); m.SetFloat("_Cull", 0f); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent; m.SetTexture("_BaseMap", tex);
            var r = go.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return ps;
        }

        void Update()
        {
            var dt = Time.deltaTime; var t = Time.time; var now = (float)(SharedChance.Now % 100000.0);
            var here = SkyDirector.Instance != null && SkyDirector.Instance.CurrentLand == Land;
            var d = MusicDirector.Instance; var playing = d != null && d.CurrentSlot.IsValid;
            var energy = SurpriseDirector.Energy;
            var id = MusicDirector.ForcedTrackId ?? (playing ? d.CurrentSlot.track.id : null);
            bool On(string song) => here && song != null && id == song;
            float Ease(float v, bool on, float secs) => Mathf.MoveTowards(v, on ? 1f : 0f, dt / secs);

            gangnam = Ease(gangnam, On(gangnamSong), 2f); sea = Ease(sea, On(wellerSong) || On(sailorSong), 4f); xmas = Ease(xmas, On(xmasSong), 4f); axel = Ease(axel, On(axelSong), 2f); hamp = Ease(hamp, On(hampSong), 2f); kahoot = Ease(kahoot, On(kahootSong), 3f); fab = Ease(fab, On(fabSong), 3f); sun = Ease(sun, On(sunSong), 5f); nye = Ease(nye, On(nyeSong), 4f); gitchee = Ease(gitchee, On(gitcheeSong), 2f);

            foreach (var (s, _, _) in songGlows) if (!songAmount.ContainsKey(s)) songAmount[s] = 0f;
            var keys = new List<string>(songAmount.Keys);
            foreach (var s in keys) songAmount[s] = Mathf.MoveTowards(songAmount[s], here && id == s ? 1f : 0f, dt / 2f);
            foreach (var (s, m, c) in songGlows) if (m != null) m.SetColor("_EmissionColor", c * (1f + songAmount[s] * (2.5f + Mathf.Sin(t * 2.4f) * .6f)));

            // the orb turns and bobs; icons bob; dolphins leap
            if (orb != null) { orb.position = orbAt + Vector3.up * Mathf.Sin(t * .4f) * .8f; orb.Rotate(0f, dt * 8f, 0f, Space.World); }
            for (var i = 0; i < icons.Count; i++) { var (tr, home) = icons[i]; if (tr != null) tr.position = home + Vector3.up * Mathf.Sin(t * .9f + i) * .4f; }
            foreach (var (tr, pool, k) in dolphins) { if (tr == null) continue; var ph = Mathf.Repeat(t / 7f + k * .37f, 1f); var leap = ph < .3f ? ph / .3f : -1f; if (leap < 0f) { tr.position = pool - Vector3.up * 3f; continue; } var dir = Quaternion.Euler(0, k * 77f, 0) * Vector3.forward; tr.position = pool + dir * (leap * 4f - 2f) + Vector3.up * (Mathf.Sin(leap * Mathf.PI) * 2.6f - .2f); tr.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(Mathf.Lerp(-50f, 50f, leap), 0, 0); }
            var lift = t - played < 5f ? 1f - (t - played) / 5f : 0f; var beat = Mathf.Repeat(now * 2.2f, 1f);
            if (aqua != null) aqua.SetColor("_EmissionColor", aquaBase * (.8f + .3f * Mathf.Sin(t * 1.3f) + nye * .8f));
            if (screens != null) screens.SetColor("_EmissionColor", Color.Lerp(screenBase, Color.HSVToRGB(Mathf.Repeat(t * .5f, 1f), .5f, 1f) * 1.4f, hamp + kahoot * .5f) * (Mathf.PerlinNoise(t * 8f, 1f) > .85f ? .6f : 1f));
            if (bubbles != null) bubbles.SetColor("_EmissionColor", bubbleBase * (.9f + .1f * Mathf.Sin(t * 2f) + fab * .5f));
            if (lamps != null) lamps.SetColor("_EmissionColor", lampBase * (1f + lift * .4f + sea * .4f));

            var tint = new Color(1f, .9f, 1f); var amount = .05f;
            if (sun > 0f) { tint = Color.Lerp(tint, new Color(1f, .94f, .7f), sun); amount = Mathf.Lerp(amount, .3f, sun); }
            if (fab > 0f) { tint = Color.Lerp(tint, new Color(1f, .5f, .8f), fab); amount = Mathf.Lerp(amount, .36f, fab); }
            if (kahoot > 0f) { tint = Color.Lerp(tint, Color.Lerp(new Color(.4f, .3f, .9f), new Color(.3f, .6f, 1f), Mathf.PingPong(t * .5f, 1f)), kahoot); amount = Mathf.Lerp(amount, .36f, kahoot); }
            if (hamp > 0f) { tint = Color.Lerp(tint, Color.HSVToRGB(Mathf.Repeat(now * 2f, 1f), .7f, 1f), hamp); amount = Mathf.Lerp(amount, .34f, hamp); }
            if (gangnam > 0f) { tint = Color.Lerp(tint, new Color(1f, .3f, .7f), gangnam); amount = Mathf.Lerp(amount, beat < .15f ? .45f : .12f, gangnam); }
            if (axel > 0f) { tint = Color.Lerp(tint, new Color(.4f, 1f, .3f), axel); amount = Mathf.Lerp(amount, beat < .15f ? .4f : .14f, axel); }
            if (nye > 0f) { tint = Color.Lerp(tint, new Color(.4f, .7f, 1f), nye); amount = Mathf.Lerp(amount, .34f, nye); }
            if (sea > 0f) { tint = Color.Lerp(tint, new Color(.3f, .4f, .6f), sea); amount = Mathf.Lerp(amount, .44f, sea); }
            if (xmas > 0f) { tint = Color.Lerp(tint, new Color(.86f, .92f, 1f), xmas); amount = Mathf.Lerp(amount, .4f, xmas); }
            SkyDirector.SetSongTint(Land, tint, amount);
            var wind = clouds != null ? clouds.Wind : new Vector2(1, 0);
            if (here) { Shader.SetGlobalFloat("_EW_Wind", .25f + .15f * energy + sea * .8f); Shader.SetGlobalVector("_EW_WindDir", new Vector4(wind.x, 0, wind.y, 0)); }

            var cam = Camera.main; if (!here || cam == null) return; var cp = cam.transform.position;
            if (t >= nextBubble) { nextBubble = t + .2f; foreach (var f in new[] { fountain, fountain2 }) if (f != Vector3.zero && (f - cp).sqrMagnitude < 60f * 60f) dots.Emit(new ParticleSystem.EmitParams { position = f + new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)), velocity = new Vector3(Random.Range(-.2f, .2f), Random.Range(.6f, 1.2f), Random.Range(-.2f, .2f)), startColor = new Color(.8f, .96f, 1f, .7f), startSize = Random.Range(.15f, .4f), startLifetime = 6f }, 1); }
            if (t >= nextFall)
            {
                nextFall = t + .06f; var p = cp + new Vector3(Random.Range(-16f, 16f), Random.Range(.5f, 7f), Random.Range(-16f, 16f));
                if (IndoorSpaces.Contains(p)) return;
                if (Random.value < .12f) dots.Emit(new ParticleSystem.EmitParams { position = p, velocity = new Vector3(wind.x * .2f, .3f, wind.y * .2f), startColor = new Color(.8f, .96f, 1f, .6f), startSize = Random.Range(.1f, .3f), startLifetime = 7f }, 1);
                var conf = Mathf.Max(gitchee, Mathf.Max(fab * .6f, lift * .6f)); if (Random.value < conf) dots.Emit(new ParticleSystem.EmitParams { position = cp + new Vector3(Random.Range(-14f, 14f), Random.Range(5f, 9f), Random.Range(-14f, 14f)), velocity = new Vector3(Random.Range(-.5f, .5f), -1f, Random.Range(-.5f, .5f)), startColor = Confetti[Random.Range(0, Confetti.Length)], startSize = .1f }, 1);
                if (xmas > .05f && Random.value < xmas) for (var k = 0; k < 2; k++) dots.Emit(new ParticleSystem.EmitParams { position = cp + new Vector3(Random.Range(-18f, 18f), Random.Range(5f, 9f), Random.Range(-18f, 18f)), velocity = new Vector3(Random.Range(-.2f, .2f), -.9f, Random.Range(-.2f, .2f)), startColor = Color.white, startSize = .08f, startLifetime = 9f }, 1);
            }
        }
    }
}
