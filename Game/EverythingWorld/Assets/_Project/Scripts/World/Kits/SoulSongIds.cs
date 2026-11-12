using System.Collections.Generic;

namespace EverythingWorld.World
{
    /// The Soul Lowlands's songs by artist (generated from scenes.json; part of SoulKit), so every one of them can have
    /// its own light (SoulKit.Song, SoulKit.SongLamps).
    public partial class SoulKit
    {
        static readonly Dictionary<string, (string title, string id)[]> SongIds = new Dictionary<string, (string, string)[]>
        {
            { "Alessia Cara", new[] { ("Here", "664gdARxaClFsoF5SXKOws") } },
            { "Ben E. King", new[] { ("Stand By Me", "3SdTKo2uVsxFblQjpScoHy") } },
            { "Booker T. & the M.G.'s", new[] { ("Green Onions", "4fQMGlCawbTkH9yPPZ49kP") } },
            { "Jubalaires", new[] { ("Noah", "5WpDW2kQPLpcIDUK1p77AC") } },
            { "Kirk Franklin", new[] { ("I Smile", "0UQDSP8cz6WmLJckO5jqUQ"), ("Stomp (Remix)", "5JggmPy0YhCLaA6Vt4cmMo") } },
            { "Life of the Party", new[] { ("Ooh Child", "36zZ7JtCaZisCgRGk5VSOw") } },
            { "Mahalia Jackson", new[] { ("How I Got Over", "4hItkuhPNuDgDrMPXx63CJ") } },
            { "Marvin Gaye", new[] { ("Ain't No Mountain High Enough", "7tqhbajSfrz2F7E1Z75ASX") } },
            { "Nathaniel Rateliff & The Night Sweats", new[] { ("S.O.B.", "1VPUpUwtbpoRVXt0txVU0w") } },
            { "Otis Redding", new[] { ("(Sittin' On) the Dock of the Bay", "4Y4Gd3ty8uut6Qw43c7yJc"), ("Try a Little Tenderness", "36AlMHDBFwSsD3FQOy1R81") } },
            { "Ray Charles", new[] { ("Georgia on My Mind - Original Master Recording", "47mA6f44zxLtdATOoY7GjN"), ("What'd I Say, Pt. 1 & 2", "5yQ9iMZXGcr5rlO4hoLsP4") } },
            { "Sam Cooke", new[] { ("(What A) Wonderful World", "7N15WWy12QAMpGAh6QMAej") } },
            { "Stevie Wonder", new[] { ("Higher Ground", "0dMd4rilfd6gPbXaLpNYhu"), ("Signed, Sealed, Delivered (I'm Yours)", "2eF8pWbiivYsYRpbntYsnc"), ("Sir Duke", "4pNiE4LCVV74vfIBaUHm1b"), ("Superstition - Single Version", "1h2xVEoJORqrg71HocgqXd") } },
            { "Syl Johnson", new[] { ("Different Strokes", "5QC0qgroozRGGHAJuTsP4c"), ("Is It Because I'm Black", "7jIUxP1H9SoD6qq8YHjSTV") } },
            { "Teddy Swims", new[] { ("Lose Control", "6usohdchdzW9oML7VC4Uhk") } },
            { "The Edwin Hawkins Singers", new[] { ("Oh Happy Day", "34yIb7hw5SuR80au1BHTtt") } },
            { "The Five Stairsteps", new[] { ("O-o-h Child", "74JdR9aXE6I74oS1BVRsvb") } },
            { "The Foundations", new[] { ("Build Me up Buttercup", "6g3EtZDHdvss7vmqigJCRJ") } },
            { "The Staple Singers", new[] { ("I'll Take You There", "5YLnfy7R2kueN0BRPkjiEG"), ("Wade In The Water", "66ngnR0i22vav5VqP6dyRd") } },
            { "Wendy Rene", new[] { ("After Laughter (Comes Tears)", "3sMTpU1SNIeMCWu7sQKXMN") } },
        };
    }
}
