using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Grunge Rainwood in its drizzle, answering the music:
    /// - low grey cloud drags over the treetops; it always drizzles, and during about half the songs (shared clock, so
    ///   friends get the same weather) a downpour rolls in for the middle of the song; mist drifts low between the pines
    ///   and banks up over the river; spray and mist rise from the foot of the falls; the water wheel turns; smoke
    ///   leaves the mill's stack; the road lamps flicker now and then; crows lift out of the trees and circle.
    /// - the stage lights in the mill and the ring of the saw blade pulse with the music; every song's own light
    ///   brightens while it plays.
    /// - "Black Hole Sun": the rain stops, the sky goes dark amber and a black sun with a burning rim rises over the
    ///   falls. "Lightning Crashes": lightning strikes again and again, often on Live's lightning rod. "No Rain": the
    ///   rain stops and gold light breaks through. "Say Hello 2 Heaven": a pillar of light rises from the temple.
    ///   "Disarm": the bells swing. "Even Flow": the river and the falls brighten.
    public partial class GrungeLife : MonoBehaviour
    {
        const string Land = "The Grunge Rainwood";
        public Vector3 center, fallsTop, fallsFoot, fallsDir = Vector3.forward, stageAt, chimney, heaven, rod, flare, sunPatch, wheelAxis = Vector3.right;
        public Material water, foam, stage, bladeRing, backdrop;
        public Vector3[] river = new Vector3[0];
        public List<Vector3> lamps = new List<Vector3>(), fires = new List<Vector3>();
        public List<Transform> bells = new List<Transform>(); public Transform wheel; readonly List<Quaternion> bellRest = new List<Quaternion>();
        public List<(string song, Material m, Color c)> songGlows = new List<(string, Material, Color)>();
        readonly Dictionary<string, float> songAmount = new Dictionary<string, float>();

        CloudLayer clouds; ParticleSystem spray, mist, smoke, flame, sparks, beam, gold;
        LineRenderer bolt; float boltLeft, nextBolt = 6f, flash;
        Transform blackSun; Material sunRim;
        readonly List<Transform> crows = new List<Transform>(); float crowsUp; long crowSong = long.MinValue;
        float holeSun, crashes, noRain, heavenAmt, disarm, evenFlow, pour;
        long pourSong = long.MinValue; double pourFrom, pourTo;
        float nextSpray, nextMist, nextSmoke, nextFlame, nextBeam, nextGold;

        void Start()
        {
            clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 80; clouds.span = 2400f; clouds.key = "rainwood";
            clouds.minHeight = 70f; clouds.maxHeight = 115f; clouds.size = 2.8f; clouds.speed = 3.5f; clouds.cover = .9f;
            clouds.color = new Color(.52f, .56f, .58f);
            spray = Particles("Falls spray", 700, new Color(.85f, .93f, 1f, .5f), 1.5f, 4f, false);
            var spm = spray.main; spm.startLifetime = new ParticleSystem.MinMaxCurve(2f, 4f); spm.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 4f);
            var sps = spray.shape; sps.shapeType = ParticleSystemShapeType.Sphere; sps.radius = 3f;
            mist = Particles("Rainwood mist", 700, new Color(.72f, .78f, .8f, .2f), 8f, 16f, false);
            var mm = mist.main; mm.startLifetime = new ParticleSystem.MinMaxCurve(10f, 16f); mm.startSpeed = new ParticleSystem.MinMaxCurve(.2f, .6f);
            smoke = Particles("Mill smoke", 200, new Color(.35f, .36f, .37f, .45f), 3f, 7f, false);
            var sm = smoke.main; sm.startLifetime = new ParticleSystem.MinMaxCurve(6f, 9f); sm.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3f);
            flame = Particles("Fire", 400, new Color(1f, .5f, .15f, .9f), .25f, .55f, true);
            var fm = flame.main; fm.startLifetime = new ParticleSystem.MinMaxCurve(.35f, .8f); fm.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.4f);
            sparks = Particles("Sparks", 300, new Color(.8f, .9f, 1f, 1f), .1f, .25f, true);
            var spk = sparks.main; spk.startLifetime = new ParticleSystem.MinMaxCurve(.5f, 1f); spk.startSpeed = new ParticleSystem.MinMaxCurve(2f, 6f); spk.gravityModifier = .8f;
            beam = Particles("Heaven beam", 600, new Color(.85f, .9f, 1f, .8f), .3f, .8f, true);
            var bm = beam.main; bm.startLifetime = new ParticleSystem.MinMaxCurve(3f, 4f); bm.startSpeed = new ParticleSystem.MinMaxCurve(10f, 16f);
            var bs = beam.shape; bs.shapeType = ParticleSystemShapeType.Cone; bs.angle = 1.5f; bs.radius = .8f; bs.rotation = new Vector3(-90f, 0, 0);
            gold = Particles("Gold light", 300, new Color(1f, .85f, .4f, .6f), .2f, .5f, true);
            var gm = gold.main; gm.startLifetime = new ParticleSystem.MinMaxCurve(3f, 5f); gm.startSpeed = new ParticleSystem.MinMaxCurve(.2f, .8f);
            bolt = Line("Lightning", new Color(.85f, .9f, 1f), .8f);
            // the black sun: a dark disc with a burning rim, hidden until its song
            var sk = new MeshKit(); var black = WorldMaterials.Solid(new Color(.02f, .02f, .02f));
            sunRim = new Material(WorldMaterials.Glow(new Color(1f, .5f, .12f), 3f)) { name = "Black sun rim" };
            sk.Prism(black, Vector3.zero, Vector3.forward, 14f, .5f, 40);
            for (var k = 0; k < 48; k++) { var a0 = k * Mathf.PI / 24f; var a1 = (k + 1) * Mathf.PI / 24f; var p0 = new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0) * 15f; var p1 = new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0) * 15f; sk.Box(sunRim, (p0 + p1) / 2f + Vector3.forward * .6f, new Vector3(2f, 2f, Vector3.Distance(p0, p1) + .2f), Quaternion.LookRotation(p1 - p0, Vector3.forward)); }
            for (var k = 0; k < 24; k++) sk.Cone(sunRim, Quaternion.Euler(0, 0, k * 15f) * Vector3.up * 15.5f + Vector3.forward * .6f, 1.4f, 4f + (k % 2) * 4f, 4);
            var go = sk.BuildSingle("Black hole sun", transform, false);
            if (go != null) { blackSun = go.transform; blackSun.gameObject.SetActive(false); }
            // the crows
            var crowM = WorldMaterials.Solid(new Color(.05f, .05f, .06f));
            for (var k = 0; k < 9; k++)
            {
                var ck = new MeshKit(); ck.Box(crowM, Vector3.zero, new Vector3(.25f, .2f, .7f), Quaternion.identity);
                foreach (var s in new[] { -1f, 1f }) ck.Box(crowM, new Vector3(s * .55f, .05f, 0), new Vector3(1f, .04f, .35f), Quaternion.Euler(0, 0, s * 12f));
                var c = ck.BuildSingle("Crow", transform, false); if (c != null) { c.SetActive(false); crows.Add(c.transform); }
            }
        }

        LineRenderer Line(string name, Color c, float width)
        {
            var go = new GameObject(name); go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>(); lr.useWorldSpace = true; lr.widthMultiplier = width; lr.positionCount = 0;
            lr.sharedMaterial = new Material(WorldMaterials.Glow(c, 4f)) { name = name }; lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
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

        /// How hard it's pouring (0 to 1): a downpour for the middle of about half the songs, the same for everyone.
        float Pour(float dt)
        {
            if (SharedChance.CurrentSong(out var song, out var start, out var length) && song != pourSong)
            {
                pourSong = song; var k = SharedChance.Key("rainwood-pour");
                if (SharedChance.Roll(k, song, .5)) { pourFrom = start + length * .3; pourTo = start + length * .75; } else pourFrom = pourTo = -1;
            }
            var now = SharedChance.Now; var on = pourFrom > 0 && now > pourFrom && now < pourTo;
            pour = Mathf.MoveTowards(pour, on ? 1f : 0f, dt / 8f); return pour;
        }

        void Update()
        {
            var dt = Time.deltaTime; var t = Time.time;
            var here = SkyDirector.Instance != null && SkyDirector.Instance.CurrentLand == Land;
            var d = MusicDirector.Instance; var playing = d != null && d.CurrentSlot.IsValid;
            var energy = SurpriseDirector.Energy;
            var id = MusicDirector.ForcedTrackId ?? (playing ? d.CurrentSlot.track.id : null);
            holeSun = Mathf.MoveTowards(holeSun, here && id == GrungeKit.BlackHoleSun ? 1f : 0f, dt / 4f);
            crashes = Mathf.MoveTowards(crashes, here && id == GrungeKit.LightningCrashes ? 1f : 0f, dt / 2f);
            noRain = Mathf.MoveTowards(noRain, here && id == GrungeKit.NoRain ? 1f : 0f, dt / 4f);
            heavenAmt = Mathf.MoveTowards(heavenAmt, here && id == GrungeKit.SayHello2Heaven ? 1f : 0f, dt / 3f);
            disarm = Mathf.MoveTowards(disarm, here && id == GrungeKit.Disarm ? 1f : 0f, dt / 2f);
            evenFlow = Mathf.MoveTowards(evenFlow, here && id == GrungeKit.EvenFlow ? 1f : 0f, dt / 2f);
            var p = Pour(dt);

            // every song's own light
            foreach (var (song, _, _) in songGlows) if (!songAmount.ContainsKey(song)) songAmount[song] = 0f;
            var keys = new List<string>(songAmount.Keys);
            foreach (var s in keys) songAmount[s] = Mathf.MoveTowards(songAmount[s], here && id == s ? 1f : 0f, dt / 2f);
            foreach (var (song, m, c) in songGlows) if (m != null) m.SetColor("_EmissionColor", c * (1f + songAmount[song] * (2.5f + Mathf.Sin(t * 2.2f) * .6f)));

            // sky and rain
            var clear = Mathf.Max(holeSun, noRain);
            var tint = new Color(.75f, .85f, .9f); var amount = .12f;
            if (holeSun > 0f) { tint = Color.Lerp(tint, new Color(1f, .62f, .2f), holeSun); amount = Mathf.Lerp(amount, .45f, holeSun); }
            if (noRain > 0f) { tint = Color.Lerp(tint, new Color(1f, .85f, .5f), noRain); amount = Mathf.Lerp(amount, .45f, noRain); }
            if (flash > 0f) { tint = Color.Lerp(tint, new Color(.9f, .93f, 1f), flash); amount = Mathf.Max(amount, .7f * flash); }
            SkyDirector.SetSongTint(Land, tint, amount);
            SkyDirector.SetSongRain(Land, Mathf.Clamp01((.4f + p * .5f + crashes * .2f) * (1f - clear)));
            if (clouds != null) clouds.cover = Mathf.Lerp(.9f, .45f, noRain);
            if (here) { var w = clouds != null ? clouds.Wind : new Vector2(1, 0); Shader.SetGlobalFloat("_EW_Wind", .45f + .3f * energy + p * .5f); Shader.SetGlobalVector("_EW_WindDir", new Vector4(w.x, 0, w.y, 0)); }

            // the black sun rises over the falls
            if (blackSun != null)
            {
                var on = holeSun > .01f && here; if (blackSun.gameObject.activeSelf != on) blackSun.gameObject.SetActive(on);
                if (on)
                {
                    var cam = Camera.main; var anchor = fallsTop != Vector3.zero ? fallsTop : center;
                    var pos = anchor + Vector3.up * (12f + 26f * Mathf.SmoothStep(0, 1, holeSun)) - fallsDir * 90f;
                    var face = cam != null ? (cam.transform.position - pos).normalized : fallsDir;
                    blackSun.SetPositionAndRotation(pos, Quaternion.LookRotation(-face)); blackSun.localScale = Vector3.one * Mathf.SmoothStep(.2f, 1f, holeSun);
                    sunRim.SetColor("_EmissionColor", new Color(1f, .5f, .12f) * (2f + Mathf.Sin(t * 1.3f) * .6f + energy * 2f));
                }
            }

            // lightning: rare in the drizzle, now and then in a downpour, again and again for Lightning Crashes
            nextBolt -= dt; boltLeft -= dt; flash = Mathf.MoveTowards(flash, 0f, dt * 5f);
            if (here && nextBolt <= 0f)
            {
                var cam = Camera.main;
                nextBolt = crashes > .3f ? Random.Range(.8f, 2.5f) : p > .5f ? Random.Range(10f, 25f) : Random.Range(40f, 90f);
                if (cam != null && (crashes > .3f || p > .5f))
                {
                    boltLeft = .2f; flash = 1f;
                    Vector3 hit; if (rod != Vector3.zero && crashes > .3f && Random.value < .5f && (rod - cam.transform.position).sqrMagnitude < 300f * 300f) { hit = rod; sparks.transform.position = rod; sparks.Emit(30); }
                    else { hit = cam.transform.position + new Vector3(Random.Range(-250f, 250f), 0, Random.Range(-250f, 250f)); hit.y = cam.transform.position.y - 8f; }
                    Jag(bolt, hit + Vector3.up * 160f + Random.insideUnitSphere * 25f, hit, 14, 8f);
                }
            }
            if (boltLeft <= 0f && bolt.positionCount > 0) bolt.positionCount = 0;

            // the water, the stage and the mill
            if (water != null) water.SetColor("_EmissionColor", new Color(.2f, .36f, .4f) * (.25f + .1f * Mathf.Sin(t * .8f) + evenFlow * 1.6f));
            if (foam != null) foam.SetColor("_EmissionColor", GrungeKit.Falls * (.45f + .1f * Mathf.Sin(t * 6f) + evenFlow * 1.2f + p * .15f));
            if (stage != null) stage.SetColor("_EmissionColor", GrungeKit.Lamp * (1.1f + energy * 1.6f + Mathf.Sin(t * 3f) * .15f));
            if (bladeRing != null) bladeRing.SetColor("_EmissionColor", GrungeKit.Lamp * (.8f + energy * 1.4f));
            if (backdrop != null) backdrop.SetColor("_EmissionColor", new Color(.75f, .2f, .16f) * (.5f + energy * .9f + Mathf.Sin(t * 1.7f) * .08f));
            if (wheel != null) wheel.Rotate(wheelAxis, (25f + evenFlow * 40f) * dt, Space.World);
            if (bellRest.Count != bells.Count) { bellRest.Clear(); foreach (var b in bells) bellRest.Add(b != null ? b.rotation : Quaternion.identity); }
            for (var k = 0; k < bells.Count; k++) if (bells[k] != null)
            {
                var swing = Mathf.Sin(t * (2.2f + k * .4f) + k) * (3f + disarm * 22f);
                bells[k].rotation = bellRest[k] * Quaternion.Euler(swing, 0, 0);
            }

            // crows: a flock lifts out of the trees during about a third of the songs and circles over the falls
            if (SharedChance.CurrentSong(out var cs, out _, out _) && cs != crowSong) { crowSong = cs; crowsUp = SharedChance.Roll(SharedChance.Key("rainwood-crows"), cs, .35) ? 1f : 0f; }
            for (var k = 0; k < crows.Count; k++)
            {
                var c = crows[k]; var on = here && crowsUp > 0f; if (c.gameObject.activeSelf != on) c.gameObject.SetActive(on); if (!on) continue;
                var a = (float)(SharedChance.Now * (.25f + k * .02f)) + k * .7f; var r = 25f + (k % 3) * 8f;
                var hub = (fallsTop != Vector3.zero ? fallsTop : center) + Vector3.up * (18f + (k % 4) * 3f);
                var pos = hub + new Vector3(Mathf.Cos(a), Mathf.Sin(a * 2f) * .08f, Mathf.Sin(a)) * r;
                c.SetPositionAndRotation(pos, Quaternion.LookRotation(new Vector3(-Mathf.Sin(a), 0, Mathf.Cos(a))) * Quaternion.Euler(0, 0, Mathf.Sin(t * 8f + k) * 20f));
            }

            if (!here) return;
            nextSpray -= dt;
            if (fallsFoot != Vector3.zero && nextSpray <= 0f) { nextSpray = .06f; spray.transform.position = fallsFoot + Vector3.up * 1f; spray.Emit(1 + (evenFlow > .3f ? 1 : 0)); }
            nextMist -= dt;
            if (nextMist <= 0f)
            {
                nextMist = .35f - p * .15f; var cam = Camera.main;
                var at = river.Length > 0 && Random.value < .4f ? river[Random.Range(0, river.Length)] + Vector3.up * 1.5f : (cam != null ? cam.transform.position : center) + new Vector3(Random.Range(-90f, 90f), Random.Range(-4f, 3f), Random.Range(-90f, 90f));
                mist.transform.position = at; mist.Emit(1);
            }
            nextSmoke -= dt;
            if (chimney != Vector3.zero && nextSmoke <= 0f) { nextSmoke = .6f; smoke.transform.position = chimney; smoke.Emit(1); }
            nextFlame -= dt;
            if (nextFlame <= 0f) { nextFlame = .15f; foreach (var f in fires) { flame.transform.position = f; flame.Emit(1); } if (flare != Vector3.zero) { flame.transform.position = flare; flame.Emit(1); } }
            nextBeam -= dt;
            if (heaven != Vector3.zero && heavenAmt > .05f && nextBeam <= 0f) { nextBeam = .02f / heavenAmt; beam.transform.position = heaven; beam.Emit(2); }
            nextGold -= dt;
            if (noRain > .05f && nextGold <= 0f) { nextGold = .05f / noRain; var cam = Camera.main; gold.transform.position = (sunPatch != Vector3.zero && Random.value < .5f ? sunPatch : cam != null ? cam.transform.position : center) + new Vector3(Random.Range(-12f, 12f), Random.Range(0f, 8f), Random.Range(-12f, 12f)); gold.Emit(1); }
        }
    }
}
