using System.Collections.Generic;

namespace EverythingWorld.World
{
    /// The Latin Coast's songs by artist (generated from scenes.json; part of LatinKit), so every one of them can have
    /// its own light (LatinKit.Song, LatinKit.SongLamps).
    public partial class LatinKit
    {
        static readonly Dictionary<string, (string title, string id)[]> SongIds = new Dictionary<string, (string, string)[]>
        {
            { "Bad Bunny", new[] { ("Me Porto Bonito", "6Sq7ltF9Qa7SNFBsV5Cogx"), ("Tití Me Preguntó", "1IHWl5LamUGEuP4ozKQSXZ") } },
            { "Camila Cabello", new[] { ("Havana (feat. Young Thug)", "1rfofaqEpACxVEHIZBJe6W") } },
            { "Don Omar", new[] { ("Danza Kuduro", "2a1o6ZejUi8U3wzzOtCOYw") } },
            { "Edgardo Nuñez", new[] { ("Renacer", "3xu6gF8R4fa0ghzvj67aI6") } },
            { "Eslabon Armado", new[] { ("Ella Baila Sola", "3dnP0JxCgygwQH9Gm7q7nb") } },
            { "Feid", new[] { ("Normal", "0T2pB7P1VdXPhLdQZ488uH") } },
            { "Los Bukis", new[] { ("Mar De Soledad", "0c1MAj86fBT8ce31cnSxQr") } },
            { "Los Del Rio", new[] { ("Macarena", "4oaj36KzXRgDg4McgcTsZK") } },
            { "Los Lobos", new[] { ("La Bamba", "0uMMLry3hzWGn3q3loqMkm"), ("When the Circus Comes", "2svD20rFaTIFzJvtOmvveY") } },
            { "Miami Sound Machine", new[] { ("Words Get In the Way", "5xbqrbNPBTAOGDA6GD3O46") } },
            { "Natalia Lafourcade", new[] { ("Ella es Bonita", "3GLa0YFGyR9vL33Bw2kjHs"), ("Hasta la Raíz", "3lGMtkONrZdJ8kTCg6KIFf") } },
            { "Nico Play", new[] { ("La Ciruela", "1009LeU2Wb7rDvbNnN4pXT"), ("La Ciruela", "1DVpjp2AEVJH3WEdQtlPdp") } },
            { "Rafa", new[] { ("Ideally Attached", "4qIzZXT8dNM8IUwC9GKbBf") } },
            { "Shakira", new[] { ("Can't Remember to Forget You (feat. Rihanna)", "7o1Pm9jpH0wFpN5g793Lnq"), ("Hips Don't Lie (feat. Wyclef Jean)", "3d0WouFnFmr0K3kjeza3fF"), ("Whenever, Wherever", "2lnzGkdtDj5mtlcOW2yRtG") } },
            { "Shawn Mendes", new[] { ("Señorita", "6v3KW9xbzN5yKLt9YKDYA2") } },
            { "The Mariachis", new[] { ("La Cucaracha", "3rgPxzkWK65kjWtP8pkRGL") } },
        };
    }
}
