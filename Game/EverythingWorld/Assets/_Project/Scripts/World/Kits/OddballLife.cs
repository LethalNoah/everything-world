using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Oddball Backroads on a hazy summer afternoon, slightly off:
    /// - the giant head's eyes follow you wherever you go; dandelion fluff drifts in the haze; the attraction signs'
    ///   bulbs blink;
    /// - "Ocean Man": the sky turns ocean blue. "Where Is My Mind?": the sky goes a dreamy violet and the thoughts over
    ///   the open head glow. "It's The End Of The World As We Know It": the sky reddens and meteors streak down (and
    ///   everyone feels fine). "Blister In The Sun": the light goes white-hot. "Yoshimi": the land blushes pink. Every
    ///   song's own light brightens while it plays.
    public class OddballLife : MonoBehaviour
    {
        const string Land = "The Oddball Backroads";
        public Vector3 center, mindAt, oceanAt, sunAt, meteorAt, robotEyes;
        public Material windows;
        public Transform eyes;
        public List<(string song, Material m, Color c)> songGlows = new List<(string, Material, Color)>();
        readonly Dictionary<string, float> songAmount = new Dictionary<string, float>();

        CloudLayer clouds; ParticleSystem fluff, streaks;
        Quaternion eyesRest;
        float ocean, mind, end, blister, yoshimi, nextFluff, nextStreak;

        void Start()
        {
            clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 45; clouds.span = 2600f; clouds.key = "oddball";
            clouds.minHeight = 200f; clouds.maxHeight = 280f; clouds.size = 3f; clouds.speed = 3f; clouds.cover = .35f;
            clouds.color = new Color(1f, .97f, .9f);
            if (eyes != null) eyesRest = eyes.rotation;
            fluff = Particles("Dandelion fluff", 600, new Color(1f, 1f, .95f, .9f), .06f, .12f, false);
            var fm = fluff.main; fm.startLifetime = new ParticleSystem.MinMaxCurve(6f, 10f); fm.startSpeed = new ParticleSystem.MinMaxCurve(.1f, .4f); fm.gravityModifier = -.005f;
            streaks = Particles("Meteors", 200, new Color(1f, .7f, .35f, 1f), .6f, 1.2f, true);
            var sm = streaks.main; sm.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 2.2f); sm.startSpeed = new ParticleSystem.MinMaxCurve(60f, 90f);
            var sr = streaks.GetComponent<ParticleSystemRenderer>(); sr.renderMode = ParticleSystemRenderMode.Stretch; sr.velocityScale = .15f; sr.lengthScale = 2f;
        }

        static Material Additive(string name)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = name };
            m.SetColor("_BaseColor", Color.white); m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 2f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            m.SetFloat("_ZWrite", 0f); m.SetFloat("_Cull", 0f); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            return m;
        }

        ParticleSystem Particles(string name, int max, Color col, float s0, float s1, bool add)
        {
            var go = new GameObject(name); go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.loop = false; main.playOnAwake = false; main.maxParticles = max;
            main.simulationSpace = ParticleSystemSimulationSpace.World; main.startSize = new ParticleSystem.MinMaxCurve(s0, s1); main.startColor = col;
            var e = ps.emission; e.rateOverTime = 0f;
            var cl = ps.colorOverLifetime; cl.enabled = true;
            var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, .1f), new GradientAlphaKey(0f, 1f) });
            cl.color = g;
            var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (var y = 0; y < 32; y++) for (var x = 0; x < 32; x++) { var d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(16, 16)) / 16f; tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Pow(Mathf.Clamp01(1f - d), 1.5f))); }
            tex.Apply();
            var r = go.GetComponent<ParticleSystemRenderer>();
            var m = add ? Additive(name) : new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = name };
            if (!add) { m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f); m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha); m.SetFloat("_ZWrite", 0f); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent; }
            m.SetTexture("_BaseMap", tex); r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return ps;
        }

        static bool Playing(string id, string artist, string title)
        {
            if (id == null || !OddballSongs.Ids(artist, out var l)) return false;
            foreach (var (t, i) in l) if (i == id && t.StartsWith(title)) return true;
            return false;
        }

        void Update()
        {
            var dt = Time.deltaTime; var t = Time.time;
            var here = SkyDirector.Instance != null && SkyDirector.Instance.CurrentLand == Land;
            var d = MusicDirector.Instance; var playing = d != null && d.CurrentSlot.IsValid;
            var energy = SurpriseDirector.Energy;
            var id = MusicDirector.ForcedTrackId ?? (playing ? d.CurrentSlot.track.id : null);

            ocean = Mathf.MoveTowards(ocean, here && Playing(id, "Ween", "Ocean Man") ? 1f : 0f, dt / 3f);
            mind = Mathf.MoveTowards(mind, here && Playing(id, "Pixies", "Where Is My Mind") ? 1f : 0f, dt / 4f);
            end = Mathf.MoveTowards(end, here && Playing(id, "R.E.M.", "It's The End") ? 1f : 0f, dt / 3f);
            blister = Mathf.MoveTowards(blister, here && Playing(id, "Violent Femmes", "Blister") ? 1f : 0f, dt / 2f);
            yoshimi = Mathf.MoveTowards(yoshimi, here && Playing(id, "The Flaming Lips", "Yoshimi") ? 1f : 0f, dt / 3f);

            foreach (var (s, _, _) in songGlows) if (!songAmount.ContainsKey(s)) songAmount[s] = 0f;
            var keys = new List<string>(songAmount.Keys);
            foreach (var s in keys) songAmount[s] = Mathf.MoveTowards(songAmount[s], here && id == s ? 1f : 0f, dt / 2f);
            foreach (var (s, m, c) in songGlows) if (m != null) m.SetColor("_EmissionColor", c * (1f + songAmount[s] * (2.5f + Mathf.Sin(t * 2.2f) * .6f)));

            // the sky: a hazy gold-green summer, and whatever the songs want
            var tint = new Color(1f, .92f, .65f); var amount = .1f;
            if (ocean > 0f) { tint = Color.Lerp(tint, new Color(.3f, .65f, 1f), ocean); amount = Mathf.Lerp(amount, .45f, ocean); }
            if (mind > 0f) { tint = Color.Lerp(tint, new Color(.6f, .5f, 1f), mind); amount = Mathf.Lerp(amount, .45f, mind); }
            if (end > 0f) { tint = Color.Lerp(tint, new Color(1f, .35f, .2f), end); amount = Mathf.Lerp(amount, .5f, end); }
            if (blister > 0f) { tint = Color.Lerp(tint, new Color(1f, 1f, .85f), blister); amount = Mathf.Lerp(amount, .5f, blister); }
            if (yoshimi > 0f) { tint = Color.Lerp(tint, new Color(1f, .5f, .75f), yoshimi); amount = Mathf.Lerp(amount, .45f, yoshimi); }
            SkyDirector.SetSongTint(Land, tint, amount);
            var wind = clouds != null ? clouds.Wind : new Vector2(1, 0);
            if (here) { Shader.SetGlobalFloat("_EW_Wind", .45f + .3f * energy); Shader.SetGlobalVector("_EW_WindDir", new Vector4(wind.x, 0, wind.y, 0)); }

            var cam = Camera.main != null ? Camera.main.transform.position : center;
            // the head's eyes follow you (only so far: they're fibreglass)
            if (eyes != null)
            {
                var want = Quaternion.LookRotation((cam - eyes.position).normalized);
                var limited = Quaternion.RotateTowards(eyesRest, want, 55f);
                eyes.rotation = Quaternion.Slerp(eyes.rotation, here ? limited : eyesRest, dt * 2f);
            }

            if (!here) return;
            nextFluff -= dt;
            if (nextFluff <= 0f) { nextFluff = .08f; fluff.transform.position = cam + new Vector3(Random.Range(-25f, 25f), Random.Range(1f, 8f), Random.Range(-25f, 25f)); fluff.Emit(1); }
            nextStreak -= dt;
            if (end > .3f && nextStreak <= 0f)
            {
                nextStreak = Random.Range(.4f, 1.2f);
                var from = cam + new Vector3(Random.Range(-200f, 200f), 160f, Random.Range(-200f, 200f)); var dir = (new Vector3(Random.Range(-1f, 1f), -1.4f, Random.Range(-1f, 1f))).normalized;
                streaks.transform.SetPositionAndRotation(from, Quaternion.LookRotation(dir)); var sh = streaks.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 1f; sh.radius = .1f; streaks.Emit(1);
            }
        }
    }

    /// Looks up the Oddball Backroads' songs from outside the kit.
    public static class OddballSongs
    {
        public static bool Ids(string artist, out (string title, string id)[] list) => OddballKit.SongTable.TryGetValue(artist, out list);
    }
}
