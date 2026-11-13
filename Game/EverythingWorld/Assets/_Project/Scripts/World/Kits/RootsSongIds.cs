using System.Collections.Generic;

namespace EverythingWorld.World
{
    /// The Roots Bay's songs by artist (generated from scenes.json; part of RootsKit), so every one of them can have
    /// its own light (RootsKit.Song, RootsKit.SongLamps).
    public partial class RootsKit
    {
        static readonly Dictionary<string, (string title, string id)[]> SongIds = new Dictionary<string, (string, string)[]>
        {
            { "Baha Men", new[] { ("Who Let The Dogs Out", "1H5tvpoApNDxvxDexoaAUo") } },
            { "Bob Marley & The Wailers", new[] { ("I Shot The Sheriff - Live At The Lyceum, London/1975", "1U3upU6WjzDGB4oLEoijsg"), ("No Woman, No Cry - Live At The Lyceum, London/1975", "423hwXFgoN8RYmqLoLuVvY"), ("Trenchtown Rock - Live At The Lyceum, London/1975", "5VFllKqmcL8ZwWcH7tDLbr") } },
            { "Burning Spear", new[] { ("Marcus Garvey", "30ELPvO4DH57Rq2kgj7a8e") } },
            { "Chronixx", new[] { ("Skankin' Sweet", "5SQaQWvBDEAeug4EPyYEGE") } },
            { "Damian Marley", new[] { ("Welcome To Jamrock", "4PvVHAkONOaS6v4wgONGYZ") } },
            { "Desmond Dekker & The Aces", new[] { ("Israelites", "5U1GMgbXXM5eLvKUDnvqNS") } },
            { "Gregory Isaacs", new[] { ("Night Nurse", "4DQttwipnILl88cru3BRZx") } },
            { "Jimmy Cliff", new[] { ("The Harder They Come", "2XIr0KTybGQsC1SFfeaZhI") } },
            { "Lee \"Scratch\" Perry", new[] { ("Chase the Devil", "0bu5oNxU5pdQuFGxT4gwNr") } },
            { "Major Lazer", new[] { ("Powerful (feat. Ellie Goulding & Tarrus Riley)", "3eUhfanL9kEmpIPeGBl1yW") } },
            { "Original Koffee", new[] { ("Toast", "45tHjcM6gERQ7kCEDTG4al") } },
            { "Peter Tosh", new[] { ("Legalize It", "73lYI49LhfsscO33ohrGN0") } },
            { "Shabba Ranks", new[] { ("Mr. Loverman (feat. Chevelle Franklin)", "3Vs5Ez1aRlNVorMwGuaPL1") } },
            { "Shaggy", new[] { ("It Wasn't Me", "3WkibOpDF7cQ5xntM1epyf") } },
            { "The Specials", new[] { ("A Message to You Rudy - 2015 Remaster", "49slT7gVW0zj1KIG8w6DoL"), ("Ghost Town", "6ewN9MaFbi78oDLT9wYDgn") } },
            { "Toots & The Maytals", new[] { ("54-46 Was My Number", "1q8gelFgFYUwoWpQV7WNCe"), ("Pressure Drop - Single Version", "2GImcOxFZ0GP49rmPnPOjT") } },
        };
    }
}
