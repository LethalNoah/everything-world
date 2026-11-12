using System.Collections.Generic;

namespace EverythingWorld.World
{
    /// Discotopia's songs by artist (generated from scenes.json; part of DiscoKit), so every one of them can have
    /// its own light (DiscoKit.Song, DiscoKit.SongLamps).
    public partial class DiscoKit
    {
        static readonly Dictionary<string, (string title, string id)[]> SongIds = new Dictionary<string, (string, string)[]>
        {
            { "ABBA", new[] { ("Dancing Queen", "0GjEhVFGZW8afUYGChu3Rr"), ("Take A Chance On Me", "5BckPAYcKEJuYs1eV1BHHe"), ("The Winner Takes It All", "3oEkrIfXfSh9zGnE7eBzSV"), ("Waterloo", "3Dy4REq8O09IlgiwuHQ3sk") } },
            { "Boney M.", new[] { ("Rasputin - Single Version", "67hbP9PFQZrb4XZc3TzB0s") } },
            { "CHIC", new[] { ("Le Freak", "7z3aH1QV2zwMKXxShBNOGe") } },
            { "Die Bokjollers", new[] { ("CCR Medley", "1JrIbXRFMEwwuYnhknpAeg") } },
            { "Dschinghis Khan", new[] { ("Moskau", "5t77UGzgSBRDDnaxJvE9R3") } },
            { "Earth, Wind & Fire", new[] { ("Let's Groove", "3koCCeSaVUyrRo3N2gHrd8"), ("September", "2grjqo0Frpf2okIBiifQKs") } },
            { "Hot Chocolate", new[] { ("You Sexy Thing", "714hERk9U1W8FMYkoC83CO") } },
            { "International Music System", new[] { ("Mojave", "6m2O1S9wEc8zX86f7lB1qA") } },
            { "Rufus", new[] { ("Ain't Nobody", "53Za5vyGc1x7GxgJVRjRKc") } },
            { "Sandy Marton", new[] { ("Camel By Camel", "6IqC6J0Csq851XKl2pfkzP") } },
            { "The Brothers Johnson", new[] { ("Strawberry Letter 23", "00kvKlIvfzQLSi6T2zwXuS") } },
            { "Walter Murphy", new[] { ("A Fifth Of Beethoven", "1hR3TNe1neCWRQatSVOFfX") } },
        };
    }
}
