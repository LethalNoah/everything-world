using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Twelfth House ring (part of SmallHoursKit): a path round the wheel's clearing with a station per song and
    /// sign, Aries to Pisces, starting just past where the forest path comes in and ending just before it, so the
    /// last ("Water") closes the circle beside the first ("The Ordinary World").
    public partial class SmallHoursKit
    {
        const float RingR = 34f;
        readonly Vector3[] ringPiece = new Vector3[12];
        readonly Vector3[] ringStop = new Vector3[12];
        readonly List<Vector3> ringPath = new List<Vector3>();
        float ringEntry;

        void RingPlan(LandContext ctx, Vector3 N, Vector3 F, Vector3 side, Vector3 C)
        {
            ringPath.Clear();
            ringEntry = Mathf.Atan2(N.z - C.z, N.x - C.x);
            for (var k = 0; k <= 96; k++)
            {
                var a = ringEntry + k * Mathf.PI * 2f / 96f; var p = G(ctx, C + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * RingR);
                ringPath.Add(p); Remember(p);
            }
            for (var i = 0; i < 12; i++)
            {
                var a = ringEntry + (i + .5f) * Mathf.PI * 2f / 12f; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                ringStop[i] = G(ctx, C + d * RingR); ringPiece[i] = G(ctx, C + d * (RingR + 8.5f));
                exhibitSpots.Add((ringPiece[i], 7f)); exhibitCentres.Add(ringPiece[i]);
            }
        }

        void RingBuild(LandContext ctx, Transform root, MeshKit mk, MeshKit solid, Transform labels)
        {
            Ribbon(ctx, mk, WorldMaterials.Solid(new Color(.16f, .22f, .24f)), ringPath, 2f, .06f);
            var stone = WorldMaterials.Solid(new Color(.2f, .22f, .26f));
            for (var i = 0; i < 12; i++)
            {
                var (id, title) = TwelfthHouse[i];
                var face = (ringStop[i] - ringPiece[i]); face.y = 0; face.Normalize();
                // a marker stone on the path with the sign and the song
                var tang = Vector3.Cross(Vector3.up, face); var mark = ringStop[i] + tang * 2.2f - face * 1.2f; mark = G(ctx, mark);
                mk.Box(stone, mark + Vector3.up * .55f, new Vector3(.9f, 1.1f, .25f), Quaternion.LookRotation(-face)); solid.Box(stone, mark + Vector3.up * .55f, new Vector3(.9f, 1.1f, .25f), Quaternion.LookRotation(-face));
                ArtistBits.Label(labels, Signs[i].ToUpper(), mark + Vector3.up * .85f + face * .14f, face, .11f, Ink, TextAnchor.MiddleCenter);
                ArtistBits.Label(labels, title, mark + Vector3.up * .55f + face * .14f, face, .08f, Ink, TextAnchor.MiddleCenter);
                mk.Box(Cue(id, Teal, .35f, 2.5f), mark + Vector3.up * 1.14f, new Vector3(.5f, .05f, .05f), Quaternion.LookRotation(-face));
                var c = ringPiece[i];
                switch (i)
                {
                    case 0: OrdinaryWorld(ctx, root, mk, solid, c, face, id); break;
                    case 1: SomethingMore(ctx, root, mk, solid, c, face, id); break;
                    case 2: BreakingThrough(ctx, root, mk, solid, c, face, id); break;
                    case 3: MomentsNotice(ctx, root, mk, solid, c, face, id); break;
                    case 4: LeyLines(ctx, root, mk, solid, c, face, id); break;
                    case 5: Gauntlet(ctx, mk, solid, c, face, id); break;
                    case 6: SiliconSanctum(ctx, root, mk, solid, c, face, id); break;
                    case 7: ScarabSong(ctx, root, mk, solid, c, face, id); break;
                    case 8: EndOfKatun(ctx, root, mk, solid, c, face, id); break;
                    case 9: Whiteout(ctx, mk, solid, c, face, id); break;
                    case 10: TrailGate(ctx, root, mk, solid, c, face, id); break;
                    case 11: Water(ctx, root, mk, solid, c, face, id); break;
                }
                ExMark(ctx, root, "exhibit, " + Signs[i] + ", " + title, c + Vector3.up * 2.5f, ringStop[i] + face * 4f + tang * 3f);
            }
        }

        // ---------------------------------------------------------------- the stations
        /// The first gate: a low stone wall with an open gate, ram's horns on its post, boots and a staff waiting,
        /// birds on the wall. A storm cloud passes over mid-song; a motif runs along the wall and a second rail
        /// copies it. ("Water" sets this wall's motif running again when it surfaces.)
        Material[] ordinaryMotif;
        void OrdinaryWorld(LandContext ctx, Transform root, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var side = Vector3.Cross(up, face); var rot = Quaternion.LookRotation(face);
            var wallM = WorldMaterials.Solid(new Color(.42f, .43f, .45f));
            foreach (var s in new[] { -1f, 1f })
            {
                var w = c + side * s * 3.2f; mk.Box(wallM, w + up * .5f, new Vector3(4f, 1f, .6f), Quaternion.LookRotation(face)); solid.Box(wallM, w + up * .5f, new Vector3(4f, 1f, .6f), Quaternion.LookRotation(face));
                mk.Box(stone, c + side * s * 1.1f + up * .75f, new Vector3(.45f, 1.5f, .45f), rot);
            }
            ArtistBits.PostCollider(root, c + side * 1.1f, 1.5f, .3f); ArtistBits.PostCollider(root, c - side * 1.1f, 1.5f, .3f);
            var horn = WorldMaterials.Solid(new Color(.85f, .8f, .68f));
            foreach (var s in new[] { -1f, 1f }) for (var k = 0; k < 6; k++) { var a = k * .55f; mk.Box(horn, c + side * (1.1f + s * (.15f + Mathf.Sin(a) * .18f)) + up * (1.55f + Mathf.Cos(a) * .18f) + face * .2f, new Vector3(.1f, .1f, .1f), rot); }
            var gate = Part(root, "The first gate", c - side * .9f + up * .1f, Quaternion.LookRotation(face) * Quaternion.Euler(0, -70f, 0));
            for (var k = 0; k < 4; k++) gate.mk.Box(darkWood, new Vector3(.9f, .2f + k * .32f, 0), new Vector3(1.8f, .08f, .05f), Quaternion.identity);
            Done(gate); solid.Box(darkWood, gate.t.position + gate.t.rotation * new Vector3(.9f, .65f, 0), new Vector3(1.8f, 1.3f, .15f), gate.t.rotation);
            var step = G(ctx, c - face * 2f + side * 1.8f); mk.Box(stone, step + up * .1f, new Vector3(1f, .2f, .6f), rot);
            foreach (var s in new[] { -.13f, .13f }) { mk.Box(darkWood, step + up * .32f + side * s, new Vector3(.12f, .25f, .22f), rot); mk.Box(darkWood, step + up * .24f + side * s + face * .12f, new Vector3(.12f, .08f, .3f), rot); }
            mk.Prism(darkWood, step + side * .5f, (up + side * .15f).normalized, .03f, 1.7f, 5);
            for (var k = 0; k < 4; k++) { var b = c + side * (2f + k * .7f) * (k % 2 == 0 ? 1f : -1f) + up * 1.05f; mk.Blob(WorldMaterials.Solid(new Color(.35f, .3f, .28f)), b, new Vector3(.1f, .08f, .13f), ctx.Rng, 0f); }
            // the motif: along the wall's top, then a second rail copying it
            ordinaryMotif = new Material[12];
            for (var k = 0; k < 12; k++)
            {
                var s = k < 6 ? -1f : 1f; var x = (k % 6 - 2.5f) * .62f; var p = c + side * s * 3.2f + side * x + up * 1.05f;
                mk.Box(ordinaryMotif[k] = Cue(id, Moon, .05f, 3f, .55f, .8f, 3f, -(k % 6) / 6f - (k < 6 ? 0f : .5f), .15f), p, new Vector3(.5f, .05f, .05f), Quaternion.LookRotation(face));
            }
            mk.Prism(Cue(id, WindowGlow, .2f, 1.5f, .15f, .4f), G(ctx, c) + up * .02f, up, 3f, .02f, 16);
            var cloud = Part(root, "The passing storm", c + up * 7f - side * 10f, Quaternion.identity);
            var cl = WorldMaterials.Solid(new Color(.35f, .37f, .42f));
            for (var k = 0; k < 5; k++) cloud.mk.Blob(cl, new Vector3((k - 2f) * 1.1f, ctx.Range(-.2f, .3f), ctx.Range(-.5f, .5f)), new Vector3(1.2f, .7f, 1f), ctx.Rng, .2f);
            for (var k = 0; k < 14; k++) cloud.mk.Box(Cue(id, Ice, .3f, 1.5f), new Vector3(ctx.Range(-2.5f, 2.5f), -1.5f - ctx.Range(0f, 2.5f), ctx.Range(-.6f, .6f)), new Vector3(.02f, .6f, .02f), Quaternion.identity);
            cloud.mk.Box(Cue(id, Color.white, 0f, 4f, .42f, .52f, .7f, 0f, .08f), new Vector3(.3f, -2.5f, 0), new Vector3(.08f, 3.5f, .08f), Quaternion.Euler(0, 0, 12f));
            Done(cloud);
            life.movers.Add(new SmallHoursLife.Mover { t = cloud.t, song = id, kind = SmallHoursLife.Move.Slide, a = c + up * 7f - side * 30f, b = c + up * 7f + side * 30f, from = .35f, to = .58f });
            cloud.t.position = c + up * 7f - side * 30f;
            ArtistBits.PointLight(root, c + up * 2.5f + face * 1.5f, WindowGlow, 10f, 1.3f);
        }

        /// A tall, slightly strange tower of mismatched layers on a stone base carved with a bull's horns, a slow pulse
        /// at its heart; long strings to the ground; a workbench below with one stool pushed back. Its layers light as
        /// the music stacks up; at the break a deep glow rolls out through the ground; the chord hits flash it all.
        void SomethingMore(LandContext ctx, Transform root, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var side = Vector3.Cross(up, face); var rot = Quaternion.LookRotation(face);
            Material[] mats = { WorldMaterials.Solid(new Color(.4f, .4f, .42f)), WorldMaterials.Solid(new Color(.45f, .33f, .22f)), WorldMaterials.Solid(new Color(.25f, .26f, .3f)), WorldMaterials.Solid(new Color(.55f, .7f, .8f), .9f), WorldMaterials.Solid(new Color(.3f, .3f, .32f)) };
            mk.Box(mats[0], c + up * .6f, new Vector3(3f, 1.2f, 3f), rot); solid.Box(mats[0], c + up * 1.5f, new Vector3(3f, 3f, 3f), rot);
            var horn = WorldMaterials.Solid(new Color(.85f, .8f, .68f));
            foreach (var s in new[] { -1f, 1f }) for (var k = 0; k < 6; k++) { var a = k * .5f; mk.Box(horn, c + face * 1.52f + side * s * (.3f + Mathf.Sin(a) * .5f) + up * (.9f - Mathf.Cos(a) * .3f + k * .03f), new Vector3(.14f, .14f, .05f), rot); }
            var y = 1.2f;
            for (var k = 0; k < 8; k++)
            {
                var w = 2.4f - k * .2f; var h = .8f + (k % 3) * .3f; var twist = Quaternion.Euler(0, k * 13f, 0);
                mk.Box(mats[1 + k % 4], c + up * (y + h / 2f), new Vector3(w, h, w), rot * twist);
                mk.Box(Cue(id, k % 2 == 0 ? Moon : Teal, .1f, 2.6f, .2f + k * .04f, .7f), c + up * (y + h * .5f) + rot * twist * (Vector3.forward * (w / 2f + .02f)), new Vector3(w * .6f, .08f, .02f), rot * twist);
                y += h;
            }
            var heart = c + up * (y * .55f); mk.Blob(Cue(id, Moon, .8f, 2f, 0f, 1f, 1.3f, 0f, .3f), heart + face * 1.3f, Vector3.one * .25f, ctx.Rng, 0f);
            var str = Cue(id, new Color(.85f, .8f, .65f), .2f, 2f);
            foreach (var s in new[] { -1f, 1f }) for (var k = 0; k < 3; k++) { var top = c + up * (y - .3f); var end = G(ctx, c + side * s * (5f + k) + face * (k - 1f) * 1.5f); mk.Box(str, (top + end) / 2f, new Vector3(.02f, .02f, Vector3.Distance(top, end)), Quaternion.LookRotation(end - top)); }
            mk.Box(Cue(id, new Color(.3f, .9f, .8f), 0f, 3f, .7f, .86f, 1.5f, 0f, .5f), G(ctx, c) + up * .03f, new Vector3(9f, .02f, 9f), rot * Quaternion.Euler(0, 45f, 0));
            mk.Box(Cue(id, Color.white, 0f, 3f, .86f, .92f, .4f, 0f, .3f), c + up * (y / 2f), new Vector3(2.7f, y - 1.3f, 2.7f) * .5f, rot);
            var bench = G(ctx, c + face * 2.8f + side * 1.5f); mk.Box(darkWood, bench + up * .8f, new Vector3(1.6f, .07f, .6f), rot); solid.Box(darkWood, bench + up * .4f, new Vector3(1.6f, .8f, .6f), rot);
            foreach (var (s, push) in new[] { (-.4f, 0f), (.6f, .7f) }) { var st = bench + side * s + face * (.6f + push); mk.Box(darkWood, st + up * .5f, new Vector3(.38f, .05f, .38f), rot); mk.Prism(darkWood, st, up, .04f, .5f, 4); ArtistBits.PostCollider(root, st, .55f, .25f); }
            ArtistBits.PointLight(root, c + up * 4f + face * 3f, Moon, 12f, 1.3f);
        }

        /// Six tall glass panels between two mirrored twin pillars, each showing the same walking pattern of light,
        /// each glitching its own way, joining one by one; then the last cracks and the pattern passes into a real
        /// guitar, drum and bass on a plain wooden frame.
        void BreakingThrough(LandContext ctx, Transform root, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var side = Vector3.Cross(up, face); var rot = Quaternion.LookRotation(face);
            var pillar = WorldMaterials.Solid(new Color(.75f, .74f, .72f));
            foreach (var s in new[] { -1f, 1f }) { var p = c + side * s * 5.4f; mk.Prism(pillar, p, up, .4f, 4f, 10); mk.Blob(pillar, p + up * 4.3f, Vector3.one * .45f, ctx.Rng, 0f); ArtistBits.PostCollider(root, p, 4f, .5f); }
            Color[] tints = { Teal, Lime, Orchid, Moon, Ice, Magenta };
            for (var k = 0; k < 6; k++)
            {
                var pc = c + side * ((k - 2.5f) * 1.6f) + up * 1.6f;
                mk.Box(WorldMaterials.Solid(new Color(.12f, .14f, .18f)), pc - face * .05f, new Vector3(1.3f, 2.8f, .08f), rot); solid.Box(darkWood, pc - face * .05f, new Vector3(1.3f, 2.8f, .2f), rot);
                var join = .05f + k * .09f;
                for (var j = 0; j < 7; j++)
                {
                    var y = -1.1f + j * .36f; var x = Mathf.Sin(j * 1.7f + k * .3f) * .35f;
                    var size = k switch { 0 => new Vector3(.6f, .03f, .02f), 1 => new Vector3(.18f, .18f, .02f), 2 => new Vector3(.08f, .08f, .02f), 3 => new Vector3(.5f, .02f, .02f), 4 => new Vector3(.4f, .06f, .02f), _ => new Vector3(.3f, .1f, .02f) };
                    mk.Box(Cue(id, tints[k], .05f, 3f, join, .7f, 2.1f, -j / 7f, .2f), pc + side * x + up * y, size, rot * Quaternion.Euler(0, 0, k == 3 ? 30f : 0f));
                }
            }
            var frame = c + side * 6.8f + face * 1.5f; mk.Box(darkWood, frame + up * 1.2f, new Vector3(.1f, 2.4f, 1.6f), rot); mk.Box(darkWood, frame + up * .05f, new Vector3(1.4f, .1f, 1.6f), rot);
            var warm = Cue(id, WindowGlow, .15f, 3f, .7f, 1f);
            mk.Blob(warm, frame + up * 1.2f + side * .2f, new Vector3(.25f, .35f, .1f), ctx.Rng, 0f); mk.Box(darkWood, frame + up * 1.85f + side * .2f, new Vector3(.05f, .7f, .04f), rot);
            mk.Prism(warm, frame + up * .1f + side * .3f + face * .5f, up, .28f, .35f, 12);
            mk.Blob(warm, frame + up * .9f + side * .2f - face * .5f, new Vector3(.2f, .5f, .12f), ctx.Rng, 0f);
            solid.Box(darkWood, frame + up * .6f, new Vector3(1.4f, 1.2f, 1.6f), rot);
            var crack = Cue(id, Color.white, 0f, 4f, .68f, .75f, .3f, 0f, .5f);
            for (var k = 0; k < 5; k++) mk.Box(crack, c + side * (2.5f * 1.6f) + up * (1.6f + (k - 2f) * .4f) + face * .01f, new Vector3(.7f, .02f, .01f), rot * Quaternion.Euler(0, 0, k * 37f));
            ArtistBits.PointLight(root, c + up * 2.5f + face * 3f, Teal, 12f, 1.3f);
        }

        /// A small round shelter curled like a shell by the water, a crescent moon on a post, a packed bag at its door,
        /// four instrument stands in an arc taking turns to glow; then the moon's reflection and the shelter's lamp
        /// alone; then all of them together.
        void MomentsNotice(LandContext ctx, Transform root, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var side = Vector3.Cross(up, face); var rot = Quaternion.LookRotation(face);
            var shell = WorldMaterials.Solid(new Color(.75f, .7f, .65f));
            var sh = c - face * 1.5f;
            for (var k = 0; k < 7; k++) { var a = k * .45f; mk.Blob(shell, sh + up * (1.4f - k * .12f) + side * Mathf.Sin(a) * (1.6f - k * .15f) - face * Mathf.Cos(a) * .4f, new Vector3(1.6f - k * .15f, 1.2f - k * .1f, 1.3f), ctx.Rng, .1f); }
            solid.Box(shell, sh + up * 1f - face * .6f, new Vector3(3f, 2f, 1.6f), rot);
            mk.Box(Cue(id, WindowGlow, .6f, 1.8f, .6f, 1f), sh + up * .9f + face * .3f, new Vector3(1.2f, .9f, .05f), rot);
            mk.Box(WorldMaterials.Solid(new Color(.6f, .45f, .5f)), sh + up * .12f + face * .9f, new Vector3(1.4f, .05f, .9f), rot);
            var bag = sh + face * 1.2f + side * .9f; mk.Box(WorldMaterials.Solid(new Color(.4f, .3f, .25f)), bag + up * .25f, new Vector3(.6f, .45f, .3f), rot); mk.Box(darkWood, bag + up * .52f, new Vector3(.3f, .05f, .05f), rot);
            var pool = WorldMaterials.Solid(new Color(.08f, .12f, .18f), .95f); var pl = G(ctx, c + face * 3f - side * 3f); mk.Prism(pool, pl + up * .02f, up, 2.2f, .02f, 18);
            var post = G(ctx, pl + side * 2.4f); mk.Prism(darkWood, post, up, .05f, 3f, 5); ArtistBits.PostCollider(root, post, 3f, .1f);
            var moonM = Cue(id, new Color(.95f, .92f, .8f), .9f, 1f);
            for (var k = 0; k <= 8; k++) { var a = -Mathf.PI / 2f + k * Mathf.PI / 8f; mk.Blob(moonM, post + up * 3.2f + (up * Mathf.Sin(a) + side * Mathf.Cos(a)) * .3f, Vector3.one * (.05f + .05f * Mathf.Cos(a)), ctx.Rng, 0f); }
            mk.Prism(Cue(id, new Color(.9f, .88f, .75f), .15f, 3f, .6f, 1f), pl + up * .045f, up, .5f, .01f, 12);
            Color[] cols = { WindowGlow, Lime, Moon, Orchid };
            for (var k = 0; k < 4; k++)
            {
                var a = (k - 1.5f) * .45f; var st = G(ctx, c + (face * Mathf.Cos(a) + side * Mathf.Sin(a)) * 4.5f);
                mk.Prism(iron, st, up, .03f, .9f, 5); ArtistBits.PostCollider(root, st, 1f, .2f);
                var g = Cue(id, cols[k], .1f, 3f, 0f, .6f, 8f, -k / 4f, .25f);
                switch (k)
                {
                    case 0: mk.Blob(g, st + up * 1.1f, new Vector3(.25f, .32f, .1f), ctx.Rng, 0f); break;
                    case 1: mk.Prism(g, st + up * .9f, (up + side * .2f).normalized, .03f, .65f, 6); break;
                    case 2: mk.Box(g, st + up * 1.1f, new Vector3(.24f, .4f, .06f), rot); break;
                    case 3: mk.Box(g, st + up * 1f, new Vector3(.6f, .08f, .25f), rot); break;
                }
                mk.Blob(Cue(id, cols[k], 0f, 2.5f, .92f, 1f), st + up * 1.45f, Vector3.one * .07f, ctx.Rng, 0f);
            }
            ArtistBits.PointLight(root, sh + up * 1.2f + face * 1.6f, WindowGlow, 9f, 1.3f);
        }

        /// A ring of standing stones round a lion built of glowing lines with a mane of fire on a sun disc. Lasers draw
        /// turning geometry between the stones; lines of light race out to every other piece of the exhibit; fire
        /// bursts from the stones; the lion's mane flares.
        void LeyLines(LandContext ctx, Transform root, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var side = Vector3.Cross(up, face);
            var stoneM = WorldMaterials.Solid(new Color(.38f, .38f, .42f)); var tops = new List<Vector3>();
            for (var k = 0; k < 8; k++)
            {
                var a = k * Mathf.PI / 4f; var p = G(ctx, c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 5.5f);
                mk.Box(stoneM, p + up * 1.4f, new Vector3(.8f, 2.8f, .5f), Quaternion.Euler(0, -a * Mathf.Rad2Deg + 90f, 0)); solid.Box(stoneM, p + up * 1.4f, new Vector3(.8f, 2.8f, .5f), Quaternion.Euler(0, -a * Mathf.Rad2Deg + 90f, 0));
                tops.Add(p + up * 2.95f);
                mk.Blob(Cue(id, new Color(1f, .5f, .15f), 0f, 3f, .3f, 1f, .5f, k / 8f, .3f), p + up * 3.2f, new Vector3(.25f, .4f, .25f), ctx.Rng, .3f);
            }
            mk.Prism(Cue(id, new Color(1f, .78f, .25f), .5f, 2f), G(ctx, c) + up * .05f, up, 2.4f, .08f, 24);
            // the lion of light
            var lion = Cue(id, new Color(1f, .82f, .35f), .8f, 2.5f); var L = G(ctx, c) + up * .2f; var f = face; var s2 = side;
            void Line(Vector3 a, Vector3 b) => mk.Box(lion, L + (a + b) / 2f, new Vector3(.05f, .05f, Vector3.Distance(a, b) + .05f), Quaternion.LookRotation(b - a + Vector3.one * .0001f));
            Vector3 V(float x, float y, float z) => s2 * x + up * y + f * z;
            Vector3[] body = { V(0, 1.1f, -1.2f), V(0, 1.3f, .6f), V(0, 1.9f, 1.1f), V(0, 1.6f, 1.6f), V(0, 1f, 1.4f) };
            for (var k = 0; k + 1 < body.Length; k++) Line(body[k], body[k + 1]);
            foreach (var x in new[] { -.35f, .35f }) { Line(V(x, 0, -1f), V(x * .6f, 1.1f, -1.1f)); Line(V(x, 0, .6f), V(x * .6f, 1.2f, .5f)); }
            Line(V(0, 1.1f, -1.2f), V(0, 1.6f, -1.9f));
            ArtistBits.PostCollider(root, G(ctx, c), 2.2f, 1.2f);
            var mane = Cue(id, new Color(1f, .45f, .1f), .6f, 3f, .3f, 1f, .5f, 0f, .5f);
            for (var k = 0; k < 14; k++) { var a = k * Mathf.PI * 2f / 14f; mk.Blob(mane, L + V(0, 1.8f, 1.15f) + (s2 * Mathf.Cos(a) + up * Mathf.Sin(a)) * .5f, new Vector3(.14f, .2f, .14f), ctx.Rng, .3f); }
            // lasers between the stones: a star and a ring (only there while it plays)
            var lasers = Part(root, "Ley lasers", Vector3.zero, Quaternion.identity);
            for (var k = 0; k < 8; k++)
            {
                var a = tops[k]; var b = tops[(k + 3) % 8]; var b2 = tops[(k + 1) % 8];
                lasers.mk.Box(Cue(id, k % 2 == 0 ? Teal : Lime, 0f, 3f, .2f, 1f, .25f, k / 8f, .6f), (a + b) / 2f, new Vector3(.04f, .04f, Vector3.Distance(a, b)), Quaternion.LookRotation(b - a));
                lasers.mk.Box(Cue(id, Orchid, 0f, 3f, .2f, 1f, .5f, k / 8f, .5f), (a + b2) / 2f, new Vector3(.04f, .04f, Vector3.Distance(a, b2)), Quaternion.LookRotation(b2 - a));
            }
            Done(lasers); Moves(lasers.t, id, SmallHoursLife.Move.Show, default, default, .3f, .2f, 1f);
            var geoShow = Part(root, "Ley geometry show", c + up * 8f, Quaternion.identity); Done(geoShow);
            var geo = Part(geoShow.t, "Ley geometry", c + up * 8f, Quaternion.identity);
            var gm = Cue(id, Teal, 0f, 3f, .2f, 1f);
            for (var r = 0; r < 3; r++) for (var k = 0; k < 6; k++) { var a0 = k * Mathf.PI / 3f; var a1 = (k + 2) * Mathf.PI / 3f; var rr = 2f + r * 1.2f; var p0 = Quaternion.Euler(r * 30f, 0, 0) * new Vector3(Mathf.Cos(a0) * rr, 0, Mathf.Sin(a0) * rr); var p1 = Quaternion.Euler(r * 30f, 0, 0) * new Vector3(Mathf.Cos(a1) * rr, 0, Mathf.Sin(a1) * rr); geo.mk.Box(gm, (p0 + p1) / 2f, new Vector3(.04f, .04f, Vector3.Distance(p0, p1)), Quaternion.LookRotation(p1 - p0)); }
            Done(geo); geo.t.localPosition = Vector3.zero; Moves(geo.t, id, SmallHoursLife.Move.Spin, new Vector3(.3f, 1f, .2f).normalized, default, 8f, 0f, 1f, 4f);
            Moves(geoShow.t, id, SmallHoursLife.Move.Show, default, default, .4f, .2f, 1f);
            // the ley lines out to every other piece: only there while it plays
            var leyPart = Part(root, "Ley lines", Vector3.zero, Quaternion.identity);
            var ley = Cue(id, new Color(1f, .85f, .4f), 0f, 2.2f, .25f, 1f, 1.2f, 0f, .7f);
            foreach (var t in exhibitCentres)
            {
                if ((t - c).sqrMagnitude < 4f) continue;
                var a = c + up * 3f; var b = t + up * 2f; leyPart.mk.Box(ley, (a + b) / 2f, new Vector3(.05f, .05f, Vector3.Distance(a, b)), Quaternion.LookRotation(b - a));
            }
            Done(leyPart); Moves(leyPart.t, id, SmallHoursLife.Move.Show, default, default, 1.2f, .25f, 1f);
            ArtistBits.PointLight(root, c + up * 4f, new Color(1f, .7f, .3f), 14f, 1.8f);
        }

        /// A small cathedral of circuitry: arches of dark green board with gold traces, windows of lit chip grids, a
        /// round rose window like a silicon die, organ pipes of glowing tubes; at the altar balance scales holding a
        /// candle and a chip. Pipes glow in chords, pulses race through the traces, the scales swing and settle.
        void SiliconSanctum(LandContext ctx, Transform root, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var side = Vector3.Cross(up, face); var rot = Quaternion.LookRotation(face);
            var board = WorldMaterials.Solid(new Color(.1f, .4f, .26f)); var candle = WorldMaterials.Solid(new Color(.95f, .92f, .82f)); var gold = Cue(id, new Color(1f, .78f, .3f), .8f, 2.5f, .12f, 1f, .4f, 0f, .4f);
            foreach (var s in new[] { -1f, 1f })
            {
                mk.Box(board, c + side * s * 2.6f + up * 2.5f, new Vector3(.3f, 5f, 6f), rot); solid.Box(board, c + side * s * 2.6f + up * 2.5f, new Vector3(.3f, 5f, 6f), rot);
                for (var k = 0; k < 3; k++)
                {
                    var w = c + side * s * 2.44f + face * (k - 1f) * 1.8f + up * 2.6f;
                    for (var i = 0; i < 3; i++) for (var j = 0; j < 4; j++) mk.Box(Cue(id, k % 2 == 0 ? Teal : Orchid, .25f, 2.5f, .25f, 1f, .6f, (i + j * 3) / 12f, .4f), w + face * ((i - 1f) * .25f) + up * ((j - 1.5f) * .3f), new Vector3(.02f, .18f, .18f), rot);
                    for (var t = 0; t < 4; t++) mk.Box(gold, w + up * (-1.4f - t * .15f) + face * ((t - 1.5f) * .3f), new Vector3(.02f, .03f, .5f), rot);
                }
                foreach (var z in new[] { -2.7f, 2.7f }) { mk.Prism(board, c + side * s * 2.6f + face * z + up * 5f, up, .25f, 2f, 4, .1f); }
            }
            for (var k = 0; k < 7; k++) { var a = (k / 6f) * Mathf.PI; mk.Box(board, c - face * 3f + up * (5f + Mathf.Sin(a) * 1.4f) + side * Mathf.Cos(a) * 2.5f, new Vector3(.8f, .3f, .3f), Quaternion.LookRotation(face) * Quaternion.Euler(0, 0, a * Mathf.Rad2Deg + 90f)); }
            var rose = Part(root, "Silicon rose window", c - face * 2.95f + up * 4f, rot);
            rose.mk.Prism(WorldMaterials.Solid(new Color(.15f, .17f, .2f)), Vector3.zero, Vector3.forward, 1.3f, .05f, 24);
            for (var k = 0; k < 12; k++) { var a = k * Mathf.PI / 6f; rose.mk.Box(Cue(id, k % 3 == 0 ? Moon : k % 3 == 1 ? Teal : Orchid, .3f, 3f, .55f, 1f, .5f, k / 12f, .5f), new Vector3(Mathf.Cos(a) * .8f, Mathf.Sin(a) * .8f, .06f), new Vector3(.35f, .12f, .02f), Quaternion.Euler(0, 0, a * Mathf.Rad2Deg)); }
            rose.mk.Box(gold, new Vector3(0, 0, .07f), new Vector3(.4f, .4f, .02f), Quaternion.identity);
            Done(rose); Moves(rose.t, id, SmallHoursLife.Move.Spin, Vector3.forward, default, 4f, 0f, 1f, 5f);
            mk.Box(board, c - face * 3.1f + up * 2.5f, new Vector3(5.5f, 5f, .2f), rot); solid.Box(board, c - face * 3.1f + up * 2.5f, new Vector3(5.5f, 5f, .2f), rot);
            for (var k = 0; k < 9; k++) { var h = 1.2f + Mathf.Sin(k / 8f * Mathf.PI) * 1.4f; mk.Prism(Cue(id, new Color(.9f, .85f, 1f), .2f, 2.6f, 0f, .2f, 3f, k / 9f, .6f), c - face * 2.7f + side * ((k - 4f) * .32f) + up * .9f, up, .1f, h, 8); }
            var altar = c - face * 1.5f; mk.Box(WorldMaterials.Solid(new Color(.2f, .2f, .22f)), altar + up * .5f, new Vector3(1.4f, 1f, .7f), rot); solid.Box(darkWood, altar + up * .5f, new Vector3(1.4f, 1f, .7f), rot);
            mk.Prism(iron, altar + up * 1f, up, .04f, .9f, 6);
            var beam = Part(root, "Silicon scales", altar + up * 1.9f, rot);
            beam.mk.Box(iron, Vector3.zero, new Vector3(1.2f, .04f, .04f), Quaternion.identity);
            foreach (var s in new[] { -.55f, .55f }) { beam.mk.Box(iron, new Vector3(s, -.2f, 0), new Vector3(.01f, .4f, .01f), Quaternion.identity); beam.mk.Prism(iron, new Vector3(s, -.42f, 0), Vector3.up, .2f, .02f, 10); }
            beam.mk.Prism(candle, new Vector3(-.55f, -.4f, 0), Vector3.up, .04f, .14f, 6); beam.mk.Blob(Cue(id, new Color(1f, .75f, .4f), 2f, 1f), new Vector3(-.55f, -.2f, 0), new Vector3(.025f, .05f, .025f), ctx.Rng, 0f);
            beam.mk.Box(WorldMaterials.Solid(new Color(.1f, .12f, .1f)), new Vector3(.55f, -.38f, 0), new Vector3(.12f, .03f, .12f), Quaternion.identity); beam.mk.Box(gold, new Vector3(.55f, -.36f, 0), new Vector3(.08f, .01f, .08f), Quaternion.identity);
            Done(beam); Moves(beam.t, id, SmallHoursLife.Move.Swing, Vector3.forward, default, 1.4f, .45f, .9f, 14f);
            ArtistBits.PointLight(root, c + up * 3f, new Color(.6f, 1f, .8f), 12f, 2f, true);
            ArtistBits.PointLight(root, c + face * 4f + up * 3f, new Color(.7f, .9f, 1f), 12f, 1.6f);
            halos.Add(c - face * 2.95f + up * 4f + face * .5f, 2f, Teal, .5f);
        }

        /// A great stone gateway down into the earth, a scorpion carved on its lintel, glyphs on its walls; a giant black
        /// scarab pushing a deep red sun down into the dark. The sun sinks over the song, the ground throbs red with
        /// the bass, the glyphs whisper, and at the end only an ember glow is left at the bottom.
        void ScarabSong(LandContext ctx, Transform root, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var side = Vector3.Cross(up, face); var rot = Quaternion.LookRotation(face);
            var sand = WorldMaterials.Solid(new Color(.55f, .46f, .35f)); var dark = WorldMaterials.Solid(new Color(.05f, .04f, .05f));
            var gateAt = c - face * 2f;
            foreach (var s in new[] { -1f, 1f }) { mk.Box(sand, gateAt + side * s * 1.8f + up * 2f, new Vector3(1f, 4f, 1.2f), rot); solid.Box(sand, gateAt + side * s * 1.8f + up * 2f, new Vector3(1f, 4f, 1.2f), rot); }
            mk.Box(sand, gateAt + up * 4.3f, new Vector3(4.8f, .7f, 1.4f), rot);
            mk.Box(dark, gateAt + up * 1.7f - face * .2f, new Vector3(2.6f, 3.4f, .1f), rot);
            mk.Box(dark, gateAt - face * 1.2f + up * .02f, new Vector3(2.6f, .04f, 2f), rot);
            var sc = WorldMaterials.Solid(new Color(.3f, .22f, .16f));
            for (var k = 0; k < 7; k++) mk.Box(sc, gateAt + up * 4.3f + face * .72f + side * ((k - 3f) * .28f) + up * Mathf.Sin(k * .9f) * .1f, new Vector3(.22f, .12f, .02f), rot);
            mk.Box(sc, gateAt + up * 4.55f + face * .72f + side * 1f, new Vector3(.08f, .35f, .02f), rot * Quaternion.Euler(0, 0, -30f));
            var glyph = Cue(id, new Color(1f, .3f, .15f), .15f, 2.5f, 0f, 1f, .6f, 0f, .35f);
            foreach (var s in new[] { -1f, 1f }) for (var k = 0; k < 6; k++) mk.Box(Cue(id, new Color(1f, .3f, .15f), .1f, 2.5f, 0f, 1f, .7f + k * .13f, k * .2f, .3f), gateAt + side * s * 1.8f + face * .61f + up * (.6f + k * .55f), new Vector3(.3f, .2f, .02f), rot * Quaternion.Euler(0, 0, k * 45f));
            var throb = Cue(id, new Color(.8f, .05f, .05f), .1f, 2.5f, 0f, .95f, .55f, 0f, .35f);
            for (var k = 0; k < 9; k++) { var a = k * .7f; var p0 = c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 1.5f + up * .05f; var p1 = c + new Vector3(Mathf.Cos(a + .3f), 0, Mathf.Sin(a + .3f)) * 6f + up * .05f; mk.Box(throb, (p0 + p1) / 2f, new Vector3(.12f, .02f, Vector3.Distance(p0, p1)), Quaternion.LookRotation(p1 - p0)); }
            // the scarab
            var beetle = c + face * 1.2f + side * .6f; var shellM = WorldMaterials.Solid(new Color(.06f, .06f, .08f), .9f);
            mk.Blob(shellM, beetle + up * .9f, new Vector3(1.1f, .8f, 1.4f), ctx.Rng, .05f); mk.Blob(shellM, beetle + up * .8f - face * 1.2f, new Vector3(.6f, .45f, .5f), ctx.Rng, .05f);
            foreach (var s in new[] { -1f, 1f }) for (var k = 0; k < 3; k++) mk.Box(shellM, beetle + side * s * 1.1f + face * ((k - 1f) * .6f) + up * .35f, new Vector3(.8f, .08f, .08f), rot * Quaternion.Euler(0, s * (30f - k * 30f), s * 30f));
            solid.Box(shellM, beetle + up * .8f, new Vector3(2.2f, 1.6f, 3f), rot);
            mk.Box(Cue(id, new Color(.5f, .7f, 1f), .3f, 1f), beetle + up * 1.5f, new Vector3(.9f, .02f, .02f), rot);
            // the red sun, sinking into the gate over the song
            var sun = Part(root, "The scarab's sun", gateAt + up * 3f - face * .4f, rot);
            sun.mk.Prism(Cue(id, new Color(.9f, .15f, .05f), .7f, 2.5f, 0f, .95f), Vector3.zero, Vector3.forward, 1.3f, .3f, 24);
            Done(sun);
            life.movers.Add(new SmallHoursLife.Mover { t = sun.t, song = id, kind = SmallHoursLife.Move.Slide, a = gateAt + up * 3f + face * .8f, b = gateAt - up * 1.2f - face * .4f, from = .05f, to = .95f });
            sun.t.position = gateAt + up * 3f + face * .8f;
            mk.Prism(Cue(id, new Color(1f, .35f, .1f), .2f, 2f, .95f, 1f), gateAt - face * 1.2f + up * .05f, up, .6f, .02f, 12);
            ArtistBits.PointLight(root, c + up * 2f + face * 2f, new Color(1f, .3f, .15f), 10f, 1.4f);
        }

        /// A celebration plaza: a stepped platform with a newly raised stela, a great calendar stone ringed with glyph
        /// segments, hand drums round the base, jade and gold banners, a great stone bow. The drums glow on the beat,
        /// the calendar's segments light round the ring, and at the solo it blazes and arrows of light fly up and burst.
        void EndOfKatun(LandContext ctx, Transform root, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var side = Vector3.Cross(up, face); var rot = Quaternion.LookRotation(face);
            var lime = WorldMaterials.Solid(new Color(.72f, .7f, .62f));
            for (var k = 0; k < 3; k++) { var w = 6f - k * 1.6f; mk.Box(lime, c + up * (.25f + k * .5f), new Vector3(w, .5f, w), rot); solid.Box(lime, c + up * (.25f + k * .5f), new Vector3(w, .5f, w), rot); }
            mk.Box(lime, c + up * 2.6f, new Vector3(.7f, 2.2f, .4f), rot); mk.Box(WorldMaterials.Solid(new Color(.6f, .58f, .5f)), c + up * 2.6f + face * .21f, new Vector3(.5f, 1.8f, .02f), rot);
            var cal = c - face * 3.8f + up * 3.8f;
            mk.Prism(lime, cal - face * .2f, face, 3f, .35f, 20); solid.Box(lime, cal, new Vector3(6f, 6f, .5f), rot);
            for (var k = 0; k < 20; k++) { var a = Mathf.PI / 2f - k * Mathf.PI * 2f / 20f; var rr = Quaternion.LookRotation(face) * new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0); mk.Box(Cue(id, new Color(.35f, 1f, .6f), .1f, 2.8f, .05f + k * .028f, 1f), cal + rr * 2.45f + face * .2f, new Vector3(.5f, .35f, .03f), Quaternion.LookRotation(face) * Quaternion.Euler(0, 0, a * Mathf.Rad2Deg)); }
            mk.Prism(Cue(id, new Color(1f, .8f, .3f), .2f, 3f, .62f, 1f), cal + face * .2f, face, 1.3f, .04f, 20);
            for (var k = 0; k < 6; k++) { var a = k * Mathf.PI / 3f + .3f; var p = G(ctx, c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 4.4f); mk.Prism(WorldMaterials.Solid(new Color(.5f, .32f, .2f)), p, up, .32f, .7f, 10); mk.Prism(Cue(id, new Color(1f, .7f, .35f), .2f, 3f, 0f, 1f, .45f, k * .17f, .3f), p + up * .7f, up, .32f, .02f, 10); solid.Prism(darkWood, p, up, .35f, .7f, 8); }
            Color[] ban = { new Color(.1f, .55f, .4f), new Color(.85f, .65f, .2f) };
            for (var k = 0; k < 4; k++)
            {
                var p = G(ctx, c + side * ((k - 1.5f) * 2.6f) + face * 4f); mk.Prism(darkWood, p, up, .05f, 4.5f, 5); ArtistBits.PostCollider(root, p, 4.5f, .1f);
                var flag = Part(root, "K'atun banner", p + up * 4.2f, rot); flag.mk.Box(WorldMaterials.Solid(ban[k % 2]), new Vector3(.5f, -.5f, 0), new Vector3(1f, 1.2f, .02f), Quaternion.identity); Done(flag);
                Moves(flag.t, id, SmallHoursLife.Move.Swing, Vector3.up, default, 2.5f, .6f, 1f, 30f);
            }
            var bow = c + side * 5f - face * 1f; var bowM = WorldMaterials.Solid(new Color(.5f, .48f, .45f));
            for (var k = 0; k < 9; k++) { var a = -Mathf.PI / 2.6f + k * (Mathf.PI / 1.3f) / 8f; mk.Box(bowM, bow + up * (2.5f + Mathf.Sin(a) * 2.2f) - face * Mathf.Cos(a) * .9f + up * .1f, new Vector3(.3f, .7f, .3f), rot * Quaternion.Euler(-a * Mathf.Rad2Deg, 0, 0)); }
            mk.Box(WorldMaterials.Solid(new Color(.85f, .82f, .75f)), bow + up * 2.6f, new Vector3(.02f, 4.2f, .02f), rot);
            ArtistBits.PostCollider(root, bow, 4.5f, .6f);
            var arrows = Part(root, "K'atun arrows", bow + up * 3f, Quaternion.identity);
            for (var k = 0; k < 24; k++) { var d = new Vector3(ctx.Range(-.6f, .6f), 1f, ctx.Range(-.6f, .6f)).normalized; arrows.mk.Box(WorldMaterials.Glow(k % 2 == 0 ? new Color(1f, .8f, .3f) : new Color(.35f, 1f, .6f), 3f), d * ctx.Range(6f, 16f), new Vector3(.08f, .08f, 1.2f), Quaternion.LookRotation(d)); }
            Done(arrows); Moves(arrows.t, id, SmallHoursLife.Move.Show, default, default, .25f, .62f, .95f);
            ArtistBits.PointLight(root, c + up * 4f + face * 2f, new Color(.6f, 1f, .7f), 13f, 1.5f);
        }

        /// The start of the Synchronous World's trail: a warm gold arch with 22 small lamps along it.
        void TrailGate(LandContext ctx, Transform root, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var side = Vector3.Cross(up, face); var rot = Quaternion.LookRotation(face);
            foreach (var s in new[] { -1f, 1f }) { mk.Box(darkWood, c + side * s * 1.5f + up * 1.6f, new Vector3(.25f, 3.2f, .25f), rot); ArtistBits.PostCollider(root, c + side * s * 1.5f, 3.2f, .2f); }
            for (var k = 0; k < 22; k++) { var a = Mathf.PI - k * Mathf.PI / 21f; mk.Blob(Cue(id, Color.Lerp(new Color(1f, .7f, .3f), Ice, k / 21f), .5f, 2.5f, k / 22f, 1f), c + side * Mathf.Cos(a) * 1.5f + up * (3.2f + Mathf.Sin(a) * 1.2f), Vector3.one * .08f, ctx.Rng, 0f); }
            ArtistBits.Label(ctx.Root, "THE SYNCHRONOUS WORLD", c + up * 2.7f + face * .14f, face, .14f, Ink, TextAnchor.MiddleCenter);
        }

        /// A waterfall off a rock ledge into a deep clear pool, glowing creatures of imagination drifting far down, two
        /// fish circling near the surface, a shaft of light. The light plunges, the creatures wake, bubbles rise in
        /// slow pulses, lights climb back up, and when they break the surface the first gate's motif runs again.
        void Water(LandContext ctx, Transform root, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var side = Vector3.Cross(up, face); var rot = Quaternion.LookRotation(face);
            var g = G(ctx, c); var waterM = WorldMaterials.Solid(new Color(.15f, .35f, .5f), .95f);
            mk.Prism(waterM, g + up * .1f, up, 4.5f, .02f, 28);
            for (var k = 0; k < 20; k++) { var a = k * Mathf.PI / 10f; mk.Blob(stone, g + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 4.7f + up * .2f, new Vector3(.6f, .45f, .5f), ctx.Rng, .2f); ArtistBits.PostCollider(root, g + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 4.7f, .6f, .5f); }
            var ledge = g - face * 5.5f; var rockM = WorldMaterials.Solid(new Color(.3f, .3f, .33f));
            mk.Blob(rockM, ledge + up * 2.5f, new Vector3(3f, 3f, 1.8f), ctx.Rng, .2f); solid.Box(rockM, ledge + up * 2.5f, new Vector3(4.5f, 5f, 2.8f), rot);
            for (var k = 0; k < 9; k++) mk.Box(Cue(id, new Color(.3f, .6f, 1f), .22f, 2.5f, 0f, 1f, 1.5f, k / 9f, .5f), ledge + face * (1.6f + (k % 2) * .08f) + side * ((k - 4f) * .26f) + up * (2.2f - (k % 3) * .2f), new Vector3(.08f, 4.2f - (k % 3) * .4f, .04f), rot);
            // the deep: creatures of imagination glowing far down under the surface
            var deep = Cue(id, new Color(.3f, .7f, 1f), .15f, 3f, .2f, .75f);
            var deepG = Cue(id, new Color(.45f, 1f, .7f), .1f, 3f, .25f, .75f);
            for (var k = 0; k < 7; k++) mk.Box(deep, g - up * (.6f + k * .05f) + side * (k - 3f) * .55f + face * Mathf.Sin(k) * .8f, new Vector3(.5f, .12f, .25f), Quaternion.Euler(0, k * 25f, 0));
            for (var k = 0; k < 6; k++) { var a = k * Mathf.PI / 3f; mk.Box(deepG, g - up * .9f + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 1.8f, new Vector3(.08f, .05f, 1f), Quaternion.Euler(0, -a * Mathf.Rad2Deg, 0)); }
            mk.Blob(deepG, g - up * .7f + face * 1.6f, new Vector3(.4f, .3f, .4f), ctx.Rng, .1f);
            for (var k = 0; k < 5; k++) mk.Blob(Cue(id, new Color(.85f, .6f, 1f), .2f, 3f, .2f, .75f, 2f, k / 5f, .5f), g - up * .4f + new Vector3(ctx.Range(-3f, 3f), 0, ctx.Range(-3f, 3f)), new Vector3(.25f, .2f, .25f), ctx.Rng, .1f);
            // two fish circling near the surface
            var fish = Part(root, "Pisces fish", g + up * .05f, Quaternion.identity);
            foreach (var s in new[] { 0f, 180f }) { var d = Quaternion.Euler(0, s, 0); fish.mk.Blob(WorldMaterials.Glow(s == 0f ? Moon : Ice, 1.6f), d * new Vector3(2f, 0, 0), new Vector3(.18f, .08f, .4f), ctx.Rng, 0f); fish.mk.Box(WorldMaterials.Glow(s == 0f ? Moon : Ice, 1.6f), d * new Vector3(2f, 0, -.45f), new Vector3(.02f, .12f, .2f), Quaternion.identity); }
            Done(fish); Moves(fish.t, id, SmallHoursLife.Move.Spin, Vector3.up, default, 20f, 0f, 1f, 1f);
            var shaft = Part(root, "Water shaft", g + up * 3f, Quaternion.Euler(8f, 0, 0)); shaft.mk.Box(WorldMaterials.Glow(new Color(.85f, .95f, 1f), 1.6f), Vector3.zero, new Vector3(.6f, 6f, .6f), Quaternion.identity); Done(shaft);
            Moves(shaft.t, id, SmallHoursLife.Move.Show, default, default, .8f, 0f, .22f);
            var bub = Part(root, "Water bubbles", g, Quaternion.identity);
            for (var k = 0; k < 24; k++) bub.mk.Blob(WorldMaterials.Glow(new Color(.8f, .95f, 1f), 2f), new Vector3(ctx.Range(-3f, 3f), ctx.Range(-.5f, .5f), ctx.Range(-3f, 3f)), Vector3.one * .06f, ctx.Rng, 0f);
            Done(bub); life.movers.Add(new SmallHoursLife.Mover { t = bub.t, song = id, kind = SmallHoursLife.Move.Slide, a = g - up * .8f, b = g + up * 3f, from = .45f, to = .8f });
            bub.t.position = g - up * .8f;
            mk.Prism(Cue(id, Color.white, 0f, 4f, .8f, .86f), g + up * .14f, up, 4.3f, .02f, 28);
            if (ordinaryMotif != null) foreach (var m in ordinaryMotif) { if (m == null) continue; var cue = life.cues.Find(q => q.m == m); if (cue != null) life.cues.Add(new SmallHoursLife.Cue { song = id, m = m, c = cue.c, idle = .05f, gain = 3f, from = .82f, to = .95f, period = 3f, phase = cue.phase, duty = .15f }); }
            var ripple = Cue(id, new Color(.6f, .85f, 1f), 0f, 2.5f, .92f, 1f, 1.2f, 0f, .5f);
            foreach (var r in new[] { 1.5f, 2.8f, 4f }) for (var k = 0; k < 24; k++) { float a0 = k * Mathf.PI / 12f, a1 = (k + 1) * Mathf.PI / 12f; var p0 = g + up * .13f + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * r; var p1 = g + up * .13f + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * r; mk.Box(ripple, (p0 + p1) / 2f, new Vector3(.04f, .01f, Vector3.Distance(p0, p1)), Quaternion.LookRotation(p1 - p0)); }
            ArtistBits.PointLight(root, g + up * 2f + face * 2f, new Color(.5f, .8f, 1f), 11f, 1.4f);
        }
    }
}
