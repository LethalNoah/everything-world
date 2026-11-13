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
            { "Erika Henningsen", new[] { ("Stupid With Love", "2fB3nWTsj1Aba2oJsAiTck") } },
            { "Idina Menzel", new[] { ("Let It Go - From \"Frozen\"/Soundtrack Version", "0qcr5FMsEO85NAQjrlDRKo") } },
            { "Joe \"Bean\" Esposito", new[] { ("You're The Best", "4zXa17K83Pp6N2yXdVc2sv") } },
            { "Kishore Kumar", new[] { ("Mere Sapnon Ki Rani", "098jYib9xwJFe6W4eIL5Ok"), ("Wada Karo - From \"Aa Gale Lag Jaa\"", "6jsh1e9MRqbfy8tI38f71k") } },
            { "Phil Harris", new[] { ("The Bare Necessities - From \"The Jungle Book\" Soundtrack", "7wbbw0cwvQUnGYautJ2Eun") } },
            { "Phineas and Ferb", new[] { ("Ain't Got Rhythm", "3F58l9SSJF6QDsN1tw4cJX"), ("Disco Miniature Golfing Queen", "3iqbmQrLIkHcBvSt2LOgi7"), ("Gitchee Gitchee Goo", "7xOuAIh0n2ftmVmK4UBHub"), ("I'm Lindana and I Wanna Have Fun", "3uJ0Sma98KPcto6erXN6O6"), ("Kick It Up A Notch", "7bVJcOojfwAUVoLA6wVKyf"), ("S.I.M.P. (Squirrels in My Pants)", "7ymrbUUrrqvUKBMmdRvieZ"), ("Summer Belongs to You", "18OvaNHgJBhNJN2Qo7FWFd"), ("You Snuck Your Way Right Into My Heart", "45MJXr9uooeBZJbXRegFMB") } },
            { "Piero Umiliani", new[] { ("Mah nà mah nà", "3Mz27X48Ey7MbfLOmSzkBK") } },
            { "Pritam", new[] { ("Kesariya (From \"Brahmastra\") - Tu Fuljhadiyo Wale Tyohaaro Jesi - Happy Diwali", "6VBhH7CyP56BXjp8VsDFPZ") } },
            { "Sharpay Evans", new[] { ("Fabulous", "6jyXQ8HGI7ikECGA0670uo") } },
            { "SpongeBob SquarePants", new[] { ("Campfire Song Song", "3LHMFgMolEbhhGBC47OuAi") } },
            { "The Disneyland Children's Chorus", new[] { ("It's a Small World - From \"It's a Small World\"", "5ouuPi1o6eOVSgHn2exemw") } },
            { "The Intermezzo Orchestra", new[] { ("Pirates Of The Caribbean (Main Theme)", "7FCdQ7d58Hal33Ce4t5RYd") } },
            { "The Lion King", new[] { ("Be Prepared", "34nxgXFCPzDphUJGuRsn10"), ("Can You Feel the Love Tonight - End Title", "0EANX0OVKSCcmarY50Xa4p"), ("Hakuna Matata", "5k3U0OGYBccHdKJJu3HrUN"), ("I Just Can't Wait to Be King", "5QsEvml3ZEnISKBEaKueUy"), ("The Lion Sleeps Tonight", "67rctxgNOXUYhKiS3cv0MT") } },
            { "The Muppets", new[] { ("Rainbow Connection", "1SJ4ycWow4yz6z4oFz8NAG") } },
            { "The Simpsons", new[] { ("Do The Bartman", "5iM1Csh4jiBHdt0TCnjB4Z") } },
            { "Tori Kelly", new[] { ("Colors Of The Wind - From \"Pocahontas\"", "7qgM0MdETmhd3mORTqvJRm") } },
            { "Troy", new[] { ("Bet On It", "4EnwhEyuVrC1CgvSur5YL4") } },
            { "Write Out Loud", new[] { ("Little Miss Perfect", "2z1xTVeAvEIdniWEnoGeAH") } },
            { "Zac Efron", new[] { ("Gotta Go My Own Way", "5rWIO1uP0GwAoU45RTBJ2F"), ("What Time Is It", "6NqfslFfCCOyBQ0Xw8kN8E") } },
        };
    }
}
