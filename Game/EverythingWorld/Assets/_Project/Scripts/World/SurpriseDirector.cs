using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EverythingWorld.World
{
    /// Spots on each land where a surprise can appear (Lanternwood registers its path lanterns).
    public static class SurpriseSpots
    {
        static readonly Dictionary<string, List<Vector3>> spots = new Dictionary<string, List<Vector3>>();
        public static void Clear() => spots.Clear();
        public static void Add(string land, Vector3 p)
        {
            if (!spots.TryGetValue(land, out var list)) spots[land] = list = new List<Vector3>();
            list.Add(p);
        }
        public static List<Vector3> Get(string land) => land != null && spots.TryGetValue(land, out var l) ? l : null;
    }

    /// Small surprises so the world is never quite the same twice, all timed by the radio's shared clock so
    /// friends see the same thing at the same moment (see SharedChance):
    /// - the lights follow the music: glows and fires burn brighter for high-energy songs, and every lantern
    ///   pulses softly when a new song starts on your station;
    /// - on starry lands: a comet in every song, a meteor shower in about one song in three, and the odd
    ///   shooting star in between;
    /// - the wandering lanterns: with each song a new handful of lanterns on the land glow different colours.
    /// Aurora nights are part of the sky (SkyDirector). Snowmen move each day (the land kit).
    public class SurpriseDirector : MonoBehaviour
    {
        /// The current song's energy (0..1, smoothed) and the new-song pulse (1 fading to 0).
        public static float Energy = .5f, Pulse;

        double lastSlotStart = -1;

        // shooting stars: now and then on starry lands, and every few hours a chance of a meteor shower, when
        // they stream from one point in the sky every second or two for about ten minutes
        const float MeteorWindow = 15f, MeteorChance = .3f, MeteorRadius = 1600f;
        const float ShowerBlock = 3 * 3600f, ShowerChance = .5f, ShowerLength = 600f, ShowerWindow = 1.6f, ShowerMeteorChance = .75f;
        const double SongShowerChance = .34;
        long cometSong = -1;
        class Meteor { public LineRenderer line; public Material mat; public double start = -1, length; public Vector3 from, to; public bool comet; }
        readonly Meteor[] meteors = new Meteor[8];
        long lastWindow = -1;

        // the wandering lantern
        static readonly Color[] LanternColors =
            { new Color(1f, .35f, .55f), new Color(.65f, .4f, 1f), new Color(.3f, 1f, .6f), new Color(.45f, .85f, 1f), new Color(1f, .82f, .3f) };
        string lanternLand;
        long lanternHour = -1;
        const int WanderingCount = 5;
        GameObject wanderer;
        readonly List<Light> wanderLights = new List<Light>();

        void Update()
        {
            // test keys, just for you (nothing shared): F7 a shooting star now, F8 the aurora on or off, F9 a meteor shower
            var kb = Keyboard.current;
            if (kb != null && kb.f7Key.wasPressedThisFrame) TestMeteor();
            if (kb != null && kb.f9Key.wasPressedThisFrame) testShowerUntil = SharedChance.Now + 60.0; // F9: a one-minute shower
            if (kb != null && kb.f8Key.wasPressedThisFrame && SkyDirector.Instance != null) SkyDirector.Instance.ToggleTestAurora();
            FollowMusic();
            ShootingStars();
            WanderingLantern();
            Shader.SetGlobalFloat("_EW_Energy", Energy);
            Shader.SetGlobalFloat("_EW_Pulse", Pulse);
        }

        void FollowMusic()
        {
            var d = MusicDirector.Instance;
            var target = .5f;
            if (d != null && d.CurrentSlot.IsValid)
            {
                target = d.CurrentSlot.track.energy;
                var start = d.CurrentSlot.startsAt;
                if (start != lastSlotStart)
                {
                    // a pulse only when the song really begins now, not when you walk in halfway through it
                    if (lastSlotStart >= 0 && RadioSchedule.NowSeconds - start < 3.0) Pulse = 1f;
                    lastSlotStart = start;
                }
            }
            Energy = Mathf.MoveTowards(Energy, target, Time.deltaTime * .25f);
            Pulse = Mathf.Max(0f, Pulse - Time.deltaTime / 2.5f);
        }

        /// Is a meteor shower on over this land right now? Returns where in the sky it streams from.
        double testShowerUntil = -1;

        bool ShowerNow(string land, out Vector3 radiant)
        {
            if (SharedChance.Now < testShowerUntil) { radiant = Quaternion.Euler(-60f, 40f, 0) * Vector3.forward; return true; }
            if (SharedChance.CurrentSong(out var song, out var songStart, out var songLength))
            {
                var sk = SharedChance.Key("songshower:" + land);
                radiant = Quaternion.Euler(-(45f + (float)SharedChance.Value(sk, song, 2) * 30f), (float)SharedChance.Value(sk, song, 3) * 360f, 0) * Vector3.forward;
                if (!SharedChance.Roll(sk, song, SongShowerChance)) return false;
                var len = System.Math.Min(60.0, songLength * .4);
                var st = songStart + SharedChance.Value(sk, song, 1) * (songLength - len);
                return SharedChance.Now >= st && SharedChance.Now < st + len;
            }
            var key = SharedChance.Key("shower:" + land);
            var block = (long)(SharedChance.Now / ShowerBlock);
            radiant = Quaternion.Euler(-(45f + (float)SharedChance.Value(key, block, 2) * 30f), (float)SharedChance.Value(key, block, 3) * 360f, 0) * Vector3.forward;
            if (!SharedChance.Roll(key, block, ShowerChance)) return false;
            var start = block * ShowerBlock + SharedChance.Value(key, block, 1) * (ShowerBlock - ShowerLength);
            var now = SharedChance.Now;
            return now >= start && now < start + ShowerLength;
        }

        void ShootingStars()
        {
            var sky = SkyDirector.Instance;
            var cam = Camera.main;
            if (sky == null || cam == null) return;
            var now = SharedChance.Now;
            var shower = ShowerNow(sky.CurrentLand, out var radiant);
            var windowLen = shower ? ShowerWindow : MeteorWindow;
            var window = (long)(now / windowLen);
            if (window != lastWindow)
            {
                lastWindow = window;
                var key = SharedChance.Key((shower ? "showermeteor:" : "meteor:") + sky.CurrentLand);
                if (SharedChance.Roll(key, window, shower ? ShowerMeteorChance : MeteorChance))
                {
                    var m = FreeMeteor();
                    if (m != null)
                    {
                        m.start = window * windowLen + SharedChance.Value(key, window, 1) * windowLen * .8;
                        m.length = .8 + SharedChance.Value(key, window, 2) * .8;
                        var travel = 18f + (float)SharedChance.Value(key, window, 5) * 18f;
                        if (shower)
                        {
                            // starts a little away from the radiant and streaks straight away from it
                            var off = Quaternion.Euler(((float)SharedChance.Value(key, window, 3) - .5f) * 50f, ((float)SharedChance.Value(key, window, 4) - .5f) * 70f, 0);
                            m.from = (off * radiant).normalized;
                            var away = (m.from - radiant).normalized;
                            m.to = (m.from + away * travel * Mathf.Deg2Rad).normalized;
                        }
                        else
                        {
                            var az = (float)SharedChance.Value(key, window, 3) * 360f;
                            var el = 28f + (float)SharedChance.Value(key, window, 4) * 38f;
                            var dir = SharedChance.Value(key, window, 6) < .5 ? -1f : 1f;
                            m.from = Quaternion.Euler(-el, az, 0) * Vector3.forward;
                            m.to = Quaternion.Euler(-(el - travel * .6f), az + dir * travel, 0) * Vector3.forward;
                        }
                        if (m.to.y < .05f) m.to = (m.to + Vector3.up * .1f).normalized;
                    }
                }
            }
            // a comet in every song: big, slow and long-tailed, partway through (the same moment for everyone)
            if (SharedChance.CurrentSong(out var song, out var songStart, out var songLength) && song != cometSong)
            {
                cometSong = song;
                var ck = SharedChance.Key("comet:" + sky.CurrentLand);
                var c = FreeMeteor();
                if (c != null)
                {
                    c.start = songStart + songLength * (.15 + SharedChance.Value(ck, song, 1) * .6);
                    c.length = 4.0;
                    c.comet = true;
                    var az = (float)SharedChance.Value(ck, song, 2) * 360f;
                    var el = 35f + (float)SharedChance.Value(ck, song, 3) * 25f;
                    c.from = Quaternion.Euler(-el, az, 0) * Vector3.forward;
                    c.to = Quaternion.Euler(-(el - 12f), az + (SharedChance.Value(ck, song, 4) < .5 ? -40f : 40f), 0) * Vector3.forward;
                }
            }
            var eye = cam.transform.position;
            foreach (var m in meteors)
            {
                if (m == null) continue;
                var t = m.start < 0 || sky.CurrentStars < .3f ? -1 : (now - m.start) / m.length;
                if (t < 0 || t > 1) { m.line.enabled = false; if (t > 1) { m.start = -1; m.comet = false; } continue; }
                var head = Vector3.Slerp(m.from, m.to, (float)t);
                var tail = Vector3.Slerp(m.from, m.to, Mathf.Max(0f, (float)t - (m.comet ? .35f : .5f)));
                m.line.enabled = true;
                m.line.widthMultiplier = m.comet ? 2.6f : 1f;
                m.line.SetPosition(0, eye + tail * MeteorRadius);
                m.line.SetPosition(1, eye + head * MeteorRadius);
                m.mat.SetFloat("_Fade", Mathf.Sin((float)t * Mathf.PI) * Mathf.Clamp01(sky.CurrentStars * 1.3f));
            }
        }

        Meteor FreeMeteor()
        {
            for (var i = 0; i < meteors.Length; i++)
            {
                if (meteors[i] == null) meteors[i] = MakeMeteor();
                if (meteors[i] != null && meteors[i].start < 0) return meteors[i];
            }
            return null;
        }

        void TestMeteor()
        {
            var m = FreeMeteor();
            if (m == null) return;
            m.start = SharedChance.Now + .2;
            m.length = 1.2;
            var az = Random.value * 360f;
            m.from = Quaternion.Euler(-50f, az, 0) * Vector3.forward;
            m.to = Quaternion.Euler(-32f, az + 26f, 0) * Vector3.forward;
        }

        Meteor MakeMeteor()
        {
            var shader = Shader.Find("Everything World/Meteor");
            if (shader == null) return null;
            var go = new GameObject("Shooting star");
            go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.useWorldSpace = true;
            line.textureMode = LineTextureMode.Stretch;
            line.widthCurve = AnimationCurve.Linear(0, 1f, 1, 14f);
            line.startColor = new Color(.7f, .8f, 1f); line.endColor = Color.white;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.enabled = false;
            var mat = new Material(shader) { name = "Shooting star" };
            line.sharedMaterial = mat;
            return new Meteor { line = line, mat = mat };
        }

        void WanderingLantern()
        {
            var sky = SkyDirector.Instance;
            var land = sky != null ? sky.CurrentLand : null;
            var hour = SharedChance.CurrentSong(out var song, out _, out _) ? song : SharedChance.Hour;
            if (land != lanternLand || hour != lanternHour)
            {
                lanternLand = land; lanternHour = hour;
                if (wanderer != null) Destroy(wanderer);
                var spots = SurpriseSpots.Get(land);
                if (spots == null || spots.Count == 0) return;
                var key = SharedChance.Key("lantern:" + land);
                wanderer = new GameObject("The wandering lanterns");
                wanderer.transform.SetParent(transform, false);
                var halos = new GlowHalos();
                wanderLights.Clear();
                var used = new HashSet<int>();
                for (var k = 0; k < WanderingCount && used.Count < spots.Count; k++)
                {
                    var idx = (int)(SharedChance.Value(key, hour, 10 + k) * spots.Count) % spots.Count;
                    while (used.Contains(idx)) idx = (idx + 1) % spots.Count;
                    used.Add(idx);
                    var at = spots[idx];
                    var color = LanternColors[(k + (int)(SharedChance.Value(key, hour, 1) * LanternColors.Length)) % LanternColors.Length];
                    halos.Add(at, 2.4f, color, 1.6f);
                    halos.Add(at, .9f, Color.Lerp(color, Color.white, .5f), 1.2f);
                    wanderLights.Add(ArtistBits.PointLight(wanderer.transform, at - Vector3.up * .3f, color, 14f, 2.6f));
                }
                halos.Build("Glow", wanderer.transform);
            }
            for (var k = 0; k < wanderLights.Count; k++)
                if (wanderLights[k] != null) wanderLights[k].intensity = 2.2f + Mathf.Sin(Time.time * 1.3f + k * 1.7f) * .6f; // a slow breath, so they catch the eye
        }
    }
}
