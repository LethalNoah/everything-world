using System.Collections.Generic;

namespace EverythingWorld.World
{
    /// The Metalcore Caldera's songs by artist (generated from scenes.json; part of CalderaKit), so every one of them
    /// can have its own light (CalderaKit.Song, CalderaKit.SongStones).
    public partial class CalderaKit
    {
        static readonly Dictionary<string, (string title, string id)[]> SongIds = new Dictionary<string, (string, string)[]>
        {
            { "Acres", new[] { ("Into Flames", "5IGi1IZ6IsXtvP1fXEvv8B"), ("My Everything", "1Zsy3xy9TvYELEGg1js4yw") } },
            { "Alexisonfire", new[] { ("Accidents", "1FuZJZqUWbwpqwbT8eESfR"), ("Happiness by the Kilowatt", "1WkBlSjsAy200erfndrRU5"), ("Hey, It's Your Funeral Mama", "0isd7xJL3Oh1Cw9RraixQL"), ("No Transitory", "3DVxs4Zojt5UzySQ5icF2U"), ("Side Walk When She Walks", "0ZQtCXF4oA6vz4W7A9iDT0") } },
            { "Bad Omens", new[] { ("The Grey", "5oZy9b1lMtREB3cqOPQusD") } },
            { "blessthefall", new[] { ("Bottomfeeder", "2ntC6NBbFHwjEKAbUOIKs3"), ("Meet Me At The Gates", "1trn3iWYhPclNUgHSXJgZN"), ("To Hell And Back", "6v0xtmSo0M16LsyDqUeWLh"), ("Undefeated", "6FmVzQZAvBo1IYATAEzTAu") } },
            { "Breathe Carolina", new[] { ("Wooly", "6UB2S7V8WlrRZw2DwISLFo") } },
            { "Broadway", new[] { ("Don’t Jump The Shark Before You Save The Whale", "2Rkq5FtGrTMlxLRt8d0QYN"), ("Same Thing We Do Everyday Pinky", "0e7Nhkz1m36BjJxlwVUjJh") } },
            { "Caskets", new[] { ("Falling Apart", "1UL8s63ZiZKPJLnHHP0KV5") } },
            { "Chiodos", new[] { ("TAPDAT", "0pQ9gtLMkVhioRBVl5yCHs") } },
            { "Dayseeker", new[] { ("Bloodlust", "5j5Fwl2oBvieAP9InfNwks"), ("Crawl Back To My Coffin", "7rhDDCVryx9frLUnXSgrZ5"), ("Neon Grave", "46b5EPoX1k0Q1VymqWZ35R"), ("Neon Grave", "4pehGtiMD6B2WZHsKmr3oo"), ("Without Me", "7oGCVuPEyp6m6DhXkU5Pfs") } },
            { "Drive Like Jehu", new[] { ("Do You Compute", "793RxZVVT5ITET0thwbjAK"), ("Here Come the Rome Plows", "2VaZ98xvuFoOwlzqAedqUZ") } },
            { "ERRA", new[] { ("House of Glass", "78XvV0xPDuG1xaCy6tR3Bj") } },
            { "Ghost Town", new[] { ("Monster", "1GSjuOkdaG3cyIW4eROHBB") } },
            { "Hands Like Houses", new[] { ("Introduced Species", "08hhubTCIVffn1jQSErXBI"), ("Lion Skin", "13i0hX9JarWMLiopuGdtVO"), ("Starving To Death In The Belly Of The Whale", "2Iwk0OnJo6lTsvnek6anLh") } },
            { "Holding Absence", new[] { ("A Crooked Melody", "4gELxlWiK6bE3MCugUcefl"), ("Afterlife", "7ridH3hy4aXG6Kn1XomKrO"), ("Gravity", "1s1G7P7dp7M5v8Jo4ZikMT") } },
            { "I Prevail", new[] { ("Bow Down", "4U9BOwX61f1TCXdukPa2ub") } },
            { "I See Stars", new[] { ("Electric Forest", "5zf0lGoG18RNUVPUUggXRI") } },
            { "Imminence", new[] { ("Heaven in Hiding", "4SwArKsYS1uHsBtNSFvi8U") } },
            { "Issues", new[] { ("Tapping Out", "2drTjmYf4KfzBeW5fCEYco") } },
            { "Jon Keith", new[] { ("YOU CAN ALWAYS COME BACK HOME", "1XCWAofU9kAmdHda2dJpbj") } },
            { "Killswitch Engage", new[] { ("Holy Diver", "3a9urnht8HvfFzPwd0ipx6"), ("My Curse", "6zKF4293k44ItKWJJgrhXv"), ("The Arms of Sorrow", "42lC6bnDdAjRqZtsFe1tmg"), ("The End of Heartache", "2UwtS6sARvcgqS9MYIqnnh"), ("The End of Heartache", "2nUy0ifVE7UwtOK4rugFsP"), ("This Fire", "07ceEXOyS7ghQkyCOycXwP"), ("This Is Absolution", "7lcuIiWzol3KwvDspHqG5c") } },
            { "Like Moths To Flames", new[] { ("Demon of My Own", "1EjEaIVXdcFsbZJvZScu9s"), ("God Complex", "6JVWK0ZZ9UkoLkQwakmRnW"), ("Habitual Decline", "7Hq6VvuWklDZLCStkvVbtE"), ("Killing What's Underneath", "1pXq5WSNCbRvUJ5VnqAjIk"), ("Spiritual Eclipse", "2rNomxA2QbC7CN1JXS5nKl"), ("The Anatomy of Evil", "22N1zTCluqKQShlNcVCMGb") } },
            { "Maybsomeday", new[] { ("Zombie", "6ffjmiuMMRmeUWQzZaUPE0") } },
            { "Nerv", new[] { ("Not The One", "7xkWkZVny1aip5hyGpFluH") } },
            { "Nevertel", new[] { ("everything in my mind", "7c2oTeI3aVfmOyzVYcDPdy") } },
            { "Normandie", new[] { ("Blood In The Water", "1eFtuUsdNIh7vTyyG6mwIo") } },
            { "NOVELISTS", new[] { ("Antares", "2Bnb2GHR55HZYOIBbzYgap"), ("Ravage", "2pUaecpU87lb2enphyDByI") } },
            { "Of Machines", new[] { ("Becoming Closer to Closure", "4GLxk8T3Juf4Tt08iuPq5h") } },
            { "One Step Closer", new[] { ("Blue Hour", "5RNrHz8la82J1OghC51kpZ") } },
            { "Our Last Night", new[] { ("Beautiful Things", "2iEjhnmySJ159VKUEH0AEv") } },
            { "Pierce The Veil", new[] { ("King For A Day", "1IT0WQk5J8NsaeII8ktdlZ") } },
            { "PISS", new[] { ("time loop at hot slit", "6c8pSAIdGhJhYipnjY2dQb") } },
            { "Rain City Drive", new[] { ("Bury a Lie", "1RNS0ntWy01bZpPkrjJAyt"), ("Frozen", "0KyPp6jAAmN1CqXd1lXGRQ"), ("Heavier", "6Ps8twA0kAJ1jSZzhvdBQI") } },
            { "Saosin", new[] { ("Collapse", "4iLRzDTNc3NbTyQ0w5cAOd") } },
            { "Siamese", new[] { ("Chemistry", "24rdJVqYaTTWa0vs0oQcZY") } },
            { "Silverstein", new[] { ("My Heroine", "6NDoBIaqTHdcudaR8RDJNw"), ("Smile In Your Sleep", "29dljceloiAgAeARUwZ3QC") } },
            { "Sleeping With Sirens", new[] { ("Better Off Dead", "4MjJlPgUYMQjvHOQq0uvOv") } },
            { "Spiritbox", new[] { ("Perfect Soul", "7CoiOXsrfI58wUZBt45HgL") } },
            { "The Fall of Troy", new[] { ("F.C.P.R.E.M.I.X.", "4OsLDuaH0bWR6xM6nj66F8") } },
            { "The Used", new[] { ("All That I've Got", "6IxQ0PdozaZKch1m8tGCwb") } },
            { "The Year End", new[] { ("If It Hadn't Ended Then, It Would've Ended Now - 2023", "6VRllAKog8Hlnv4IG7iW1x") } },
            { "Thousand Below", new[] { ("Tradition", "6lrXcmmm0Rsg1K5FdkZE0U"), ("Venenosa", "6mDeO2CcsbmvE6DKN2VBmD") } },
            { "Tom Morello", new[] { ("Everything Burns (feat. Beartooth)", "5u6Ia3xPeftMOeWGmhVU3s") } },
            { "Turnstile", new[] { ("BIRDS", "0kshHISCRGn9MwpkbqafG4"), ("BLACKOUT", "0bGImSqDB2ebdeoCidUC8o"), ("NEVER ENOUGH", "1zoXDi9AY4TF5JoUuCCTL6") } },
            { "Underoath", new[] { ("To Whom It May Concern", "3FNaxrbtUnEnnVOaDQRe4W"), ("Writing On The Walls", "313yF0VXTCmFFoYEJF7nBQ") } },
            { "Until I Wake", new[] { ("Cold", "03lSuwFFTlNMlsdbAVL33N") } },
            { "Vanna", new[] { ("A Dead Language for a Dying Lady", "4WAFtkCvX6S76MF886qsTT") } },
            { "Wendy's", new[] { ("The Best Combo Meal on the Worst Night of my Life (feat. Beau Bokan)", "42Db9MjkKCYzTwTFgwJo3j") } },
        };
    }
}
