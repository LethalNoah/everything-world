using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Power Metal Peaks under a blazing sunset, answering the music:
    /// - torches and braziers flicker and throw sparks; DragonForce's flame jets burst with the beat; the kawaii neon
    ///   pulses; the pirate ship rolls at anchor and the giant duck bobs;
    /// - the dragon on the castle breathes fire now and then (about half the songs, at a moment the shared clock picks,
    ///   so friends see it together), and all through "Through the Fire and Flames", when the sky burns red;
    /// - fireworks burst over HANABIE.'s rack in their songs, and sometimes for no reason at all;
    /// - a storm rolls over the sea in some songs, lightning striking out past the cove; "Haten ni raimei" always
    ///   brings it. "My Heart Will Go On" turns the sky cold; "Gimme Chocolate!!" turns it pink; "Drink" lights every
    ///   lantern up. Every song's own light brightens while it plays.
    public class PowerLife : MonoBehaviour
    {
        const string Land = "The Power Metal Peaks";
        public Vector3 center, dragonMouth, dragonDir, shipAt, duckAt, coveAt;
        public Quaternion shipRot = Quaternion.identity;
        public Material fire, embers, neonPink, neonCyan, lanterns, dragonEyes;
        public Transform ship, duck;
        public List<Vector3> torches = new List<Vector3>(), fires = new List<Vector3>(), jets = new List<Vector3>(), flameAt = new List<Vector3>(), neonSigns = new List<Vector3>(), fireworksAt = new List<Vector3>();
        public List<string> fireworksSongs = new List<string>();
        public List<(string song, Material m, Color c)> songGlows = new List<(string, Material, Color)>();
        readonly Dictionary<string, float> songAmount = new Dictionary<string, float>();

        CloudLayer clouds; ParticleSystem flame, sparks, breath, burst, rain;
        LineRenderer bolt; float boltLife;
        Color fireBase, emberBase, pinkBase, cyanBase, lanternBase, eyeBase;
        float burn, cold, choc, drink, storm, breathAmt, flash, nextFlame, nextSpark, nextJet, nextFirework, nextBolt;
        long songSeen = long.MinValue; double breathAt = -1; bool stormSong, fireworkSong;

        void Start()
        {
            clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 80; clouds.span = 2600f; clouds.key = "power";
            clouds.minHeight = 140f; clouds.maxHeight = 220f; clouds.size = 2.4f; clouds.speed = 5f; clouds.cover = .6f;
            clouds.color = new Color(.55f, .3f, .35f);
            Color E(Material m) => m != null ? m.GetColor("_EmissionColor") : Color.black;
            fireBase = E(fire); emberBase = E(embers); pinkBase = E(neonPink); cyanBase = E(neonCyan); lanternBase = E(lanterns); eyeBase = E(dragonEyes);
            flame = Particles("Flames", 1500, new Color(1f, .55f, .15f, .9f), .25f, .6f, true);
            var fm = flame.main; fm.startLifetime = new ParticleSystem.MinMaxCurve(.4f, .9f); fm.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3f);
            Cone(flame, 12f, .2f);
            sparks = Particles("Sparks", 800, new Color(1f, .7f, .3f, 1f), .05f, .1f, true);
            var sm = sparks.main; sm.startLifetime = new ParticleSystem.MinMaxCurve(1f, 2f); sm.startSpeed = new ParticleSystem.MinMaxCurve(2f, 4f); sm.gravityModifier = -.1f;
            Cone(sparks, 25f, .2f);
            breath = Particles("Dragon fire", 3000, new Color(1f, .5f, .12f, 1f), .8f, 2.2f, true);
            var bm = breath.main; bm.startLifetime = new ParticleSystem.MinMaxCurve(1f, 1.6f); bm.startSpeed = new ParticleSystem.MinMaxCurve(16f, 24f); bm.gravityModifier = -.05f;
            var bs = breath.shape; bs.shapeType = ParticleSystemShapeType.Cone; bs.angle = 10f; bs.radius = .3f;
            var sz = breath.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, .4f, 1, 2.5f));
            burst = Particles("Fireworks", 4000, Color.white, .25f, .5f, true);
            var fwm = burst.main; fwm.startLifetime = new ParticleSystem.MinMaxCurve(1.4f, 2.2f); fwm.startSpeed = new ParticleSystem.MinMaxCurve(9f, 14f); fwm.gravityModifier = .35f;
            var fsh = burst.shape; fsh.shapeType = ParticleSystemShapeType.Sphere; fsh.radius = .3f;
            rain = Particles("Storm rain", 3000, new Color(.7f, .75f, .85f, .45f), .05f, .08f, false);
            var rm = rain.main; rm.startLifetime = 1.4f; rm.startSpeed = 0f; rm.gravityModifier = 2.2f;
            var rr = rain.GetComponent<ParticleSystemRenderer>(); rr.renderMode = ParticleSystemRenderMode.Stretch; rr.velocityScale = .05f; rr.lengthScale = 3f;
            var boltGo = new GameObject("Lightning"); boltGo.transform.SetParent(transform, false);
            bolt = boltGo.AddComponent<LineRenderer>(); bolt.positionCount = 12; bolt.widthMultiplier = .8f; bolt.useWorldSpace = true; bolt.enabled = false;
            var lm = Additive("Lightning"); lm.SetColor("_BaseColor", new Color(.85f, .9f, 1f) * 6f); bolt.sharedMaterial = lm;
            bolt.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        static void Cone(ParticleSystem ps, float angle, float radius) { var s = ps.shape; s.shapeType = ParticleSystemShapeType.Cone; s.angle = angle; s.radius = radius; s.rotation = new Vector3(-90f, 0, 0); }

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
            if (id == null || !PowerSongs.Ids(artist, out var l)) return false;
            foreach (var (t, i) in l) if (i == id && (title == null || t.StartsWith(title))) return true;
            return false;
        }

        void Update()
        {
            var dt = Time.deltaTime; var t = Time.time;
            var here = SkyDirector.Instance != null && SkyDirector.Instance.CurrentLand == Land;
            var d = MusicDirector.Instance; var playing = d != null && d.CurrentSlot.IsValid;
            var energy = SurpriseDirector.Energy;
            var id = MusicDirector.ForcedTrackId ?? (playing ? d.CurrentSlot.track.id : null);

            var ttfaf = here && Playing(id, "DragonForce", "Through the Fire");
            if (SharedChance.CurrentSong(out var song, out var start, out var length) && song != songSeen)
            {
                songSeen = song;
                breathAt = SharedChance.Roll(SharedChance.Key("power-breath"), song, .5) ? start + length * (.2 + .6 * SharedChance.Value(SharedChance.Key("power-breath-when"), song)) : -1;
                stormSong = SharedChance.Roll(SharedChance.Key("power-storm"), song, .3);
                fireworkSong = SharedChance.Roll(SharedChance.Key("power-fireworks"), song, .2);
            }
            var since = breathAt > 0 ? (float)(SharedChance.Now - breathAt) : -999f;
            breathAmt = Mathf.MoveTowards(breathAmt, here && (ttfaf || (since >= 0f && since < 7f)) ? 1f : 0f, dt / .5f);
            burn = Mathf.MoveTowards(burn, ttfaf ? 1f : 0f, dt / 3f);
            cold = Mathf.MoveTowards(cold, here && Playing(id, "DragonForce", "My Heart") ? 1f : 0f, dt / 4f);
            choc = Mathf.MoveTowards(choc, here && Playing(id, "BABYMETAL", "Gimme") ? 1f : 0f, dt / 3f);
            drink = Mathf.MoveTowards(drink, here && Playing(id, "Alestorm", "Drink") ? 1f : 0f, dt / 2f);
            var wantStorm = here && (stormSong || Playing(id, "LADYBABY", "Haten"));
            storm = Mathf.MoveTowards(storm, wantStorm ? 1f : 0f, dt / 6f);
            var fireworks = here && (fireworkSong || Playing(id, "HANABIE.", null));
            flash = Mathf.MoveTowards(flash, 0f, dt * 4f);

            foreach (var (s, _, _) in songGlows) if (!songAmount.ContainsKey(s)) songAmount[s] = 0f;
            var keys = new List<string>(songAmount.Keys);
            foreach (var s in keys) songAmount[s] = Mathf.MoveTowards(songAmount[s], here && id == s ? 1f : 0f, dt / 2f);
            foreach (var (s, m, c) in songGlows) if (m != null) m.SetColor("_EmissionColor", c * (1f + songAmount[s] * (2.5f + Mathf.Sin(t * 2.2f) * .6f)));

            var flick = .85f + .15f * Mathf.PerlinNoise(t * 3f, 0f);
            if (fire != null) fire.SetColor("_EmissionColor", fireBase * (flick + energy * .4f + burn * 1.2f));
            if (embers != null) embers.SetColor("_EmissionColor", emberBase * (flick + burn));
            var beat = .5f + .5f * Mathf.Sin(t * (3f + energy * 5f));
            if (neonPink != null) neonPink.SetColor("_EmissionColor", pinkBase * (1f + energy * .6f * beat + choc * 1.5f));
            if (neonCyan != null) neonCyan.SetColor("_EmissionColor", cyanBase * (1f + energy * .6f * (1f - beat)));
            if (lanterns != null) lanterns.SetColor("_EmissionColor", lanternBase * (flick + drink * 2f));
            if (dragonEyes != null) dragonEyes.SetColor("_EmissionColor", eyeBase * (1f + breathAmt * 2.5f + .3f * Mathf.Sin(t * 1.7f)));

            // the sky: a blazing sunset, burning red in "Through the Fire and Flames", cold in "My Heart Will Go On",
            // pink in "Gimme Chocolate!!", dark and flashing in a storm
            var tint = new Color(1f, .5f, .25f); var amount = .14f;
            if (burn > 0f) { tint = Color.Lerp(tint, new Color(1f, .15f, .05f), burn); amount = Mathf.Lerp(amount, .5f, burn); }
            if (cold > 0f) { tint = Color.Lerp(tint, new Color(.45f, .65f, 1f), cold); amount = Mathf.Lerp(amount, .4f, cold); }
            if (choc > 0f) { tint = Color.Lerp(tint, Color.Lerp(new Color(1f, .45f, .75f), new Color(1f, .3f, .55f), beat), choc); amount = Mathf.Lerp(amount, .45f, choc); }
            if (storm > 0f) { tint = Color.Lerp(tint, new Color(.3f, .3f, .45f), storm * .7f); amount = Mathf.Lerp(amount, .4f, storm); }
            if (flash > 0f) { tint = Color.Lerp(tint, new Color(.9f, .92f, 1f), flash); amount = Mathf.Max(amount, .6f * flash); }
            SkyDirector.SetSongTint(Land, tint, amount);
            SkyDirector.SetSongRain(Land, storm * .8f);
            if (here) { var w = clouds != null ? clouds.Wind : new Vector2(1, 0); Shader.SetGlobalFloat("_EW_Wind", .5f + .3f * energy + storm * .6f); Shader.SetGlobalVector("_EW_WindDir", new Vector4(w.x, 0, w.y, 0)); }

            if (ship != null) ship.SetPositionAndRotation(shipAt + Vector3.up * Mathf.Sin(t * .7f) * (.25f + storm * .4f), shipRot * Quaternion.Euler(Mathf.Sin(t * .55f) * (2f + storm * 4f), 0, Mathf.Sin(t * .8f) * (1.5f + storm * 3f)));
            if (duck != null) duck.position = duckAt + Vector3.up * Mathf.Sin(t * 1.1f) * (.35f + storm * .5f);

            if (!here) { if (bolt.enabled) bolt.enabled = false; return; }
            var cam = Camera.main != null ? Camera.main.transform.position : center;
            // flames and sparks from the torches and braziers near you
            nextFlame -= dt;
            if (nextFlame <= 0f)
            {
                nextFlame = .08f;
                foreach (var f in fires) if ((f - cam).sqrMagnitude < 120f * 120f && Random.value < .5f) { flame.transform.SetPositionAndRotation(f, Quaternion.identity); flame.Emit(1); }
                foreach (var f in flameAt) if (Random.value < .6f + burn * .4f) { flame.transform.SetPositionAndRotation(f, Quaternion.identity); flame.Emit(1 + (int)(burn * 2f)); }
            }
            nextSpark -= dt;
            if (nextSpark <= 0f) { nextSpark = .15f; foreach (var f in torches) if ((f - cam).sqrMagnitude < 60f * 60f && Random.value < .15f) { sparks.transform.SetPositionAndRotation(f, Quaternion.identity); sparks.Emit(1); } }
            // DragonForce's flame jets
            nextJet -= dt;
            if (jets.Count > 0 && nextJet <= 0f)
            {
                nextJet = ttfaf ? .12f : Mathf.Lerp(1.2f, .35f, energy);
                var jm = flame.main; var keep = jm.startSpeed;
                jm.startSpeed = new ParticleSystem.MinMaxCurve(8f, 14f);
                foreach (var j in jets) if (ttfaf || Random.value < .2f + energy * .5f) { flame.transform.SetPositionAndRotation(j, Quaternion.identity); flame.Emit(ttfaf ? 6 : 3); }
                jm.startSpeed = keep;
            }
            // the dragon's breath
            if (breathAmt > .05f && dragonMouth != Vector3.zero) { breath.transform.SetPositionAndRotation(dragonMouth, Quaternion.LookRotation(dragonDir)); breath.Emit(Mathf.CeilToInt(6 * breathAmt)); }
            // fireworks
            nextFirework -= dt;
            if (fireworks && fireworksAt.Count > 0 && nextFirework <= 0f)
            {
                nextFirework = Random.Range(.5f, 1.3f);
                var at = fireworksAt[Random.Range(0, fireworksAt.Count)] + new Vector3(Random.Range(-8f, 8f), Random.Range(26f, 44f), Random.Range(-8f, 8f));
                Color[] cols = { new Color(1f, .35f, .7f), new Color(.35f, .85f, 1f), new Color(1f, .85f, .3f), new Color(.6f, 1f, .5f), new Color(1f, .45f, .25f) };
                var bmain = burst.main; bmain.startColor = cols[Random.Range(0, cols.Length)] * 1.8f;
                burst.transform.position = at; burst.Emit(160);
            }
            // the storm: rain round you, lightning out over the sea
            if (storm > .05f) { for (var k = 0; k < Mathf.CeilToInt(8 * storm); k++) { rain.transform.position = cam + new Vector3(Random.Range(-30f, 30f), Random.Range(12f, 20f), Random.Range(-30f, 30f)); rain.Emit(1); } }
            nextBolt -= dt;
            if (storm > .5f && nextBolt <= 0f && coveAt != Vector3.zero)
            {
                nextBolt = Random.Range(2.5f, 7f);
                var ground = coveAt + Quaternion.Euler(0, Random.Range(0f, 360f), 0) * Vector3.forward * Random.Range(80f, 260f); ground.y = 0f;
                var top = ground + Vector3.up * 220f + new Vector3(Random.Range(-30f, 30f), 0, Random.Range(-30f, 30f));
                for (var i = 0; i < bolt.positionCount; i++) { var f = i / (bolt.positionCount - 1f); bolt.SetPosition(i, Vector3.Lerp(top, ground, f) + (i == 0 || i == bolt.positionCount - 1 ? Vector3.zero : new Vector3(Random.Range(-10f, 10f), 0, Random.Range(-10f, 10f)))); }
                bolt.enabled = true; boltLife = .18f; flash = 1f;
            }
            if (bolt.enabled) { boltLife -= dt; if (boltLife <= 0f) bolt.enabled = false; }
        }
    }

    /// Looks up the Power Metal Peaks' songs from outside the kit.
    public static class PowerSongs
    {
        public static bool Ids(string artist, out (string title, string id)[] list) => PowerKit.SongTable.TryGetValue(artist, out list);
    }
}
