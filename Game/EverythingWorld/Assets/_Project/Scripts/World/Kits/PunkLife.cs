using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Punk Badlands at noon, a teenage summer:
    /// - kids skate the drained pool and the bowls, carving up the walls and back down (the shared clock, so friends
    ///   see the same skaters); tumbleweeds roll across with the wind; Panic!'s marquee bulbs chase;
    /// - "Wake Me up When September Ends": autumn leaves fall and the light turns gold. "Boulevard of Broken Dreams": the
    ///   land dims to a lonely blue and the boulevard's lamps flicker. "Welcome to the Black Parade": the sky darkens and
    ///   black confetti falls. "99 Red Balloons": red balloons float up across the land. "High Hopes": balloons rise from
    ///   the top of the ladder. Every song's own light brightens while it plays.
    public class PunkLife : MonoBehaviour
    {
        const string Land = "The Punk Badlands";
        public Vector3 center, pool, poolAxis = Vector3.forward, truck, september, highHopes, blackParade, balloonsAt;
        public float poolR = 26f, poolDepth = 4f;
        public Material boulevard, marquee;
        public List<Vector3> streetlights = new List<Vector3>(), tumbleweeds = new List<Vector3>();
        public List<Vector4> bowls = new List<Vector4>();
        public List<(string song, Material m, Color c)> songGlows = new List<(string, Material, Color)>();
        readonly Dictionary<string, float> songAmount = new Dictionary<string, float>();

        CloudLayer clouds; ParticleSystem leaves, confetti, balloons;
        readonly List<Transform> skaters = new List<Transform>(); readonly List<int> skaterBowl = new List<int>();
        readonly List<Transform> weeds = new List<Transform>();
        Color boulevardBase, marqueeBase;
        float sept, blvd, parade, red, hopes, nextLeaf, nextConfetti, nextBalloon, nextHope;

        void Start()
        {
            clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 30; clouds.span = 2600f; clouds.key = "punk";
            clouds.minHeight = 220f; clouds.maxHeight = 300f; clouds.size = 2.4f; clouds.speed = 5f; clouds.cover = .25f;
            clouds.color = new Color(1f, 1f, .98f);
            Color E(Material m) => m != null ? m.GetColor("_EmissionColor") : Color.black;
            boulevardBase = E(boulevard); marqueeBase = E(marquee);
            leaves = Particles("September leaves", 800, new Color(.95f, .5f, .15f, 1f), .12f, .22f, false);
            var lm = leaves.main; lm.startLifetime = new ParticleSystem.MinMaxCurve(5f, 8f); lm.startSpeed = new ParticleSystem.MinMaxCurve(0f, .4f); lm.gravityModifier = .03f;
            var lr = leaves.rotationOverLifetime; lr.enabled = true; lr.z = new ParticleSystem.MinMaxCurve(-3f, 3f);
            confetti = Particles("Black confetti", 1200, new Color(.05f, .05f, .06f, 1f), .08f, .16f, false);
            var cm = confetti.main; cm.startLifetime = new ParticleSystem.MinMaxCurve(4f, 6f); cm.startSpeed = 0f; cm.gravityModifier = .05f;
            var cr = confetti.rotationOverLifetime; cr.enabled = true; cr.z = new ParticleSystem.MinMaxCurve(-5f, 5f);
            balloons = Particles("Balloons", 600, new Color(1f, .15f, .15f, 1f), .7f, 1f, false);
            var bm = balloons.main; bm.startLifetime = new ParticleSystem.MinMaxCurve(12f, 18f); bm.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 2.5f); bm.gravityModifier = -.02f;
            var bs = balloons.shape; bs.shapeType = ParticleSystemShapeType.Cone; bs.angle = 20f; bs.radius = 1f; bs.rotation = new Vector3(-90f, 0, 0);
            // the skaters: a figure on a board each, three in the pool and one in each bowl
            var body = WorldMaterials.Solid(new Color(.12f, .12f, .14f)); var deck = WorldMaterials.Solid(new Color(.85f, .3f, .2f));
            Color[] shirts = { new Color(.9f, .2f, .3f), new Color(.2f, .6f, .9f), new Color(.95f, .8f, .2f), new Color(.4f, .85f, .4f) };
            var n = (pool != Vector3.zero ? 3 : 0) + bowls.Count;
            for (var k = 0; k < n; k++)
            {
                var fk = new MeshKit(); var s = 1f;
                fk.Box(deck, new Vector3(0, .06f, 0), new Vector3(.22f, .04f, .8f), Quaternion.identity);
                fk.Box(body, new Vector3(-.1f, .45f, .12f), new Vector3(.12f, .75f, .14f) * s, Quaternion.Euler(0, 0, 8f)); fk.Box(body, new Vector3(.1f, .45f, -.12f), new Vector3(.12f, .75f, .14f) * s, Quaternion.Euler(0, 0, -8f));
                fk.Box(WorldMaterials.Solid(shirts[k % shirts.Length]), new Vector3(0, 1.1f, 0), new Vector3(.4f, .55f, .25f), Quaternion.Euler(0, 70f, 0));
                fk.Blob(WorldMaterials.Solid(new Color(.9f, .75f, .6f)), new Vector3(0, 1.5f, 0), Vector3.one * .15f, new System.Random(k), 0f);
                fk.Box(body, new Vector3(0, 1.1f, .35f), new Vector3(.08f, .08f, .45f), Quaternion.Euler(0, 0, 0)); fk.Box(body, new Vector3(0, 1.1f, -.35f), new Vector3(.08f, .08f, .45f), Quaternion.identity);
                var go = fk.BuildSingle("Skater", transform, false); if (go == null) continue;
                skaters.Add(go.transform); skaterBowl.Add(k < (pool != Vector3.zero ? 3 : 0) ? -1 : k - (pool != Vector3.zero ? 3 : 0));
            }
            // tumbleweeds
            var weedM = WorldMaterials.Solid(new Color(.6f, .5f, .32f));
            for (var k = 0; k < Mathf.Min(14, tumbleweeds.Count); k++) { var wk = new MeshKit(); for (var j = 0; j < 5; j++) wk.Blob(weedM, Random.insideUnitSphere * .2f, Vector3.one * .45f, new System.Random(k * 7 + j), .6f); var go = wk.BuildSingle("Tumbleweed", transform, false); if (go != null) weeds.Add(go.transform); }
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
            for (var y = 0; y < 32; y++) for (var x = 0; x < 32; x++) { var d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(16, 16)) / 16f; tex.SetPixel(x, y, new Color(1, 1, 1, d < .9f ? 1f : 0f)); }
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
            if (id == null || !PunkSongs.Ids(artist, out var l)) return false;
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

            sept = Mathf.MoveTowards(sept, here && Playing(id, "Green Day", "Wake Me up When September") ? 1f : 0f, dt / 4f);
            blvd = Mathf.MoveTowards(blvd, here && Playing(id, "Green Day", "Holiday / Boulevard") ? 1f : 0f, dt / 4f);
            parade = Mathf.MoveTowards(parade, here && Playing(id, "My Chemical Romance", "Welcome to the Black Parade") ? 1f : 0f, dt / 4f);
            red = Mathf.MoveTowards(red, here && Playing(id, "Goldfinger", "99 Red Balloons") ? 1f : 0f, dt / 2f);
            hopes = Mathf.MoveTowards(hopes, here && Playing(id, "Panic! At The Disco", "High Hopes") ? 1f : 0f, dt / 2f);

            foreach (var (s, _, _) in songGlows) if (!songAmount.ContainsKey(s)) songAmount[s] = 0f;
            var keys = new List<string>(songAmount.Keys);
            foreach (var s in keys) songAmount[s] = Mathf.MoveTowards(songAmount[s], here && id == s ? 1f : 0f, dt / 2f);
            foreach (var (s, m, c) in songGlows) if (m != null && m != boulevard) m.SetColor("_EmissionColor", c * (1f + songAmount[s] * (2.5f + Mathf.Sin(t * 2.2f) * .6f)));
            if (boulevard != null) boulevard.SetColor("_EmissionColor", boulevardBase * (blvd > .05f ? (Mathf.PerlinNoise(t * 6f, 0f) > .45f ? 2.2f : .2f) : 1f));
            if (marquee != null) marquee.SetColor("_EmissionColor", marqueeBase * (.7f + .5f * Mathf.Abs(Mathf.Sin(t * (3f + energy * 4f)))));

            // the sky: a white-hot noon; gold in September, a lonely blue on the boulevard, dark for the black parade
            var tint = new Color(1f, .95f, .8f); var amount = .06f;
            if (sept > 0f) { tint = Color.Lerp(tint, new Color(1f, .65f, .25f), sept); amount = Mathf.Lerp(amount, .4f, sept); }
            if (blvd > 0f) { tint = Color.Lerp(tint, new Color(.3f, .38f, .55f), blvd); amount = Mathf.Lerp(amount, .5f, blvd); }
            if (parade > 0f) { tint = Color.Lerp(tint, new Color(.12f, .1f, .12f), parade); amount = Mathf.Lerp(amount, .6f, parade); }
            SkyDirector.SetSongTint(Land, tint, amount);
            var wind = clouds != null ? clouds.Wind : new Vector2(1, 0);
            if (here) { Shader.SetGlobalFloat("_EW_Wind", .5f + .3f * energy); Shader.SetGlobalVector("_EW_WindDir", new Vector4(wind.x, 0, wind.y, 0)); }

            // the skaters: back and forth across the pool and round the bowls, up the walls and down again
            if (data != null)
                for (var k = 0; k < skaters.Count; k++)
                {
                    var speed = .9f + (k % 3) * .2f + energy * .3f; var ph = now * speed + k * 2.1f;
                    Vector3 p, fwd;
                    if (skaterBowl[k] < 0)
                    {
                        var across = Vector3.Cross(Vector3.up, poolAxis); var lane = (k - 1) * poolR * .4f;
                        var x = Mathf.Sin(ph) * poolR * .55f; p = pool + poolAxis * lane + across * x;
                        fwd = (across * Mathf.Cos(ph) + poolAxis * .15f).normalized;
                    }
                    else
                    {
                        var b = bowls[skaterBowl[k]]; var r = b.w * (.55f + .3f * Mathf.Abs(Mathf.Sin(ph * .5f)));
                        p = new Vector3(b.x, 0, b.z) + new Vector3(Mathf.Cos(ph * .6f), 0, Mathf.Sin(ph * .6f)) * r;
                        fwd = new Vector3(-Mathf.Sin(ph * .6f), 0, Mathf.Cos(ph * .6f));
                    }
                    p.y = data.HeightAt(p.x, p.z) + .02f;
                    var ahead = p + fwd * .5f; var aheadY = data.HeightAt(ahead.x, ahead.z); var pitchDir = new Vector3(fwd.x, (aheadY - p.y) / .5f, fwd.z).normalized;
                    skaters[k].SetPositionAndRotation(p, Quaternion.LookRotation(pitchDir));
                }
            // tumbleweeds, rolling with the wind round where they started
            for (var k = 0; k < weeds.Count; k++)
            {
                var home = tumbleweeds[k]; var w3 = new Vector3(wind.x, 0, wind.y).normalized; var ph = Mathf.Repeat(now * .04f + k * .17f, 1f);
                var p = home + w3 * (ph - .5f) * 60f + Vector3.Cross(Vector3.up, w3) * Mathf.Sin(now * .3f + k) * 3f;
                if (data != null) p.y = data.HeightAt(p.x, p.z) + .45f + Mathf.Abs(Mathf.Sin(now * 3f + k)) * .3f;
                weeds[k].SetPositionAndRotation(p, Quaternion.Euler(now * 200f + k * 30f, 0, now * 90f));
                weeds[k].localScale = Vector3.one * Mathf.Clamp01(Mathf.Min(ph, 1f - ph) * 10f);
            }

            if (!here) return;
            var cam = Camera.main != null ? Camera.main.transform.position : center;
            nextLeaf -= dt;
            if (sept > .05f && nextLeaf <= 0f) { nextLeaf = .04f / sept; leaves.transform.position = cam + new Vector3(Random.Range(-25f, 25f), Random.Range(6f, 14f), Random.Range(-25f, 25f)); leaves.Emit(1); }
            nextConfetti -= dt;
            if (parade > .05f && nextConfetti <= 0f) { nextConfetti = .02f / parade; confetti.transform.position = cam + new Vector3(Random.Range(-20f, 20f), Random.Range(8f, 16f), Random.Range(-20f, 20f)); confetti.Emit(1); }
            nextBalloon -= dt;
            if (red > .05f && nextBalloon <= 0f) { nextBalloon = .25f; var bm = balloons.main; bm.startColor = new Color(1f, .15f, .15f); balloons.transform.SetPositionAndRotation(cam + new Vector3(Random.Range(-60f, 60f), -2f, Random.Range(-60f, 60f)), Quaternion.identity); balloons.Emit(1); if (balloonsAt != Vector3.zero) { balloons.transform.position = balloonsAt; balloons.Emit(1); } }
            nextHope -= dt;
            if (hopes > .05f && highHopes != Vector3.zero && nextHope <= 0f) { nextHope = .4f; var bm = balloons.main; bm.startColor = Random.value < .5f ? new Color(.3f, .75f, 1f) : new Color(1f, .3f, .55f); balloons.transform.SetPositionAndRotation(highHopes, Quaternion.identity); balloons.Emit(1); }
        }
    }

    /// Looks up the Punk Badlands' songs from outside the kit.
    public static class PunkSongs
    {
        public static bool Ids(string artist, out (string title, string id)[] list) => PunkKit.SongTable.TryGetValue(artist, out list);
    }
}
