using EverythingWorld.World;
using UnityEngine;

namespace EverythingWorld.Player
{
    /// The multiplayer test room: a plain, closed, softly lit hall floating far outside the map (so none of the world
    /// shows and no music plays), for checking that friends can see each other. A grid floor to judge movement by,
    /// a few pillars to walk round, a colour station (E on an orb takes its colour) and a sign. Built the first time
    /// you go in, from the title screen.
    public static class TestRoom
    {
        public static readonly Vector3 Centre = new Vector3(-4000f, 400f, -4000f);
        static GameObject built;

        /// Builds the room if needed; returns where to stand (the body's middle).
        public static Vector3 Enter()
        {
            if (built == null) built = Build();
            return Centre + new Vector3(0, 1.2f, -12f);
        }

        static GameObject Build()
        {
            var root = new GameObject("Multiplayer test room"); var t = root.transform; t.position = Vector3.zero;
            var mk = new MeshKit(); var solid = new MeshKit(); var up = Vector3.up; var c = Centre;
            var floor = WorldMaterials.Solid(new Color(.22f, .23f, .27f)); var line = WorldMaterials.Glow(new Color(.4f, .7f, 1f), .5f);
            var wall = WorldMaterials.Solid(new Color(.16f, .16f, .19f)); var trim = WorldMaterials.Glow(new Color(1f, .8f, .45f), 1.2f);
            const float half = 22f, h = 9f;
            mk.Box(floor, c - up * .25f, new Vector3(half * 2f, .5f, half * 2f)); solid.Box(floor, c - up * .25f, new Vector3(half * 2f, .5f, half * 2f));
            for (var k = -half + 2f; k < half; k += 4f) { mk.Box(line, c + new Vector3(k, .01f, 0), new Vector3(.05f, .01f, half * 2f)); mk.Box(line, c + new Vector3(0, .01f, k), new Vector3(half * 2f, .01f, .05f)); }
            foreach (var (d, s) in new[] { (Vector3.forward, Vector3.right), (Vector3.back, Vector3.right), (Vector3.right, Vector3.forward), (Vector3.left, Vector3.forward) })
            {
                var wc = c + d * half + up * h / 2f; var size = new Vector3(Mathf.Abs(s.x) * half * 2f + .6f, h, Mathf.Abs(s.z) * half * 2f + .6f) + new Vector3(Mathf.Abs(d.x) * .6f, 0, Mathf.Abs(d.z) * .6f);
                mk.Box(wall, wc, size); solid.Box(wall, wc, size);
                mk.Box(trim, c + d * (half - .32f) + up * 2.2f, new Vector3(Mathf.Abs(s.x) * half * 2f + .1f, .08f, Mathf.Abs(s.z) * half * 2f + .1f));
            }
            mk.Box(wall, c + up * (h + .25f), new Vector3(half * 2f + .6f, .5f, half * 2f + .6f)); solid.Box(wall, c + up * (h + .25f), new Vector3(half * 2f + .6f, .5f, half * 2f + .6f));
            // pillars to walk round, glowing at their feet
            foreach (var p in new[] { new Vector3(-9f, 0, 8f), new Vector3(9f, 0, 8f), new Vector3(-9f, 0, -2f), new Vector3(9f, 0, -2f) })
            {
                mk.Box(WorldMaterials.Solid(new Color(.3f, .3f, .34f)), c + p + up * h / 2f, new Vector3(1.2f, h, 1.2f)); solid.Box(wall, c + p + up * h / 2f, new Vector3(1.2f, h, 1.2f));
                mk.Box(line, c + p + up * .05f, new Vector3(1.6f, .04f, 1.6f));
            }
            for (var k = 0; k < 6; k++) { var lp = c + new Vector3((k % 3 - 1) * 14f, h - .6f, (k / 3 - .5f) * 18f); ArtistBits.PointLight(t, lp, new Color(1f, .93f, .82f), 26f, 1.6f); mk.Box(WorldMaterials.Glow(new Color(1f, .95f, .85f), 2f), lp + up * .4f, new Vector3(1.4f, .1f, 1.4f)); }
            // the sign on the far wall
            ArtistBits.Label(t, "MULTIPLAYER TEST ROOM", c + Vector3.forward * (half - .4f) + up * 5.2f, Vector3.back, .9f, new Color(1f, .86f, .58f), TextAnchor.MiddleCenter);
            ArtistBits.Plain(ArtistBits.Label(t, "Host or join from Play with Friends (Esc). No music plays here.", c + Vector3.forward * (half - .4f) + up * 4.1f, Vector3.back, .32f, new Color(.85f, .85f, .82f), TextAnchor.MiddleCenter));
            // a colour station: E on an orb takes its colour
            var pal = PlayerOrb.Palette;
            for (var i = 0; i < pal.Length; i++)
            {
                var pp = c + new Vector3((i - (pal.Length - 1) / 2f) * 1.6f, 0, 14f);
                mk.Prism(WorldMaterials.Solid(new Color(.5f, .48f, .45f)), pp, up, .2f, 1.05f, 10); ArtistBits.PostCollider(t, pp, 1.1f, .22f);
                var go = new GameObject("Colour: " + pal[i].name); go.SetActive(false); go.transform.SetParent(t, false); go.transform.position = pp + up * 1.45f;
                var show = go.AddComponent<PlayerOrb>(); show.isLocal = false; show.display = true; show.radius = .17f; go.SetActive(true); show.Choose(i); show.Place(pp + up * 1.45f);
                var pick = new GameObject("Pick " + pal[i].name); pick.transform.SetParent(t, false); pick.transform.position = pp + up * 1.45f; var idx = i;
                Interact.Toy(pick, Toy.Kind.Custom, "Glow " + pal[i].name, new Vector3(.5f, .9f, .5f)).onUse = _ => { if (PlayerOrb.Local != null) PlayerOrb.Local.Choose(idx); };
            }
            mk.Build("Room", t, false); solid.BuildCollider("Room walls", t);
            return root;
        }
    }
}
