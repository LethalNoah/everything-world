using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// Low-poly clouds drifting over one land on the day's wind, the same for everyone (worked out from the
    /// shared clock). How cloudy it is changes each hour, or the land can set 'cover' itself (0..1) to follow
    /// the music. Clouds fade in and out slowly, never pop. With 'shadows' on, their shadows sweep across
    /// the ground. Lands set the fields right after adding it; the clouds are made in Start.
    public class CloudLayer : MonoBehaviour
    {
        public Vector3 center;
        public int count = 120;
        public float span = 3600f;                // the square of sky the clouds wrap round in
        public float minHeight = 150f, maxHeight = 290f, speed = 5f, size = 1f;
        public string key = "clouds";             // the wind and the hourly cloudiness are shared per key
        public float minCover = .25f;             // the clearest an hour can be
        public float cover = -1f;                 // set 0..1 to choose the cloudiness yourself (-1 = by the hour)
        public bool shadows;
        public string shadowLand;                 // if set, the shadows only fall while you're in this land (the clouds drift over neighbours too, whose ground went black under them)
        bool shadowsOn;
        readonly List<MeshRenderer> renderers = new List<MeshRenderer>();
        public Color color = new Color(1f, .97f, .93f);

        class Cloud { public Transform t; public Vector2 home; public float height, scale; public int rank; }
        readonly List<Cloud> clouds = new List<Cloud>();

        void Start()
        {
            var rng = new System.Random(SharedChance.Key(key).GetHashCode());
            var white = WorldMaterials.Solid(color);
            var root = new GameObject("Clouds").transform; root.SetParent(transform, false);
            for (var i = 0; i < count; i++)
            {
                var t = new GameObject("Cloud").transform; t.SetParent(root, false);
                var mk = new MeshKit();
                var puffs = 4 + rng.Next(5);
                var length = (40f + (float)rng.NextDouble() * 70f) * size;
                for (var k = 0; k < puffs; k++)
                {
                    var f = k / (puffs - 1f) - .5f;
                    var big = 1f - Mathf.Abs(f) * 1.1f;
                    var r = (14f + (float)rng.NextDouble() * 12f) * (.6f + big * .6f) * size;
                    var c = new Vector3(f * length, r * .25f + (float)rng.NextDouble() * 4f, ((float)rng.NextDouble() - .5f) * 18f * size);
                    mk.Blob(white, c, new Vector3(r, r * .6f, r * .85f), rng, .12f);
                }
                var go = mk.BuildSingle("Puffs", t, false);
                go.isStatic = false;
                var mr = go.GetComponent<MeshRenderer>(); renderers.Add(mr);
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                t.localRotation = Quaternion.Euler(0, (float)rng.NextDouble() * 40f - 20f, 0);
                clouds.Add(new Cloud
                {
                    t = t, home = new Vector2((float)rng.NextDouble() * span, (float)rng.NextDouble() * span),
                    height = Mathf.Lerp(minHeight, maxHeight, (float)rng.NextDouble()), rank = i,
                });
            }
        }

        float Wrap(double v) => (float)(((v % span) + span) % span - span / 2f);

        /// The day's wind direction for this key (shared by everyone).
        public Vector2 Wind
        {
            get
            {
                var a = (float)SharedChance.Value(SharedChance.Key(key + "-wind"), SharedChance.Day) * Mathf.PI * 2f;
                return new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            }
        }

        void Update()
        {
            var want = shadows && (string.IsNullOrEmpty(shadowLand) || (SkyDirector.Instance != null && SkyDirector.Instance.CurrentLand == shadowLand));
            if (want != shadowsOn)
            {
                shadowsOn = want;
                foreach (var r in renderers) if (r != null) r.shadowCastingMode = want ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            var now = SharedChance.Now;
            var wind = Wind;
            var c = cover >= 0f ? cover : minCover + (1f - minCover) * (float)SharedChance.Value(SharedChance.Key(key + "-clouds"), SharedChance.Hour);
            foreach (var cl in clouds)
            {
                var x = Wrap(cl.home.x + wind.x * speed * now);
                var z = Wrap(cl.home.y + wind.y * speed * now);
                var edge = Mathf.Clamp01((span / 2f - Mathf.Max(Mathf.Abs(x), Mathf.Abs(z))) / (span * .11f));
                var goal = (cl.rank + .5f) / count < c ? 1f : 0f;
                cl.scale = Mathf.MoveTowards(cl.scale, goal, Time.deltaTime / 30f);
                var s = cl.scale * edge;
                cl.t.gameObject.SetActive(s > .01f);
                cl.t.position = new Vector3(center.x + x, center.y + cl.height, center.z + z);
                cl.t.localScale = Vector3.one * Mathf.Max(.01f, s);
            }
        }
    }
}
