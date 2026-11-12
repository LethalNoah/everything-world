using System.Collections.Generic;

namespace EverythingWorld.World
{
    /// The Djent Spires' songs by artist (generated from scenes.json; part of DjentKit), so every one of them
    /// can have its own light (DjentKit.Song, DjentKit.SongShards).
    public partial class DjentKit
    {
        static readonly Dictionary<string, (string title, string id)[]> SongIds = new Dictionary<string, (string, string)[]>
        {
            { "Angel Vivaldi", new[] { ("Eight", "6mY0cMHPRW5zom3nurTxMr") } },
            { "Anima Tempo", new[] { ("Primal Symmetry", "1C1ue5mtAf9MMOPGQm6OZt") } },
            { "Animals As Leaders", new[] { ("Odessa", "0KU0ZSnnPo5QjncWPz1g1f") } },
            { "Arch Echo", new[] { ("Color Wheel", "10pIIWp1LGrtoRpvwMGLHm") } },
            { "Caligula's Horse", new[] { ("Marigold", "4I5NcGVjREt60IThG2GGu9") } },
            { "Coheed and Cambria", new[] { ("Welcome Home", "42GP0xKtkolBnmqQRvSllO") } },
            { "David Maxim Micic", new[] { ("Living Room", "1f6rRePaUvX8aEVbXwi3ZY") } },
            { "Destiny Potato", new[] { ("Walls of Thoughts", "79InGa7AAQJeOxtDWkOJgi") } },
            { "DGM", new[] { ("Second Chance", "3B5r18X9InnMunlIeWIJ0I") } },
            { "DVSR", new[] { ("Six Figures Deep", "4jRBJjBuQdtEVfChsEKOps") } },
            { "Fernando Molinari", new[] { ("Control", "1t1xdY7Xwcnd1mprrjQ8nt") } },
            { "Intervals", new[] { ("D.O.S.E.", "4Rkf1dl1EC2r4Me0ht2xDQ") } },
            { "Leprous", new[] { ("Below", "6rIwcMGcZtPdpi6U9llpb0"), ("Castaway Angels", "3Gy5q1K4nTwDVVtyQWYv3t"), ("The Sky Is Red", "7cqnK70AUCBypcveB7OCs1") } },
            { "Liquid Tension Experiment", new[] { ("Biaxident", "4M6UwQO3CqWVqTirfZspgr") } },
            { "Opeth", new[] { ("Windowpane", "4uJSCrI7r0usNJ3aaHAuC6") } },
            { "Outrun The Sunlight", new[] { ("Red Bird", "2L5oeVqLk6YjN3hxqfaMH9") } },
            { "Plini", new[] { ("Blue Angel", "0bkOlnwhJmPt7liffxaICA"), ("Electric Sunrise", "28uTV1xcMTcIPTsKVbyhFV"), ("Pan", "4S0iFqlxNbwThYQWRgvBDB") } },
            { "Polyphia", new[] { ("Light", "3YMT7YnliDwUzG3FL4y4kq") } },
            { "Sleep Token", new[] { ("The Summoning", "3hql0mbMMRqo3V3XfrTu73") } },
            { "StarSystems", new[] { ("It's Raining on the Moon, Pt. 1", "5K8hsK21BinXBIqrSeEC2L") } },
            { "The Omnific", new[] { ("Objets de Vertu", "3ykYzbCnEmzzZfDrxSQQtK") } },
            { "Unprocessed", new[] { ("Ruins", "5oFP3acS6IqMXK9SG0qXUF") } },
        };
    }
}
