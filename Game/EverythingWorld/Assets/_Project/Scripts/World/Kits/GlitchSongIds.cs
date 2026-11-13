using System.Collections.Generic;

namespace EverythingWorld.World
{
    /// The IDM Glitchlands's songs by artist (generated from scenes.json; part of GlitchKit), so every one of them can have
    /// its own light (GlitchKit.Song, GlitchKit.SongLamps).
    public partial class GlitchKit
    {
        static readonly Dictionary<string, (string title, string id)[]> SongIds = new Dictionary<string, (string, string)[]>
        {
            { "Ametsub", new[] { ("Snowy Lava", "0GUZTiJprOf8OMtrVmjTyD") } },
            { "Autechre", new[] { ("Bike", "3Om7xxNAn64xJOuU9WVKtp"), ("Eutow", "4uRXAyNL5T4KY7iawdfJBZ") } },
            { "Circuit Bent", new[] { ("Tonic - Mr. Bill Remix", "6t0HEbZyoEM3ljcXtgi2Wl") } },
            { "Iglooghost", new[] { ("New Vectors", "2fB9W8v12kKC2dbin3AV45"), ("Xiangjiao", "52FXxuT9ZrXVML5Ar0WRG7") } },
            { "Igorrr", new[] { ("Downgrade Desert", "79xLxZrpdURjzJmdHKB85a") } },
            { "Kyegi", new[] { ("Tl.return", "6XWBI8dIAIcPVLDgOOoWDh") } },
            { "Lorn", new[] { ("Acid Rain", "7pM9cLbrgRGX1hHElTX92m") } },
            { "Memo Boy", new[] { ("Facebook.com", "1d0Ol4OODB5ioYsds2TtJC") } },
            { "Mr. Bill", new[] { ("Caffeine", "32X8nRLyRrQZKW10BKXPQB"), ("Composite 12", "7sa2ly0ErkBSSPSBdp2r1K"), ("Corot-7b", "6TyCyldftxRxuWeXlIAA4F"), ("Crowd Sourced Data", "1QChxDKGnTgXR3DGJVuUVC"), ("Cryptomnesia", "7KRSnwBngnUGmOd3nyGiU9"), ("Ejecta", "0NSIeaa8SDSbm1LACOsGh7"), ("IDM Anthem", "2gYqdEs14iQc2cDpF94zfh"), ("IDM Anthem", "5Jj2O8ue89PAcV4ZA8Upb8"), ("Jeetvak", "0au2oo0RJitUOERgjYAP4P"), ("Ladder Anxiety", "3qZ6uM5qJi2VKtDMQDk4Ou"), ("Pish Posh", "6oeNF7ufnpxbxlt75jgwGU"), ("Pleasure Seeker", "0inAo4hdWILQX1EbvMSseA"), ("Traveler", "2BZ3ME7nsNK468gfNC48PP") } },
            { "Squarepusher", new[] { ("My Red Hot Car", "0r9NLcp6718EYibVX6diWN"), ("Papalon", "2fgqJCAFExZZPCPm7BRUP7") } },
            { "Tim Koch", new[] { ("Ik Zak Fout", "5ZSw2IWAHqkaiZE7bcCNX3") } },
            { "Venetian Snares", new[] { ("Öngyilkos Vasárnap", "5PdgFJLNp2H4mM2eg9blX2") } },
            { "Woulg", new[] { ("The Water", "6MrnWgi1oYYPkOOUXkoSu5") } },
            { "Zebbler Encanti Experience", new[] { ("Flesh Out", "3znSOpNOHt6RwUbg4Y9sKx") } },
            { "diaphane", new[] { ("Rhizomes", "68rduHI9BlGnAzglpDuWEX") } },
        };
    }
}
