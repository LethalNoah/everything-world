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

        class Cloud { public Transform t; public Vector2 home; public float height, scale; public int rank; }
        readonly List<Cloud> clouds = new List<Cloud>();
        const int CloudCount = 220;
        const float CloudSpan = 3600f, CloudSpeed = 5f;

        class Gull { public Transform body, left, right; public float radius, height, speed, phase, flap; }
        readonly List<Gull> gulls = new List<Gull>();

        ParticleSystem sparks;
        class Rocket { public Vector3 pos, vel; public float fuse; public Color color; public int style; public float power; }
        readonly List<Rocket> rockets = new List<Rocket>();
        string songId; double songStart = -1;
        double showAt = -1; int showCount, launched; System.Random showRng;

        Color tint = new Color(1f, .68f, .3f); float tintAmount;

        static readonly Color Gold = new Color(1f, .78f, .3f);
        static readonly Color[] Palette =
        {
            new Color(1f, .78f, .3f), new Color(1f, .95f, .85f), new Color(1f, .35f, .5f), new Color(.3f, .7f, 1f),
            new Color(.5f, 1f, .55f), new Color(.8f, .5f, 1f), new Color(1f, .55f, .2f), new Color(.4f, 1f, 1f),
        };

        void Start()
        {
            MakeClouds();
            MakeGulls();
            MakeSparks();
        }

        bool Here => SkyDirector.Instance != null && SkyDirector.Instance.CurrentLand == Land;

        void Update()
        {
            var now = SharedChance.Now;
            UpdateClouds(now);
            UpdateGulls(now);
            UpdateSong(now);
            UpdateRockets(Time.deltaTime);
            if (Keyboard.current != null && Keyboard.current.f10Key.wasPressedThisFrame) StartShow(now, .8f, Random.Range(0, int.MaxValue));
        }

        // ---------------------------------------------------------------- clouds
        void MakeClouds()
        {
            var rng = new System.Random(1234);
            var white = WorldMaterials.Solid(new Color(1f, .97f, .93f));
            var root = new GameObject("Clouds").transform; root.SetParent(transform, false);
            for (var i = 0; i < CloudCount; i++)
            {
                var t = new GameObject("Cloud").transform; t.SetParent(root, false);
                var mk = new MeshKit();
                var puffs = 4 + rng.Next(5);
                var length = 40f + (float)rng.NextDouble() * 70f;
                for (var k = 0; k < puffs; k++)
                {
                    var f = puffs == 1 ? 0f : k / (puffs - 1f) - .5f;
                    var big = 1f - Mathf.Abs(f) * 1.1f;
                    var r = (14f + (float)rng.NextDouble() * 12f) * (.6f + big * .6f);
                    var c = new Vector3(f * length, r * .25f + (float)rng.NextDouble() * 4f, ((float)rng.NextDouble() - .5f) * 18f);
                    mk.Blob(white, c, new Vector3(r, r * .6f, r * .85f), rng, .12f);
                }
                var go = mk.Build("Puffs", t, false);
                go.isStatic = false;
                go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                t.localRotation = Quaternion.Euler(0, (float)rng.NextDouble() * 40f - 20f, 0);
                clouds.Add(new Cloud
                {
                    t = t, home = new Vector2((float)rng.NextDouble() * CloudSpan, (float)rng.NextDouble() * CloudSpan),
                    height = 150f + (float)rng.NextDouble() * 140f, rank = i, scale = 0f,
                });
            }
        }

        static float Wrap(double v) => (float)(((v % CloudSpan) + CloudSpan) % CloudSpan - CloudSpan / 2f);

        void UpdateClouds(double now)
        {
            var windAngle = (float)SharedChance.Value(SharedChance.Key("hub-wind"), SharedChance.Day) * Mathf.PI * 2f;
            var wind = new Vector2(Mathf.Cos(windAngle), Mathf.Sin(windAngle));
            // how cloudy this hour is, the same for everyone: from a few fair-weather clouds to a full sky
            var cover = .55f + .45f * (float)SharedChance.Value(SharedChance.Key("hub-clouds"), SharedChance.Hour);
            foreach (var c in clouds)
            {
                var x = Wrap(c.home.x + wind.x * CloudSpeed * now);
                var z = Wrap(c.home.y + wind.y * CloudSpeed * now);
                var edge = Mathf.Clamp01((CloudSpan / 2f - Mathf.Max(Mathf.Abs(x), Mathf.Abs(z))) / 400f);
                var goal = (c.rank + .5f) / CloudCount < cover ? 1f : 0f;
                c.scale = Mathf.MoveTowards(c.scale, goal, Time.deltaTime / 30f);
                var s = c.scale * edge;
                c.t.gameObject.SetActive(s > .01f);
                c.t.position = new Vector3(center.x + x, center.y + c.height, center.z + z);
                c.t.localScale = Vector3.one * Mathf.Max(.01f, s);
            }
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
            tintAmount = Mathf.MoveTowards(tintAmount, here ? .42f : 0f, Time.deltaTime / 4f);
            SkyDirector.SongTint = tint;
            SkyDirector.SongTintAmount = tintAmount;

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
                        var height = 75f + (float)showRng.NextDouble() * 55f;
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
            if (now - start < 4) Launch(center + Vector3.up * 2f, 95f, Gold, 0, 1.3f);
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
            main.maxParticles = 8000;
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
            r.maxParticleSize = 1f;
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
                Spark(r.pos, Random.insideUnitSphere * 1.5f - r.vel * .05f, new Color(1f, .8f, .5f), .8f, .6f);
                if (r.fuse > 0f) continue;
                Burst(r);
                rockets.RemoveAt(i);
            }
        }

        void Burst(Rocket r)
        {
            var n = Mathf.RoundToInt(150 * r.power);
            var speed = 26f * r.power;
            var second = Palette[Random.Range(0, Palette.Length)];
            var ringAxis = Random.onUnitSphere;
            for (var k = 0; k < n; k++)
            {
                Vector3 dir;
                if (r.style == 1) dir = Vector3.ProjectOnPlane(Random.onUnitSphere, ringAxis).normalized;   // a ring
                else dir = Random.onUnitSphere;                                                             // a sphere
                var c = r.style == 2 && k % 2 == 0 ? second : r.color;                                     // two colours
                if (k % 7 == 0) c = Color.Lerp(c, Color.white, .6f);
                Spark(r.pos, dir * speed * Random.Range(.85f, 1.05f), c, Random.Range(1.1f, 1.6f) * r.power, Random.Range(1.6f, 2.6f));
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

        void OnDestroy()
        {
            SkyDirector.SongTintAmount = 0f;
        }
    }
}
