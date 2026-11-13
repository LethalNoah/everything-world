using System.Collections.Generic;

namespace EverythingWorld.World
{
    /// The Liquid Rapids's songs by artist (generated from scenes.json; part of RapidsKit), so every one of them can have
    /// its own light (RapidsKit.Song, RapidsKit.SongLamps).
    public partial class RapidsKit
    {
        static readonly Dictionary<string, (string title, string id)[]> SongIds = new Dictionary<string, (string, string)[]>
        {
            { "B-complex", new[] { ("Beautiful Lies", "6SJ2J7yqMSWPyJ4Mts4MhO") } },
            { "Birdy", new[] { ("Wings - Nu:Logic Remix", "0l9aqnEwV2h2m3EpLsJINW") } },
            { "Calibre", new[] { ("Mr Majestic", "0wrg4FXpFKz1y1tYsZPJOw") } },
            { "Camo & Krooked", new[] { ("Turn Up (The Music) - Pola & Bryson Remix", "4j8clbIUQKapjzwXrDXko0") } },
            { "Goldie", new[] { ("Inner City Life", "0O7xFEqePrcTUgOi4qe0uB") } },
            { "High Contrast", new[] { ("If We Ever", "33LQ2hkSecs8ax6Bz2t9D8"), ("Return Of Forever", "3H8lyID1vH8viYA5RRobfs") } },
            { "Hybrid Minds", new[] { ("Touch", "2jtlU6H5C9WFB0x72f6YGo") } },
            { "LAOS", new[] { ("Panda Style", "2vaDb5ltVsEKIeCz2kc61C") } },
            { "Lenzman", new[] { ("Open Page", "6feSAu9lOLKiMf7AbFSLSV") } },
            { "Logistics", new[] { ("Hayling", "74O4aeJxNooiQe0lng5B3V") } },
            { "London Elektricity", new[] { ("Life Is Beautiful", "50aWN8cmrecv7rKG7yVoIO") } },
            { "Melano", new[] { ("On Fire", "6it78A4UO46gMuq0GgkPro") } },
            { "Metrik", new[] { ("We Are The Energy", "4w9cDtwU46gMFizCmxbxR9") } },
            { "Netsky", new[] { ("Everybody Loves The Sunshine", "4ch0ejqrSwJrotLroa9mXn"), ("Rio (feat. Digital Farm Animals)", "5IfSQQzGijWKd7dbkbqHuN") } },
            { "Pendulum", new[] { ("Witchcraft", "4Y2glvLjQGOb4dXnwm1hQf") } },
            { "Roni Size", new[] { ("Brown Paper Bag", "3ZQs8RHO3lPZoUwpavPENL") } },
            { "Sub Focus", new[] { ("Desire (Sub Focus & Dimension)", "507UUx311ghfFTMSuHUeyS") } },
            { "Uk Apache", new[] { ("Original Nuttah 25", "55K24vPjLgAX8yLAq8fErj") } },
            { "Wilkinson", new[] { ("Afterglow", "6LW3Z1GqbL78TIjfDyg4zp") } },
            { "jungle wizard", new[] { ("Peace Within Colors", "2VsN8lp62Uu96DGuYDSW0b") } },
        };
    }
}
