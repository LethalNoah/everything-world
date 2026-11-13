using System.Collections.Generic;

namespace EverythingWorld.World
{
    /// The Melodic Bass Highlands's songs by artist (generated from scenes.json; part of HighKit), so every one of them can have
    /// its own light (HighKit.Song, HighKit.SongLamps).
    public partial class HighKit
    {
        static readonly Dictionary<string, (string title, string id)[]> SongIds = new Dictionary<string, (string, string)[]>
        {
            { "Adventure Club", new[] { ("Retro City", "31R8eTyaTxfuhcdOi5r9q8") } },
            { "Dabin", new[] { ("Bloom", "0bey95Gcplo3PsvEubZjv1") } },
            { "Direct", new[] { ("Hysteria", "3hHkRLrUkQMPOdg62rR625") } },
            { "Diveo", new[] { ("Summer Trees", "0kCsANasMTT4VF3SX0XtUN") } },
            { "Foxes", new[] { ("Youth - Adventure Club Remix", "46F2qArLwz8WT1qJ2bNRfR") } },
            { "Mr FijiWiji", new[] { ("Cynical", "2YiM47FxYboCbE3BL5wHyj"), ("Gina", "77z0nbNyLJKb0qA5hMLDIC"), ("Insomnia", "5MsEcECw0TFwSJzm7uzD7o"), ("The Mentalist", "7DNzzksJEjF02iI6Tzee0H"), ("Time to Say Goodbye", "2JfgVUugCL9YPPuC1eGq0U") } },
            { "Noisestorm", new[] { ("Crab Rave", "4qDHt2ClApBBzDAvhNGWFd") } },
            { "Pixel Terror", new[] { ("Enigma", "0X5L64QO8VX9nUgI7Evti9") } },
            { "Pure 100%", new[] { ("BREATH", "4HsFiZr0inQmoz9rReElFV") } },
            { "Rogue", new[] { ("Dreams", "2XGt9OgW0zu2X55NXnmjTa"), ("Nightfall", "02JPJITCSCp4Y9F42HjbLo") } },
            { "Seven Lions", new[] { ("Creation", "4vfdACbOXF6IXIPrH2u5dI"), ("Days to Come", "1Db1TsTyBKq4yZcXbVD9Qq"), ("Days to Come (feat. Fiora)", "1FRFgyPNuqfXIRYlCJ1kwR"), ("Every Time", "3H1Z6BKmFwQN8PX7yapaIR"), ("Nepenthe", "1q3E9rGb23QYUVVef8dCKz"), ("Rush Over Me (feat. HALIENE) - Acoustic", "5IuajfaENB9U4nbbXiXm5L"), ("The Fall", "21DJnuowF67gBqyu7iAH9H"), ("Tyven", "3UHKHqKaKp75T1Qmcpb3BQ"), ("Worlds Apart (feat. Kerli)", "57CFIKmALHxvzvMRsK8Kgp") } },
            { "Skrux", new[] { ("Essence", "03OeNAtCG0dOi8BnY7LBOs") } },
            { "Sound Remedy", new[] { ("Medicine", "33dV4CarQ8IOwZMJqZAFpj") } },
            { "Vexare", new[] { ("Rattlesnakes", "49ANENVxFaSzb8PLX6i4pk"), ("The Clockmaker", "6vfc9S8PEstfRvXwTqDd8q") } },
            { "Xilent", new[] { ("Animation", "42IXJMPjUePygZBXS8VPeH") } },
        };
    }
}
