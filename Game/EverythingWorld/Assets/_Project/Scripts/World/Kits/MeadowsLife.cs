using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Bedroom Pop Meadows coming alive with the music:
    /// - the pulses of light chase round the track faster for energetic songs, slower for quiet ones, and every
    ///   new song sends a bright pulse racing round;
    /// - fireflies drift low over the lilac grass, more of them (and slower) the quieter the song;
    /// - soap bubbles float up from the field, more for happy songs;
    /// - glow-in-the-dark star stickers fade in across the sky as a song goes quiet;
    /// - the light leans pinker for happy songs and deeper violet for sad ones.
    public class MeadowsLife : MonoBehaviour
    {
        const string Land = "The Bedroom Pop Meadows";
        public Vector3 center;

        ParticleSystem fireflies, bubbles;
        Material stickers;
        Transform stickerDome;
        Camera cam;
        float head, speed = .02f, quiet = .5f, glow;
        string songId;
        class Lantern { public Transform t; public Vector3 home; public float phase, speed; }
        readonly List<Lantern> lanterns = new List<Lantern>();

        void Start()
        {
            var clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 45; clouds.span = 2600f; clouds.key = "meadows";
            clouds.minHeight = 170f; clouds.maxHeight = 280f; clouds.cover = .4f; clouds.speed = 3f;
            clouds.color = new Color(.96f, .72f, .78f);
            fireflies = Particles("Fireflies", 900, new Color(.78f, 1f, .45f), true);
            var fm = fireflies.main; fm.startSize = new ParticleSystem.MinMaxCurve(.08f, .16f); fm.startLifetime = new ParticleSystem.MinMaxCurve(5f, 9f);
            var fs = fireflies.shape; fs.shapeType = ParticleSystemShapeType.Box; fs.scale = new Vector3(70f, 3f, 70f);
            var fc = fireflies.colorOverLifetime; fc.enabled = true;
            var blink = new Gradient();
            blink.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, .1f), new GradientAlphaKey(.15f, .25f), new GradientAlphaKey(1f, .4f), new GradientAlphaKey(.1f, .55f), new GradientAlphaKey(1f, .7f), new GradientAlphaKey(.2f, .85f), new GradientAlphaKey(0f, 1f) });
            fc.color = blink;
            var fn = fireflies.noise; fn.enabled = true; fn.strength = .9f; fn.frequency = .3f; fn.scrollSpeed = .2f;
            bubbles = Particles("Bubbles", 400, new Color(.95f, .9f, 1f, .5f), false);
            var bm = bubbles.main; bm.startSize = new ParticleSystem.MinMaxCurve(.25f, .8f); bm.startLifetime = new ParticleSystem.MinMaxCurve(8f, 14f);
            var bs = bubbles.shape; bs.shapeType = ParticleSystemShapeType.Box; bs.scale = new Vector3(60f, 1f, 60f);
            var bv = bubbles.velocityOverLifetime; bv.enabled = true; bv.space = ParticleSystemSimulationSpace.World; bv.x = new ParticleSystem.MinMaxCurve(-.1f, .1f); bv.y = new ParticleSystem.MinMaxCurve(.4f, .9f); bv.z = new ParticleSystem.MinMaxCurve(-.1f, .1f);
            var bn = bubbles.noise; bn.enabled = true; bn.strength = .5f; bn.frequency = .2f;
            var bc = bubbles.colorOverLifetime; bc.enabled = true;
            var irid = new Gradient();
            irid.SetKeys(new[] { new GradientColorKey(new Color(.85f, .95f, 1f), 0f), new GradientColorKey(new Color(1f, .85f, .95f), .5f), new GradientColorKey(new Color(.9f, 1f, .85f), 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(.6f, .1f), new GradientAlphaKey(.6f, .9f), new GradientAlphaKey(0f, 1f) });
            bc.color = irid;
            MakeStickers();
            MakeLanterns();
        }

        /// Paper sky lanterns in pastel colours, rising slowly over the field and drifting, fading out high up.
        void MakeLanterns()
        {
            var rng = new System.Random(321);
            Color[] cols = { new Color(1f, .7f, .8f), new Color(.85f, .75f, 1f), new Color(1f, .85f, .6f), new Color(.75f, .95f, .9f) };
            var root = new GameObject("Sky lanterns").transform; root.SetParent(transform, false);
            for (var i = 0; i < 32; i++)
            {
                var mk = new MeshKit();
                var col = cols[i % cols.Length];
                mk.Prism(WorldMaterials.Glow(col, 2.2f), Vector3.zero, Vector3.up, .32f, .75f, 6, 1.15f);
                mk.Box(WorldMaterials.Glow(new Color(1f, .8f, .4f), 4f), Vector3.up * .08f, Vector3.one * .14f, Quaternion.identity);
                var t = new GameObject("Lantern").transform; t.SetParent(root, false);
                var go = mk.Build("Paper", t, false); go.isStatic = false;
                go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var a = (float)rng.NextDouble() * Mathf.PI * 2f; var r = 30f + (float)rng.NextDouble() * 260f;
                lanterns.Add(new Lantern { t = t, home = center + new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r), phase = (float)rng.NextDouble(), speed = .006f + (float)rng.NextDouble() * .008f });
            }
        }

        ParticleSystem Particles(string name, int max, Color color, bool additive)
        {
            var go = new GameObject(name); go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true; main.playOnAwake = false; main.maxParticles = max;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startSpeed = 0f; main.startColor = color;
            var e = ps.emission; e.rateOverTime = 0f;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = Soft(name, additive);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ps.Play();
            return ps;
        }

        static Material Soft(string name, bool additive)
        {
            const int n = 32;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = name, wrapMode = TextureWrapMode.Clamp };
            for (var y = 0; y < n; y++)
            for (var x = 0; x < n; x++)
            {
                var d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                // fireflies: a soft dot; bubbles: a thin bright rim with a clear middle
                var a = additive ? Mathf.Pow(Mathf.Clamp01(1f - d), 2f) : Mathf.Clamp01(1f - Mathf.Abs(d - .82f) * 7f) + Mathf.Clamp01(.25f - Vector2.Distance(new Vector2(x, y), new Vector2(n * .35f, n * .65f)) / n) * 3f;
                tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(a)));
            }
            tex.Apply();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = name };
            mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", additive ? 2f : 0f);
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)(additive ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
            mat.SetFloat("_ZWrite", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            return mat;
        }

        /// Star stickers on a dome round the camera: five-pointed, a little uneven, the way they end up on a ceiling.
        void MakeStickers()
        {
            var shader = Shader.Find("Everything World/Sticker Stars");
            if (shader == null) return;
            var rng = new System.Random(808);
            var v = new List<Vector3>(); var c = new List<Color>(); var t = new List<int>();
            const float R = 700f;
            for (var s = 0; s < 140; s++)
            {
                var yaw = (float)rng.NextDouble() * Mathf.PI * 2f;
                var pitch = Mathf.Lerp(12f, 80f, Mathf.Sqrt((float)rng.NextDouble())) * Mathf.Deg2Rad;
                var dir = new Vector3(Mathf.Cos(yaw) * Mathf.Cos(pitch), Mathf.Sin(pitch), Mathf.Sin(yaw) * Mathf.Cos(pitch));
                var right = Vector3.Cross(Vector3.up, dir).normalized; var up = Vector3.Cross(dir, right);
                var size = (5f + (float)rng.NextDouble() * 9f);
                var spin = (float)rng.NextDouble() * Mathf.PI * 2f;
                var centre = dir * R; var b = v.Count;
                v.Add(centre); c.Add(new Color(1, 1, 1, .5f + (float)rng.NextDouble() * .5f));
                for (var k = 0; k < 10; k++)
                {
                    var a = spin + k * Mathf.PI / 5f; var rr = k % 2 == 0 ? size : size * .42f;
                    v.Add(centre + (right * Mathf.Cos(a) + up * Mathf.Sin(a)) * rr); c.Add(c[b]);
                }
                for (var k = 0; k < 10; k++) { t.Add(b); t.Add(b + 1 + k); t.Add(b + 1 + (k + 1) % 10); }
            }
            var mesh = new Mesh { name = "Sticker stars" };
            mesh.SetVertices(v); mesh.SetColors(c); mesh.SetTriangles(t, 0);
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * R * 2.2f);
            var go = new GameObject("Sticker stars"); go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            stickers = new Material(shader) { name = "Sticker stars" };
            r.sharedMaterial = stickers;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            stickerDome = go.transform;
        }

        void Update()
        {
            var dt = Time.deltaTime;
            var here = SkyDirector.Instance != null && SkyDirector.Instance.CurrentLand == Land;
            var d = MusicDirector.Instance;
            var playing = d != null && d.CurrentSlot.IsValid;
            var energy = SurpriseDirector.Energy;
            var valence = playing ? d.CurrentSlot.track.valence : .5f;
            if (playing && d.CurrentSlot.track.id != songId) { songId = d.CurrentSlot.track.id; glow = 1f; }
            glow = Mathf.MoveTowards(glow, 0f, dt / 5f);
            quiet = Mathf.MoveTowards(quiet, 1f - Mathf.InverseLerp(.25f, .8f, energy), dt / 4f);

            // the pulses round the track: pacing faster when the song is energetic, and a dash on a new song
            speed = Mathf.MoveTowards(speed, .012f + .06f * (1f - quiet) + glow * .08f, dt * .05f);
            head = Mathf.Repeat(head + speed * dt, 1f);
            Shader.SetGlobalFloat("_EW_ChaseHead", head);

            // the light: pinker for happy songs, deeper violet for sad ones
            var tint = Color.Lerp(new Color(.48f, .4f, .82f), new Color(1f, .62f, .78f), Mathf.SmoothStep(0, 1, valence));
            SkyDirector.SetSongTint(Land, tint, .35f);

            var now = (float)(SharedChance.Now % 100000.0);
            foreach (var l in lanterns)
            {
                var f = Mathf.Repeat(l.phase + now * l.speed, 1f);              // 0 = just let go, 1 = gone
                l.t.gameObject.SetActive(here);
                if (!here) continue;
                l.t.position = l.home + new Vector3(Mathf.Sin(now * .05f + l.phase * 9f) * 18f * f, 8f + f * 120f, Mathf.Cos(now * .04f + l.phase * 7f) * 18f * f);
                l.t.localScale = Vector3.one * (1.6f * Mathf.Clamp01((1f - f) * 4f) * Mathf.Clamp01(f * 12f));
            }
            if (cam == null) cam = Camera.main;
            if (cam == null) return;
            var at = cam.transform.position;
            fireflies.transform.position = new Vector3(at.x, at.y + .5f, at.z);
            var fe = fireflies.emission; fe.rateOverTime = here ? 40f + 160f * quiet : 0f;
            var fm = fireflies.main; fm.simulationSpeed = .6f + .8f * (1f - quiet);
            bubbles.transform.position = new Vector3(at.x, at.y - 1.5f, at.z);
            var be = bubbles.emission; be.rateOverTime = here ? 2f + 22f * Mathf.InverseLerp(.35f, .85f, valence) : 0f;
            if (stickerDome != null)
            {
                stickerDome.position = at;
                stickers.SetFloat("_Strength", here ? Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.35f, .9f, quiet)) * .9f : 0f);
            }
        }
    }
}
