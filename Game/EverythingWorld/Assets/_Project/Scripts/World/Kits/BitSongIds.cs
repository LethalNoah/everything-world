using System.Collections.Generic;

namespace EverythingWorld.World
{
    /// The 8-Bit Isle's songs by artist (generated from scenes.json; part of BitKit), so every one of them can have
    /// its own light (BitKit.Song, BitKit.SongLamps).
    public partial class BitKit
    {
        static readonly Dictionary<string, (string title, string id)[]> SongIds = new Dictionary<string, (string, string)[]>
        {
            { "Arcade Player", new[] { ("Cave Dweller Concert (From \"Donkey Kong Country\")", "7kEj9inN9XoGT2HyOS7LK0") } },
            { "Big Giant Circles", new[] { ("The Trials of Man", "3fcnvhiSyt6kFnHV9Y4wHO") } },
            { "Blue Brew Music", new[] { ("Wii Shop Channel", "1XrturmnA9B2sJjTTXW47Q") } },
            { "Brad Knauber", new[] { ("Don't Mine at Night - Minecraft Parody", "68qjS7DsWSTfxuaKLcPc3j") } },
            { "C418", new[] { ("Living Mice", "5NkhjCiXwRdadCqbkZqdsj") } },
            { "Captainsparklez", new[] { ("Fallen Kingdom", "0gYnzvD4ktjOm8EWPN9vhh"), ("Revenge", "23IzQljXqkHXVn6rHTKWDI") } },
            { "ConcernedApe", new[] { ("Fall (Ghost Synth)", "5inqbGqSc2i7iBOrgOlIaA"), ("Spring (It's a Big World Outside)", "335Blw6tg6mJzQxK9dRbrt"), ("Summer (The Sun Can Bend an Orange Sky)", "2uKkYCiGW09gxyNFxX9HTj"), ("Winter (Nocturne of Ice)", "4ACs7pxDMiMbgBSC2xTdJS") } },
            { "Halo", new[] { ("Breaking the Covenant", "6enS8SfhigPF9H1iWamx2X") } },
            { "Jack Wall", new[] { ("Snakeskin Boots", "0Ia1kDjLTqorPZfe3rech9") } },
            { "Kenneth C M Young", new[] { ("The Gardens", "02vMtnwBaho6oxziL1LqFB") } },
            { "Lena Raine", new[] { ("First Steps", "4MsC3bu5B8WQGHQjOoH2NG") } },
            { "Martin O'Donnell", new[] { ("Ghosts of Reach", "1allV1HJZsFfJpjNfdJJe7"), ("Halo Theme Mjolnir Mix", "1SJ2ndZrIfBttZ9UfF3YS0") } },
            { "Nobuo Uematsu", new[] { ("Advent: One-Winged Angel", "0BAWoUYQ9YPIn5DuBRnbcE") } },
            { "SIE Sound Team", new[] { ("Hunter's Dream", "0514vhH4KBZmNHHVZWfmXD") } },
            { "Super Mario 64", new[] { ("Bob-Omb Battlefield (From \"Super Mario 64 GMB Cinematic Soundtrack\")", "1R8Ua61Dgq5ybjqwjtmn3Z") } },
            { "Toby Fox", new[] { ("MEGALOVANIA", "1J03Vp93ybKIxfzYI4YJtL") } },
            { "meganeko", new[] { ("Astro Soda", "0jJr8xz3ZZzNM9vq5kpMqf"), ("Lights Camera Action", "7AfpmKjp92jvcDrV6o7BNa"), ("Sharkatzor", "17l6ira7kdYxXveh5tZErG") } },
            { "she", new[] { ("Atomic", "4h5vc9YUxavCQotpiJ02H9") } },
        };
    }
}
