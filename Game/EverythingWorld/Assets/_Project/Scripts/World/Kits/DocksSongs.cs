using UnityEngine;

namespace EverythingWorld.World
{
    /// The Docklands' songs (part of DocksKit): every one of the 39 has its own light that brightens while it plays
    /// (DocksLife ramps it), and a few change the whole harbour:
    /// - "There Is a Light That Never Goes Out": every street lamp in the docks goes out, leaving the night black but for
    ///   the one lit window in the tower, the lighthouse and the car's headlights, which blaze.
    /// - "How Soon Is Now?": the sodium lamps throb in a slow tremolo.
    /// - "99 Red Balloons": ninety-nine red balloons rise slowly from the harbour into the night.
    /// - "Everybody Wants To Rule The World": searchlights from the cranes sweep the sky.
    /// - "The Sprawl": every window in every tower lights up. "Golden Brown": the night turns gold.
    /// - "Pink Steam": pink steam pours from the grates. "Thunder": lightning strikes Maruja's rod.
    public partial class DocksKit
    {
        public const string TakeOnMe = "2WfaOiMkCvy7F5fcp2zZ8L", ChaosSpaceMarine = "2UEH1NjNHGsoEIr3GKLhNR", Sunglasses = "2LgZuHzjrsSLvuHyt7eyLH", SoreEyes = "7hMygpq9zdZUntsQ4HFBvO",
            ComeOnEileen = "0EMmVUYs9ZZRHtlADB88uz", RomanHoliday = "4GKJALTXTwhZgLPp0usd5C", Sail = "5s4UUYyaJjYnR9ebX1JYm7", LongIslandCity = "1oevydxCBM8J6aGHKymgl1",
            Schmelztiegel = "3Q821r222nNbE4gaeLAvbe", Disorder = "5fbQCQt91LsVgXusFS0CCD", OneStepBeyond = "1G6eFFDRaLr9EbThnhzMBD", OurHouse = "1qV6e4ITnfGmA6m6Vklvbi",
            Rage = "0DnADspu1dcqImqpb8byQv", Saoirse = "3g0xd9m1MxKRiIC2Xw4998", Thunder = "376Np9VAQXL4OgvOeNMXSj", CandyGun = "3CJT3NY7LLs3dSwm2C59vC",
            ShieldForYourEyes = "5jxKCf7EPQCoiMgD5Fzjlq", Bangkok = "6erBowZaW6Ur3vNOWhS2zM", RedBalloons = "7p8HVe22aGW1XtO1hoDHGo", SoundsWeCannotHear = "2WU8SZDNivaj43lDPZqPFX",
            Emily = "7zEanUDqPrxM0RDxKxOq1v", KoolThing = "1ZozGivTAYsOwhy6LVHsPX", PinkSteam = "6RHy5vb8iLqqa2LmIjxxYK", TheSprawl = "6R0dQ5mWpzygCXhkObmNQ9",
            Crosseyed = "0yhwdmbgkKdE1plV8xWdrd", RuleTheWorld = "4RvWPyQ5RL0ao9LPZeSouE", Lullaby = "7oNg2UTfQIs0IafmyzRc0g", PicturesOfYou = "1tuwC1Ob5vnYZhvg3zyzJg",
            EndOfTheWorld = "0FMAfHeqwXjJPy0mcCm6ur", Vacation = "0oc7YoWV783fGr2GbdcIrz", Roxanne = "3EYOJ48Et32uATr9ZmLnAo", ZeroSum = "6bjpmghStgfcmTsqsG6Yb6",
            Asleep = "7LZgdL0MxiElfaKZbuuE4l", HalfAPerson = "3ccAZx3wLZDVB2Nq6rikf9", HowSoonIsNow = "1YrnDTqvcnUKxAIeXyaEmU", IKnowItsOver = "3M2bD9SMYnJIPdrTKUnBd3",
            PrettyGirls = "3wUMcPzXcmaeW8QxTdyXQO", LightNeverGoesOut = "0WQiDwKJclirSYG9v5tayI", GoldenBrown = "2AX5E86cn9n2dgioZEjirI";

        public override (string song, string label, string artist)[] SpecialSongs => new[]
        {
            (LightNeverGoesOut, "There Is a Light That Never Goes Out", "@the tower with one lit window"), (RedBalloons, "99 Red Balloons", "Nena"),
            (RuleTheWorld, "Everybody Wants To Rule The World", "@the cranes and the ship"), (TheSprawl, "The Sprawl", "@the tower with one lit window"),
            (HowSoonIsNow, "How Soon Is Now?", "The Smiths"), (GoldenBrown, "Golden Brown", "@the square"), (PinkSteam, "Pink Steam", "Sonic Youth"), (Lullaby, "Lullaby", "The Cure"),
        };

        /// A glow that brightens while its song plays.
        Material SongGlow(string song, Color c, float s = 1.6f)
        {
            var m = new Material(GlowM(c, s)) { name = "Song glow" }; life.songGlows.Add((song, m, c * (s * .65f))); return m;
        }
    }
}
