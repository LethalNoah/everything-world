using System.Collections.Generic;

namespace EverythingWorld.World
{
    /// The Heartbreak Boardwalk's songs by artist (generated from scenes.json; part of HeartKit), so every one of them can have
    /// its own light (HeartKit.Song, HeartKit.SongLamps).
    public partial class HeartKit
    {
        static readonly Dictionary<string, (string title, string id)[]> SongIds = new Dictionary<string, (string, string)[]>
        {
            { "Ashe", new[] { ("Funeral", "6nQqP66npzndHw2KXIPyUZ"), ("Lasso The Moon", "0ibJ83Ei65gTmiV0aHZg47"), ("Stop The Wedding!", "7MOO89Qv9AV7VZkt5mcqA8") } },
            { "Avril Lavigne", new[] { ("Sk8er Boi", "00Mb3DuaIH1kjrwOku9CGU") } },
            { "Billie Eilish", new[] { ("Happier Than Ever", "4RVwu0g32PAqgUiJoXsdF8"), ("No Time To Die", "73SpzrcaHk0RQPFP73vqVR"), ("ocean eyes", "2uIX8YMNjGMD7441kqyyNU") } },
            { "Halsey", new[] { ("Carry the Weight", "7p3oJMceNz5skGLw52ZOjY") } },
            { "Harry Styles", new[] { ("Sign of the Times", "5Ohxk2dO5COHF1krpoPigN") } },
            { "Holly Humberstone", new[] { ("Beauty Pageant", "5bvK9yntY8XIcNkTKSSNYr") } },
            { "Lana Del Rey", new[] { ("Black Beauty", "63hHlajVLQnlFMAqSyePxO"), ("Brooklyn Baby", "1NZs6n6hl8UuMaX0UC0YTz"), ("Cinnamon Girl", "2mdEsXPu8ZmkHRRtAdC09e"), ("Happiness is a butterfly", "3lG6OtGDsYAOALxEmubQQm"), ("Mariners Apartment Complex", "6OG05bPAwUuV3OMvy2Vy1P"), ("Norman fucking Rockwell", "3RIgHHpnFKj5Rni1shokDj"), ("Salvatore", "21qg0IBZf8R12qHd9A3AA4"), ("Say Yes To Heaven", "6GGtHZgBycCgGBUhZo81xe"), ("Summertime Sadness", "1Ist6PR2BZR3n2z2Y5R6S1"), ("Ultraviolence", "1y3r6RXiJZNBV1EI0NggpS"), ("West Coast", "5Y6nVaayzitvsD5F7nr3DV") } },
            { "Lola Young", new[] { ("Messy", "3SKH53SPQbEnZR4cJPVaz2") } },
            { "Maroon 5", new[] { ("Payphone", "1LmN9SSHISbtp9LoaR5ZVJ") } },
            { "Meredith Brooks", new[] { ("Bitch", "3i6qNxyVgIdUZTTi5m25EM") } },
            { "Olivia Rodrigo", new[] { ("All I Want - From \"High School Musical: The Musical: The Series\"", "4Yxc55NX3tAXC2mHRAhtcW"), ("all-american bitch", "34sOdxWu9FljH84UXdRwu1"), ("cigarette smoke", "0e4u0sFRGxHL4U2iFBrUvZ"), ("good 4 u", "4ZtFanR9U6ndgddUvNcjcG") } },
            { "OneRepublic", new[] { ("Counting Stars", "6sy3LkhNFjJWlaeSMNwQ62") } },
            { "Suki Waterhouse", new[] { ("Good Looking", "026wpXkLAjImiWOzzcJBHj") } },
            { "Tal Bachman", new[] { ("She's so High", "7mnGQesk1TzQLzQ9bYWZPR") } },
        };
    }
}
