using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Prog Marches under the eclipse, answering the music:
    /// - the eclipse hangs low in the sky where the sun should be, a black disc in a blazing corona; the spectrum
    ///   shimmers; the gears turn and the fallen clock keeps the real time; organ pipes glow with the music's energy;
    ///   the runes on the stones pulse in 7/8; the monoliths float and turn; the pig drifts between its chimneys;
    ///   the saucers hover; the searchlight sweeps; the beacon on the Rush mast flashes Y-Y-Z in Morse;
    /// - "Time": the gears race, the clock hands spin and the runes flash on every beat; "Mr. Blue Sky": the eclipse
    ///   passes and the whole sky turns bright blue; "Wish You Were Here": the burning man blazes; "Any Colour You
    ///   Like": the spectrum and the organ pipes cycle through every colour; "YYZ": the beacon flashes fast and the
    ///   light pulses red; "Inca Roads": the saucer drops low over the terraces, lights blazing.
    public class ProgLife : MonoBehaviour
    {
        const string Land = "The Prog Marches";
        public Vector3 center, prism, beamDir = Vector3.forward;
        public List<Material> spectrum = new List<Material>(), pipes = new List<Material>();
        public List<Vector3> runes = new List<Vector3>(), organs = new List<Vector3>();
        public List<Transform> gears = new List<Transform>(), floaters = new List<Transform>();
        public List<float> gearSpeed = new List<float>(); public List<Vector3> floatBase = new List<Vector3>();
        public Vector3 clockCenter; public Quaternion clockRot = Quaternion.identity; public Transform hourHand, minuteHand;
        public Transform pig, ufo, saucer, searchlight; public Vector3 pigBase, ufoBase, saucerBase, burning, beaconAt;
        public Material beacon, neon;

        CloudLayer clouds; ParticleSystem fire, motes, runeSpark;
        Transform eclipse, corona; Material coronaM, discM;
        float time78, blueSky, wish, colours, yyz, inca;
        float nextFire, nextMote, nextRune; int lastBeat = -1;
        // Y = -.--  Y = -.--  Z = --..  (dot 1 unit, dash 3, gap in a letter 1, between letters 3, then a long rest)
        static readonly bool[] Morse = Build("-.--", "-.--", "--..");
        static bool[] Build(params string[] letters)
        {
            var l = new List<bool>();
            foreach (var w in letters) { foreach (var ch in w) { for (var k = 0; k < (ch == '-' ? 3 : 1); k++) l.Add(true); l.Add(false); } l.Add(false); l.Add(false); }
            for (var k = 0; k < 6; k++) l.Add(false);
            return l.ToArray();
        }

        void Start()
        {
            clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 26; clouds.span = 2400f; clouds.key = "prog";
            clouds.minHeight = 150f; clouds.maxHeight = 220f; clouds.size = 2.2f; clouds.speed = 1.5f; clouds.cover = .25f;
            clouds.color = new Color(.55f, .45f, .7f);
            fire = Particles("Burning man", 400, new Color(1f, .55f, .15f, .9f), .3f, .7f, true);
            var fm = fire.main; fm.startLifetime = new ParticleSystem.MinMaxCurve(.4f, 1f); fm.startSpeed = new ParticleSystem.MinMaxCurve(1f, 2.4f);
            motes = Particles("Spectrum motes", 500, Color.white, .1f, .25f, true);
            var mm = motes.main; mm.startLifetime = new ParticleSystem.MinMaxCurve(3f, 6f); mm.startSpeed = new ParticleSystem.MinMaxCurve(.2f, .8f);
            runeSpark = Particles("Rune sparks", 300, new Color(.7f, .45f, 1f, 1f), .15f, .35f, true);
            var rm = runeSpark.main; rm.startLifetime = new ParticleSystem.MinMaxCurve(.6f, 1.2f); rm.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3f);
            // the eclipse: a black disc in front of a blazing corona, kept low in the sky round the camera
            coronaM = new Material(WorldMaterials.Glow(new Color(1f, .82f, .5f), 2.4f)) { name = "Corona" };
            discM = WorldMaterials.Solid(new Color(.02f, .02f, .03f));
            var ck = new MeshKit();
            var rays = new System.Random(1973);
            for (var k = 0; k < 72; k++) { var a = k * 5f; var len = 1.25f + (float)rays.NextDouble() * (k % 4 == 0 ? 1.4f : .6f); ck.Tri(coronaM, Vector3.zero, Quaternion.Euler(0, 0, a - 1.5f) * Vector3.up * 1.05f, Quaternion.Euler(0, 0, a + 2.5f) * Vector3.up * len, Vector3.back); ck.Tri(coronaM, Vector3.zero, Quaternion.Euler(0, 0, a + 2.5f) * Vector3.up * len, Quaternion.Euler(0, 0, a + 6.5f) * Vector3.up * 1.05f, Vector3.back); }   // faces +z, toward the camera
            corona = ck.BuildSingle("Corona", transform, false).transform;
            var dk = new MeshKit(); dk.Prism(discM, Vector3.zero, Vector3.back, 1f, .01f, 40);
            eclipse = dk.BuildSingle("Eclipse", transform, false).transform;
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
            var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, .2f), new GradientAlphaKey(0f, 1f) });
            cl.color = g;
            var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (var y = 0; y < 32; y++) for (var x = 0; x < 32; x++) { var d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(16, 16)) / 16f; tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Pow(Mathf.Clamp01(1f - d), 1.5f))); }
            tex.Apply();
            var r = go.GetComponent<ParticleSystemRenderer>();
            var m = add ? Additive(name) : new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = name };
            m.SetTexture("_BaseMap", tex); r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return ps;
        }

        void Update()
        {
            var dt = Time.deltaTime; var t = Time.time; var now = SharedChance.Now;
            var here = SkyDirector.Instance != null && SkyDirector.Instance.CurrentLand == Land;
            var d = MusicDirector.Instance; var playing = d != null && d.CurrentSlot.IsValid;
            var energy = SurpriseDirector.Energy;
            var id = MusicDirector.ForcedTrackId ?? (playing ? d.CurrentSlot.track.id : null);
            time78 = Mathf.MoveTowards(time78, here && id == ProgKit.TimeSong ? 1f : 0f, dt / 2f);
            blueSky = Mathf.MoveTowards(blueSky, here && id == ProgKit.BlueSky ? 1f : 0f, dt / 4f);
            wish = Mathf.MoveTowards(wish, here && id == ProgKit.WishYouWereHere ? 1f : 0f, dt / 2f);
            colours = Mathf.MoveTowards(colours, here && id == ProgKit.AnyColour ? 1f : 0f, dt / 2f);
            yyz = Mathf.MoveTowards(yyz, here && id == ProgKit.YYZ ? 1f : 0f, dt / 2f);
            inca = Mathf.MoveTowards(inca, here && id == ProgKit.IncaRoads ? 1f : 0f, dt / 3f);

            var tint = new Color(.85f, .75f, 1f); var amount = .12f;
            if (blueSky > 0f) { tint = Color.Lerp(tint, new Color(.55f, .78f, 1f), blueSky); amount = Mathf.Lerp(amount, .7f, blueSky); }
            if (wish > 0f) { tint = Color.Lerp(tint, new Color(1f, .6f, .3f), wish * .6f); amount = Mathf.Max(amount, .35f * wish); }
            if (yyz > 0f) { var pulse = .5f + .5f * Mathf.Sin(t * 9f); tint = Color.Lerp(tint, new Color(1f, .25f, .2f), yyz * pulse * .7f); amount = Mathf.Max(amount, .4f * yyz * pulse); }
            if (time78 > 0f) { tint = Color.Lerp(tint, new Color(1f, .75f, .35f), time78 * .5f); amount = Mathf.Max(amount, .25f * time78); }
            SkyDirector.SetSongTint(Land, tint, amount);
            SkyDirector.SetSongRain(Land, 0f);
            if (here) { var w = clouds != null ? clouds.Wind : new Vector2(1, 0); Shader.SetGlobalFloat("_EW_Wind", .3f + .25f * energy); Shader.SetGlobalVector("_EW_WindDir", new Vector4(w.x, 0, w.y, 0)); }

            // the eclipse
            var cam = Camera.main;
            if (eclipse != null)
            {
                var show = here && cam != null;
                eclipse.gameObject.SetActive(show && blueSky < .95f); corona.gameObject.SetActive(show);
                if (show)
                {
                    var sun = RenderSettings.sun != null ? -RenderSettings.sun.transform.forward : new Vector3(-.6f, .25f, .75f).normalized;
                    if (sun.y < .12f) { sun.y = .12f; sun.Normalize(); }
                    var pos = cam.transform.position + sun * 900f; var rot = Quaternion.LookRotation(-sun);
                    var size = 46f;
                    eclipse.SetPositionAndRotation(pos - sun * 2f, rot); eclipse.localScale = new Vector3(size, size, 1f) * (1f - blueSky);
                    corona.SetPositionAndRotation(pos, rot * Quaternion.Euler(0, 0, t * 2f)); corona.localScale = new Vector3(size, size, 1f) * (1f + .05f * Mathf.Sin(t * .7f) + blueSky * .3f);
                    coronaM.SetColor("_EmissionColor", Color.Lerp(new Color(1f, .8f, .5f) * 4f, new Color(1f, .97f, .85f) * 6f, blueSky));
                }
            }

            // the spectrum and the organ pipes
            for (var k = 0; k < spectrum.Count; k++)
            {
                var baseCol = ProgKit.Spectrum[k % ProgKit.Spectrum.Length]; var cyc = Color.HSVToRGB(Mathf.Repeat(t * .15f + k / 6f, 1f), 1f, 1f);
                spectrum[k].SetColor("_EmissionColor", Color.Lerp(baseCol, cyc, colours) * (1.2f + .25f * Mathf.Sin(t * 1.3f + k) + energy * .4f + colours * .8f));
            }
            for (var k = 0; k < pipes.Count; k++)
            {
                var c = Color.Lerp(ProgKit.Amber, Color.HSVToRGB(Mathf.Repeat(t * .2f + k * .3f, 1f), .9f, 1f), colours);
                pipes[k].SetColor("_EmissionColor", c * (.4f + energy * 1.8f + .3f * Mathf.Sin(t * 2f + k)));
            }
            // the gears and the clock
            for (var k = 0; k < gears.Count; k++) gears[k].Rotate(Vector3.forward, gearSpeed[k] * dt * (1f + energy + time78 * 6f), Space.Self);
            if (hourHand != null)
            {
                var clock = System.DateTime.Now; var hrs = (clock.Hour % 12 + clock.Minute / 60f) * 30f; var mins = (clock.Minute + clock.Second / 60f) * 6f;
                if (time78 > 0f) { hrs += t * 200f * time78; mins += t * 2400f * time78; }
                hourHand.SetPositionAndRotation(clockCenter, clockRot * Quaternion.Euler(0, 0, -hrs)); minuteHand.SetPositionAndRotation(clockCenter + clockRot * Vector3.forward * .03f, clockRot * Quaternion.Euler(0, 0, -mins));
            }
            // runes pulse on the beat in 7/8: bar of 7 eighths at about 150 bpm
            var beat = Mathf.FloorToInt((float)(now * 5f));
            if (here && beat != lastBeat && runes.Count > 0)
            {
                lastBeat = beat; var b = beat % runes.Count;
                if (time78 > .2f || beat % 7 == 0) { runeSpark.transform.position = runes[b]; runeSpark.Emit(time78 > .2f ? 10 : 4); }
            }
            // floaters, pig, saucers, searchlight
            for (var k = 0; k < floaters.Count; k++) floaters[k].SetPositionAndRotation(floatBase[k] + Vector3.up * Mathf.Sin(t * .4f + k * 1.3f) * .8f, Quaternion.Euler(0, t * (6f + k * 2f), Mathf.Sin(t * .3f + k) * 4f));
            if (pig != null) pig.SetPositionAndRotation(pigBase + new Vector3(Mathf.Sin(t * .15f) * 3f, Mathf.Sin(t * .4f) * .8f, Mathf.Cos(t * .12f) * 2f), Quaternion.Euler(Mathf.Sin(t * .5f) * 5f, t * 4f, Mathf.Sin(t * .3f) * 6f));
            if (ufo != null) { var low = Mathf.SmoothStep(0f, 1f, inca); ufo.SetPositionAndRotation(ufoBase + Vector3.up * (Mathf.Sin(t * .7f) * .6f - low * 5f), Quaternion.Euler(Mathf.Sin(t * .5f) * 4f, t * (20f + inca * 120f), 0)); }
            if (saucer != null) saucer.SetPositionAndRotation(saucerBase + Vector3.up * Mathf.Sin(t * .6f) * .7f, Quaternion.Euler(Mathf.Sin(t * .4f) * 3f, t * 15f, Mathf.Cos(t * .4f) * 3f));
            if (neon != null) neon.SetColor("_EmissionColor", new Color(.3f, .7f, 1f) * (1.2f + energy + blueSky * 3f));
            if (searchlight != null) searchlight.rotation = Quaternion.Euler(-35f + Mathf.Sin(t * .3f) * 15f, t * 25f, 0);
            if (beacon != null)
            {
                var unit = yyz > .2f ? .09f : .18f; var i = Mathf.FloorToInt((float)(now / unit)) % Morse.Length;
                beacon.SetColor("_EmissionColor", new Color(1f, .15f, .1f) * (Morse[i] ? 5f : .15f));
            }

            nextFire -= dt;
            if (here && burning != Vector3.zero && nextFire <= 0f) { nextFire = wish > .1f ? .02f : .12f; fire.transform.position = burning + new Vector3(Random.Range(-.25f, .25f), Random.Range(-.6f, .9f), Random.Range(-.25f, .25f)); fire.Emit(wish > .1f ? 3 : 1); }
            nextMote -= dt;
            if (here && nextMote <= 0f && spectrum.Count > 0)
            {
                nextMote = .05f; var k = Random.Range(0, 6); var along = Random.Range(10f, 120f); var side = Vector3.Cross(Vector3.up, beamDir);
                motes.transform.position = prism + beamDir * along + side * ((k - 2.5f) * .045f * along) + Vector3.down * along * .1f;
                var mmain = motes.main; mmain.startColor = ProgKit.Spectrum[k]; motes.Emit(1);
            }
        }
    }
}
