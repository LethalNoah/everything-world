using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Ambient Mists at dawn:
    /// - the rope swing sways; the rowing boat rocks; sheep graze and wander a little; balloons drift on their lines; the
    ///   radio tower's red light blinks slowly; the old TVs flicker and roll; low wisps of mist drift over the grass, with
    ///   pollen and dew catching the light; now and then the whole picture wobbles like an old tape;
    /// - "Weightless": everything goes very soft and slow. "Avril 14th": a warm gold. "Roygbiv": the light drifts through
    ///   faded rainbow colours. "Xtal": a cool shimmer. "Divine Moments of Truth": colours swirl. "Japan": soft pink.
    ///   "Into The Woods": green. "I'm God": a great pale light from above. "owari no kisetsu": falling red leaves. Every
    ///   song's own light brightens while it plays.
    public class MistLife : MonoBehaviour
    {
        const string Land = "The Ambient Mists";
        public Vector3 center, lake, boatHome;
        public string roySong, avrilSong, xtalSong, weightSong, seasonSong, shpongleSong, woodsSong, japanSong, godSong;
        public Material lamps, screens, water, towerLight; public Transform swing, boat; public Quaternion swingBase; public float played = -99f;
        public List<(Transform t, Vector3 home)> sheep = new List<(Transform, Vector3)>();
        public List<(Transform t, Vector3 home)> balloons = new List<(Transform, Vector3)>();
        public List<(string song, Material m, Color c)> songGlows = new List<(string, Material, Color)>();
        readonly Dictionary<string, float> songAmount = new Dictionary<string, float>();

        CloudLayer clouds; ParticleSystem dots;
        Color lampBase, screenBase, waterBase, towerBase;
        float roy, avril, xtal, weight, season, shpongle, woods, japan, god, nextFall, wobble;

        void Start()
        {
            clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 30; clouds.span = 2400f; clouds.key = "mists";
            clouds.minHeight = 80f; clouds.maxHeight = 150f; clouds.size = 4f; clouds.speed = .5f; clouds.cover = .55f;
            clouds.color = new Color(.86f, .74f, .66f);
            Color E(Material m) => m != null ? m.GetColor("_EmissionColor") : Color.black;
            lampBase = E(lamps); screenBase = E(screens); waterBase = E(water); towerBase = E(towerLight);
            dots = Particles("Mist, pollen and leaves", 1400, Color.white, .05f, .2f, false);
            var dm = dots.main; dm.startLifetime = new ParticleSystem.MinMaxCurve(4f, 9f);
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

            roy = Ease(roy, On(roySong), 4f); avril = Ease(avril, On(avrilSong), 5f); xtal = Ease(xtal, On(xtalSong), 4f); weight = Ease(weight, On(weightSong), 8f); season = Ease(season, On(seasonSong), 4f); shpongle = Ease(shpongle, On(shpongleSong), 3f); woods = Ease(woods, On(woodsSong), 4f); japan = Ease(japan, On(japanSong), 4f); god = Ease(god, On(godSong), 3f);

            foreach (var (s, _, _) in songGlows) if (!songAmount.ContainsKey(s)) songAmount[s] = 0f;
            var keys = new List<string>(songAmount.Keys);
            foreach (var s in keys) songAmount[s] = Mathf.MoveTowards(songAmount[s], here && id == s ? 1f : 0f, dt / 2f);
            foreach (var (s, m, c) in songGlows) if (m != null) m.SetColor("_EmissionColor", c * (1f + songAmount[s] * (2.5f + Mathf.Sin(t * 2.4f) * .6f)));

            var slow = 1f - weight * .6f;
            if (swing != null) swing.rotation = swingBase * Quaternion.Euler(Mathf.Sin(t * 1.1f * slow) * 14f, 0, 0);
            if (boat != null) { boat.position = boatHome + Vector3.up * Mathf.Sin(t * .8f) * .05f; boat.rotation = Quaternion.LookRotation(boat.forward) * Quaternion.Euler(0, 0, Mathf.Sin(t * .7f) * 2f); }
            for (var i = 0; i < sheep.Count; i++) { var (tr, home) = sheep[i]; if (tr == null) continue; var a = now * .03f * slow + i * 1.7f; var off = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a * .7f)) * 1.6f; tr.position = home + off; tr.rotation = Quaternion.LookRotation(new Vector3(Mathf.Cos(a), 0, -Mathf.Sin(a * .7f) * .7f + .01f)); }
            for (var i = 0; i < balloons.Count; i++) { var (tr, home) = balloons[i]; if (tr == null) continue; tr.position = home + new Vector3(Mathf.Sin(t * .3f * slow + i), Mathf.Sin(t * .2f + i) * .3f, Mathf.Cos(t * .25f * slow + i)) * .5f; }

            if (Random.value < dt * .03f) wobble = 1f; wobble = Mathf.MoveTowards(wobble, 0f, dt * .8f);
            var lift = t - played < 6f ? 1f - (t - played) / 6f : 0f;
            if (lamps != null) lamps.SetColor("_EmissionColor", lampBase * (1f + avril * .5f + lift * .3f));
            if (screens != null) screens.SetColor("_EmissionColor", screenBase * ((Mathf.PerlinNoise(t * 6f, 1f) > .8f ? .5f : 1f) + lift + wobble * .5f));
            if (water != null) water.SetColor("_EmissionColor", waterBase * (.9f + .1f * Mathf.Sin(t * .6f) + xtal * .6f));
            if (towerLight != null) towerLight.SetColor("_EmissionColor", towerBase * (Mathf.Repeat(t / 3f, 1f) < .25f ? 1.2f : .1f));

            var tint = new Color(1f, .8f, .64f); var amount = .12f + wobble * .1f;
            if (woods > 0f) { tint = Color.Lerp(tint, new Color(.6f, .86f, .6f), woods); amount = Mathf.Lerp(amount, .3f, woods); }
            if (japan > 0f) { tint = Color.Lerp(tint, new Color(1f, .74f, .8f), japan); amount = Mathf.Lerp(amount, .3f, japan); }
            if (avril > 0f) { tint = Color.Lerp(tint, new Color(1f, .82f, .5f), avril); amount = Mathf.Lerp(amount, .34f, avril); }
            if (season > 0f) { tint = Color.Lerp(tint, new Color(1f, .6f, .44f), season); amount = Mathf.Lerp(amount, .3f, season); }
            if (xtal > 0f) { tint = Color.Lerp(tint, new Color(.7f, .9f, 1f), xtal); amount = Mathf.Lerp(amount, .32f + Mathf.Sin(t * 2f) * .04f, xtal); }
            if (roy > 0f) { tint = Color.Lerp(tint, Color.HSVToRGB(Mathf.Repeat(now * .03f, 1f), .35f, 1f), roy); amount = Mathf.Lerp(amount, .3f, roy); }
            if (shpongle > 0f) { tint = Color.Lerp(tint, Color.HSVToRGB(Mathf.Repeat(now * .12f, 1f), .6f, 1f), shpongle); amount = Mathf.Lerp(amount, .38f, shpongle); }
            if (weight > 0f) { tint = Color.Lerp(tint, new Color(.84f, .86f, .9f), weight); amount = Mathf.Lerp(amount, .3f, weight); }
            if (god > 0f) { tint = Color.Lerp(tint, new Color(1f, .98f, .9f), god); amount = Mathf.Lerp(amount, .44f, god); }
            SkyDirector.SetSongTint(Land, tint, amount);
            var wind = clouds != null ? clouds.Wind : new Vector2(1, 0);
            if (here) { Shader.SetGlobalFloat("_EW_Wind", (.2f + .1f * energy) * slow + season * .3f); Shader.SetGlobalVector("_EW_WindDir", new Vector4(wind.x, 0, wind.y, 0)); }

            var cam = Camera.main; if (!here || cam == null) return; var cp = cam.transform.position;
            if (t >= nextFall)
            {
                nextFall = t + .05f;
                var p = cp + new Vector3(Random.Range(-20f, 20f), Random.Range(.2f, 1.6f), Random.Range(-20f, 20f));
                if (IndoorSpaces.Contains(p)) return;
                if (Random.value < .25f) dots.Emit(new ParticleSystem.EmitParams { position = p, velocity = new Vector3(wind.x * .2f, 0, wind.y * .2f), startColor = new Color(.86f, .84f, .8f, .18f), startSize = Random.Range(2f, 4f), startLifetime = 9f }, 1);
                if (Random.value < .2f) dots.Emit(new ParticleSystem.EmitParams { position = cp + new Vector3(Random.Range(-14f, 14f), Random.Range(.5f, 4f), Random.Range(-14f, 14f)), velocity = new Vector3(Random.Range(-.1f, .1f), Random.Range(-.02f, .08f), Random.Range(-.1f, .1f)), startColor = new Color(1f, .9f, .7f, .8f), startSize = .05f, startLifetime = 7f }, 1);
                if (season > .05f && Random.value < season) dots.Emit(new ParticleSystem.EmitParams { position = cp + new Vector3(Random.Range(-14f, 14f), Random.Range(5f, 9f), Random.Range(-14f, 14f)), velocity = new Vector3(wind.x * .6f, -.6f, wind.y * .6f), startColor = new Color(.9f, .4f, .3f), startSize = .14f, startLifetime = 8f }, 1);
            }
        }
    }
}
