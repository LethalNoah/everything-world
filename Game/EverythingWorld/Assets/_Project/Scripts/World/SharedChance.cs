using EverythingWorld.Music;

namespace EverythingWorld.World
{
    /// Randomness everyone shares: the same answer on every computer for the same moment, because it's
    /// worked out from the radio's shared clock (RadioSchedule.NowSeconds) instead of a random generator.
    /// That's how friends see the same shooting star, aurora night or wandering lantern without the game
    /// sending anything between them.
    public static class SharedChance
    {
        public static double Now => RadioSchedule.NowSeconds;
        /// The shared "day": it starts at 10:00 UTC (5 am US Eastern), so one aurora night or snowmen day
        /// covers a whole evening of playing instead of flipping mid-evening.
        public static long Day => (long)((Now - 10 * 3600.0) / 86400.0);
        public static long Hour => (long)(Now / 3600.0);

        /// A stable number for a piece of text, the same on every computer.
        public static ulong Key(string text)
        {
            ulong h = 1469598103934665603UL;
            foreach (var c in text ?? "") { h ^= c; h *= 1099511628211UL; }
            return h;
        }

        /// A value in 0..1 for this key and time window (and an optional extra number).
        public static double Value(ulong key, long window, long extra = 0)
        {
            var z = key ^ ((ulong)window * 0x9E3779B97F4A7C15UL) ^ ((ulong)extra * 0xD1B54A32D192ED03UL);
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            z ^= z >> 31;
            return (z >> 11) * (1.0 / (1UL << 53));
        }

        public static bool Roll(ulong key, long window, double chance, long extra = 0) => Value(key, window, extra) < chance;

        /// The song playing on the station you're tuned to, as a window number everyone on that station
        /// shares (its start time and track). False if nothing is playing.
        public static bool CurrentSong(out long song, out double start, out double length)
        {
            var d = EverythingWorld.Music.MusicDirector.Instance;
            song = 0; start = 0; length = 0;
            if (d == null || !d.CurrentSlot.IsValid) return false;
            start = d.CurrentSlot.startsAt; length = d.CurrentSlot.endsAt - start;
            song = (long)(Key(d.CurrentSlot.track.id) & 0xFFFFFFF) ^ (long)start;
            return true;
        }
    }
}
