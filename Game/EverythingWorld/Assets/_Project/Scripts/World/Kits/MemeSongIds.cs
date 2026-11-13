using System.Collections.Generic;

namespace EverythingWorld.World
{
    /// The Isle of Memes & Musicals's songs by artist (generated from scenes.json; part of MemeKit), so every one of them can have
    /// its own light (MemeKit.Song, MemeKit.SongLamps).
    public partial class MemeKit
    {
        static readonly Dictionary<string, (string title, string id)[]> SongIds = new Dictionary<string, (string, string)[]>
        {
            { "90s Classics", new[] { ("Everybody's Free (To Wear Sunscreen)", "6bROd4U6i4AgQGhYJonPgq") } },
            { "Axzured", new[] { ("DIDDY BLUD FUNK", "4lBMJADXNp2ymkTzT0uMqZ") } },
            { "Beedle The Bardcore", new[] { ("Lose Yourself - Medieval Bardcore Version", "1bWNOwxy5UWA7O6j6VByTX") } },
            { "Brigham Phillips", new[] { ("Ratlin' Bog", "0EaYaOmZoEIjUWzZM8YJyn") } },
            { "Burl Ives", new[] { ("A Holly Jolly Christmas", "77khP2fIVhSW23NwxrRluh") } },
            { "CHAD", new[] { ("I Don't Dance", "6p6TjiJHc1kJQt5dXzkdrs") } },
            { "CUNST", new[] { ("Temporal", "0osarfzoMVr4udc4AMwBKJ") } },
            { "Camilla and the Chickens", new[] { ("Forget You", "1UjTxvgo1rfEowwU7SJeU8") } },
            { "DJ Chipman", new[] { ("Peanut butter jelly original studio version - radio", "6Ztp4Jvpkfv1yf89EajBCb") } },
            { "Double Take", new[] { ("Hot Problems", "5lOWRR5lCnZV6eadYvbGHN") } },
            { "Ed Helms", new[] { ("How Bad Can I Be?", "4l1R5g85v3bUA1bSSbzg6w") } },
            { "Erika Henningsen", new[] { ("Stupid With Love", "2fB3nWTsj1Aba2oJsAiTck") } },
            { "First of October", new[] { ("Ravioli", "5urjNYNqbC9cDAmdZOuLpq") } },
            { "Hampton The Hampster", new[] { ("The HampsterDance Song", "0ouSkB2t2fGeW60MPcvmXl") } },
            { "Joe Siris", new[] { ("It's Okay to Leave a Dog in a Hot Car", "7JP8bPdaQ3xunbuzai8Ggu") } },
            { "John Cena", new[] { ("The Time Is Now", "1ceQ6GnflE32iovHqnsJQA") } },
            { "Kahoot!", new[] { ("Lobby Music (Original Soundtrack)", "7IEdlE4ZwzPDxnoWFv10aj") } },
            { "LeviathanJPTV", new[] { ("Chug Jug With You (Number One Victory Royale)", "15RSD04RKmM1fjrBONNtqK") } },
            { "MicroDragon", new[] { ("Bill Nye The Science Guy", "6ZTTRjw0B1z4n0J3YvhjOH") } },
            { "Nathan Evans", new[] { ("Wellerman - Sea Shanty", "54OBgO0Xwu20Jak9TMXbR7") } },
            { "PSY", new[] { ("Gangnam Style (강남스타일)", "03UrZgTINDqvnUMbbIMhql") } },
            { "Phineas and Ferb", new[] { ("Ain't Got Rhythm", "3F58l9SSJF6QDsN1tw4cJX"), ("Disco Miniature Golfing Queen", "3iqbmQrLIkHcBvSt2LOgi7"), ("Gitchee Gitchee Goo", "7xOuAIh0n2ftmVmK4UBHub"), ("I'm Lindana and I Wanna Have Fun", "3uJ0Sma98KPcto6erXN6O6"), ("Kick It Up A Notch", "7bVJcOojfwAUVoLA6wVKyf"), ("S.I.M.P. (Squirrels in My Pants)", "7ymrbUUrrqvUKBMmdRvieZ"), ("Summer Belongs to You", "18OvaNHgJBhNJN2Qo7FWFd"), ("You Snuck Your Way Right Into My Heart", "45MJXr9uooeBZJbXRegFMB") } },
            { "Pink Guy", new[] { ("Hentai", "5N6l51sD0KZKwAWJEo8dg2") } },
            { "Sharpay Evans", new[] { ("Fabulous", "6jyXQ8HGI7ikECGA0670uo") } },
            { "Shred Meyer", new[] { ("Bazooka", "5mOGQyZOwtDdeW0YIHIS3p") } },
            { "Spalexma", new[] { ("We Are Charlie Kirk", "1VTEoni0vuY5eRjPKD3aqo") } },
            { "Stantough", new[] { ("Boulevard of Broken Dreams - Medieval Style Instrumental", "5WPmvbT9Irf8MClSJXcaji"), ("Smells Like Teen Spirit", "6mv4BDicbFAG5iB3O2E9py"), ("Umbrella", "0gsXmvqT85VYfUGlcZXHsA") } },
            { "The Duck", new[] { ("The Duck Song", "2I66eI2j2ZfOe9q8TMLPbj") } },
            { "The Gregory Brothers", new[] { ("Revenge by Obama (Creeper, Aw Man)", "0LfNHsWnqrmYRTVxR9djNw") } },
            { "The Infield Boys", new[] { ("I Don't Wanna Lose Your Love Tonight", "5QNWNtpYS3GUmNi0DLtBCZ") } },
            { "The Irish Rovers", new[] { ("Drunken Sailor", "5Su5ILFp6U83cnIXwfmlC5") } },
            { "The Little Singers", new[] { ("Conjunction Junction", "0RG8jH2BDK3yB5aQPF3H9d"), ("I'm Just a Bill", "4i8KYYhQtTbvOPNsd7eS5h") } },
            { "The Moonies", new[] { ("Axel F", "7FGL2js90v5NGpIN5jdssB") } },
            { "The Muppets Barbershop Quartet", new[] { ("Smells Like Teen Spirit", "1uaSwNLLRtAvsvV6o4GWu1") } },
            { "The Whitest Kids U' Know", new[] { ("Triumph of the Ill (Hitler Rap)", "6TDfzzvzFBeEfu7hLLTugH") } },
            { "Troy", new[] { ("Bet On It", "4EnwhEyuVrC1CgvSur5YL4") } },
            { "VRIGGER", new[] { ("EPSTEIN ISLAND FUNK (SLOWED)", "47Qq8MgiuogI11YBCt2oo4") } },
            { "Vincent E. L.", new[] { ("Fuck the Fire Department", "4JcTqcHEH2R6brcSWy0u9V") } },
            { "Wesley Willis", new[] { ("Rock N Roll McDonalds", "190l7oYBQe6JBsWPJM2uNN") } },
            { "Write Out Loud", new[] { ("Little Miss Perfect", "2z1xTVeAvEIdniWEnoGeAH") } },
            { "Zac Efron", new[] { ("Gotta Go My Own Way", "5rWIO1uP0GwAoU45RTBJ2F"), ("What Time Is It", "6NqfslFfCCOyBQ0Xw8kN8E") } },
            { "yums", new[] { ("SCOTLAND FOREVER", "3SPu3n4NKYNpwFgQZAU0sl") } },
        };
    }
}
