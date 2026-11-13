using System.Collections.Generic;

namespace EverythingWorld.World
{
    /// The Lo-Fi Hills's songs by artist (generated from scenes.json; part of LofiKit), so every one of them can have
    /// its own light (LofiKit.Song, LofiKit.SongLamps).
    public partial class LofiKit
    {
        static readonly Dictionary<string, (string title, string id)[]> SongIds = new Dictionary<string, (string, string)[]>
        {
            { "Alecs DeLarge", new[] { ("North Street", "0as33YFhrlDSCilqA6ODpx") } },
            { "Bonus Points", new[] { ("Cup of Joe", "6A60wLoL8u7QM1Xc0dHUY2") } },
            { "Doctor Do", new[] { ("the world is small", "2uIScdCnKKPxcGa6M13TGe") } },
            { "Felipe Jara", new[] { ("Inside the Castle Walls (From \"Super Mario 64\")", "3jcA7siOXJtG9enynAPiyH") } },
            { "Idealism", new[] { ("dont say a word", "6cufomJu4w7bQEUGctpDf9"), ("monster gambling in tokyo", "3WKInwdmLC2tFATeFrGp26"), ("waon to ame", "6q65COfzljqN3WnT4R2SBT") } },
            { "Jinsang", new[] { ("Genesis", "730macsD2sK1p5MYemRCAa") } },
            { "Jonny Abbey", new[] { ("The Superb Lyrebird", "7smJkDHfGPTWHGTlX7knWa") } },
            { "Michael Lementi", new[] { ("Aquatic Ambience", "23Gd6QcHo2ifUgM6HM1rDk"), ("Big Blue", "5WCLzjUETIDRUeZlK63egB") } },
            { "Sleepdealer", new[] { ("Weed", "6N5sUQtcOY2Z73JTEZ7J1w") } },
            { "Stan Forebee", new[] { ("Awakening of the Flowers", "1yjqIsqGHebyfFO9WtEMAX") } },
            { "TOKiMONSTA", new[] { ("Darkest (Dim)", "548HwuXDxj3wxRu45e0uf8") } },
            { "The Deli", new[] { ("Flowers", "6ugrRFZUNIpLiqhLUgC7ix") } },
            { "digitalluc", new[] { ("Driver", "2CDPapOfdTYQTfgmn85Ns8"), ("Soft Hell", "2ZnEWmpSNoEWvqBaU1b19q"), ("Underwater", "75kV29N3NsJIOkfuIy0113") } },
            { "letherette", new[] { ("Rich & Dan", "4c3YOeHLBe6hWbWUbJVsbS") } },
            { "o k h o", new[] { ("41058", "3099fRL8fkb7zgC4VQzuG0") } },
            { "quickly, quickly", new[] { ("Getsomerest/sleepwell", "2RycgtfFQZOPgEGrzBGE0j") } },
            { "tides.", new[] { ("tuscano", "6mAzWmlKj5molQ5pinZ85T") } },
            { "wuudlum", new[] { ("Häwelmann", "1tompGg0ZSPXgHUNPW9HZx") } },
        };
    }
}
