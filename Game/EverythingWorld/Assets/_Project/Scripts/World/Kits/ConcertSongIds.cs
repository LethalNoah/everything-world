using System.Collections.Generic;

namespace EverythingWorld.World
{
    /// The Concert Isles's songs by artist (generated from scenes.json; part of ConcertKit), so every one of them can have
    /// its own light (ConcertKit.Song, ConcertKit.SongLamps).
    public partial class ConcertKit
    {
        static readonly Dictionary<string, (string title, string id)[]> SongIds = new Dictionary<string, (string, string)[]>
        {
            { "Antonio Vivaldi", new[] { ("The Four Seasons, \"La primavera\" (Spring)‚ Op. 8, No. 1", "42WsY2pXNjyzZriR7hWukl") } },
            { "Antonín Dvořák", new[] { ("Symphony No. 9 in E Minor, Op. 95, B. 178 \"From the New World\": IV. Allegro con fuoco", "6nqHzwOdGIaX57U6VU6kMO") } },
            { "Claude Debussy", new[] { ("Clair de Lune, L. 32", "5u5aVJKjSMJr4zesMPz7bL") } },
            { "Edvard Grieg", new[] { ("Morning Mood", "58qWLnRZc6McLaLQx0zU7e") } },
            { "Erik Satie", new[] { ("Gymnopédie No. 1", "5NGtFXVpXSvwunEIGeviY3") } },
            { "Franz Liszt", new[] { ("La campanella in G-Sharp Minor (From \"Grandes études de Paganini\", S. 141/3)", "2Hnurh1BcigGbCGFOb4Uid") } },
            { "Franz Schubert", new[] { ("Ave Maria, D. 839", "4JsMpgsep6XpuHSzKCTxCz") } },
            { "Frédéric Chopin", new[] { ("Ballade No. 1 in G Minor, Op. 23", "5Ks5ENUFNQDfaqxjZnCkVJ"), ("Nocturne Op. 9 No. 2", "61YM5SkqqeUjIBL7It56cs") } },
            { "George Frideric Handel", new[] { ("Hallelujah Chorus - From the \"Messiah\"", "3IpRbLPTEMXrGa2yxZWfpF") } },
            { "Gustav Holst", new[] { ("The Planets, Op. 32: I. Mars, the Bringer of War", "1C3RIRKQZhSk5d2iioIhkg") } },
            { "Igor Stravinsky", new[] { ("The Rite of Spring, K15, Pt. 1: II. The Augurs of Spring", "4odlTAKrvaQ4UUJqatxAHt") } },
            { "Johann Pachelbel", new[] { ("Canon in D", "1c3GkbZBnyrQ1cm4TGHFrK") } },
            { "Johann Sebastian Bach", new[] { ("Cello Suite No. 1 in G Major, BWV 1007: I. Prélude", "61dYvvfIRtIDFuqZypPAta"), ("Toccata and Fugue in D minor", "5cYyCE4b3T5TdQ3sGg8CYc") } },
            { "London Symphony Orchestra", new[] { ("William Tell - Overture", "5CoFV2yoZAWMlI7lhoc703") } },
            { "Ludwig van Beethoven", new[] { ("Piano Sonata No. 14 in C-Sharp Minor, Op. 27 No. 2 \"Moonlight Sonata\": I. Adagio sostenuto", "630zE7N1xPxdK2Zz4S3rPA"), ("Symphony No. 5 in C Minor, Op. 67: I. Allegro con brio", "02EjNQRJohFLY4NaXiFdH1") } },
            { "Max Mathews", new[] { ("Daisy Bell - IBM 1961 First Computer Song", "5iaGZ2ThB1gzqwj9zWg3Sn") } },
            { "Max Richter", new[] { ("On the Nature of Daylight", "56oReVXIfUO9xkX7pHmEU0") } },
            { "Penguin Cafe Orchestra", new[] { ("Music For A Found Harmonium - 2008 Digital Remaster", "4Nf4pScvTosqe9xz0FCBui") } },
            { "Philip Glass", new[] { ("Metamorphosis: One", "4MQjH7bUOKCZlJXtAlfzDK") } },
            { "Wolfgang Amadeus Mozart", new[] { ("Eine kleine Nachtmusik, K. 525: I. Allegro", "5nVahCoRVadqiNLZ8pbmrG"), ("Mozart: Requiem - Lacrimosa - Excerpt", "25ZQZns0WZJXUlQ4c9dvWu") } },
        };
    }
}
