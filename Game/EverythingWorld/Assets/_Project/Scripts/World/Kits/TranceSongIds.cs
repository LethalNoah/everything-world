using System.Collections.Generic;

namespace EverythingWorld.World
{
    /// The Trance Steppe's songs by artist (generated from scenes.json; part of TranceKit), so every one of them can have
    /// its own light (TranceKit.Song, TranceKit.SongLamps).
    public partial class TranceKit
    {
        static readonly Dictionary<string, (string title, string id)[]> SongIds = new Dictionary<string, (string, string)[]>
        {
            { "Above & Beyond", new[] { ("Sun & Moon - Original Mix", "4rfhAoZjGwraqwX1w47uij"), ("Thing Called Love - Original Mix", "0YuDHcx0WM1tGOyidHMwnz"), ("You Got To Go - Seven Lions Remix", "2AygMLiFGcIqJ5IZH3pOQV") } },
            { "Darude", new[] { ("Sandstorm", "6Sy9BUbgFse0n0LPA5lwy5") } },
            { "Francesco Farfa", new[] { ("Universal Love", "2ZL4Cy7yBycVFTvAbgt3nQ") } },
            { "Gesaffelstein", new[] { ("Hate or Glory", "7Jlqg70T1RhZICoFJmj1Bm") } },
            { "Myon", new[] { ("Omen In The Rain - Seven Lions Remix", "6hX9jATESvv2cdXQMRU4m8") } },
            { "Primer", new[] { ("Waste Away", "6JMGMyMMHn3GzMJgTnpbLP") } },
            { "Robert Miles", new[] { ("Children - Dream Version", "7cEkyAXkwXCxTR3IKE0XHu") } },
            { "Soulero", new[] { ("Aura", "5YbvRCX4xqJKqtZWozwR8i") } },
            { "Velvetine", new[] { ("The Great Divide - Seven Lions Remix", "3tMEev0gTxaHJ9PiKpdij4") } },
            { "Zombie Nation", new[] { ("Kernkraft 400", "6PUzxtIHkv346yP89NzP9X") } },
            { "zts", new[] { ("goldenslaughterer", "6TPQ1r3hHhjfwY72zYQ5S4") } },
        };
    }
}
