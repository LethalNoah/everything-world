using System.Collections.Generic;

namespace EverythingWorld.World
{
    /// The Dubstep Shards's songs by artist (generated from scenes.json; part of ShardKit), so every one of them can have
    /// its own light (ShardKit.Song, ShardKit.SongLamps).
    public partial class ShardKit
    {
        static readonly Dictionary<string, (string title, string id)[]> SongIds = new Dictionary<string, (string, string)[]>
        {
            { "Bassnectar", new[] { ("Bass Head", "7Em0GU0uX7BzhIzy7V5D9I"), ("Timestretch", "2O2gPss8XE08fsodduWsvt"), ("Underground Communication", "6NJjPvoQvLdjTPR0aZV9oT"), ("Wildstyle Method", "0pkDaFdpiTf3YXevNwGJZ2") } },
            { "Black Tiger Sex Machine", new[] { ("Destroy It - Urban Contact Remix", "2u0SSpdM9XNfzWAwoJWxzF") } },
            { "Careless Castle", new[] { ("Echo", "6YMxb6QoJcLlDaoVbDZsbc") } },
            { "Eccentric", new[] { ("funk of the week", "6a53HroJufw9yjFfc9q6jy") } },
            { "Freestylers", new[] { ("Cracks - Flux Pavilion Remix", "2grynUHH0A3faPhGjdkjeJ") } },
            { "NGHTMRE", new[] { ("Teardrop", "7ztFTiaEtc1TF5AV9909Bb") } },
            { "Of The Trees", new[] { ("Dolori", "3ACgRmJStyjPbGyOeirKrA"), ("Windhorse", "6tTDDMmEJpyA97VNYQjjeP") } },
            { "Omnitica", new[] { ("Yo! Ho! And a Bottle of Bass!", "4wENG4FhLIjqYqIUF1VlNj") } },
            { "Panda Eyes", new[] { ("Colorblind", "1HqlcFPHc4INT94rVbLIFU"), ("Highscore", "7FADpmBJVWay6aHy8q34oa") } },
            { "Prismatic", new[] { ("Minty - Au5 & Fractal (USA) Remix", "1bBYEtxzg9eHxUmKt5UOK5") } },
            { "Quartzone", new[] { ("Supersymmetry II", "6cboMDvPReaCw96Gog1rZU") } },
            { "Skrillex", new[] { ("Scary Monsters and Nice Sprites", "4rwpZEcnalkuhPyGkEdhu0"), ("Summit (feat. Ellie Goulding)", "1SbqxemPdwGlvmirtQVkCU") } },
            { "Skybreak", new[] { ("Into The Light - driftcat Remix", "4kM8VUW2FTPEQDnjvYm0uo") } },
            { "Subtronics", new[] { ("Spacetime - Virtual Riot Remix", "3xZPlShJoPZbY0AgTPmBzD") } },
            { "Teminite", new[] { ("Accelerate", "61kmDZXjXGBWh8YnWjFCQY") } },
            { "Virtual Riot", new[] { ("Blackout", "0QSdRZkHrfbFbZIxFF1EIM"), ("CHROMA", "4bjHtvWwIVu7b6Acj1QaaV"), ("Different World", "4rKtTUqoItgjsr1aC82Wtv"), ("Edge", "5XCLRmmYZBCNkV8a0uBD6K"), ("Lunar", "3KdAUFpiqkuOJUmCfbjlV9"), ("Neon Angel", "2JDlRPMO9WUZonTCm8M3Tn"), ("Rewrite", "0Kd54oKcbAOd7e3pf4vwsO"), ("Stargarden", "4XSe2GELxdO3CLES4fsZdg") } },
            { "ZEOL", new[] { ("Iridescent", "2fKGBZxV6vX5SCYCOYsp1z") } },
        };
    }
}
