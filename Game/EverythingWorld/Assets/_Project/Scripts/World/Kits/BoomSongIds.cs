using System.Collections.Generic;

namespace EverythingWorld.World
{
    /// Boom Bap Harbour's songs by artist (generated from scenes.json; part of BoomKit), so every one of them can have
    /// its own light (BoomKit.Song, BoomKit.SongLamps).
    public partial class BoomKit
    {
        static readonly Dictionary<string, (string title, string id)[]> SongIds = new Dictionary<string, (string, string)[]>
        {
            { "A Tribe Called Quest", new[] { ("Can I Kick It?", "1gSDTZHemEViEBiXysrlo0") } },
            { "AZ", new[] { ("Mo Money Mo Murder (Homicide)", "62YPBeWVcD1FDWo7yTBXix") } },
            { "Beastie Boys", new[] { ("Sabotage", "0Puj4YlTm6xNzDDADXHMI9") } },
            { "Cannibal Ox", new[] { ("Iron Galaxy", "1OKZqBMP3JK8LkpXVKNEr5") } },
            { "Ceren Yaldız", new[] { ("Ara Ara", "5kHgimnV68HjsbHLzqp4q9") } },
            { "Diddy", new[] { ("I'll Be Missing You", "25tsnA5lDP4UctAQOoa8Ks") } },
            { "Fugees", new[] { ("Killing Me Softly With His Song", "0Q0IVlqMV64kNLlwjPj0Hl") } },
            { "Grandmaster Flash", new[] { ("The Message", "5bvNTjZl2XURJEDHTkN71h") } },
            { "JAŸ-Z", new[] { ("99 Problems", "7sLpSWxQazJzDVG6YGzlVs"), ("Empire State Of Mind", "2igwFfvr1OAGX9SKDCPBwO") } },
            { "Kurtis Blow", new[] { ("Basketball", "17qPnfty4qHRnAxJKazvEt") } },
            { "Outkast", new[] { ("B.O.B. - Bombs Over Baghdad", "3WibbMr6canxRJXhNtAvLU"), ("Hey Ya!", "2PpruBYCo4H7WOBJ7Q2EwM"), ("Ms. Jackson", "0I3q5fE6wg7LIfHGngUTnV") } },
            { "RZA", new[] { ("Quentin Tarantino", "2LjEovfvxovl807CqAoJS8") } },
            { "Shabazz Palaces", new[] { ("Swerve... the reeping of all that is worthwhile (Noir not withstanding)", "6FDVvFYeywoxfp0DtQZDOR") } },
            { "T La Rock", new[] { ("It's Yours (Radio Mix)", "5Olg0sqyi5XGo7lMLNeRAy") } },
            { "The Notorious B.I.G.", new[] { ("Big Poppa - 2005 Remaster", "2g8HN35AnVGIk7B8yMucww"), ("Juicy", "2AP7m2dBb8ULTx4Gc1rdMc"), ("Juicy - 2005 Remaster", "5ByAIlEEnxYdvpnezg7HTX") } },
            { "Wu-Tang Clan", new[] { ("C.R.E.A.M. (Cash Rules Everything Around Me) (feat. Method Man, Raekwon, Inspectah Deck & Buddha Monk)", "2hcnWf9j7TPl3tQWMCmdvf"), ("Da Mystery of Chessboxin' (feat. Method Man, U-God, Inspectah Deck, Raekwon, Ol' Dirty Bastard, Ghostface Killah & Masta Killa) - Radio Edit", "6hrnXzJeuXE2epZHTHiFMD"), ("Method Man (feat. Method Man, Raekwon, GZA, RZA & Ghostface Killah)", "4Ljm0j7lyA4wZ6xpd5N4AO"), ("Protect Ya Neck (feat. RZA, Method Man, Inspectah Deck, Raekwon, U-God, Ol' Dirty Bastard, Ghostface Killah & GZA)", "2OKIlDMpZD66xVmdlt1zzx"), ("Wu-Tang: 7th Chamber - Part II - Conclusion", "4mkdUdHTDfnbcie1vSmbCK") } },
        };
    }
}
