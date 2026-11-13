using System.Collections.Generic;

namespace EverythingWorld.World
{
    /// The Synthwave Coast's songs by artist (generated from scenes.json; part of SynthKit), so every one of them can have
    /// its own light (SynthKit.Song, SynthKit.SongLamps).
    public partial class SynthKit
    {
        static readonly Dictionary<string, (string title, string id)[]> SongIds = new Dictionary<string, (string, string)[]>
        {
            { "Blood Orange", new[] { ("Best to You", "1p2TWJxUlHE5JR0sE6E1d7") } },
            { "Cannons", new[] { ("Light as a Feather", "7pucKxQeLhLzu84oZLnkHf") } },
            { "FM-84", new[] { ("Arcade Summer", "6Vy3rIRe1ANhRLm6vL1nGB") } },
            { "Futurecop!", new[] { ("We Belong", "1XZtRlKJLsoKnDEiqa1tsy") } },
            { "Gost", new[] { ("Genesee Avenue", "5Wrd7eU7jcJPoZEU1Z26ow") } },
            { "Haute & Freddy", new[] { ("Femme Hysteria", "2g8y30PtQ3jO7fR4wKQaOU") } },
            { "Home", new[] { ("Nosebleed", "6zz0INaLVknRW6W7u1wI7t") } },
            { "Love Spells", new[] { ("Keep It To Yourself", "0UUnMVRHGJKFq4mlsld8GV") } },
            { "NINA", new[] { ("Beyond Memory", "7zmbtfKgZMTQ1GlqBno5T5") } },
            { "ODESZA", new[] { ("A Moment Apart", "59wlTaYOL5tDUgXnbBQ3my") } },
            { "Panama", new[] { ("Always", "4jozjPDv6YVVDmDqDQqyTf") } },
            { "Parallels", new[] { ("Dry Blood", "7lu5cs8mhVWLYsB90yvQRf") } },
            { "Radio Wolf", new[] { ("Journeys End", "4UATyG3pA9HjqEv0gnZPtr") } },
            { "SG Lewis", new[] { ("Feelings Gone (feat. London Grammar)", "7qSaRUz9tOTtjzzifPL2Jv") } },
            { "The Midnight", new[] { ("Days of Thunder", "28uitw2AXQQIUHvSJW9qy6"), ("Neon Odyssey", "2mE4Uei06p37of4KmaLXRy"), ("Vampires", "5lVsAfvY2ij8A2T6oYdAyt") } },
            { "Washed Out", new[] { ("Feel It All Around", "6IE47jpPeatF2Iay7GZtEc") } },
            { "YELLOW MAGIC ORCHESTRA", new[] { ("RYDEEN", "0RAXoT7hwOoqAXI95ryxT5") } },
        };
    }
}
