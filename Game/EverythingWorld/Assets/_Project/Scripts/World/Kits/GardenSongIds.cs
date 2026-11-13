using System.Collections.Generic;

namespace EverythingWorld.World
{
    /// The Sample Gardens's songs by artist (generated from scenes.json; part of GardenKit), so every one of them can have
    /// its own light (GardenKit.Song, GardenKit.SongLamps).
    public partial class GardenKit
    {
        static readonly Dictionary<string, (string title, string id)[]> SongIds = new Dictionary<string, (string, string)[]>
        {
            { "Atoms For Peace", new[] { ("Dropped", "1Ewb1UPrJC9mCJiLPa2sk0"), ("Ingenue", "2sEBtJ37VgGe048SH9Bvqq") } },
            { "BLARF", new[] { ("Stars Without Light", "5ktIr0VWafVU9Cn3Z66iTv") } },
            { "Bibio", new[] { ("Curls", "3bylizl2h9xMjnP8fRKrdz"), ("Petals", "2wQlywnCQG88drPNdEOuRO"), ("lovers’ carvings", "2cRYvcyWfz7jieYIBkI7JR") } },
            { "Burka Band", new[] { ("Burka Blue - Orig.", "1NPzGmAtlAE2Pqz5W6c4rB") } },
            { "Caribou", new[] { ("Silver", "23oI6EIMQgdiAeiIP209FH") } },
            { "DJ Plead", new[] { ("pa700", "0SqqAF6V4ClXmpZ2LKAZLf") } },
            { "Dolphin Machine", new[] { ("Sunshine Gradient - Haircuts for Men Remix", "7aIYGRin0SbExMlzxotSQP") } },
            { "Four Tet", new[] { ("Two Thousand and Seventeen", "2ZIaH69kaz55RM4Pjx6KXl") } },
            { "The Avalanches", new[] { ("ETOH", "4Zxk3oLRAvHX6eSaIcor1T"), ("Since I Left You", "1AAYbsAIgEJMbxgLgpjE9y") } },
            { "The Books", new[] { ("Tokyo", "3MXBrZkK51A3Z4izKFnMkn") } },
            { "Thom Yorke", new[] { ("Hearing Damage", "1fTtPH0xGwFOXM5VlkxOX1") } },
            { "Wintergatan", new[] { ("Sommarfågel", "6xCm0ritbxwRwSJGKOD8GC") } },
            { "alyzea", new[] { ("aero plaza", "0gVj0UaWp5XQOlInyjPqJ5") } },
            { "tictacto", new[] { ("homesick - tacto flip", "0MyI0Ea3u7ZBNLxwFDd37i") } },
        };
    }
}
