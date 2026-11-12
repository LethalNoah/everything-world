using System.Collections.Generic;

namespace EverythingWorld.World
{
    /// The Nu Metal Quarry's songs by artist (generated from scenes.json; part of QuarryKit), so every one of them
    /// can have its own light (QuarryKit.Song, QuarryKit.SongLamps).
    public partial class QuarryKit
    {
        static readonly Dictionary<string, (string title, string id)[]> SongIds = new Dictionary<string, (string, string)[]>
        {
            { "3 Doors Down", new[] { ("Here Without You", "3NLrRZoMF0Lx6zTlYqeIo4"), ("Kryptonite", "3bbUkaQYGQHkx1TJi7gPSL"), ("Kryptonite", "6ZOBP3NvffbU4SZcrnt1k6") } },
            { "APG", new[] { ("Embrace", "3U3kbTHdVxOFs2gQIVrlUc") } },
            { "Badflower", new[] { ("Heroin", "74drJLr20XF1f4yqyNPRSZ") } },
            { "Breaking Benjamin", new[] { ("Anthem Of The Angels", "0qmmx55Ei3i75ViWcY6mEp"), ("Dear Agony", "0c3fJd2HRi6uHWgTljIae5"), ("Give Me A Sign", "6BWrIaoodtt7EX6kToWZw0") } },
            { "Creed", new[] { ("Higher", "1ZozJfi8u9cO2Ob8KwiwNT"), ("My Own Prison", "5vRPXm59z8ewWO6WiJHg3m"), ("With Arms Wide Open", "0eKyHwckh9vQb8ncZ2DXCs") } },
            { "Disturbed", new[] { ("Stricken", "6RJdYpFQwLyNfDc5FbjkgV"), ("The Sound of Silence", "1Cj2vqUwlJVG27gJrun92y") } },
            { "Hoobastank", new[] { ("The Reason", "77loZpT5Y5PRP1S451P9Yz") } },
            { "Linkin Park", new[] { ("In the End", "60a0Rd6pjrkxjPbaKzXjfq") } },
            { "Nickelback", new[] { ("Bones For The Crows", "1OLUxve0ddtop3lCBp4zvN"), ("Far Away", "0ADZ5dmXhlfzjMw6lefoPl"), ("How You Remind Me", "0gmbgwZ8iqyMPmXefof8Yf"), ("If Today Was Your Last Day", "4QJLKU75Rg4558f4LbDBRi"), ("Photograph", "3hb2ScEVkGchcAQqrPLP0R"), ("Rockstar", "6n9yCXvLhnYMgJIiIcMu7D"), ("S.E.X.", "56h3v9zaotemQ0NB21dlkg") } },
            { "NOTHING MORE", new[] { ("Fade In / Fade Out", "3HYs3wvhafGbyoGtLspEzr"), ("VALHALLA (Too Young To See)", "5WMfeZzBIS2s2I1gnFVNCg") } },
            { "Puddle Of Mudd", new[] { ("Blurry", "6lSr3iZTC144PKhvbPFzMp") } },
            { "Seether", new[] { ("Broken", "507bYMYfbm6sUS9iEAaeSd") } },
        };
    }
}
