using System.Collections.Generic;

namespace EverythingWorld.World
{
    /// The Velvet Rooftops's songs by artist (generated from scenes.json; part of VelvetKit), so every one of them can have
    /// its own light (VelvetKit.Song, VelvetKit.SongLamps).
    public partial class VelvetKit
    {
        static readonly Dictionary<string, (string title, string id)[]> SongIds = new Dictionary<string, (string, string)[]>
        {
            { "070 Shake", new[] { ("Baby Driver", "5dtDj7mdGJjlviTxyfMrZE") } },
            { "Brent Faiyaz", new[] { ("DEAD MAN WALKING", "2c2tlXfEmLgUNvxngIi1qL") } },
            { "Daniel Caesar", new[] { ("Get You (feat. Kali Uchis)", "2uP6t2J5MEwhr9rDkAAzwh") } },
            { "Danny Madden", new[] { ("Facts of Life", "7gE76oTWSAWzHoLVcos9PL") } },
            { "Diljit Dosanjh", new[] { ("G.O.A.T.", "46QbY78ha62aiu6gBgC7lS") } },
            { "Frank Ocean", new[] { ("Pink + White", "3xKsf9qdS1CyvXSMEid6g8"), ("Thinkin Bout You", "7DfFc7a6Rwfi3YQMRbDMau"), ("White Ferrari", "2LMkwUfqC6S6s6qDVlEuzV") } },
            { "GIVĒON", new[] { ("Heartbreak Anniversary", "2QfznFotJNZmnIEYFdzE5T") } },
            { "Ginuwine", new[] { ("Pony", "6mz1fBdKATx6qP4oP1I65G") } },
            { "H.E.R.", new[] { ("Focus", "3tZs3nVjySLbL320lP4mvs") } },
            { "J Rice", new[] { ("Where My Diamonds Hide", "7EQtjGj0vvHUK1iFmBb6o9") } },
            { "Jason Derulo", new[] { ("Whatcha Say", "67T6l4q3zVjC5nZZPXByU8") } },
            { "Jay Sean", new[] { ("Down", "6cmm1LMvZdB5zsCwX5BjqE") } },
            { "Karan Aujla", new[] { ("Softly", "0YQJoDL6f46J0n1rOVkpxJ") } },
            { "Mike Posner", new[] { ("I Took A Pill In Ibiza - Seeb Remix", "1MtUq6Wp1eQ8PC6BbPCj8P") } },
            { "Prabh Singh", new[] { ("9:45", "6c4Po8zyQjB4cR5XgLhcGb") } },
            { "R. Kelly", new[] { ("I Believe I Can Fly", "2RzJwBCXsS1VnjDm2jKKAa") } },
            { "Rihanna", new[] { ("Diamonds", "1z9kQ14XBSN0r2v6fx4IdG"), ("SOS", "30cSNer6TV8x2utjULVeQ5"), ("Umbrella", "2yPoXCs7BSIUrucMdK5PzV") } },
            { "SZA", new[] { ("Good Days", "4PMqSO5qyjpvzhlLI5GnID"), ("Kill Bill", "3OHfY25tqY28d16oZczHc8") } },
            { "Sean Kingston", new[] { ("Beautiful Girls", "1hGy2eLcmC8eKx7qr1tOqx") } },
            { "Steve Lacy", new[] { ("Bad Habit", "4k6Uh1HXdhtusDW5y8Gbvy") } },
            { "TLC", new[] { ("Waterfalls", "6qspW4YKycviDFjHBOaqUY") } },
            { "The Weeknd", new[] { ("Blinding Lights", "0VjIjW4GlUZAMYd2vXMi3b"), ("Lonely Star", "6nNr7G34ZmUpyIiYWpQzgP") } },
            { "blackbear", new[] { ("idfc - Tarro Remix", "1wsZ0cp5He4Wm4yohNjwIs") } },
        };
    }
}
