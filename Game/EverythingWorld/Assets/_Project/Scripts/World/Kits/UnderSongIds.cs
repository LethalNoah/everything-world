using System.Collections.Generic;

namespace EverythingWorld.World
{
    /// The Trip-Hop Undercity's songs by artist (generated from scenes.json; part of UnderKit), so every one of them can have
    /// its own light (UnderKit.Song, UnderKit.SongLamps).
    public partial class UnderKit
    {
        static readonly Dictionary<string, (string title, string id)[]> SongIds = new Dictionary<string, (string, string)[]>
        {
            { "Amon Tobin", new[] { ("Slowly", "15J6PrzKaLtvw0dBr9sq72") } },
            { "Blue Foundation", new[] { ("Eyes on Fire - Zeds Dead Remix", "3Oiauiojgokw9vWQvFmEoI") } },
            { "Glass Animals", new[] { ("Heat Waves", "3USxtqRwSYz57Ewm6wWRMp") } },
            { "Hidden Orchestra", new[] { ("Overture", "1oZvvVAYF9RRzlVTD2W8eX") } },
            { "Hooverphonic", new[] { ("Eden", "5IpTvvTopPoAhJLbtQ798i"), ("Mad About You", "5drK2kTE2mrUdV33iHWyrx") } },
            { "Massive Attack", new[] { ("Angel", "7uv632EkfwYhXoqf8rhYrg"), ("Teardrop", "67Hna13dNDkZvBpTXRIaOJ") } },
            { "Moby", new[] { ("Porcelain", "5fJAYWoFgXm26HLNViFiUh") } },
            { "Moth Equals", new[] { ("Jeunesse", "6Nle9TgEz07IaOgOv4pgDG") } },
            { "Portishead", new[] { ("Glory Box", "3Ty7OTBNSigGEpeW2PqcsC"), ("Roads", "2sW8fmnISifQTRgnRrQTYW"), ("Sour Times", "6vTtCOimcPs5H1Jr9d0Aep") } },
            { "Rob Dougan", new[] { ("Clubbed to Death - Kurayamino Variation", "0vSVDF2fHaxL2L9eEmRdAK") } },
            { "Silent Poets", new[] { ("Hope", "3LaXXL9ddQGMtI6Bs61kW8") } },
            { "il:lo", new[] { ("Reine", "4HCjylaAs1gZHIyls8PhjJ") } },
        };
    }
}
