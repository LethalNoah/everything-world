using System.Collections.Generic;

namespace EverythingWorld.World
{
    /// The Far Shores's songs by artist (generated from scenes.json; part of FarKit), so every one of them can have
    /// its own light (FarKit.Song, FarKit.SongLamps).
    public partial class FarKit
    {
        static readonly Dictionary<string, (string title, string id)[]> SongIds = new Dictionary<string, (string, string)[]>
        {
            { "Bossario", new[] { ("By the Sea", "0mqsJVr05jzGQhvEkFuC3D") } },
            { "Dave Richmond", new[] { ("Visit to Motown", "53HJ7TmC9HynQ3Huea1lpF") } },
            { "Elis Regina", new[] { ("Águas De Março", "15ouqiBsgL12olEwP3COsH") } },
            { "Esperanza Spalding", new[] { ("Judas", "4Oln7MnB1f8nE5Ot4ySooV") } },
            { "Harry Belafonte", new[] { ("Banana Boat (Day-O)", "4fHDlIntTsRGSyTg5UYZYC") } },
            { "Herb Alpert & The Tijuana Brass", new[] { ("Spanish Flea", "3ZwIgfynY9l03MU7x4P5h7") } },
            { "Jorge Ben Jor", new[] { ("Mas, Que Nada!", "6U03Orwr5Dxt8jahLnQpYV"), ("Meus Filhos, Meu Tesouro", "21qLDWpxc2uHHTR89a47kO"), ("O Filósofo", "41eIpNKK7qiZP2SYSFSHtD") } },
            { "Keith Mansfield", new[] { ("Funky Fanfare", "3Vr406z58yyyO2Ofh1fbqA") } },
            { "Marcos Valle", new[] { ("Estrelar", "2koS4fD3kzizdnzWzyrxyT") } },
            { "Mulatu Astatke", new[] { ("Yègellé Tezeta", "0fxjsll9jmmRgMFbwdp4Xg"), ("Yègellé Tezeta (My Own Memory)", "0nIfIiTrlOeiFEYpVCJZtY"), ("Yèkèrmo Sèw", "4CwAgkBoKAGT6UMas89F0v") } },
            { "Robert Stillman", new[] { ("It's All Is", "0CbQpokPLu2TrHzLjv4snw") } },
            { "Rodrigo y Gabriela", new[] { ("Tamacún", "0nNwfRyw0eY323NMkBM0y8") } },
            { "Santo & Johnny", new[] { ("Sleep Walk", "0yK8sWD6cfrc7pGfaPIcZH") } },
            { "Tinariwen", new[] { ("Cler Achel", "1sXLitmQULzwiWR8D38Ekw"), ("Nànnuflày", "6i2a4jGE9FDvENdZYFDgGz"), ("Toumast Tincha", "4aHv6x8P4EEWkIAkbQkIBh") } },
            { "Tony Bennett", new[] { ("Body and Soul", "01hJnhpAmjzg85Etnz2ECH") } },
        };
    }
}
