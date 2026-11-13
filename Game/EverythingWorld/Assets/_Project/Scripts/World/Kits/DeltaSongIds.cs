using System.Collections.Generic;

namespace EverythingWorld.World
{
    /// The Delta Crossroads's songs by artist (generated from scenes.json; part of DeltaKit), so every one of them can have
    /// its own light (DeltaKit.Song, DeltaKit.SongLamps).
    public partial class DeltaKit
    {
        static readonly Dictionary<string, (string title, string id)[]> SongIds = new Dictionary<string, (string, string)[]>
        {
            { "Albert King", new[] { ("Born Under A Bad Sign", "0vDn81gdOuRxjbIwcASuiV") } },
            { "B.B. King", new[] { ("The Thrill Is Gone", "4NQfrmGs9iQXVQI9IpRhjM") } },
            { "Big Mama Thornton", new[] { ("Hound Dog", "5Oc0vLGWdEWeCqIU8zyELt") } },
            { "Etta James", new[] { ("I'd Rather Go Blind", "1kPBT8S2wJFNAyBMnGVZgL") } },
            { "Fats Domino", new[] { ("Blueberry Hill", "06PdA0DLgF4BfAeUNZAbFG") } },
            { "Gary Clark Jr.", new[] { ("Bright Lights", "5nlAKJhde77caBJIHMfDcV") } },
            { "Howlin' Wolf", new[] { ("Smokestack Lightnin'", "2HUZVffVPXvqnrml0gXggp") } },
            { "John Lee Hooker", new[] { ("Boom Boom", "2Mr1bGI2E10K7Mt1UJZ6Mw") } },
            { "Junior Kimbrough", new[] { ("Meet Me in the City", "5LmmZgxFg1sSfxENxsdtak") } },
            { "Lead Belly", new[] { ("Where Did You Sleep Last Night", "13U0E6CXh2dnEi6eDzNyK0") } },
            { "Muddy Waters", new[] { ("I'm Your Hoochie Coochie Man", "3KSchPNSklO5McIqRH3qYX"), ("Mannish Boy", "58PSYdY0GFg0LFb2PxYk4T") } },
            { "R.L. Burnside", new[] { ("Goin' Down South", "0AbLQMdKJFIaO6FrNRMmuZ"), ("Someday Baby", "5ZBUC1YafitJgLt8dgZYTW") } },
            { "Robert Johnson", new[] { ("Cross Road Blues", "1TrGdXSgiBm8W68D2K1COG"), ("Me and the Devil Blues", "1QCUrXrY1qlxRhL3a9LYCA") } },
            { "Sister Rosetta Tharpe", new[] { ("Didn't It Rain", "3MxAT7MRx7V0CL3tVtJby1") } },
            { "Stevie Ray Vaughan", new[] { ("Pride and Joy", "1a2iF9XymafjRk56q7oCxo") } },
        };
    }
}
