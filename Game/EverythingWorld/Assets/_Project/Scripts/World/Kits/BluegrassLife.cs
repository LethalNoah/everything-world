using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Bluegrass Hollows on a bright summer afternoon, answering the music:
    /// - glows tied to songs brighten while their song plays (Billy Strings' jail window, mire and tinfoil, Bill
    ///   Monroe's footlights, Doc Watson's window, Tony Rice's chapel window, Ricky Skaggs' lamp, Del McCoury's
    ///   headlight); Billy Strings' panel wall cycles its colours and two rings of light swirl round the banjo,
    ///   faster with the music; mist lies round Ricky Skaggs, Del McCoury, Flatt & Scruggs, the mire and the mine;
    /// - the instruments over the picking circle float round it, bobbing and swaying as if played (faster with the
    ///   music's energy); the mill wheel turns; a mule pulls a covered wagon along the longest trail and back (from
    ///   the shared clock, so friends see the same wagon); butterflies flutter; chimney smoke and the still's fire;
    ///   a pale moon hangs in the daytime sky; embers drift up from the fire lookout;
    /// - "Foggy Mountain Breakdown": fog rolls down into the hollows and the instruments whirl; "Blue Moon Of
    ///   Kentucky": the sky deepens to blue and the moon glows big and blue (and the moon at the fence blazes);
    ///   "Super Moon": the moon swells huge; "Big Rock Candy Mountain": the candy mountain sparkles and the lemonade
    ///   spring bubbles up; "Wagon Wheel": the wagon picks up speed; "I'm a Man of Constant Sorrow": grey skies
    ///   and rain; "The Ballad of Jed Clampett": the derrick gushes; "Wildfire": the lookout throws sparks.
    public class BluegrassLife : MonoBehaviour
    {
        const string Land = "The Bluegrass Hollows";
        const string JedClampett = "0txfcNqfEvdOxk9Ue35220", Wildfire = "0EIS2zaB3aQGtKOvzrodOU";
        public Vector3 center, circle, lemonade, candy, gusher, embers;
        public List<Transform> instruments = new List<Transform>();
        public Vector3[] creek = new Vector3[0], route = new Vector3[0];
        public Transform wheel; public Vector3 wheelAxis = Vector3.right;
        public Material blueMoon;
        public List<Vector3> chimneys = new List<Vector3>(), fires = new List<Vector3>(), mists = new List<Vector3>();
        public List<(string song, Material m, Color c)> songGlows = new List<(string, Material, Color)>();
        public List<(Material m, Color c, float phase)> panels = new List<(Material, Color, float)>();
        public List<(Transform t, float dir)> swirls = new List<(Transform, float)>();
        readonly Dictionary<string, float> songAmount = new Dictionary<string, float>();
        ParticleSystem mist; float nextMist;

        CloudLayer clouds; ParticleSystem smoke, flame, fog, sparkle, fizz, oil, sparks;
        Transform moon, wagon; Material moonM;
        readonly List<Transform> butterflies = new List<Transform>(); readonly List<Vector3> flutterHome = new List<Vector3>();
        float[] routeLen = new float[0]; float routeTotal;
        float foggy, blue, super, candyOn, wagonOn, sorrow, jed, wild;
        float nextSmoke, nextFlame, nextFog, nextSparkle, nextFizz, nextOil, nextSpark;

        void Start()
        {
            clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 40; clouds.span = 2400f; clouds.key = "bluegrass";
            clouds.minHeight = 130f; clouds.maxHeight = 190f; clouds.size = 2f; clouds.speed = 3f; clouds.cover = .4f;
            clouds.color = new Color(1f, 1f, 1f);
            smoke = Particles("Chimney smoke", 400, new Color(.75f, .75f, .78f, .35f), .5f, 1.2f, false);
            var km = smoke.main; km.startLifetime = new ParticleSystem.MinMaxCurve(3f, 5f); km.startSpeed = new ParticleSystem.MinMaxCurve(.6f, 1.2f);
            flame = Particles("Fire", 300, new Color(1f, .55f, .2f, .9f), .25f, .55f, true);
            var fm = flame.main; fm.startLifetime = new ParticleSystem.MinMaxCurve(.4f, .9f); fm.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.4f);
            fog = Particles("Hollow fog", 700, new Color(.95f, .97f, 1f, .35f), 12f, 22f, false);
            var gm = fog.main; gm.startLifetime = new ParticleSystem.MinMaxCurve(8f, 12f); gm.startSpeed = new ParticleSystem.MinMaxCurve(.3f, .9f);
            sparkle = Particles("Candy sparkle", 300, new Color(1f, .8f, .95f, 1f), .15f, .35f, true);
            var spm = sparkle.main; spm.startLifetime = new ParticleSystem.MinMaxCurve(1f, 2f); spm.startSpeed = new ParticleSystem.MinMaxCurve(.5f, 2f);
            fizz = Particles("Lemonade fizz", 300, new Color(1f, .95f, .5f, .9f), .1f, .22f, true);
            var fzm = fizz.main; fzm.startLifetime = new ParticleSystem.MinMaxCurve(.8f, 1.6f); fzm.startSpeed = new ParticleSystem.MinMaxCurve(2f, 4f); fzm.gravityModifier = .6f;
            oil = Particles("Gusher", 400, new Color(.05f, .05f, .06f, .9f), .2f, .45f, false);
            var om = oil.main; om.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 2.4f); om.startSpeed = new ParticleSystem.MinMaxCurve(8f, 12f); om.gravityModifier = 1f;
            var os = oil.shape; os.shapeType = ParticleSystemShapeType.Cone; os.angle = 8f; os.radius = .2f; os.rotation = new Vector3(-90f, 0, 0);
            mist = Particles("Ground mist", 500, new Color(.92f, .95f, 1f, .2f), 3.5f, 7f, false);
            var mm = mist.main; mm.startLifetime = new ParticleSystem.MinMaxCurve(7f, 11f); mm.startSpeed = new ParticleSystem.MinMaxCurve(.1f, .35f);
            sparks = Particles("Sparks", 300, new Color(1f, .6f, .2f, 1f), .08f, .18f, true);
            var skm = sparks.main; skm.startLifetime = new ParticleSystem.MinMaxCurve(2f, 4f); skm.startSpeed = new ParticleSystem.MinMaxCurve(.5f, 2f); skm.gravityModifier = -.05f;
            // the day moon: a pale disc far off in the sky, kept in place round the camera
            moonM = new Material(WorldMaterials.Glow(new Color(.88f, .92f, 1f), .35f)) { name = "Day moon" };
            var mk = new MeshKit(); mk.Prism(moonM, Vector3.zero, Vector3.forward, 1f, .01f, 32);
            moon = mk.BuildSingle("Day moon", transform, false).transform;
            // the covered wagon and its mule
            if (route.Length > 1)
            {
                routeLen = new float[route.Length]; for (var i = 1; i < route.Length; i++) routeLen[i] = routeLen[i - 1] + Vector3.Distance(route[i - 1], route[i]); routeTotal = routeLen[route.Length - 1];
                var w = new MeshKit(); var wood = WorldMaterials.Solid(new Color(.55f, .4f, .27f)); var canvas = WorldMaterials.Solid(new Color(.95f, .92f, .84f)); var dark = WorldMaterials.Solid(new Color(.1f, .1f, .1f)); var mule = WorldMaterials.Solid(new Color(.45f, .35f, .28f));
                w.Box(wood, new Vector3(0, 1f, -1f), new Vector3(1.6f, .5f, 3f), Quaternion.identity);
                for (var k = 0; k < 7; k++) { var a = Mathf.Lerp(-80f, 80f, k / 6f) * Mathf.Deg2Rad; w.Box(canvas, new Vector3(Mathf.Sin(a) * .85f, 1.3f + Mathf.Cos(a) * .9f, -1f), new Vector3(.42f, .1f, 3f), Quaternion.Euler(0, 0, -Mathf.Rad2Deg * a)); }
                // spoked wheels: a rim, eight spokes and a hub
                foreach (var s in new[] { -.85f, .85f }) foreach (var zz in new[] { -2.1f, .1f })
                {
                    var hub = new Vector3(s, .55f, zz);
                    for (var k = 0; k < 14; k++) { float a0 = k * Mathf.PI / 7f, a1 = (k + 1) * Mathf.PI / 7f; var p0 = hub + new Vector3(0, Mathf.Cos(a0), Mathf.Sin(a0)) * .52f; var p1 = hub + new Vector3(0, Mathf.Cos(a1), Mathf.Sin(a1)) * .52f; w.Box(dark, (p0 + p1) / 2f, new Vector3(.1f, .07f, Vector3.Distance(p0, p1) + .03f), Quaternion.LookRotation(p1 - p0)); }
                    for (var k = 0; k < 4; k++) w.Box(wood, hub, new Vector3(.05f, 1f, .05f), Quaternion.Euler(k * 45f, 0, 0));
                    w.Prism(wood, hub - Vector3.right * .1f, Vector3.right, .1f, .2f, 8);
                }
                w.Box(wood, new Vector3(0, .9f, .9f), new Vector3(.12f, .12f, 1.6f), Quaternion.identity);
                w.Box(mule, new Vector3(0, 1.25f, 2.3f), new Vector3(.7f, .7f, 1.5f), Quaternion.identity); w.Box(mule, new Vector3(0, 1.75f, 3.2f), new Vector3(.4f, .45f, .8f), Quaternion.Euler(-30f, 0, 0));
                foreach (var s in new[] { -.12f, .12f }) w.Box(mule, new Vector3(s, 2.2f, 3.3f), new Vector3(.08f, .45f, .1f), Quaternion.Euler(-10f, 0, s * 80f));
                foreach (var s in new[] { -.25f, .25f }) foreach (var zz in new[] { 1.8f, 2.8f }) w.Box(mule, new Vector3(s, .5f, zz), new Vector3(.14f, 1f, .14f), Quaternion.identity);
                wagon = w.BuildSingle("Covered wagon", transform, false).transform;
            }
            Color[] wings = { new Color(1f, .8f, .1f), new Color(.35f, .6f, 1f), new Color(1f, .5f, .15f), new Color(.95f, .95f, .95f) };
            for (var i = 0; i < 16; i++)
            {
                var mkb = new MeshKit(); var m = WorldMaterials.Solid(wings[i % wings.Length], .3f);
                foreach (var s in new[] { -1f, 1f }) { mkb.Tri(m, Vector3.zero, new Vector3(s * .22f, 0, .12f), new Vector3(s * .2f, 0, -.14f), Vector3.up * .1f); mkb.Tri(m, Vector3.zero, new Vector3(s * .2f, 0, -.14f), new Vector3(s * .22f, 0, .12f), Vector3.down * .1f); }
                butterflies.Add(mkb.BuildSingle("Butterfly", transform, false).transform);
                var a = i * 2.4f; flutterHome.Add((i % 3 == 2 && candy != Vector3.zero ? candy : center) + new Vector3(Mathf.Cos(a) * (8f + i * 1.5f), 1.5f, Mathf.Sin(a) * (8f + i * 1.5f)));
            }
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

        Vector3 OnRoute(float s, out Vector3 dir)
        {
            dir = Vector3.forward; if (route.Length < 2) return center;
            var lo = 0; var hi = route.Length - 1;
            while (hi - lo > 1) { var mid = (lo + hi) / 2; if (routeLen[mid] < s) lo = mid; else hi = mid; }
            var t = (s - routeLen[lo]) / Mathf.Max(.001f, routeLen[hi] - routeLen[lo]);
            dir = (route[hi] - route[lo]).normalized; return Vector3.Lerp(route[lo], route[hi], t);
        }

        void Update()
        {
            var dt = Time.deltaTime; var t = Time.time; var now = SharedChance.Now;
            var here = SkyDirector.Instance != null && SkyDirector.Instance.CurrentLand == Land;
            var d = MusicDirector.Instance; var playing = d != null && d.CurrentSlot.IsValid;
            var energy = SurpriseDirector.Energy;
            var id = MusicDirector.ForcedTrackId ?? (playing ? d.CurrentSlot.track.id : null);
            foggy = Mathf.MoveTowards(foggy, here && id == BluegrassKit.FoggyMountain ? 1f : 0f, dt / 4f);
            blue = Mathf.MoveTowards(blue, here && id == BluegrassKit.BlueMoon ? 1f : 0f, dt / 3f);
            super = Mathf.MoveTowards(super, here && id == BluegrassKit.SuperMoon ? 1f : 0f, dt / 4f);
            candyOn = Mathf.MoveTowards(candyOn, here && id == BluegrassKit.CandyMountain ? 1f : 0f, dt / 2f);
            wagonOn = Mathf.MoveTowards(wagonOn, here && id == BluegrassKit.WagonWheel ? 1f : 0f, dt / 2f);
            sorrow = Mathf.MoveTowards(sorrow, here && id == BluegrassKit.ConstantSorrow ? 1f : 0f, dt / 4f);
            jed = Mathf.MoveTowards(jed, here && id == JedClampett ? 1f : 0f, dt / 2f);
            wild = Mathf.MoveTowards(wild, here && id == Wildfire ? 1f : 0f, dt / 2f);

            var tint = new Color(1f, .97f, .9f); var amount = .05f;
            if (foggy > 0f) { tint = Color.Lerp(tint, new Color(.85f, .88f, .92f), foggy); amount = Mathf.Lerp(amount, .45f, foggy); }
            if (blue > 0f) { tint = Color.Lerp(tint, new Color(.3f, .42f, .85f), blue); amount = Mathf.Lerp(amount, .55f, blue); }
            if (super > 0f) { tint = Color.Lerp(tint, new Color(.55f, .55f, .95f), super * .6f); amount = Mathf.Max(amount, .3f * super); }
            if (sorrow > 0f) { tint = Color.Lerp(tint, new Color(.45f, .48f, .55f), sorrow); amount = Mathf.Lerp(amount, .55f, sorrow); }
            SkyDirector.SetSongTint(Land, tint, amount);
            SkyDirector.SetSongRain(Land, sorrow * .8f);
            if (here) { var w = clouds != null ? clouds.Wind : new Vector2(1, 0); Shader.SetGlobalFloat("_EW_Wind", .25f + .25f * energy + sorrow * .6f); Shader.SetGlobalVector("_EW_WindDir", new Vector4(w.x, 0, w.y, 0)); }

            // the picking circle: the instruments float round, bob and sway as if played
            var spin = (.12f + energy * .15f) * (1f + foggy * 4f);
            for (var i = 0; i < instruments.Count; i++)
            {
                var tr = instruments[i]; if (tr == null) continue;
                var a = (float)(now * spin % (Mathf.PI * 2f)) + i * Mathf.PI * 2f / instruments.Count;
                var p = circle + new Vector3(Mathf.Cos(a) * 2.4f, (i == 4 ? 2f : 2.2f) + Mathf.Sin(t * 1.3f + i) * .25f, Mathf.Sin(a) * 2.4f);
                var face = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                tr.SetPositionAndRotation(p, Quaternion.LookRotation(face) * Quaternion.Euler(-15f + Mathf.Sin(t * 2f + i) * 6f, 0, 25f + Mathf.Sin(t * (3f + energy * 4f) + i * 1.7f) * (8f + foggy * 30f)));
            }
            if (wheel != null) wheel.Rotate(wheelAxis, -dt * (25f + energy * 20f), Space.World);
            if (wagon != null)
            {
                wagon.gameObject.SetActive(here);
                if (here)
                {
                    var speed = 1.6f * (1f + wagonOn * 1.5f); var s = Mathf.PingPong((float)(now * 1.6f % (routeTotal * 2f)), routeTotal);
                    var back = Mathf.Repeat((float)(now * 1.6f), routeTotal * 2f) > routeTotal;
                    var p = OnRoute(Mathf.Clamp(s, 0f, routeTotal), out var dir); if (back) dir = -dir;
                    wagon.SetPositionAndRotation(p + Vector3.up * .1f, Quaternion.LookRotation(new Vector3(dir.x, 0, dir.z)) * Quaternion.Euler(0, 0, Mathf.Sin(t * 4f * speed) * 1.5f));
                }
            }
            for (var i = 0; i < butterflies.Count; i++)
            {
                butterflies[i].gameObject.SetActive(here && sorrow < .5f);
                if (!here) continue;
                var h = flutterHome[i]; var a = t * (.4f + (i % 4) * .1f) + i;
                var p = h + new Vector3(Mathf.Sin(a) * 3f + Mathf.Sin(a * 2.3f), Mathf.Sin(a * 3.1f) * .6f + 1.2f, Mathf.Cos(a * .8f) * 3f);
                butterflies[i].position = new Vector3(p.x, Mathf.Max(p.y, h.y), p.z);
                butterflies[i].rotation = Quaternion.LookRotation(new Vector3(Mathf.Cos(a), 0, -Mathf.Sin(a * .8f))) * Quaternion.Euler(0, 0, 0);
                butterflies[i].localScale = new Vector3(Mathf.Abs(Mathf.Sin(t * 14f + i)) * .8f + .2f, 1f, 1f);
            }
            // the day moon
            var cam = Camera.main;
            if (moon != null)
            {
                moon.gameObject.SetActive(here && cam != null);
                if (here && cam != null)
                {
                    var mdir = new Vector3(-.55f, .42f, .72f).normalized; var size = 34f * (1f + super * 2.2f + blue * .6f);
                    moon.SetPositionAndRotation(cam.transform.position + mdir * 900f, Quaternion.LookRotation(-mdir)); moon.localScale = new Vector3(size, size, 1f);
                    var mc = Color.Lerp(new Color(.88f, .92f, 1f) * .35f, new Color(.45f, .65f, 1f) * 2.4f, Mathf.Max(blue, super * .7f));
                    moonM.SetColor("_EmissionColor", mc); moonM.SetColor("_BaseColor", Color.Lerp(new Color(.9f, .93f, 1f), new Color(.6f, .75f, 1f), blue));
                }
            }
            if (blueMoon != null) blueMoon.SetColor("_EmissionColor", new Color(.35f, .55f, 1f) * (.25f + blue * 1.1f + Mathf.Sin(t * .8f) * .04f));

            nextSmoke -= dt;
            if (here && nextSmoke <= 0f) { nextSmoke = .6f; foreach (var c in chimneys) { smoke.transform.position = c; smoke.Emit(1); } }
            nextFlame -= dt;
            if (here && nextFlame <= 0f) { nextFlame = .12f; foreach (var f in fires) { flame.transform.position = f; flame.Emit(2); } }
            nextFog -= dt;
            if (here && foggy > .05f && nextFog <= 0f && creek.Length > 0) { nextFog = .08f / foggy; var p = creek[Random.Range(0, creek.Length)]; fog.transform.position = p + new Vector3(Random.Range(-14f, 14f), Random.Range(1f, 5f), Random.Range(-14f, 14f)); fog.Emit(1); }
            nextSparkle -= dt;
            if (here && candy != Vector3.zero && nextSparkle <= 0f) { nextSparkle = candyOn > .1f ? .03f : .4f; sparkle.transform.position = candy + new Vector3(Random.Range(-5f, 5f), Random.Range(-3f, 5f), Random.Range(-5f, 5f)); sparkle.Emit(1); }
            nextFizz -= dt;
            if (here && lemonade != Vector3.zero && nextFizz <= 0f) { nextFizz = candyOn > .1f ? .02f : .5f; fizz.transform.position = lemonade + new Vector3(Random.Range(-.6f, .6f), .2f, Random.Range(-.6f, .6f)); fizz.Emit(1); }
            nextOil -= dt;
            if (here && jed > .1f && gusher != Vector3.zero && nextOil <= 0f) { nextOil = .02f; oil.transform.position = gusher; oil.Emit(2); }
            // song glows, Billy Strings' panels and swirls, mist round the old-timers
            foreach (var (song, m, c) in songGlows)
            {
                if (m == null) continue;
                songAmount.TryGetValue(song, out var k);
                k = Mathf.MoveTowards(k, here && id == song ? 1f : 0f, dt / 2f); songAmount[song] = k;
                m.SetColor("_EmissionColor", c * (1f + k * (2.5f + Mathf.Sin(t * 2.2f) * .6f)));
            }
            foreach (var (m, c, phase) in panels) if (m != null) m.SetColor("_EmissionColor", c * (.4f + 1f * (.5f + .5f * Mathf.Sin(t * (1.1f + energy * 1.5f) + phase))) * (1f + energy * .6f));
            foreach (var (tr, sd) in swirls) if (tr != null) tr.Rotate(0, 0, sd * (25f + energy * 70f) * dt, Space.Self);
            nextMist -= dt;
            if (here && mists.Count > 0 && nextMist <= 0f) { nextMist = .3f; var mp = mists[Random.Range(0, mists.Count)]; mist.transform.position = mp + new Vector3(Random.Range(-8f, 8f), Random.Range(.4f, 1.6f), Random.Range(-8f, 8f)); mist.Emit(1); }
            nextSpark -= dt;
            if (here && embers != Vector3.zero && nextSpark <= 0f) { nextSpark = wild > .1f ? .02f : .7f; sparks.transform.position = embers + new Vector3(Random.Range(-1.2f, 1.2f), 1f, Random.Range(-1.2f, 1.2f)); sparks.Emit(1); }
        }
    }
}
