using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Nu Metal Quarry, overcast and gritty, the radio on:
    /// - oil drums burn and spark; dust drifts in the pit; rocks ride the conveyor up to the crusher; a haul truck crawls
    ///   up and down the haul road (the shared clock, so friends see the same truck); the floodlights hum;
    /// - "Kryptonite": everything turns a little green. "The Sound of Silence": the fires and lamps sink low and the sky
    ///   darkens. "Photograph": camera flashes go off round the picture frame. "Higher": a beam rises from Creed's tower.
    ///   "Bones For The Crows": crows circle over the rig. "Give Me A Sign" flickers. "Stricken": lightning hits the rod.
    ///   Every song's own light brightens while it plays.
    public class QuarryLife : MonoBehaviour
    {
        const string Land = "The Nu Metal Quarry";
        public Vector3 center, pit, rampTop, rampBottom, beltA, beltB, flashAt, crowsAt, higherAt, kryptonite, rodAt;
        public Material fires, lamps, flickerSign;
        public Transform conveyorRocks; public string crowSong, higherSong;
        public List<Vector3> drumAt = new List<Vector3>();
        public List<(string song, Material m, Color c)> songGlows = new List<(string, Material, Color)>();
        readonly Dictionary<string, float> songAmount = new Dictionary<string, float>();

        CloudLayer clouds; ParticleSystem flame, sparks, dust, beam;
        Transform truck; readonly List<Transform> crows = new List<Transform>(); LineRenderer bolt; float boltLife, flash;
        Color fireBase, lampBase, signBase;
        float krypto, silence, photo, higher, crowAmt, stricken, nextFlame, nextSpark, nextDust, nextFlash, nextBeam, nextBolt;

        void Start()
        {
            clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 90; clouds.span = 2600f; clouds.key = "quarry";
            clouds.minHeight = 120f; clouds.maxHeight = 180f; clouds.size = 3.2f; clouds.speed = 3f; clouds.cover = .85f;
            clouds.color = new Color(.62f, .62f, .64f);
            Color E(Material m) => m != null ? m.GetColor("_EmissionColor") : Color.black;
            fireBase = E(fires); lampBase = E(lamps); signBase = E(flickerSign);
            flame = Particles("Drum flames", 1200, new Color(1f, .55f, .2f, .9f), .3f, .7f, true);
            var fm = flame.main; fm.startLifetime = new ParticleSystem.MinMaxCurve(.4f, .9f); fm.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3f);
            var fs = flame.shape; fs.shapeType = ParticleSystemShapeType.Cone; fs.angle = 12f; fs.radius = .25f; fs.rotation = new Vector3(-90f, 0, 0);
            sparks = Particles("Sparks", 500, new Color(1f, .7f, .3f, 1f), .04f, .08f, true);
            var sm = sparks.main; sm.startLifetime = new ParticleSystem.MinMaxCurve(1f, 2f); sm.startSpeed = new ParticleSystem.MinMaxCurve(2f, 4f); sm.gravityModifier = -.1f;
            var ss = sparks.shape; ss.shapeType = ParticleSystemShapeType.Cone; ss.angle = 25f; ss.radius = .2f; ss.rotation = new Vector3(-90f, 0, 0);
            dust = Particles("Dust", 600, new Color(.75f, .72f, .66f, .25f), 3f, 7f, false);
            var dm = dust.main; dm.startLifetime = new ParticleSystem.MinMaxCurve(8f, 12f); dm.startSpeed = new ParticleSystem.MinMaxCurve(.2f, .6f);
            beam = Particles("Higher", 600, new Color(1f, .95f, .85f, .8f), .3f, .8f, true);
            var bm = beam.main; bm.startLifetime = new ParticleSystem.MinMaxCurve(3f, 4f); bm.startSpeed = new ParticleSystem.MinMaxCurve(14f, 20f);
            var bs = beam.shape; bs.shapeType = ParticleSystemShapeType.Cone; bs.angle = 1.5f; bs.radius = .3f; bs.rotation = new Vector3(-90f, 0, 0);
            // the haul truck on the road
            if (rampTop != Vector3.zero)
            {
                var tk = new MeshKit(); var yel = WorldMaterials.Solid(new Color(.85f, .68f, .15f)); var dk = WorldMaterials.Solid(new Color(.08f, .08f, .09f));
                foreach (var e in new[] { -1f, 1f }) foreach (var f in new[] { -2.8f, 2.6f }) tk.Prism(dk, new Vector3(e * 1.9f, 1.6f, f), Vector3.right * e, 1.6f, 1f, 12);
                tk.Box(yel, new Vector3(0, 3f, 2.6f), new Vector3(5f, 2.2f, 2.6f), Quaternion.identity); tk.Box(yel, new Vector3(0, 4f, -1.2f), new Vector3(5.4f, 2.2f, 6f), Quaternion.Euler(-8f, 0, 0));
                tk.Box(WorldMaterials.Glow(new Color(1f, .95f, .8f), 1.4f), new Vector3(0, 2.6f, 3.92f), new Vector3(3.6f, .3f, .05f), Quaternion.identity);
                var go = tk.BuildSingle("Haul truck", transform, true); if (go != null) truck = go.transform;
            }
            var crowM = WorldMaterials.Solid(new Color(.05f, .05f, .06f));
            for (var k = 0; k < 10; k++) { var ck = new MeshKit(); ck.Box(crowM, Vector3.zero, new Vector3(.2f, .15f, .45f), Quaternion.identity); foreach (var s in new[] { -1f, 1f }) ck.Box(crowM, new Vector3(s * .4f, .03f, 0), new Vector3(.7f, .03f, .25f), Quaternion.Euler(0, 0, s * 12f)); var go = ck.BuildSingle("Crow", transform, false); if (go != null) { go.SetActive(false); crows.Add(go.transform); } }
            var bg = new GameObject("Lightning"); bg.transform.SetParent(transform, false); bolt = bg.AddComponent<LineRenderer>(); bolt.positionCount = 10; bolt.widthMultiplier = .4f; bolt.useWorldSpace = true; bolt.enabled = false;
            var lm = Additive("Lightning"); lm.SetColor("_BaseColor", new Color(.85f, .9f, 1f) * 6f); bolt.sharedMaterial = lm; bolt.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
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
            if (id == null || !QuarrySongs.Ids(artist, out var l)) return false;
            foreach (var (t, i) in l) if (i == id && t.StartsWith(title)) return true;
            return false;
        }

        void Update()
        {
            var dt = Time.deltaTime; var t = Time.time; var now = (float)(SharedChance.Now % 100000.0);
            var here = SkyDirector.Instance != null && SkyDirector.Instance.CurrentLand == Land;
            var d = MusicDirector.Instance; var playing = d != null && d.CurrentSlot.IsValid;
            var energy = SurpriseDirector.Energy;
            var id = MusicDirector.ForcedTrackId ?? (playing ? d.CurrentSlot.track.id : null);
            var data = IslandWorldBuilder.Instance != null ? IslandWorldBuilder.Instance.Data : null;

            krypto = Mathf.MoveTowards(krypto, here && Playing(id, "3 Doors Down", "Kryptonite") ? 1f : 0f, dt / 3f);
            silence = Mathf.MoveTowards(silence, here && Playing(id, "Disturbed", "The Sound of Silence") ? 1f : 0f, dt / 5f);
            photo = Mathf.MoveTowards(photo, here && Playing(id, "Nickelback", "Photograph") ? 1f : 0f, dt / 2f);
            higher = Mathf.MoveTowards(higher, here && Playing(id, "Creed", "Higher") ? 1f : 0f, dt / 3f);
            crowAmt = Mathf.MoveTowards(crowAmt, here && Playing(id, "Nickelback", "Bones For The Crows") ? 1f : 0f, dt / 2f);
            stricken = Mathf.MoveTowards(stricken, here && Playing(id, "Disturbed", "Stricken") ? 1f : 0f, dt / 2f);
            flash = Mathf.MoveTowards(flash, 0f, dt * 4f);

            foreach (var (s, _, _) in songGlows) if (!songAmount.ContainsKey(s)) songAmount[s] = 0f;
            var keys = new List<string>(songAmount.Keys);
            foreach (var s in keys) songAmount[s] = Mathf.MoveTowards(songAmount[s], here && id == s ? 1f : 0f, dt / 2f);
            foreach (var (s, m, c) in songGlows) if (m != null && m != flickerSign) m.SetColor("_EmissionColor", c * (1f + songAmount[s] * (2.5f + Mathf.Sin(t * 2.2f) * .6f)));
            if (flickerSign != null) flickerSign.SetColor("_EmissionColor", signBase * (Mathf.PerlinNoise(t * 5f, 3f) > .4f ? 1.2f : .1f));

            var low = 1f - silence * .8f;
            var flick = .85f + .15f * Mathf.PerlinNoise(t * 3f, 0f);
            if (fires != null) fires.SetColor("_EmissionColor", fireBase * (flick + energy * .4f) * low);
            if (lamps != null) lamps.SetColor("_EmissionColor", lampBase * (1f + .03f * Mathf.Sin(t * 50f)) * low);

            // the sky: flat grey overcast; green for Kryptonite, dark for the sound of silence
            var tint = new Color(.75f, .72f, .65f); var amount = .1f;
            if (krypto > 0f) { tint = Color.Lerp(tint, Color.Lerp(QuarryKit.Green, new Color(.3f, .7f, .35f), .5f + .5f * Mathf.Sin(t * 2f)), krypto); amount = Mathf.Lerp(amount, .4f, krypto); }
            if (silence > 0f) { tint = Color.Lerp(tint, new Color(.12f, .12f, .16f), silence); amount = Mathf.Lerp(amount, .6f, silence); }
            if (flash > 0f) { tint = Color.Lerp(tint, Color.white, flash); amount = Mathf.Max(amount, .5f * flash); }
            SkyDirector.SetSongTint(Land, tint, amount);
            var wind = clouds != null ? clouds.Wind : new Vector2(1, 0);
            if (here) { Shader.SetGlobalFloat("_EW_Wind", .5f + .3f * energy); Shader.SetGlobalVector("_EW_WindDir", new Vector4(wind.x, 0, wind.y, 0)); }

            // the truck up and down the haul road
            if (truck != null)
            {
                const float cycle = 90f; var ph = Mathf.Repeat(now, cycle) / cycle;
                var s = ph < .45f ? ph / .45f : ph < .5f ? 1f : ph < .95f ? 1f - (ph - .5f) / .45f : 0f;
                var p = Vector3.Lerp(rampTop, rampBottom, s); if (data != null) p.y = data.HeightAt(p.x, p.z);
                var dir = (rampBottom - rampTop); dir.y = 0; if (ph >= .5f) dir = -dir;
                truck.SetPositionAndRotation(p, Quaternion.LookRotation(dir.normalized));
            }
            // rocks riding the conveyor
            if (conveyorRocks != null) { var belt = beltB - beltA; var off = Mathf.Repeat(now * 1.2f, 1.6f); conveyorRocks.SetPositionAndRotation(beltA + belt.normalized * off + Vector3.up * .2f, Quaternion.LookRotation(belt)); conveyorRocks.localScale = new Vector3(1f, 1f, Mathf.Min(1f, (belt.magnitude - 2f) / (14 * 1.6f))); }
            // crows over the rig
            for (var k = 0; k < crows.Count; k++)
            {
                var on = here && (crowAmt > .05f || k < 3); if (crows[k].gameObject.activeSelf != on) crows[k].gameObject.SetActive(on); if (!on) continue;
                var a = now * (.4f + (k % 4) * .05f) + k * .6f; var r = 9f + (k % 3) * 4f;
                crows[k].SetPositionAndRotation(crowsAt + new Vector3(Mathf.Cos(a) * r, (k % 4) * 1.5f + Mathf.Sin(a * 2f), Mathf.Sin(a) * r), Quaternion.LookRotation(new Vector3(-Mathf.Sin(a), 0, Mathf.Cos(a))) * Quaternion.Euler(0, 0, Mathf.Sin(t * 8f + k) * 25f));
            }

            if (!here) { if (bolt.enabled) bolt.enabled = false; return; }
            var cam = Camera.main != null ? Camera.main.transform.position : center;
            nextFlame -= dt;
            if (nextFlame <= 0f) { nextFlame = .08f; foreach (var f in drumAt) if ((f - cam).sqrMagnitude < 120f * 120f && Random.value < .5f * low) { flame.transform.position = f; flame.Emit(1); } }
            nextSpark -= dt;
            if (nextSpark <= 0f) { nextSpark = .15f; foreach (var f in drumAt) if ((f - cam).sqrMagnitude < 60f * 60f && Random.value < .12f) { sparks.transform.position = f; sparks.Emit(1); } }
            nextDust -= dt;
            if (pit != Vector3.zero && nextDust <= 0f) { nextDust = .4f; dust.transform.position = pit + new Vector3(Random.Range(-70f, 70f), Random.Range(2f, 20f), Random.Range(-70f, 70f)); dust.Emit(1); }
            nextFlash -= dt;
            if (photo > .05f && nextFlash <= 0f) { nextFlash = Random.Range(.3f, 1.2f); flash = .6f; }
            nextBeam -= dt;
            if (higher > .05f && higherAt != Vector3.zero && nextBeam <= 0f) { nextBeam = .03f; beam.transform.position = higherAt; beam.Emit(2); }
            nextBolt -= dt;
            if (stricken > .5f && rodAt != Vector3.zero && nextBolt <= 0f)
            {
                nextBolt = Random.Range(2f, 5f); var top = rodAt + Vector3.up * 120f + new Vector3(Random.Range(-20f, 20f), 0, Random.Range(-20f, 20f));
                for (var i = 0; i < bolt.positionCount; i++) { var f = i / (bolt.positionCount - 1f); bolt.SetPosition(i, Vector3.Lerp(top, rodAt, f) + (i == 0 || i == bolt.positionCount - 1 ? Vector3.zero : new Vector3(Random.Range(-5f, 5f), 0, Random.Range(-5f, 5f)))); }
                bolt.enabled = true; boltLife = .2f; flash = 1f;
            }
            if (bolt.enabled) { boltLife -= dt; if (boltLife <= 0f) bolt.enabled = false; }
        }
    }

    /// Looks up the Nu Metal Quarry's songs from outside the kit.
    public static class QuarrySongs
    {
        public static bool Ids(string artist, out (string title, string id)[] list) => QuarryKit.SongTable.TryGetValue(artist, out list);
    }
}
