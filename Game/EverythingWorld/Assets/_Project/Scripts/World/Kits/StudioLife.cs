using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// Studio Lot Island at dusk:
    /// - the searchlights sweep the sky; the studio cart drives up and down Backlot Avenue; the ON AIR lights blink now and
    ///   then; the Tank's water shimmers; the campfire flickers and sends up sparks;
    /// - "Can You Feel the Love Tonight": night falls deep blue and the stars come out (sparkles drift down). "Be Prepared":
    ///   a sickly green. "Let It Go": ice blue and snow. "Under the Sea": bubbles rise all around. "Circle of Life": a golden
    ///   sunrise. "Rainbow Connection": soft rainbow colours drift by. "Pirates of the Caribbean": a storm over the Tank, wind
    ///   and lightning. "It's a Small World": pastel colours cycle. "Jai Ho": saffron and a shower of marigold petals. Every
    ///   song's own light brightens while it plays.
    public class StudioLife : MonoBehaviour
    {
        const string Land = "Studio Lot Island";
        public Vector3 center, tank, cartA, cartB, cartSide;
        public string loveSong, prepSong, letItSong, seaSong, circleSong, rainbowSong, pirateSong, worldSong, jaiSong;
        public Material lamps, onAir, sea, windows; public Transform cart; public float played = -99f;
        public List<Transform> searchlights = new List<Transform>();
        public List<Vector3> fires = new List<Vector3>();
        public List<(string song, Material m, Color c)> songGlows = new List<(string, Material, Color)>();
        readonly Dictionary<string, float> songAmount = new Dictionary<string, float>();

        CloudLayer clouds; ParticleSystem dots;
        Color lampBase, onAirBase, seaBase, windowBase;
        float love, prep, letIt, under, circle, rainbow, pirate, world, jai, nextFall, nextSpark, flash;
        static readonly Color[] Pastels = { new Color(1f, .7f, .8f), new Color(.7f, 1f, .8f), new Color(.7f, .8f, 1f), new Color(1f, .94f, .6f) };

        void Start()
        {
            clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 30; clouds.span = 2400f; clouds.key = "studiolot";
            clouds.minHeight = 130f; clouds.maxHeight = 220f; clouds.size = 3.2f; clouds.speed = 1f; clouds.cover = .35f;
            clouds.color = new Color(1f, .8f, .7f);
            Color E(Material m) => m != null ? m.GetColor("_EmissionColor") : Color.black;
            lampBase = E(lamps); onAirBase = E(onAir); seaBase = E(sea); windowBase = E(windows);
            dots = Particles("Sparks, stars and petals", 1200, Color.white, .06f, .2f, true);
            var dm = dots.main; dm.startLifetime = new ParticleSystem.MinMaxCurve(3f, 7f);
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

            love = Ease(love, On(loveSong), 5f); prep = Ease(prep, On(prepSong), 3f); letIt = Ease(letIt, On(letItSong), 4f); under = Ease(under, On(seaSong), 3f); circle = Ease(circle, On(circleSong), 6f); rainbow = Ease(rainbow, On(rainbowSong), 4f); pirate = Ease(pirate, On(pirateSong), 4f); world = Ease(world, On(worldSong), 3f); jai = Ease(jai, On(jaiSong), 2f);

            foreach (var (s, _, _) in songGlows) if (!songAmount.ContainsKey(s)) songAmount[s] = 0f;
            var keys = new List<string>(songAmount.Keys);
            foreach (var s in keys) songAmount[s] = Mathf.MoveTowards(songAmount[s], here && id == s ? 1f : 0f, dt / 2f);
            foreach (var (s, m, c) in songGlows) if (m != null) m.SetColor("_EmissionColor", c * (1f + songAmount[s] * (2.5f + Mathf.Sin(t * 2.4f) * .6f)));

            // the searchlights sweep in slow figures of eight; the cart drives up and down the Avenue
            for (var i = 0; i < searchlights.Count; i++) { var s = searchlights[i]; if (s == null) continue; var yaw = now * 9f + i * 140f + Mathf.Sin(now * .13f + i) * 40f; var pitch = -55f - Mathf.Sin(now * .21f + i * 2f) * 20f; s.rotation = Quaternion.Euler(pitch, yaw, 0f); }
            if (cart != null) { var ph = Mathf.PingPong(now / 40f, 1f); var dir = Mathf.Repeat(now / 40f, 2f) < 1f ? 1f : -1f; var p = Vector3.Lerp(cartA, cartB, Mathf.SmoothStep(0f, 1f, ph)) + cartSide * (dir > 0 ? 0f : -4.8f); cart.position = p; var fwd = (cartB - cartA); fwd.y = 0; cart.rotation = Quaternion.LookRotation(fwd.normalized * dir); }

            var lift = t - played < 5f ? 1f - (t - played) / 5f : 0f;
            if (lamps != null) lamps.SetColor("_EmissionColor", lampBase * (1f + lift * .5f + .15f * energy));
            if (onAir != null) onAir.SetColor("_EmissionColor", onAirBase * (Mathf.Repeat(now / 6f, 1f) < .8f ? 1f : .15f));
            if (sea != null) sea.SetColor("_EmissionColor", seaBase * (.8f + .2f * Mathf.Sin(t * 1.4f) + under * .8f + pirate * .3f));
            if (windows != null) windows.SetColor("_EmissionColor", windowBase * (1f + love * .4f));

            // lightning over the Tank in the pirate storm
            if (pirate > .3f && Random.value < dt * .4f * pirate) flash = 1f; flash = Mathf.MoveTowards(flash, 0f, dt * 4f);

            var tint = new Color(1f, .8f, .66f); var amount = .1f;
            if (circle > 0f) { tint = Color.Lerp(tint, new Color(1f, .7f, .3f), circle); amount = Mathf.Lerp(amount, .4f, circle); }
            if (jai > 0f) { tint = Color.Lerp(tint, new Color(1f, .56f, .14f), jai); amount = Mathf.Lerp(amount, .4f, jai); }
            if (world > 0f) { tint = Color.Lerp(tint, Pastels[Mathf.FloorToInt(Mathf.Repeat(now / 2f, 4f))], world); amount = Mathf.Lerp(amount, .3f, world); }
            if (rainbow > 0f) { tint = Color.Lerp(tint, Color.HSVToRGB(Mathf.Repeat(now * .05f, 1f), .5f, 1f), rainbow); amount = Mathf.Lerp(amount, .3f, rainbow); }
            if (under > 0f) { tint = Color.Lerp(tint, new Color(.3f, .7f, .9f), under); amount = Mathf.Lerp(amount, .4f, under); }
            if (prep > 0f) { tint = Color.Lerp(tint, new Color(.4f, .9f, .2f), prep); amount = Mathf.Lerp(amount, .42f, prep); }
            if (letIt > 0f) { tint = Color.Lerp(tint, new Color(.6f, .8f, 1f), letIt); amount = Mathf.Lerp(amount, .42f, letIt); }
            if (love > 0f) { tint = Color.Lerp(tint, new Color(.2f, .26f, .6f), love); amount = Mathf.Lerp(amount, .5f, love); }
            if (pirate > 0f) { tint = Color.Lerp(tint, Color.Lerp(new Color(.24f, .3f, .4f), new Color(.9f, .94f, 1f), flash), pirate); amount = Mathf.Lerp(amount, .46f + flash * .2f, pirate); }
            SkyDirector.SetSongTint(Land, tint, amount);
            var wind = clouds != null ? clouds.Wind : new Vector2(1, 0);
            if (here) { Shader.SetGlobalFloat("_EW_Wind", .25f + .15f * energy + pirate * 1f + letIt * .4f); Shader.SetGlobalVector("_EW_WindDir", new Vector4(wind.x, 0, wind.y, 0)); }

            var cam = Camera.main; if (!here || cam == null) return; var cp = cam.transform.position;
            if (t >= nextSpark) { nextSpark = t + .15f; foreach (var f in fires) if ((f - cp).sqrMagnitude < 50f * 50f) dots.Emit(new ParticleSystem.EmitParams { position = f + new Vector3(Random.Range(-.2f, .2f), 0, Random.Range(-.2f, .2f)), velocity = new Vector3(Random.Range(-.2f, .2f), Random.Range(1f, 2f), Random.Range(-.2f, .2f)), startColor = new Color(1f, .6f, .2f), startSize = .06f, startLifetime = 2f }, 1); }
            if (t >= nextFall)
            {
                nextFall = t + .05f;
                var p = cp + new Vector3(Random.Range(-18f, 18f), Random.Range(5f, 10f), Random.Range(-18f, 18f));
                if (IndoorSpaces.Contains(p)) return;
                if (love > .05f && Random.value < love) dots.Emit(new ParticleSystem.EmitParams { position = p + Vector3.up * 4f, velocity = new Vector3(0, -.3f, 0), startColor = new Color(1f, .96f, .8f), startSize = Random.Range(.06f, .14f), startLifetime = 7f }, 1);
                if (letIt > .05f && Random.value < letIt) for (var k = 0; k < 2; k++) dots.Emit(new ParticleSystem.EmitParams { position = cp + new Vector3(Random.Range(-18f, 18f), Random.Range(5f, 9f), Random.Range(-18f, 18f)), velocity = new Vector3(Random.Range(-.3f, .3f), -1f, Random.Range(-.3f, .3f)), startColor = new Color(.9f, .96f, 1f), startSize = .08f, startLifetime = 8f }, 1);
                if (under > .05f && Random.value < under) dots.Emit(new ParticleSystem.EmitParams { position = cp + new Vector3(Random.Range(-14f, 14f), Random.Range(0f, 2f), Random.Range(-14f, 14f)), velocity = new Vector3(Random.Range(-.1f, .1f), Random.Range(.6f, 1.2f), Random.Range(-.1f, .1f)), startColor = new Color(.7f, .95f, 1f, .7f), startSize = Random.Range(.1f, .3f), startLifetime = 6f }, 1);
                var petals = Mathf.Max(jai, lift * .5f); if (Random.value < petals) dots.Emit(new ParticleSystem.EmitParams { position = cp + new Vector3(Random.Range(-14f, 14f), Random.Range(5f, 9f), Random.Range(-14f, 14f)), velocity = new Vector3(Random.Range(-.5f, .5f), -1f, Random.Range(-.5f, .5f)), startColor = Random.value < .5f ? new Color(1f, .6f, .1f) : new Color(1f, .82f, .2f), startSize = .12f }, 1);
                if (pirate > .05f && Random.value < pirate) for (var k = 0; k < 3; k++) dots.Emit(new ParticleSystem.EmitParams { position = cp + new Vector3(Random.Range(-16f, 16f), Random.Range(6f, 10f), Random.Range(-16f, 16f)), velocity = new Vector3(wind.x * 3f, -9f, wind.y * 3f), startColor = new Color(.7f, .8f, .9f, .5f), startSize = .04f, startLifetime = 1.2f }, 1);
            }
        }
    }
}
