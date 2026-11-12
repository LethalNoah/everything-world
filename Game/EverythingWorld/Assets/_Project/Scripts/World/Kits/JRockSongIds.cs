using System.Collections.Generic;

namespace EverythingWorld.World
{
    /// The J-Rock Isles' songs by artist (generated from scenes.json; part of JRockKit), so every one of them
    /// can have its own light (JRockKit.Song, JRockKit.SongLanterns).
    public partial class JRockKit
    {
        static readonly Dictionary<string, (string title, string id)[]> SongIds = new Dictionary<string, (string, string)[]>
        {
            { "Ado", new[] { ("Aishite Aishite Aishite", "4gpvY7b9wHA9G1ZsMDu15b"), ("New Genesis", "6A8NfypDHuwiLWbo4a1yca") } },
            { "ASIAN KUNG-FU GENERATION", new[] { ("遥か彼方", "5ORPYXJKlpHWIdceavSGrL") } },
            { "Cyntia", new[] { ("閃光ストリングス", "5dOOn0zgnkzhbyV3FkrQEk") } },
            { "DEATHNYANN", new[] { ("illumina", "3meAQqsXMmGfghnqLhMgI3"), ("tarantula", "6l0yHMEt8XAlnpGIU0VjrK") } },
            { "Feryquitous", new[] { ("Monochrome Anomaly (feat. F9)", "1SAJNDJO9s3oEi10CItFsU") } },
            { "FLOW", new[] { ("COLORS", "16pAi17I31YI5blYUcM0lz") } },
            { "GARNiDELiA", new[] { ("grilletto", "5XNp2U3aDIx0j7MZuUa7oz") } },
            { "Hoshikuma Minami", new[] { ("CRAVE", "5JHheC8X4EFsPmYhoZQn9f"), ("HOLLOW EYES", "1HiXy7BSM1Uxs0jcBTlAbQ"), ("NIRVANA", "7MpkwGg4jXx1LnTc637uIC"), ("OXYGEN", "1OX2njCNPrDHzG1UdG0KDb") } },
            { "King Gnu", new[] { ("SPECIALZ", "5oQpH1uuZte4axR411rIlN") } },
            { "Ling tosite sigure", new[] { ("abnormalize", "00nmZvZRyzQiYe2tAcDDUb"), ("Enigmatic Feeling", "0YHTrd78qCsMhzmgBHiZ28") } },
            { "Mai Yamane", new[] { ("The Real Folk Blues", "5Cmf3LmbLd9g79rS55X7qK") } },
            { "MindaRyn", new[] { ("SURVIVE", "3hm6SR9VEMgNuNKfH9lTAv") } },
            { "Mitsukiyo", new[] { ("Irodori Canvas - Korean Version", "2Z6DzhXc6KqnaqeHwU6Y95") } },
            { "nomico", new[] { ("Bad Apple!!", "7pasphkxAGCk8VSpwrLAxe") } },
            { "Roselia", new[] { ("FIRE BIRD", "0qMpU4VmLf6aMNc7CUErzV") } },
            { "Sithu Aye", new[] { ("It's the Second Season, So We Need a New Character After All!", "4cBpVXgq5qE4LlUU2Yf3wr"), ("Skye", "04hupcfZvLWlEveLa8IyqC"), ("The Noticing!", "2srPAO5mxuiswYTci5WUrB") } },
            { "Supercar", new[] { ("YUMEGIWA LAST BOY", "5NnqtgN9MzZ7TI9Rrxi4tw") } },
            { "Takashi Ohmama", new[] { ("Cockpit", "3JiYc70dMBSZhJJgR22sjj") } },
            { "technoplanet", new[] { ("Megastructure", "47JrhvvH4AFUJIk857xwqj") } },
            { "Yorushika", new[] { ("ただ君に晴れ", "3wJHCry960drNlAUGrJLmz"), ("だから僕は音楽を辞めた", "5j7ixaLeGTGSv4DzKs0pCM") } },
            { "天狼群", new[] { ("BAD SURPRISE", "0YoL1XsArVSrlEHeXjYPv9") } },
            { "松澤由美", new[] { ("Kuusou mesorogii", "7Fkgp2iEhCZW1gRen41H8p") } },
            { "楽音", new[] { ("mosi mosi? LEESOL (KiiiKiii) Remix", "5vepxhsGuqu2QtXX6pbMkz") } },
        };
    }
}
