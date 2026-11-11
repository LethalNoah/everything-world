using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// Lanternwood, hushed and a little magic:
    /// - snow always drifting down round you, a few flakes catching the light and glinting, more in sad songs;
    /// - wisps of pale light wandering low between the birches;
    /// - curtains of aurora over the frozen lake, teal and violet, swaying slowly: in many of the hushed songs, and
    ///   now and then in others (the same for everyone, from the shared clock);
    /// - every song place's glow brightening and breathing while its song plays.
    public class LanternwoodLife : MonoBehaviour
    {
        const string Land = "Lanternwood";
        public Vector3 lakeCenter; public float lakeRadius = 40f; public bool hasLake;
        public List<(string song, Material m, Color c)> songGlows = new List<(string, Material, Color)>();
        readonly Dictionary<string, float> songAmount = new Dictionary<string, float>();

        ParticleSystem snow, glints, wisps;
        readonly List<(Transform t, Material m, float phase)> curtains = new List<(Transform, Material, float)>();
        Camera cam; float aurora, auroraWant, snowAmt = .5f;
        long songKey = long.MinValue;

        static readonly Color Teal = new Color(.25f, 1f, .75f), Violet = new Color(.6f, .4f, 1f);

        void Start()
        {
            snow = Particles("Magic snow", 1400, new Color(.95f, .97f, 1f, .9f), .05f, .11f, false, new Vector3(60f, 1f, 60f));
            glints = Particles("Snow glints", 300, new Color(.8f, .9f, 1f, 1f), .06f, .12f, true, new Vector3(50f, 1f, 50f));
            wisps = Particles("Wisps", 80, new Color(.75f, .88f, 1f, .9f), .18f, .32f, true, new Vector3(70f, 2f, 70f));
            if (hasLake) MakeAurora();
        }

        static Texture2D Soft()
        {
            const int n = 32; var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (var y = 0; y < n; y++) for (var x = 0; x < n; x++)
            {
                var d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Pow(Mathf.Clamp01(1f - d), 1.6f)));
            }
            tex.Apply(); return tex;
        }

        static Material Additive(string name, Texture tex)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = name };
            if (tex != null) m.SetTexture("_BaseMap", tex);
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 2f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            m.SetFloat("_ZWrite", 0f); m.SetFloat("_Cull", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            return m;
        }

        ParticleSystem Particles(string name, int max, Color col, float s0, float s1, bool glow, Vector3 box)
        {
            var go = new GameObject(name); go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.loop = true; main.playOnAwake = false; main.maxParticles = max;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(7f, 12f); main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(s0, s1); main.startColor = col;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = box;
            var e = ps.emission; e.rateOverTime = 0f;
            var colLife = ps.colorOverLifetime; colLife.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, .15f), new GradientAlphaKey(1f, .8f), new GradientAlphaKey(0f, 1f) });
            colLife.color = g;
            var noise = ps.noise; noise.enabled = true; noise.strength = glow ? .6f : .35f; noise.frequency = .25f; noise.scrollSpeed = .2f;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = Additive(name, Soft());
            if (!glow) r.sharedMaterial.SetColor("_BaseColor", new Color(1f, 1f, 1f, .75f));
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ps.Play();
            return ps;
        }

        /// Long curtains of light over the lake: tall soft strips hanging in a gentle arc, each its own material
        /// so it can breathe on its own.
        void MakeAurora()
        {
            var tex = new Texture2D(1, 32, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (var y = 0; y < 32; y++) { var v = y / 31f; tex.SetPixel(0, y, new Color(1, 1, 1, Mathf.Pow(Mathf.Sin(v * Mathf.PI), 1.5f) * (1f - v * .6f))); }
            tex.Apply();
            for (var c = 0; c < 4; c++)
            {
                var go = new GameObject("Aurora " + c); go.transform.SetParent(transform, false);
                var v = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
                const int n = 40; var len = lakeRadius * 3.2f + c * 18f; var h = 26f + c * 6f;
                for (var k = 0; k <= n; k++)
                {
                    var t = k / (float)n; var x = (t - .5f) * len; var z = Mathf.Sin(t * Mathf.PI * 2f + c) * 14f + c * 10f;
                    v.Add(new Vector3(x, 0, z)); v.Add(new Vector3(x, h, z + 6f)); uv.Add(new Vector2(t, 0)); uv.Add(new Vector2(t, 1));
                    if (k > 0) { var i = k * 2; tri.AddRange(new[] { i - 2, i - 1, i, i, i - 1, i + 1 }); }
                }
                var mesh = new Mesh(); mesh.SetVertices(v); mesh.SetUVs(0, uv); mesh.SetTriangles(tri, 0); mesh.RecalculateBounds();
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var m = Additive("Aurora", tex); m.SetColor("_BaseColor", new Color(0, 0, 0, 0));
                var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                go.transform.position = lakeCenter + Vector3.up * (38f + c * 5f) + new Vector3(0, 0, lakeRadius * .3f);
                go.transform.rotation = Quaternion.Euler(0, c * 23f, 0);
                curtains.Add((go.transform, m, c * 1.7f));
            }
        }

        void Update()
        {
            var dt = Time.deltaTime; var t = Time.time;
            var here = SkyDirector.Instance != null && SkyDirector.Instance.CurrentLand == Land;
            var d = MusicDirector.Instance; var playing = d != null && d.CurrentSlot.IsValid;
            var valence = playing ? d.CurrentSlot.track.valence : .4f; var energy = SurpriseDirector.Energy;
            var id = MusicDirector.ForcedTrackId ?? (playing ? d.CurrentSlot.track.id : null);

            // song places
            foreach (var (song, _, _) in songGlows) if (!songAmount.ContainsKey(song)) songAmount[song] = 0f;
            var keys = new List<string>(songAmount.Keys);
            foreach (var s in keys) songAmount[s] = Mathf.MoveTowards(songAmount[s], here && id == s ? 1f : 0f, dt / 2.5f);
            foreach (var (song, m, c) in songGlows) if (m != null) m.SetColor("_EmissionColor", c * (1f + songAmount[song] * (2.4f + Mathf.Sin(t * 1.4f) * .5f)));

            // the aurora, decided per song from the shared clock: likelier in the hushed songs
            if (SharedChance.CurrentSong(out var song2, out _, out _) && song2 != songKey)
            {
                songKey = song2;
                auroraWant = SharedChance.Roll(SharedChance.Key("lanternwood-aurora"), song2, valence < .35f ? .7 : .3) ? 1f : 0f;
            }
            if (!here) auroraWant = 0f;
            aurora = Mathf.MoveTowards(aurora, auroraWant, dt / 6f);
            for (var i = 0; i < curtains.Count; i++)
            {
                var (tr, m, ph) = curtains[i]; if (tr == null) continue;
                var breathe = .55f + .45f * Mathf.Sin(t * .35f + ph);
                var col = Color.Lerp(Teal, Violet, .5f + .5f * Mathf.Sin(t * .12f + ph * 2f));
                col.a = aurora * breathe * (.5f + .25f * energy);
                m.SetColor("_BaseColor", col);
                tr.rotation = Quaternion.Euler(Mathf.Sin(t * .2f + ph) * 4f, i * 23f + Mathf.Sin(t * .07f + ph) * 6f, 0);
            }

            // snow, glints and wisps round you
            if (cam == null) cam = Camera.main;
            if (cam != null)
            {
                var p = cam.transform.position;
                snowAmt = Mathf.MoveTowards(snowAmt, valence < .35f ? 1f : .55f, dt / 8f);
                snow.transform.position = p + Vector3.up * 14f;
                var e = snow.emission; e.rateOverTime = here ? 40f + 90f * snowAmt : 0f;
                var vel = snow.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
                vel.x = new ParticleSystem.MinMaxCurve(-.2f, .3f); vel.y = new ParticleSystem.MinMaxCurve(-1.1f, -.7f); vel.z = new ParticleSystem.MinMaxCurve(-.2f, .2f);
                glints.transform.position = p + Vector3.up * 10f;
                var ge = glints.emission; ge.rateOverTime = here ? 6f + 14f * energy : 0f;
                var gv = glints.velocityOverLifetime; gv.enabled = true; gv.space = ParticleSystemSimulationSpace.World;
                gv.x = new ParticleSystem.MinMaxCurve(-.2f, .3f); gv.y = new ParticleSystem.MinMaxCurve(-.9f, -.6f); gv.z = new ParticleSystem.MinMaxCurve(-.2f, .2f);
                wisps.transform.position = new Vector3(p.x, p.y - .5f, p.z);
                var we = wisps.emission; we.rateOverTime = here ? 3f + 3f * (1f - valence) : 0f;
                var wv = wisps.velocityOverLifetime; wv.enabled = true; wv.space = ParticleSystemSimulationSpace.World;
                wv.x = new ParticleSystem.MinMaxCurve(-.3f, .3f); wv.y = new ParticleSystem.MinMaxCurve(-.05f, .15f); wv.z = new ParticleSystem.MinMaxCurve(-.3f, .3f);
            }
        }
    }
}
