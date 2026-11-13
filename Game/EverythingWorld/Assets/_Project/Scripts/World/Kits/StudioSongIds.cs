using System.Collections.Generic;

namespace EverythingWorld.World
{
    /// Studio Lot Island's songs by artist (generated from scenes.json; part of StudioKit), so every one of them can have
    /// its own light (StudioKit.Song, StudioKit.SongLamps).
    public partial class StudioKit
    {
        static readonly Dictionary<string, (string title, string id)[]> SongIds = new Dictionary<string, (string, string)[]>
        {
            { "A.R. Rahman", new[] { ("Jai Ho! (You Are My Destiny)", "7Kpqjspw4Y7HrvItIRcBiW") } },
            { "Alan Menken", new[] { ("Under the Sea", "6oYkwjI1TKP9D0Y9II1GT7") } },
            { "Alberto Pomeranz", new[] { ("Il barone rosso", "49UzbfgLtI4INuA0TpSvij") } },
            { "Arijit Singh", new[] { ("Tum Hi Ho", "56zZ48jdyY2oDXHVnwg5Di") } },
            { "Carmen Twillie", new[] { ("Circle of Life - From \"The Lion King\" Soundtrack", "7bhfPxNDc3Wtzu5Fx8FnNx") } },
            { "Carolina Gaitán - La Gaita", new[] { ("We Don't Talk About Bruno", "52xJxFP6TqMuO4Yt0eOkMz") } },
            { "Catherine O'Hara", new[] { ("Sally's Song", "0Zj3Ihs4nn3G3xZ2LKLEbP") } },
            { "Daniele Zandara", new[] { ("Copper Green Intent", "2eFQmVD4rNeww373N6UZZY"), ("Moms are Tough", "3hKMIUdfMCFN5cPnsSg27x"), ("Red & Blue", "5g3adjeEh64WNW2LDf4Lci"), ("Reincarnated Echoes", "72DFAaDLMzHbdAaHNs62up") } },
            { "Filippo Vicarelli", new[] { ("I'm Every Reaper", "6joNhaPTUdOvZO0cg6Q2ul") } },
            { "Fonola Band", new[] { ("Bella ciao", "5XYPTwya4YqPystALy9cLJ") } },
            { "Geek Music", new[] { ("Jeopardy! Main Theme (From \"Jeopardy!\")", "4qkYiZablQoG7f0Qu4Nd1c") } },
            { "Idina Menzel", new[] { ("Let It Go - From \"Frozen\"/Soundtrack Version", "0qcr5FMsEO85NAQjrlDRKo") } },
            { "Joe \"Bean\" Esposito", new[] { ("You're The Best", "4zXa17K83Pp6N2yXdVc2sv") } },
            { "Kishore Kumar", new[] { ("Mere Sapnon Ki Rani", "098jYib9xwJFe6W4eIL5Ok"), ("Wada Karo - From \"Aa Gale Lag Jaa\"", "6jsh1e9MRqbfy8tI38f71k") } },
            { "Patryk Scelina", new[] { ("Fondue los Muertos", "0swnsTZ8tdOJ0u48iLUCCx") } },
            { "Phil Harris", new[] { ("The Bare Necessities - From \"The Jungle Book\" Soundtrack", "7wbbw0cwvQUnGYautJ2Eun") } },
            { "Piero Umiliani", new[] { ("Mah nà mah nà", "3Mz27X48Ey7MbfLOmSzkBK") } },
            { "Pritam", new[] { ("Kesariya (From \"Brahmastra\") - Tu Fuljhadiyo Wale Tyohaaro Jesi - Happy Diwali", "6VBhH7CyP56BXjp8VsDFPZ") } },
            { "Shady Cicada", new[] { ("Sweet Pea", "41ch2dnzxqGmZvuInTZhrM") } },
            { "SpongeBob SquarePants", new[] { ("Campfire Song Song", "3LHMFgMolEbhhGBC47OuAi") } },
            { "The Disneyland Children's Chorus", new[] { ("It's a Small World - From \"It's a Small World\"", "5ouuPi1o6eOVSgHn2exemw") } },
            { "The Intermezzo Orchestra", new[] { ("Pirates Of The Caribbean (Main Theme)", "7FCdQ7d58Hal33Ce4t5RYd") } },
            { "The Lion King", new[] { ("Be Prepared", "34nxgXFCPzDphUJGuRsn10"), ("Can You Feel the Love Tonight - End Title", "0EANX0OVKSCcmarY50Xa4p"), ("Hakuna Matata", "5k3U0OGYBccHdKJJu3HrUN"), ("I Just Can't Wait to Be King", "5QsEvml3ZEnISKBEaKueUy"), ("The Lion Sleeps Tonight", "67rctxgNOXUYhKiS3cv0MT") } },
            { "The Muppets", new[] { ("Rainbow Connection", "1SJ4ycWow4yz6z4oFz8NAG") } },
            { "The Simpsons", new[] { ("Do The Bartman", "5iM1Csh4jiBHdt0TCnjB4Z") } },
            { "Tori Kelly", new[] { ("Colors Of The Wind - From \"Pocahontas\"", "7qgM0MdETmhd3mORTqvJRm") } },
        };
    }
}
