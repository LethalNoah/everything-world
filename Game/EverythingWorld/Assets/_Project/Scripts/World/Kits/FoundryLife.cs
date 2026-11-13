using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Electro Foundry on the night shift, all on one 128 bpm beat from the shared clock:
    /// - robot arms swing and drop on the beat; crates ride the conveyors; welders and the robots' tools throw sparks; the
    ///   gantry crane travels up and down the Line; beacons turn; smokestacks smoke; the furnace breathes; the neon tubes,
    ///   the synth wall's patch lights and meganeko's pixel wall pulse; pulling the lever runs everything at double speed;
    /// - "Atomic": a white flash on every beat. "Sharkatzor": blue and pink racing. "Astro Soda": fizz rises. "Apollo":
    ///   a launch-orange glow. "Super Game": rainbow. "Globe Warmer": hot orange. "Halo": white. "Imaginationland": pastel
    ///   colours drift. "Luvu": pink pulses. Every song's own light brightens while it plays.
    public class FoundryLife : MonoBehaviour
    {
        const string Land = "The Electro Foundry";
        public Vector3 center, craneA, craneB, sodaAt, dripAt;
        public string apolloSong, gameSong, goSong, sharkSong, sodaSong, atomSong, warmSong, haloSong, imagSong, luvuSong;
        public Material greens, blues, furnace, beacons, lamps, patch, pixelWall, pixelWall2; public float played = -99f;
        public List<(Transform arm, Transform fore, float ph)> arms = new List<(Transform, Transform, float)>();
        public List<Transform> weldTips = new List<Transform>(), beacons2 = new List<Transform>(), hearts = new List<Transform>();
        public List<Vector3> welds = new List<Vector3>(), smoke = new List<Vector3>();
        public List<(Transform t, Vector3 a, Vector3 b, float ph)> crates = new List<(Transform, Vector3, Vector3, float)>();
        public Transform lever, crane, pitch; public Quaternion leverRot, craneRot;
        public List<(string song, Material m, Color c)> songGlows = new List<(string, Material, Color)>();
        readonly Dictionary<string, float> songAmount = new Dictionary<string, float>();

        CloudLayer clouds; ParticleSystem dots;
        Color greenBase, blueBase, furnaceBase, beaconBase, lampBase, patchBase, wallBase, wall2Base;
        Quaternion pitchBase; bool pitchSet; float apollo, game, go, shark, soda, atom, warm, halo, imag, luvu, nextSpark, nextFall, beatPos;

        void Start()
        {
            clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 24; clouds.span = 2400f; clouds.key = "foundry";
            clouds.minHeight = 100f; clouds.maxHeight = 180f; clouds.size = 3.6f; clouds.speed = .9f; clouds.cover = .45f;
            clouds.color = new Color(.3f, .34f, .3f);
            Color E(Material m) => m != null ? m.GetColor("_EmissionColor") : Color.black;
            greenBase = E(greens); blueBase = E(blues); furnaceBase = E(furnace); beaconBase = E(beacons); lampBase = E(lamps); patchBase = E(patch); wallBase = E(pixelWall); wall2Base = E(pixelWall2);
            dots = Particles("Sparks, smoke and fizz", 1600, Color.white, .04f, .14f, true);
            var dm = dots.main; dm.startLifetime = new ParticleSystem.MinMaxCurve(1f, 4f);
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

            apollo = Ease(apollo, On(apolloSong), 4f); game = Ease(game, On(gameSong), 3f); go = Ease(go, On(goSong), 3f); shark = Ease(shark, On(sharkSong), 2f); soda = Ease(soda, On(sodaSong), 3f); atom = Ease(atom, On(atomSong), 2f); warm = Ease(warm, On(warmSong), 4f); halo = Ease(halo, On(haloSong), 4f); imag = Ease(imag, On(imagSong), 4f); luvu = Ease(luvu, On(luvuSong), 2f);

            foreach (var (s, _, _) in songGlows) if (!songAmount.ContainsKey(s)) songAmount[s] = 0f;
            var keys = new List<string>(songAmount.Keys);
            foreach (var s in keys) songAmount[s] = Mathf.MoveTowards(songAmount[s], here && id == s ? 1f : 0f, dt / 2f);
            foreach (var (s, m, c) in songGlows) if (m != null) m.SetColor("_EmissionColor", c * (1f + songAmount[s] * (2.5f + Mathf.Sin(t * 2.4f) * .6f)));

            // the beat: 128 bpm, double time after the lever
            var rush = t - played < 15f ? 1f : 0f; var rate = 128f / 60f * (1f + rush);
            beatPos = now * rate; var beat = Mathf.Repeat(beatPos, 1f); var kick = Mathf.Exp(-beat * 6f); var bar = Mathf.FloorToInt(beatPos / 4f);
            foreach (var (arm, fore, ph) in arms) { if (arm == null) continue; var step = Mathf.Floor(beatPos + ph); var target = Mathf.Sin(step * 1.7f + ph) * 60f; var drop = Mathf.Exp(-beat * 8f); arm.rotation = Quaternion.Euler(0, target + Mathf.Sin(beatPos * .5f + ph) * 10f, 0); if (fore != null) fore.localRotation = Quaternion.Euler(20f + drop * 40f, 0, 0); }
            foreach (var (tr, a, b, ph) in crates) if (tr != null) tr.position = Vector3.Lerp(a, b, Mathf.Repeat(now * .04f * (1f + rush) + ph, 1f));
            for (var i = 0; i < beacons2.Count; i++) if (beacons2[i] != null) beacons2[i].rotation = Quaternion.Euler(0, t * (240f + rush * 240f) + i * 90f, 0);
            if (crane != null) { crane.position = Vector3.Lerp(craneA, craneB, Mathf.SmoothStep(0f, 1f, Mathf.PingPong(now / 30f, 1f))); crane.rotation = craneRot; }
            if (lever != null) lever.rotation = leverRot * Quaternion.Euler(rush > 0f ? 30f : -30f, 0, 0);
            if (pitch != null) { if (!pitchSet) { pitchBase = pitch.rotation; pitchSet = true; } pitch.rotation = pitchBase * Quaternion.Euler(0, 0, Mathf.Sin(t * .8f) * 25f); }
            foreach (var h in hearts) if (h != null) h.localScale = Vector3.one * (1f + kick * (.15f + luvu * .3f));

            if (greens != null) greens.SetColor("_EmissionColor", greenBase * (.6f + kick * .7f + rush * .4f + go * .5f));
            if (blues != null) blues.SetColor("_EmissionColor", blueBase * (.6f + (bar % 2 == 0 ? kick : 1f - kick) * .7f + rush * .4f));
            if (furnace != null) furnace.SetColor("_EmissionColor", furnaceBase * (.85f + .15f * Mathf.PerlinNoise(t * 2f, 0f) + warm * .6f + apollo * .4f));
            if (beacons != null) beacons.SetColor("_EmissionColor", beaconBase * (Mathf.Repeat(t * 2f, 1f) < .5f ? 1.2f : .5f));
            if (lamps != null) lamps.SetColor("_EmissionColor", lampBase * (Mathf.PerlinNoise(t * 7f, 5f) > .94f ? .4f : 1f) * (1f + halo * .6f + atom * (beat < .1f ? 2f : 0f)));
            if (patch != null) patch.SetColor("_EmissionColor", patchBase * (.5f + Mathf.Repeat(beatPos * .25f + .3f, 1f) + go));
            var race = 1f + shark * 4f + energy;
            if (pixelWall != null) pixelWall.SetColor("_EmissionColor", wallBase * (Mathf.PerlinNoise(t * race, 1f) > .4f ? 1.2f : .3f));
            if (pixelWall2 != null) pixelWall2.SetColor("_EmissionColor", wall2Base * (Mathf.PerlinNoise(t * race, 7f) > .4f ? 1.2f : .3f));

            var tint = new Color(.5f, 1f, .7f); var amount = .08f + kick * .03f;
            if (warm > 0f) { tint = Color.Lerp(tint, new Color(1f, .5f, .2f), warm); amount = Mathf.Lerp(amount, .4f, warm); }
            if (apollo > 0f) { tint = Color.Lerp(tint, new Color(1f, .6f, .3f), apollo); amount = Mathf.Lerp(amount, .36f, apollo); }
            if (halo > 0f) { tint = Color.Lerp(tint, new Color(.9f, .95f, 1f), halo); amount = Mathf.Lerp(amount, .34f, halo); }
            if (imag > 0f) { tint = Color.Lerp(tint, Color.HSVToRGB(Mathf.Repeat(now * .05f, 1f), .35f, 1f), imag); amount = Mathf.Lerp(amount, .32f, imag); }
            if (game > 0f) { tint = Color.Lerp(tint, Color.HSVToRGB(Mathf.Repeat(now * .3f, 1f), .7f, 1f), game); amount = Mathf.Lerp(amount, .34f, game); }
            if (shark > 0f) { tint = Color.Lerp(tint, Mathf.Repeat(beatPos * .5f, 1f) < .5f ? new Color(.3f, .6f, 1f) : new Color(1f, .3f, .7f), shark); amount = Mathf.Lerp(amount, .38f, shark); }
            if (luvu > 0f) { tint = Color.Lerp(tint, new Color(1f, .3f, .6f), luvu); amount = Mathf.Lerp(amount, .3f + kick * .2f, luvu); }
            if (atom > 0f) { tint = Color.Lerp(tint, Color.white, atom * (beat < .1f ? 1f : .2f)); amount = Mathf.Lerp(amount, beat < .1f ? .55f : .25f, atom); }
            SkyDirector.SetSongTint(Land, tint, amount);
            var wind = clouds != null ? clouds.Wind : new Vector2(1, 0);
            if (here) { Shader.SetGlobalFloat("_EW_Wind", .25f + .15f * energy + rush * .2f); Shader.SetGlobalVector("_EW_WindDir", new Vector4(wind.x, 0, wind.y, 0)); }

            var cam = Camera.main; if (!here || cam == null) return; var cp = cam.transform.position;
            if (t >= nextSpark)
            {
                nextSpark = t + (rush > 0f ? .05f : .1f);
                foreach (var w in welds) if ((w - cp).sqrMagnitude < 45f * 45f && Random.value < .6f) for (var k = 0; k < 3; k++) dots.Emit(new ParticleSystem.EmitParams { position = w, velocity = new Vector3(Random.Range(-2f, 2f), Random.Range(1f, 3f), Random.Range(-2f, 2f)), startColor = new Color(1f, Random.Range(.5f, .9f), .2f), startSize = .05f, startLifetime = .8f }, 1);
                foreach (var tip in weldTips) if (tip != null && beat < .15f && (tip.position - cp).sqrMagnitude < 40f * 40f) { var tp = tip.position + tip.forward * tip.lossyScale.z * 2.2f; dots.Emit(new ParticleSystem.EmitParams { position = tp, velocity = new Vector3(Random.Range(-1.5f, 1.5f), Random.Range(.5f, 2f), Random.Range(-1.5f, 1.5f)), startColor = new Color(.6f, 1f, .7f), startSize = .05f, startLifetime = .6f }, 2); }
                foreach (var sm in smoke) if ((sm - cp).sqrMagnitude < 120f * 120f && Random.value < .4f) dots.Emit(new ParticleSystem.EmitParams { position = sm, velocity = new Vector3(wind.x * .8f, Random.Range(.8f, 1.6f), wind.y * .8f), startColor = new Color(.4f, .42f, .4f, .25f), startSize = Random.Range(2f, 3.4f), startLifetime = 6f }, 1);
                if (soda > .05f && sodaAt != Vector3.zero && (sodaAt - cp).sqrMagnitude < 40f * 40f) dots.Emit(new ParticleSystem.EmitParams { position = sodaAt + new Vector3(Random.Range(-.4f, .4f), 0, Random.Range(-.4f, .4f)), velocity = new Vector3(0, Random.Range(1f, 2.4f), 0), startColor = new Color(.7f, 1f, 1f, .8f), startSize = .08f, startLifetime = 2f }, 2);
                if (dripAt != Vector3.zero && (dripAt - cp).sqrMagnitude < 30f * 30f && Random.value < .3f) dots.Emit(new ParticleSystem.EmitParams { position = dripAt, velocity = Vector3.down * 2f, startColor = new Color(.4f, 1f, .4f), startSize = .06f, startLifetime = .8f }, 1);
            }
            if (t >= nextFall)
            {
                nextFall = t + .06f;
                var p = cp + new Vector3(Random.Range(-14f, 14f), Random.Range(.5f, 4f), Random.Range(-14f, 14f));
                if (Random.value < .12f) dots.Emit(new ParticleSystem.EmitParams { position = p, velocity = new Vector3(Random.Range(-.2f, .2f), Random.Range(-.05f, .2f), Random.Range(-.2f, .2f)), startColor = new Color(1f, .6f, .2f, .6f), startSize = .04f, startLifetime = 3f }, 1);
            }
        }
    }
}
