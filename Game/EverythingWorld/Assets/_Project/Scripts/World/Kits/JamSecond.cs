using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Commonwealth's second pass (part of JamKit):
    /// - the story of Gamehendge, told in a few lines on a slanted board in front of each of its places;
    /// - a short description beside every artist's plaque;
    /// - a lantern by each piece on the album trail that wakes whenever a song from that album plays.
    public partial class JamKit
    {
        static readonly Dictionary<string, string[]> GamehendgeStory = new Dictionary<string, string[]>
        {
            { "THE RHOMBUS", new[] { "Colonel Forbin found this glowing frame", "in a field near his home, stepped", "through it, and was in Gamehendge." } },
            { "ICCULUS'S MOUNTAIN", new[] { "Icculus, the great prophet, lives at", "the top. He wrote the Helping Friendly", "Book, and sent the Famous Mockingbird", "to bring it home when it was stolen." } },
            { "THE HELPING FRIENDLY BOOK", new[] { "The Lizards' book of all knowledge.", "While they kept it they lived in peace;", "when it was taken, they forgot." } },
            { "WILSON'S CASTLE", new[] { "Wilson, a traveller from another world,", "hid the Book in his tower and made", "himself king over the Lizards." } },
            { "THE LIZARDS' VILLAGE", new[] { "Home of the Lizards, the gentle people", "of Gamehendge, waiting for the day", "the Book comes home." } },
            { "THE LIZARDS", new[] { "Round the fire the Lizards tell", "the old stories, and wait for", "Icculus to send help." } },
        };

        /// A slanted board with the story, in front of a Gamehendge place's sign.
        void StoryBoard(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 signAt, Vector3 face, string title)
        {
            if (!GamehendgeStory.TryGetValue(title, out var lines)) return;
            var side = Vector3.Cross(Vector3.up, face);
            var at = signAt + face * 1.6f - side * .2f; at.y = ctx.Ground(at.x, at.z);
            var rot = Quaternion.LookRotation(face) * Quaternion.Euler(-35f, 0, 0);
            var h = .3f + lines.Length * .17f;
            foreach (var s in new[] { -1f, 1f }) mk.Prism(wood, at + side * s * 1.1f - Vector3.up * .2f, Vector3.up, .05f, 1.1f, 5);
            var board = at + Vector3.up * 1f;
            mk.Box(wood, board, new Vector3(2.6f, h, .06f), rot);
            BoxCol(ctx.Root, board - Vector3.up * .25f, new Vector3(2.6f, h + .5f, .25f), rot);   // the slanted board and its posts
            mk.Box(WorldMaterials.Solid(new Color(.88f, .82f, .68f)), board + rot * Vector3.forward * .035f, new Vector3(2.45f, h - .12f, .01f), rot);
            for (var k = 0; k < lines.Length; k++)
            {
                var p = board + rot * new Vector3(0, (lines.Length - 1) / 2f * .17f - k * .17f, .05f);
                var t = ArtistBits.Label(ctx.Root, lines[k], p, rot * Vector3.forward, .085f, new Color(.18f, .12f, .08f), TextAnchor.MiddleCenter);
            }
            if (solid != null) solid.Box(wood, at + Vector3.up * .55f, new Vector3(2.4f, 1.1f, .3f), Quaternion.LookRotation(face));
            ArtistBits.PointLight(ctx.Root, board + face * 1.2f + Vector3.up * .8f, Warm, 5f, 1f);
            var m = new GameObject("Shot: Gamehendge story, " + title).transform; m.SetParent(ctx.Root, false);
            var cam = board + face * 2.2f + Vector3.up * .5f; m.SetPositionAndRotation(cam, Quaternion.LookRotation((board - cam).normalized));
        }

        static readonly Dictionary<string, string> ArtistWords = new Dictionary<string, string>
        {
            { "Phish", "Four friends from Vermont, together since 1983. Every show is different: long improvisations, and Gamehendge, the land their songs live in." },
            { "Trey Anastasio", "Phish's guitarist and main songwriter, here on his own records." },
            { "Widespread Panic", "Athens, Georgia's Southern rock jam band, on the road since the eighties." },
            { "Umphrey's McGee", "Chicago-born progressive jam band: metal riffs, prog and long improvisations." },
            { "Goose", "Connecticut band of the 2020s jam revival: big, joyful builds." },
            { "Blues Traveler", "New York band led by John Popper's harmonica, jam-scene favourites since the nineties." },
            { "Jerry Garcia Band", "Jerry Garcia's band away from the Grateful Dead: soulful covers and long, easy jams." },
            { "The Disco Biscuits", "Philadelphia's trance-fusion band, where jam meets electronic music." },
            { "Yonder Mountain String Band", "Colorado string band bringing jam improvisation to bluegrass." },
            { "Oysterhead", "A supergroup of Trey Anastasio, Les Claypool and Stewart Copeland." },
            { "Rusted Root", "Pittsburgh band of acoustic guitars and world-music drums." },
            { "TAUK", "Instrumental rock-fusion quartet from Long Island." },
            { "The String Cheese Incident", "Colorado band blending bluegrass, rock and electronic grooves." },
            { "The Werks", "Ohio band of the jam scene: funky, psychedelic dance parties." },
        };

        /// A short description on a little board beside the plaque (from what's known, or from the songs here).
        void ArtistDescription(Plot z, IslandWorldData.Artist a, Vector3 plaque, Vector3 front, Transform labels)
        {
            string text;
            if (!ArtistWords.TryGetValue(a.name, out text))
            {
                var titles = new List<string>(); if (a.songList != null) foreach (var s in a.songList) titles.Add("\"" + s.title + "\"");
                text = titles.Count == 0 ? a.name : "In the playlist: " + string.Join(", ", titles) + ".";
            }
            // wrap to about 34 characters a line
            var lines = new List<string>(); var line = "";
            foreach (var w in text.Split(' ')) { if ((line + " " + w).Trim().Length > 34) { lines.Add(line.Trim()); line = w; } else line += " " + w; }
            if (line.Trim().Length > 0) lines.Add(line.Trim());
            var side = Vector3.Cross(Vector3.up, front);
            var at = plaque - side * 2.2f; at.y = z.ctx.Ground(at.x, at.z);   // (the lantern stands on the plaque's other side)
            var h = .25f + lines.Count * .15f; var rot = Quaternion.LookRotation(front);
            z.mk.Prism(wood, at - Vector3.up * .2f, Vector3.up, .05f, .9f + h * .5f, 5);
            var board = at + Vector3.up * (1f + h * .5f);
            z.mk.Box(dark, board, new Vector3(1.9f, h, .06f), rot); BoxCol(z.root, board, new Vector3(1.9f, h, .15f), rot);
            for (var k = 0; k < lines.Count; k++)
                ArtistBits.Label(labels, lines[k], board + front * .045f + Vector3.up * ((lines.Count - 1) / 2f * .15f - k * .15f), front, .075f, Ink, TextAnchor.MiddleCenter);
            ArtistBits.PostCollider(z.root, at, 1.4f, .12f);
        }

        // ---------------------------------------------------------------- the album lanterns
        static readonly Dictionary<string, string[]> AlbumSongs = new Dictionary<string, string[]>
        {
            { "HOIST", new[] { "2TuOkb5mdkO2IBTG7Ch4ns", "0YGHTxydVF22ibIwrWfEpZ" } },
            { "RIFT", new[] { "3yK7JvDME8TSsjrFyFzWTz", "5gHEd60IymWEuoRYrkxJEQ", "5kR2udD6lT3moKehFrpRvF", "2EtxqjLs4wSBTNtkm5VqkE", "4HlPpD7gpUq0ErOFyTHLtu", "0aJNDN5nBJYzQ4cJVUYX1l", "4li0OBUU5YGNgtdb91qZ7U" } },
            { "FARMHOUSE", new[] { "4BVKiPQ2Ix7mohDYo11wyO", "5wmRXK50YdwAXGvBzsAWH2", "7AcfBtVaUefLhVA0pLkVA4" } },
            { "BIG BOAT", new[] { "2f26s2OTd5PospyTITUR0n", "3WE5sqT3VTUADHqJMlWG9X" } },
            { "SIGMA OASIS", new[] { "3xoE9uQr8yLSYRrvk3Hk3w", "7M33YSoNDKTWEgjQ4iF9oV" } },
            { "A PICTURE OF NECTAR", new[] { "0xhSsFzHWFw58m8Kl8BO6R", "5rW0ZWRDYrqeHJpt9TppB3", "31akCREzBZ4oV68AnXVk95", "4ZHLkLWfcctxef8HztbE7a", "0M7qIbIfd9IQy68DibTqQ9", "7zf6pOzTt27uz3xo0AoFgL" } },
            { "LAWN BOY", new[] { "35K3Lf0ueeQ38V3nJOJjuj", "3wGNOxXUzeqC00i6E8GwwL", "2YKSgzOabJDgDzSmy8R5dy", "7zJlSL4rNqr8dzI55XSp2t" } },
            { "THE STORY OF THE GHOST", new[] { "0xCUU7DSGDTVOjiZEyy0md", "0OcjvgeS6S1oTSz0JcYeWy", "5pOO6jEgjh3psC4ahpJEP5", "3WmN0R4loh6ft6B2guDyat" } },
        };

        /// A lantern by an album's sign that wakes whenever a song from that album plays.
        void AlbumLantern(LandContext ctx, MeshKit mk, Vector3 sign, Vector3 face, string album)
        {
            if (!AlbumSongs.TryGetValue(album, out var ids)) return;
            var side = Vector3.Cross(Vector3.up, face); var post = sign - side * 1.8f; post.y = ctx.Ground(post.x, post.z);
            mk.Prism(wood, post - Vector3.up * .2f, Vector3.up, .06f, 2.5f, 6); ArtistBits.PostCollider(ctx.Root, post, 2.4f, .1f);
            mk.Box(wood, post + Vector3.up * 2.3f + face * .25f, new Vector3(.05f, .05f, .55f), Quaternion.LookRotation(face));
            var col = new Color(1f, .78f, .35f); var m = new Material(GlowM(col, 1.2f)) { name = "Album lantern " + album };
            var lamp = post + Vector3.up * 1.95f + face * .48f;
            mk.Box(m, lamp, new Vector3(.2f, .28f, .2f), Quaternion.identity);
            halos.Add(lamp, 1.2f, col, .7f);
            life.albumGlows.Add((ids, m, col * (1.2f * .65f)));
        }
    }

    public partial class JamLife
    {
        public List<(string[] ids, Material m, Color c)> albumGlows = new List<(string[], Material, Color)>();
        readonly Dictionary<Material, float> albumAmount = new Dictionary<Material, float>();

        void AlbumGlowUpdate(float dt, bool here, string id)
        {
            var t = Time.time;
            foreach (var (ids, m, c) in albumGlows)
            {
                if (m == null) continue;
                albumAmount.TryGetValue(m, out var a);
                var on = here && id != null && System.Array.IndexOf(ids, id) >= 0;
                a = Mathf.MoveTowards(a, on ? 1f : 0f, dt / 2f); albumAmount[m] = a;
                m.SetColor("_EmissionColor", c * (1f + a * (3f + Mathf.Sin(t * 2f) * .6f)));
            }
        }
    }
}
