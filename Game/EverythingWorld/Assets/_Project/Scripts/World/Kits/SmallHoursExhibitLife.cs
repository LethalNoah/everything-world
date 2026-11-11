using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// Noah Gallagher's exhibit, alive (part of SmallHoursLife): every piece follows its own song as it plays,
    /// section by section, from how far through the song the station is.
    /// - cues: lights that wake inside a stretch of the song (from..to, 0..1), steady or blinking in a pattern;
    /// - movers: things that turn, orbit, bob, slide along a path over a stretch of the song, or appear in it.
    public partial class SmallHoursLife
    {
        public class Cue { public string song; public Material m; public Color c; public float idle, gain, from, to, period, phase, duty; public float level; }
        public enum Move { Spin, Bob, Slide, Show, Swing }
        public class Mover { public Transform t; public string song; public Move kind; public Vector3 a, b; public float speed, from, to, extra; public Vector3 rest; public Quaternion restRot; public Vector3 restScale; public float level; }
        public readonly List<Cue> cues = new List<Cue>();
        public readonly List<Mover> movers = new List<Mover>();

        /// How far through the given song the station is (0..1), or -1 if it isn't playing here. For the photo tour
        /// (a forced song) it loops every 40 seconds, so every section gets seen.
        public static float Progress(string song)
        {
            if (string.IsNullOrEmpty(song)) return -1f;
            if (MusicDirector.ForcedTrackId == song) return Mathf.Repeat(Time.time / 40f, 1f);
            var d = MusicDirector.Instance;
            if (d == null || !d.CurrentSlot.IsValid || d.CurrentSlot.track.id != song) return -1f;
            if (SkyDirector.Instance == null || SkyDirector.Instance.CurrentLand != "The Small Hours") return -1f;
            var s = d.CurrentSlot; var len = s.endsAt - s.startsAt;
            return len > 0 ? Mathf.Clamp01((float)((RadioSchedule.NowSeconds - s.startsAt) / len)) : -1f;
        }

        static float Window(float p, float from, float to) => p >= from && p <= to ? 1f : 0f;

        void ExhibitUpdate(float dt)
        {
            var t = Time.time;
            var progress = new Dictionary<string, float>();
            float P(string s) { if (!progress.TryGetValue(s, out var v)) progress[s] = v = Progress(s); return v; }
            // a material can answer more than one cue (the first gate's motif wakes for "Water" too): the brightest wins
            var best = new Dictionary<Material, (float f, Color c)>();
            foreach (var c in cues)
            {
                if (c.m == null) continue;
                var p = P(c.song); var want = 0f;
                if (p >= 0f && Window(p, c.from, c.to) > 0f)
                    want = c.period > 0f ? (Mathf.Repeat(t / c.period + c.phase, 1f) < c.duty ? 1f : .12f) : 1f;
                c.level = c.period > 0f ? want : Mathf.MoveTowards(c.level, want, dt * 1.5f);
                var f = Mathf.Max(0f, c.idle + c.gain * c.level);
                if (!best.TryGetValue(c.m, out var b) || f > b.f) best[c.m] = (f, c.c);
            }
            foreach (var kv in best) kv.Key.SetColor("_EmissionColor", kv.Value.c * kv.Value.f);
            foreach (var m in movers)
            {
                if (m.t == null) continue;
                var p = P(m.song); var on = p >= 0f;
                m.level = Mathf.MoveTowards(m.level, on ? 1f : 0f, dt / 1.5f);
                switch (m.kind)
                {
                    case Move.Spin:   // turn about axis a, faster while the song plays
                        m.t.localRotation = m.restRot * Quaternion.AngleAxis(Mathf.Repeat(t * m.speed * (1f + m.extra * m.level), 360f), m.a); break;
                    case Move.Bob:
                        m.t.localPosition = m.rest + m.a * Mathf.Sin(t * m.speed + m.extra) * (1f + m.level); break;
                    case Move.Slide:  // from a to b over the stretch from..to of the song; rests at a otherwise
                        var f = on ? Mathf.Clamp01(Mathf.InverseLerp(m.from, m.to, p)) : 0f;
                        m.t.localPosition = Vector3.Lerp(m.a, m.b, Mathf.SmoothStep(0, 1, f)); break;
                    case Move.Show:   // grows in while the song is inside from..to
                        var want = on && p >= m.from && p <= m.to ? 1f : 0f;
                        var cur = m.t.localScale.x / Mathf.Max(.0001f, m.restScale.x);
                        cur = Mathf.MoveTowards(cur, want, dt / Mathf.Max(.05f, m.speed));
                        m.t.localScale = m.restScale * Mathf.Max(.0001f, cur); break;
                    case Move.Swing:  // rocks about axis a, more while the song is inside from..to
                        var amp = m.extra * (on && p >= m.from && p <= m.to ? 1f : .15f);
                        m.t.localRotation = m.restRot * Quaternion.AngleAxis(Mathf.Sin(t * m.speed) * amp, m.a); break;
                }
            }
        }
    }
}
