using System.Collections.Generic;

namespace EverythingWorld.World
{
    /// The Punk Badlands' songs by artist (generated from scenes.json; part of PunkKit), so every one of them
    /// can have its own light (PunkKit.Song, PunkKit.SongTags).
    public partial class PunkKit
    {
        static readonly Dictionary<string, (string title, string id)[]> SongIds = new Dictionary<string, (string, string)[]>
        {
            { "Begin Again", new[] { ("I Ws Nvr Yr Grlfrnd", "7wPWyqbz7ZMOdwgl8lj7Zg") } },
            { "Belvedere", new[] { ("Good Grief Retreat", "21KHmVTtJl6YUumZK4DD77") } },
            { "blink-182", new[] { ("What's My Age Again?", "5JZcX7TTLx4l0xFIXJ3DBt") } },
            { "Bowling For Soup", new[] { ("1985", "4UuHWIkAWnN176A2rx7YFB") } },
            { "Die Toten Hosen", new[] { ("Hier kommt Alex", "1zs0BxFFLfbJQTSo84rkhy") } },
            { "Fall Out Boy", new[] { ("Dance, Dance", "0a7BloCiNzLDD9qSQHh5m7"), ("Heaven, Iowa", "69D0E2HjmuE82mGStp0D7Y"), ("Love From The Other Side", "5zqdoQXjMmqb1GyzTKa4E9"), ("Sugar, We're Goin Down", "2TfSHkHiFO4gRztVIkggkE"), ("Thnks fr th Mmrs", "3Zwu2K0Qa5sT6teCCHPShP"), ("Uma Thurman", "5PUawWFG1oIS2NwEcyHaCr") } },
            { "Goldfinger", new[] { ("99 Red Balloons", "0rOTMSSa6yscfOrGnzfheM") } },
            { "Green Day", new[] { ("American Idiot", "6nTiIhLmQ3FWhvrGafw2zj"), ("Basket Case", "6L89mwZXSOwYl76YXfX13s"), ("Good Riddance (Time of Your Life)", "6ORqU0bHbVCRjXm9AjyHyZ"), ("Holiday", "4VgEM12OeaN3GyBV487Me7"), ("Holiday / Boulevard of Broken Dreams", "0HJRAM7Gt9jXskuXjZeFX3"), ("Wake Me up When September Ends", "3ZffCQKLFLUvYM59XKLbVm"), ("When I Come Around", "1Dr1fXbc2IxaK1Mu8P8Khz") } },
            { "Jimmy Eat World", new[] { ("The Middle", "6GG73Jik4jUlQCkKg9JuGO") } },
            { "Less Than Jake", new[] { ("Gainesville Rock City", "527mMOhIzkT4wTBKUChkX7") } },
            { "Mayday Parade", new[] { ("I Swear This Time I Mean It", "2OYTFaOB4D1bWLXaT5mrfH"), ("Miserable At Best", "1ONoPkp5XIuw3tZ1GzrNKZ") } },
            { "My Chemical Romance", new[] { ("Dead!", "0uukw2CgEIApv4IWAjXrBC"), ("Welcome to the Black Parade", "5wQnmLuC1W7ATsArWACrgW") } },
            { "Neck Deep", new[] { ("Staircase Wit", "0rQybU8pbJFcaUU7Mqju0z") } },
            { "Panic! At The Disco", new[] { ("Death of a Bachelor", "1BECwm5qkaBwlbfo4kpYx8"), ("Don't Threaten Me with a Good Time", "2fh3bZ8jZhMxOcfESE9nQY"), ("High Hopes", "1rqqCSm0Qe4I9rUvWncaom"), ("I Write Sins Not Tragedies", "4bPQs0PHn4xbipzdPfn6du"), ("Nine in the Afternoon - Single Mix", "1S30kHvkkdMkcuCTGSgS41") } },
            { "Patti Smith", new[] { ("Gloria: In Excelsis Deo", "272Z4tzaSMMcgrZ0R494S2") } },
            { "Plain White T's", new[] { ("Hey There Delilah", "4RCWB3V8V0dignt99LZ8vH") } },
            { "Point North", new[] { ("RECOVER", "2k7lPTbMXcVLDoHqTE2Yzt") } },
            { "Reel Big Fish", new[] { ("Sell Out", "6SJ1hWFVBIiUgGgHKvwXCQ") } },
            { "Streetlight Manifesto", new[] { ("Point / Counterpoint", "1NM2qI63ylqg62yxpEvTXu"), ("The Blonde Lead the Blind", "1M6aT0VOjcXPkFmjm2yBZd") } },
            { "Strung Out", new[] { ("The Animal and the Machine", "53RMp6kzJvM9v1sw1dBCgE") } },
            { "SWMRS", new[] { ("Lose It", "0Ceozg27V0Ws9jgQixD8fr") } },
            { "The Clash", new[] { ("Charlie Don't Surf - Remastered", "5T4j3Uv33fQO7tsR2lMl4y"), ("Rock the Casbah - Remastered", "56KqaFSGTb7ifpt16t5Y1N") } },
            { "The Get Up Kids", new[] { ("I'm a Loner Dottie, a Rebel", "5TltGtLMjveIan5HvLAh1z") } },
            { "The Interrupters", new[] { ("She's Kerosene", "60FJeNTK5edr72G2xtCWuP") } },
            { "The Offspring", new[] { ("Pretty Fly (For A White Guy)", "3SFXsFpeGmBTtQvKiwYMDA"), ("The Kids Aren't Alright", "4EchqUKQ3qAQuRNKmeIpnf"), ("Why Don't You Get A Job", "0sNKiz82ATCvT3f3XVVUUj"), ("You're Gonna Go Far, Kid", "6TfBA04WJ3X1d1wXhaCFVT") } },
            { "The Story So Far", new[] { ("Nerve", "1DlT3Udf11689ezYr6R8aA") } },
        };
    }
}
