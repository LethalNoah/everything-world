using System.Collections.Generic;

namespace EverythingWorld.World
{
    /// The Country Plains's songs by artist (generated from scenes.json; part of CountryKit), so every one of them can have
    /// its own light (CountryKit.Song, CountryKit.SongLamps).
    public partial class CountryKit
    {
        static readonly Dictionary<string, (string title, string id)[]> SongIds = new Dictionary<string, (string, string)[]>
        {
            { "Alabama", new[] { ("Mountain Music", "7BjmEKAVTnJNkRNaUzBy2K"), ("Song of the South", "1r2L8Z4IyRbDeYiA7YNAKx") } },
            { "Alan Jackson", new[] { ("Chattahoochee", "59kHPbwyyCApYA8RQQEuXm"), ("Where Were You (When the World Stopped Turning)", "4aOQG9TYcZOhT3sngkMI9K") } },
            { "Blake Shelton", new[] { ("God's Country", "1dyxS3YzBbL0t8rEVwVQ1K") } },
            { "Brad Paisley", new[] { ("Remind Me (with Carrie Underwood)", "4ABua0yuWcpTotImEEJTaw") } },
            { "Carrie Underwood", new[] { ("Before He Cheats", "0ZUo4YjG4saFnEJhdWp9Bt") } },
            { "Darius Rucker", new[] { ("Alright", "5JlH51YDYsGj2dyEJKKjcN"), ("Wagon Wheel", "3xdjjKMcMOFgo1eQrfbogM") } },
            { "Dasha", new[] { ("Memo", "74ULkLG9L8FiqL8PyxbsFG") } },
            { "Dolly Parton", new[] { ("9 to 5", "4w3tQBXhn5345eUXDGBWZG"), ("Jolene", "5fdhThPDe6jQQDqCyWrdAn") } },
            { "Dylan Gossett", new[] { ("Coal", "4dtIxANFBU6CfhI9c2hf2g") } },
            { "Eric Church", new[] { ("Springsteen", "05ZACkzW8YbRq3eFgIfSNB") } },
            { "Florida Georgia Line", new[] { ("Cruise", "0i5el041vd6nxrGEU8QRxy") } },
            { "Gabby Barrett", new[] { ("I Hope", "23T0OX7QOiIUFShSzbJ5Uo") } },
            { "George Strait", new[] { ("Amarillo By Morning", "4TnUKixNWMfajncgdSwFoi") } },
            { "Hank Williams", new[] { ("Your Cheatin' Heart", "0WwmFdGXe7Yky4T2J1oqLk") } },
            { "Hank Williams, Jr.", new[] { ("If Heaven Ain't A Lot Like Dixie", "2VV9KHwOfrS7ZOEzPZJFLJ"), ("Weatherman", "0XqB5gOpCdI9vV3MhKQyyX") } },
            { "Jake Owen", new[] { ("Barefoot Blue Jean Night", "3ts6xK5GzfMAAriT9AIBmP") } },
            { "Jason Aldean", new[] { ("Big Green Tractor", "0Ajm7DFmbGEURySKm7G2jf"), ("Big Green Tractor", "4KAzYqfcijCSZa2P3etHoM"), ("My Kinda Party", "215FCyLSBcXeX0NASXEjyn") } },
            { "John Anderson", new[] { ("Seminole Wind", "7xdQBiKGzfN8ripbLkihWu") } },
            { "Johnny Cash", new[] { ("God's Gonna Cut You Down", "6RFkVsPmrM4pzlDkFswwJl"), ("Hurt", "28cnXtME493VX9NOw9cIUh") } },
            { "Kacey Musgraves", new[] { ("Abilene", "1jqDLQJVbfYh525gPEZkvC") } },
            { "Kenny Rogers", new[] { ("The Gambler", "5KqldkCunQ2rWxruMEtGh0") } },
            { "Lady A", new[] { ("American Honey", "4xInIiKipU1mtUogJ3ZdYr"), ("Bartender", "6olKv2HP3XgBpvVxAswowe"), ("Need You Now", "7h70RTLbnhDyPGXjl3iEhh") } },
            { "LeAnn Rimes", new[] { ("Can't Fight The Moonlight", "2AogRMqARWyUP7VQ3gmSoY") } },
            { "Luke Bryan", new[] { ("Rain Is A Good Thing", "1YhFtqwcN138S6ng3MT1nN") } },
            { "Luke Combs", new[] { ("Where the Wild Things Are", "0eBFgRxyVSeuT4iyrbukdn") } },
            { "Maren Morris", new[] { ("The Bones", "7yFhA2fUsL2oIMWlw5DaHQ") } },
            { "Marty Robbins", new[] { ("Big Iron", "0AQquaENerGps8BQmbPw14") } },
            { "Merle Haggard", new[] { ("Mama Tried", "6h3YJ05BovN1Pgk145J3r9") } },
            { "Patsy Cline", new[] { ("Walkin' After Midnight", "7E8nKMtXMqIQbvl1Ta9Ucw") } },
            { "Rascal Flatts", new[] { ("Life is a Highway", "2Fs18NaCDuluPG1DHGw1XG") } },
            { "Reba McEntire", new[] { ("The Night The Lights Went Out In Georgia", "2MiXC2LfpsoeUP5RvMGEQ3") } },
            { "Sierra Ferrell", new[] { ("Pair of Dice", "0SIaQRxv2uyjjeWP8PV7uj"), ("Trying to Love You", "4kJ0V9jYljXTK9sWcLFzYZ") } },
            { "Sturgill Simpson", new[] { ("Call to Arms", "4m1opmaYT9zk5OP7IHUb9R") } },
            { "Sugarland", new[] { ("Stuck Like Glue", "0JcKdUGNR7zI4jJDLyYXbi") } },
            { "Tanya Tucker", new[] { ("Delta Dawn", "7GH8qNOm2gEKryVUyVzkmt") } },
            { "Tebey", new[] { ("Blinding Lights - Country Version", "6cRJTmba0JHDquftAkxUgG") } },
            { "The Band Perry", new[] { ("If I Die Young", "4u26EevCNXMhlvE1xFBJwX") } },
            { "The Charlie Daniels Band", new[] { ("The Devil Went Down to Georgia", "383Xl5QTigwj3QiA3Qc6S7") } },
            { "Tim McGraw", new[] { ("Live Like You Were Dying", "7B1QliUMZv7gSTUGAfMRRD") } },
            { "Toby Keith", new[] { ("Courtesy Of The Red, White And Blue (The Angry American)", "3OmWPC4tmUpJtLEq5xzX88"), ("Should've Been A Cowboy", "69ku805AjIcFQh0IfkGohf") } },
            { "Tyler Childers", new[] { ("All Your'n", "0fCwTrRNfoNk962ati8n2K"), ("Feathered Indians", "2tgQaL85WoRfgEa4hFQgrE"), ("Follow You To Virgie (OurVinyl Sessions)", "4Oz6xl8qhJTdjYZeT98V4m"), ("Lady May", "3t5OV1asJHmOlf23T08CLv") } },
            { "Waylon Jennings", new[] { ("Mammas Don't Let Your Babies Grow up to Be Cowboys", "7nVQ8mo77KaUvhUQzh4vMy") } },
            { "Zac Brown Band", new[] { ("Chicken Fried", "0LQtEJt7x0s6knb6RKdRYc"), ("Colder Weather", "3U8dHeggJ8IBe0UCb1gbyB"), ("Keep Me in Mind", "7B2ETOSXeXPfWQl4gLjcat"), ("Knee Deep (feat. Jimmy Buffett)", "0b2IVjeD268pYd7VUhmtKI"), ("Toes", "4S518ci8UY4op0Xyp6Hx6o") } },
            { "Zach Bryan", new[] { ("Dawns (feat. Maggie Rogers)", "42TtHTxWk782ZBiqyOi2d1"), ("Something in the Orange", "3WMj8moIAXJhHsyLaqIIHI") } },
        };
    }
}
