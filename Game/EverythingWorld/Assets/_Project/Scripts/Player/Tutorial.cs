using EverythingWorld.World;
using UnityEngine;

namespace EverythingWorld.Player
{
    /// The tutorial: a night walk along a path of lights, far from the Everything World, taught by signs only.
    /// Look, walk, jump a low wall, run, sit on a bench, pick up and throw a ball, switch on a lamp, read an
    /// artist's plaque, the map and the passport, choose your light, then a blinding stretch of white where the
    /// sunglasses earn their keep, then Esc and the towers. The path ends at a cliff. The signs say keep walking.
    /// You fall, everything goes white, and you wake at the pearly gates, where a terminal asks a riddle; the
    /// answer opens the gates, and the Everything World opens on the title screen (remembered).
    public class Tutorial : MonoBehaviour
    {
        public static bool Done { get => PlayerPrefs.GetInt("EW_GatesOpened", 0) == 1; set => PlayerPrefs.SetInt("EW_GatesOpened", value ? 1 : 0); }
        public static bool Active => instance != null && instance.running;
        static Tutorial instance;

        static readonly Vector3 Walk = new Vector3(-4000f, 300f, 2000f), Gates = new Vector3(-4000f, 700f, 2700f);
        const float PathEnd = 182f;

        System.Action finished; bool running, fallen; float white; int fade;   // fade: 0 none, 1 to white, -1 from white
        GameObject walkRoot, gatesRoot; Transform gateL, gateR; float gatesOpenAt = -1f;
        SimplePlayerController walker;

        void Awake() { instance = this; walker = GetComponent<SimplePlayerController>(); }

        /// Starts the walk at its beginning; 'whenDone' runs once the gates have opened.
        public void Begin(System.Action whenDone)
        {
            finished = whenDone; running = true; fallen = false; gatesOpenAt = -1f;
            if (walkRoot == null) walkRoot = BuildWalk();
            if (gatesRoot == null) gatesRoot = BuildGates();
            gateL.localRotation = Quaternion.identity; gateR.localRotation = Quaternion.identity;
            Put(Walk + new Vector3(0, 1.2f, 0), 0f);
            Sky("The Tutorial Walk");
            white = 1f; fade = -1;
        }

        /// Leaves the walk (from the pause menu).
        public void Stop() { running = false; fade = 0; white = 0f; }

        void Put(Vector3 p, float yaw)
        {
            var cc = GetComponent<CharacterController>(); if (cc != null) cc.enabled = false;
            transform.SetPositionAndRotation(p, Quaternion.Euler(0, yaw, 0)); if (cc != null) cc.enabled = true;
            if (walker != null && walker.cameraPivot != null) walker.cameraPivot.localEulerAngles = Vector3.zero;
        }

        static void Sky(string mood) { var s = SkyDirector.Instance; if (s != null) { s.SetLand(mood); s.Snap(); } }

        void Update()
        {
            if (!running) return;
            if (fade != 0) { white = Mathf.Clamp01(white + fade * Time.deltaTime / (fade > 0 ? 1.4f : 2.6f)); if (fade < 0 && white <= 0f) fade = 0; }
            // off the cliff: white, then the gates
            if (!fallen && transform.position.y < Walk.y - 16f) { fallen = true; fade = 1; }
            if (fallen && fade == 1 && white >= 1f)
            {
                Put(Gates + new Vector3(0, 1.2f, -14f), 0f); Sky("The Pearly Gates"); fade = -1;
            }
            if (gatesOpenAt > 0f)
            {
                var t = Mathf.Clamp01((Time.time - gatesOpenAt) / 3f); var e = t * t * (3f - 2f * t);
                gateL.localRotation = Quaternion.Euler(0, -100f * e, 0); gateR.localRotation = Quaternion.Euler(0, 100f * e, 0);
                if (Time.time - gatesOpenAt > 4.5f && fade == 0) fade = 1;
                if (Time.time - gatesOpenAt > 4.5f && white >= 1f) { running = false; gatesOpenAt = -1f; white = 0f; fade = 0; Done = true; finished?.Invoke(); }
            }
        }

        Texture2D whiteTex;
        void OnGUI()
        {
            if (white <= 0f) return;
            if (whiteTex == null) { whiteTex = new Texture2D(1, 1); whiteTex.SetPixel(0, 0, new Color(1f, .99f, .96f)); whiteTex.Apply(); }
            GUI.depth = -200; var old = GUI.color; GUI.color = new Color(1, 1, 1, white); GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), whiteTex); GUI.color = old;
        }

        // ---------------------------------------------------------------- the walk
        static Vector3 P(float x, float y, float z) => Walk + new Vector3(x, y, z);

        GameObject BuildWalk()
        {
            var root = new GameObject("The tutorial walk"); var t = root.transform;
            var mk = new MeshKit(); var solid = new MeshKit(); var up = Vector3.up; var rng = new System.Random(7);
            var rock = WorldMaterials.Solid(new Color(.1f, .1f, .13f)); var stone = WorldMaterials.Solid(new Color(.3f, .29f, .3f)); var wood = WorldMaterials.Solid(new Color(.36f, .26f, .18f));
            var iron = WorldMaterials.Solid(new Color(.12f, .12f, .14f)); var warm = new Color(1f, .78f, .45f); var lamp = WorldMaterials.Glow(warm, 2.2f);
            // the plateau and the path, ending at the cliff
            mk.Box(rock, P(0, -6f, PathEnd / 2f - 3f), new Vector3(18f, 12f, PathEnd + 6f)); solid.Box(rock, P(0, -6f, PathEnd / 2f - 3f), new Vector3(18f, 12f, PathEnd + 6f));
            for (var z = -4f; z < PathEnd; z += 3f) mk.Box(stone, P(Mathf.Sin(z * .05f) * .6f, .02f, z + 1.5f), new Vector3(3.2f, .06f, 2.9f), Quaternion.Euler(0, Mathf.Cos(z * .05f) * 3f, 0));
            for (var k = 0; k < 40; k++) mk.Blob(rock, P(((float)rng.NextDouble() - .5f) * 16f, -.1f, (float)rng.NextDouble() * PathEnd), new Vector3(.8f, .5f, .7f) * (.5f + (float)rng.NextDouble()), rng, .3f);
            // walls you can't see, so the only way off is the end
            foreach (var x in new[] { -8.5f, 8.5f }) solid.Box(rock, P(x, 3f, PathEnd / 2f - 6f), new Vector3(1f, 8f, PathEnd + 4f));
            solid.Box(rock, P(0, 3f, -6f), new Vector3(18f, 8f, 1f));
            // lanterns both sides, all the way to the edge
            var n = 0;
            for (var z = 2f; z <= PathEnd - 2f; z += 6f, n++)
                foreach (var x in new[] { -3f, 3f })
                {
                    var g = P(x, 0, z); mk.Prism(iron, g, up, .06f, 2.4f, 6); mk.Blob(lamp, g + up * 2.5f, new Vector3(.16f, .22f, .16f), rng, 0f); ArtistBits.PostCollider(t, g, 2.4f, .1f);
                    if ((n + (x > 0 ? 1 : 0)) % 2 == 0) ArtistBits.PointLight(t, g + up * 2.5f, warm, 9f, 1.4f);
                }
            // the signs
            void Sign(float z, float x, string text)
            {
                var g = P(x, 0, z); mk.Prism(wood, g, up, .07f, 1.7f, 6); ArtistBits.PostCollider(t, g, 1.7f, .1f);
                var lines = text.Split('\n').Length; var h = .3f + lines * .2f;
                mk.Box(wood, g + up * (1.6f + h / 2f), new Vector3(2.6f, h, .08f)); mk.Box(WorldMaterials.Solid(new Color(.86f, .8f, .68f)), g + up * (1.6f + h / 2f) - Vector3.forward * .045f, new Vector3(2.45f, h - .12f, .02f));
                ArtistBits.Label(t, text, g + up * (1.6f + h / 2f) - Vector3.forward * .06f, Vector3.back, .15f, new Color(.18f, .12f, .08f), TextAnchor.MiddleCenter);
                ArtistBits.PointLight(t, g + up * 2.6f - Vector3.forward * .8f, warm, 4f, .8f);
            }
            Sign(4f, -1.8f, "Move the mouse to look around.");
            Sign(9f, 1.8f, "W A S D to walk.\nFollow the lights.");
            Sign(17f, -1.8f, "Space to jump.");
            mk.Box(stone, P(0, .35f, 20f), new Vector3(16f, .7f, .5f)); solid.Box(stone, P(0, .35f, 20f), new Vector3(16f, .7f, .5f));
            Sign(26f, 1.8f, "Hold Shift to run.");
            // a bench to sit on
            Sign(46f, -1.8f, "Look at the bench and press E to sit.\nPress E again to stand.");
            var bg = P(2.4f, 0, 49f); var bf = Vector3.left; var br = Quaternion.LookRotation(bf);
            for (var k = 0; k < 3; k++) mk.Box(wood, bg + up * .46f + bf * (-.16f + k * .16f), new Vector3(.13f, .05f, 2f), br * Quaternion.Euler(0, 90f, 0));
            mk.Box(wood, bg + up * .8f - bf * .26f, new Vector3(.05f, .2f, 2f), br * Quaternion.Euler(0, 90f, 0)); solid.Box(wood, bg + up * .45f, new Vector3(.5f, .9f, 2f), Quaternion.identity);
            Interact.Bench(t, bg + up * .52f - Vector3.forward * .5f, bg + up * .52f + Vector3.forward * .5f, bf);
            // a ball to pick up and throw
            Sign(55f, 1.8f, "E picks things up. Click throws.\nE puts them down.");
            mk.Prism(stone, P(-2.2f, 0, 58f), up, .35f, .9f, 10); ArtistBits.PostCollider(t, P(-2.2f, 0, 58f), .9f, .35f);
            var bm = new MeshKit(); bm.Blob(WorldMaterials.Glow(new Color(1f, .4f, .3f), .7f), up * .16f, Vector3.one * .16f, rng, 0f);
            var ball = bm.BuildSingle("Ball", t, false); ball.transform.position = P(-2.2f, .9f, 58f); var cb = Interact.Carry(ball, "ball", .4f, Walk.y - 30f);
            var bc = cb.GetComponent<BoxCollider>(); if (bc != null) { bc.size = new Vector3(.32f, .32f, .32f); bc.center = up * .16f; } var rb = ball.GetComponent<Rigidbody>(); if (rb != null) rb.isKinematic = true;
            // a lamp to switch on
            Sign(63f, -1.8f, "Glowing things can be used.\nLook at the lamp and press E.");
            var lg = P(2.4f, 0, 66f); mk.Prism(iron, lg, up, .08f, 2f, 6); ArtistBits.PostCollider(t, lg, 2f, .12f);
            var lm = new MeshKit(); var glowMat = new Material(WorldMaterials.Glow(new Color(.5f, .8f, 1f), 1f)) { name = "Tutorial lamp" }; lm.Blob(glowMat, Vector3.zero, Vector3.one * .3f, rng, 0f);
            var lgo = lm.BuildSingle("Lamp", t, false); lgo.transform.position = lg + up * 2.25f;
            var toy = Interact.Toy(lgo, Toy.Kind.Glow, "Switch on", new Vector3(.8f, .8f, .8f)); toy.glow = glowMat; toy.glowColor = new Color(.5f, .8f, 1f);
            // an artist's plaque
            Sign(71f, 1.8f, "Every artist has a plaque.\nPress E at one to read about them.");
            var data = IslandWorldBuilder.Instance != null ? IslandWorldBuilder.Instance.Data : null; IslandWorldData.Artist artist = null;
            if (data != null) { foreach (var a in data.layout.artists) if (a.name == "Radiohead") artist = a; if (artist == null && data.layout.artists.Length > 0) artist = data.layout.artists[0]; }
            var labels = new GameObject("Labels").transform; labels.SetParent(t, false);
            if (artist != null) ArtistBits.Plaque(mk, solid, labels, artist, P(-2.4f, 0, 74f), Vector3.right, wood, WorldMaterials.Solid(new Color(.9f, .86f, .78f)), new Color(.12f, .1f, .08f));
            Sign(80f, 1.8f, "M shows your map.\nJ opens your passport: a stamp for every land\nyou visit, every artist you read about,\nand every song you hear for 30 seconds.");
            // choose your light
            Sign(88f, -1.8f, "This light is you.\nPress E on an orb to take its colour.");
            var pal = PlayerOrb.Palette;
            for (var i = 0; i < pal.Length; i++)
            {
                var side = i % 2 == 0 ? -1f : 1f; var pp = P(side * (4.4f + (i % 3) * .1f), 0, 90f + (i / 2) * 1.5f);
                mk.Prism(stone, pp, up, .2f, 1.05f, 10); ArtistBits.PostCollider(t, pp, 1.1f, .22f);
                var go = new GameObject("Colour: " + pal[i].name); go.SetActive(false); go.transform.SetParent(t, false); go.transform.position = pp + up * 1.45f;
                var show = go.AddComponent<PlayerOrb>(); show.isLocal = false; show.display = true; show.radius = .17f; go.SetActive(true); show.Choose(i); show.Place(pp + up * 1.45f);
                var pick = new GameObject("Pick " + pal[i].name); pick.transform.SetParent(t, false); pick.transform.position = pp + up * 1.45f; var idx = i;
                Interact.Toy(pick, Toy.Kind.Custom, "Glow " + pal[i].name, new Vector3(.5f, .9f, .5f)).onUse = _ => { if (PlayerOrb.Local != null) PlayerOrb.Local.Choose(idx); };
            }
            // the blinding stretch
            Sign(100f, 1.8f, "Some places are very bright.\nPress G for sunglasses.\nG again takes them off.");
            var dazzle = WorldMaterials.Glow(new Color(1f, .97f, .9f), 7f);
            mk.Box(dazzle, P(0, .03f, 121f), new Vector3(15f, .04f, 34f));
            foreach (var x in new[] { -7f, 7f }) mk.Box(dazzle, P(x, 3f, 121f), new Vector3(.4f, 6f, 34f));
            for (var z = 106f; z <= 136f; z += 6f) { ArtistBits.PointLight(t, P(-4f, 4f, z), Color.white, 16f, 6f); ArtistBits.PointLight(t, P(4f, 4f, z + 3f), new Color(1f, .95f, .8f), 16f, 6f); }
            // pausing and the towers, then the end
            Sign(144f, -1.8f, "Esc pauses the game.\nIn the world, towers carry you between realms,\nand H at any tower brings you back to the Hub.");
            Sign(156f, 1.8f, "That's everything.\nKeep walking.");
            Sign(170f, -1.8f, "Don't stop.");
            mk.Build("Walk", t, false); solid.BuildCollider("Walk walls", t);
            return root;
        }

        // ---------------------------------------------------------------- the pearly gates
        GameObject BuildGates()
        {
            var root = new GameObject("The pearly gates"); var t = root.transform;
            var mk = new MeshKit(); var solid = new MeshKit(); var up = Vector3.up; var rng = new System.Random(9); var c = Gates;
            var cloud = WorldMaterials.Glow(new Color(.98f, .97f, .95f), .5f); var marble = WorldMaterials.Glow(new Color(.95f, .93f, .9f), .35f);
            var gold = WorldMaterials.Glow(new Color(1f, .82f, .4f), 1.1f);
            mk.Prism(cloud, c - up * 1f, up, 34f, 1f, 40); solid.Prism(cloud, c - up * 1f, up, 34f, 1f, 40);
            for (var k = 0; k < 70; k++) { var a = k * Mathf.PI * 2f / 70f; var r = 30f + (float)rng.NextDouble() * 6f; mk.Blob(cloud, c + new Vector3(Mathf.Cos(a) * r, (float)rng.NextDouble() * 1.5f - .4f, Mathf.Sin(a) * r), new Vector3(3f, 1.6f, 3f) * (.7f + (float)rng.NextDouble() * .6f), rng, .25f); }
            for (var k = 0; k < 30; k++) mk.Blob(cloud, c + new Vector3(((float)rng.NextDouble() - .5f) * 50f, -.3f, ((float)rng.NextDouble() - .5f) * 50f), new Vector3(2f, .8f, 2f) * (.6f + (float)rng.NextDouble()), rng, .25f);
            for (var k = 0; k < 24; k++) { var a = k * Mathf.PI / 12f; solid.Box(cloud, c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 33f + up * 4f, new Vector3(9f, 10f, 1f), Quaternion.LookRotation(new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)))); }
            // the gates: two tall pillars, an arch, and two golden doors
            var g = c + Vector3.forward * 8f;
            foreach (var x in new[] { -4.2f, 4.2f }) { mk.Prism(marble, g + Vector3.right * x, up, .9f, 11f, 16); mk.Blob(gold, g + Vector3.right * x + up * 11.4f, Vector3.one * .7f, rng, 0f); solid.Box(marble, g + Vector3.right * x + up * 5.5f, new Vector3(1.8f, 11f, 1.8f)); }
            for (var k = 0; k <= 16; k++) { var a = Mathf.PI * k / 16f; mk.Box(gold, g + new Vector3(Mathf.Cos(a) * 3.4f, 8.5f + Mathf.Sin(a) * 2.4f, 0), new Vector3(.35f, .35f, .35f)); }
            Transform Door(float side)
            {
                var pivot = new GameObject(side < 0 ? "Gate left" : "Gate right").transform; pivot.SetParent(t, false); pivot.position = g + Vector3.right * side * 3.3f;
                var dm = new MeshKit(); var dir = -side;   // the door reaches in toward the middle
                for (var k = 0; k <= 8; k++) dm.Box(gold, new Vector3(dir * (.2f + k * .38f), 4f, 0), new Vector3(.1f, 8f, .1f));
                foreach (var y in new[] { .4f, 4f, 7.6f }) dm.Box(gold, new Vector3(dir * 1.6f, y, 0), new Vector3(3.3f, .14f, .14f));
                var d = dm.BuildSingle("Door", pivot, false); d.isStatic = false; d.transform.localPosition = Vector3.zero;
                var col = new GameObject("Door collider"); col.transform.SetParent(pivot, false); col.transform.localPosition = new Vector3(dir * 1.6f, 4f, 0); col.AddComponent<BoxCollider>().size = new Vector3(3.3f, 8f, .3f);
                return pivot;
            }
            gateL = Door(-1f); gateR = Door(1f);
            ArtistBits.PointLight(t, g + up * 6f - Vector3.forward * 3f, new Color(1f, .9f, .7f), 26f, 2.2f);
            // the terminal before the gates, and its sign
            var tp = c + Vector3.forward * 1.5f; var f = new PlaceFrame(tp, Vector3.back);
            mk.Box(marble, f.P(0, .5f, 0), new Vector3(1.4f, 1f, .8f), f.r); solid.Box(marble, f.P(0, .5f, 0), new Vector3(1.4f, 1f, .8f), f.r);
            mk.Box(gold, f.P(0, 1.45f, -.1f), new Vector3(1f, .9f, .25f), f.r * Quaternion.Euler(-12f, 0, 0));
            var scr = new Material(WorldMaterials.Glow(new Color(.08f, .07f, .05f), .2f)) { name = "Gate screen" };
            mk.Box(scr, f.P(0, 1.47f, .05f), new Vector3(.86f, .74f, .02f), f.r * Quaternion.Euler(-12f, 0, 0));
            var tgo = new GameObject("Gate terminal"); tgo.SetActive(false); tgo.transform.SetParent(t, false); tgo.transform.position = f.P(0, 1.45f, .2f);
            var term = tgo.AddComponent<HubTerminal>(); term.riddle = true; term.glow = scr; term.glowColor = scr.GetColor("_EmissionColor");
            term.screen = ArtistBits.Plain(ArtistBits.Label(tgo.transform, "", f.P(.4f, 1.8f, .07f), f.Fwd, .038f, new Color(1f, .85f, .5f), TextAnchor.UpperLeft));
            term.screen.transform.rotation = f.r * Quaternion.Euler(-12f, 180f, 0);
            term.onAnswer = () => gatesOpenAt = Time.time;
            tgo.SetActive(true);
            Interact.Toy(tgo, Toy.Kind.Custom, "Use the terminal", new Vector3(1.1f, 1.1f, .6f)).onUse = _ => term.Begin(FindAnyObjectByType<PlayerInteractor>());
            var sg = c - Vector3.forward * 4f + Vector3.right * 2.4f; mk.Prism(marble, sg, up, .07f, 1.7f, 6);
            mk.Box(marble, sg + up * 1.9f, new Vector3(2.6f, .6f, .08f));
            ArtistBits.Label(t, "Answer, and the gates will open.", sg + up * 1.9f - Vector3.forward * .06f, Vector3.back, .15f, new Color(.4f, .3f, .15f), TextAnchor.MiddleCenter);
            mk.Build("Gates", t, false); solid.BuildCollider("Gates walls", t);
            return root;
        }
    }
}
