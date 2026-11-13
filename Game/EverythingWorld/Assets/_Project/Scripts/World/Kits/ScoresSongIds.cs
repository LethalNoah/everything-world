using System.Collections.Generic;

namespace EverythingWorld.World
{
    /// The Isle of Scores's songs by artist (generated from scenes.json; part of ScoresKit), so every one of them can have
    /// its own light (ScoresKit.Song, ScoresKit.SongLamps).
    public partial class ScoresKit
    {
        static readonly Dictionary<string, (string title, string id)[]> SongIds = new Dictionary<string, (string, string)[]>
        {
            { "Ancient Chinese composer", new[] { ("Imperial Concubine's Tears", "2YdYOmcCptg8zWsOlE8mG0"), ("White Snow In Early Spring", "3sHcWA4z8QomPRQVOP3Exg") } },
            { "Danny Elfman", new[] { ("Jack's Lament", "4VCexsNDyC2j8ZrY34RwV0"), ("Kidnap The Sandy Claws", "06iUp1XbbaOvAmzrrZeT32"), ("What's This?", "3fytQQcjEOWXlvI9BDXVcK") } },
            { "Ennio Morricone", new[] { ("L'Estasi Dell'Oro", "4eVaWO0viurPB3C33Rz7xi") } },
            { "Hollywood Studio Orchestra", new[] { ("Indiana Jones Theme", "5QMytOF6piEYwZDCHbjxTf") } },
            { "Jerry Goldsmith", new[] { ("Soarin' - From \"Soarin' Over California\"", "10n8QgSpFaTQDwpy73YCfC") } },
            { "Joe Hisaishi", new[] { ("One Summer Day", "3gFQOMoUwlR6aUZj81gCzu") } },
            { "John Williams", new[] { ("Anakin vs. Obi-Wan", "3fzL0RuACZtv5cQ6R78Sjq"), ("Cantina Band", "5ZSAdkQb23NPIcUGt6exdm"), ("Duel of the Fates", "1ghlpxVfPbFH2jenrv9vVw"), ("Finale", "27xA5HEUnWPBDo0z4hcDq1"), ("Imperial Attack", "3KVsq9veMUESIb3ln7Rowv"), ("Star Wars (Main Theme)", "7a9UUo3zfID7Ik2fTQjRLi"), ("The Imperial March (Darth Vader's Theme)", "2bw4WgXyXP90hIex7ur58y") } },
            { "Mark Governor", new[] { ("Stadium Rave", "2Fxn9wa3Nv1QGuRptSqPHO") } },
            { "Mark Snow", new[] { ("Materia Primoris: The X-Files Theme (Main Title)", "4th0mPYPuZUs564oCu5I1U") } },
            { "Michael Giacchino", new[] { ("Space Mountain - From \"Space Mountain\"", "7dP8wsVwcW5Km76Fqgn3jQ") } },
            { "Pyotr Ilyich Tchaikovsky", new[] { ("Tchaikovsky: Swan Lake, Op. 20, Act 2: No. 10, Scene. Moderato", "2xizRhme7pYeITbH1NLLGt") } },
            { "Richard Wagner", new[] { ("Ride of the Valkyries", "2A7qdr3UNP9Pxjcxa5Jj53") } },
            { "Sergei Rachmaninoff", new[] { ("Piano Concerto No. 2 in C Minor, Op. 18: 2. Adagio sostenuto", "4rrrn8OLrttq7r9RgNXalU") } },
            { "The Citizens of Halloween", new[] { ("This Is Halloween", "01YROQCnF1AQm7SCWJmD2o") } },
            { "The Lorax Singers", new[] { ("Let It Grow - From \"Dr. Seuss' The Lorax\"", "5BWbhDZB3TJgF7hb2TlTIG") } },
        };
    }
}
