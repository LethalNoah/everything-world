using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Kingdom of Classic Rock on a bright afternoon, answering the music:
    /// - white cumulus drifting over a blue sky; a flock of birds wheeling over the village; the airship rides
    ///   gently at its mast and the yellow submarine bobs on the pond; jukeboxes and lamps pulse with the song;
    /// - a rain cloud always hangs over the Doors' doorway, raining on it alone; Hendrix's guitar always burns;
    /// - now and then (the shared clock decides, so friends see the same) a hot-air balloon drifts across;
    /// - "Lucy in the Sky with Diamonds": the sky turns soft violet and diamonds sparkle over the pond;
    ///   "Stairway to Heaven": the stair stones light one by one up to the glowing door as the song goes on;
    ///   "Space Oddity": the rocket counts down and lifts off, climbing out of sight by the end of the song;
    ///   "Purple Haze": purple mist rolls over the land; "Bohemian Rhapsody": the sky darkens and lightning flashes.
    public class ClassicLife : MonoBehaviour
    {
        const string Land = "Kingdom of Classic Rock";
        public Vector3 center, pond, hall, hallEye, hallFlame, stairTop, rocketPad, guitarFire, rainCloud;
        public List<Vector3> candles = new List<Vector3>();
        public Transform submarine, airship, rocket;
        public Material[] stairSteps = new Material[0];
        public Material heavenDoor;
        public List<Vector3> landmarks = new List<Vector3>(), jukeboxes = new List<Vector3>();

        CloudLayer clouds;
        ParticleSystem sparkle, haze, fire, rain, exhaust, motes;
        Transform beam; Material beamMat; float nextMote, nextFlame;
        readonly List<Transform> birds = new List<Transform>();
        Transform balloon;
        Light lightning, jukeLight;
        Vector3 subHome, shipHome; Quaternion subRot, shipRot;
        float lucy, stair, purple, bohemian, oddity, flash;
        long songKey = long.MinValue; double songStart, songLength = 1; double balloonAt = -1;
        float nextSparkle, nextHaze, nextFire, nextRain, nextBolt;

        void Start()
        {
            clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 70; clouds.span = 2400f; clouds.key = "classic";
            clouds.minHeight = 120f; clouds.maxHeight = 180f; clouds.size = 1.6f; clouds.speed = 5f; clouds.cover = .45f;
            clouds.color = new Color(1f, .9f, .75f);
            // gold dust drifting in the low sun
            motes = Particles("Gold dust", 1500, new Color(1f, .85f, .5f, .8f), .05f, .1f, true);
            var mm = motes.main; mm.startLifetime = new ParticleSystem.MinMaxCurve(5f, 9f); mm.startSpeed = new ParticleSystem.MinMaxCurve(.05f, .2f);
            var mn = motes.noise; mn.enabled = true; mn.strength = .3f; mn.frequency = .3f;
            var mc = motes.colorOverLifetime; var mg = new Gradient();
            mg.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, .3f), new GradientAlphaKey(1f, .7f), new GradientAlphaKey(0f, 1f) });
            mc.color = mg;
            // a soft shaft of golden light rising through the eye of the Hall's dome
            if (hallEye != Vector3.zero)
            {
                var v = new List<Vector3>(); var col = new List<Color>(); var tri = new List<int>();
                const int seg = 16;
                for (var k = 0; k <= seg; k++)
                {
                    var a = k * Mathf.PI * 2f / seg; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                    v.Add(d * 2.2f); v.Add(d * 6f + Vector3.up * 140f);
                    col.Add(new Color(1f, .85f, .55f, .35f)); col.Add(new Color(1f, .9f, .7f, 0f));
                    if (k < seg) { var b = k * 2; tri.AddRange(new[] { b, b + 1, b + 3, b, b + 3, b + 2 }); }
                }
                var mesh = new Mesh { name = "Hall light" }; mesh.SetVertices(v); mesh.SetColors(col); mesh.SetTriangles(tri, 0); mesh.RecalculateBounds();
                var go = new GameObject("Hall light"); go.transform.SetParent(transform, false); go.transform.position = hallEye;
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>(); beamMat = Additive("Hall light"); mr.sharedMaterial = beamMat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                beam = go.transform;
            }
            sparkle = Particles("Diamonds", 800, new Color(1f, 1f, 1f, 1f), .5f, 1.4f, true);
            var sm = sparkle.main; sm.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 3f); sm.startSpeed = 0f;
            var sc = sparkle.colorOverLifetime; var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(new Color(.8f, .9f, 1f), 0f), new GradientColorKey(new Color(1f, .7f, 1f), .5f), new GradientColorKey(new Color(1f, 1f, .8f), 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, .3f), new GradientAlphaKey(0f, 1f) });
            sc.color = g;
            haze = Particles("Purple haze", 600, new Color(.6f, .35f, 1f, .22f), 14f, 26f, false);
            var hm = haze.main; hm.startLifetime = new ParticleSystem.MinMaxCurve(8f, 12f); hm.startSpeed = new ParticleSystem.MinMaxCurve(.5f, 1.5f);
            fire = Particles("Guitar fire", 300, new Color(1f, .55f, .2f, .9f), .3f, .8f, true);
            var fm = fire.main; fm.startLifetime = new ParticleSystem.MinMaxCurve(.4f, .9f); fm.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3f);
            var fs = fire.shape; fs.shapeType = ParticleSystemShapeType.Cone; fs.angle = 15f; fs.radius = .25f; fs.rotation = new Vector3(-90f, 0, 0);
            rain = Particles("Doors rain", 600, new Color(.75f, .8f, .9f, .6f), .05f, .08f, false);
            var rm = rain.main; rm.startLifetime = 1f; rm.startSpeed = 0f; rm.gravityModifier = 1.2f;
            var rs = rain.shape; rs.shapeType = ParticleSystemShapeType.Box; rs.scale = new Vector3(6f, .1f, 3f);
            var rr = rain.GetComponent<ParticleSystemRenderer>(); rr.renderMode = ParticleSystemRenderMode.Stretch; rr.velocityScale = .06f;
            exhaust = Particles("Rocket exhaust", 800, new Color(1f, .8f, .5f, .8f), 1f, 3f, true);
            var em = exhaust.main; em.startLifetime = new ParticleSystem.MinMaxCurve(.6f, 1.4f); em.startSpeed = new ParticleSystem.MinMaxCurve(4f, 8f);
            var es = exhaust.shape; es.shapeType = ParticleSystemShapeType.Cone; es.angle = 20f; es.radius = .6f; es.rotation = new Vector3(90f, 0, 0);

            var bird = WorldMaterials.Solid(new Color(.15f, .15f, .18f));
            for (var i = 0; i < 14; i++)
            {
                var mk = new MeshKit();
                mk.Tri(bird, new Vector3(0, 0, .3f), new Vector3(-.9f, .15f, -.1f), new Vector3(0, 0, -.3f), Vector3.up);
                mk.Tri(bird, new Vector3(0, 0, .3f), new Vector3(.9f, .15f, -.1f), new Vector3(0, 0, -.3f), Vector3.up);
                mk.Tri(bird, new Vector3(0, 0, .3f), new Vector3(0, 0, -.3f), new Vector3(-.9f, .15f, -.1f), Vector3.down);
                mk.Tri(bird, new Vector3(0, 0, .3f), new Vector3(0, 0, -.3f), new Vector3(.9f, .15f, -.1f), Vector3.down);
                birds.Add(mk.BuildSingle("Bird", transform, false).transform);
            }
            balloon = MakeBalloon(); balloon.gameObject.SetActive(false);
            lightning = new GameObject("Lightning").AddComponent<Light>(); lightning.transform.SetParent(transform, false);
            lightning.type = LightType.Point; lightning.range = 900f; lightning.color = new Color(.85f, .9f, 1f); lightning.intensity = 0f;
            lightning.transform.position = center + Vector3.up * 220f;
            if (jukeboxes.Count > 0)
            {
                jukeLight = new GameObject("Jukebox pulse").AddComponent<Light>(); jukeLight.transform.SetParent(transform, false);
                jukeLight.type = LightType.Point; jukeLight.range = 10f; jukeLight.color = new Color(1f, .5f, .8f); jukeLight.transform.position = jukeboxes[0] + Vector3.up;
            }
            if (submarine != null) { subHome = submarine.position; subRot = submarine.rotation; }
            if (airship != null) { shipHome = airship.position; shipRot = airship.rotation; }
        }

        Transform MakeBalloon()
        {
            var mk = new MeshKit();
            Color[] stripes = { new Color(.9f, .2f, .2f), new Color(1f, .85f, .2f), new Color(.2f, .5f, .9f), new Color(.95f, .95f, .9f) };
            const int seg = 12, rings = 8;
            for (var r = 0; r < rings; r++)
            {
                float a0 = r / (float)rings * Mathf.PI, a1 = (r + 1) / (float)rings * Mathf.PI;
                for (var s = 0; s < seg; s++)
                {
                    float b0 = s * Mathf.PI * 2f / seg, b1 = (s + 1) * Mathf.PI * 2f / seg;
                    Vector3 P(float a, float b) { var rad = Mathf.Sin(a) * 9f * (a > Mathf.PI * .6f ? Mathf.Lerp(1f, .35f, (a - Mathf.PI * .6f) / (Mathf.PI * .4f)) : 1f); return new Vector3(Mathf.Cos(b) * rad, Mathf.Cos(a) * 11f, Mathf.Sin(b) * rad); }
                    mk.Quad(WorldMaterials.Solid(stripes[s % stripes.Length], .4f), P(a0, b0), P(a0, b1), P(a1, b1), P(a1, b0), Vector3.zero);
                }
            }
            mk.Box(WorldMaterials.Solid(new Color(.5f, .38f, .24f)), Vector3.down * 15f, new Vector3(2f, 1.4f, 2f), Quaternion.identity);
            foreach (var d in new[] { new Vector3(1, 0, 1), new Vector3(-1, 0, 1), new Vector3(1, 0, -1), new Vector3(-1, 0, -1) })
                mk.Box(WorldMaterials.Solid(new Color(.3f, .25f, .2f)), Vector3.down * 12.4f + d * .85f, new Vector3(.05f, 4f, .05f), Quaternion.identity);
            mk.Box(WorldMaterials.Glow(new Color(1f, .6f, .2f), 2.4f), Vector3.down * 13.6f, new Vector3(.5f, .5f, .5f), Quaternion.identity);
            return mk.BuildSingle("Hot-air balloon", transform, false).transform;
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

        void Update()
        {
            var dt = Time.deltaTime; var t = Time.time; var now = SharedChance.Now;
            var here = SkyDirector.Instance != null && SkyDirector.Instance.CurrentLand == Land;
            var d = MusicDirector.Instance;
            var playing = d != null && d.CurrentSlot.IsValid;
            var energy = SurpriseDirector.Energy;
            var id = MusicDirector.ForcedTrackId ?? (playing ? d.CurrentSlot.track.id : null);

            if (SharedChance.CurrentSong(out var song, out var start, out var length) && song != songKey)
            {
                songKey = song; songStart = start; songLength = System.Math.Max(1, length);
                var k = SharedChance.Key("classic-balloon");
                balloonAt = SharedChance.Roll(k, song, .35) ? start + length * .05 * SharedChance.Value(k, song, 1) : -1;
            }
            var progress = Mathf.Clamp01((float)((now - songStart) / songLength));
            if (MusicDirector.ForcedTrackId != null) progress = Mathf.Repeat(t / 40f, 1f); // the photo tour: show the moment mid-flight

            lucy = Mathf.MoveTowards(lucy, here && id == ClassicKit.Lucy ? 1f : 0f, dt / 3f);
            stair = Mathf.MoveTowards(stair, here && id == ClassicKit.Stairway ? 1f : 0f, dt / 3f);
            oddity = Mathf.MoveTowards(oddity, here && id == ClassicKit.SpaceOddity ? 1f : 0f, dt / 2f);
            purple = Mathf.MoveTowards(purple, here && id == ClassicKit.PurpleHaze ? 1f : 0f, dt / 4f);
            bohemian = Mathf.MoveTowards(bohemian, here && id == ClassicKit.Bohemian ? 1f : 0f, dt / 3f);

            // the sky: clear blue, violet for Lucy, purple for the haze, storm-dark for Bohemian Rhapsody
            var tint = new Color(1f, .95f, .85f); var amount = .1f;
            if (lucy > 0f) { tint = Color.Lerp(tint, new Color(.8f, .6f, 1f), lucy); amount = Mathf.Lerp(amount, .45f, lucy); }
            if (purple > 0f) { tint = Color.Lerp(tint, new Color(.6f, .3f, 1f), purple); amount = Mathf.Lerp(amount, .6f, purple); }
            if (bohemian > 0f) { tint = Color.Lerp(tint, new Color(.25f, .28f, .4f), bohemian); amount = Mathf.Lerp(amount, .65f, bohemian); }
            flash = Mathf.MoveTowards(flash, 0f, dt * 5f);
            if (flash > 0f) { tint = Color.Lerp(tint, Color.white, flash); amount = Mathf.Max(amount, flash * .8f); }
            SkyDirector.SetSongTint(Land, tint, amount);
            SkyDirector.SetSongRain(Land, bohemian * .4f);
            if (here)
            {
                var w = clouds != null ? clouds.Wind : new Vector2(1, 0);
                Shader.SetGlobalFloat("_EW_Wind", .3f + .3f * energy + bohemian * .4f);
                Shader.SetGlobalVector("_EW_WindDir", new Vector4(w.x, 0, w.y, 0));
            }

            // Bohemian Rhapsody: lightning
            nextBolt -= dt;
            if (bohemian > .5f && nextBolt <= 0f) { nextBolt = Random.Range(2.5f, 7f); flash = 1f; }
            if (lightning != null) lightning.intensity = here ? flash * 6f : 0f;

            // birds wheeling over the village
            for (var i = 0; i < birds.Count; i++)
            {
                birds[i].gameObject.SetActive(here);
                if (!here) continue;
                var a = t * (.12f + (i % 3) * .015f) + i * .45f;
                var r = 40f + (i % 4) * 6f;
                var p = center + new Vector3(Mathf.Cos(a) * r, 38f + Mathf.Sin(t * .5f + i) * 3f, Mathf.Sin(a) * r);
                birds[i].SetPositionAndRotation(p, Quaternion.LookRotation(new Vector3(-Mathf.Sin(a), 0, Mathf.Cos(a))) * Quaternion.Euler(0, 0, Mathf.Sin(t * 8f + i) * 25f));
            }

            // the airship and submarine riding gently
            if (airship != null) airship.SetPositionAndRotation(shipHome + Vector3.up * Mathf.Sin(t * .3f) * .6f, shipRot * Quaternion.Euler(Mathf.Sin(t * .25f) * 1.5f, Mathf.Sin(t * .1f) * 6f, 0));
            if (submarine != null) submarine.SetPositionAndRotation(subHome + Vector3.up * Mathf.Sin(t * .8f) * .15f, subRot * Quaternion.Euler(0, Mathf.Sin(t * .05f) * 25f, Mathf.Sin(t * .7f) * 2f));

            // a hot-air balloon drifting across now and then
            var bt = (float)(now - balloonAt);
            var bon = here && balloonAt > 0 && bt > 0f && bt < 300f;
            balloon.gameObject.SetActive(bon);
            if (bon)
            {
                var k = SharedChance.Key("classic-balloon"); var dir = (float)SharedChance.Value(k, songKey, 2) * 360f;
                var heading = Quaternion.Euler(0, dir, 0) * Vector3.forward;
                balloon.position = center - heading * 450f + heading * bt * 3f + Vector3.up * (70f + Mathf.Sin(bt * .05f) * 10f) + Vector3.Cross(Vector3.up, heading) * 60f;
            }

            // Lucy: diamonds sparkling in the sky over the pond and the village
            nextSparkle -= dt;
            if (here && lucy > .2f && nextSparkle <= 0f)
            {
                nextSparkle = .05f;
                foreach (var c in new[] { pond, center })
                {
                    if (c == Vector3.zero) continue;
                    sparkle.transform.position = c + new Vector3(Random.Range(-40f, 40f), Random.Range(8f, 45f), Random.Range(-40f, 40f));
                    sparkle.Emit(2);
                }
            }

            // Purple Haze: mist rolling round the land
            nextHaze -= dt;
            if (here && purple > .2f && nextHaze <= 0f)
            {
                nextHaze = .15f;
                var cam = Camera.main != null ? Camera.main.transform.position : center;
                haze.transform.position = new Vector3(cam.x + Random.Range(-80f, 80f), cam.y + Random.Range(-2f, 8f), cam.z + Random.Range(-80f, 80f));
                haze.Emit(1);
            }

            // Stairway to Heaven: the stones light one by one up to the door as the song climbs
            for (var i = 0; i < stairSteps.Length; i++)
            {
                var lit = stair > 0f && progress * 1.15f > i / (float)stairSteps.Length ? stair : 0f;
                stairSteps[i].SetColor("_EmissionColor", new Color(1f, .9f, .65f) * (.02f + lit * (1.6f + .5f * Mathf.Sin(t * 3f + i * .4f))));
            }
            if (heavenDoor != null) heavenDoor.SetColor("_EmissionColor", new Color(1f, .95f, .8f) * (1.3f + stair * (2f + energy * 2f)));

            // Space Oddity: countdown, lift-off, gone by the end
            if (rocket != null)
            {
                var climb = 0f;
                if (oddity > 0f && id == ClassicKit.SpaceOddity) { var f = Mathf.Clamp01((progress - .12f) / .8f); climb = f * f * 700f; }
                rocket.position = rocketPad + Vector3.up * climb + (climb > 0f ? Vector3.right * Mathf.Sin(t * 20f) * .03f : Vector3.zero);
                if (here && climb > 0f && climb < 650f) { exhaust.transform.position = rocket.position; exhaust.Emit(Mathf.CeilToInt(60f * dt * 10f)); }
                else if (here && oddity > 0f && progress < .12f && progress > .06f) { exhaust.transform.position = rocketPad; exhaust.Emit(1); }
            }

            // gold dust round you; the Hall's flame and the light through its dome breathe with the music
            nextMote -= dt;
            if (here && nextMote <= 0f)
            {
                nextMote = .06f;
                var cp = Camera.main != null ? Camera.main.transform.position : center;
                for (var k = 0; k < 3; k++) { motes.transform.position = cp + new Vector3(Random.Range(-25f, 25f), Random.Range(-1f, 6f), Random.Range(-25f, 25f)); motes.Emit(1); }
            }
            nextFlame -= dt;
            if (here && nextFlame <= 0f && hallFlame != Vector3.zero) { nextFlame = .07f; fire.transform.position = hallFlame; fire.Emit(3); foreach (var cd in candles) { if (Random.value < .1f) { fire.transform.position = cd; fire.Emit(1); } } }
            if (beamMat != null) beamMat.SetColor("_BaseColor", new Color(1f, 1f, 1f, here ? .55f + .45f * energy + stair * .6f : 0f));
            if (beam != null) beam.rotation = Quaternion.Euler(0, t * 3f, 0);

            // always: Hendrix's guitar burns, the Doors' cloud rains
            nextFire -= dt;
            if (here && nextFire <= 0f && guitarFire != Vector3.zero) { nextFire = .08f; fire.transform.position = guitarFire; fire.Emit(purple > .2f ? 8 : 2); }
            nextRain -= dt;
            if (here && nextRain <= 0f && rainCloud != Vector3.zero) { nextRain = .05f; rain.transform.position = rainCloud; rain.Emit(4); }

            if (jukeLight != null)
            {
                var cam = Camera.main != null ? Camera.main.transform.position : center; var best = jukeboxes[0]; var bd = float.MaxValue;
                foreach (var j in jukeboxes) { var dd = (j - cam).sqrMagnitude; if (dd < bd) { bd = dd; best = j; } }
                jukeLight.transform.position = best + Vector3.up * .5f;
                jukeLight.intensity = here ? 1f + energy * 2f * (.6f + .4f * Mathf.Sin(t * 6f)) : 0f;
            }
        }
    }
}
