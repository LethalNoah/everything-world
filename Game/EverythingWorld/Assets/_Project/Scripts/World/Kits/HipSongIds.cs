using System.Collections.Generic;

namespace EverythingWorld.World
{
    /// The Hip-Hop Boroughs's songs by artist (generated from scenes.json; part of HipKit), so every one of them can have
    /// its own light (HipKit.Song, HipKit.SongLamps).
    public partial class HipKit
    {
        static readonly Dictionary<string, (string title, string id)[]> SongIds = new Dictionary<string, (string, string)[]>
        {
            { "Ab-Soul", new[] { ("Terrorist Threats (feat. Danny Brown & Jhene Aiko)", "7fU07zHBVjXu4JVry5oEsX") } },
            { "Afroman", new[] { ("Crazy Rap (Colt 45 & 2 Zig Zags)", "1ACZpHI5vZ5Ea4xGlkdGWM") } },
            { "Awkwafina", new[] { ("The Scuttlebutt", "2wLDS3oCMufFrvwN2IK3Bt") } },
            { "Bakar", new[] { ("Alive!", "6KzI6EM1QYyOq5oudPlcWT") } },
            { "Benny The Butcher", new[] { ("Johnny P's Caddy", "6AshXllQhobwSXsdpgp41w") } },
            { "Black Eyed Peas", new[] { ("I Gotta Feeling", "2H1047e0oMSj10dgp7p2VG"), ("Let's Get It Started", "37LEkmNdegFwoS55DdL6Ov") } },
            { "Cardi B", new[] { ("WAP (feat. Megan Thee Stallion)", "4Oun2ylbjFKMPTiaSbbCih") } },
            { "Central Cee", new[] { ("Doja", "3LtpKP5abr2qqjunvjlX5i") } },
            { "Childish Gambino", new[] { ("Redbone", "0wXuerDYiBnERgIpbb3JBR") } },
            { "Dave", new[] { ("Sprinter", "6NYbDTmstd7JSUWl2hVBqR") } },
            { "Denzel Curry", new[] { ("Bulls on Parade - triple j Like A Version", "6FCfj69zzQHnrx4fTukhfo") } },
            { "Dizzee Rascal", new[] { ("Fix Up, Look Sharp", "0AOeHlRaiVoSMjjoB5fB5i") } },
            { "Doechii", new[] { ("DENIAL IS A RIVER", "1eTaznNW4Xxtx9za2SMTXB") } },
            { "Eminem", new[] { ("Lose Yourself", "5Z01UMMf7V1o0MzF86s6WJ"), ("Lose Yourself", "77Ft1RJngppZlq59B6uP0z") } },
            { "Fort Minor", new[] { ("Remember the Name (feat. Styles of Beyond)", "6ndmKwWqMozN2tcZqzCX4K") } },
            { "J. Cole", new[] { ("No Role Modelz", "68Dni7IE4VyPkTOH9mRWHr") } },
            { "JID", new[] { ("Community (with Clipse, Pusha T & Malice)", "3LXxHFZWt6OHdBsgucSIjJ") } },
            { "Jamws", new[] { ("Toronto", "7gM7OSSKozedhoigEvPaQ7") } },
            { "K.Flay", new[] { ("High Enough - RAC Remix", "1cxttvflVlYD4QA34tR5Yj") } },
            { "KAYTRANADA", new[] { ("BUS RIDE", "1ZTDfHyxJJVBSkFA9h6GRs") } },
            { "Kanye West", new[] { ("Gold Digger", "1PS1QMdUqOal0ai3Gt7sDQ"), ("Runaway", "3DK6m7It6Pw857FcQftMds") } },
            { "Kendrick Lamar", new[] { ("Alright", "3iVcZ5G6tvkXZkZKlMpIUs"), ("HUMBLE.", "7KXjTSCq5nL1LoYtL7XAwS"), ("Not Like Us", "6AI3ezQ4o3HUoP6Dhudph3") } },
            { "Kid Cudi", new[] { ("Day 'N' Nite (nightmare)", "5FEXPoPnzueFJQCPRIrC3c"), ("Pursuit Of Happiness (Nightmare)", "5iSEsR6NKjlC9SrIJkyL3k") } },
            { "Lil Wayne", new[] { ("Don't Cry (feat. XXXTENTACION)", "5BQrp63SHCVf4bzCzJePne") } },
            { "Little Simz", new[] { ("Introvert", "1Tva251P6CYwQWpJOedwQ8"), ("Venom", "2tHdQjwO5zN86MLF7ZtU1X") } },
            { "Logic", new[] { ("44 Bars", "4s2BWgnSQ9NZiOQM4PP4HB"), ("Nikki", "1JFx9R87En9oJOi4DRH1e9"), ("Slave II", "4Mr73gFwZUeFnrfRov23we") } },
            { "Loyle Carner", new[] { ("Ottolenghi", "64I9byMYBlS1ARsC3vtpgW") } },
            { "Lupe Fiasco", new[] { ("Daydreamin'", "4yzXvKrnhzV39vbI9fT0ff") } },
            { "M.I.A.", new[] { ("Paper Planes", "1ixbwbeBi5ufN4noUKmW5a") } },
            { "Madvillain", new[] { ("Accordion", "67vrmPygrZ9giGqHUSReAT") } },
            { "Megan Thee Stallion", new[] { ("Thot Shit", "7FdmHr87G79PDRGy9SPBkZ") } },
            { "Missy Elliott", new[] { ("Get Ur Freak On", "6zsk6uF3MxfIeHPlubKBvR") } },
            { "Nas", new[] { ("N.Y. State of Mind", "0trHOzAhNpGCsGBEu7dOJo") } },
            { "Nicki Minaj", new[] { ("Anaconda", "794F99D5BQHS5ZGRXAs7I5"), ("Starships", "1oHNvJVbFkexQc0BpQp7Y4") } },
            { "Public Enemy", new[] { ("Fight The Power", "1yo16b3u0lptm6Cs7lx4AD") } },
            { "Sheck Wes", new[] { ("Mo Bamba", "1xzBco0xcoJEDXktl7Jxrr") } },
            { "Stormzy", new[] { ("Shut Up", "4P8dRfSz0LBgyWfjod6v3J"), ("Vossi Bop", "5DXKtoZLm31msT7tNGNHLG") } },
            { "The Streets", new[] { ("Fit but You Know It", "312A8WfROSLvZbMDHBUPDp") } },
            { "Travis Scott", new[] { ("SICKO MODE", "2xLMifQCjDGFmkHkpNLD9h") } },
            { "Tyler, The Creator", new[] { ("EARFQUAKE", "5hVghJ4KaYES3BFUATCYn0") } },
            { "Will Smith", new[] { ("Arabian Nights (2019)", "6ZxPYutGhjLTrcmvcshCa4") } },
            { "cupcakKe", new[] { ("Cpr", "35mBYyB61qTcGGj7MXIRM1"), ("Deepthroat", "5cXtT63OTadsjqfRkm5Dwd") } },
            { "mgk", new[] { ("JJK (with Yuki Chiba)", "6acCX2vGgm8mBFJ65mGySQ") } },
        };
    }
}
