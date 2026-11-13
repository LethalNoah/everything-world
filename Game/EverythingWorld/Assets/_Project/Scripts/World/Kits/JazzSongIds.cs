using System.Collections.Generic;

namespace EverythingWorld.World
{
    /// The Old Jazz Quarter's songs by artist (generated from scenes.json; part of JazzKit), so every one of them can have
    /// its own light (JazzKit.Song, JazzKit.SongLamps).
    public partial class JazzKit
    {
        static readonly Dictionary<string, (string title, string id)[]> SongIds = new Dictionary<string, (string, string)[]>
        {
            { "Alice Coltrane", new[] { ("Journey In Satchidananda", "2gG3ivmsfylVXLyIJvLXyN"), ("Journey to Satchidananda", "3kmfx8L4sEpxs7766pLxrG"), ("Turiya & Ramakrishna", "4DVu7YgHVJaeN0C7HNpy2q") } },
            { "Billie Holiday", new[] { ("Strange Fruit", "1CTex49P0iWwzUGsMNjgaV") } },
            { "Cab Calloway", new[] { ("Minnie the Moocher (Theme Song)", "2JOJBxwRHWYcdXiOxZmoFv") } },
            { "Cannonball Adderley", new[] { ("Mercy, Mercy, Mercy", "6NF8MZ2trxckVBr82pyyPt") } },
            { "Chet Baker", new[] { ("Tis Autumn", "6g82W4awZ4ZqSs2jUT5goS") } },
            { "Django Reinhardt", new[] { ("Minor Swing", "0NDjQezJaEC1KX9TfwEfzf") } },
            { "Donald Byrd", new[] { ("Dominoes", "3s8Anp8dKObIw8Zy8uGgYC") } },
            { "Duke Ellington", new[] { ("Jeep's Blues", "3yizjcTNKg1lhBOr6wTeOB"), ("Take the \"A\" Train", "2BuIAKijtji4xpab2QCTBv") } },
            { "Ella Fitzgerald", new[] { ("A-Tisket, A-Tasket", "5935p4F2K5crvwdc6mBLPg") } },
            { "Frank Sinatra", new[] { ("I've Got You Under My Skin - Remastered 1998", "3aEJMh1cXKEjgh52claxQp") } },
            { "Glenn Miller", new[] { ("In the Mood", "1xsY8IFXUrxeet1Fcmk4oC") } },
            { "Herbie Hancock", new[] { ("Cantaloupe Island - Remastered 1999/Rudy Van Gelder Edition", "0sCeNwt8xRCMR4NhKpMyBe"), ("Sansho Shima", "1Ku9ln2irtAodKQ2bIyNm5") } },
            { "Jimmy Smith", new[] { ("Root Down (And Get It) - Live", "19PG9tIlRRi56n7Tgywkxm") } },
            { "Joe Henderson", new[] { ("Earth", "5tR80sypNbMyWIbDfiBBXh") } },
            { "Les McCann", new[] { ("Compared to What - Live at Montreux Jazz Festival", "32hdwm17L8lc2OfBxfarsJ") } },
            { "Louis Armstrong", new[] { ("West End Blues", "1fcHyenlOvYHnF3xMz2wDB"), ("What A Wonderful World", "29U7stRjqHU6rMiS8BfaI9") } },
            { "Miles Davis", new[] { ("So What (feat. John Coltrane, Cannonball Adderley & Bill Evans)", "4vLYewWIvqHfKtJDk8c8tq") } },
            { "Moondog", new[] { ("Bird's Lament", "1EuFHPoVGfyfEZBaYExK7v") } },
            { "Nat King Cole", new[] { ("Unforgettable", "648TTtYB0bH0P8Hfy0FmkL") } },
            { "Pharoah Sanders", new[] { ("Astral Traveling", "55X66sKArUevBeGD7buroR") } },
            { "Preservation Hall Jazz Band", new[] { ("Santiago", "1P2J9E5gHkJXP8wsiOOa81") } },
            { "Sonny Rollins", new[] { ("St. Thomas", "05LNBtMuc26HSAHh7C6iG8") } },
            { "Sonny Rollins Quartet", new[] { ("St. Thomas", "7pTti5mG3iekdMu8Txr2lc") } },
            { "Stan Getz", new[] { ("The Girl From Ipanema", "0knHsI5KDPj1nRoya1p8Tu") } },
            { "The Dave Brubeck Quartet", new[] { ("Blue Rondo à la Turk", "7CrNF9zL7tIQ2269DVxzST") } },
            { "Zoh Amba", new[] { ("Another Time", "77Ukfo8YLUQg17odJF4brC") } },
        };
    }
}
