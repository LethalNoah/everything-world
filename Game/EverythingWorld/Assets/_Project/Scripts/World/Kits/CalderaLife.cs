using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Metalcore Caldera, building and breaking like the music:
    /// - ash drifts down all the time; steam breathes from the vents; low violet-grey cloud; a crowd runs the circle pit
    ///   round the arena stage, faster with the music.
    /// - During about half the songs (shared clock, so friends see the same), at the breakdown: for a few seconds the
    ///   cracks throb faster and faster and the lava brightens, then the volcanoes erupt, throwing neon sparks and violet
    ///   smoke from every crater under a magenta sky, the lava streams blazing; afterwards everything glows quietly and the
    ///   ash falls thicker. "My Curse", "To Hell And Back" and "God Complex" always erupt.
    /// - "BLACKOUT": the neon blacks out in stutters. "Happiness by the Kilowatt": every neon post blazes. "Spiritual
    ///   Eclipse": a black disc with a violet corona rises over the basin. "BIRDS": a flock wheels over Turnstile's wire.
    ///   "Meet Me At The Gates" and "Afterlife": pillars of light rise from the gates and the door. Moths circle the
    ///   flame; the lure bobs over the black pool; rocks float round Holding Absence. Every song's own light brightens.
    public class CalderaLife : MonoBehaviour
    {
        const string Land = "Metalcore Caldera";
        public Vector3 center, arena, stageAt, curse, mothFlame, gates, lureAt, bulb, afterlife, birdsAt;
        public Material lavaCyan, lavaViolet, cracks, neon, blackoutSign;
        public List<Vector3> posts = new List<Vector3>(), vents = new List<Vector3>(), craters = new List<Vector3>(), fires = new List<Vector3>(), hazards = new List<Vector3>(), floatAt = new List<Vector3>();
        public List<Vector3[]> streams = new List<Vector3[]>();
        public List<Transform> crowd = new List<Transform>(), moths = new List<Transform>(), floaters = new List<Transform>(); public List<float> crowdR = new List<float>();
        public Transform lure;
        public List<(string song, Material m, Color c)> songGlows = new List<(string, Material, Color)>();
        readonly Dictionary<string, float> songAmount = new Dictionary<string, float>();

        CloudLayer clouds; ParticleSystem ash, steam, sparksC, sparksM, smoke, flame, beam;
        Transform eclipse; Material corona; readonly List<Transform> birds = new List<Transform>();
        float build, erupt, after, flash, blackout, kilowatt, eclipseAmt, birdsAmt, gatesAmt, afterAmt;
        long eruptSong = long.MinValue; double breakAt = -1;
        float nextAsh, nextSteam, nextSpark, nextSmoke, nextFlame, nextBeam;
        static readonly HashSet<string> AlwaysErupt = new HashSet<string>();

        void Start()
        {
            foreach (var a in new[] { "Killswitch Engage", "blessthefall", "Like Moths To Flames" })
                if (CalderaSongs.Ids(a, out var l)) foreach (var (t, id) in l) if (t.StartsWith("My Curse") || t.StartsWith("To Hell And Back") || t.StartsWith("God Complex")) AlwaysErupt.Add(id);
            clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 70; clouds.span = 2600f; clouds.key = "caldera";
            clouds.minHeight = 150f; clouds.maxHeight = 210f; clouds.size = 2.6f; clouds.speed = 4f; clouds.cover = .75f;
            clouds.color = new Color(.2f, .18f, .24f);
            ash = Particles("Falling ash", 1500, new Color(.75f, .74f, .78f, .8f), .06f, .14f, false);
            var am = ash.main; am.startLifetime = new ParticleSystem.MinMaxCurve(8f, 12f); am.startSpeed = new ParticleSystem.MinMaxCurve(0f, .2f); am.gravityModifier = .02f;
            steam = Particles("Vent steam", 400, new Color(.7f, .68f, .75f, .3f), 1.5f, 3f, false);
            var sm = steam.main; sm.startLifetime = new ParticleSystem.MinMaxCurve(3f, 5f); sm.startSpeed = new ParticleSystem.MinMaxCurve(1f, 2f);
            sparksC = Particles("Eruption sparks cyan", 2500, new Color(.3f, .95f, 1f, 1f), .9f, 1.8f, true);
            sparksM = Particles("Eruption sparks magenta", 2500, new Color(1f, .3f, .8f, 1f), .9f, 1.8f, true);
            foreach (var s in new[] { sparksC, sparksM })
            {
                var m = s.main; m.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4.5f); m.startSpeed = new ParticleSystem.MinMaxCurve(18f, 34f); m.gravityModifier = .9f;
                var sh = s.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 22f; sh.radius = 4f; sh.rotation = new Vector3(-90f, 0, 0);
            }
            smoke = Particles("Eruption smoke", 500, new Color(.35f, .22f, .45f, .55f), 12f, 26f, false);
            var smm = smoke.main; smm.startLifetime = new ParticleSystem.MinMaxCurve(8f, 12f); smm.startSpeed = new ParticleSystem.MinMaxCurve(8f, 14f);
            var smsh = smoke.shape; smsh.shapeType = ParticleSystemShapeType.Cone; smsh.angle = 12f; smsh.radius = 4f; smsh.rotation = new Vector3(-90f, 0, 0);
            flame = Particles("Fire", 500, new Color(1f, .4f, .75f, .9f), .25f, .55f, true);
            var fm = flame.main; fm.startLifetime = new ParticleSystem.MinMaxCurve(.35f, .8f); fm.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.4f);
            beam = Particles("Pillar of light", 600, new Color(.9f, .95f, 1f, .8f), .3f, .8f, true);
            var bm = beam.main; bm.startLifetime = new ParticleSystem.MinMaxCurve(3f, 4f); bm.startSpeed = new ParticleSystem.MinMaxCurve(10f, 16f);
            var bs = beam.shape; bs.shapeType = ParticleSystemShapeType.Cone; bs.angle = 1.5f; bs.radius = .8f; bs.rotation = new Vector3(-90f, 0, 0);
            // the eclipse, hidden until its song
            var ek = new MeshKit(); corona = new Material(WorldMaterials.Glow(CalderaKit.Violet, 2.5f)) { name = "Eclipse corona" };
            ek.Prism(WorldMaterials.Solid(new Color(.02f, .02f, .03f)), Vector3.zero, Vector3.forward, 20f, .5f, 40);
            for (var k = 0; k < 48; k++) { var a0 = k * Mathf.PI / 24f; var a1 = (k + 1) * Mathf.PI / 24f; var p0 = new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0) * 21.5f; var p1 = new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0) * 21.5f; ek.Box(corona, (p0 + p1) / 2f + Vector3.forward * .6f, new Vector3(3f, 3f, Vector3.Distance(p0, p1) + .2f), Quaternion.LookRotation(p1 - p0, Vector3.forward)); }
            var eg = ek.BuildSingle("Spiritual eclipse", transform, false); if (eg != null) { eclipse = eg.transform; eclipse.gameObject.SetActive(false); }
            var birdM = WorldMaterials.Solid(new Color(.03f, .03f, .04f));
            for (var k = 0; k < 14; k++) { var bk = new MeshKit(); bk.Box(birdM, Vector3.zero, new Vector3(.2f, .15f, .5f), Quaternion.identity); foreach (var s in new[] { -1f, 1f }) bk.Box(birdM, new Vector3(s * .45f, .04f, 0), new Vector3(.8f, .03f, .3f), Quaternion.Euler(0, 0, s * 14f)); var b = bk.BuildSingle("Bird", transform, false); if (b != null) { b.SetActive(false); birds.Add(b.transform); } }
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
            var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, .15f), new GradientAlphaKey(0f, 1f) });
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

        bool Playing(string id, string artist, string title)
        {
            if (id == null || !CalderaSongs.Ids(artist, out var l)) return false;
            foreach (var (t, i) in l) if (i == id && t.StartsWith(title)) return true;
            return false;
        }

        void Update()
        {
            var dt = Time.deltaTime; var t = Time.time;
            var here = SkyDirector.Instance != null && SkyDirector.Instance.CurrentLand == Land;
            var d = MusicDirector.Instance; var playing = d != null && d.CurrentSlot.IsValid;
            var energy = SurpriseDirector.Energy;
            var id = MusicDirector.ForcedTrackId ?? (playing ? d.CurrentSlot.track.id : null);

            // the breakdown: when it comes, how close, whether it's erupting
            var forced = here && id != null && AlwaysErupt.Contains(id);
            if (SharedChance.CurrentSong(out var song, out var start, out var length) && song != eruptSong)
            {
                eruptSong = song;
                breakAt = SharedChance.Roll(SharedChance.Key("caldera-erupt"), song, .5) || (id != null && AlwaysErupt.Contains(id)) ? start + length * (.35 + .3 * SharedChance.Value(SharedChance.Key("caldera-when"), song)) : -1;
            }
            var now = SharedChance.Now; var since = breakAt > 0 ? (float)(now - breakAt) : -999f;
            var wantBuild = here && ((since > -12f && since < 0f) || (forced && MusicDirector.ForcedTrackId != null && Mathf.Repeat(t, 40f) < 8f));
            var wantErupt = here && ((since >= 0f && since < 18f) || (forced && MusicDirector.ForcedTrackId != null && Mathf.Repeat(t, 40f) >= 8f));
            build = Mathf.MoveTowards(build, wantBuild ? 1f : 0f, dt / (wantBuild ? 10f : 1f));
            var wasErupting = erupt > .5f;
            erupt = Mathf.MoveTowards(erupt, wantErupt ? 1f : 0f, dt / (wantErupt ? .6f : 4f));
            if (!wasErupting && erupt > .5f) flash = 1f;
            after = wantErupt ? 1f : Mathf.MoveTowards(after, 0f, dt / 35f);
            flash = Mathf.MoveTowards(flash, 0f, dt * 2f);

            blackout = Mathf.MoveTowards(blackout, here && Playing(id, "Turnstile", "BLACKOUT") ? 1f : 0f, dt / 1.5f);
            kilowatt = Mathf.MoveTowards(kilowatt, here && Playing(id, "Alexisonfire", "Happiness") ? 1f : 0f, dt / 2f);
            eclipseAmt = Mathf.MoveTowards(eclipseAmt, here && Playing(id, "Like Moths To Flames", "Spiritual Eclipse") ? 1f : 0f, dt / 4f);
            birdsAmt = Mathf.MoveTowards(birdsAmt, here && Playing(id, "Turnstile", "BIRDS") ? 1f : 0f, dt / 2f);
            gatesAmt = Mathf.MoveTowards(gatesAmt, here && Playing(id, "blessthefall", "Meet Me At The Gates") ? 1f : 0f, dt / 3f);
            afterAmt = Mathf.MoveTowards(afterAmt, here && Playing(id, "Holding Absence", "Afterlife") ? 1f : 0f, dt / 3f);

            foreach (var (s, _, _) in songGlows) if (!songAmount.ContainsKey(s)) songAmount[s] = 0f;
            var keys = new List<string>(songAmount.Keys);
            foreach (var s in keys) songAmount[s] = Mathf.MoveTowards(songAmount[s], here && id == s ? 1f : 0f, dt / 2f);
            foreach (var (s, m, c) in songGlows) if (m != null) m.SetColor("_EmissionColor", c * (1f + songAmount[s] * (2.5f + Mathf.Sin(t * 2.2f) * .6f)));

            // the lava, the cracks, the neon: throbbing faster through the build, blazing in the eruption, then glowing quietly
            var stutter = blackout > .1f ? (Mathf.Repeat(t * 1.3f, 1f) < .55f ? 1f - blackout : 1f) : 1f;
            var pulseRate = 1.2f + build * 9f + erupt * 4f; var pulse = .5f + .5f * Mathf.Sin(t * pulseRate);
            var lava = (1f + .25f * Mathf.Sin(t * .9f) + energy * .5f + build * (.6f + pulse * 1.2f) + erupt * 2.2f + after * .9f) * stutter;
            if (lavaCyan != null) lavaCyan.SetColor("_EmissionColor", CalderaKit.Cyan * .9f * lava);
            if (lavaViolet != null) lavaViolet.SetColor("_EmissionColor", CalderaKit.Violet * 1.05f * lava);
            if (cracks != null) cracks.SetColor("_EmissionColor", CalderaKit.Magenta * (.8f + pulse * (.4f + build * 2f) + erupt * 2f) * stutter);
            if (neon != null) neon.SetColor("_EmissionColor", CalderaKit.Cyan * (1.4f + energy * .4f + kilowatt * 3f + erupt * 1.2f) * stutter);
            if (blackoutSign != null && blackout > .1f) blackoutSign.SetColor("_EmissionColor", CalderaKit.Cyan * (Mathf.Repeat(t * 2.3f, 1f) < .4f ? 0f : 3f));

            // the sky
            var tint = new Color(.6f, .45f, 1f); var amount = .12f;
            if (build > 0f) amount = Mathf.Max(amount, .12f + .15f * build * pulse);
            if (erupt > 0f) { tint = Color.Lerp(tint, CalderaKit.Magenta, erupt); amount = Mathf.Lerp(amount, .5f, erupt); }
            if (eclipseAmt > 0f) { tint = Color.Lerp(tint, new Color(.25f, .15f, .45f), eclipseAmt); amount = Mathf.Max(amount, .45f * eclipseAmt); }
            if (flash > 0f) { tint = Color.Lerp(tint, new Color(1f, .8f, 1f), flash); amount = Mathf.Max(amount, .7f * flash); }
            SkyDirector.SetSongTint(Land, tint, amount);
            if (here) { var w = clouds != null ? clouds.Wind : new Vector2(1, 0); Shader.SetGlobalFloat("_EW_Wind", .35f + .3f * energy + erupt * .6f); Shader.SetGlobalVector("_EW_WindDir", new Vector4(w.x, 0, w.y, 0)); }

            // the circle pit
            var spin = .45f + energy * .5f + erupt * .8f;
            for (var k = 0; k < crowd.Count; k++)
            {
                var a = (float)(SharedChance.Now * spin * (12f / Mathf.Max(6f, crowdR[k]))) + k * Mathf.PI * 2f / crowd.Count;
                var r = crowdR[k] + Mathf.Sin(t * 1.3f + k) * .6f;
                var p = arena + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * r + Vector3.up * Mathf.Abs(Mathf.Sin(t * 6f + k * 1.7f)) * .25f * (1f + erupt);
                crowd[k].SetPositionAndRotation(p, Quaternion.LookRotation(new Vector3(-Mathf.Sin(a), 0, Mathf.Cos(a))));
            }
            // moths round the flame, the lure, the floating rocks
            for (var k = 0; k < moths.Count; k++) { var a = t * (1.6f + k * .2f) + k * 1.1f; moths[k].SetPositionAndRotation(mothFlame + new Vector3(Mathf.Cos(a) * (1.2f + k * .25f), Mathf.Sin(a * 1.7f) * .6f + k * .15f, Mathf.Sin(a) * (1.2f + k * .25f)), Quaternion.LookRotation(new Vector3(-Mathf.Sin(a), 0, Mathf.Cos(a))) * Quaternion.Euler(0, 0, Mathf.Sin(t * 20f + k) * 40f)); }
            if (lure != null) lure.position = lureAt + Vector3.up * Mathf.Sin(t * 1.4f) * .25f;
            for (var k = 0; k < floaters.Count; k++) { var a = t * .25f + k * Mathf.PI / 3f; floaters[k].SetPositionAndRotation(floatAt[k] + new Vector3(Mathf.Cos(a) * 2.6f, Mathf.Sin(t * .8f + k) * .6f + (k % 2) * .8f, Mathf.Sin(a) * 2.6f), Quaternion.Euler(t * 10f + k * 30f, t * 14f, 0)); }

            // the eclipse
            if (eclipse != null)
            {
                var on = eclipseAmt > .01f && here; if (eclipse.gameObject.activeSelf != on) eclipse.gameObject.SetActive(on);
                if (on) { var cam = Camera.main; var fwd = cam != null ? Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized : Vector3.forward; var pos = (cam != null ? cam.transform.position : center) + fwd * 200f + Vector3.up * (40f + 40f * eclipseAmt); eclipse.SetPositionAndRotation(pos, Quaternion.LookRotation(cam != null ? pos - cam.transform.position : Vector3.forward)); eclipse.localScale = Vector3.one * Mathf.SmoothStep(.3f, 1f, eclipseAmt); corona.SetColor("_EmissionColor", CalderaKit.Violet * (2f + Mathf.Sin(t) * .5f)); }
            }
            // birds
            for (var k = 0; k < birds.Count; k++)
            {
                var b = birds[k]; var on = here && birdsAmt > .05f; if (b.gameObject.activeSelf != on) b.gameObject.SetActive(on); if (!on) continue;
                var a = t * (.5f + (k % 4) * .06f) + k * .45f; var r = 10f + (k % 3) * 4f;
                b.SetPositionAndRotation(birdsAt + new Vector3(Mathf.Cos(a) * r, (k % 5) * 1.2f + Mathf.Sin(a * 2f), Mathf.Sin(a) * r), Quaternion.LookRotation(new Vector3(-Mathf.Sin(a), 0, Mathf.Cos(a))) * Quaternion.Euler(0, 0, Mathf.Sin(t * 9f + k) * 25f));
            }

            if (!here) return;
            var camT = Camera.main != null ? Camera.main.transform.position : center;
            nextAsh -= dt;
            if (nextAsh <= 0f) { nextAsh = .02f / (1f + after * 2f + erupt * 2f); ash.transform.position = camT + new Vector3(Random.Range(-40f, 40f), Random.Range(8f, 25f), Random.Range(-40f, 40f)); ash.Emit(1); }
            nextSteam -= dt;
            if (vents.Count > 0 && nextSteam <= 0f) { nextSteam = .2f; foreach (var v in vents) if (Random.value < .3f + build * .5f) { steam.transform.position = v; steam.Emit(1); } }
            nextSpark -= dt;
            if (erupt > .05f && craters.Count > 0 && nextSpark <= 0f)
            {
                nextSpark = .02f;
                foreach (var c in craters) { sparksC.transform.SetPositionAndRotation(c, Quaternion.identity); sparksC.Emit(Mathf.CeilToInt(3 * erupt)); sparksM.transform.SetPositionAndRotation(c, Quaternion.identity); sparksM.Emit(Mathf.CeilToInt(3 * erupt)); }
            }
            nextSmoke -= dt;
            if (erupt > .05f && craters.Count > 0 && nextSmoke <= 0f) { nextSmoke = .3f; foreach (var c in craters) { smoke.transform.SetPositionAndRotation(c, Quaternion.identity); smoke.Emit(1); } }
            nextFlame -= dt;
            if (nextFlame <= 0f) { nextFlame = .15f; foreach (var f in fires) { flame.transform.position = f; flame.Emit(1); } }
            nextBeam -= dt;
            if (nextBeam <= 0f && (gatesAmt > .05f || afterAmt > .05f))
            {
                nextBeam = .03f;
                if (gatesAmt > .05f && gates != Vector3.zero) { beam.transform.SetPositionAndRotation(gates, Quaternion.identity); beam.Emit(2); }
                if (afterAmt > .05f && afterlife != Vector3.zero) { beam.transform.SetPositionAndRotation(afterlife, Quaternion.identity); beam.Emit(1); }
            }
        }
    }

    /// Looks up the Caldera's songs from outside the kit.
    public static class CalderaSongs
    {
        public static bool Ids(string artist, out (string title, string id)[] list) => CalderaKit.SongTable.TryGetValue(artist, out list);
    }
}
