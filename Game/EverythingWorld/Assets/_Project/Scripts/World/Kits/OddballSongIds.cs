using System.Collections.Generic;

namespace EverythingWorld.World
{
    /// The Oddball Backroads' songs by artist (generated from scenes.json; part of OddballKit), so every one of them
    /// can have its own light (OddballKit.Song, OddballKit.SongSigns).
    public partial class OddballKit
    {
        static readonly Dictionary<string, (string title, string id)[]> SongIds = new Dictionary<string, (string, string)[]>
        {
            { "Beck", new[] { ("In The Night", "04E1YpF9dRxIrOE6Q5uJQS"), ("Loser", "5PntSbMHC1ud6Vvl8x56qd") } },
            { "Bloodhound Gang", new[] { ("Fire Water Burn", "7FkyQsWH9mmxoYLCNj4f8y") } },
            { "Butthole Surfers", new[] { ("Pepper", "1ng36571Iyov4HBxUClySn") } },
            { "CAKE", new[] { ("The Distance", "1szwDbVdIdoQq5mG5GXAmJ") } },
            { "Dexter and The Moonrocks", new[] { ("It's A Lot", "5AiKETKUAet8GFGOos5Suq") } },
            { "Eels", new[] { ("Novocaine For The Soul", "2fKdsBazcOLLIzDiZUQCih") } },
            { "MJ Lenderman", new[] { ("Wristwatch", "4j2DQgFAyVuKOBFpq4vEPR") } },
            { "Pixies", new[] { ("All I Think About Now", "3NfqN997u9A3YAAafgzhIa"), ("Hey", "0p5eZCY0R7uNCZS1YDtIYI"), ("Monkey Gone to Heaven", "6BT7qiDY4A5tOyklohjQuW"), ("Wave of Mutilation", "51Z2IzJvLCnZaIpBV0IYRr"), ("Where Is My Mind? - 2007 Remaster", "7wCmS9TTVUcIhRalDYFgPy") } },
            { "R.E.M.", new[] { ("It's The End Of The World As We Know It (And I Feel Fine)", "2oSpQ7QtIKTNFfA08Cy0ku"), ("Losing My Religion", "31AOj9sFz2gM0O3hMARRBx") } },
            { "Silver Jews", new[] { ("Random Rules", "6lquLzGE5CRoq2htyr2QGS") } },
            { "That Handsome Devil", new[] { ("Charlie's Inferno", "2tmei4gBweN2TsRcTgIh0t") } },
            { "The Flaming Lips", new[] { ("Yoshimi Battles the Pink Robots, Pt. 1", "0ccCwNzXvr1Yoz91vKz31Z") } },
            { "They Might Be Giants (For Kids)", new[] { ("Hot Dog!", "0grxFyenxR3OYMdIhxRzJ6") } },
            { "Victor Jones", new[] { ("Moon Bright Rain", "1nzKrriwKIZAxjVWEqeSTm") } },
            { "Violent Femmes", new[] { ("Blister In The Sun", "6eDwkS4IHADqk06ssrqfUX"), ("Blister In The Sun", "7jIAttgQTpLDoNtykIQXjH"), ("Kiss Off", "5fj76kVAnqRKKhAw5d06jj") } },
            { "Ween", new[] { ("Ocean Man", "6M14BiCN00nOsba4JaYsHW"), ("Roses Are Free", "5taUV8kbsjppc3Krz3KgFr"), ("She's Your Baby", "4Chah9FS9ar1F6qZR6FuoH"), ("Weed Whore - Live at Trocadero Theatre, Philadelphia, PA, 6/16/1995", "6ID6of2pyNjsedy6LYi0gd") } },
            { "Wilco", new[] { ("Too Far Apart - 2017 Remaster", "31ju3T6qfWE8yKPBwv0WSx") } },
        };
    }
}
