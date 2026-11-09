using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EverythingWorld.World
{
    /// Life over the Everything Hub, driven by the music and the shared clock so friends see the same thing:
    /// - clouds drifting on the day's wind, more on some hours than others;
    /// - gulls circling the mast, flapping and gliding;
    /// - the golden light shifting with each song's mood: bright amber for happy songs, rose and violet for sad ones;
    /// - a golden rocket from the dais whenever a new song starts;
    /// - and for some songs (more often the more energetic they are) a fireworks show over the ring.
    /// F10 in play starts a fireworks show right away, for testing.
    public class HubLife : MonoBehaviour
    {
        const string Land = "The Everything Hub";
        public Vector3 center;
        public float ringRadius = 104f;

        class Gull { public Transform body, left, right; public float radius, height, speed, phase, flap; }
        readonly List<Gull> gulls = new List<Gull>();

        ParticleSystem sparks;
        class Rocket { public Vector3 pos, vel; public float fuse; public Color color; public int style; public float power; }
        readonly List<Rocket> rockets = new List<Rocket>();
        string songId; double songStart = -1;
        double showAt = -1; int showCount, launched; System.Random showRng;

        Color tint = new Color(1f, .68f, .3f);

        static readonly Color Gold = new Color(1f, .78f, .3f);
        static readonly Color[] Palette =
        {
            new Color(1f, .78f, .3f), new Color(1f, .95f, .85f), new Color(1f, .35f, .5f), new Color(.3f, .7f, 1f),
            new Color(.5f, 1f, .55f), new Color(.8f, .5f, 1f), new Color(1f, .55f, .2f), new Color(.4f, 1f, 1f),
        };

        void Start()
        {
            var clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 220; clouds.key = "hub"; clouds.minCover = .55f;
            MakeGulls();
            MakeSparks();
        }

        bool Here => SkyDirector.Instance != null && SkyDirector.Instance.CurrentLand == Land;

        void Update()
        {
            var now = SharedChance.Now;
            UpdateGulls(now);
            UpdateSong(now);
            UpdateRockets(Time.deltaTime);
            if (Keyboard.current != null && Keyboard.current.f10Key.wasPressedThisFrame) StartShow(now, .8f, Random.Range(0, int.MaxValue));
        }

        // ---------------------------------------------------------------- gulls
        void MakeGulls()
        {
            var rng = new System.Random(77);
            var white = WorldMaterials.Solid(new Color(.97f, .97f, .95f));
            var grey = WorldMaterials.Solid(new Color(.55f, .58f, .62f));
            var root = new GameObject("Gulls").transform; root.SetParent(transform, false);
            for (var i = 0; i < 14; i++)
            {
                var body = new GameObject("Gull").transform; body.SetParent(root, false);
                var mk = new MeshKit();
                mk.Box(white, Vector3.zero, new Vector3(.22f, .2f, .8f), Quaternion.identity);
                mk.Box(grey, new Vector3(0, .02f, -.5f), new Vector3(.3f, .04f, .25f), Quaternion.identity);
                mk.Build("Body", body, false).isStatic = false;
                Transform Wing(float side)
                {
                    var w = new GameObject(side < 0 ? "Left wing" : "Right wing").transform; w.SetParent(body, false);
                    var wk = new MeshKit();
                    wk.Box(white, new Vector3(side * .55f, 0, 0), new Vector3(1.1f, .04f, .36f), Quaternion.identity);
                    wk.Box(grey, new Vector3(side * 1.25f, 0, -.04f), new Vector3(.4f, .04f, .26f), Quaternion.identity);
                    wk.Build("Wing", w, false).isStatic = false;
                    return w;
                }
                var g = new Gull
                {
                    body = body, left = Wing(-1f), right = Wing(1f),
                    radius = 35f + (float)rng.NextDouble() * 70f, height = 28f + (float)rng.NextDouble() * 40f,
                    speed = (.08f + (float)rng.NextDouble() * .08f) * (rng.NextDouble() < .5 ? -1f : 1f),
                    phase = (float)rng.NextDouble() * 100f, flap = 5f + (float)rng.NextDouble() * 2f,
                };
                body.localScale = Vector3.one * 1.4f;
                gulls.Add(g);
            }
        }

        void UpdateGulls(double now)
        {
            var t = (float)(now % 100000.0);
            foreach (var g in gulls)
            {
                var a = g.phase + g.speed * t;
                var p = center + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * g.radius
                        + Vector3.up * (g.height + Mathf.Sin(t * .3f + g.phase) * 4f);
                var tangent = new Vector3(-Mathf.Sin(a), 0, Mathf.Cos(a)) * Mathf.Sign(g.speed);
                g.body.SetPositionAndRotation(p, Quaternion.LookRotation(tangent) * Quaternion.Euler(0, 0, -18f * Mathf.Sign(g.speed)));
                var flapping = Mathf.Clamp01(Mathf.Sin(t * .35f + g.phase) * 2f + .4f);     // flap for a while, then glide
                var angle = 6f + Mathf.Sin(t * g.flap + g.phase) * 38f * flapping;
                g.left.localRotation = Quaternion.Euler(0, 0, -angle);
                g.right.localRotation = Quaternion.Euler(0, 0, angle);
            }
        }

        // ---------------------------------------------------------------- the song: sunset colour, rockets
        void UpdateSong(double now)
        {
            var d = MusicDirector.Instance;
            var here = Here;
            var valence = .5f;
            if (d != null && d.CurrentSlot.IsValid)
            {
                var track = d.CurrentSlot.track;
                valence = track.valence;
                if (track.id != songId || d.CurrentSlot.startsAt != songStart)
                {
                    songId = track.id; songStart = d.CurrentSlot.startsAt;
                    var length = d.CurrentSlot.endsAt - songStart;
                    if (here) NewSong(now, track, songStart, length);
                }
            }
            // happy songs: bright amber; sad songs: rose and violet
            var goal = Color.Lerp(new Color(.82f, .48f, .78f), new Color(1f, .66f, .28f), Mathf.SmoothStep(0, 1, valence));
            tint = Color.Lerp(tint, goal, Time.deltaTime / 5f);
            SkyDirector.SetSongTint(Land, tint, .42f);

            if (showAt >= 0)
            {
                var elapsed = now - showAt;
                const double spacing = .75;
                if (elapsed > showCount * spacing + 5) showAt = -1;
                else
                {
                    var due = Mathf.Min(showCount, (int)(elapsed / spacing) + 1);
                    while (elapsed >= 0 && launched < due)
                    {
                        var angle = (float)showRng.NextDouble() * Mathf.PI * 2f;
                        var r = 55f + (float)showRng.NextDouble() * (ringRadius - 60f);
                        var height = 140f + (float)showRng.NextDouble() * 90f;
                        var color = Palette[showRng.Next(Palette.Length)];
                        var style = showRng.Next(3);
                        var late = elapsed - launched * spacing > 2.0;       // arrived mid-show: skip the ones already gone
                        if (!late && here) Launch(center + new Vector3(Mathf.Cos(angle) * r, 1f, Mathf.Sin(angle) * r), height, color, style, 1f);
                        launched++;
                    }
                }
            }
        }

        void NewSong(double now, TrackInfo track, double start, double length)
        {
            // a golden rocket from the dais as the song begins (only if we're here for the start)
            if (now - start < 4) Launch(center + Vector3.up * 2f, 190f, Gold, 0, 1.3f);
            // some songs get a fireworks show: more likely the more energetic the song is
            var key = SharedChance.Key("hub-fireworks:" + track.id);
            var window = (long)start;
            if (SharedChance.Roll(key, window, .2 + .5 * track.energy))
            {
                var at = start + length * (.3 + .45 * SharedChance.Value(key, window, 1));
                StartShow(at, track.energy, (int)(SharedChance.Value(key, window, 2) * int.MaxValue));
            }
            else showAt = -1;
        }

        void StartShow(double at, float energy, int seed)
        {
            showAt = at;
            showCount = 8 + Mathf.RoundToInt(energy * 16f);
            showRng = new System.Random(seed);
            launched = 0;
        }

        // ---------------------------------------------------------------- fireworks
        void MakeSparks()
        {
            var go = new GameObject("Fireworks");
            go.transform.SetParent(transform, false);
            sparks = go.AddComponent<ParticleSystem>();
            sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = sparks.main;
            main.loop = false; main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 30000;
            main.startLifetime = 2f; main.startSpeed = 0f; main.startSize = 1f;
            main.gravityModifier = .15f;
            var emission = sparks.emission; emission.enabled = false;
            var shape = sparks.shape; shape.enabled = false;
            var col = sparks.colorOverLifetime; col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                         new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, .55f), new GradientAlphaKey(0f, 1f) });
            col.color = grad;
            var size = sparks.sizeOverLifetime; size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, .35f));
            var limit = sparks.limitVelocityOverLifetime; limit.enabled = true; limit.limit = 1000f; limit.drag = 1.1f;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = SparkMaterial();
            r.maxParticleSize = 3f;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            sparks.Play();
        }

        void Launch(Vector3 from, float burstHeight, Color color, int style, float power)
        {
            const float fuse = 2.2f;
            var up = (burstHeight - (from.y - center.y)) / fuse;
            rockets.Add(new Rocket { pos = from, vel = new Vector3(Random.Range(-2f, 2f), up, Random.Range(-2f, 2f)), fuse = fuse, color = color, style = style, power = power });
        }

        void Spark(Vector3 pos, Vector3 vel, Color c, float size, float life)
        {
            var p = new ParticleSystem.EmitParams { position = pos, velocity = vel, startColor = c, startSize = size, startLifetime = life, applyShapeToPosition = false };
            sparks.Emit(p, 1);
        }

        void UpdateRockets(float dt)
        {
            for (var i = rockets.Count - 1; i >= 0; i--)
            {
                var r = rockets[i];
                r.pos += r.vel * dt;
                r.fuse -= dt;
                Spark(r.pos, Random.insideUnitSphere * 3f - r.vel * .05f, new Color(1f, .8f, .5f), 2.4f, .8f);
                if (r.fuse > 0f) continue;
                Burst(r);
                rockets.RemoveAt(i);
            }
        }

        void Burst(Rocket r)
        {
            var n = Mathf.RoundToInt(320 * r.power);
            var speed = 100f * r.power;
            var second = Palette[Random.Range(0, Palette.Length)];
            var ringAxis = Random.onUnitSphere;
            for (var k = 0; k < n; k++)
            {
                Vector3 dir;
                if (r.style == 1) dir = Vector3.ProjectOnPlane(Random.onUnitSphere, ringAxis).normalized;   // a ring
                else dir = Random.onUnitSphere;                                                             // a sphere
                var c = r.style == 2 && k % 2 == 0 ? second : r.color;                                     // two colours
                if (k % 7 == 0) c = Color.Lerp(c, Color.white, .6f);
                Spark(r.pos, dir * speed * Random.Range(.85f, 1.05f), c, Random.Range(4f, 6f) * r.power, Random.Range(2f, 3.2f));
            }
        }

        static Material SparkMaterial()
        {
            const int n = 32;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "Spark", wrapMode = TextureWrapMode.Clamp };
            for (var y = 0; y < n; y++)
            for (var x = 0; x < n; x++)
            {
                var d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                var a = Mathf.Clamp01(1f - d);
                tex.SetPixel(x, y, new Color(1, 1, 1, a * a));
            }
            tex.Apply();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = "Firework sparks" };
            mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 2f);                                  // additive
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            mat.SetFloat("_ZWrite", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            return mat;
        }

    }
}
