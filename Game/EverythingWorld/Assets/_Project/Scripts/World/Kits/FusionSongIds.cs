using System.Collections.Generic;

namespace EverythingWorld.World
{
    /// The Fusion Isthmus's songs by artist (generated from scenes.json; part of FusionKit), so every one of them can have
    /// its own light (FusionKit.Song, FusionKit.SongLamps).
    public partial class FusionKit
    {
        static readonly Dictionary<string, (string title, string id)[]> SongIds = new Dictionary<string, (string, string)[]>
        {
            { "Allan Holdsworth", new[] { ("Devil Take the Hindmost - Remastered", "2Wg7HdjbyrblvIsFjWk0mh") } },
            { "BADBADNOTGOOD", new[] { ("Confessions", "1Sc2RHxyKHMeyCChQTWTnp") } },
            { "CASIOPEA", new[] { ("ドミノ・ライン - Live at Chuo Kaikan Hall, Tokyo, Feb. 1982", "1y76Hl0nXEOYBf4C9rl5rf") } },
            { "Ezra Collective", new[] { ("Only Love (feat. Pa Salieu)", "7lmh1ByCWe26Vtkz2w21lC"), ("Victory Dance", "6GumLQysBiahvtJmxMXOpn"), ("Well Organised (feat. Lila Iké)", "4zrTWDe8FwBQQNxL6XwdNM") } },
            { "GoGo Penguin", new[] { ("Garden Dog Barbecue", "0q9pEXqEUZivY4Ll7uBxOy") } },
            { "Krystyna Prońko", new[] { ("Jesteś lekiem na całe zło - 1994", "6hqftK2O6t2sI5AmwLhnLs") } },
            { "Latarnik", new[] { ("Insia", "32EPUX5W4pCWvyisqtwM3L") } },
            { "Medeski, Martin & Wood", new[] { ("Bubblehouse", "2YtX5xh7y5FoQywRmsGCeK") } },
            { "Mildlife", new[] { ("The Magnificent Moon", "2gEOUPrIi4NgX175mZbKH7") } },
            { "Pat Metheny Group", new[] { ("The Roots of Coincidence", "4RSVaT769OstCHABytr1m7") } },
            { "Rishab Rikhiram Sharma", new[] { ("Chanakya", "4WZE6JnKn3jacgnaJOTF9T") } },
            { "Tara Clerkin Trio", new[] { ("Silently", "3L4f41uumSBoUWdjb3W0bn"), ("Slow Island", "6AKjInJpVHG365ACzhShJC") } },
            { "The Comet Is Coming", new[] { ("Birth Of Creation", "3CuOokDRR0Z2r1WaPRXKaF") } },
            { "Tom Misch", new[] { ("Beautiful Escape", "5MChi9fdCbTIWDJPPUuuW6"), ("Disco Yes", "2fvdQ1Uq04qE4RLN5zKBXS"), ("Last 100", "39WFZVKifA8Fl2TZI2kp0S"), ("Nightrider", "5TgZ7lOUugOdabBcD9a6ud"), ("Sensational", "5oEhWt6RfUHSBiSfZVnymh"), ("Tidal Wave", "4Goj7amQLl2PiPYaiB0VlV"), ("What Kinda Music", "47w6S27JF4Zvrz1Wd6R66Z") } },
            { "Venna", new[] { ("Sicily' Box", "7EdQ9Z80HAKiISr1MZABpZ") } },
            { "Victor Wooten", new[] { ("Dc10", "02DnrxW4vWrDT4EvP0fPbh") } },
            { "Yussef Dayes", new[] { ("Birds of Paradise", "1L317PdBTTcuMQaxhRXnZU"), ("Rust (feat. Tom Misch)", "1nsaIli1TgKxLjHmWqDRmE"), ("Turquoise Galaxy", "71vcxMgWBvsUbwtRBzk2Vd") } },
            { "Yussef Kamaal", new[] { ("Joint 17", "5MJNFfr1ZcBith6OTHb2gv"), ("Strings of Light", "3hEOsfDNsdpV78Ty01vaxi") } },
        };
    }
}
