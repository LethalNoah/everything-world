using System.Collections.Generic;

namespace EverythingWorld.World
{
    /// The Glitterlands's songs by artist (generated from scenes.json; part of GlitKit), so every one of them can have
    /// its own light (GlitKit.Song, GlitKit.SongLamps).
    public partial class GlitKit
    {
        static readonly Dictionary<string, (string title, string id)[]> SongIds = new Dictionary<string, (string, string)[]>
        {
            { "ADÉLA", new[] { ("Nicole Kidman", "70cHKK8bHAfJrOGVnfRG9J") } },
            { "Ace of Base", new[] { ("Happy Nation", "7yCF65vpc7sUwPjftI0iFb") } },
            { "Audrey Hobert", new[] { ("Phoebe", "6tthJ4M25QGZiCIG2np8tr"), ("Sue me", "6ZAuQOgLrNQb9s7BXheuTy") } },
            { "Ava Max", new[] { ("Sweet but Psycho", "7DnAm9FOTWE3cUvso43HhI") } },
            { "Caramella Girls", new[] { ("Caramelldansen", "7MwwPyZJ7UKFROj2oVnH6R") } },
            { "Carly Rae Jepsen", new[] { ("Don’t Leave Me on the Dance Floor", "34XRbhIQAaZnZWKjEaFtBd"), ("On Wires", "1vrzdK4WwGuDBYNqtFpHog"), ("Run Away With Me", "3rLLDjCJ9JllYvwUKfd3vW") } },
            { "Chappell Roan", new[] { ("Red Wine Supernova", "7FOgcfdz9Nx5V9lCNXdBYv") } },
            { "Charli xcx", new[] { ("Guess featuring billie eilish", "0IsIY8pfu1yaGkPUD7pkDx"), ("party 4 u", "2RdEC8Ff83WkX7kDVCHseE") } },
            { "Cryalot", new[] { ("Labyrinth", "3ik9VEtwDTgsCLN8NpqAtt"), ("See You Again", "032AdL868w0ztLqyZ7K48w") } },
            { "Haddaway", new[] { ("What Is Love - 7\" Mix", "2IHaGyfxNoFPLJnaEg4GTs") } },
            { "Kero Kero Bonito", new[] { ("The Princess and the Clock", "2zSFmjJR2PCE1eXGWcMiMI") } },
            { "Kim Petras", new[] { ("Coconuts", "71yN0yrHej3jhKXewbmtEh") } },
            { "Lorde", new[] { ("Supercut", "6K8VQ84MqhsoakN5MjrnVR") } },
            { "Magdalena Bay", new[] { ("Image", "4O45RxFmWr88UfFlNtsXUy"), ("Mercurial World", "30YK7cZlruEW0Clyxwk2nH") } },
            { "O-Zone", new[] { ("Dragostea din tei", "52Zr0sinhWTCRrQhRRY4EP") } },
            { "Owl City", new[] { ("Good Time", "1kPpge9JDLpcj15qgrPbYX") } },
            { "Ruelle", new[] { ("Slow Motion Memory Fade", "1bjNcyllLjeEjXd2yUXZQo") } },
            { "Stepford Wives", new[] { ("After Red 40", "2xiCC6Dy8FeWWz7Z98lxSO") } },
            { "Tove Lo", new[] { ("des fleurs x stromae", "0lNSoCSUfvKSmjm8B7rZYI") } },
            { "Vylet Pony", new[] { ("Creature City", "0fZyOCGrzIjEJAJo352wuJ") } },
        };
    }
}
