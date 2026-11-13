using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Latin Coast at sunset:
    /// - the lanterns glow; the lighthouse beam sweeps round; jacaranda petals drift down in the plaza; pigeons burst up
    ///   from the bell tower whenever anybody rings it; confetti when anybody plays;
    /// - "Hips Don't Lie": a hot golden light. "Whenever, Wherever": the wind gets up. "Danza Kuduro" and "La Bamba":
    ///   confetti and party colours. "Macarena": a bright pink flash on every beat. "Havana": a hot amber haze. "Mar De
    ///   Soledad": a lonely blue dusk. "Tití Me Preguntó": pink. "Renacer": a new dawn. "Ella Baila Sola": everything
    ///   dims but a warm spot. "La Cucaracha": marigold. Every song's own light brightens while it plays.
    public class LatinLife : MonoBehaviour
    {
        const string Land = "The Latin Coast";
        public Vector3 center, bellAt, beam;
        public string hipsSong, wheneverSong, kuduroSong, bambaSong, macarenaSong, havanaSong, soledadSong, titiSong, renacerSong, solaSong, cucaSong;
        public Material lamps, windows, sea; public float played = -99f, rang = -99f;
        public List<(Vector3 p, Quaternion r)> boats = new List<(Vector3, Quaternion)>();
        public List<(string song, Material m, Color c)> songGlows = new List<(string, Material, Color)>();
        readonly Dictionary<string, float> songAmount = new Dictionary<string, float>();

        CloudLayer clouds; ParticleSystem dots; Light beamLight;
        Color lampBase, windowBase, seaBase;
        float hips, whenever, party, maca, havana, soledad, titi, renacer, sola, cuca, nextFall, lastRang = -99f;
        static readonly Color[] Confetti = { new Color(.92f, .2f, .5f), new Color(1f, .62f, .1f), new Color(.1f, .7f, .7f), new Color(.56f, .82f, .2f), new Color(.16f, .3f, .76f) };

        void Start()
        {
            clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 34; clouds.span = 2600f; clouds.key = "latincoast";
            clouds.minHeight = 170f; clouds.maxHeight = 250f; clouds.size = 3.6f; clouds.speed = 1.8f; clouds.cover = .34f;
            clouds.color = new Color(1f, .7f, .56f);
            Color E(Material m) => m != null ? m.GetColor("_EmissionColor") : Color.black;
            lampBase = E(lamps); windowBase = E(windows); seaBase = E(sea);
            dots = Particles("Petals, pigeons and confetti", 900, Color.white, .08f, .14f, false);
            var dm = dots.main; dm.startLifetime = new ParticleSystem.MinMaxCurve(4f, 8f);
            if (beam != Vector3.zero) { var go = new GameObject("Lighthouse beam"); go.transform.SetParent(transform, false); go.transform.position = beam; beamLight = go.AddComponent<Light>(); beamLight.type = LightType.Spot; beamLight.range = 120f; beamLight.spotAngle = 14f; beamLight.intensity = 6f; beamLight.color = new Color(1f, .92f, .7f); }
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

            hips = Ease(hips, On(hipsSong), 4f); whenever = Ease(whenever, On(wheneverSong), 4f); party = Ease(party, On(kuduroSong) || On(bambaSong), 2f); maca = Ease(maca, On(macarenaSong), 2f); havana = Ease(havana, On(havanaSong), 5f); soledad = Ease(soledad, On(soledadSong), 6f); titi = Ease(titi, On(titiSong), 4f); renacer = Ease(renacer, On(renacerSong), 6f); sola = Ease(sola, On(solaSong), 5f); cuca = Ease(cuca, On(cucaSong), 3f);

            foreach (var (s, _, _) in songGlows) if (!songAmount.ContainsKey(s)) songAmount[s] = 0f;
            var keys = new List<string>(songAmount.Keys);
            foreach (var s in keys) songAmount[s] = Mathf.MoveTowards(songAmount[s], here && id == s ? 1f : 0f, dt / 2f);
            foreach (var (s, m, c) in songGlows) if (m != null) m.SetColor("_EmissionColor", c * (1f + songAmount[s] * (2.5f + Mathf.Sin(t * 2.4f) * .6f)));

            var lift = t - played < 5f ? 1f - (t - played) / 5f : 0f; var beat = Mathf.Repeat(now * 2f, 1f);
            if (lamps != null) lamps.SetColor("_EmissionColor", lampBase * (.95f + .05f * Mathf.Sin(t * .7f) + soledad * .4f + lift * .4f) * (1f - sola * .6f));
            if (windows != null) windows.SetColor("_EmissionColor", windowBase * (1f + soledad * .5f) * (1f - sola * .5f));
            if (sea != null) sea.SetColor("_EmissionColor", seaBase * (.8f + .3f * Mathf.PerlinNoise(t * .8f, 1.1f)));
            if (beamLight != null) beamLight.transform.rotation = Quaternion.Euler(8f, t * 24f, 0f);

            var tint = new Color(1f, .86f, .72f); var amount = .05f;
            if (cuca > 0f) { tint = Color.Lerp(tint, new Color(1f, .7f, .2f), cuca); amount = Mathf.Lerp(amount, .3f, cuca); }
            if (hips > 0f) { tint = Color.Lerp(tint, new Color(1f, .78f, .3f), hips); amount = Mathf.Lerp(amount, .38f, hips); }
            if (havana > 0f) { tint = Color.Lerp(tint, new Color(1f, .62f, .3f), havana); amount = Mathf.Lerp(amount, .42f, havana); }
            if (titi > 0f) { tint = Color.Lerp(tint, new Color(1f, .5f, .74f), titi); amount = Mathf.Lerp(amount, .34f, titi); }
            if (party > 0f) { tint = Color.Lerp(tint, Confetti[Mathf.FloorToInt(now * 2f) % Confetti.Length], party); amount = Mathf.Lerp(amount, .26f, party); }
            if (maca > 0f) { tint = Color.Lerp(tint, new Color(1f, .4f, .7f), maca); amount = Mathf.Lerp(amount, beat < .15f ? .4f : .14f, maca); }
            if (renacer > 0f) { tint = Color.Lerp(tint, new Color(1f, .82f, .7f), renacer); amount = Mathf.Lerp(amount, .3f, renacer); }
            if (soledad > 0f) { tint = Color.Lerp(tint, new Color(.3f, .4f, .7f), soledad); amount = Mathf.Lerp(amount, .5f, soledad); }
            if (sola > 0f) { tint = Color.Lerp(tint, new Color(.18f, .12f, .14f), sola); amount = Mathf.Lerp(amount, .55f, sola); }
            SkyDirector.SetSongTint(Land, tint, amount);
            var wind = clouds != null ? clouds.Wind : new Vector2(1, 0);
            if (here) { Shader.SetGlobalFloat("_EW_Wind", .3f + .2f * energy + whenever * .8f); Shader.SetGlobalVector("_EW_WindDir", new Vector4(wind.x, 0, wind.y, 0)); }

            var cam = Camera.main; if (!here || cam == null) return; var cp = cam.transform.position;
            // pigeons burst up from the bell tower
            if (rang > lastRang && t - rang < .2f && (bellAt - cp).sqrMagnitude < 120f * 120f) { lastRang = rang; for (var k = 0; k < 40; k++) dots.Emit(new ParticleSystem.EmitParams { position = bellAt + Random.insideUnitSphere * 1.4f, velocity = new Vector3(Random.Range(-4f, 4f), Random.Range(3f, 6f), Random.Range(-4f, 4f)), startColor = new Color(.7f, .7f, .74f), startSize = .3f, startLifetime = 4f }, 1); }
            if (t >= nextFall)
            {
                nextFall = t + .06f; var p = cp + new Vector3(Random.Range(-16f, 16f), Random.Range(3f, 8f), Random.Range(-16f, 16f));
                if (IndoorSpaces.Contains(p)) return;
                if ((center - cp).sqrMagnitude < 40f * 40f && Random.value < .3f) dots.Emit(new ParticleSystem.EmitParams { position = center + new Vector3(Random.Range(-6f, 6f), Random.Range(5f, 8f), Random.Range(-6f, 6f)), velocity = new Vector3(wind.x * .4f, -.5f, wind.y * .4f), startColor = new Color(.66f, .5f, .96f), startSize = .1f, startLifetime = 9f }, 1);
                var conf = Mathf.Max(party, lift * .7f); if (Random.value < conf) for (var k = 0; k < 2; k++) dots.Emit(new ParticleSystem.EmitParams { position = cp + new Vector3(Random.Range(-14f, 14f), Random.Range(4f, 8f), Random.Range(-14f, 14f)), velocity = new Vector3(Random.Range(-.5f, .5f), -1f, Random.Range(-.5f, .5f)), startColor = Confetti[Random.Range(0, Confetti.Length)], startSize = .12f }, 1);
            }
        }
    }
}
