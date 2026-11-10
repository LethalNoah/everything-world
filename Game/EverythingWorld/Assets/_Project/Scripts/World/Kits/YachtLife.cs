using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Riviera at its endless sunset, answering the music:
    /// - the boats rock gently at their moorings; the tiny dancer turns; gulls wheel over the bay; the lighthouse
    ///   lamp pulses; now and then (shared clock) a sailboat glides across the bay;
    /// - "Rocket Man": Elton's rocket lifts off and climbs out of sight; "Africa": rain falls over the bay and the
    ///   drums glow; "Hotel California": the light deepens to a dusky red and the hotel's windows blaze;
    ///   "Margaritaville": the tiki lights blaze and the bay glows tropical teal; "Danger Zone": a jet screams low
    ///   over the bay, round and round.
    public partial class YachtLife : MonoBehaviour
    {
        const string Land = "The Yacht Rock Riviera";
        public Vector3 center, bay, club, lighthouse, rocketPad; public float bayR = 90f;
        public Transform[] boats = new Transform[0]; public Transform rocket, dancer;
        public Material hotelWindows, tiki, drums;

        CloudLayer clouds; ParticleSystem exhaust;
        readonly List<Transform> gulls = new List<Transform>(); Transform jet, glider;
        Vector3[] boatHome = new Vector3[0]; Quaternion[] boatRot = new Quaternion[0];
        float rocketOn, africa, hotel, marg, danger;
        long songKey = long.MinValue; double songStart, songLength = 1, glideAt = -1;

        void Start()
        {
            clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 55; clouds.span = 2400f; clouds.key = "yacht";
            clouds.minHeight = 110f; clouds.maxHeight = 170f; clouds.size = 1.6f; clouds.speed = 2.5f; clouds.cover = .4f;
            clouds.color = new Color(1f, .62f, .55f);
            exhaust = Particles("Rocket exhaust", 800, new Color(1f, .8f, .5f, .8f), 1f, 3f);
            var em = exhaust.main; em.startLifetime = new ParticleSystem.MinMaxCurve(.6f, 1.4f); em.startSpeed = new ParticleSystem.MinMaxCurve(4f, 8f);
            var es = exhaust.shape; es.shapeType = ParticleSystemShapeType.Cone; es.angle = 20f; es.radius = .5f; es.rotation = new Vector3(90f, 0, 0);
            boatHome = new Vector3[boats.Length]; boatRot = new Quaternion[boats.Length];
            for (var i = 0; i < boats.Length; i++) { boatHome[i] = boats[i].position; boatRot[i] = boats[i].rotation; }
            var gull = WorldMaterials.Solid(new Color(.95f, .95f, .95f));
            for (var i = 0; i < 8; i++)
            {
                var mk = new MeshKit();
                foreach (var s in new[] { -1f, 1f }) { mk.Tri(gull, new Vector3(0, 0, .25f), new Vector3(s * .8f, .2f, -.05f), new Vector3(0, 0, -.25f), Vector3.up); mk.Tri(gull, new Vector3(0, 0, .25f), new Vector3(0, 0, -.25f), new Vector3(s * .8f, .2f, -.05f), Vector3.down); }
                gulls.Add(mk.BuildSingle("Gull", transform, false).transform);
            }
            var jk = new MeshKit(); var grey = WorldMaterials.Solid(new Color(.55f, .58f, .62f), .7f);
            jk.Prism(grey, new Vector3(0, 0, -4f), Vector3.forward, .7f, 7f, 10, .4f);
            jk.Tri(grey, new Vector3(-4f, 0, -1f), new Vector3(0, 0, 1.5f), new Vector3(0, 0, -2.5f), Vector3.up); jk.Tri(grey, new Vector3(4f, 0, -1f), new Vector3(0, 0, -2.5f), new Vector3(0, 0, 1.5f), Vector3.up);
            jk.Tri(grey, new Vector3(-4f, 0, -1f), new Vector3(0, 0, -2.5f), new Vector3(0, 0, 1.5f), Vector3.down); jk.Tri(grey, new Vector3(4f, 0, -1f), new Vector3(0, 0, 1.5f), new Vector3(0, 0, -2.5f), Vector3.down);
            jk.Tri(grey, new Vector3(0, 0, -3.5f), new Vector3(0, 2f, -4f), new Vector3(0, 0, -2.5f), Vector3.right); jk.Tri(grey, new Vector3(0, 0, -3.5f), new Vector3(0, 0, -2.5f), new Vector3(0, 2f, -4f), Vector3.left);
            jk.Prism(WorldMaterials.Glow(new Color(1f, .6f, .25f), 2.6f), new Vector3(0, 0, -4.1f), Vector3.back, .45f, .2f, 10);
            jet = jk.BuildSingle("Jet", transform, false).transform; jet.gameObject.SetActive(false);
            var gk = new MeshKit(); var hull = WorldMaterials.Solid(new Color(.95f, .95f, .93f), .7f);
            gk.Box(hull, new Vector3(0, .5f, 0), new Vector3(2.2f, 1f, 7f), Quaternion.identity); gk.Box(new Material(WorldMaterials.Solid(new Color(.95f, .9f, .85f))), new Vector3(0, 5f, .3f), new Vector3(.06f, 7f, 4f), Quaternion.identity);
            gk.Box(WorldMaterials.Solid(new Color(.8f, .82f, .85f), .9f), new Vector3(0, 5.5f, 1.8f), new Vector3(.12f, 10f, .12f), Quaternion.identity);
            glider = gk.BuildSingle("Gliding sailboat", transform, false).transform; glider.gameObject.SetActive(false);
            SunsetStart();
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

        ParticleSystem Particles(string name, int max, Color col, float s0, float s1)
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
            var r = go.GetComponent<ParticleSystemRenderer>(); var m = Additive(name); m.SetTexture("_BaseMap", tex); r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return ps;
        }

        void Update()
        {
            var dt = Time.deltaTime; var t = Time.time; var now = SharedChance.Now;
            var here = SkyDirector.Instance != null && SkyDirector.Instance.CurrentLand == Land;
            var d = MusicDirector.Instance; var playing = d != null && d.CurrentSlot.IsValid;
            var energy = SurpriseDirector.Energy;
            var id = MusicDirector.ForcedTrackId ?? (playing ? d.CurrentSlot.track.id : null);
            if (SharedChance.CurrentSong(out var song, out var start, out var length) && song != songKey)
            {
                songKey = song; songStart = start; songLength = System.Math.Max(1, length);
                var k = SharedChance.Key("yacht-glide"); glideAt = SharedChance.Roll(k, song, .5) ? start + length * .5 * SharedChance.Value(k, song, 1) : -1;
            }
            var progress = Mathf.Clamp01((float)((now - songStart) / songLength));
            if (MusicDirector.ForcedTrackId != null) progress = Mathf.Repeat(t / 30f, 1f);

            rocketOn = Mathf.MoveTowards(rocketOn, here && id == YachtKit.RocketMan ? 1f : 0f, dt / 2f);
            africa = Mathf.MoveTowards(africa, here && id == YachtKit.Africa ? 1f : 0f, dt / 3f);
            hotel = Mathf.MoveTowards(hotel, here && id == YachtKit.HotelCalifornia ? 1f : 0f, dt / 3f);
            marg = Mathf.MoveTowards(marg, here && id == YachtKit.Margaritaville ? 1f : 0f, dt / 3f);
            danger = Mathf.MoveTowards(danger, here && id == YachtKit.DangerZone ? 1f : 0f, dt / 1.5f);

            var tint = new Color(1f, .75f, .6f); var amount = .15f;
            if (hotel > 0f) { tint = Color.Lerp(tint, new Color(.75f, .3f, .35f), hotel); amount = Mathf.Lerp(amount, .55f, hotel); }
            if (marg > 0f) { tint = Color.Lerp(tint, new Color(.4f, 1f, .85f), marg * .6f); amount = Mathf.Max(amount, .35f * marg); }
            if (africa > 0f) { tint = Color.Lerp(tint, new Color(.8f, .7f, .55f), africa * .5f); amount = Mathf.Max(amount, .3f * africa); }
            SkyDirector.SetSongTint(Land, tint, amount);
            SkyDirector.SetSongRain(Land, africa * .6f);
            if (here) { var w = clouds != null ? clouds.Wind : new Vector2(1, 0); Shader.SetGlobalFloat("_EW_Wind", .25f + .2f * energy); Shader.SetGlobalVector("_EW_WindDir", new Vector4(w.x, 0, w.y, 0)); }

            for (var i = 0; i < boats.Length; i++) { if (boats[i] == null) continue; boats[i].SetPositionAndRotation(boatHome[i] + Vector3.up * Mathf.Sin(t * .7f + i) * .12f, boatRot[i] * Quaternion.Euler(Mathf.Sin(t * .5f + i) * 1.5f, 0, Mathf.Sin(t * .6f + i * 1.3f) * 3f)); }
            if (dancer != null) dancer.Rotate(0, (60f + Amount(YachtKit.TinyDancer) * 120f) * dt, 0, Space.World);
            for (var i = 0; i < gulls.Count; i++)
            {
                gulls[i].gameObject.SetActive(here);
                if (!here) continue;
                var a = t * (.15f + (i % 3) * .02f) + i * .8f; var r = bayR * (.4f + (i % 4) * .12f);
                gulls[i].SetPositionAndRotation(bay + new Vector3(Mathf.Cos(a) * r, 18f + Mathf.Sin(t * .4f + i) * 3f, Mathf.Sin(a) * r), Quaternion.LookRotation(new Vector3(-Mathf.Sin(a), 0, Mathf.Cos(a))) * Quaternion.Euler(0, 0, Mathf.Sin(t * 6f + i) * 20f));
            }
            if (hotelWindows != null) hotelWindows.SetColor("_EmissionColor", new Color(1f, .7f, .4f) * (1.4f + hotel * 3f));
            if (tiki != null) tiki.SetColor("_EmissionColor", new Color(.3f, 1f, .8f) * (1.4f + marg * (2.5f + Mathf.Sin(t * 4f))));
            if (drums != null) drums.SetColor("_EmissionColor", new Color(1f, .6f, .3f) * (.5f + africa * (2.5f + 1.5f * Mathf.Sin(t * 8f))));

            if (rocket != null)
            {
                var climb = 0f;
                if (rocketOn > 0f && id == YachtKit.RocketMan) { var f = Mathf.Clamp01((progress - .1f) / .8f); climb = f * f * 600f; }
                rocket.position = rocketPad + Vector3.up * climb;
                if (here && climb > 0f && climb < 550f) { exhaust.transform.position = rocket.position; exhaust.Emit(Mathf.CeilToInt(30f * dt * 10f)); }
            }
            var jetOn = danger > .05f; jet.gameObject.SetActive(jetOn);
            if (jetOn)
            {
                var a = t * .6f; var r = bayR * .9f;
                var p = bay + new Vector3(Mathf.Cos(a) * r, 22f + Mathf.Sin(a * 2f) * 4f, Mathf.Sin(a) * r);
                jet.SetPositionAndRotation(p, Quaternion.LookRotation(new Vector3(-Mathf.Sin(a), Mathf.Cos(a * 2f) * .1f, Mathf.Cos(a))) * Quaternion.Euler(0, 0, -40f));
            }
            var gt = (float)(now - glideAt); var gon = here && glideAt > 0 && gt > 0f && gt < 120f;
            glider.gameObject.SetActive(gon);
            SunsetUpdate(dt, t, id, here, energy);
            if (gon) { var x = Mathf.Lerp(-bayR * .8f, bayR * .8f, gt / 120f); glider.SetPositionAndRotation(bay + new Vector3(x, -.05f + Mathf.Sin(t) * .08f, bayR * .25f), Quaternion.LookRotation(Vector3.right) * Quaternion.Euler(0, 0, 6f)); }
        }
    }
}
