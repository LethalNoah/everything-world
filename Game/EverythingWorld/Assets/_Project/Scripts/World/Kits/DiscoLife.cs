using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// Discotopia on a clear night:
    /// - the lighthouse's mirror ball turns and throws specks of light that sweep over the ground round you; the
    ///   light-up tiles and strings of lights chase in gold, white, amber and aqua on a shared beat; the silver globes
    ///   and gold windows glow; a few pale clouds, a sky full of stars;
    /// - "Dancing Queen": every tile goes gold, the crown turns faster, the specks double. "Waterloo": the cannon
    ///   fires confetti. "The Winner Takes It All": the town dims and one cold light falls on the podium. "September":
    ///   fireworks over the sea by the lighthouse. "Rasputin": the onion dome spins and the light goes red and gold.
    ///   Every song's own light brightens while it plays.
    public class DiscoLife : MonoBehaviour
    {
        const string Land = "Discotopia";
        public Vector3 center, lightAt, cannonAt, cannonDir;
        public string queenSong, waterlooSong, winnerSong, septemberSong, rasputinSong;
        public Material[] tiles; public Material lamps, windows, floorBallM, ballM; public Transform ball, crown, dome; public Light floorLight, lightLamp, winnerLight;
        public float request = -99f, lever = -99f;
        public List<(string song, Material m, Color c)> songGlows = new List<(string, Material, Color)>();
        readonly Dictionary<string, float> songAmount = new Dictionary<string, float>();

        CloudLayer clouds; ParticleSystem specks, confetti, fireworks;
        Color[] tileBase; Color lampBase, windowBase, floorBallBase, ballBase;
        float queen, waterloo, winner, september, rasputin, nextSpeck, nextConfetti, nextFirework, spin;
        static readonly Color[] Party = { DiscoKit.Gold, DiscoKit.Silver, DiscoKit.Amber, DiscoKit.Aqua, new Color(1f, .4f, .65f) };

        void Start()
        {
            clouds = gameObject.AddComponent<CloudLayer>();
            clouds.center = center; clouds.count = 22; clouds.span = 2600f; clouds.key = "disco";
            clouds.minHeight = 170f; clouds.maxHeight = 240f; clouds.size = 2.6f; clouds.speed = 1.6f; clouds.cover = .2f;
            clouds.color = new Color(.5f, .54f, .72f);
            Color E(Material m) => m != null ? m.GetColor("_EmissionColor") : Color.black;
            tileBase = new Color[tiles.Length]; for (var i = 0; i < tiles.Length; i++) tileBase[i] = E(tiles[i]);
            lampBase = E(lamps); windowBase = E(windows); floorBallBase = E(floorBallM); ballBase = E(ballM);
            specks = Particles("Mirror-ball specks", 2500, new Color(1f, 1f, 1f, .95f), .1f, .2f);
            var sm = specks.main; sm.startLifetime = new ParticleSystem.MinMaxCurve(.35f, .6f); sm.startSpeed = 0f;
            confetti = Particles("Confetti", 800, Color.white, .08f, .14f);
            var cm = confetti.main; cm.startLifetime = new ParticleSystem.MinMaxCurve(3f, 5f); cm.gravityModifier = .25f;
            fireworks = Particles("Fireworks", 2000, Color.white, .25f, .5f);
            var fm = fireworks.main; fm.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.2f); fm.gravityModifier = .12f;
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
            var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, .15f), new GradientAlphaKey(0f, 1f) });
            cl.color = g;
            var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (var y = 0; y < 32; y++) for (var x = 0; x < 32; x++) { var d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(16, 16)) / 16f; tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Pow(Mathf.Clamp01(1f - d), 1.5f))); }
            tex.Apply();
            var m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = name };
            m.SetColor("_BaseColor", Color.white); m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 2f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            m.SetFloat("_ZWrite", 0f); m.SetFloat("_Cull", 0f); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent; m.SetTexture("_BaseMap", tex);
            var r = go.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return ps;
        }

        void Update()
        {
            var dt = Time.deltaTime; var t = Time.time;
            var here = SkyDirector.Instance != null && SkyDirector.Instance.CurrentLand == Land;
            var d = MusicDirector.Instance; var playing = d != null && d.CurrentSlot.IsValid;
            var energy = SurpriseDirector.Energy;
            var id = MusicDirector.ForcedTrackId ?? (playing ? d.CurrentSlot.track.id : null);
            bool On(string song) => here && song != null && id == song;

            queen = Mathf.MoveTowards(queen, On(queenSong) ? 1f : 0f, dt / 3f);
            waterloo = Mathf.MoveTowards(waterloo, On(waterlooSong) ? 1f : 0f, dt / 2f);
            winner = Mathf.MoveTowards(winner, On(winnerSong) ? 1f : 0f, dt / 6f);
            september = Mathf.MoveTowards(september, On(septemberSong) ? 1f : 0f, dt / 2f);
            rasputin = Mathf.MoveTowards(rasputin, On(rasputinSong) ? 1f : 0f, dt / 2f);

            foreach (var (s, _, _) in songGlows) if (!songAmount.ContainsKey(s)) songAmount[s] = 0f;
            var keys = new List<string>(songAmount.Keys);
            foreach (var s in keys) songAmount[s] = Mathf.MoveTowards(songAmount[s], here && id == s ? 1f : 0f, dt / 2f);
            foreach (var (s, m, c) in songGlows) if (m != null) m.SetColor("_EmissionColor", c * (1f + songAmount[s] * (2.5f + Mathf.Sin(t * 2.2f) * .6f)));

            // the tiles chase on a shared beat (120 a minute); a request, or Dancing Queen, lights the lot
            var beats = SharedChance.Now * 2.0; var beat = (float)(beats % 1.0); var kick = Mathf.Pow(1f - beat, 2.5f);
            var asked = t - request < 4f ? 1f - (t - request) / 4f : 0f; var hush = 1f - winner * .75f;
            for (var i = 0; i < tiles.Length; i++) if (tiles[i] != null)
            {
                var ph = Mathf.Repeat((float)(beats % 4.0) - i, 4f); var on = Mathf.Clamp01(1.4f - ph);
                var col = Color.Lerp(tileBase[i], tileBase[0] * 1.15f, queen * .85f);
                tiles[i].SetColor("_EmissionColor", col * (.38f + (.9f + energy * .5f) * on * (.5f + .5f * kick) + asked * 1.6f + queen * .7f) * hush);
            }
            if (lamps != null) lamps.SetColor("_EmissionColor", lampBase * (1f + energy * .25f + queen * .5f) * hush);
            if (windows != null) windows.SetColor("_EmissionColor", windowBase * (1f + .05f * Mathf.Sin(t * 1.3f) + queen * .5f) * (1f - winner * .5f));
            if (floorBallM != null) floorBallM.SetColor("_EmissionColor", floorBallBase * (1f + kick * .4f + asked * 1.5f + queen)); if (floorLight != null) floorLight.intensity = (3f + kick * 1f + asked * 3f + queen * 2f) * hush;
            var fast = t - lever < 20f ? 1f - (t - lever) / 20f : 0f;
            spin += dt * (14f + fast * 90f + queen * 30f); if (ball != null) ball.rotation = Quaternion.Euler(0, spin, 0);
            if (ballM != null) ballM.SetColor("_EmissionColor", ballBase * (1f + fast * .8f + queen * .6f + .1f * Mathf.Sin(t * 2f))); if (lightLamp != null) lightLamp.intensity = (3.4f + fast * 2f + queen * 1.5f) * (1f - winner * .5f);
            if (crown != null) crown.Rotate(0, (20f + queen * 120f) * dt, 0, Space.World);
            if (dome != null) dome.Rotate(0, (10f + rasputin * 260f) * dt, 0, Space.World);
            if (winnerLight != null) winnerLight.intensity = .8f + winner * 7f;

            // the sky: a clear deep blue night; gold for the Queen, cold and dark for the Winner, red and gold for Rasputin
            var tint = new Color(.12f, .16f, .36f); var amount = .1f;
            if (queen > 0f) { tint = Color.Lerp(tint, new Color(.9f, .7f, .3f), queen); amount = Mathf.Lerp(amount, .24f, queen); }
            if (rasputin > 0f) { tint = Color.Lerp(tint, Color.Lerp(new Color(.85f, .15f, .12f), new Color(1f, .75f, .25f), .5f + .5f * Mathf.Sin(t * 4f)), rasputin); amount = Mathf.Lerp(amount, .3f, rasputin); }
            if (september > 0f) { tint = Color.Lerp(tint, new Color(.3f, .22f, .5f), september); amount = Mathf.Lerp(amount, .2f, september); }
            if (winner > 0f) { tint = Color.Lerp(tint, new Color(.02f, .03f, .08f), winner); amount = Mathf.Lerp(amount, .7f, winner); }
            SkyDirector.SetSongTint(Land, tint, amount);
            var wind = clouds != null ? clouds.Wind : new Vector2(1, 0);
            if (here) { Shader.SetGlobalFloat("_EW_Wind", .25f + .2f * energy); Shader.SetGlobalVector("_EW_WindDir", new Vector4(wind.x, 0, wind.y, 0)); }

            // specks of light from the mirror ball, sweeping over the ground round you in turning rays
            var cam = Camera.main; var data = IslandWorldBuilder.Instance != null ? IslandWorldBuilder.Instance.Data : null;
            if (here && cam != null && data != null && lightAt != Vector3.zero && t >= nextSpeck)
            {
                nextSpeck = t + .03f; var cp = cam.transform.position; var n = 6 + Mathf.RoundToInt(fast * 10f + queen * 8f); const int rays = 28;
                for (var k = 0; k < n; k++)
                {
                    // pick a ray that passes near you, and a point along it near you
                    var toCam = Mathf.Atan2(cp.z - lightAt.z, cp.x - lightAt.x) * Mathf.Rad2Deg; var baseAng = spin * .5f; var idx = Mathf.Round((toCam - baseAng) / (360f / rays)) + Random.Range(-3, 4);
                    var ang = (baseAng + idx * (360f / rays)) * Mathf.Deg2Rad; var dir = new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang));
                    var along = Vector3.Dot(cp - lightAt, dir) + Random.Range(-26f, 26f); if (along < 6f) continue;
                    var p = lightAt + dir * along; p.y = data.HeightAt(p.x, p.z) + .12f; if (Mathf.Abs(p.y - cp.y) > 14f) continue;
                    specks.Emit(new ParticleSystem.EmitParams { position = p, startColor = k % 5 == 0 ? new Color(1f, .9f, .6f) : Color.white, startSize = Random.Range(.16f, .34f) }, 1);
                }
            }
            if (waterloo > 0f && cannonAt != Vector3.zero && t >= nextConfetti)
            {
                nextConfetti = t + 2.6f;
                for (var k = 0; k < 90; k++) confetti.Emit(new ParticleSystem.EmitParams { position = cannonAt, velocity = (cannonDir * Random.Range(6f, 12f) + Random.insideUnitSphere * 3f), startColor = Party[Random.Range(0, Party.Length)] }, 1);
            }
            if (september > 0f && lightAt != Vector3.zero && t >= nextFirework)
            {
                nextFirework = t + Random.Range(.7f, 1.6f); var at = lightAt + new Vector3(Random.Range(-70f, 70f), Random.Range(14f, 46f), Random.Range(-70f, 70f)); var col = Party[Random.Range(0, Party.Length)];
                for (var k = 0; k < 110; k++) fireworks.Emit(new ParticleSystem.EmitParams { position = at, velocity = Random.onUnitSphere * Random.Range(7f, 11f), startColor = col }, 1);
            }
        }
    }
}
