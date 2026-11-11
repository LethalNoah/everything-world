using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Folk Woods on a clear morning, answering the music:
    /// - the little steam train runs its loop round the lake (where it is comes from the shared clock, so friends
    ///   see the same train), puffing steam; mist drifts on the lake; smoke rises from the cabin chimneys; birds
    ///   cross the sky; campfires crackle;
    /// - "Peace Train": the train's carriages light up with strings of flowers and it picks up speed; "Rocky
    ///   Mountain High": shooting stars fall through the morning sky and the light turns golden; "The Wreck of the
    ///   Edmund Fitzgerald": a storm rolls over the lake, dark and wet and windy; "Bridge over Troubled Water": the
    ///   footbridge glows and the light goes soft and still.
    public partial class FolkLife : MonoBehaviour
    {
        const string Land = "The Folk Woods";
        public Vector3 center, tree, lake; public float lakeR = 70f;
        public Vector3[] track = new Vector3[0]; public int station;
        public Material bridgeGlow;
        public List<Vector3> chimneys = new List<Vector3>(), fires = new List<Vector3>(), shafts = new List<Vector3>();
        public int giants;

        CloudLayer clouds; ParticleSystem steam, mist, smoke, flame, stars, seeds; Material shaftM;
        readonly List<Transform> train = new List<Transform>(); Material trainLights;
        readonly List<Transform> birds = new List<Transform>();
        float[] trackLen = new float[0]; float total;
        float peace, rocky, storm, bridge;
        float nextSteam, nextMist, nextSmoke, nextFlame, nextStar, nextSeed;

        void Start()
        {
            clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 45; clouds.span = 2400f; clouds.key = "folk";
            clouds.minHeight = 120f; clouds.maxHeight = 180f; clouds.size = 1.6f; clouds.speed = 2f; clouds.cover = .35f;
            clouds.color = new Color(.95f, .97f, 1f);
            steam = Particles("Steam", 500, new Color(.95f, .95f, .95f, .5f), .6f, 1.4f, false);
            var sm = steam.main; sm.startLifetime = new ParticleSystem.MinMaxCurve(2f, 3.5f); sm.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3f);
            mist = Particles("Lake mist", 600, new Color(.92f, .95f, 1f, .26f), 12f, 24f, false);
            var mm = mist.main; mm.startLifetime = new ParticleSystem.MinMaxCurve(10f, 16f); mm.startSpeed = new ParticleSystem.MinMaxCurve(.2f, .6f);
            smoke = Particles("Chimney smoke", 400, new Color(.7f, .7f, .72f, .35f), .5f, 1.2f, false);
            var km = smoke.main; km.startLifetime = new ParticleSystem.MinMaxCurve(3f, 5f); km.startSpeed = new ParticleSystem.MinMaxCurve(.6f, 1.2f);
            flame = Particles("Fire", 600, new Color(1f, .55f, .2f, .9f), .3f, .7f, true);
            var fm = flame.main; fm.startLifetime = new ParticleSystem.MinMaxCurve(.4f, 1f); fm.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3f);
            var fs = flame.shape; fs.shapeType = ParticleSystemShapeType.Cone; fs.angle = 12f; fs.radius = .4f; fs.rotation = new Vector3(-90f, 0, 0);
            stars = Particles("Shooting stars", 300, new Color(1f, .95f, .8f, 1f), .3f, .5f, true);
            var stm = stars.main; stm.startLifetime = new ParticleSystem.MinMaxCurve(.8f, 1.4f); stm.startSpeed = new ParticleSystem.MinMaxCurve(60f, 90f);
            var sr = stars.GetComponent<ParticleSystemRenderer>(); sr.renderMode = ParticleSystemRenderMode.Stretch; sr.velocityScale = .25f;
            // glowing seeds drifting on the air round you, like dandelion clocks lit by the low sun
            seeds = Particles("Drifting seeds", 600, new Color(1f, .95f, .75f, .9f), .08f, .16f, true);
            var sdm = seeds.main; sdm.startLifetime = new ParticleSystem.MinMaxCurve(7f, 11f); sdm.startSpeed = new ParticleSystem.MinMaxCurve(.1f, .4f); sdm.gravityModifier = -.01f;
            var sdn = seeds.noise; sdn.enabled = true; sdn.strength = .5f; sdn.frequency = .2f;
            // slanting shafts of morning sun in the clearings: crossed sheets of soft light, leaning away from the east
            if (shafts.Count > 0)
            {
                shaftM = Additive("Sun shafts"); shaftM.SetFloat("_Blend", 0f); shaftM.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha); shaftM.SetColor("_BaseColor", new Color(1f, .74f, .3f, .5f));
                var tex = new Texture2D(8, 32, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                for (var y = 0; y < 32; y++) for (var x = 0; x < 8; x++) { var e = Mathf.Sin((x + .5f) / 8f * Mathf.PI); var v = y / 31f; tex.SetPixel(x, y, new Color(1, 1, 1, e * Mathf.SmoothStep(0f, .08f, v) * Mathf.SmoothStep(1f, .55f, v))); }
                tex.Apply(); shaftM.SetTexture("_BaseMap", tex);
                var verts = new List<Vector3>(); var uvs = new List<Vector2>(); var tris = new List<int>();
                var lean = new Vector3(-.55f, 1f, .12f).normalized; var rng = new System.Random(77);
                foreach (var g in shafts)
                {
                    var len = 30f + (float)rng.NextDouble() * 14f; var w = 4f + (float)rng.NextDouble() * 5f;
                    var top = g + lean * len;
                    foreach (var across in new[] { new Vector3(1f, 0, 0), new Vector3(0, 0, 1f) })
                    {
                        var b = verts.Count;
                        verts.Add(g - across * w * .5f); verts.Add(g + across * w * .5f); verts.Add(top + across * w * .8f); verts.Add(top - across * w * .8f);
                        uvs.Add(new Vector2(0, 0)); uvs.Add(new Vector2(1, 0)); uvs.Add(new Vector2(1, 1)); uvs.Add(new Vector2(0, 1));
                        tris.AddRange(new[] { b, b + 1, b + 2, b, b + 2, b + 3 });
                    }
                }
                var mesh = new Mesh { name = "Sun shafts" }; mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                var cols = new List<Color>(); foreach (var _ in verts) cols.Add(Color.white);
                mesh.SetVertices(verts); mesh.SetColors(cols); mesh.SetUVs(0, uvs); mesh.SetTriangles(tris, 0); mesh.RecalculateBounds();
                var go = new GameObject("Sun shafts"); go.transform.SetParent(transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh; var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = shaftM; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
            }
            // the train: an engine and three carriages
            trackLen = new float[track.Length]; for (var i = 1; i < track.Length; i++) trackLen[i] = trackLen[i - 1] + Vector3.Distance(track[i - 1], track[i]); total = track.Length > 1 ? trackLen[track.Length - 1] : 1f;
            trainLights = new Material(WorldMaterials.Glow(new Color(1f, .8f, .5f), .4f)) { name = "Train lights" };
            var red = WorldMaterials.Solid(new Color(.65f, .15f, .12f)); var green = WorldMaterials.Solid(new Color(.2f, .4f, .3f)); var black = WorldMaterials.Solid(new Color(.08f, .08f, .08f)); var gold = WorldMaterials.Solid(new Color(.85f, .7f, .35f), .8f);
            for (var c = 0; c < 4; c++)
            {
                var mk = new MeshKit();
                if (c == 0)
                {
                    mk.Prism(black, new Vector3(0, 1.1f, -1.2f), Vector3.forward, .55f, 2.6f, 12); mk.Box(red, new Vector3(0, 1.3f, -1.8f), new Vector3(1.2f, 1.4f, 1.2f), Quaternion.identity);
                    mk.Prism(black, new Vector3(0, 1.6f, 1f), Vector3.up, .18f, .9f, 8, 1.4f); mk.Prism(gold, new Vector3(0, 1.1f, 1.42f), Vector3.forward, .25f, .05f, 10);
                    mk.Box(black, new Vector3(0, 2.05f, -1.8f), new Vector3(1.4f, .1f, 1.4f), Quaternion.identity);
                }
                else
                {
                    mk.Box(green, new Vector3(0, 1.2f, 0), new Vector3(1.3f, 1.2f, 2.8f), Quaternion.identity); mk.Box(black, new Vector3(0, 1.85f, 0), new Vector3(1.4f, .1f, 3f), Quaternion.identity);
                    for (var w = 0; w < 3; w++) foreach (var s in new[] { -1f, 1f }) mk.Box(trainLights, new Vector3(s * .66f, 1.3f, -.9f + w * .9f), new Vector3(.02f, .4f, .5f), Quaternion.identity);
                }
                foreach (var s in new[] { -.55f, .55f }) foreach (var z in new[] { -.9f, .9f }) mk.Prism(black, new Vector3(s, .45f, z), Vector3.right * Mathf.Sign(s), .3f, .1f, 10);
                var t = mk.BuildSingle(c == 0 ? "Engine" : "Carriage", transform, false).transform; train.Add(t);
            }
            var bird = WorldMaterials.Solid(new Color(.2f, .2f, .22f));
            for (var i = 0; i < 10; i++)
            {
                var mk = new MeshKit();
                foreach (var s in new[] { -1f, 1f }) { mk.Tri(bird, new Vector3(0, 0, .2f), new Vector3(s * .6f, .12f, -.05f), new Vector3(0, 0, -.2f), Vector3.up); mk.Tri(bird, new Vector3(0, 0, .2f), new Vector3(0, 0, -.2f), new Vector3(s * .6f, .12f, -.05f), Vector3.down); }
                birds.Add(mk.BuildSingle("Bird", transform, false).transform);
            }
            StoriesStart();
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

        Vector3 OnTrack(float s, out Vector3 dir)
        {
            dir = Vector3.forward; if (track.Length < 2) return center;
            s = Mathf.Repeat(s, total);
            var lo = 0; var hi = track.Length - 1;
            while (hi - lo > 1) { var mid = (lo + hi) / 2; if (trackLen[mid] < s) lo = mid; else hi = mid; }
            var t = (s - trackLen[lo]) / Mathf.Max(.001f, trackLen[hi] - trackLen[lo]);
            dir = (track[hi] - track[lo]).normalized; return Vector3.Lerp(track[lo], track[hi], t);
        }

        void Update()
        {
            var dt = Time.deltaTime; var t = Time.time; var now = SharedChance.Now;
            var here = SkyDirector.Instance != null && SkyDirector.Instance.CurrentLand == Land;
            var d = MusicDirector.Instance; var playing = d != null && d.CurrentSlot.IsValid;
            var energy = SurpriseDirector.Energy;
            var id = MusicDirector.ForcedTrackId ?? (playing ? d.CurrentSlot.track.id : null);
            peace = Mathf.MoveTowards(peace, here && id == FolkKit.PeaceTrain ? 1f : 0f, dt / 2f);
            rocky = Mathf.MoveTowards(rocky, here && id == FolkKit.RockyMountainHigh ? 1f : 0f, dt / 3f);
            storm = Mathf.MoveTowards(storm, here && id == FolkKit.EdmundFitzgerald ? 1f : 0f, dt / 4f);
            bridge = Mathf.MoveTowards(bridge, here && id == FolkKit.BridgeOverTroubledWater ? 1f : 0f, dt / 3f);
            StoriesUpdate(dt, t, id, here);

            var tint = new Color(.85f, .95f, 1f); var amount = .1f;
            if (rocky > 0f) { tint = Color.Lerp(tint, new Color(1f, .85f, .5f), rocky); amount = Mathf.Lerp(amount, .4f, rocky); }
            if (storm > 0f) { tint = Color.Lerp(tint, new Color(.3f, .35f, .45f), storm); amount = Mathf.Lerp(amount, .6f, storm); }
            if (bridge > 0f) { tint = Color.Lerp(tint, new Color(1f, .92f, .8f), bridge * .7f); amount = Mathf.Max(amount, .3f * bridge); }
            SkyDirector.SetSongTint(Land, tint, amount);
            SkyDirector.SetSongRain(Land, storm * .9f);
            if (here) { var w = clouds != null ? clouds.Wind : new Vector2(1, 0); Shader.SetGlobalFloat("_EW_Wind", .2f + .2f * energy + storm * 1f); Shader.SetGlobalVector("_EW_WindDir", new Vector4(w.x, 0, w.y, 0)); }

            // the train round the lake
            var speed = 6f + peace * 4f; var head = (float)(now * speed % total);
            for (var i = 0; i < train.Count; i++)
            {
                train[i].gameObject.SetActive(here && track.Length > 1);
                if (!here || track.Length < 2) continue;
                var p = OnTrack(head - i * 3.6f, out var dir);
                train[i].SetPositionAndRotation(p + Vector3.up * .2f, Quaternion.LookRotation(new Vector3(dir.x, dir.y * .5f, dir.z)));
            }
            if (trainLights != null) trainLights.SetColor("_EmissionColor", Color.Lerp(new Color(1f, .8f, .5f) * .4f, Color.HSVToRGB(Mathf.Repeat(t * .3f, 1f), .5f, 1f) * 2.8f, peace));
            if (bridgeGlow != null) bridgeGlow.SetColor("_EmissionColor", new Color(.9f, .95f, 1f) * (.2f + bridge * (2.2f + Mathf.Sin(t * 1.2f) * .5f)));
            for (var i = 0; i < birds.Count; i++)
            {
                birds[i].gameObject.SetActive(here && storm < .5f);
                if (!here) continue;
                var a = t * (.1f + (i % 3) * .015f) + i * .6f; var r = lakeR * (.5f + (i % 4) * .15f);
                birds[i].SetPositionAndRotation(lake + new Vector3(Mathf.Cos(a) * r, 26f + Mathf.Sin(t * .3f + i) * 4f, Mathf.Sin(a) * r), Quaternion.LookRotation(new Vector3(-Mathf.Sin(a), 0, Mathf.Cos(a))) * Quaternion.Euler(0, 0, Mathf.Sin(t * 7f + i) * 22f));
            }

            nextSteam -= dt;
            if (here && nextSteam <= 0f && train.Count > 0) { nextSteam = .15f; steam.transform.position = train[0].position + train[0].forward * 1f + Vector3.up * 2.4f; steam.Emit(1); }
            nextMist -= dt;
            if (here && nextMist <= 0f && lake != Vector3.zero) { nextMist = .25f; mist.transform.position = lake + new Vector3(Random.Range(-lakeR, lakeR) * .8f, 1f, Random.Range(-lakeR, lakeR) * .8f); mist.Emit(1); }
            nextSmoke -= dt;
            if (here && nextSmoke <= 0f) { nextSmoke = .6f; foreach (var c in chimneys) { smoke.transform.position = c; smoke.Emit(1); } }
            nextFlame -= dt;
            if (here && nextFlame <= 0f) { nextFlame = .1f; foreach (var f in fires) { flame.transform.position = f; flame.Emit(2); } }
            if (shaftM != null) shaftM.SetColor("_BaseColor", new Color(1f, .74f, .3f, (.52f + .06f * Mathf.Sin(t * .4f) + .15f * bridge + .1f * rocky) * (1f - storm)));
            nextSeed -= dt;
            var cam = Camera.main;
            if (here && cam != null && nextSeed <= 0f && storm < .6f) { nextSeed = .08f; seeds.transform.position = cam.transform.position + new Vector3(Random.Range(-28f, 28f), Random.Range(-1f, 9f), Random.Range(-28f, 28f)); seeds.Emit(1); }
            nextStar -= dt;
            if (here && rocky > .3f && nextStar <= 0f) { nextStar = Random.Range(.3f, .9f); stars.transform.position = center + new Vector3(Random.Range(-200f, 200f), Random.Range(120f, 180f), Random.Range(-200f, 200f)); stars.transform.rotation = Quaternion.LookRotation(new Vector3(Random.Range(-1f, 1f), -.6f, Random.Range(-1f, 1f))); stars.Emit(1); }
        }
    }
}
