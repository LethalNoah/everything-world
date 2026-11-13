using System.Collections.Generic;

namespace EverythingWorld.World
{
    /// The Pop Republic's songs by artist (generated from scenes.json; part of PopKit), so every one of them can have
    /// its own light (PopKit.Song, PopKit.SongLamps).
    public partial class PopKit
    {
        static readonly Dictionary<string, (string title, string id)[]> SongIds = new Dictionary<string, (string, string)[]>
        {
            { "Adele", new[] { ("Skyfall", "6VObnIkLVruX4UVyxWhlqm") } },
            { "Alex Warren", new[] { ("FEVER DREAM", "7ogpGbKc4Io6NCU6ydxXwZ") } },
            { "B.J. Thomas", new[] { ("Most Of All", "4T1TSoRaDPP0UxEYk7guXb") } },
            { "BLACKPINK", new[] { ("DDU-DU DDU-DU", "4lQsB3ERTWSNaAN1IkuNRl") } },
            { "BTS", new[] { ("Dynamite", "5QDLhrAOJJdNAmCTJ8xMyW"), ("Spring Day", "2j1fFjWHCI9KJSwcuYAOyF") } },
            { "Bebe Rexha", new[] { ("One Day", "3xG0S7f83zPliFIy5QTF5R") } },
            { "Christina Perri", new[] { ("a thousand years", "6lanRgr6wXibZr8KgzXxBl") } },
            { "Dido", new[] { ("White Flag", "3adnLFXKO5rC1lhUNSeg3N") } },
            { "Ellie Goulding", new[] { ("Lights", "11ozIUBoXAgRTVWelDn4pL"), ("Lights - Bassnectar Remix - Bonus Track", "4qxUFzBKlmP98kIRKFszju") } },
            { "GAYLE", new[] { ("abcdefu", "4fouWK6XVHhzl78KzQ1UjL") } },
            { "IVE", new[] { ("LOVE DIVE", "0Q5VnK2DYzRyfqQRJuUtvi") } },
            { "Imagine Dragons", new[] { ("Demons", "3LlAyCYU26dvFZBDUIMb7a"), ("Radioactive", "4G8gkOterJn0Ywt6uhqbhp") } },
            { "Isabel LaRosa", new[] { ("Hallucination", "3Fcnn90rR3xPYf1yluDyhu") } },
            { "Katy Perry", new[] { ("Dark Horse", "4jbmgIyjGoXjY01XxatOx6"), ("Firework", "4r6eNCsrZnQWJzzvFh4nlg"), ("I Kissed A Girl", "005lwxGU1tms6HGELIcUv9"), ("Last Friday Night (T.G.I.F.)", "3avYqdwHKEq8beXbeWCKqJ"), ("Roar", "27tNWlhdAryQY04Gb2ZhUI") } },
            { "Kristine Blond", new[] { ("Love Shy - Tuff Jam Dub Mix", "31tuwNVYbXIRTGTXdjgSgM") } },
            { "Lady Gaga", new[] { ("Just Dance", "2x7MyWybabEz6Y6wvHuwGE") } },
            { "Michael Jackson", new[] { ("Beat It", "1OOtq8tRnDM8kG2gqUPjAj"), ("Billie Jean", "5ChkMS8OtdzJeqyybCc9R5"), ("Thriller", "3S2R0EVwBSAVMd5UMgKTL0") } },
            { "Miley Cyrus", new[] { ("7 Things - Single Version", "5ecZWU5uQOiCVSnPxBZNmT") } },
            { "NewJeans", new[] { ("Super Shy", "5sdQOyqq2IDhvmx2lHOpwd") } },
            { "Pentatonix", new[] { ("Hallelujah", "0HZk0QsXPhMNAWNDR3rYE8") } },
            { "Petite Meller", new[] { ("In Love With Myself (from Cyberpunk: Edgerunners 2)", "5uKBhXUSYkwdoV4M6vfn8l") } },
            { "Reneé Rapp", new[] { ("I Hate Boston", "5862ZA0ML6YODkxQF5UkgR"), ("Snow Angel", "6skUuqrKMQIjjo75hWGAvQ") } },
            { "Rick Astley", new[] { ("Never Gonna Give You Up", "4PTG3Z6ehGkBFwjybzWkR8") } },
            { "Sabrina Carpenter", new[] { ("Juno", "21B4gaTWnTkuSh77iWEXdS"), ("Manchild", "2BwO5K8Q7EPAJSGze3AAh9"), ("Nonsense", "6dgUya35uo964z7GZXM07g") } },
            { "Savage Garden", new[] { ("I Want You", "3XorCFmcupSm5QS6hA9g4N") } },
            { "TWICE", new[] { ("Feel Special", "3Hz3tTQwOdM6XkA0ALB2G9"), ("What is Love?", "3zhbXKFjUDw40pTYyCgt1Y") } },
            { "Taylor Swift", new[] { ("22", "2ULNeSomDxVNmdDy8VxEBU"), ("All Too Well (10 Minute Version) (Taylor's Version) (From The Vault)", "5enxwA8aAbwZbf5qCHORXi"), ("Anti-Hero", "0V3wPSX9ygBnCm8psDIegu"), ("Blank Space", "1p80LdxRV74UKvL8gnD7ky"), ("Cruel Summer", "1BxfuPKGuaTgP7aM0Bbdwr"), ("Karma", "7KokYm8cMIXCsGVmUvKtqf"), ("Lavender Haze", "5jQI2r1RdgtuT8S3iG8zFC"), ("Mean", "5yEPktRqvIhko5QFF3aBhQ"), ("Paper Rings", "4y5bvROuBDPr5fuwXbIBZR"), ("Red", "0cITLOYn1Sv4q27zZPqlNK"), ("Wildest Dreams", "59HjlYCeBsxdI0fcm3zglw"), ("no body, no crime (feat. HAIM)", "3RaT22zZsxVYxxKR7TAaYF") } },
            { "The Proclaimers", new[] { ("I'm Gonna Be (500 Miles)", "66S14BkJDxgkYxLl5DCqOz") } },
            { "The Wanted", new[] { ("Glad You Came", "3AGOgQzp0YcPH41u9p7dOp") } },
            { "aespa", new[] { ("Supernova", "5lKnZbdGCBViitE1Ce5TZh") } },
        };
    }
}
