using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Docklands at night, answering the music: low dark cloud drifts over; the sodium lamps hum and now and then
    /// one flickers; red lights blink on the cranes and the tower; the lighthouse beam turns over the harbour; mist lies
    /// on the black water; steam breathes from the grates; oil drums burn. Every song's own light brightens while it
    /// plays, and the big songs change the whole harbour (see DocksSongs).
    public class DocksLife : MonoBehaviour
    {
        const string Land = "The Post-Punk Docklands";
        public Vector3 center, oneWindowAt, towerTop, ship, lighthouse, pinkSteam, dish, rod, balloonsFrom;
        public Material sodium, windows, oneWindow, blink; public GameObject lampHalos;
        public List<Vector3> lamps = new List<Vector3>(), blinkers = new List<Vector3>(), craneTops = new List<Vector3>(), fires = new List<Vector3>(), grates = new List<Vector3>();
        public List<(string song, Material m, Color c)> songGlows = new List<(string, Material, Color)>();
        readonly Dictionary<string, float> songAmount = new Dictionary<string, float>();

        CloudLayer clouds; ParticleSystem steam, pink, flame, mist;
        LineRenderer beamA, beamB, bolt; readonly List<LineRenderer> searchlights = new List<LineRenderer>();
        readonly List<Transform> balloons = new List<Transform>(); readonly List<Vector3> balloonOffset = new List<Vector3>(); float balloonT;
        float light, tremolo, red99, rule, sprawl, golden, pinkAmt, thunder;
        float nextSteam, nextPink, nextFlame, nextMist, nextBolt = 2f, boltLeft, flash, flickerT; int flickerLamp = -1;

        void Start()
        {
            clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 60; clouds.span = 2400f; clouds.key = "docklands";
            clouds.minHeight = 90f; clouds.maxHeight = 140f; clouds.size = 2.4f; clouds.speed = 3f; clouds.cover = .55f;
            clouds.color = new Color(.12f, .11f, .12f);
            steam = Particles("Grate steam", 400, new Color(.7f, .66f, .62f, .35f), 1.2f, 2.6f, false);
            var sm = steam.main; sm.startLifetime = new ParticleSystem.MinMaxCurve(3f, 5f); sm.startSpeed = new ParticleSystem.MinMaxCurve(.8f, 1.6f);
            pink = Particles("Pink steam", 500, new Color(1f, .45f, .75f, .6f), 1.2f, 3f, true);
            var pm = pink.main; pm.startLifetime = new ParticleSystem.MinMaxCurve(3f, 5f); pm.startSpeed = new ParticleSystem.MinMaxCurve(.8f, 2f);
            // steam rises: the default cone points sideways, so puffs drifted off level from the grates and hung over the
            // falling ground like dark balls in mid-air
            foreach (var ps in new[] { steam, pink }) { var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 10f; sh.radius = .4f; sh.rotation = new Vector3(-90f, 0, 0); }
            flame = Particles("Drum fire", 400, new Color(1f, .5f, .15f, .9f), .25f, .55f, true);
            var fm = flame.main; fm.startLifetime = new ParticleSystem.MinMaxCurve(.35f, .8f); fm.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.4f);
            mist = Particles("Harbour mist", 500, new Color(.45f, .45f, .5f, .18f), 10f, 20f, false);
            var mm = mist.main; mm.startLifetime = new ParticleSystem.MinMaxCurve(12f, 18f); mm.startSpeed = new ParticleSystem.MinMaxCurve(.2f, .5f);
            beamA = Beam("Lighthouse beam A"); beamB = Beam("Lighthouse beam B");
            foreach (var c in craneTops) { var s = Beam("Searchlight"); s.positionCount = 0; searchlights.Add(s); }
            bolt = new GameObject("Lightning").AddComponent<LineRenderer>(); bolt.transform.SetParent(transform, false);
            bolt.useWorldSpace = true; bolt.widthMultiplier = .7f; bolt.positionCount = 0; bolt.sharedMaterial = new Material(WorldMaterials.Glow(new Color(.85f, .9f, 1f), 4f)) { name = "Lightning" };
            // ninety-nine red balloons, waiting
            var red = new Material(WorldMaterials.Glow(new Color(1f, .12f, .12f), .9f)) { name = "Red balloon" };
            var rng = new System.Random(99);
            for (var k = 0; k < 99; k++)
            {
                var bk = new MeshKit(); bk.Blob(red, Vector3.zero, new Vector3(.4f, .5f, .4f), rng, 0f); bk.Box(WorldMaterials.Solid(new Color(.1f, .1f, .1f)), new Vector3(0, -.9f, 0), new Vector3(.01f, .8f, .01f), Quaternion.identity);
                var go = bk.BuildSingle("Red balloon", transform, false); if (go == null) continue; go.SetActive(false); balloons.Add(go.transform);
                balloonOffset.Add(new Vector3((float)rng.NextDouble() * 70f - 35f, (float)rng.NextDouble() * 6f, (float)rng.NextDouble() * 70f - 35f));
            }
        }

        LineRenderer Beam(string name)
        {
            var go = new GameObject(name); go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>(); lr.useWorldSpace = true; lr.positionCount = 2;
            lr.widthCurve = new AnimationCurve(new Keyframe(0f, .6f), new Keyframe(1f, 9f));
            // the texture runs along the beam (u) and across it (v): fading out toward the far end and soft at both edges
            // (it used to fade across the width only, so the beam ended in a hard pale edge out over the water)
            var m = Additive(name); var tex = new Texture2D(32, 8, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (var y = 0; y < 8; y++) for (var x = 0; x < 32; x++) { var across = 1f - Mathf.Abs((y + .5f) / 4f - 1f); tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Pow(1f - x / 31f, 1.3f) * Mathf.SmoothStep(0f, 1f, across))); }
            tex.Apply(); m.SetTexture("_BaseMap", tex); m.SetColor("_BaseColor", new Color(1f, .95f, .8f, .22f));
            lr.sharedMaterial = m; lr.textureMode = LineTextureMode.Stretch; lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return lr;
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
            if (!add) { m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f); m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha); m.SetFloat("_ZWrite", 0f); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent; }
            m.SetTexture("_BaseMap", tex); r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return ps;
        }

        static void Jag(LineRenderer lr, Vector3 a, Vector3 b, int n, float wander)
        {
            lr.positionCount = n + 1;
            for (var i = 0; i <= n; i++) { var t = i / (float)n; var p = Vector3.Lerp(a, b, t); if (i > 0 && i < n) p += Random.insideUnitSphere * wander; lr.SetPosition(i, p); }
        }

        float Ramp(float v, bool on, float dt, float secs) => Mathf.MoveTowards(v, on ? 1f : 0f, dt / secs);

        void Update()
        {
            var dt = Time.deltaTime; var t = Time.time;
            var here = SkyDirector.Instance != null && SkyDirector.Instance.CurrentLand == Land;
            var d = MusicDirector.Instance; var playing = d != null && d.CurrentSlot.IsValid;
            var energy = SurpriseDirector.Energy;
            var id = MusicDirector.ForcedTrackId ?? (playing ? d.CurrentSlot.track.id : null);
            light = Ramp(light, here && id == DocksKit.LightNeverGoesOut, dt, 4f);
            tremolo = Ramp(tremolo, here && id == DocksKit.HowSoonIsNow, dt, 2f);
            red99 = Ramp(red99, here && id == DocksKit.RedBalloons, dt, 3f);
            rule = Ramp(rule, here && id == DocksKit.RuleTheWorld, dt, 3f);
            sprawl = Ramp(sprawl, here && id == DocksKit.TheSprawl, dt, 3f);
            golden = Ramp(golden, here && id == DocksKit.GoldenBrown, dt, 4f);
            pinkAmt = Ramp(pinkAmt, here && id == DocksKit.PinkSteam, dt, 2f);
            thunder = Ramp(thunder, here && id == DocksKit.Thunder, dt, 2f);

            foreach (var (song, _, _) in songGlows) if (!songAmount.ContainsKey(song)) songAmount[song] = 0f;
            var keys = new List<string>(songAmount.Keys);
            foreach (var s in keys) songAmount[s] = Mathf.MoveTowards(songAmount[s], here && id == s ? 1f : 0f, dt / 2f);
            foreach (var (song, m, c) in songGlows) if (m != null) m.SetColor("_EmissionColor", c * (1f + songAmount[song] * (2.5f + Mathf.Sin(t * 2.2f) * .6f)));

            // the sky: a deep blue night, gold for Golden Brown, blacker when the lamps go out
            var tint = new Color(.45f, .5f, .85f); var amount = .1f;
            if (golden > 0f) { tint = Color.Lerp(tint, new Color(1f, .72f, .3f), golden); amount = Mathf.Lerp(amount, .45f, golden); }
            if (light > 0f) { tint = Color.Lerp(tint, new Color(.2f, .25f, .5f), light); amount = Mathf.Lerp(amount, .4f, light); }
            if (flash > 0f) { tint = Color.Lerp(tint, new Color(.9f, .93f, 1f), flash); amount = Mathf.Max(amount, .7f * flash); }
            SkyDirector.SetSongTint(Land, tint, amount);
            if (here) { var w = clouds != null ? clouds.Wind : new Vector2(1, 0); Shader.SetGlobalFloat("_EW_Wind", .3f + .25f * energy); Shader.SetGlobalVector("_EW_WindDir", new Vector4(w.x, 0, w.y, 0)); }

            // the sodium lamps: a hum, the odd flicker, a throb for How Soon Is Now?, and out for There Is a Light
            flickerT -= dt; if (flickerT <= 0f) { flickerT = Random.Range(4f, 12f); flickerLamp = Random.Range(0, 3); }
            var flick = flickerT < .4f && flickerLamp == 0 ? (Random.value < .5f ? .3f : 1f) : 1f;
            var throb = Mathf.Lerp(1f, .35f + .65f * Mathf.Abs(Mathf.Sin(t * Mathf.PI * 1.55f)), tremolo);
            if (sodium != null) sodium.SetColor("_EmissionColor", DocksKit.Sodium * (1.45f + energy * .3f) * flick * throb * (1f - light * .97f));
            if (lampHalos != null && lampHalos.activeSelf != light < .4f) lampHalos.SetActive(light < .4f);
            if (windows != null) windows.SetColor("_EmissionColor", DocksKit.Window * (sprawl * 1.6f) * (1f - light));
            if (oneWindow != null) oneWindow.SetColor("_EmissionColor", DocksKit.Window * (1.6f + light * 4f + Mathf.Sin(t * .7f) * .1f));
            if (blink != null) blink.SetColor("_EmissionColor", new Color(1f, .1f, .08f) * (Mathf.Repeat(t, 1.6f) < .5f ? 2.4f : .15f));

            // the lighthouse beam, turning
            if (lighthouse != Vector3.zero && here)
            {
                var a = t * .55f; var dir = new Vector3(Mathf.Cos(a), -.04f, Mathf.Sin(a));
                var bright = 1f + light * 1.5f;
                beamA.SetPosition(0, lighthouse); beamA.SetPosition(1, lighthouse + dir * 160f); beamB.SetPosition(0, lighthouse); beamB.SetPosition(1, lighthouse - new Vector3(dir.x, .04f, dir.z) * 160f);
                beamA.sharedMaterial.SetColor("_BaseColor", new Color(1f, .95f, .8f, .2f * bright)); beamB.sharedMaterial.SetColor("_BaseColor", new Color(1f, .95f, .8f, .2f * bright));
                beamA.enabled = beamB.enabled = true;
            }
            else if (beamA != null) beamA.enabled = beamB.enabled = false;

            // searchlights for Everybody Wants To Rule The World
            for (var k = 0; k < searchlights.Count; k++)
            {
                var s = searchlights[k];
                if (rule < .02f || !here) { s.positionCount = 0; continue; }
                var a = t * (.4f + k * .13f) + k * 2f; var dir = new Vector3(Mathf.Cos(a) * .5f, 1f, Mathf.Sin(a) * .5f).normalized;
                s.positionCount = 2; s.SetPosition(0, craneTops[k]); s.SetPosition(1, craneTops[k] + dir * 260f);
                s.sharedMaterial.SetColor("_BaseColor", new Color(.85f, .9f, 1f, .25f * rule));
            }

            // ninety-nine red balloons, rising
            if (red99 > .01f && here) balloonT += dt; else if (red99 <= .01f) balloonT = 0f;
            var from = balloonsFrom != Vector3.zero ? balloonsFrom : center;
            for (var k = 0; k < balloons.Count; k++)
            {
                var b = balloons[k]; var delay = k * .6f; var on = here && red99 > .01f && balloonT > delay;
                if (b.gameObject.activeSelf != on) b.gameObject.SetActive(on); if (!on) continue;
                var age = balloonT - delay; var o = balloonOffset[k];
                b.position = from + new Vector3(o.x * Mathf.Clamp01(age / 20f), o.y + age * (1.1f + (k % 7) * .08f), o.z * Mathf.Clamp01(age / 20f)) + new Vector3(Mathf.Sin(t * .6f + k), 0, Mathf.Cos(t * .5f + k)) * .8f;
            }

            // Thunder: lightning on Maruja's rod
            nextBolt -= dt; boltLeft -= dt; flash = Mathf.MoveTowards(flash, 0f, dt * 5f);
            if (here && thunder > .3f && nextBolt <= 0f && rod != Vector3.zero) { nextBolt = Random.Range(1.5f, 4f); boltLeft = .2f; flash = 1f; Jag(bolt, rod + Vector3.up * 140f + Random.insideUnitSphere * 20f, rod, 14, 7f); }
            if (boltLeft <= 0f && bolt.positionCount > 0) bolt.positionCount = 0;

            if (!here) return;
            nextSteam -= dt;
            if (grates.Count > 0 && nextSteam <= 0f) { nextSteam = .25f; foreach (var g in grates) if (Random.value < .35f) { steam.transform.position = g; steam.Emit(1); } }
            nextPink -= dt;
            if (pinkSteam != Vector3.zero && nextPink <= 0f)
            {
                nextPink = pinkAmt > .1f ? .02f : .3f; pink.transform.position = pinkSteam; pink.Emit(1);
                if (pinkAmt > .1f) foreach (var g in grates) if (Random.value < .2f * pinkAmt) { pink.transform.position = g; pink.Emit(1); }
            }
            nextFlame -= dt;
            if (nextFlame <= 0f) { nextFlame = .15f; foreach (var f in fires) { flame.transform.position = f; flame.Emit(1); } }
            nextMist -= dt;
            if (nextMist <= 0f && (ship != Vector3.zero || lighthouse != Vector3.zero))
            {
                nextMist = .4f; var at = (Random.value < .5f && ship != Vector3.zero ? ship : lighthouse) + new Vector3(Random.Range(-60f, 60f), .8f, Random.Range(-60f, 60f));
                at.y = 1f; mist.transform.position = at; mist.Emit(1);
            }
        }
    }
}
