using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Roots Bay on a bright morning after rain:
    /// - mist drifts in the low ground and burns off; big white clouds move away east; the rainbow stands over the bay;
    ///   the speaker cones of the Sound System beat slowly, like a heart, harder when anybody plays; butterflies;
    /// - Bob Marley's songs: the cones beat hard and the whole stack glows. "No Woman, No Cry": the rainbow blazes and
    ///   the morning turns gold. "Ghost Town": the colour drains, the mist comes down thick. "Pressure Drop": the rain
    ///   comes back for a while. "Chase the Devil": red flickers in the mist. "Night Nurse": the light goes dusk-blue.
    ///   "Legalize It": a green haze. "Israelites": a hard gold sunrise. Every song's own light brightens while it plays.
    public class RootsLife : MonoBehaviour
    {
        const string Land = "The Roots Bay";
        public Vector3 center;
        public string cryString, ghostSong, pressureSong, devilSong, nurseSong, legalizeSong, israelitesSong;
        public HashSet<string> bob = new HashSet<string>();
        public Material cones, fire; public Material[] rainbow; public float played = -99f;
        public List<Vector3> sparks = new List<Vector3>();
        public List<(string song, Material m, Color c)> songGlows = new List<(string, Material, Color)>();
        readonly Dictionary<string, float> songAmount = new Dictionary<string, float>();

        CloudLayer clouds; ParticleSystem mist, dots;
        Color coneBase, fireBase; Color[] bowBase;
        float wailers, cry, ghost, pressure, devil, nurse, legal, israel, nextMist, nextFly, nextSpark;
        static readonly Color[] Wings = { new Color(1f, .8f, .2f), new Color(.95f, .3f, .2f), new Color(.3f, .8f, .4f), Color.white };

        void Start()
        {
            clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 46; clouds.span = 2600f; clouds.key = "rootsbay";
            clouds.minHeight = 150f; clouds.maxHeight = 250f; clouds.size = 3.6f; clouds.speed = 3.4f; clouds.cover = .4f;
            clouds.color = new Color(1f, 1f, .98f);
            Color E(Material m) => m != null ? m.GetColor("_EmissionColor") : Color.black;
            coneBase = E(cones); fireBase = E(fire); bowBase = new Color[rainbow != null ? rainbow.Length : 0]; for (var i = 0; i < bowBase.Length; i++) bowBase[i] = E(rainbow[i]);
            mist = Particles("Morning mist", 500, new Color(1f, 1f, 1f, .1f), 3f, 6f, false);
            var mm = mist.main; mm.startLifetime = new ParticleSystem.MinMaxCurve(6f, 10f);
            dots = Particles("Butterflies and sparks", 300, Color.white, .06f, .12f, false);
            var dm = dots.main; dm.startLifetime = new ParticleSystem.MinMaxCurve(3f, 6f);
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
            var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, .3f), new GradientAlphaKey(0f, 1f) });
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

            wailers = Mathf.MoveTowards(wailers, here && id != null && bob.Contains(id) ? 1f : 0f, dt / 2f);
            cry = Mathf.MoveTowards(cry, On(cryString) ? 1f : 0f, dt / 5f);
            ghost = Mathf.MoveTowards(ghost, On(ghostSong) ? 1f : 0f, dt / 5f);
            pressure = Mathf.MoveTowards(pressure, On(pressureSong) ? 1f : 0f, dt / 5f);
            devil = Mathf.MoveTowards(devil, On(devilSong) ? 1f : 0f, dt / 3f);
            nurse = Mathf.MoveTowards(nurse, On(nurseSong) ? 1f : 0f, dt / 5f);
            legal = Mathf.MoveTowards(legal, On(legalizeSong) ? 1f : 0f, dt / 4f);
            israel = Mathf.MoveTowards(israel, On(israelitesSong) ? 1f : 0f, dt / 4f);

            foreach (var (s, _, _) in songGlows) if (!songAmount.ContainsKey(s)) songAmount[s] = 0f;
            var keys = new List<string>(songAmount.Keys);
            foreach (var s in keys) songAmount[s] = Mathf.MoveTowards(songAmount[s], here && id == s ? 1f : 0f, dt / 2f);
            foreach (var (s, m, c) in songGlows) if (m != null) m.SetColor("_EmissionColor", c * (1f + songAmount[s] * (2.5f + Mathf.Sin(t * 2.2f) * .6f)));

            // the Sound System's heartbeat: a slow one-drop, harder for the Wailers and when anybody plays
            var lift = t - played < 4f ? 1f - (t - played) / 4f : 0f; var beat = Mathf.Repeat(now * 1.25f, 1f); var thump = beat < .16f ? 1f - beat / .16f : 0f;
            if (cones != null) cones.SetColor("_EmissionColor", coneBase * (.6f + thump * (.8f + wailers * 2.4f + lift * 1.4f + energy * .5f)));
            if (fire != null) fire.SetColor("_EmissionColor", fireBase * (.8f + .35f * Mathf.PerlinNoise(t * 5f, 2.7f) + cry * .8f));
            if (rainbow != null) for (var i = 0; i < rainbow.Length; i++) if (rainbow[i] != null) rainbow[i].SetColor("_EmissionColor", bowBase[i] * Mathf.Max(.1f, 1f + .08f * Mathf.Sin(t * .6f + i) + cry * 1.6f + israel * .6f - ghost * .85f - pressure * .7f));

            SkyDirector.SetSongRain(Land, pressure * .5f);
            var tint = new Color(1f, .98f, .9f); var amount = .03f;
            if (israel > 0f) { tint = Color.Lerp(tint, new Color(1f, .8f, .4f), israel); amount = Mathf.Lerp(amount, .3f, israel); }
            if (cry > 0f) { tint = Color.Lerp(tint, new Color(1f, .86f, .55f), cry); amount = Mathf.Lerp(amount, .3f, cry); }
            if (legal > 0f) { tint = Color.Lerp(tint, new Color(.6f, 1f, .6f), legal); amount = Mathf.Lerp(amount, .25f, legal); }
            if (pressure > 0f) { tint = Color.Lerp(tint, new Color(.6f, .68f, .76f), pressure); amount = Mathf.Lerp(amount, .4f, pressure); }
            if (nurse > 0f) { tint = Color.Lerp(tint, new Color(.3f, .4f, .8f), nurse); amount = Mathf.Lerp(amount, .55f, nurse); }
            if (ghost > 0f) { tint = Color.Lerp(tint, new Color(.56f, .57f, .6f), ghost); amount = Mathf.Lerp(amount, .62f, ghost); }
            if (devil > 0f) { tint = Color.Lerp(tint, new Color(1f, .25f, .15f), devil); amount = Mathf.Lerp(amount, .2f + .14f * Mathf.PerlinNoise(t * 6f, 1f), devil); }
            SkyDirector.SetSongTint(Land, tint, amount);
            var wind = clouds != null ? clouds.Wind : new Vector2(1, 0);
            if (here) { Shader.SetGlobalFloat("_EW_Wind", .3f + .15f * energy + pressure * .3f); Shader.SetGlobalVector("_EW_WindDir", new Vector4(wind.x, 0, wind.y, 0)); }

            var cam = Camera.main; if (!here || cam == null) return; var cp = cam.transform.position;
            if (t >= nextMist)
            {
                nextMist = t + Mathf.Lerp(.16f, .03f, ghost); var p = cp + new Vector3(Random.Range(-34f, 34f), Random.Range(-1.5f, 1.5f), Random.Range(-34f, 34f));
                if (!IndoorSpaces.Contains(p)) mist.Emit(new ParticleSystem.EmitParams { position = p, velocity = new Vector3(wind.x * .4f, .12f, wind.y * .4f), startColor = devil > 0f ? Color.Lerp(new Color(1f, 1f, 1f, .1f), new Color(1f, .4f, .3f, .12f), devil) : new Color(1f, 1f, 1f, .1f + ghost * .1f) }, 1);
            }
            if (t >= nextFly && pressure < .5f && ghost < .5f)
            {
                nextFly = t + .4f; var p = cp + new Vector3(Random.Range(-12f, 12f), Random.Range(-.6f, 2.4f), Random.Range(-12f, 12f));
                if (!IndoorSpaces.Contains(p)) dots.Emit(new ParticleSystem.EmitParams { position = p, velocity = new Vector3(Random.Range(-.6f, .6f), Random.Range(-.1f, .3f), Random.Range(-.6f, .6f)), startColor = Wings[Random.Range(0, Wings.Length)] }, 1);
            }
            if (t >= nextSpark)
            {
                nextSpark = t + .12f;
                foreach (var p in sparks) if ((p - cp).sqrMagnitude < 70f * 70f) dots.Emit(new ParticleSystem.EmitParams { position = p + new Vector3(Random.Range(-.3f, .3f), 0, Random.Range(-.3f, .3f)), velocity = new Vector3(wind.x * .3f, Random.Range(.8f, 1.8f), wind.y * .3f), startColor = new Color(1f, .6f, .2f), startSize = .06f, startLifetime = 2f }, 1);
            }
        }
    }
}
