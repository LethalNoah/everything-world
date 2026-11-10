using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// Garage Rock Row at night, answering the music:
    /// - late-night sky, low dark cloud; the town glows sodium orange, colder blue in sad songs; drizzle in some
    ///   of the sad ones, so the street shines;
    /// - cars cruise up and down the Row with their headlights on (where they are comes from the shared clock, so
    ///   friends see the same cars), sometimes a police car with its lights going;
    /// - the casino's marquee bulbs chase, faster in loud songs; steam rises from the alleys; the radio mast's red
    ///   light blinks;
    /// - "Sex on Fire": every burn barrel roars up in flames; "505": the street floods red and room 505's door
    ///   pulses; "When You Were Young": searchlights sweep the sky from the casino and the marquee goes wild.
    public class GarageLife : MonoBehaviour
    {
        const string Land = "Garage Rock Row";
        public Vector3 center, streetA, streetDir = Vector3.forward, room505, casino, wave;
        public float streetLen = 300f;
        public float[] profile = new float[0];
        public Material[] marquee = new Material[0];
        public Material mastLight;
        public List<(Vector3 at, Color col)> neon = new List<(Vector3, Color)>();
        public List<Vector3> neonRedSigns = new List<Vector3>(), barrels = new List<Vector3>(), steam = new List<Vector3>();

        CloudLayer clouds;
        ParticleSystem fire, steamPs;
        Transform searchlights; Material searchMat;
        readonly List<Transform> cars = new List<Transform>();
        Transform police; Light policeLight;
        Light redFlood, light505;
        float mood = .5f, onFire, red, young;
        long songKey = long.MinValue; bool drizzle; double policeAt = -1;
        float nextFire, nextSteam;

        void Start()
        {
            clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 100; clouds.span = 2400f; clouds.key = "garage";
            clouds.minHeight = 90f; clouds.maxHeight = 160f; clouds.size = 1.5f; clouds.speed = 6f; clouds.cover = .7f;
            clouds.color = new Color(.2f, .38f, .44f);
            fire = Particles("Barrel flames", 1500, new Color(1f, .55f, .2f, .9f), .6f, 1.4f, true);
            var fm = fire.main; fm.startLifetime = new ParticleSystem.MinMaxCurve(.5f, 1.1f); fm.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
            var fs = fire.shape; fs.shapeType = ParticleSystemShapeType.Cone; fs.angle = 12f; fs.radius = .3f; fs.rotation = new Vector3(-90f, 0, 0);
            steamPs = Particles("Steam", 400, new Color(.85f, .85f, .9f, .25f), 1f, 2.4f, false);
            var sm = steamPs.main; sm.startLifetime = new ParticleSystem.MinMaxCurve(3f, 5f); sm.startSpeed = new ParticleSystem.MinMaxCurve(.6f, 1.4f);
            var ss = steamPs.shape; ss.shapeType = ParticleSystemShapeType.Cone; ss.angle = 15f; ss.radius = .3f; ss.rotation = new Vector3(-90f, 0, 0);
            for (var i = 0; i < 4; i++) cars.Add(MakeCar(new[] { new Color(.75f, .15f, .12f), new Color(.1f, .1f, .12f), new Color(.2f, .45f, .7f), new Color(.9f, .75f, .2f) }[i], false));
            police = MakeCar(new Color(.1f, .1f, .14f), true); police.gameObject.SetActive(false);
            redFlood = new GameObject("Red flood").AddComponent<Light>(); redFlood.transform.SetParent(transform, false);
            redFlood.type = LightType.Point; redFlood.range = 120f; redFlood.color = new Color(1f, .1f, .15f); redFlood.intensity = 0f;
            redFlood.transform.position = StreetPoint(streetLen * .6f, 0f) + Vector3.up * 30f;
            if (room505 != Vector3.zero)
            {
                light505 = new GameObject("Room 505").AddComponent<Light>(); light505.transform.SetParent(transform, false);
                light505.type = LightType.Point; light505.range = 14f; light505.color = new Color(1f, .2f, .25f); light505.intensity = .8f; light505.transform.position = room505;
            }
            if (casino != Vector3.zero) searchlights = Beams(casino + Vector3.up * 14f, 4, out searchMat);
        }

        float StreetY(float t)
        {
            if (profile.Length == 0) return center.y;
            var f = Mathf.Clamp(t / 8f, 0, profile.Length - 1.001f); var i = Mathf.FloorToInt(f);
            return Mathf.Lerp(profile[i], profile[Mathf.Min(i + 1, profile.Length - 1)], f - i);
        }

        Vector3 StreetPoint(float t, float across) { var p = streetA + streetDir * t + Vector3.Cross(Vector3.up, streetDir) * across; p.y = StreetY(t); return p; }

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
            var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
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

        Transform Beams(Vector3 at, int count, out Material mat)
        {
            var v = new List<Vector3>(); var c = new List<Color>(); var t = new List<int>();
            for (var i = 0; i < count; i++)
            {
                var d = Quaternion.Euler(-60f, i * 360f / count, 0) * Vector3.forward; var side = Vector3.Cross(Vector3.up, d).normalized * .8f;
                var b = v.Count; var col = new Color(.95f, .95f, 1f, .5f);
                v.Add(-side); v.Add(side); v.Add(d * 300f + side * 12f); v.Add(d * 300f - side * 12f);
                c.Add(col); c.Add(col); c.Add(new Color(1, 1, 1, 0)); c.Add(new Color(1, 1, 1, 0));
                t.AddRange(new[] { b, b + 1, b + 2, b, b + 2, b + 3 });
            }
            var mesh = new Mesh { name = "Searchlights" }; mesh.SetVertices(v); mesh.SetColors(c); mesh.SetTriangles(t, 0); mesh.RecalculateBounds();
            var go = new GameObject("Searchlights"); go.transform.SetParent(transform, false); go.transform.position = at;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>(); mat = Additive("Searchlights"); r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go.transform;
        }

        Transform MakeCar(Color paint, bool isPolice)
        {
            var mk = new MeshKit(); var body = WorldMaterials.Solid(paint, .85f);
            mk.Box(body, new Vector3(0, .65f, 0), new Vector3(2f, .6f, 4.8f), Quaternion.identity);
            mk.Box(body, new Vector3(0, 1.15f, -.3f), new Vector3(1.8f, .5f, 2.2f), Quaternion.identity);
            mk.Box(WorldMaterials.Solid(new Color(.1f, .12f, .15f), .9f), new Vector3(0, 1.17f, -.3f), new Vector3(1.82f, .38f, 2.1f), Quaternion.identity);
            foreach (var s in new[] { -1f, 1f }) foreach (var z in new[] { -1.5f, 1.5f }) mk.Prism(WorldMaterials.Solid(new Color(.05f, .05f, .05f)), new Vector3(s * .88f, .4f, z), Vector3.right * s, .42f, .3f, 12);
            foreach (var s in new[] { -.65f, .65f }) mk.Box(WorldMaterials.Glow(new Color(1f, .95f, .8f), 2.6f), new Vector3(s, .7f, 2.41f), new Vector3(.3f, .2f, .04f), Quaternion.identity);
            foreach (var s in new[] { -.7f, .7f }) mk.Box(WorldMaterials.Glow(new Color(1f, .15f, .15f), 1.8f), new Vector3(s, .7f, -2.41f), new Vector3(.35f, .12f, .04f), Quaternion.identity);
            if (isPolice)
            {
                mk.Box(WorldMaterials.Glow(new Color(1f, .1f, .1f), 3f), new Vector3(-.35f, 1.5f, -.3f), new Vector3(.6f, .2f, .3f), Quaternion.identity);
                mk.Box(WorldMaterials.Glow(new Color(.2f, .4f, 1f), 3f), new Vector3(.35f, 1.5f, -.3f), new Vector3(.6f, .2f, .3f), Quaternion.identity);
                mk.Box(WorldMaterials.Solid(new Color(.95f, .95f, .95f)), new Vector3(0, .7f, 0), new Vector3(2.02f, .3f, 2f), Quaternion.identity);
            }
            var car = mk.Build(isPolice ? "Police car" : "Cruising car", transform, false).transform;
            var head = new GameObject("Headlights").AddComponent<Light>(); head.transform.SetParent(car, false);
            head.type = LightType.Spot; head.spotAngle = 60f; head.range = 28f; head.intensity = 3f; head.color = new Color(1f, .93f, .78f);
            head.transform.localPosition = new Vector3(0, .8f, 2.6f); head.transform.localRotation = Quaternion.Euler(8f, 0, 0);
            if (isPolice)
            {
                policeLight = new GameObject("Flashers").AddComponent<Light>(); policeLight.transform.SetParent(car, false);
                policeLight.type = LightType.Point; policeLight.range = 18f; policeLight.intensity = 3f; policeLight.transform.localPosition = new Vector3(0, 2f, -.3f);
            }
            return car;
        }

        void Drive(Transform car, double now, float speed, float offset, bool forward)
        {
            // up the Row in one lane, back down in the other, timed by the shared clock
            var loop = streetLen * 2f;
            var d = (float)((now * speed + offset) % loop);
            var goingUp = forward ? d < streetLen : d >= streetLen;
            var t = d < streetLen ? d : loop - d;
            if (!forward) t = streetLen - t;
            var lane = goingUp ? 2.8f : -2.8f;
            var p = StreetPoint(Mathf.Clamp(t, 2f, streetLen - 2f), lane);
            var dir = goingUp ? streetDir : -streetDir;
            car.SetPositionAndRotation(p + Vector3.up * .05f, Quaternion.LookRotation(dir));
        }

        void Update()
        {
            var dt = Time.deltaTime; var t = Time.time; var now = SharedChance.Now;
            var here = SkyDirector.Instance != null && SkyDirector.Instance.CurrentLand == Land;
            var d = MusicDirector.Instance;
            var playing = d != null && d.CurrentSlot.IsValid;
            var energy = SurpriseDirector.Energy;
            var valence = playing ? d.CurrentSlot.track.valence : .45f;
            var id = MusicDirector.ForcedTrackId ?? (playing ? d.CurrentSlot.track.id : null);

            if (SharedChance.CurrentSong(out var song, out var start, out var length) && song != songKey)
            {
                songKey = song;
                drizzle = playing && valence < .4f && SharedChance.Roll(SharedChance.Key("garage-drizzle"), song, .4);
                var k = SharedChance.Key("garage-police");
                policeAt = SharedChance.Roll(k, song, .25) ? start + length * (.1 + .6 * SharedChance.Value(k, song, 1)) : -1;
            }
            SkyDirector.SetSongRain(Land, drizzle ? .3f : 0f);

            onFire = Mathf.MoveTowards(onFire, here && id == GarageKit.SexOnFire ? 1f : 0f, dt / 2f);
            red = Mathf.MoveTowards(red, here && id == GarageKit.FiveOhFive ? 1f : 0f, dt / 3f);
            young = Mathf.MoveTowards(young, here && id == GarageKit.WhenYouWereYoung ? 1f : 0f, dt / 3f);

            mood = Mathf.MoveTowards(mood, Mathf.Clamp01(valence * 1.3f), dt / 4f);
            var tint = Color.Lerp(new Color(.35f, .8f, .95f), new Color(1f, .85f, .4f), mood);
            SkyDirector.SetSongTint(Land, Color.Lerp(tint, new Color(1f, .15f, .2f), red), Mathf.Lerp(.35f, .75f, red));
            if (redFlood != null) redFlood.intensity = here ? red * 5f : 0f;
            if (light505 != null) light505.intensity = .8f + red * (3f + 2f * Mathf.Sin(t * 5f));
            if (here)
            {
                var w = clouds != null ? clouds.Wind : new Vector2(1, 0);
                Shader.SetGlobalFloat("_EW_Wind", .25f + .25f * energy);
                Shader.SetGlobalVector("_EW_WindDir", new Vector4(w.x, 0, w.y, 0));
            }

            // cars cruising the Row
            for (var i = 0; i < cars.Count; i++)
            {
                cars[i].gameObject.SetActive(here);
                if (here) Drive(cars[i], now, 7f + i * 1.3f, i * streetLen * .55f, i % 2 == 0);
            }
            var pt = (float)(now - policeAt);
            var policeOn = here && policeAt > 0 && pt > 0f && pt < streetLen / 11f;
            police.gameObject.SetActive(policeOn);
            if (policeOn)
            {
                var p = StreetPoint(Mathf.Clamp(pt * 11f, 0f, streetLen), 2.8f);
                police.SetPositionAndRotation(p + Vector3.up * .05f, Quaternion.LookRotation(streetDir));
                policeLight.color = Mathf.Repeat(t * 4f, 1f) < .5f ? new Color(1f, .1f, .1f) : new Color(.2f, .4f, 1f);
            }

            // the casino's marquee chasing, wild in "When You Were Young"
            var chase = Mathf.FloorToInt(t * (3f + 6f * energy + 10f * young));
            for (var i = 0; i < marquee.Length; i++) marquee[i].SetColor("_EmissionColor", new Color(1f, .85f, .45f) * ((chase % marquee.Length) == i ? 3.2f : .5f + young));
            if (searchlights != null)
            {
                searchlights.rotation = Quaternion.Euler(Mathf.Sin(t * .4f) * 10f, t * 25f, 0);
                searchMat.SetColor("_BaseColor", new Color(1f, 1f, 1f, here ? young * .8f : 0f));
            }
            if (mastLight != null) mastLight.SetColor("_EmissionColor", new Color(1f, .1f, .1f) * (Mathf.Repeat(t, 1.6f) < .3f ? 3f : .2f));

            // flames out of every burn barrel in "Sex on Fire"; steam from the alleys always
            nextFire -= dt;
            if (here && nextFire <= 0f && barrels.Count > 0)
            {
                nextFire = .08f;
                foreach (var b in barrels)
                {
                    fire.transform.position = b;
                    fire.Emit(onFire > .5f ? 14 : 1);
                }
            }
            nextSteam -= dt;
            if (here && nextSteam <= 0f && steam.Count > 0)
            {
                nextSteam = .4f;
                foreach (var s in steam) { steamPs.transform.position = s; steamPs.Emit(1); }
            }
        }
    }
}
