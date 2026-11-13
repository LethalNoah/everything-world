using System.Collections.Generic;

namespace EverythingWorld.World
{
    /// The Afrobeat Coast's songs by artist (generated from scenes.json; part of AfroKit), so every one of them can have
    /// its own light (AfroKit.Song, AfroKit.SongLamps).
    public partial class AfroKit
    {
        static readonly Dictionary<string, (string title, string id)[]> SongIds = new Dictionary<string, (string, string)[]>
        {
            { "Ayra Starr", new[] { ("Rush", "1rrqJ9QkOBYJlsZgqqwxgB") } },
            { "Burna Boy", new[] { ("Last Last", "5YbPxJwPfrj7uswNwoF1pJ"), ("Ye", "3FskQrDXcY24ur2fCvz35O") } },
            { "CKay", new[] { ("love nwantiti (ah ah ah)", "2Xr1dTzJee307rmrkt8c0g") } },
            { "Davido", new[] { ("Fall", "4ou5xyFUJX4VwX76tw1qb1") } },
            { "Diamond Platnumz", new[] { ("Komasava (Comment Ça Va)", "5mjxSIPHAMNk40q0nON5Cb") } },
            { "Fela Kuti", new[] { ("Water No Get Enemy", "4YZv78tF4qCI841KHRF3vf") } },
            { "I Hate My Village", new[] { ("Tony Hawk of Ghana", "4U0AlhWv6AQF9PHiwfaLdH") } },
            { "Letta Mbulu", new[] { ("Nomalizo", "3lB31lvF4GEwKwMgFWwCJ7") } },
            { "Manu Dibango", new[] { ("Mpuli Mwa Moni", "4DIyNg8b662jBsM4ECZDc6") } },
            { "Nomo", new[] { ("Upside Down", "0VaKi3rIlh275tMwCQzHuj") } },
            { "William Onyeabor", new[] { ("Fantastic Man", "1ETZhP9orTkDclKEyt0xqm") } },
            { "Wizkid", new[] { ("Essence (feat. Tems)", "5FG7Tl93LdH117jEKYl3Cm") } },
        };
    }
}
