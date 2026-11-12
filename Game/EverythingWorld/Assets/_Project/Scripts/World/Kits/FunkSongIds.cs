using System.Collections.Generic;

namespace EverythingWorld.World
{
    /// The Funk Tropics's songs by artist (generated from scenes.json; part of FunkKit), so every one of them can have
    /// its own light (FunkKit.Song, FunkKit.SongLamps).
    public partial class FunkKit
    {
        static readonly Dictionary<string, (string title, string id)[]> SongIds = new Dictionary<string, (string, string)[]>
        {
            { "Average White Band", new[] { ("Pick Up the Pieces", "2x1LQq8lsUzAA2wNj8yjC9") } },
            { "Commodores", new[] { ("Brick House", "5VJjhHyG8NZ5xdgG6uTb3P"), ("Easy", "65e06mBpNFN6HAetXnsRYh") } },
            { "Curtis Mayfield", new[] { ("Pusherman", "7zrPswSV1yxrill5OyCuaU") } },
            { "Flea", new[] { ("Maggot Brain", "3poJZBl2yoeWbMMxX63YcE") } },
            { "Funk Factory - Michael Urbaniak", new[] { ("Rien Ne Va Plus", "7EsnZSIx0pUN5786tI7raq") } },
            { "Funkadelic", new[] { ("Maggot Brain", "5WJU527RQNyMLuKecjsL8V"), ("Red Hot Mama", "2a9wXgPKioc8bX1x3PkdSt") } },
            { "Ginger Root", new[] { ("Loretta", "4v3icU8NQfjM8uwIz9Ug1h") } },
            { "Incredible Bongo Band", new[] { ("Apache", "51ml2bJs9zDLv1PbzNQzPP") } },
            { "James Brown", new[] { ("People Get Up And Drive Your Funky Soul - Remix", "5U3i59kbTLrxo46TU1FRnF"), ("The Payback", "5OcyX5eg7VrJuI833qOCUc") } },
            { "Kaelin Ellis", new[] { ("Move", "50rXrrhxikEix1NFRtJ6zQ") } },
            { "Lafayette Afro Rock Band", new[] { ("Malik - Remastered", "1YjXMR4JHkmjnDrnAEfMg6") } },
            { "Lettuce", new[] { ("Phyllis", "1wZc8QBnlxQZbE6lZc6QOy") } },
            { "Parliament", new[] { ("Flash Light", "1v1PV2wERHiMPesMWX0qmO"), ("Give Up The Funk (Tear The Roof Off The Sucker)", "4XRkQloZFcRrCONN7ZQ49Y") } },
            { "Prince", new[] { ("1999 - 2019 Remaster", "2QSUyofqpGDCo026OPiTBQ"), ("Purple Rain", "54X78diSLoUDI3joC2bjMz"), ("Raspberry Beret", "2I1W3hcSsPIsEJfD69pHrW") } },
            { "Shuggie Otis", new[] { ("Inspiration Information", "3q8AgabNzK1mPJdI8egIcn") } },
            { "The Cactus Channel", new[] { ("Stay a While", "7EdFmnGr4QvCpuMtcP1AC0") } },
            { "The Meters", new[] { ("Fire on the Bayou", "0BRETWVsgLUOyuw1GQdhiG"), ("Hey Pocky A-Way - 2002 Remaster", "3QWref6eXBTcypIrVvFSTX"), ("It Ain't No Use", "3MaqEiVpKGxqd2JhMH9xR7"), ("Just Kissed My Baby", "1TB0dZqFRQsnf4fLLLRKQV") } },
            { "Thundercat", new[] { ("Dragonball Durag", "7eWGnKg4B44sbBPpQp4y2c"), ("Funny Thing", "1c5CllrZr45UBhWpIqM332"), ("Them Changes", "7CH99b2i1TXS5P8UUyWtnM") } },
            { "Voilaaa", new[] { ("Spies Are Watching Me", "1UIqpCB0b56K7U0JJPfskN") } },
        };
    }
}
