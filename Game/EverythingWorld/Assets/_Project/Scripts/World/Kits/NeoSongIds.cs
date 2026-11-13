using System.Collections.Generic;

namespace EverythingWorld.World
{
    /// The Neo Soul Quarter's songs by artist (generated from scenes.json; part of NeoKit), so every one of them can have
    /// its own light (NeoKit.Song, NeoKit.SongLamps).
    public partial class NeoKit
    {
        static readonly Dictionary<string, (string title, string id)[]> SongIds = new Dictionary<string, (string, string)[]>
        {
            { "Children of Zeus", new[] { ("Hard Work", "4CuNDg0YWkNiSg0DZaAKuV") } },
            { "D'Angelo", new[] { ("Untitled (How Does It Feel)", "4oPNN7syJYSjzDhRerF966") } },
            { "Erykah Badu", new[] { ("Didn't Cha Know", "7pv80uUHfocFqfTytu1MVi"), ("On & On", "0tNuJpBgtE65diL6Q8Q7fI") } },
            { "Hiatus Kaiyote", new[] { ("Red Room", "55mvtucws4Mnro27744t9X") } },
            { "Jill Scott", new[] { ("Golden", "0bHs3ly4Bv5BlzE3KrePfX") } },
            { "Jorja Smith", new[] { ("Blue Lights", "1It1TxBK3xIQAB4xJkPevn") } },
            { "KIRBY", new[] { ("Saving Grace - From the Prime Original Series Spider-Noir", "0WnGsQz6tZl0r4E1lC9Hlq") } },
            { "Kali Uchis", new[] { ("telepatía", "6tDDoYIxWvMLTdKpjFkc1B") } },
            { "Maxwell", new[] { ("Ascension (Don't Ever Wonder) - Remastered 2021", "6l4TNbfPtNAOOlGIfceSaO"), ("Fortunate", "2RtvkLz5tmOXlzz918It6E"), ("Pretty Wings - uncut", "22NLm3IIR9NLG0cUYtmHMW"), ("Sumthin' Sumthin' - Remastered 2021", "60Pjyla2BNnWzWxNXxGa4u"), ("This Woman's Work - 2021 Remaster", "741OE401HO7ZCaPhEqCZ7w") } },
            { "Ms. Lauryn Hill", new[] { ("Doo Wop (That Thing)", "0uEp9E98JB5awlA084uaIg"), ("Ex-Factor", "2VjXGuPVVxyhMgER3Uz2Fe"), ("Nothing Even Matters (feat. D'Angelo)", "1Q0lOGB52skgfzdcq233vs") } },
            { "SAULT", new[] { ("Why Why Why Why Why", "4zwq3QUKgMNk0NSLl7fpbP"), ("Wildfires", "4Tzw9IdZbVyzmpWrvsGaNX") } },
            { "Solange", new[] { ("Almeda", "6IAgarq2wDeYzpsY2Kx0Hw"), ("Cranes in the Sky", "48EjSdYh8wz2gBxxqzrsLe"), ("Losing You", "5hR0wTnydz6lmErOV4c0sp") } },
            { "Victoria Monét", new[] { ("On My Mama", "1o8Z7GD1CeOaVBEyuzu4HO") } },
        };
    }
}
