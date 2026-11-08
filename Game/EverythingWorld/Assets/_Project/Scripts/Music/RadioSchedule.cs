using System;

namespace EverythingWorld.Music
{
    /// Zone radio: every scene is a station that is always on air, even with nobody listening.
    /// Each station plays its tracks in a shuffled order and reshuffles every time it loops.
    /// The schedule is pure math on (scene, time), so every player's game computes the same song at the
    /// same position without talking to each other, as long as they share the clock and the scene data.
    public static class RadioSchedule
    {
        /// Time zero for all stations.
        public static readonly DateTime Epoch = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// Changing this reshuffles every station.
        public const ulong WorldSeed = 1856;

        /// Seconds to add to this PC's clock. 0 for now: Windows keeps clocks within about a second.
        /// Later the multiplayer host can send its clock so everyone matches exactly.
        public static double ClockOffsetSeconds;

        public static double NowSeconds => (DateTime.UtcNow - Epoch).TotalSeconds + ClockOffsetSeconds;

        /// One song on a station's schedule. Times are seconds since Epoch.
        public readonly struct Slot
        {
            public readonly TrackInfo track;
            public readonly double startsAt, endsAt;

            public Slot(TrackInfo track, double startsAt, double endsAt)
            {
                this.track = track;
                this.startsAt = startsAt;
                this.endsAt = endsAt;
            }

            public bool IsValid => track != null;
            public int PositionMsAt(double time) => (int)Math.Max(0, (time - startsAt) * 1000.0);
        }

        /// What the station for this scene is playing at the given time.
        public static Slot At(SceneInfo scene, double time)
        {
            var tracks = scene.tracks;
            if (tracks == null || tracks.Length == 0) return default;

            long lapMs = 0;
            foreach (var t in tracks) lapMs += LengthMs(t);

            var timeMs = (long)Math.Floor(time * 1000.0);
            var lap = Math.Max(0, timeMs / lapMs);
            var intoLap = timeMs - lap * lapMs;

            long elapsed = 0;
            foreach (var i in Order(scene.id, lap, tracks.Length))
            {
                var length = LengthMs(tracks[i]);
                if (intoLap < elapsed + length)
                {
                    var start = (lap * lapMs + elapsed) / 1000.0;
                    return new Slot(tracks[i], start, start + length / 1000.0);
                }
                elapsed += length;
            }
            return default; // not reached
        }

        static long LengthMs(TrackInfo t) => t.duration_ms > 0 ? t.duration_ms : 210000;

        /// The play order for one lap of a station: a shuffle seeded by world seed, scene and lap number.
        static int[] Order(int sceneId, long lap, int count)
        {
            var order = new int[count];
            for (var i = 0; i < count; i++) order[i] = i;
            var state = WorldSeed ^ ((ulong)(uint)sceneId * 0x9E3779B97F4A7C15UL) ^ ((ulong)lap * 0xD1B54A32D192ED03UL);
            for (var i = count - 1; i > 0; i--)
            {
                var j = (int)(SplitMix64(ref state) % (ulong)(i + 1));
                (order[i], order[j]) = (order[j], order[i]);
            }
            return order;
        }

        /// Small, fixed random-number generator, so every PC and Unity version shuffles identically.
        static ulong SplitMix64(ref ulong state)
        {
            var z = state += 0x9E3779B97F4A7C15UL;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }
    }
}
