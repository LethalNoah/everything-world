using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Small Hours' homes: a tiny cottage per artist that you can walk into, its window lit, with a desk, a
    /// desk lamp, a laptop and a microphone, plants, and the artist's covers on the wall. Each has its own detail:
    /// Steph.fm broadcasts from a radio cottage with an aerial; Bell Thompson's room glows jazz-club blue with a
    /// trumpet on the sill and a New York fire escape; Boowah plays a pedal steel on the porch; runfromobm has a
    /// grilled cheese on the stove; Sōra has a streetlight outside; Fionavair's window flickers with the TV;
    /// Hailey Picardi's room is chaos; John Luther's is full of plants; Carmen Silver's lamp lights floating
    /// dust; Josie on the Rocks has a cat on the windowsill.
    public partial class SmallHoursKit
    {
        public override void BuildArtist(LandContext ctx, IslandWorldData.Artist a)
        {
            if (wood == null) MakeMaterials();
            if (a.name == NoahName) { NoahPlace(ctx, a); return; }
            if (a.name == MoonName) { MoonhunterPlace(ctx, a); return; }
            var root = new GameObject("Artist: " + a.name).transform; root.SetParent(ctx.Root, false);
            var front = facing.TryGetValue(a.name, out var f) ? f : new Vector3(center.x - a.x, 0, center.z - a.z).normalized;
            if (front.sqrMagnitude < .01f) front = Vector3.forward;
            var mk = new MeshKit(); var solid = new MeshKit(); var pictures = new MeshKit();
            var labels = new GameObject("Labels").transform; labels.SetParent(root, false);
            var area = root.gameObject.AddComponent<ArtistArea>();
            area.artistName = a.name; area.stationId = a.stationId; area.radius = a.r; area.glowColor = WindowGlow;
            var glows = new List<Renderer>();
            var c = ctx.OnGround(a.x, a.z) - front * 1.5f;
            var windowColor = a.name == "Bell Thompson" ? new Color(.35f, .55f, 1f) : a.name == "Fionavair" ? new Color(.55f, .7f, 1f) : WindowGlow;
            var home = Cottage(ctx, root, mk, solid, pictures, labels, a, c, front, windowColor, glows, out var floorY, out var inside, a.name != "Boowah");
            var side = Vector3.Cross(Vector3.up, front);
            Vector3 G(Vector3 p) => ctx.OnGround(p.x, p.z);
            switch (a.name)
            {
                case "Steph.fm": Aerial(ctx, mk, home, root); OnAir(mk, labels, inside, front, floorY); break;
                case "Bell Thompson": Trumpet(mk, home + front * 4.35f + side * 2.4f + Vector3.up * 1.6f, side); FireEscape(mk, solid, home, front, side); break;
                case "Boowah": Porch(ctx, mk, solid, home, front, side, floorY, glows, root); break;
                case "runfromobm": Stove(mk, solid, inside, front, side, floorY); break;
                case "Sōra": StreetLamp(ctx, mk, G(home + front * 7.5f - side * 3.5f), side); break;
                case "Fionavair": life.tvWindow = TvGlow(root, inside + Vector3.up * (floorY + 1.4f) - front * 2.8f + side * 2f, front); break;
                case "Hailey Picardi": Papers(ctx, mk, inside, floorY); break;
                case "John Luther": Plants(ctx, mk, inside, home, front, side, floorY); break;
                case "Carmen Silver": Dust(ctx, root, inside, floorY); break;
                case "Josie on the Rocks": Cat(mk, home + front * 4.3f - side * 2.4f + Vector3.up * 1.15f, front); break;
            }
            ArtistBits.Plaque(mk, solid, labels, a, G(c + front * 7f + side * 3.2f), front, darkWood, board, Ink, 1.4f);
            foreach (var g in glows) if (g != null) halos.Add(g.transform.position, 1.1f, WindowGlow, .8f);
            area.glowRenderers = glows.ToArray();
            mk.Build("Home", root, false);
            solid.BuildCollider("Walls", root);
            pictures.Build("Covers", root, false);
        }

        /// A small cottage with a door you can walk through, lit windows, and a desk set up for making music.
        /// Returns its ground-level centre; 'inside' is the room's centre at floor height zero (add floorY).
        Vector3 Cottage(LandContext ctx, Transform root, MeshKit mk, MeshKit solid, MeshKit pictures, Transform labels, IslandWorldData.Artist a,
                        Vector3 c, Vector3 front, Color windowColor, List<Renderer> glows, out float floorY, out Vector3 inside, bool doorstep = true)
        {
            const float W = 8.4f, D = 8f, H = 4.2f, t = .3f;
            var rot = Quaternion.LookRotation(front);
            var side = Vector3.Cross(Vector3.up, front);
            var (lo, hi) = Footprint(ctx, c, rot, W, D);
            floorY = hi + .2f;
            var flat = new Vector3(c.x, 0, c.z);
            inside = flat;
            var paint = WorldMaterials.Solid(Paint[Mathf.Abs(a.name.GetHashCode()) % Paint.Length]);
            var floorMat = WorldMaterials.Solid(new Color(.36f, .28f, .24f));
            var fy = floorY;
            mk.Box(stone, new Vector3(c.x, (lo + fy) / 2f - .2f, c.z), new Vector3(W + .3f, fy - lo + .6f, D + .3f), rot);
            mk.Box(floorMat, new Vector3(c.x, fy - .05f, c.z), new Vector3(W, .1f, D), rot);
            solid.Box(floorMat, new Vector3(c.x, fy - .25f, c.z), new Vector3(W, .5f, D), rot);
            void Wall(Vector3 p, Vector3 s) { mk.Box(paint, p, s, rot); solid.Box(paint, p, s, rot); }
            var up = Vector3.up;
            Wall(flat - front * (D / 2f - t / 2f) + up * (fy + H / 2f), new Vector3(W, H, t));
            foreach (var s in new[] { -1f, 1f })
            {
                Wall(flat + side * s * (W / 2f - t / 2f) + up * (fy + H / 2f), new Vector3(t, H, D - 2f * t));
                // a lit side window
                var win = flat + side * s * (W / 2f + .01f) + up * (fy + 2f) - front * 1f;
                mk.Box(WorldMaterials.Glow(windowColor, 1.6f), win, new Vector3(.06f, 1.1f, 1.3f), rot);
                mk.Box(darkWood, win + up * .62f, new Vector3(.12f, .1f, 1.5f), rot);
            }
            // the front: a doorway with a lit window either side
            var door = HumanScale.DoorWidth;
            var fz = flat + front * (D / 2f - t / 2f);
            var pier = (W - door) / 2f;
            foreach (var s in new[] { -1f, 1f })
            {
                Wall(fz + side * s * (door / 2f + pier / 2f) + up * (fy + H / 2f), new Vector3(pier, H, t));
                var win = fz + side * s * (door / 2f + pier / 2f) + front * (t / 2f + .01f) + up * (fy + 2f);
                glows.Add(ArtistBits.GlassBox(root, win, new Vector3(1.4f, 1.2f, .05f), windowColor, rot));
                mk.Box(darkWood, win - up * .68f + front * .08f, new Vector3(1.7f, .1f, .25f), rot);   // sill
            }
            Wall(fz + up * (fy + HumanScale.DoorHeight + (H - HumanScale.DoorHeight) / 2f), new Vector3(door, H - HumanScale.DoorHeight, t));
            mk.GableRoof(slate, flat + up * (fy + H), new Vector3(D + .8f, 0, W + .8f), 2.4f, Quaternion.LookRotation(side));
            mk.Box(stone, flat - side * (W / 2f - 1.2f) + up * (fy + H + 1.8f), new Vector3(.7f, 1.8f, .7f), rot);   // chimney
            // inside: a desk under the covers, a lamp, a laptop, a microphone, a chair, a rug
            var desk = flat - front * (D / 2f - 1f) + up * fy;
            mk.Box(wood, desk + up * .75f, new Vector3(3f, .08f, 1f), rot);
            foreach (var s in new[] { -1f, 1f }) mk.Box(wood, desk + side * s * 1.4f + up * .37f, new Vector3(.08f, .74f, .9f), rot);
            solid.Box(wood, desk + up * .4f, new Vector3(3f, .8f, 1f), rot);
            mk.Box(board, desk + up * .8f + side * .2f, new Vector3(.9f, .03f, .6f), rot);
            mk.Box(WorldMaterials.Glow(new Color(.6f, .75f, 1f), 1.4f), desk + up * 1.05f + side * .2f - front * .28f, new Vector3(.9f, .55f, .03f), rot * Quaternion.Euler(-15f, 0, 0));
            mk.Prism(iron, desk + up * .8f - side * 1.1f, up, .05f, .5f, 6);
            mk.Box(iron, desk + up * 1.3f - side * .95f, new Vector3(.04f, .04f, .4f), rot * Quaternion.Euler(0, 40, 0));
            mk.Prism(WorldMaterials.Glow(WindowGlow, 2.2f), desk + up * 1.15f - side * .8f, Vector3.down, .18f, .2f, 8, .5f);
            var mic = desk + front * 1.6f + side * .9f;
            mk.Prism(iron, mic, up, .03f, 1.55f, 6);
            mk.Box(iron, mic + up * .02f, new Vector3(.5f, .04f, .5f), rot);
            mk.Box(WorldMaterials.Solid(new Color(.6f, .6f, .62f), .6f), mic + up * 1.65f, new Vector3(.12f, .25f, .12f), rot);
            ArtistBits.PostCollider(root, mic, 1.7f, .2f);
            mk.Box(darkWood, desk + front * .9f + up * .45f, new Vector3(.5f, .06f, .5f), rot);
            mk.Box(WorldMaterials.Solid(new Color(.38f, .3f, .42f)), flat + up * (fy + .02f) + front * .8f, new Vector3(3.6f, .02f, 2.6f), rot);
            ArtistBits.CoverWall(pictures, labels, ctx, a, flat - front * (D / 2f - t - .03f) + up * (fy + 2.6f), front, 3, .75f, 3, true, Ink);
            ArtistBits.PointLight(root, flat + up * (fy + H - .6f), WindowGlow, 9f, 1.4f, true);
            IndoorSpaces.Add(flat + up * (fy + H / 2f), rot, new Vector3(W, H, D));
            // a step up to the door
            if (doorstep && fy - ctx.Ground(fz.x + front.x * 1.5f, fz.z + front.z * 1.5f) > .2f)
            {
                var step = fz + front * 1.1f; step.y = (fy + ctx.Ground(step.x, step.z)) / 2f - .1f;
                mk.Box(stone, step, new Vector3(door + .6f, fy - ctx.Ground(step.x, step.z) + .2f, 1.6f), rot);
                solid.Box(stone, step, new Vector3(door + .6f, fy - ctx.Ground(step.x, step.z) + .2f, 1.6f), rot);
            }
            return new Vector3(c.x, fy, c.z);
        }

        // ---------------------------------------------------------------- details
        void Aerial(LandContext ctx, MeshKit mk, Vector3 home, Transform root)
        {
            var b = home + Vector3.up * 6.2f;
            mk.Prism(iron, b, Vector3.up, .1f, 9f, 4, .3f);
            for (var i = 1; i < 5; i++) mk.Box(iron, b + Vector3.up * i * 1.8f, new Vector3(1.6f - i * .25f, .06f, .06f), Quaternion.identity);
            var red = new Material(WorldMaterials.Glow(new Color(1f, .15f, .1f), 3f)) { name = "Aerial light" };
            mk.Blob(red, b + Vector3.up * 9.1f, Vector3.one * .18f, ctx.Rng, 0f);
            halos.Add(b + Vector3.up * 9.1f, 1.4f, new Color(1f, .2f, .15f), 1f);
            life.aerialLight = red;
        }

        void OnAir(MeshKit mk, Transform labels, Vector3 inside, Vector3 front, float floorY)
        {
            var at = inside - front * 3.85f + Vector3.up * (floorY + 3.75f);
            mk.Box(board, at, new Vector3(1.4f, .42f, .08f), Quaternion.LookRotation(front));
            mk.Box(WorldMaterials.Glow(new Color(1f, .15f, .1f), 2.5f), at + front * .045f, new Vector3(1.3f, .34f, .02f), Quaternion.LookRotation(front));
            ArtistBits.Label(labels, "ON AIR", at + front * .06f + Vector3.up * .13f, front, .22f, Color.white);
        }

        void Trumpet(MeshKit mk, Vector3 at, Vector3 along)
        {
            // built about 1.4x life size so it reads on the sill; along = the way the bell points
            const float s = 1.4f;
            var brass = WorldMaterials.Solid(new Color(.88f, .68f, .3f), .8f);
            var pearl = WorldMaterials.Solid(new Color(.95f, .93f, .88f), .6f);
            var up = Vector3.up;
            var acrossDir = Vector3.Cross(up, along);
            void Tube(Vector3 a, Vector3 b, float r) { var d = b - a; if (d.sqrMagnitude > 1e-6f) mk.Prism(brass, a, d.normalized, r, d.magnitude, 8); }
            // the valve block, three valves standing up with pearl finger buttons
            var valves = at + up * .03f * s;
            for (var i = 0; i < 3; i++)
            {
                var v = valves + along * (i - 1f) * .035f * s;
                mk.Prism(brass, v - up * .07f * s, up, .014f * s, .14f * s, 8);
                mk.Prism(brass, v + up * .07f * s, up, .006f * s, .03f * s, 6);
                mk.Prism(pearl, v + up * .1f * s, up, .012f * s, .008f * s, 10);
            }
            // the lead pipe from the mouthpiece into the valves (upper tube)
            var mouth = valves - along * .32f * s + up * .045f * s;
            mk.Prism(brass, mouth - along * .04f * s, along, .006f * s, .04f * s, 8, 2.2f);   // the mouthpiece cup
            Tube(mouth, valves - along * .05f * s + up * .045f * s, .007f * s);
            // the bell pipe from the valves out to the bell (lower tube), and the flared bell
            var bellStart = valves + along * .12f * s - up * .03f * s;
            Tube(valves - along * .05f * s - up * .03f * s, bellStart, .008f * s);
            mk.Prism(brass, bellStart, along, .01f * s, .09f * s, 12, 2.4f);
            mk.Prism(brass, bellStart + along * .09f * s, along, .024f * s, .06f * s, 14, 2.7f);
            mk.Prism(brass, bellStart + along * .15f * s, along, .065f * s, .012f * s, 16);
            // the main tuning slide: a U-loop running back under the lead pipe
            var back = valves - along * .2f * s;
            Tube(valves - along * .05f * s - up * .005f * s, back - up * .005f * s, .007f * s);
            Tube(valves - along * .05f * s - up * .055f * s, back - up * .055f * s, .007f * s);
            for (var k = 0; k < 6; k++)
            {
                float a0 = Mathf.PI / 2f + k * Mathf.PI / 6f, a1 = Mathf.PI / 2f + (k + 1) * Mathf.PI / 6f;
                Vector3 P(float a) => back - up * .03f * s + (up * Mathf.Sin(a) + along * Mathf.Cos(a)) * .025f * s;
                Tube(P(a0), P(a1), .007f * s);
            }
            // the little slides on the valves and a finger hook
            foreach (var i in new[] { 0, 2 }) Tube(valves + along * (i - 1f) * .035f * s - up * .07f * s, valves + along * (i - 1f) * .035f * s - up * .07f * s + acrossDir * .05f * s, .005f * s);
            Tube(valves + up * .045f * s + along * .02f * s, valves + up * .065f * s + along * .03f * s, .003f * s);
        }

        void FireEscape(MeshKit mk, MeshKit solid, Vector3 home, Vector3 front, Vector3 side)
        {
            var wall = home + side * 4.35f;
            for (var level = 0; level < 2; level++)
            {
                var y = 1.2f + level * 2.1f;
                var deck = wall + side * .7f + Vector3.up * y;
                mk.Box(iron, deck, new Vector3(1.4f, .06f, 3.2f), Quaternion.LookRotation(front));
                for (var k = 0; k < 6; k++) mk.Box(iron, deck + side * .68f + front * (k * .6f - 1.5f) + Vector3.up * .45f, new Vector3(.03f, .9f, .03f), Quaternion.identity);
                mk.Box(iron, deck + side * .68f + Vector3.up * .9f, new Vector3(.04f, .04f, 3.2f), Quaternion.LookRotation(front));
                var s0 = deck + front * (level == 0 ? 1.4f : -1.4f); var s1 = deck + front * (level == 0 ? -1.4f : 1.4f) - Vector3.up * 2.1f;
                if (level == 1) mk.Box(iron, (s0 + s1) / 2f, new Vector3(.9f, .05f, Vector3.Distance(s0, s1)), Quaternion.LookRotation(s0 - s1));
            }
        }

        void Porch(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 home, Vector3 front, Vector3 side, float floorY, List<Renderer> glows, Transform root)
        {
            var rot = Quaternion.LookRotation(front);
            var deck = home + front * 5.6f;
            var groundUnder = Mathf.Min(ctx.Ground(deck.x, deck.z), ctx.Ground(deck.x + front.x * 1.5f, deck.z + front.z * 1.5f));
            mk.Box(wood, new Vector3(deck.x, floorY - .1f, deck.z), new Vector3(8.4f, .2f, 3f), rot);
            mk.Box(stone, new Vector3(deck.x, (floorY + groundUnder) / 2f - .25f, deck.z), new Vector3(8.2f, floorY - groundUnder + .3f, 2.8f), rot);
            solid.Box(wood, new Vector3(deck.x, floorY - .3f, deck.z), new Vector3(8.4f, .6f, 3f), rot);
            var stepAt = deck + front * 2f; stepAt.y = (floorY + ctx.Ground(stepAt.x, stepAt.z)) / 2f - .1f;
            if (floorY - ctx.Ground(stepAt.x, stepAt.z) > .2f)
            {
                mk.Box(stone, stepAt, new Vector3(2.4f, floorY - ctx.Ground(stepAt.x, stepAt.z) + .2f, 1f), rot);
                solid.Box(stone, stepAt, new Vector3(2.4f, floorY - ctx.Ground(stepAt.x, stepAt.z) + .2f, 1f), rot);
            }
            foreach (var s in new[] { -1f, 1f }) mk.Prism(darkWood, new Vector3(deck.x, floorY, deck.z) + side * s * 4f + front * 1.3f, Vector3.up, .1f, 3.4f, 6);
            mk.Box(slate, new Vector3(deck.x, floorY + 3.5f, deck.z), new Vector3(8.8f, .15f, 3.4f), rot * Quaternion.Euler(-8f, 0, 0));
            // the pedal steel: a long body on four legs, strings along it, a pedal bar beneath
            var ps = new Vector3(deck.x, floorY, deck.z) - side * 1.6f;
            var body = WorldMaterials.Solid(new Color(.32f, .24f, .2f), .5f);
            mk.Box(body, ps + Vector3.up * .78f, new Vector3(1.6f, .14f, .5f), Quaternion.LookRotation(side));
            for (var k = 0; k < 10; k++) mk.Box(WorldMaterials.Solid(new Color(.8f, .8f, .82f), .7f), ps + Vector3.up * .86f + front * (k * .04f - .18f), new Vector3(1.5f, .006f, .006f), Quaternion.LookRotation(side));
            foreach (var (a, b) in new[] { (-1f, -1f), (1f, -1f), (-1f, 1f), (1f, 1f) }) mk.Prism(iron, ps + side * a * .7f + front * b * .2f, Vector3.up, .025f, .74f, 5);
            mk.Box(iron, ps + Vector3.up * .15f, new Vector3(.08f, .05f, 1.2f), rot);
            mk.Box(darkWood, ps - front * .7f + Vector3.up * .45f, new Vector3(.45f, .06f, .45f), rot);
            glows.Add(ArtistBits.GlassBox(root, new Vector3(deck.x, floorY + 3.1f, deck.z) - front * 1.2f, new Vector3(.3f, .3f, .3f), WindowGlow, rot));
            solid.Box(body, ps + Vector3.up * .5f, new Vector3(1.7f, 1f, .6f), Quaternion.LookRotation(side));
        }

        void Stove(MeshKit mk, MeshKit solid, Vector3 inside, Vector3 front, Vector3 side, float floorY)
        {
            var rot = Quaternion.LookRotation(front);
            var at = inside + side * 2.9f - front * 2.6f + Vector3.up * floorY;
            mk.Box(WorldMaterials.Solid(new Color(.86f, .84f, .8f)), at + Vector3.up * .45f, new Vector3(1.2f, .9f, 1f), rot);
            solid.Box(stone, at + Vector3.up * .45f, new Vector3(1.2f, .9f, 1f), rot);
            mk.Prism(WorldMaterials.Glow(new Color(1f, .4f, .15f), 1.8f), at + Vector3.up * .9f, Vector3.up, .25f, .02f, 10);
            mk.Prism(iron, at + Vector3.up * .93f, Vector3.up, .3f, .06f, 12);
            mk.Box(iron, at + Vector3.up * .95f + side * .45f, new Vector3(.4f, .03f, .06f), rot);
            mk.Box(WorldMaterials.Solid(new Color(.88f, .66f, .28f)), at + Vector3.up * 1.0f, new Vector3(.3f, .05f, .3f), rot * Quaternion.Euler(0, 20, 0));
            mk.Box(WorldMaterials.Solid(new Color(1f, .82f, .3f)), at + Vector3.up * 1.035f, new Vector3(.28f, .015f, .28f), rot * Quaternion.Euler(0, 20, 0));
        }

        Material TvGlow(Transform root, Vector3 at, Vector3 front)
        {
            var mat = new Material(WorldMaterials.Glow(new Color(.55f, .7f, 1f), 2f)) { name = "TV glow" };
            var mk = new MeshKit();
            mk.Box(mat, at, new Vector3(1.1f, .7f, .05f), Quaternion.LookRotation(front));
            mk.Box(board, at - front * .08f, new Vector3(1.2f, .8f, .1f), Quaternion.LookRotation(front));
            mk.Build("TV", root, false);
            return mat;
        }

        void Papers(LandContext ctx, MeshKit mk, Vector3 inside, float floorY)
        {
            var paper = WorldMaterials.Solid(new Color(.94f, .93f, .9f));
            for (var i = 0; i < 40; i++)
            {
                var p = inside + new Vector3(ctx.Range(-3.6f, 3.6f), floorY + .02f + i * .0005f, ctx.Range(-3.4f, 3.4f));
                mk.Box(paper, p, new Vector3(.3f, .005f, .42f), Quaternion.Euler(0, ctx.Range(0, 360f), 0));
            }
        }

        void Plants(LandContext ctx, MeshKit mk, Vector3 inside, Vector3 home, Vector3 front, Vector3 side, float floorY)
        {
            var pot = WorldMaterials.Solid(new Color(.62f, .38f, .28f));
            var green = WorldMaterials.Solid(new Color(.22f, .42f, .3f));
            for (var i = 0; i < 14; i++)
            {
                var p = i < 9 ? inside + new Vector3(ctx.Range(-3.4f, 3.4f), floorY, ctx.Range(-3.2f, 3.2f))
                              : ctx.OnGround((home + front * 4.8f + side * ctx.Range(-3.5f, 3.5f)).x, (home + front * 4.8f + side * ctx.Range(-3.5f, 3.5f)).z);
                if ((new Vector2(p.x, p.z) - new Vector2(inside.x, inside.z)).magnitude < 1.2f) continue;
                var h = ctx.Range(.4f, 1.2f);
                mk.Prism(pot, p, Vector3.up, .22f, .35f, 8, 1.2f);
                ArtistBits.PostCollider(ctx.Root, p, .9f, .3f);
                mk.Blob(green, p + Vector3.up * (.35f + h * .5f), new Vector3(.4f, h * .6f, .4f), ctx.Rng, .3f);
            }
        }

        void Dust(LandContext ctx, Transform root, Vector3 inside, float floorY)
        {
            var mk = new MeshKit();             // floating dust you walk through (the land audit skips "Soft")
            var mote = WorldMaterials.Glow(new Color(1f, .9f, .7f), 2f);
            for (var i = 0; i < 60; i++)
            {
                var t = ctx.Range(0f, 1f);
                var p = inside + new Vector3(ctx.Range(-.6f, .6f) * (1f + t * 2f), floorY + 3.4f - t * 3f, ctx.Range(-.6f, .6f) * (1f + t * 2f));
                mk.Box(mote, p, Vector3.one * .025f, Quaternion.identity);
            }
            mk.Build("Soft dust", root, false);
        }

        void Cat(MeshKit mk, Vector3 at, Vector3 front)
        {
            var fur = WorldMaterials.Solid(new Color(.1f, .1f, .12f));
            var side = Vector3.Cross(Vector3.up, front);
            mk.Blob(fur, at + Vector3.up * .18f, new Vector3(.2f, .18f, .28f), new System.Random(7), .1f);
            mk.Blob(fur, at + Vector3.up * .42f + front * .15f, new Vector3(.14f, .13f, .13f), new System.Random(8), .05f);
            foreach (var s in new[] { -1f, 1f }) mk.Box(fur, at + Vector3.up * .56f + front * .15f + side * s * .07f, new Vector3(.05f, .09f, .05f), Quaternion.LookRotation(front));
            mk.Box(fur, at + Vector3.up * .05f - front * .2f + side * .2f, new Vector3(.05f, .05f, .4f), Quaternion.LookRotation(side));
            mk.Box(WorldMaterials.Glow(new Color(.8f, 1f, .4f), 1.5f), at + Vector3.up * .44f + front * .27f + side * .05f, Vector3.one * .02f, Quaternion.identity);
            mk.Box(WorldMaterials.Glow(new Color(.8f, 1f, .4f), 1.5f), at + Vector3.up * .44f + front * .27f - side * .05f, Vector3.one * .02f, Quaternion.identity);
        }
    }
}
