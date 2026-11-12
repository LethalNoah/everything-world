using System.Collections.Generic;

namespace EverythingWorld.World
{
    /// The Power Metal Peaks' songs by artist (generated from scenes.json; part of PowerKit), so every one of them
    /// can have its own light (PowerKit.Song, PowerKit.SongLanterns).
    public partial class PowerKit
    {
        static readonly Dictionary<string, (string title, string id)[]> SongIds = new Dictionary<string, (string, string)[]>
        {
            { "Alestorm", new[] { ("Drink", "0SPHbfBIe0mdRnae2MWuzr"), ("Mexico", "6xEcJi5tmXFhaT0m62VZd9") } },
            { "BABYMETAL", new[] { ("Gimme Chocolate!!", "3AspYFTkJFk9lXyaa2xxjW") } },
            { "BRIDEAR", new[] { ("G・A・M・E", "59kX2YzJwP8z4WDZHchC07"), ("In The Labyrinth", "0k0Y0SxccfJgKXUX8AG2T2"), ("NEXUS", "1iZLxf79IuF6Ay6RiF7xCr"), ("Reason For My Life", "03tiNO2YfylYOGA14YasX2") } },
            { "DragonForce", new[] { ("My Heart Will Go On", "2XdidRMHUsStcYW1Gujcry"), ("Through the Fire and Flames", "6nnacTL5on2aVsRhVDNUSo") } },
            { "HANABIE.", new[] { ("お先に失礼します。", "51o21I12ZFahXaCviz95M6"), ("命短し対する乙女よ", "3uXfyndmsK3fRNSMr7uWrI") } },
            { "LADYBABY", new[] { ("Haten ni raimei", "72WNxQ730lDmjeCoSrgK9O"), ("Nippon Manju", "06NEuecnoNSQxDC3lfHpyy") } },
            { "Mary's Blood", new[] { ("Driver's High", "3C9V2LR3HC19mDTh8rBRJQ"), ("World's End", "4ai25DPUnzJ5VUUzWYliAr") } },
            { "NEMOPHILA", new[] { ("REVIVE", "4TmAlgqhKXdtwq6EB56pbX"), ("鬼灯", "6HnFAt2bH1rKApLlRnCZeR") } },
            { "Sabaton", new[] { ("82nd All the Way", "4zvXVO0SrMYIMHVGdOm0DA") } },
            { "Wind Rose", new[] { ("Drunken Dwarves", "4qxK1lnNoYm1kevG69mhHU"), ("Mine Mine Mine!", "2dDCZgcFGl9yj3ZEmaQM6x") } },
        };
    }
}
