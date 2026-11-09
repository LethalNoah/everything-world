using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

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
        /// Lands can wash their light with a colour for the current song (the hub's sunset follows each song's
        /// mood; the Indie Downs clouds over for quiet songs). Each land asks for its own; only the land you're
        /// on shows, blending in and out as you cross.
        static readonly Dictionary<string, (Color color, float amount)> songTints = new Dictionary<string, (Color, float)>();
        public static void SetSongTint(string land, Color color, float amount) { if (land != null) songTints[land] = (color, amount); }
        Color tintNow = Color.white; float tintAmountNow;
        /// A land can also bring a passing shower for a song (the Indie Downs' summer showers).
        static readonly Dictionary<string, float> songRain = new Dictionary<string, float>();
        public static void SetSongRain(string land, float amount) { if (land != null) songRain[land] = amount; }
        float SongRain() => currentLand != null && songRain.TryGetValue(currentLand, out var r) ? r : 0f;
        Volume bloomVolume; Bloom bloomFx; float bloomNow;

        struct State
        {
            public Quaternion sunRot; public Color sun; public float sunI;
            public Color sky, horizon, ground, fog; public float fogStart, fogEnd;
            public float snow, rain, stars, aurora, auroraChance;
            public bool sunShadows;
            public float bloom;
        }

        LandMoodSet set;
        readonly Dictionary<string, LandMood> byLand = new Dictionary<string, LandMood>();
        State current, target;
        ParticleSystem snow, rain;
        GameObject starDome, auroraObject;
        Material starMaterial, lineMaterial, auroraMaterial;
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

        /// The land whose sky is showing, and how starry it is right now (for the surprises).
        public string CurrentLand => currentLand;

        bool testAurora;
        /// F8 in play: show or hide the aurora on any land, for testing.
        public void ToggleTestAurora() => testAurora = !testAurora;
        public float CurrentStars => current.stars;

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
                fogStart = m.fogStart, fogEnd = m.fogEnd, stars = m.stars, sunShadows = m.sunShadows,
                aurora = m.aurora, auroraChance = m.auroraChance, bloom = m.bloom,
            };
            var amount = m.weatherAmount;
            if (m.dailyWeather)
            {
                // some days heavy weather and few stars, most days clear with only a little: the same for everyone
                var day = (int)(System.DateTime.UtcNow - new System.DateTime(2026, 1, 1)).TotalDays;
                var landHash = 0; foreach (var ch in m.land ?? "") landHash = landHash * 31 + ch; // same on every computer
                var stormy = (Mathf.Abs((day * 73856093) ^ landHash) % 3) == 0;
                amount = stormy ? Mathf.Min(1f, amount * 1.8f) : amount * .2f;
                s.stars = stormy ? m.stars * .15f : m.stars;
            }
            if (m.weather == "snow") s.snow = amount;
            if (m.weather == "rain") s.rain = amount;
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
            current.rain = Mathf.Lerp(current.rain, Mathf.Max(target.rain, SongRain()), k);
            bloomNow = Mathf.MoveTowards(bloomNow, target.bloom, Time.deltaTime / 2f);
            current.stars = Mathf.Lerp(current.stars, target.stars, k);
            // some songs bring the aurora (the same songs for everyone on the station)
            var auroraGoal = target.aurora;
            if (target.auroraChance > 0 && SharedChance.CurrentSong(out var song, out _, out _) &&
                SharedChance.Roll(SharedChance.Key("aurora:" + currentLand), song, target.auroraChance)) auroraGoal = 1f;
            current.aurora = Mathf.Lerp(current.aurora, auroraGoal, Time.deltaTime / 4f);
            var tintGoal = currentLand != null && songTints.TryGetValue(currentLand, out var tg) ? tg : (Color.white, 0f);
            tintNow = Color.Lerp(tintNow, tintGoal.Item1, Time.deltaTime / 3f);
            tintAmountNow = Mathf.MoveTowards(tintAmountNow, tintGoal.Item2, Time.deltaTime / 4f);
            Apply();
        }

        void Apply()
        {
            if (sun != null)
            {
                sun.shadows = target.sunShadows ? LightShadows.Soft : LightShadows.None;
                sun.transform.rotation = current.sunRot;
                sun.color = Color.Lerp(current.sun, tintNow, tintAmountNow);
                sun.intensity = current.sunI;
            }
            RenderSettings.ambientSkyColor = current.sky;
            RenderSettings.ambientEquatorColor = Color.Lerp(current.horizon, tintNow, tintAmountNow * .5f);
            RenderSettings.ambientGroundColor = current.ground;
            RenderSettings.fogColor = Color.Lerp(current.fog, tintNow, tintAmountNow * .35f);
            RenderSettings.fogStartDistance = current.fogStart;
            RenderSettings.fogEndDistance = current.fogEnd;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = RenderSettings.fogColor;
                Bloom_();
                snow ??= MakeWeather("Snow", false);
                rain ??= MakeWeather("Rain", true);
                // the weather boxes follow the camera but stay level
                var above = cam.transform.position + Vector3.up * 14f + Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up) * 10f;
                snow.transform.position = above; rain.transform.position = above;
                SetRate(snow, current.snow * 900f);
                SetRate(rain, current.rain * 2500f);
                var indoors = IndoorSpaces.Contains(cam.transform.position);
                snow.GetComponent<ParticleSystemRenderer>().enabled = !indoors;
                rain.GetComponent<ParticleSystemRenderer>().enabled = !indoors;
                starDome ??= MakeStars();
                if (starDome != null)
                {
                    starDome.transform.position = cam.transform.position;
                    starMaterial.SetFloat("_Strength", current.stars);
                    if (lineMaterial != null) lineMaterial.SetFloat("_Strength", current.stars);
                    starDome.SetActive(current.stars > .01f);
                }
                auroraObject ??= MakeAurora();
                if (auroraObject != null)
                {
                    auroraObject.transform.position = cam.transform.position;
                    auroraObject.transform.rotation = Quaternion.Euler(0, (float)SharedChance.Value(SharedChance.Key("aurora-dir"), SharedChance.Hour) * 360f, 0);
                    var aurora = testAurora ? 1f : current.aurora;
                    auroraMaterial.SetFloat("_Strength", aurora);
                    auroraObject.SetActive(aurora > .01f);
                }
            }
        }

        /// Bloom for lands that ask for it. Post-processing is only switched on while a land's bloom is showing,
        /// so lands without it look exactly as they always have.
        void Bloom_()
        {
            if (bloomVolume == null)
            {
                var go = new GameObject("Land bloom");
                go.transform.SetParent(transform, false);
                bloomVolume = go.AddComponent<Volume>();
                bloomVolume.isGlobal = true; bloomVolume.priority = 50;
                var profile = ScriptableObject.CreateInstance<VolumeProfile>();
                bloomFx = profile.Add<Bloom>(true);
                bloomFx.threshold.Override(1f);
                bloomFx.scatter.Override(.7f);
                bloomFx.intensity.Override(0f);
                bloomVolume.profile = profile;
            }
            bloomFx.intensity.Override(bloomNow * 1.4f);
            var data = cam.GetComponent<UniversalAdditionalCameraData>();
            if (data != null) data.renderPostProcessing = bloomNow > .01f;
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

        /// The northern lights: three tall curtains in a full ring round the camera, at different distances,
        /// with wavy upper and lower edges.
        GameObject MakeAurora()
        {
            var shader = Shader.Find("Everything World/Aurora");
            if (shader == null) return null;
            var v = new System.Collections.Generic.List<Vector3>(); var uv = new System.Collections.Generic.List<Vector2>(); var t = new System.Collections.Generic.List<int>();
            var bands = new[] { (1250f, 220f, 560f, 0f), (1500f, 280f, 700f, 2.1f), (1750f, 330f, 820f, 4.3f) };
            foreach (var (radius, bottom, top, phase) in bands)
            {
                const int n = 260;
                var along = phase * 3000f; Vector3 prev = default;
                for (var i = 0; i <= n; i++)
                {
                    var a = i * Mathf.PI * 2f / n;
                    var r = radius * (1f + .14f * Mathf.Sin(i * .09f + phase) + .07f * Mathf.Sin(i * .31f + phase * 2f));
                    var p = new Vector3(Mathf.Sin(a) * r, 0, Mathf.Cos(a) * r);
                    if (i > 0) along += Vector3.Distance(p, prev);
                    prev = p;
                    var lo = bottom + 70f * Mathf.Sin(i * .13f + phase) + 35f * Mathf.Sin(i * .41f + phase * 3f);
                    var hi = top + 120f * Mathf.Sin(i * .07f + phase * 1.7f) + 50f * Mathf.Sin(i * .23f + phase);
                    v.Add(p + Vector3.up * lo); v.Add(p + Vector3.up * hi);
                    uv.Add(new Vector2(along, 0)); uv.Add(new Vector2(along, 1));
                    if (i > 0) { var k = v.Count - 4; t.Add(k); t.Add(k + 1); t.Add(k + 3); t.Add(k); t.Add(k + 3); t.Add(k + 2); }
                }
            }
            var mesh = new Mesh { name = "Aurora", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(v); mesh.SetUVs(0, uv); mesh.SetTriangles(t, 0);
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 4500f);
            var go = new GameObject("Aurora");
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r2 = go.AddComponent<MeshRenderer>();
            r2.sharedMaterial = auroraMaterial = new Material(shader) { name = "Aurora" };
            r2.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        /// About 2,500 stars on a dome round the camera, a few bright, most faint, some in small clusters.
        GameObject MakeStars()
        {
            var shader = Shader.Find("Everything World/Stars");
            if (shader == null) return null;
            var rng = new System.Random(2026);
            var v = new System.Collections.Generic.List<Vector3>(); var c = new System.Collections.Generic.List<Color>();
            var uv = new System.Collections.Generic.List<Vector2>(); var data = new System.Collections.Generic.List<Vector2>();
            var t = new System.Collections.Generic.List<int>();
            const float radius = 1800f;
            void Star(Vector3 dir, float size, float bright)
            {
                var i = v.Count;
                var tint = Color.Lerp(new Color(.75f, .82f, 1f), new Color(1f, .92f, .8f), (float)rng.NextDouble());
                tint.a = bright;
                var phase = (float)rng.NextDouble();
                foreach (var k in new[] { new Vector2(-1, -1), new Vector2(1, -1), new Vector2(1, 1), new Vector2(-1, 1) })
                { v.Add(dir * radius); c.Add(tint); uv.Add(k); data.Add(new Vector2(size, phase)); }
                t.Add(i); t.Add(i + 1); t.Add(i + 2); t.Add(i); t.Add(i + 2); t.Add(i + 3);
            }
            Vector3 RandomUp()
            {
                var y = .05f + (float)rng.NextDouble() * .95f; var a = (float)rng.NextDouble() * Mathf.PI * 2f; var r = Mathf.Sqrt(1 - y * y);
                return new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r);
            }
            for (var i = 0; i < 2300; i++)
            {
                var b = Mathf.Pow((float)rng.NextDouble(), 3f);   // mostly faint
                Star(RandomUp(), 1.4f + b * 3.2f, .3f + b * 1.5f);
            }
            // constellations: small groups of brighter stars, joined by faint lines
            var lv = new System.Collections.Generic.List<Vector3>(); var luv = new System.Collections.Generic.List<Vector2>(); var lt = new System.Collections.Generic.List<int>();
            for (var g = 0; g < 22; g++)
            {
                var center = RandomUp();
                if (center.y < .3f) center = (center + Vector3.up * .5f).normalized;
                var pts = new System.Collections.Generic.List<Vector3>();
                var links = new System.Collections.Generic.List<(int, int)>();
                var count = 6 + rng.Next(5);
                var walk = center;
                // a wandering chain that reaches well across the sky, with the odd branch off an earlier star
                for (var i = 0; i < count; i++)
                {
                    var from = i > 2 && rng.NextDouble() < .3 ? rng.Next(i) : i - 1;
                    var origin = from >= 0 ? pts[from] : walk;
                    var stepLen = .1f + (float)rng.NextDouble() * .16f;
                    var dir = new Vector3((float)rng.NextDouble() - .5f, ((float)rng.NextDouble() - .5f) * .7f, (float)rng.NextDouble() - .5f).normalized;
                    walk = (origin + dir * stepLen).normalized;
                    if (walk.y < .12f) walk = (walk + Vector3.up * .2f).normalized;
                    pts.Add(walk);
                    if (from >= 0) links.Add((from, i));
                    Star(walk, 3.2f + (float)rng.NextDouble() * 1.4f, 1.5f + (float)rng.NextDouble() * .5f);
                }
                foreach (var (ia, ib) in links)
                {
                    Vector3 a = pts[ia] * radius * .999f, b = pts[ib] * radius * .999f;
                    var across = Vector3.Cross((b - a).normalized, (a + b).normalized) * 1.1f;  // 2 m wide strip on the dome
                    var k = lv.Count;
                    lv.Add(a - across); lv.Add(a + across); lv.Add(b + across); lv.Add(b - across);
                    luv.Add(new Vector2(0, -1)); luv.Add(new Vector2(0, 1)); luv.Add(new Vector2(1, 1)); luv.Add(new Vector2(1, -1));
                    lt.Add(k); lt.Add(k + 1); lt.Add(k + 2); lt.Add(k); lt.Add(k + 2); lt.Add(k + 3);
                }
            }
            var mesh = new Mesh { name = "Stars", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(v); mesh.SetColors(c); mesh.SetUVs(0, uv); mesh.SetUVs(1, data); mesh.SetTriangles(t, 0);
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * radius * 2.2f);
            var go = new GameObject("Stars");
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r2 = go.AddComponent<MeshRenderer>();
            r2.sharedMaterial = starMaterial = new Material(shader) { name = "Stars" };
            r2.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var lineShader = Shader.Find("Everything World/Star Lines");
            if (lineShader != null && lv.Count > 0)
            {
                var lm = new Mesh { name = "Constellation lines" };
                lm.SetVertices(lv); lm.SetUVs(0, luv); lm.SetTriangles(lt, 0);
                lm.bounds = new Bounds(Vector3.zero, Vector3.one * radius * 2.2f);
                var lines = new GameObject("Constellation lines");
                lines.transform.SetParent(go.transform, false);
                lines.AddComponent<MeshFilter>().sharedMesh = lm;
                var lr = lines.AddComponent<MeshRenderer>();
                lr.sharedMaterial = lineMaterial = new Material(lineShader) { name = "Constellation lines" };
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            return go;
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
