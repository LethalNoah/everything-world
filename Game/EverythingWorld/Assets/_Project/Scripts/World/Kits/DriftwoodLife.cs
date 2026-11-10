using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Driftwood Shore's weather, the morning after a storm, answering the music:
    /// - low grey cloud racing in from the sea, and salt mist blowing across the shore on a strong wind;
    /// - the light breaks rose and gold for hopeful songs, and turns cold and silver for sad ones;
    /// - some sad songs bring a passing drizzle (the same songs for everyone);
    /// - the beacon at the end of the jetty sweeps its beam through the mist; sea glass glints along the tide line;
    /// - when "Somewhere Over the Rainbow" plays, a rainbow rises through the driftwood arch, and stays while it plays;
    /// - gulls, a passing ship, a whale, lightning at sea, the cloud breaking, a message in a bottle (DriftwoodHappenings.cs).
    public partial class DriftwoodLife : MonoBehaviour
    {
        const string Land = "The Driftwood Shore";
        public Vector3 center, archAt, seaDir = Vector3.forward, beaconAt;
        public Material[] seaGlass = new Material[0];
        public List<Vector3> landmarks = new List<Vector3>();

        CloudLayer clouds;
        ParticleSystem mist;
        Transform beam, rainbow;
        Material rainbowMat;
        Camera cam;
        float bow, mood = .4f;
        long songKey = long.MinValue; bool drizzle;

        static readonly Color[] GlassColors = { new Color(.4f, .9f, .8f), new Color(.45f, .85f, .5f), new Color(1f, .7f, .3f), new Color(.85f, .92f, 1f), new Color(.4f, .6f, 1f) };

        void Start()
        {
            clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 130; clouds.span = 2600f; clouds.key = "driftwood";
            clouds.minHeight = 70f; clouds.maxHeight = 140f; clouds.size = 1.5f; clouds.speed = 13f; clouds.cover = .85f;
            clouds.color = new Color(.66f, .68f, .72f);
            MakeMist();
            if (beaconAt != Vector3.zero) MakeBeam();
            MakeRainbow();
            MakeHappenings();
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

        void MakeMist()
        {
            var go = new GameObject("Salt mist"); go.transform.SetParent(transform, false);
            mist = go.AddComponent<ParticleSystem>();
            mist.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = mist.main;
            main.loop = true; main.playOnAwake = false; main.maxParticles = 1200;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 7f); main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(2.5f, 6f);
            main.startColor = new Color(.9f, .92f, .94f, .14f);
            var shape = mist.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(70f, 6f, 70f);
            var e = mist.emission; e.rateOverTime = 0f;
            var col = mist.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, .3f), new GradientAlphaKey(1f, .7f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            const int n = 32;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "Mist", wrapMode = TextureWrapMode.Clamp };
            for (var y = 0; y < n; y++) for (var x = 0; x < n; x++)
            {
                var d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Pow(Mathf.Clamp01(1f - d), 2f)));
            }
            tex.Apply();
            var m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = "Salt mist" };
            m.SetTexture("_BaseMap", tex); m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            var r = go.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mist.Play();
        }

        /// Two long soft cones of light turning round the beacon.
        void MakeBeam()
        {
            var v = new List<Vector3>(); var c = new List<Color>(); var t = new List<int>();
            foreach (var dir in new[] { 1f, -1f })
            {
                const int sides = 10; const float len = 90f, r = 9f;
                var b = v.Count;
                v.Add(Vector3.zero); c.Add(new Color(1f, .92f, .75f, .5f));
                for (var k = 0; k < sides; k++)
                {
                    var a = k * Mathf.PI * 2f / sides;
                    v.Add(new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r * .6f, len * dir)); c.Add(new Color(1f, .92f, .75f, 0f));
                }
                for (var k = 0; k < sides; k++) { t.Add(b); t.Add(b + 1 + k); t.Add(b + 1 + (k + 1) % sides); }
            }
            var mesh = new Mesh { name = "Beacon beam" };
            mesh.SetVertices(v); mesh.SetColors(c); mesh.SetTriangles(t, 0); mesh.RecalculateBounds();
            var go = new GameObject("Beacon beam"); go.transform.SetParent(transform, false);
            go.transform.position = beaconAt;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var rend = go.AddComponent<MeshRenderer>(); rend.sharedMaterial = Additive("Beacon beam", Texture2D.whiteTexture);
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            beam = go.transform;
        }

        void MakeRainbow()
        {
            var cols = new[] { new Color(.9f, .3f, .3f), new Color(.95f, .58f, .25f), new Color(.95f, .9f, .35f), new Color(.45f, .85f, .45f), new Color(.35f, .6f, .95f), new Color(.45f, .38f, .85f), new Color(.65f, .38f, .8f) };
            var v = new List<Vector3>(); var c = new List<Color>(); var t = new List<int>();
            const float R = 150f, band = 7f; const int segs = 64;
            for (var b = 0; b < cols.Length; b++)
            {
                float r0 = R - b * band, r1 = r0 - band;
                for (var i = 0; i < segs; i++)
                {
                    float a0 = Mathf.PI * i / segs, a1 = Mathf.PI * (i + 1) / segs;
                    var k = v.Count;
                    v.Add(new Vector3(Mathf.Cos(a0) * r0, Mathf.Sin(a0) * r0, 0)); v.Add(new Vector3(Mathf.Cos(a1) * r0, Mathf.Sin(a1) * r0, 0));
                    v.Add(new Vector3(Mathf.Cos(a1) * r1, Mathf.Sin(a1) * r1, 0)); v.Add(new Vector3(Mathf.Cos(a0) * r1, Mathf.Sin(a0) * r1, 0));
                    float F(float a) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(Mathf.Sin(a) / .45f));     // the feet melt into the mist
                    var e = (b == 0 || b == cols.Length - 1) ? .3f : .6f;
                    c.Add(new Color(cols[b].r, cols[b].g, cols[b].b, e * F(a0))); c.Add(new Color(cols[b].r, cols[b].g, cols[b].b, e * F(a1)));
                    c.Add(new Color(cols[b].r, cols[b].g, cols[b].b, e * F(a1))); c.Add(new Color(cols[b].r, cols[b].g, cols[b].b, e * F(a0)));
                    t.AddRange(new[] { k, k + 1, k + 2, k, k + 2, k + 3 });
                }
            }
            var mesh = new Mesh { name = "Rainbow" };
            mesh.SetVertices(v); mesh.SetColors(c); mesh.SetTriangles(t, 0); mesh.RecalculateBounds();
            var go = new GameObject("Rainbow"); go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var rend = go.AddComponent<MeshRenderer>();
            rainbowMat = Additive("Rainbow", Texture2D.whiteTexture);
            rend.sharedMaterial = rainbowMat; rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // rising behind the arch, out over the sea, seen through it from the land
            var foot = new Vector3(archAt.x, archAt.y - 22f, archAt.z) + seaDir * 110f;
            go.transform.SetPositionAndRotation(foot, Quaternion.LookRotation(seaDir));
            rainbow = go.transform;
            go.SetActive(false);
        }

        void Update()
        {
            var dt = Time.deltaTime; var t = Time.time;
            var here = SkyDirector.Instance != null && SkyDirector.Instance.CurrentLand == Land;
            var d = MusicDirector.Instance;
            var playing = d != null && d.CurrentSlot.IsValid;
            var energy = SurpriseDirector.Energy;
            var valence = playing ? d.CurrentSlot.track.valence : .4f;
            var id = MusicDirector.ForcedTrackId ?? (playing ? d.CurrentSlot.track.id : null);

            // a passing drizzle for some sad songs, decided by the song itself
            if (SharedChance.CurrentSong(out var song, out _, out _) && song != songKey)
            {
                songKey = song;
                drizzle = playing && d.CurrentSlot.track.valence < .4f && SharedChance.Roll(SharedChance.Key("driftwood-drizzle"), song, .4);
            }
            SkyDirector.SetSongRain(Land, drizzle ? .22f : 0f);

            // the light: rose and gold breaking through for hopeful songs, cold silver for sad ones
            UpdateHappenings(here, valence, drizzle);
            mood = Mathf.MoveTowards(mood, Mathf.Clamp01(valence * 1.2f - (drizzle ? .3f : 0f) + Sunbreak * .35f), dt / 4f);
            SkyDirector.SetSongTint(Land, Color.Lerp(new Color(.62f, .68f, .76f), new Color(1f, .74f, .62f), mood), Mathf.Lerp(.5f, .3f, mood));
            if (clouds != null) clouds.cover = Mathf.Lerp(.95f, .6f, mood) - Sunbreak * .2f;

            // the wind off the sea: always strong here, stronger for energetic songs
            if (here)
            {
                var w = clouds != null ? clouds.Wind : new Vector2(1, 0);
                Shader.SetGlobalFloat("_EW_Wind", .55f + .45f * energy);
                Shader.SetGlobalVector("_EW_WindDir", new Vector4(w.x, 0, w.y, 0));
            }

            if (cam == null) cam = Camera.main;
            if (cam != null && mist != null)
            {
                mist.transform.position = cam.transform.position + Vector3.up * 1.5f;
                var e = mist.emission; e.rateOverTime = here ? 30f + 30f * energy : 0f;
                var vel = mist.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
                var w = clouds != null ? clouds.Wind : new Vector2(1, 0);
                vel.x = w.x * (3f + 4f * energy); vel.y = .1f; vel.z = w.y * (3f + 4f * energy);
            }
            if (beam != null) beam.Rotate(0, 22f * dt, 0, Space.World);
            for (var i = 0; i < seaGlass.Length; i++)
                seaGlass[i].SetColor("_EmissionColor", GlassColors[i % GlassColors.Length] * (1.2f + 1.4f * Mathf.Max(0f, Mathf.Sin(t * (1.7f + i * .37f) + i * 2f))));

            // "Somewhere Over the Rainbow": the rainbow rises through the arch while it plays
            bow = Mathf.MoveTowards(bow, here && id == DriftwoodKit.RainbowSong ? 1f : 0f, dt / 6f);
            if (rainbow != null)
            {
                rainbow.gameObject.SetActive(bow > .01f);
                rainbowMat.SetColor("_BaseColor", new Color(1f, 1f, 1f, bow * (.85f + .15f * Mathf.Sin(t * .6f))));
            }
        }
    }
}
