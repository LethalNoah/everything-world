using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace EverythingWorld.World
{
    /// Sky per land: when the player crosses onto another land, the sun, ambient light, fog and weather
    /// blend to that land's mood over a few seconds, like the music switching stations.
    /// The IslandExplorer tells it which land the player is on.
    public class SkyDirector : MonoBehaviour
    {
        public static SkyDirector Instance { get; private set; }

        [Tooltip("Assets/_Project/Data/land_moods.json")]
        public TextAsset moodsJson;
        [Tooltip("Seconds to blend most of the way to a new land's sky.")]
        public float blendSeconds = 3f;
        public Light sun;

        struct State
        {
            public Quaternion sunRot; public Color sun; public float sunI;
            public Color sky, horizon, ground, fog; public float fogStart, fogEnd;
            public float snow, rain;
        }

        LandMoodSet set;
        readonly Dictionary<string, LandMood> byLand = new Dictionary<string, LandMood>();
        State current, target;
        ParticleSystem snow, rain;
        Camera cam;
        string currentLand;

        void Awake()
        {
            Instance = this;
            set = moodsJson != null ? JsonUtility.FromJson<LandMoodSet>(moodsJson.text) : new LandMoodSet();
            set.fallback ??= new LandMood();
            foreach (var m in set.lands ?? new LandMood[0])
                if (!string.IsNullOrEmpty(m.land)) byLand[m.land] = m;
            if (sun == null)
                foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                    if (l.type == LightType.Directional) { sun = l; break; }
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            current = target = From(set.fallback);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// The player is now on this land (null or unknown names use the fallback mood).
        public void SetLand(string landName)
        {
            if (landName == currentLand) return;
            currentLand = landName;
            target = From(landName != null && byLand.TryGetValue(landName, out var m) ? m : set.fallback);
        }

        /// Jump straight to the target sky (after fast travel, for example).
        public void Snap() => current = target;

        static State From(LandMood m)
        {
            var s = new State
            {
                sunRot = Quaternion.Euler(m.sunPitch, m.sunYaw, 0f),
                sun = LandMood.Hex(m.sunColor, Color.white), sunI = m.sunIntensity,
                sky = LandMood.Hex(m.skyLight, Color.grey), horizon = LandMood.Hex(m.horizonLight, Color.grey),
                ground = LandMood.Hex(m.groundLight, Color.grey), fog = LandMood.Hex(m.fogColor, Color.grey),
                fogStart = m.fogStart, fogEnd = m.fogEnd,
            };
            if (m.weather == "snow") s.snow = m.weatherAmount;
            if (m.weather == "rain") s.rain = m.weatherAmount;
            return s;
        }

        void LateUpdate()
        {
            if (cam == null) cam = Camera.main;
            var k = 1f - Mathf.Exp(-Time.deltaTime * 3f / Mathf.Max(.1f, blendSeconds));
            current.sunRot = Quaternion.Slerp(current.sunRot, target.sunRot, k);
            current.sun = Color.Lerp(current.sun, target.sun, k);
            current.sunI = Mathf.Lerp(current.sunI, target.sunI, k);
            current.sky = Color.Lerp(current.sky, target.sky, k);
            current.horizon = Color.Lerp(current.horizon, target.horizon, k);
            current.ground = Color.Lerp(current.ground, target.ground, k);
            current.fog = Color.Lerp(current.fog, target.fog, k);
            current.fogStart = Mathf.Lerp(current.fogStart, target.fogStart, k);
            current.fogEnd = Mathf.Lerp(current.fogEnd, target.fogEnd, k);
            current.snow = Mathf.Lerp(current.snow, target.snow, k);
            current.rain = Mathf.Lerp(current.rain, target.rain, k);
            Apply();
        }

        void Apply()
        {
            if (sun != null)
            {
                sun.transform.rotation = current.sunRot;
                sun.color = current.sun;
                sun.intensity = current.sunI;
            }
            RenderSettings.ambientSkyColor = current.sky;
            RenderSettings.ambientEquatorColor = current.horizon;
            RenderSettings.ambientGroundColor = current.ground;
            RenderSettings.fogColor = current.fog;
            RenderSettings.fogStartDistance = current.fogStart;
            RenderSettings.fogEndDistance = current.fogEnd;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = current.fog;
                snow ??= MakeWeather("Snow", false);
                rain ??= MakeWeather("Rain", true);
                // the weather boxes follow the camera but stay level
                var above = cam.transform.position + Vector3.up * 14f + Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up) * 10f;
                snow.transform.position = above; rain.transform.position = above;
                SetRate(snow, current.snow * 900f);
                SetRate(rain, current.rain * 2500f);
            }
        }

        /// A soft round dot, see-through at the edges.
        static Material FlakeMaterial(string name)
        {
            const int n = 32;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "Soft dot", wrapMode = TextureWrapMode.Clamp };
            for (var y = 0; y < n; y++)
            for (var x = 0; x < n; x++)
            {
                var d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(1f - d * d) * Mathf.Clamp01((1f - d) * 3f)));
            }
            tex.Apply();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = name + " particles" };
            mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Surface", 1f);                         // transparent
            mat.SetFloat("_Blend", 0f);                           // alpha blend
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            return mat;
        }

        static void SetRate(ParticleSystem ps, float rate)
        {
            var e = ps.emission;
            e.rateOverTime = rate < 1f ? 0f : rate;
        }

        /// Snowflakes or raindrops falling in a box around the camera.
        ParticleSystem MakeWeather(string name, bool isRain)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = isRain ? 1.4f : 9f;
            main.startSpeed = 0f;
            main.startSize = isRain ? new ParticleSystem.MinMaxCurve(.03f, .05f) : new ParticleSystem.MinMaxCurve(.06f, .16f);
            main.startColor = isRain ? new Color(.75f, .8f, .9f, .55f) : new Color(1f, 1f, 1f, .9f);
            main.maxParticles = isRain ? 6000 : 9000;
            main.gravityModifier = isRain ? 1.6f : .03f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(70, 1, 70);
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = isRain ? new ParticleSystem.MinMaxCurve(0f, 0f) : new ParticleSystem.MinMaxCurve(-.4f, .4f);
            vel.y = isRain ? new ParticleSystem.MinMaxCurve(-14f, -12f) : new ParticleSystem.MinMaxCurve(-1.6f, -.9f);
            vel.z = isRain ? new ParticleSystem.MinMaxCurve(0f, 0f) : new ParticleSystem.MinMaxCurve(-.4f, .4f);
            var em = ps.emission;
            em.rateOverTime = 0f;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = isRain ? ParticleSystemRenderMode.Stretch : ParticleSystemRenderMode.Billboard;
            if (isRain) r.lengthScale = 6f;
            // a flake right in front of the camera must never fill the screen
            r.maxParticleSize = isRain ? .02f : .012f;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.sharedMaterial = FlakeMaterial(name);
            ps.Play();
            return ps;
        }
    }
}
