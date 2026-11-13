using System.Collections.Generic;

namespace EverythingWorld.World
{
    /// The Isle of Memes's songs by artist (generated from scenes.json; part of MemeKit), so every one of them can have
    /// its own light (MemeKit.Song, MemeKit.SongLamps).
    public partial class MemeKit
    {
        static readonly Dictionary<string, (string title, string id)[]> SongIds = new Dictionary<string, (string, string)[]>
        {
            { "90s Classics", new[] { ("Everybody's Free (To Wear Sunscreen)", "6bROd4U6i4AgQGhYJonPgq") } },
            { "Axzured", new[] { ("DIDDY BLUD FUNK", "4lBMJADXNp2ymkTzT0uMqZ") } },
            { "Beedle The Bardcore", new[] { ("Lose Yourself - Medieval Bardcore Version", "1bWNOwxy5UWA7O6j6VByTX") } },
            { "Brad Knauber", new[] { ("Don't Mine at Night - Minecraft Parody", "68qjS7DsWSTfxuaKLcPc3j") } },
            { "Brigham Phillips", new[] { ("Ratlin' Bog", "0EaYaOmZoEIjUWzZM8YJyn") } },
            { "Burl Ives", new[] { ("A Holly Jolly Christmas", "77khP2fIVhSW23NwxrRluh") } },
            { "CHAD", new[] { ("I Don't Dance", "6p6TjiJHc1kJQt5dXzkdrs") } },
            { "CUNST", new[] { ("Temporal", "0osarfzoMVr4udc4AMwBKJ") } },
            { "Camilla and the Chickens", new[] { ("Forget You", "1UjTxvgo1rfEowwU7SJeU8") } },
            { "Captainsparklez", new[] { ("Fallen Kingdom", "0gYnzvD4ktjOm8EWPN9vhh"), ("Revenge", "23IzQljXqkHXVn6rHTKWDI") } },
            { "DJ Chipman", new[] { ("Peanut butter jelly original studio version - radio", "6Ztp4Jvpkfv1yf89EajBCb") } },
            { "Double Take", new[] { ("Hot Problems", "5lOWRR5lCnZV6eadYvbGHN") } },
            { "Ed Helms", new[] { ("How Bad Can I Be?", "4l1R5g85v3bUA1bSSbzg6w") } },
            { "First of October", new[] { ("Ravioli", "5urjNYNqbC9cDAmdZOuLpq") } },
            { "Geek Music", new[] { ("Jeopardy! Main Theme (From \"Jeopardy!\")", "4qkYiZablQoG7f0Qu4Nd1c") } },
            { "Hampton The Hampster", new[] { ("The HampsterDance Song", "0ouSkB2t2fGeW60MPcvmXl") } },
            { "Joe Siris", new[] { ("It's Okay to Leave a Dog in a Hot Car", "7JP8bPdaQ3xunbuzai8Ggu") } },
            { "John Cena", new[] { ("The Time Is Now", "1ceQ6GnflE32iovHqnsJQA") } },
            { "Kahoot!", new[] { ("Lobby Music (Original Soundtrack)", "7IEdlE4ZwzPDxnoWFv10aj") } },
            { "LeviathanJPTV", new[] { ("Chug Jug With You (Number One Victory Royale)", "15RSD04RKmM1fjrBONNtqK") } },
            { "Mark Governor", new[] { ("Stadium Rave", "2Fxn9wa3Nv1QGuRptSqPHO") } },
            { "Mark Snow", new[] { ("Materia Primoris: The X-Files Theme (Main Title)", "4th0mPYPuZUs564oCu5I1U") } },
            { "MicroDragon", new[] { ("Bill Nye The Science Guy", "6ZTTRjw0B1z4n0J3YvhjOH") } },
            { "Nathan Evans", new[] { ("Wellerman - Sea Shanty", "54OBgO0Xwu20Jak9TMXbR7") } },
            { "PSY", new[] { ("Gangnam Style (강남스타일)", "03UrZgTINDqvnUMbbIMhql") } },
            { "Pink Guy", new[] { ("Hentai", "5N6l51sD0KZKwAWJEo8dg2") } },
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
            { "VRIGGER", new[] { ("EPSTEIN ISLAND FUNK (SLOWED)", "47Qq8MgiuogI11YBCt2oo4") } },
            { "Vincent E. L.", new[] { ("Fuck the Fire Department", "4JcTqcHEH2R6brcSWy0u9V") } },
            { "Wesley Willis", new[] { ("Rock N Roll McDonalds", "190l7oYBQe6JBsWPJM2uNN") } },
            { "yums", new[] { ("SCOTLAND FOREVER", "3SPu3n4NKYNpwFgQZAU0sl") } },
        };
    }
}
