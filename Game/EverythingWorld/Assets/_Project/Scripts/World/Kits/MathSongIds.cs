using System.Collections.Generic;

namespace EverythingWorld.World
{
    /// The Math Rock Escarpment's songs by artist (generated from scenes.json; part of MathKit), so every one of them
    /// can have its own light (MathKit.Song, MathKit.SongChimes).
    public partial class MathKit
    {
        static readonly Dictionary<string, (string title, string id)[]> SongIds = new Dictionary<string, (string, string)[]>
        {
            { "65daysofstatic", new[] { ("Supermoon", "7h6GcTsaJpCWlvwW1PMUjA") } },
            { "American Football", new[] { ("Wake Her Up", "0DvVy1XSbcRatqCTxs4yWK") } },
            { "arai tasuku", new[] { ("Dear Alice", "69kPmtjz4qEkia1BBRYJ5e") } },
            { "Ben Rosett", new[] { ("Dark Energy", "4oFaYjcKSWaw86hmOHcY6R") } },
            { "Chinese Football", new[] { ("Electronic Girl", "3jLdAQ7guhHOppAj02RJmW") } },
            { "Chon", new[] { ("Bubble Dream", "0mnyjCNu9XUi5dv0JD3UqM") } },
            { "Clever Girl", new[] { ("Elm", "24dSu0ahW3wMgluLKjJmeW") } },
            { "Covet", new[] { ("shibuya (feat. San Holo)", "7BsPyswBtiM1PbPnUnuNzE") } },
            { "Do Make Say Think", new[] { ("The Landlord Is Dead", "6QXxmGsc0A2PG3alWT5bYi") } },
            { "Elephant Gym", new[] { ("春雨 Spring Rain", "5G1wOifWhpTlEhxbaQxQe5") } },
            { "Explosions In The Sky", new[] { ("Your Hand In Mine - Remastered", "4sYn64V9gqP6bpkOGw5B9k") } },
            { "Floral", new[] { ("Marsh Partition", "3NrmXanmUQKkbJEUQlgQUv") } },
            { "God Is An Astronaut", new[] { ("New Years End", "6HpchlfDUQzjRYYQCvTWUH") } },
            { "hitohira", new[] { ("That Scene", "7vX0nAoFY2eit8q60xEXXd") } },
            { "Ichika Nito", new[] { ("The Moon's Elbow", "25qXnyckAQv0djLzPQ6ECh") } },
            { "Infinity Shred", new[] { ("Long May You Run", "30g2zo9ZsQCwfiSWcW37zD") } },
            { "Just Neighbors", new[] { ("L I G H T", "7kWPhkahh5mFasCBzpP9pb") } },
            { "Lilac Stranger", new[] { ("Bubble Party!", "51FfLjzDMmuZsTxavl90Oa") } },
            { "LITE", new[] { ("Deep Inside", "1jDOObnZ18kkIqZAFIv1j1") } },
            { "Mogwai", new[] { ("Coolverine", "7a80O0Na5IqC7qt4GCB6IS") } },
            { "MONO", new[] { ("Ashes in the Snow - Remastered", "6ieH021IWLGOGifX0FUzsM"), ("Dream Odyssey", "1mNLJ6NjBPbWjmbbdMLCzf"), ("Everlasting Light - Remastered", "3XdNMSoRZdF6cM5pXpZCxQ"), ("Gerbera", "1nfSDVadgipoLHUFMed1v3"), ("Winter Daphne", "1BNYZ25JkOF7bUkJT5UWS2") } },
            { "saddyowner", new[] { ("Minecraft", "2hZNjkclkCJUFfOHTKYOKr") } },
            { "Saxon Shore", new[] { ("Replacement Driver", "6F3G2ll8jLAienikfbVbRD") } },
            { "sincerely", new[] { ("Glazey", "6TlZ3nVLUYynTdLPJl01Kb") } },
            { "Stage Kids", new[] { ("Cyniculture", "70bq0ZJVXu93cvGlluYrXu") } },
            { "Strawberry Girls", new[] { ("Gospel (feat. Joey Lancaster)", "7CLnWceuoyZrfXCIWQalUJ") } },
            { "The Physics House Band", new[] { ("Teratology", "5xF88xyWRwEMVq3waLRcah") } },
            { "This Will Destroy You", new[] { ("I Believe in Your Victory", "5LaatZmLycE71jtbQNgpM3"), ("Quiet", "4llDF3RuBQYj2nkjTCJY4w") } },
            { "toe", new[] { ("two moons", "6qcUFIPZU2sJXiI1sqzwZG"), ("エソテリック", "2CnIwnCbIAraVqX5SkvptE"), ("サニーボーイ・ラプソディ", "40cjVqOXZrz03kVt2lNI0y") } },
            { "Totorro", new[] { ("Chevalier Bulltoe", "6SBxee2sZTBLCfNZYz0lww") } },
            { "tricot", new[] { ("18,19", "3x8V1Brh7ODYQsRwCup6hx"), ("artsick", "7Jzw6ZqrFefr5KlrQzgGJr"), ("potage", "6n9AvpTLSNunpIr2Gr2AXa"), ("混ぜるな危険", "3WWslYheowgYhLk1wrW0T0") } },
            { "TTNG", new[] { ("Baboon", "4mgS9q5U0a8sh42tp3UgMy") } },
            { "Via Luna", new[] { ("Infinite Spice", "40hF24KMxg9LpglA9We3As") } },
        };
    }
}
