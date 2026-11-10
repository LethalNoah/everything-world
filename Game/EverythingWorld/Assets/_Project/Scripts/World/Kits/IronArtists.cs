using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Iron Range's artists, each an alcove off the mine roads with covers on an iron board, a plaque and a brazier:
    /// - Metallica: a hall carved into the rock like a mine entrance, lit within; before it a field of white crosses on
    ///   puppet strings that rise to a control bar hung from a gantry; the stars of Orion overhead.
    /// - Black Sabbath: a cloaked figure among dead trees before a crumbling stone mill, and two iron pylons with
    ///   electric arcs crackling between them. Elena Siegman: a boarded-up bunker, a meteorite glowing green in its
    ///   crater, a mystery crate with a beam of light rising from it, a row of glowing soda machines.
    /// - Ozzy Osbourne: mine carts racing round a looping track. Motörhead: a giant ace of spades turning. Dio: a brass
    ///   diving helmet on a rock, chained. Iron Maiden: a cannon and crossed sabres. Judas Priest: a chrome motorcycle and
    ///   a broken chain. Danzig: a great skull rock. Rainbow: a stone tower with a star on top and a rainbow ring. Riot
    ///   City: a stone jaguar head with burning eyes. Diamond Head: a giant diamond. Anthrax: a sword buried in the rock.
    ///   Aria: an iron gate in red light. Manilla Road: a rune stone. Sunstroke: a burning sun on a pole.
    public partial class IronKit
    {
        class Plot
        {
            public LandContext ctx; public Transform root, labels; public MeshKit mk, solid, pictures;
            public IslandWorldData.Artist a; public Vector3 c, front, side; public List<Renderer> glows; public ArtistArea area;
            public Vector3 At(float fw, float rt) { var p = c + front * fw + side * rt; return ctx.OnGround(p.x, p.z); }
            public Quaternion Rot => Quaternion.LookRotation(front);
        }

        public override void BuildArtist(LandContext ctx, IslandWorldData.Artist a)
        {
            if (rock == null) MakeMaterials();
            var root = new GameObject("Artist: " + a.name).transform; root.SetParent(ctx.Root, false);
            var front = facing.TryGetValue(a.name, out var f) ? f : new Vector3(center.x - a.x, 0, center.z - a.z).normalized;
            if (front.sqrMagnitude < .01f) front = Vector3.forward;
            var labels = new GameObject("Labels").transform; labels.SetParent(root, false);
            var area = root.gameObject.AddComponent<ArtistArea>();
            area.artistName = a.name; area.stationId = a.stationId; area.radius = a.r; area.glowColor = Molten;
            var z = new Plot { ctx = ctx, root = root, labels = labels, mk = new MeshKit(), solid = new MeshKit(), pictures = new MeshKit(), a = a, c = ctx.OnGround(a.x, a.z), front = front, side = Vector3.Cross(Vector3.up, front), glows = new List<Renderer>(), area = area };
            var n = a.name;
            if (n == "Metallica") { Metallica(z); MetallicaMore(z); }
            else if (n == "Black Sabbath") { Sabbath(z); SabbathMore(z); }
            else if (n == "Elena Siegman") { Bunker(z); ZombiesMore(z); }
            else if (n == "Kevin Sherwood") Sherwood(z);
            else if (n == "Ozzy Osbourne") { CartLoop(z); OzzyMore(z); }
            else if (n.StartsWith("Mot")) Ace(z);
            else if (n == "Dio") Helmet(z);
            else if (n == "Iron Maiden") Cannon(z);
            else if (n == "Judas Priest") Bike(z);
            else if (n == "Danzig") { SkullRock(ctx, z.mk, z.solid, z.At(-3f, 0)); }
            else if (n == "Rainbow") Tower(z);
            else if (n == "Riot City") Jaguar(z);
            else if (n == "Diamond Head") Diamond(z);
            else if (n == "Anthrax") Sword(z);
            else if (n == "Aria") RedGate(z);
            else if (n == "Sunstroke") BurningSun(z);
            else RuneStone(z);
            var reach = Foot(a);
            Board(z, z.At(reach * .8f, -reach * .5f - 2.5f), front);
            ArtistBits.Plaque(z.mk, z.solid, labels, a, z.At(reach * .8f + 1.5f, reach * .5f + 2.5f), front, iron, dark, Ink, 1.6f);
            Brazier(ctx, z.mk, z.At(reach * .8f, reach * .5f + 5f));
            Beacon(z, z.At(reach * .8f, reach * .5f + 7.5f));
            if (z.area.glowLight == null) z.area.glowLight = ArtistBits.PointLight(root, z.c + Vector3.up * 5f + front * 5f, Molten, HasStation(a) ? 20f : 13f, 1.4f);
            area.glowRenderers = z.glows.ToArray();
            z.mk.Build("Area", root, false);
            z.solid.BuildCollider("Walls", root);
            z.pictures.Build("Covers", root, false);
            noGrass.Add(new Vector3(z.c.x, reach * .9f, z.c.z));
        }

        void Board(Plot z, Vector3 ground, Vector3 dir)
        {
            dir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized; var rot = Quaternion.LookRotation(dir); var side = Vector3.Cross(Vector3.up, dir);
            var n = Mathf.Max(1, Mathf.Min(5, z.a.songList?.Length ?? 1)); const float size = .9f; var w = n * size * 1.25f + .4f;
            foreach (var s in new[] { -1f, 1f }) { z.mk.Box(iron, ground + side * s * (w / 2f - .1f) + Vector3.up * 1.2f, new Vector3(.2f, 2.6f, .2f), rot); ArtistBits.PostCollider(z.root, ground + side * s * (w / 2f - .1f), 2.4f, .12f); }
            var c = ground + Vector3.up * 1.8f;
            z.mk.Box(iron, c - Vector3.up * .1f, new Vector3(w, size + .6f, .14f), rot); z.solid.Box(iron, c - Vector3.up * .1f, new Vector3(w, size + .6f, .24f), rot);
            for (var k = 0; k < 6; k++) foreach (var y in new[] { -1f, 1f }) z.mk.Prism(chain, c + dir * .08f + side * ((k / 5f - .5f) * (w - .2f)) + Vector3.up * y * (size / 2f + .2f), dir, .05f, .04f, 6);
            ArtistBits.CoverWall(z.pictures, z.labels, z.ctx, z.a, c + dir * .075f + Vector3.up * .08f, dir, n, size, n, true, Ink);
        }

        // ---------------------------------------------------------------- Metallica
        void Metallica(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot;
            // the hall in the rock: a great rock mass with a timber-and-iron mine portal, lit within
            var h = z.At(-10f, 0);
            z.mk.Blob(rock, h + up * 5f - z.front * 3f, new Vector3(13f, 8f, 9f), z.ctx.Rng, .2f);
            z.solid.Box(rock, h + up * 4f - z.front * 4f, new Vector3(18f, 8f, 10f), rot);
            z.mk.Box(dark, h + up * 2.6f + z.front * 3.1f, new Vector3(5f, 5.2f, .2f), rot);
            z.mk.Box(GlowM(Molten, 1.6f), h + up * 2f + z.front * 3f, new Vector3(3.6f, 3.6f, .05f), rot);
            foreach (var s in new[] { -1f, 1f }) { z.mk.Box(timber, h + up * 3f + z.front * 3.4f + z.side * s * 2.8f, new Vector3(.6f, 6f, .6f), rot); z.solid.Box(timber, h + up * 3f + z.front * 3.4f + z.side * s * 2.8f, new Vector3(.6f, 6f, .6f), rot); }
            z.mk.Box(iron, h + up * 6.2f + z.front * 3.4f, new Vector3(7f, .8f, .8f), rot);
            ArtistBits.Label(z.labels, "METALLICA", h + up * 6.2f + z.front * 3.85f, z.front, .4f, new Color(1f, .75f, .45f), TextAnchor.MiddleCenter);
            ArtistBits.PointLight(z.root, h + up * 3f + z.front * 6f, Molten, 18f, 2f);
            // the field of crosses on puppet strings, a control bar hung from a gantry above them
            var strings = new Material(WorldMaterials.Glow(new Color(.9f, .9f, .95f), .5f)) { name = "Puppet strings" }; life.strings = strings;
            var white = WorldMaterials.Solid(new Color(.92f, .92f, .9f)); var barY = 11f; var fc = z.At(-1f, 0);
            for (var i = 0; i < 4; i++) for (var j = 0; j < 4; j++)
            {
                var p = z.At(-3.5f + i * 2.2f, -4.2f + j * 2.8f); if ((p - fc).magnitude > 7f) continue;
                z.mk.Box(white, p + up * .9f, new Vector3(.18f, 1.8f, .12f), rot); z.mk.Box(white, p + up * 1.35f, new Vector3(.9f, .16f, .12f), rot);
                var top = new Vector3(p.x, fc.y + barY, p.z); z.mk.Box(strings, (p + up * 1.8f + top) / 2f, new Vector3(.03f, (top - p).y - 1.8f, .03f), Quaternion.identity);
                ArtistBits.PostCollider(z.root, p, 1.8f, .15f);
            }
            foreach (var s in new[] { -1f, 1f }) { var gp = z.At(-1f, s * 7.5f); z.mk.Box(iron, gp + up * barY / 2f, new Vector3(.5f, barY + 1f, .5f), rot); ArtistBits.PostCollider(z.root, gp, barY, .3f); }
            z.mk.Box(iron, fc + up * (barY + .5f), new Vector3(.6f, .5f, 15.5f), Quaternion.LookRotation(z.side));
            z.mk.Box(timber, fc + up * barY, new Vector3(9f, .4f, .4f), rot); z.mk.Box(timber, fc + up * barY, new Vector3(.4f, .4f, 8f), rot);
            // Orion overhead
            Vector3[] orion = { new Vector3(-3f, 6f, 0), new Vector3(3f, 6.5f, 0), new Vector3(-1f, 0f, 0), new Vector3(0f, .3f, 0), new Vector3(1f, .6f, 0), new Vector3(-3.2f, -6f, 0), new Vector3(3.5f, -5.5f, 0) };
            var oc = h + up * 26f;
            foreach (var o in orion) { var sp = oc + z.side * o.x + up * o.y; z.mk.Blob(GlowM(new Color(.8f, .88f, 1f), 4f), sp, Vector3.one * .35f, z.ctx.Rng, 0f); halos.Add(sp, 1.8f, new Color(.8f, .88f, 1f), .7f); life.stars.Add(sp); }
            noGrass.Add(new Vector3(h.x, 12f, h.z));
        }

        // ---------------------------------------------------------------- Black Sabbath
        void Sabbath(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot;
            // the crumbling mill
            var m = z.At(-8f, 3f); z.mk.Box(rock, m + up * 3f, new Vector3(6f, 6f, 5f), rot); z.solid.Box(rock, m + up * 3f, new Vector3(6f, 6f, 5f), rot);
            z.mk.GableRoof(darkRock, m + up * 6f, new Vector3(5.6f, 0, 6.6f), 2f, Quaternion.LookRotation(z.side)); z.mk.Box(dark, m + up * 1.4f + z.front * 2.51f, new Vector3(1.2f, 2.4f, .05f), rot);
            var wheelC = m + z.side * 3.3f + up * 2.6f; for (var k = 0; k < 8; k++) z.mk.Box(timber, wheelC, new Vector3(.2f, 5f, .3f), Quaternion.LookRotation(z.side) * Quaternion.Euler(0, 0, k * 22.5f) * Quaternion.Euler(0, 90f, 0));
            // the cloaked figure among dead trees
            var fig = z.At(-1.5f, -2f); var cloak = WorldMaterials.Solid(new Color(.12f, .08f, .14f));
            z.mk.Cone(cloak, fig, .8f, 2.2f, 8); z.mk.Blob(cloak, fig + up * 2f, new Vector3(.35f, .4f, .35f), z.ctx.Rng, 0f); z.mk.Blob(WorldMaterials.Solid(new Color(.75f, .7f, .65f)), fig + up * 1.98f + z.front * .22f, new Vector3(.18f, .22f, .1f), z.ctx.Rng, 0f);
            ArtistBits.PostCollider(z.root, fig, 2.2f, .6f);
            foreach (var p in new[] { z.At(-4f, -6f), z.At(-6f, -3f), z.At(-2f, -7f) }) DeadPine(z.ctx, WorldMaterials.Solid(new Color(.14f, .16f, .15f)), WorldMaterials.Solid(new Color(.2f, .17f, .16f)), p);
            var pond = z.At(-3f, -1f); z.mk.Prism(WorldMaterials.Glow(new Color(.15f, .1f, .25f), .1f), pond + up * .03f, up, 2.4f, .02f, 14);
            // the electric arcs between two pylons
            foreach (var s in new[] { -1f, 1f }) { var py = z.At(1f, 5f + s * 3f); z.mk.Prism(iron, py, up, .3f, 6f, 6, .4f); z.mk.Blob(GlowM(new Color(.6f, .5f, 1f), 3f), py + up * 6.2f, Vector3.one * .45f, z.ctx.Rng, 0f); ArtistBits.PostCollider(z.root, py, 6f, .4f); }
            life.arcA = z.At(1f, 2f) + up * 6.2f; life.arcB = z.At(1f, 8f) + up * 6.2f;
            halos.Add((life.arcA + life.arcB) / 2f, 4f, new Color(.6f, .5f, 1f), .35f);
            ArtistBits.PointLight(z.root, fig + up * 3f + z.front * 3f, new Color(.6f, .45f, 1f), 14f, 1.4f);
        }

        // ---------------------------------------------------------------- Elena Siegman
        void Bunker(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var concrete = WorldMaterials.Solid(new Color(.42f, .42f, .4f));
            var b = z.At(-8f, -2f); z.mk.Box(concrete, b + up * 2f, new Vector3(9f, 4f, 6f), rot); z.solid.Box(concrete, b + up * 2f, new Vector3(9f, 4f, 6f), rot);
            foreach (var s in new[] { -2.6f, 2.6f }) { z.mk.Box(dark, b + up * 2.2f + z.front * 3.01f + z.side * s, new Vector3(1.6f, 1.4f, .04f), rot); for (var k = 0; k < 3; k++) z.mk.Box(timber, b + up * (1.7f + k * .5f) + z.front * 3.08f + z.side * s, new Vector3(1.9f, .2f, .05f), rot * Quaternion.Euler(0, 0, (k - 1) * 12f)); }
            z.mk.Box(timber, b + up * 1.2f + z.front * 3.05f, new Vector3(1.4f, 2.4f, .1f), rot);
            // the meteorite in its crater
            var mc = z.At(-1f, 6f); z.mk.Prism(darkRock, mc - up * .2f, up, 3.4f, .4f, 12, .8f);
            var met = new Material(WorldMaterials.Glow(new Color(.3f, 1f, .4f), 1.4f)) { name = "Element 115" }; life.meteor = met; life.meteorAt = mc + up * 1.2f;
            z.mk.Blob(darkRock, mc + up * .9f, new Vector3(1.4f, 1.1f, 1.3f), z.ctx.Rng, .35f); for (var k = 0; k < 6; k++) z.mk.Box(met, mc + up * .9f + Quaternion.Euler(k * 37f, k * 61f, 0) * Vector3.forward * 1.05f, new Vector3(.4f, .1f, .25f), Quaternion.Euler(k * 37f, k * 61f, 0));
            halos.Add(mc + up * 1.2f, 3f, new Color(.3f, 1f, .4f), .5f); z.solid.Prism(darkRock, mc - up * .2f, up, 1.4f, 2.4f, 8);
            ArtistBits.PointLight(z.root, mc + up * 2.5f, new Color(.3f, 1f, .45f), 14f, 1.8f);
            // the mystery crate
            var cr = z.At(1f, -5f); z.mk.Box(timber, cr + up * .5f, new Vector3(2.2f, 1f, 1f), Quaternion.LookRotation(z.side)); z.mk.Box(GlowM(new Color(1f, .85f, .4f), 2.6f), cr + up * 1.02f, new Vector3(2f, .04f, .8f), Quaternion.LookRotation(z.side));
            z.solid.Box(timber, cr + up * .5f, new Vector3(2.2f, 1f, 1f), Quaternion.LookRotation(z.side)); life.crate = cr + up * 1.1f;
            ArtistBits.Label(z.labels, "?", cr + up * .5f + z.front * .52f, z.front, .5f, new Color(1f, .85f, .4f), TextAnchor.MiddleCenter);
            // a row of soda machines
            Color[] sodas = { new Color(1f, .2f, .2f), new Color(.3f, .6f, 1f), new Color(1f, .8f, .2f), new Color(.4f, 1f, .5f) };
            for (var k = 0; k < 4; k++) { var sp = z.At(-3f, -9f + k * 1.6f); z.mk.Box(dark, sp + up * 1.2f, new Vector3(1.3f, 2.4f, 1f), rot); z.mk.Box(GlowM(sodas[k], 1.8f), sp + up * 1.4f + z.front * .51f, new Vector3(1.1f, 1.8f, .02f), rot); halos.Add(sp + up * 1.4f + z.front * .9f, 1.3f, sodas[k], .45f); }
            z.solid.Box(dark, z.At(-3f, -6.6f) + up * 1.2f, new Vector3(1.3f, 2.4f, 6.4f), Quaternion.LookRotation(z.side) * Quaternion.Euler(0, 90f, 0));
        }

        // ---------------------------------------------------------------- the others
        void CartLoop(Plot z)
        {
            var up = Vector3.up; var c = z.At(-3f, 0); var pts = new List<Vector3>();
            for (var k = 0; k <= 48; k++) { var a = k * Mathf.PI * 2f / 48f; var p = c + z.side * Mathf.Cos(a) * 6f + z.front * Mathf.Sin(a) * 3.5f; p.y = c.y + 1.2f + Mathf.Sin(a * 2f) * .9f; pts.Add(p); }
            for (var k = 0; k + 1 < pts.Count; k++)
            {
                var a = pts[k]; var b = pts[k + 1]; var d = b - a; var sd = Vector3.Cross(up, new Vector3(d.x, 0, d.z).normalized);
                z.mk.Box(timber, (a + b) / 2f - up * .1f, new Vector3(1.3f, .12f, .25f), Quaternion.LookRotation(sd, up));
                foreach (var s in new[] { -.4f, .4f }) z.mk.Box(iron, (a + b) / 2f + sd * s, new Vector3(.07f, .08f, d.magnitude + .05f), Quaternion.LookRotation(d));
                if (k % 4 == 0) { var g = z.ctx.Ground(a.x, a.z); z.mk.Box(timber, new Vector3(a.x, (a.y + g) / 2f, a.z), new Vector3(.2f, a.y - g + .2f, .2f), Quaternion.identity); }
            }
            life.loop = pts.ToArray();
            for (var k = 0; k < 3; k++)
            {
                var ck = new MeshKit(); ck.Box(rust, new Vector3(0, .5f, 0), new Vector3(1f, .7f, 1.3f), Quaternion.identity); ck.Blob(darkRock, new Vector3(0, .9f, 0), new Vector3(.45f, .2f, .6f), z.ctx.Rng, .4f);
                foreach (var s in new[] { -.5f, .5f }) foreach (var zz in new[] { -.4f, .4f }) ck.Prism(iron, new Vector3(s, .15f, zz), Vector3.right * Mathf.Sign(s), .18f, .08f, 8);
                ck.Prism(GlowM(Molten, 2.4f), new Vector3(0, .5f, .66f), Vector3.forward, .12f, .05f, 8);
                life.carts.Add(ck.BuildSingle("Mine cart", life.transform, false).transform);
            }
            z.solid.Box(timber, c + up * 1f, new Vector3(13f, 2f, 1f), Quaternion.LookRotation(z.side) * Quaternion.Euler(0, 90f, 0));
        }

        void Ace(Plot z)
        {
            var up = Vector3.up; var c = z.At(-3f, 0);
            z.mk.Prism(darkRock, c - up * .3f, up, 1.6f, 1f, 8); z.solid.Prism(darkRock, c - up * .3f, up, 1.6f, 6f, 8);
            var card = new MeshKit(); var white = WorldMaterials.Solid(new Color(.95f, .94f, .9f)); var black = WorldMaterials.Solid(new Color(.05f, .05f, .05f));
            card.Box(white, Vector3.zero, new Vector3(3f, 4.2f, .08f), Quaternion.identity);
            foreach (var zz in new[] { -.05f, .05f })
            {
                var f = Vector3.forward * zz; var bump = Mathf.Sign(zz) * .02f;
                card.Blob(black, f + new Vector3(-.35f, .25f, bump), new Vector3(.5f, .5f, .02f), z.ctx.Rng, 0f); card.Blob(black, f + new Vector3(.35f, .25f, bump), new Vector3(.5f, .5f, .02f), z.ctx.Rng, 0f);
                card.Cone(black, f + new Vector3(0, .45f, bump) + Vector3.down * .1f, .55f, .9f, 4); card.Box(black, f + new Vector3(0, -.45f, bump), new Vector3(.18f, .7f, .03f), Quaternion.identity);
                card.Box(black, f + new Vector3(-1.1f, 1.6f, bump), new Vector3(.4f, .5f, .02f), Quaternion.identity); card.Box(black, f + new Vector3(1.1f, -1.6f, bump), new Vector3(.4f, .5f, .02f), Quaternion.identity);
            }
            var t = card.BuildSingle("Ace of spades", life.transform, false).transform; t.position = c + up * 3.4f; life.card = t;
            halos.Add(c + up * 3.4f, 3f, new Color(1f, .9f, .8f), .25f);
        }

        void Helmet(Plot z)
        {
            var up = Vector3.up; var c = z.At(-3f, 0); var brass = WorldMaterials.Solid(new Color(.75f, .55f, .25f), .85f);
            z.mk.Blob(rock, c + up * .8f, new Vector3(2.4f, 1.2f, 2f), z.ctx.Rng, .3f);
            var h = c + up * 2.8f; z.mk.Blob(brass, h, Vector3.one * 1.3f, z.ctx.Rng, 0f); z.mk.Prism(brass, h - up * 1.5f, up, 1.2f, .6f, 14);
            foreach (var d in new[] { z.front, z.side, -z.side }) { z.mk.Prism(brass, h + d * 1.15f, d, .55f, .25f, 12); z.mk.Prism(SongGlow(HolyDiver, new Color(.4f, .8f, 1f), 1.4f), h + d * 1.38f, d, .45f, .03f, 12); }
            z.solid.Box(rock, c + up * 2f, new Vector3(3f, 4f, 3f), z.Rot);
            Chain(z.mk, h - up * .9f - z.front * 1f, z.At(-6f, 2f) + up * .2f, .8f);
            halos.Add(h + z.front * 1.6f, 1.6f, new Color(.4f, .8f, 1f), .5f);
        }

        void Cannon(Plot z)
        {
            var up = Vector3.up; var c = z.At(-2f, 0); var bronze = WorldMaterials.Solid(new Color(.45f, .35f, .2f), .7f);
            z.mk.Box(timber, c + up * .6f, new Vector3(1.4f, .8f, 2.4f), z.Rot); foreach (var s in new[] { -.8f, .8f }) foreach (var zz in new[] { -.8f, .8f }) z.mk.Prism(timber, c + z.side * s + z.front * zz + up * .5f, z.side * Mathf.Sign(s), .5f, .15f, 10);
            z.mk.Prism(bronze, c + up * 1.2f - z.front * 1.2f, (z.front + up * .25f).normalized, .45f, 3.2f, 12, .75f);
            z.solid.Box(timber, c + up * .8f, new Vector3(2f, 1.6f, 3.4f), z.Rot);
            z.mk.Blob(SongGlow(Trooper, new Color(1f, .55f, .15f), 2.6f), c + up * 1.75f - z.front * 1.1f, Vector3.one * .12f, z.ctx.Rng, 0f);
            for (var k = 0; k < 5; k++) z.mk.Blob(dark, c + z.side * 2f + up * (.25f + (k / 3) * .4f) + z.front * ((k % 3) - 1f) * .5f, Vector3.one * .25f, z.ctx.Rng, 0f);
            var w = z.At(-4f, -3f) + up * 2f; var steel = WorldMaterials.Solid(new Color(.8f, .8f, .85f), .9f);
            foreach (var s in new[] { -30f, 30f }) z.mk.Box(steel, w, new Vector3(.12f, 3f, .05f), z.Rot * Quaternion.Euler(0, 0, s));
            z.mk.Prism(darkRock, z.At(-4f, -3f) - up * .3f, up, .9f, 1f, 8);
        }

        void Bike(Plot z)
        {
            var up = Vector3.up; var c = z.At(-2f, 0); var dir = z.side; var rot = Quaternion.LookRotation(dir); var chrome = WorldMaterials.Solid(new Color(.85f, .85f, .88f), .95f); var black = WorldMaterials.Solid(new Color(.06f, .06f, .07f), .8f);
            foreach (var s in new[] { -1f, 1f }) { var wc = c + dir * s * 1.1f + up * .6f; z.mk.Prism(black, wc - z.front * .1f, z.front, .55f, .2f, 16); z.mk.Prism(chrome, wc - z.front * .11f, z.front, .2f, .22f, 10); }
            z.mk.Box(chrome, c + up * 1.15f, new Vector3(.4f, .4f, 1f), rot); z.mk.Box(black, c + up * 1.05f - dir * .6f, new Vector3(.35f, .15f, .8f), rot); z.mk.Box(chrome, c + up * .75f, new Vector3(.45f, .5f, .7f), rot);
            z.mk.Box(chrome, c + up * 1.1f + dir * .95f, new Vector3(.08f, 1.1f, .08f), rot * Quaternion.Euler(-25f, 0, 0)); z.mk.Box(chrome, c + up * 1.65f + dir * .75f, new Vector3(1f, .06f, .06f), rot);
            z.mk.Prism(SongGlow(BreakingLaw, new Color(1f, .95f, .8f), 3f), c + up * 1.4f + dir * 1.1f, dir, .15f, .08f, 10);
            z.solid.Box(black, c + up * .8f, new Vector3(.8f, 1.6f, 3f), rot);
            Chain(z.mk, z.At(1f, -3f) + up * .1f, z.At(1f, -1f) + up * .5f, .1f); Chain(z.mk, z.At(1.4f, 1f) + up * .5f, z.At(1.8f, 3f) + up * .1f, .1f);
        }

        void Tower(Plot z)
        {
            var up = Vector3.up; var c = z.At(-4f, 0);
            for (var k = 0; k < 6; k++) z.mk.Prism(k % 2 == 0 ? rock : darkRock, c + up * k * 2.4f, up, 2.2f - k * .12f, 2.4f, 10, (2.2f - (k + 1) * .12f) / (2.2f - k * .12f));
            z.solid.Prism(rock, c, up, 2.2f, 14.4f, 10);
            var star = c + up * 15.4f; z.mk.Blob(SongGlow(Stargazer, new Color(1f, .95f, .8f), 3f), star, Vector3.one * .6f, z.ctx.Rng, 0f); halos.Add(star, 3f, Color.white, .7f);
            for (var k = 0; k < 6; k++) { var r = 4f + k * .25f; for (var s = 0; s < 32; s++) { var a0 = s * Mathf.PI / 16f; var a1 = (s + 1) * Mathf.PI / 16f; var p0 = star + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * r; var p1 = star + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * r; z.mk.Box(GlowM(ProgKit.Spectrum[k], 1.4f), (p0 + p1) / 2f, new Vector3(.2f, .08f, Vector3.Distance(p0, p1) + .02f), Quaternion.LookRotation(p1 - p0)); } }
        }

        void Jaguar(Plot z)
        {
            var up = Vector3.up; var c = z.At(-3f, 0) + up * 2.2f; var stoneM = WorldMaterials.Solid(new Color(.45f, .42f, .38f));
            z.mk.Box(stoneM, c - up * 1.6f, new Vector3(3f, 1.4f, 3f), z.Rot); z.mk.Blob(stoneM, c, new Vector3(1.8f, 1.5f, 1.8f), z.ctx.Rng, .1f);
            z.mk.Box(stoneM, c + z.front * 1.4f - up * .4f, new Vector3(1.6f, 1f, 1.4f), z.Rot);
            foreach (var s in new[] { -1f, 1f }) { z.mk.Cone(stoneM, c + up * 1.2f + z.side * s * 1f, .5f, .7f, 4); z.mk.Box(SongGlow(JaguarSong, new Color(1f, .7f, .1f), 3f), c + up * .4f + z.front * 1.55f + z.side * s * .55f, new Vector3(.45f, .2f, .05f), z.Rot * Quaternion.Euler(0, 0, s * -15f)); }
            z.mk.Box(dark, c - up * .7f + z.front * 2.1f, new Vector3(1.2f, .4f, .1f), z.Rot);
            halos.Add(c + up * .4f + z.front * 1.9f, 1.6f, new Color(1f, .7f, .1f), .7f);
            z.solid.Box(stoneM, c - up * .8f, new Vector3(3f, 4f, 3.4f), z.Rot);
        }

        void Diamond(Plot z)
        {
            var up = Vector3.up; var c = z.At(-3f, 0) + up * 3.5f; var gem = SongGlow(SuckingLove, new Color(.75f, .9f, 1f), 1.2f);
            z.mk.Cone(gem, c, 1.8f, 1.2f, 8); z.mk.Prism(gem, c, Vector3.down, 1.8f, 2.6f, 8, .02f, 0f, false);
            z.mk.Prism(darkRock, z.At(-3f, 0) - up * .3f, up, 1.6f, 1.2f, 8); z.solid.Prism(darkRock, z.At(-3f, 0) - up * .3f, up, 1.6f, 5f, 8);
            halos.Add(c, 3f, new Color(.75f, .9f, 1f), .5f); ArtistBits.PointLight(z.root, c + z.front * 3f, new Color(.8f, .9f, 1f), 12f, 1.4f);
        }

        void Sword(Plot z)
        {
            var up = Vector3.up; var c = z.At(-3f, 0); var steel = WorldMaterials.Solid(new Color(.8f, .82f, .86f), .95f);
            z.mk.Blob(rock, c + up * .8f, new Vector3(2.6f, 1.4f, 2.2f), z.ctx.Rng, .3f); z.solid.Box(rock, c + up * .8f, new Vector3(4f, 1.6f, 3.4f), z.Rot);
            z.mk.Box(steel, c + up * 3.2f, new Vector3(.5f, 4.4f, .1f), z.Rot); z.mk.Box(iron, c + up * 5.5f, new Vector3(2f, .3f, .3f), z.Rot); z.mk.Box(WorldMaterials.Solid(new Color(.3f, .15f, .1f)), c + up * 6.2f, new Vector3(.25f, 1.2f, .25f), z.Rot); z.mk.Blob(SongGlow(EdgeOfPerfection, new Color(1f, .2f, .15f), 3f), c + up * 6.9f, Vector3.one * .25f, z.ctx.Rng, 0f);
        }

        void RedGate(Plot z)
        {
            var up = Vector3.up; var c = z.At(-3f, 0); var rot = z.Rot;
            foreach (var s in new[] { -1f, 1f }) { z.mk.Box(darkRock, c + z.side * s * 2.6f + up * 2.5f, new Vector3(1f, 5f, 1f), rot); z.solid.Box(darkRock, c + z.side * s * 2.6f + up * 2.5f, new Vector3(1f, 5f, 1f), rot); }
            for (var b = 0; b < 9; b++) z.mk.Box(iron, c + z.side * ((b - 4f) * .5f) + up * 2.2f, new Vector3(.1f, 4.2f, .1f), rot);
            foreach (var y in new[] { .6f, 2.2f, 3.8f }) z.mk.Box(iron, c + up * y, new Vector3(4.4f, .15f, .15f), rot);
            z.solid.Box(iron, c + up * 2.2f, new Vector3(4.4f, 4.4f, .3f), rot);
            Chain(z.mk, c + z.side * -2.2f + up * 3f + z.front * .1f, c + z.side * 2.2f + up * 3f + z.front * .1f, .6f);
            z.mk.Box(SongGlow(KrovZaKrov, new Color(1f, .1f, .08f), 1.6f), c + up * .1f - z.front * .4f, new Vector3(4.4f, .08f, .4f), rot);
            ArtistBits.PointLight(z.root, c + up * 3f + z.front * 3f, new Color(1f, .1f, .08f), 12f, 2f); halos.Add(c + up * 2.2f - z.front * .6f, 3f, new Color(1f, .1f, .08f), .4f);
        }

        void BurningSun(Plot z)
        {
            var up = Vector3.up; var c = z.At(-3f, 0);
            z.mk.Prism(iron, c, up, .15f, 5f, 6); ArtistBits.PostCollider(z.root, c, 5f, .2f);
            var s = c + up * 6f; z.mk.Prism(SongGlow(RunAway, new Color(1f, .6f, .1f), 3f), s - z.front * .2f, z.front, 1.4f, .4f, 16);
            for (var k = 0; k < 12; k++) z.mk.Cone(GlowM(new Color(1f, .4f, .05f), 2.6f), s + Quaternion.AngleAxis(k * 30f, z.front) * up * 1.4f, .35f, 1.2f, 4);
            halos.Add(s + z.front * .5f, 3.5f, new Color(1f, .5f, .1f), .6f); life.fires.Add(s + up * .5f);
        }

        void RuneStone(Plot z)
        {
            var up = Vector3.up; var c = z.At(-3f, 0);
            z.mk.Box(darkRock, c + up * 2.4f, new Vector3(2f, 4.8f, .9f), z.Rot * Quaternion.Euler(0, 0, 3f)); z.solid.Box(darkRock, c + up * 2.4f, new Vector3(2f, 4.8f, .9f), z.Rot);
            for (var k = 0; k < 5; k++) z.mk.Box(SongGlow(Mystification, new Color(.5f, .8f, 1f), 2f), c + up * (1f + k * .75f) + z.front * .46f + z.side * ((k % 2) * .4f - .2f), new Vector3(.5f, .08f, .02f), z.Rot * Quaternion.Euler(0, 0, k * 35f));
            halos.Add(c + up * 2.4f + z.front * .8f, 2f, new Color(.5f, .8f, 1f), .4f);
        }
    }
}
