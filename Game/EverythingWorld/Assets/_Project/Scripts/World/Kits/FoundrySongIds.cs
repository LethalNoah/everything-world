using System.Collections.Generic;

namespace EverythingWorld.World
{
    /// The Electro Foundry's songs by artist (generated from scenes.json; part of FoundryKit), so every one of them can have
    /// its own light (FoundryKit.Song, FoundryKit.SongLamps).
    public partial class FoundryKit
    {
        static readonly Dictionary<string, (string title, string id)[]> SongIds = new Dictionary<string, (string, string)[]>
        {
            { "Bioblitz", new[] { ("Globe Warmer - Farleon Remix", "6YkQxEwK0y75YYTAep5Sya") } },
            { "Dave Era", new[] { ("Dave Era- Infinite Journeys", "0lFIf9iq6olVx5ZmQaoPDX") } },
            { "Farleon", new[] { ("Apollo - Farleon Remix", "10zVQaJqtOFDjJjTvjHXzl"), ("Far From Home - Farleon Remix", "56GQ6SU2PF9LCIDW16VkwN"), ("Super Game", "4WNxeNTjCSpWyf34gj6Ira") } },
            { "Insan3lik3", new[] { ("Bad Pitched", "5tN5NpSgrHnsIEW25rZhEG"), ("The Other Day", "3xrdlyNFDCtgbuYyaWxZyW"), ("You Make Me Go - Original Mix", "177TmGyY9GfeIusmJRNHYs") } },
            { "James Egbert", new[] { ("Halo - Original Mix", "6fLHZMRqV9XOPZ4JPrslZn") } },
            { "Manteeny", new[] { ("One Two - Original Mix", "4VIQi69hoxd3peZtXl9SJ8") } },
            { "Mord Fustang", new[] { ("Drivel", "5vu8Na7XZcwoOmnO8mB55z") } },
            { "Rabbit Killer", new[] { ("Imaginationland", "3hOiSgV0fkoUJeJGAp0FIY") } },
            { "Ryeland", new[] { ("Luvu - Original Mix", "0bUbXudPzqKhX0BZMcygRQ") } },
            { "meganeko", new[] { ("Astro Soda", "0jJr8xz3ZZzNM9vq5kpMqf"), ("Lights Camera Action", "7AfpmKjp92jvcDrV6o7BNa"), ("Sharkatzor", "17l6ira7kdYxXveh5tZErG") } },
            { "she", new[] { ("Atomic", "4h5vc9YUxavCQotpiJ02H9") } },
        };
    }
}
