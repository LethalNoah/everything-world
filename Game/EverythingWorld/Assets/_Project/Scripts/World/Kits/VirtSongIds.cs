using System.Collections.Generic;

namespace EverythingWorld.World
{
    /// Mount Virtuoso's songs by artist (generated from scenes.json; part of VirtKit), so every one of them can have
    /// its own light (VirtKit.Song, VirtKit.SongLamps).
    public partial class VirtKit
    {
        static readonly Dictionary<string, (string title, string id)[]> SongIds = new Dictionary<string, (string, string)[]>
        {
            { "Buckethead", new[] { ("Ghost", "4wWejkfOcMPsZjxZ99vZXC") } },
            { "Enya", new[] { ("Caribbean Blue - 2009 Remaster", "5KZ4DC772dYcRBAizx0yYk"), ("Only Time", "6FLwmdmW77N1Pxb1aWsZmO") } },
            { "Eric Johnson", new[] { ("Cliffs Of Dover - Instrumental", "5qm0KiVKMXW1kq6VrnIhz5") } },
            { "Joe Satriani", new[] { ("Raspberry Jam Delta-V", "2L6mZ7KHMqfUQV3kWmEFlj") } },
            { "Nili Brosh", new[] { ("The Chase", "2oX98Yc8Nn3elnjH35mRtJ") } },
            { "Pat's Soundhouse", new[] { ("Eggshell Song", "3VsNta0mUSXCEPALQxPrQf") } },
            { "Richie Kotzen", new[] { ("Chase the Dragon", "2axYoGoMFff1jH6OzeCGRp") } },
            { "Vangelis", new[] { ("Titles", "36qn2bFfZzUwM6eIv2XOc9") } },
            { "Ólafur Arnalds", new[] { ("Near Light", "6Xajr512QEGAZ20bqAWkyT"), ("saman", "5RqKo9P4DLA0bVyY70cWkr"), ("Öldurót - Island Songs IV", "7gOi88gP4o1uv1kaL02yBB") } },
        };
    }
}
