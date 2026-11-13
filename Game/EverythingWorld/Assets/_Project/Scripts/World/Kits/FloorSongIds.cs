using System.Collections.Generic;

namespace EverythingWorld.World
{
    /// The Four-on-the-Floor Steppe's songs by artist (generated from scenes.json; part of FloorKit), so every one of them can have
    /// its own light (FloorKit.Song, FloorKit.SongLamps).
    public partial class FloorKit
    {
        static readonly Dictionary<string, (string title, string id)[]> SongIds = new Dictionary<string, (string, string)[]>
        {
            { "+TEK", new[] { ("Disco Necropolis (Graveyard Stage)", "2RUisMVUJXK2nUXYdg0f6Q") } },
            { "Astronaut", new[] { ("Champions - WRLD Remix", "2gs6rBDPvocRM3yrMZd0HM"), ("Earthsphere VIP", "2jqdhXfdKVpG5SrlJq3vKm"), ("Rain - Calvertron Remix", "7gjff8nfICbjyq5rpXUE3n"), ("War - F.O.O.L & Didrick Remix", "2OvfUrtzroV8XstUN4VyDL") } },
            { "Avicii", new[] { ("Levels - Radio Edit", "5UqCQaDshqbIk3pkhy4Pjg"), ("Wake Me Up", "4h8VwCb1MTGoLKueQ1WgbD") } },
            { "Barry Can't Swim", new[] { ("Return To Bhibo", "0b04O4aNqXhiUtHJcb6bcm") } },
            { "Cash Cash", new[] { ("Overtime", "2E01JGJGMa5gW1PmqAWYXG") } },
            { "Chromeo", new[] { ("Needy Girl", "1DFfyHLfG6NdZ7UD14NrEI") } },
            { "Daft Punk", new[] { ("Contact", "2KHRENHQzTIQ001nlP9Gdc"), ("End of Line", "09TlxralXOGX35LUutvw7I") } },
            { "Duke", new[] { ("Habibi", "6zcVRQpHsxfGm90HgnDsjh") } },
            { "Electric Joy Ride", new[] { ("Power-Up - Original Mix", "6yZZFVaYNVvVNVpEjiB3Q3") } },
            { "Flight Facilities", new[] { ("Crave You - Adventure Club Remix", "5jdGLLEA1e85jy9VYdggoP") } },
            { "Francis Mercier", new[] { ("Unjust", "7qHc4dGw5Bnfaa6RkGOFu0") } },
            { "Justice", new[] { ("Flower In A Hurricane (feat. Tame Impala, SZA, King Gizzard & the Lizard Wizard) [From GTAVI: Original Score]", "4HCOHRCSQelz2GPX8iTamO"), ("Genesis", "4wSmqFg31t6LsQWtzYAJob") } },
            { "Loukeman", new[] { ("Elktorn", "7eNm3jioKKGq2XF7qiTesW") } },
            { "Madeon", new[] { ("Finale (feat. Nicholas Petricca)", "4ATmY1hv93ehw77LrIdbEh") } },
            { "Metro Area", new[] { ("Miura", "7wu4CDoKfHqbaJCNO2qvza") } },
            { "Rob Gasser", new[] { ("Ricochet", "5ypVg9oa8Ud0uEP7P02FU1") } },
            { "Sonic Future", new[] { ("Waiting For", "0FRPckiF7FmkJHdHHkB3ag") } },
            { "Swedish House Mafia", new[] { ("Don't You Worry Child - Radio Edit", "2V65y3PX4DkRhy1djlxd9p") } },
            { "TAKIO", new[] { ("Extradimensional Cosmic Phenomenon (feat. tug)", "0NGKGQKrT6zsbXYA1dknty") } },
            { "Throttle", new[] { ("Next Big Thing", "3x0bV1xmHrE2bFmgNSwx0r") } },
            { "Vicetone", new[] { ("Heartbeat - Rameses B Remix", "5IIQC12W0XaBeBMeA0xYIO") } },
            { "Wuki", new[] { ("Ring of Fire", "3UoyCBnr5YJ4qKzHkrM2fj") } },
            { "Zero Eden", new[] { ("Anything Is Possible", "3iSUWGxK6RnevRuygavowY"), ("Between Binaries", "5ErytQsFAZzr0aMjrGSR9b"), ("Ocean Simulator", "6HJr7WeuEMDL2WQXtCvumS") } },
            { "deadmau5", new[] { ("Fabienk (deadmau5 remake)", "27VvbabSCwEpCiayUobIvM"), ("Jupiter", "4nFSITI81LYuC5TCeaEjDT"), ("Snowcone", "5hwvyCTN2mxly8ipXDLf4R") } },
        };
    }
}
