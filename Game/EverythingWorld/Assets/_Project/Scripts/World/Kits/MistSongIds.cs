using System.Collections.Generic;

namespace EverythingWorld.World
{
    /// The Ambient Mists's songs by artist (generated from scenes.json; part of MistKit), so every one of them can have
    /// its own light (MistKit.Song, MistKit.SongLamps).
    public partial class MistKit
    {
        static readonly Dictionary<string, (string title, string id)[]> SongIds = new Dictionary<string, (string, string)[]>
        {
            { "Aphex Twin", new[] { ("Ageispolis", "7KRQoq9GeWeCm0ZAXg5XMb"), ("Avril 14th", "2MZSXhq4XDJWu6coGoXX1V"), ("Green Calx", "3fFOi1PqxGjFKmWW21uQNa"), ("Omgyjya-Switch7", "6qjGx36gz8SmoS0wJYlTYD"), ("Pulsewidth", "643gyipSU7dkmrFhJ8UAIm"), ("Xtal", "7o2AeQZzfCERsRmOM86EcB") } },
            { "Boards of Canada", new[] { ("Dayvan Cowboy - Mixed", "0Slp4vNp2ZSIDEfJafmEF3"), ("Hydrogen Helium Lithium Leviathan", "1NnBADacrHxF7oSprSFoWn"), ("Into The Magic Land", "2dgrdKpGX3Vgag91cM0qgK"), ("Naraka", "6IuquJdJVxdSEgsJv5YU6Z"), ("Prophecy At 1420 MHz", "4CpyZQzwxCzQPiUTvlOBrn"), ("Roygbiv", "5Hf2h59YLInKlic7ooWZVd"), ("Satellite Anthem Icarus", "6pxWghcPKRSjZPj8Ba9vpB"), ("You Retreat In Time And Space", "5m3Uzjuiyj8656EGM1bwhk") } },
            { "Douglas Holmquist", new[] { ("Hypersphere", "1joULm5birdUYPkANMfhtP") } },
            { "Elori Saxl", new[] { ("Seeing Is Forgetting", "19iE95ZowwXzcMaLZNcWyn") } },
            { "Express Rising", new[] { ("Keys to Be", "1c3sxSaNZjVWblhiBHSv0I") } },
            { "KatzPascale", new[] { ("SINK WHERE YOU SLEEP", "0JtscTBmEhAhw3PalM7AST"), ("SPILL YOUR TIME", "6OoGLHsnbEBnaEC6oM5qJF") } },
            { "Marconi Union", new[] { ("Weightless", "6kkwzB6hXLIONkEk9JciA6") } },
            { "Mark Barrott", new[] { ("Baby Come Home", "3ufLGsgYAiwIKyAJatdCvK") } },
            { "Pipetka", new[] { ("Hero's Ascent", "5DxG8HbM2mdIE2Vfy8ffVd") } },
            { "Prosecutor Archangels", new[] { ("Cifra palota", "5dUcoiJ2kFvXXOcShPn1uj") } },
            { "Rei Harakami", new[] { ("owari no kisetsu", "0MpYcFOQXnjkQF4hRKpetC") } },
            { "Shpongle", new[] { ("Divine Moments of Truth", "5z0QLEpkbUh00SevSuYxOh") } },
            { "Tycho", new[] { ("Into The Woods", "6kPMh9GFP7LSv5H1yIdrnv"), ("Japan (Instrumental)", "2wqyAaRNAZBO9OncYwAQSR") } },
            { "Vegyn", new[] { ("La femme d'argent - Vegyn Version", "7jDxY5BblYMlamymg6jqfA"), ("There’s Something Here for Us Both", "310bPg4gYgXqHKolfGS3Sr") } },
            { "ridgeclub", new[] { ("I'm God", "5DysE4kUIUSJntpnQt993V") } },
            { "⣎⡇ꉺლ༽இ•̛)ྀ◞ ༎ຶ ༽ৣৢ؞ৢ؞ؖ ꉺლ", new[] { ("(̸̢̛̼̞̭͋ͅ)̸͚̰͛̔̾̀̿͒͂v̴̢͚͚͎ȯ̶̞̮͖̑̈́OOOOOOOooo☼⃝☼⃝◉࿃ूੂ࿃ूੂੂ ʅ͡͡͡͡͡͡͡͡͡͡͡()ʃ͡͡͡͡͡͡͡͡͡͡ ꐑ(ఠీੂȯ̶̞̮͖̑̈́̿)̸̳̥̰̜̥̺̐ͅ࿃ूੂ✧⃛✧⃛)̴͎̜͍̱̋̌͋̓̾̚͜ ̷̨̢̥̅͝ͅ(̸̢̛̼̞̭͋ͅ)̸͚̰͛̔̾̀̿͒͂:̴͓̞̑̌̂̆̊͋̀:̸͎̟̯̂̓̌:̶̢͙͙͕̠̩͆(̷̮͍͚̫͚͂̍̒̋̕)̵̳̗̊̍̽́ ̵͍̪̲̀̍ ̴̤̚ ̴̹̙͙̻̓̉̚͝ ̟̞̝̜̙̘̗̖҉̵̴̨̧̢̡̼̻̺̹̳̲̱̰̯̮̭̬̫̪̩", "68ozhSrI4eLbnUgih1SlOT") } },
        };
    }
}
