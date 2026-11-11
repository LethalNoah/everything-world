using UnityEngine;

namespace EverythingWorld.World
{
    /// The Grunge Rainwood's songs (part of GrungeKit): every one of the 40 has its own light somewhere in its artist's
    /// area that brightens while it plays (GrungeLife ramps it), and a few set off something bigger:
    /// - "Black Hole Sun": the rain stops, the sky goes dark amber, and a black sun with a burning rim rises over the falls.
    /// - "Lightning Crashes": lightning strikes again and again, often on the rod over Live's farmhouse.
    /// - "No Rain": the rain stops, the clouds thin and warm gold light breaks through.
    /// - "Say Hello 2 Heaven": a pillar of light rises from the little temple into the clouds.
    /// - "Disarm": the bells in the Pumpkins' bell frame swing and glow. "Even Flow": the river and the falls brighten.
    public partial class GrungeKit
    {
        public const string Nutshell = "2JuasWPUodaUxf5nwNpciQ", Would = "5sFDReWLrZHLFZFjHsjUTS",
            DoesntRemind = "0o995gJCGebaza0LWybToU", Highway = "3bwAS8cN7Xtzvv5UurJOOX", LikeAStone = "3YuaBvuZqcwN3CEAyyoaei",
            NoRain = "6txWz9UapYHVxEd7dDIHXT", EverythingZen = "5l2DXYLyBVZWhdpgqa3a9k", Machinehead = "1wrhhPzd2ncJPNgUcGgBGg",
            FeelThePain = "6xUMt58RCX98m7g0KcMwAS", Everlong = "5UWwZ5lm5PKu6eKsHAGxOk", MyHero = "4dVbhS6OiYvFikshyaQaCN", SeanSong = "1T8ZHOyYzHFd0t4LvdIHfz",
            ThatsMe = "0ivNXNBIslaG7LeLfFaQ0T", AndILoveHer = "7x4b0UccXSKBWxWmjcrG2T",
            AllOverYou = "0sqE6XPi9VAaWl5NxDyMQK", IAlone = "3LpnzPxkMI6XS4JCbhNeek", LightningCrashes = "48iWbsOaBUAGzMdoSmqyg1",
            HoneyBucket = "5oABn2E9jiDrmhDdiNWbKS", HeartShapedBox = "11LmqTE2naFULdEP94AUBa", Lithium = "4ZJz3TASJ0TA9vF1KrZvUz", WhereDidYouSleep = "551qy5vUgrUfEUc4dCNfht",
            EvenFlow = "6QewNVIDKdSl8Y3ycuHIei", Jeremy = "62nQ8UZVqR2RMvkJHkcO2o", BackToStrangers = "2GpNjyQO67Ss38PmPaL6uA",
            Asylum = "7w0mvl5gWx92K4E2zGVlcn", Freak = "2RDgs05sg2vrpwiAEUkWd0",
            BlackHoleSun = "2EoOZnxNgtmZaD8uUmz2nD", BurdenInMyHand = "1iArQTuOzxvrtniGmkyy92", PrettyNoose = "6ZzeXWwDvMzrgCWt9RwBj1", Spoonman = "1jMaB19DiVR8OihLSuYFOt",
            BigEmpty = "2E6elGQS3KaKR7QdfzQXdP", InterstateLoveSong = "6qLEOZvf5gI7kWE63JE7p3", PaperHeart = "2gdtLnVGGg80Kj9GiqP0vH",
            YoungestDaughter = "3tYxhPqkioZEV5el3DJxLQ", SayHello2Heaven = "4L2S9GbwEgwfSbHjTCvNQX",
            CherubRock = "3ow0TQVttXQF8rLckmXgRx", Disarm = "6YIggUJW3ttAAPRdnki8RM", Empires = "6W2PWC5H7FSG3fdWc70EgZ", Mayonaise = "0jmKzJmUEKNbC7eU8YfOiA", Zero = "4YFcGTdgmEuw8xTO4XrxbB";

        public override (string song, string label, string artist)[] SpecialSongs => new[]
        {
            (BlackHoleSun, "Black Hole Sun", "@the falls"), (LightningCrashes, "Lightning Crashes", "Live"), (NoRain, "No Rain", "Blind Melon"),
            (SayHello2Heaven, "Say Hello 2 Heaven", "Temple Of The Dog"), (Disarm, "Disarm", "The Smashing Pumpkins"), (EvenFlow, "Even Flow", "@the falls"),
            (LikeAStone, "Like a Stone", "Audioslave"), (InterstateLoveSong, "Interstate Love Song", "Stone Temple Pilots"),
        };

        /// A glow that brightens while its song plays.
        Material SongGlow(string song, Color c, float s = 1.6f)
        {
            var m = new Material(GlowM(c, s)) { name = "Song glow" }; life.songGlows.Add((song, m, c * (s * .65f))); return m;
        }
    }
}
