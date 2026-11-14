using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The hub's later additions (part of HubKit):
    /// - the departures board: a six-sided board hung round the mast, a row for every land with the song its station
    ///   is playing right now, the cover, the land's colour and a bar for how far through the song it is;
    /// - the flags: a ring of flags round the plaza, one for every land in its colour, each standing in the direction
    ///   of its land, with the name and distance on a stone at its foot; the land the jukebox's song comes from lights
    ///   its flag and its row;
    /// - light: lamps down every causeway, garlands of bulbs between the plaza lanterns, statues lit from below;
    /// - people: friends sitting in the stands, at the jukebox and the map, reading the board, walking the plaza and
    ///   playing in the park; seats along the stands, a rocket launcher on the dais, balls and frisbees in the park,
    ///   and a kiosk where you can ask the board to pick a land for you.
    public partial class HubKit
    {
        HubLife life; Transform people; System.Random prng; Jukebox jukebox; HubBoard departures;

        static readonly Color[] Tops = { new Color(.92f, .9f, .86f), new Color(.3f, .5f, .8f), new Color(.85f, .35f, .3f), new Color(.35f, .6f, .4f), new Color(1f, .75f, .3f), new Color(.6f, .4f, .75f), new Color(.2f, .2f, .24f) };
        static readonly Color[] Bottoms = { new Color(.22f, .26f, .36f), new Color(.5f, .44f, .36f), new Color(.16f, .16f, .18f), new Color(.7f, .66f, .58f) };
        static readonly string[] HubTalk =
        {
            "Look up at the board: every land, playing right now.", "Every flag round here points at its land. Pick one and walk.",
            "I come back here between every trip.", "The ring's got every song we ever added. Mine's up there somewhere.",
            "When the jukebox plays a song from a land, that land's flag lights up.", "Meet you back here after?",
        };

        Residents.Look Look() => Residents.Random(prng, Tops, Bottoms, new[] { Residents.Hat.None, Residents.Hat.Cap, Residents.Hat.None, Residents.Hat.Bucket, Residents.Hat.Straw }, new[] { Warm, new Color(.3f, .5f, .8f), new Color(.85f, .35f, .3f) });
        Resident Folk(Vector3 feet, Vector3 face, Resident.Pose pose, string[] lines = null, Vector3[] route = null, float seatH = HumanScale.SeatHeight)
            => Residents.Make(people, feet, face, Look(), pose, prng, lines ?? HubTalk, route, seatH);

        Toy Spot(string name, Vector3 at, string verb, Vector3 size)
        {
            var go = new GameObject(name); go.transform.SetParent(people, false); go.transform.position = at; return Interact.Toy(go, Toy.Kind.Custom, verb, size);
        }

        Carryable Thing(MeshKit m, string name, string label, Vector3 at, Vector3 size, float mass)
        {
            var go = m.BuildSingle(name, people, false); go.transform.position = at; var c = Interact.Carry(go, label, mass, at.y - 30f);
            var bc = c.GetComponent<BoxCollider>(); if (bc != null) { bc.size = size; bc.center = new Vector3(0, size.y / 2f, 0); }
            var rb = go.GetComponent<Rigidbody>(); if (rb != null) rb.isKinematic = true;
            return c;
        }

        static void Shot(LandContext ctx, string name, Vector3 from, Vector3 at)
        {
            var mark = new GameObject("Shot: " + name).transform; mark.SetParent(ctx.Root, false); mark.SetPositionAndRotation(from, Quaternion.LookRotation((at - from).normalized));
        }

        static SceneCatalog Catalog()
        {
            var d = Object.FindFirstObjectByType<MusicDirector>();
            return d != null && d.sceneData != null ? SceneCatalog.FromJson(d.sceneData) : null;
        }

        static Color LandColor(SceneCatalog cat, IslandWorldData.Island isl)
        {
            var t = isl.tintA; var fb = t != null && t.Length >= 3 ? Color.Lerp(new Color(t[0] / 255f, t[1] / 255f, t[2] / 255f), Color.white, .3f) : Color.grey;
            return ZoneFields.VividColor(cat?.Get(isl.sceneId)?.palette, fb);
        }

        /// Every land but the hub, realm by realm.
        static List<IslandWorldData.Island> Lands(IslandWorldData data)
        {
            var list = new List<IslandWorldData.Island>(); foreach (var isl in data.layout.islands) if (isl.sceneId != 1000) list.Add(isl);
            var order = new Dictionary<IslandWorldData.Island, int>(); for (var i = 0; i < list.Count; i++) order[list[i]] = i;
            list.Sort((a, b) => { var c = string.CompareOrdinal(a.realm ?? "", b.realm ?? ""); return c != 0 ? c : order[a].CompareTo(order[b]); });
            return list;
        }

        static string Short(string name) => name.StartsWith("The ") ? name.Substring(4) : name;

        /// A flat picture quad whose UVs a script changes (a cover).
        static MeshFilter CoverQuad(Transform parent, Vector3 c, Vector3 right, Vector3 up, Vector3 face, float half, Material m)
        {
            var go = new GameObject("Cover"); go.transform.SetParent(parent, false);
            var mesh = new Mesh { name = "Board cover" };
            mesh.SetVertices(new List<Vector3> { c - right * half - up * half, c + right * half - up * half, c + right * half + up * half, c - right * half + up * half });
            mesh.SetUVs(0, new List<Vector2> { Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero });
            mesh.SetTriangles(Vector3.Dot(Vector3.Cross(right, up), face) > 0 ? new[] { 0, 1, 2, 0, 2, 3 } : new[] { 0, 2, 1, 0, 3, 2 }, 0);
            mesh.RecalculateNormals();
            var f = go.AddComponent<MeshFilter>(); f.sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterial = m;
            return f;
        }

        // ---------------------------------------------------------------- the departures board
        void Board(LandContext ctx, MeshKit mk, MeshKit solid, Transform labels, GlowHalos halos)
        {
            var cat = Catalog(); var lands = Lands(ctx.Data); var up = Vector3.up;
            const int faces = 6; const float apo = 7.2f, bottom = 6.5f, rowH = 1.05f;
            var perFace = Mathf.CeilToInt(lands.Count / (float)faces); var faceH = perFace * rowH + 1.7f; var w = 2f * apo * Mathf.Tan(Mathf.PI / faces);
            var root = new GameObject("Departures board").transform; root.SetParent(ctx.Root, false);
            departures = root.gameObject.AddComponent<HubBoard>(); departures.coverRect = ctx.CoverRect; departures.jukebox = jukebox; departures.barLength = w - 1.9f;
            foreach (var s in ctx.Data.layout.hubTimeline ?? new IslandWorldData.TimelineSong[0]) if (s.c >= 0) departures.coverOf[s.id] = s.c;
            var black = WorldMaterials.Solid(new Color(.04f, .04f, .06f)); var track = WorldMaterials.Solid(new Color(.16f, .16f, .2f)); var pic = WorldMaterials.Picture(ctx.CoverAtlas);
            var barM = WorldMaterials.Glow(Warm, 1.6f);
            for (var f = 0; f < faces; f++)
            {
                var n = Quaternion.Euler(0, f * 60f + 30f, 0) * Vector3.forward; var right = Vector3.Cross(n, up).normalized; var rot = Quaternion.LookRotation(n);
                var fc = center + n * apo + up * (bottom + faceH / 2f);
                mk.Box(black, fc - n * .16f, new Vector3(w + .02f, faceH, .3f), rot);
                foreach (var e in new[] { -1f, 1f }) mk.Box(bronze, fc + up * e * (faceH / 2f + .09f) - n * .12f, new Vector3(w + .12f, .18f, .42f), rot);
                mk.Box(WorldMaterials.Glow(Warm, 1.4f), fc + up * (faceH / 2f - .95f) + n * .01f, new Vector3(w - .5f, .04f, .02f), rot);
                ArtistBits.Label(labels, "NOW PLAYING IN EVERY LAND", fc + up * (faceH / 2f - .5f) + n * .01f, n, .42f, Warm, TextAnchor.MiddleCenter);
                for (var i = 0; i < perFace; i++)
                {
                    var idx = f * perFace + i; if (idx >= lands.Count) break;
                    var isl = lands[idx]; var col = LandColor(cat, isl);
                    var top = fc + up * (faceH / 2f - 1.15f - i * rowH) + n * .01f; var left = top - right * (w / 2f);
                    var tab = new Material(WorldMaterials.Glow(col, 1.2f)) { name = "Board tab" };
                    mk.Box(tab, left + right * .22f - up * rowH / 2f, new Vector3(.14f, rowH - .16f, .04f), rot);
                    var row = new HubBoard.Row { sceneId = isl.sceneId, land = isl.name, tab = tab, tabColor = tab.GetColor("_EmissionColor") };
                    row.cover = CoverQuad(root, left + right * .82f - up * rowH / 2f + n * .02f, right, up, n, .4f, pic);
                    var tx = left + right * 1.4f;
                    row.name = ArtistBits.Label(root, Short(isl.name), tx - up * .1f, n, .36f, new Color(1f, .8f, .42f), TextAnchor.UpperLeft);
                    row.song = ArtistBits.Plain(ArtistBits.Label(root, "", tx - up * .5f, n, .27f, Ink, TextAnchor.UpperLeft));
                    var by = tx - up * (rowH - .14f);
                    mk.Box(track, by + right * departures.barLength / 2f - n * .005f, new Vector3(departures.barLength, .05f, .02f), rot);
                    var bar = new GameObject("Progress").transform; bar.SetParent(root, false); bar.SetPositionAndRotation(by + n * .01f, Quaternion.LookRotation(-n));
                    var bk = new MeshKit(); bk.Box(barM, new Vector3(.5f, 0, 0), new Vector3(1f, .07f, .02f), Quaternion.identity); var bg = bk.BuildSingle("Bar", bar, false); bg.transform.localPosition = Vector3.zero; bg.transform.localRotation = Quaternion.identity;
                    bar.localScale = new Vector3(.001f, 1f, 1f); row.bar = bar;
                    departures.rows.Add(row);
                }
                ArtistBits.PointLight(ctx.Root, center + n * (apo + 4f) + up * (bottom - 1f), Warm, 18f, 1.1f);
                halos.Add(fc + n * .8f + up * (faceH / 2f - .5f), 3f, Warm, .25f);
            }
            // struts from the mast out to the board's corners, top and bottom
            var corner = apo / Mathf.Cos(Mathf.PI / faces);
            for (var k = 0; k < faces; k++)
            {
                var d = Quaternion.Euler(0, k * 60f, 0) * Vector3.forward;
                foreach (var y in new[] { bottom + .4f, bottom + faceH - .4f }) { var a = center + d * 4.3f + up * y; var b = center + d * (corner - .2f) + up * y; mk.Box(iron, (a + b) / 2f, new Vector3(.3f, .3f, (b - a).magnitude), Quaternion.LookRotation(b - a)); }
                mk.Box(iron, center + d * (corner - .25f) + up * (bottom + faceH / 2f), new Vector3(.3f, faceH + .3f, .3f), Quaternion.LookRotation(d));
            }
            var sd = Quaternion.Euler(0, 30f, 0) * Vector3.forward; Shot(ctx, "the departures board", center + sd * 30f + up * 3.4f, center + sd * apo + up * (bottom + faceH / 2f));
            var sd2 = Quaternion.Euler(0, 90f, 0) * Vector3.forward; Shot(ctx, "the board up close", center + sd2 * 16f + up * 3.2f, center + sd2 * apo + up * (bottom + faceH * .7f));
            Debug.Log($"The Everything Hub: departures board with {departures.rows.Count} lands on {faces} faces, {faceH:0.0} m tall");
        }

        // ---------------------------------------------------------------- a flag for every land
        void Flags(LandContext ctx, MeshKit mk, Transform labels, GlowHalos halos)
        {
            var cat = Catalog(); var lands = Lands(ctx.Data); var up = Vector3.up; const float R = 48.5f;
            var n = lands.Count; if (n == 0) return;
            var ang = new float[n]; for (var i = 0; i < n; i++) ang[i] = Mathf.Atan2(lands[i].labelZ - center.z, lands[i].labelX - center.x);
            var order = new List<int>(); for (var i = 0; i < n; i++) order.Add(i); order.Sort((x, y) => ang[x].CompareTo(ang[y]));
            var a = new float[n]; for (var j = 0; j < n; j++) a[j] = ang[order[j]];
            var open = new List<float>(); foreach (var o in openings) open.Add(Mathf.Atan2(o.z, o.x));
            float Wrap(float v) { while (v > Mathf.PI) v -= 2f * Mathf.PI; while (v <= -Mathf.PI) v += 2f * Mathf.PI; return v; }
            var gap = 4.1f * Mathf.Deg2Rad; var half = ClearAngle(R, 4.6f) * Mathf.Deg2Rad;
            for (var it = 0; it < 600; it++)
            {
                for (var j = 0; j < n; j++) { var k = (j + 1) % n; var d = Wrap(a[k] - a[j]); if (d < gap) { var push = (gap - d) / 2f; a[j] -= push; a[k] += push; } }
                for (var j = 0; j < n; j++) foreach (var o in open) { var d = Wrap(a[j] - o); if (Mathf.Abs(d) < half) a[j] = o + (d >= 0f ? half : -half); }
            }
            var flagsRoot = new GameObject("Flags").transform; flagsRoot.SetParent(ctx.Root, false);
            var plaque = WorldMaterials.Solid(new Color(.3f, .28f, .26f));
            for (var j = 0; j < n; j++)
            {
                var isl = lands[order[j]]; var dir = new Vector3(Mathf.Cos(a[j]), 0, Mathf.Sin(a[j])); var p = center + dir * R; p.y = ctx.Ground(p.x, p.z);
                var col = LandColor(cat, isl); var tangent = Vector3.Cross(up, dir);
                mk.Prism(iron, p, up, .08f, 8f, 6); ArtistBits.PostCollider(ctx.Root, p, 8f, .12f);
                mk.Blob(WorldMaterials.Glow(Warm, 2f), p + up * 8.1f, Vector3.one * .18f, ctx.Rng, 0f); halos.Add(p + up * 8.1f, .7f, Warm, .7f);
                var cloth = new Material(WorldMaterials.Glow(col, .9f)) { name = "Flag " + isl.name };
                var pivot = new GameObject("Flag: " + isl.name).transform; pivot.SetParent(flagsRoot, false); pivot.SetPositionAndRotation(p + up * 7.8f, Quaternion.LookRotation(j % 2 == 0 ? tangent : -tangent));
                var fk = new MeshKit(); fk.Box(cloth, new Vector3(0, -.75f, 1.32f), new Vector3(.04f, 1.5f, 2.5f), Quaternion.identity); var fg = fk.BuildSingle("Cloth", pivot, false); fg.transform.localPosition = Vector3.zero; fg.transform.localRotation = Quaternion.identity;
                fg.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                life.flags.Add((pivot, pivot.localRotation));
                // the stone at its foot, facing the plaza: the land's name and how far it is
                var rot = Quaternion.LookRotation(-dir); mk.Box(plaque, p + up * .35f - dir * .2f, new Vector3(1.1f, .7f, .16f), rot); PlaceTools.BoxCol(ctx.Root, p + up * .35f - dir * .2f, new Vector3(1.1f, .7f, .2f), rot);
                var km = new Vector2(isl.labelX - center.x, isl.labelZ - center.z).magnitude / 1000f;
                ArtistBits.Label(labels, MapName(isl.name) + $"\n{km:0.0} km", p + up * .62f - dir * .29f, -dir, .11f, Ink);
                foreach (var row in departures != null ? departures.rows : new List<HubBoard.Row>()) if (row.land == isl.name) { row.flag = cloth; row.flagColor = cloth.GetColor("_EmissionColor"); }
            }
            var fd = Quaternion.Euler(0, 200f, 0) * Vector3.forward; Shot(ctx, "the flags", center + fd * 43f + Vector3.Cross(up, fd) * 6f + up * 2f, center + fd * 48.5f - Vector3.Cross(up, fd) * 10f + up * 3f);
            Debug.Log($"The Everything Hub: {n} flags round the plaza, each toward its land");
        }

        // ---------------------------------------------------------------- more light
        void Lights(LandContext ctx, MeshKit mk, GlowHalos halos)
        {
            var up = Vector3.up; var bulbCols = new[] { Warm, new Color(1f, .55f, .5f), new Color(.6f, .8f, 1f), new Color(1f, .9f, .6f) };
            // lamps down every causeway path, kept clear of the bridge decks where the paths meet them
            var lampN = 0; var decks = new List<Vector3>();
            var ends = new List<Vector3>(); foreach (var br in ctx.Data.layout.bridges) if (br.a == 1000 || br.b == 1000) { var bp = IslandWorldBuilder.BridgePath(ctx.Data, br); decks.AddRange(bp); ends.Add(bp[0]); ends.Add(bp[bp.Count - 1]); }
            bool OnDeck(Vector3 p) { foreach (var d in ends) if ((new Vector2(d.x - p.x, d.z - p.z)).sqrMagnitude < 14f * 14f) return true; foreach (var d in decks) if ((new Vector2(d.x - p.x, d.z - p.z)).sqrMagnitude < 5.5f * 5.5f) return true; return false; }
            foreach (var o in openings)
            {
                var side = Vector3.Cross(up, o);
                for (var d = PlazaR + 10f; d < 400f; d += 14f)
                {
                    var q = center + o * d; if (!ctx.IsLand(q.x, q.z, 3f)) break;
                    var s = (lampN % 2 == 0 ? 1f : -1f); var p = q + side * s * 3.4f; p.y = ctx.Ground(p.x, p.z);
                    if (OnDeck(p)) { lampN++; continue; }
                    mk.Prism(iron, p, up, .09f, 3.6f, 6); mk.Blob(WorldMaterials.Glow(Warm, 2f), p + up * 3.8f, new Vector3(.28f, .36f, .28f), ctx.Rng, .05f); ArtistBits.PostCollider(ctx.Root, p, 3.6f, .12f);
                    halos.Add(p + up * 3.8f, 1.6f, Warm, .7f); if (lampN % 2 == 0) ArtistBits.PointLight(ctx.Root, p + up * 3.6f, Warm, 12f, 1.2f);
                    lampN++;
                }
            }
            // garlands of bulbs between neighbouring plaza lanterns
            var posts = new List<(Vector3 p, int k)>();
            for (var k = 0; k < 16; k++) { var a = k * Mathf.PI * 2f / 16f + .1f; var p = center + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * LampR; if (NearOpening(p - center, 8f)) continue; p.y = ctx.Ground(p.x, p.z); posts.Add((p, k)); }
            var bulbs = 0;
            for (var i = 0; i < posts.Count; i++)
            {
                var (p0, k0) = posts[i]; var (p1, k1) = posts[(i + 1) % posts.Count]; if ((k0 + 1) % 16 != k1) continue;
                Vector3 A = p0 + up * 4f, B = p1 + up * 4f;
                for (var b = 1; b < 14; b++) { var t = b / 14f; var bp = Vector3.Lerp(A, B, t) - up * Mathf.Sin(t * Mathf.PI) * 1.1f; var c = bulbCols[(b + i) % bulbCols.Length]; mk.Blob(WorldMaterials.Glow(c, 2.2f), bp, Vector3.one * .1f, ctx.Rng, 0f); if (b % 3 == 0) halos.Add(bp, .5f, c, .6f); bulbs++; }
                for (var b = 0; b < 14; b++) { var t0 = b / 14f; var t1 = (b + 1) / 14f; var a0 = Vector3.Lerp(A, B, t0) - up * Mathf.Sin(t0 * Mathf.PI) * 1.1f; var a1 = Vector3.Lerp(A, B, t1) - up * Mathf.Sin(t1 * Mathf.PI) * 1.1f; mk.Box(iron, (a0 + a1) / 2f + up * .1f, new Vector3(.02f, .02f, (a1 - a0).magnitude), Quaternion.LookRotation(a1 - a0)); }
            }
            // every statue lit from below
            foreach (var s in statueSpots) { var toMast = (center - s); toMast.y = 0; toMast.Normalize(); var lp = s + toMast * 2.2f + up * .3f; mk.Prism(iron, s + toMast * 2.2f, up, .18f, .25f, 8); halos.Add(lp + up * .2f, .8f, Warm, .8f); ArtistBits.PointLight(ctx.Root, lp + up * .6f, Warm, 7f, 1.3f); }
            Debug.Log($"The Everything Hub: {lampN} causeway lamps, {bulbs} garland bulbs, {statueSpots.Count} statue lights");
        }

        // ---------------------------------------------------------------- people and things to do
        void People(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var rng = new System.Random(2026);
            const float tierDepth = 1.6f, tierRise = .32f; const int segments = 72;
            Vector3 Seg(int s) { var am = (s + .5f) * Mathf.PI * 2f / segments; return new Vector3(Mathf.Cos(am), 0, Mathf.Sin(am)); }
            // seats along the stands (every fourth stretch of the second and fourth tiers)
            for (var s = 0; s < segments; s += 4)
            {
                var dm = Seg(s); if (NearOpening(dm, 12f)) continue;
                foreach (var k in new[] { 1, 3 })
                {
                    var r = StandsInner + k * tierDepth + .4f; var y = groundY + (k + 1) * tierRise + .05f;
                    float a0 = (s + .15f) * Mathf.PI * 2f / segments, a1 = (s + .85f) * Mathf.PI * 2f / segments;
                    var pa = center + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * r; pa.y = y; var pb = center + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * r; pb.y = y;
                    Interact.Bench(people, pa, pb, -dm);
                }
            }
            // friends sitting in the stands
            string[] standTalk = { "Best seat in the hub. You can see the whole board from here.", "We sit here and argue about which land to go to next.", "Watch the ring. Your song comes round eventually.", "Every time a new song starts, a rocket goes up. Every time." };
            for (var i = 0; i < 16; i++)
            {
                var s = rng.Next(segments); var dm = Seg(s); if (NearOpening(dm, 12f)) continue; var k = 1 + rng.Next(5);
                var feet = center + dm * (StandsInner + k * tierDepth + .35f); feet.y = groundY + (k + 1) * tierRise;
                Folk(feet, -dm, Resident.Pose.Sit, i % 3 == 0 ? standTalk : null, null, tierRise);
            }
            // at the jukebox, the map and the signpost
            var jd = Quaternion.Euler(0, 60f, 0) * Vector3.forward; var jp = center + jd * (FeatureR - 3.2f); jp.y = groundY;
            Folk(jp + Vector3.Cross(up, jd) * 1.2f, jd, Resident.Pose.Wave, new[] { "This one's from a land I haven't been to yet. Look where the arrow points.", "I love this song. Who added it? Look at the frame colour on the ring." });
            Folk(jp - Vector3.Cross(up, jd) * 1.4f, jd, Resident.Pose.Stand);
            var md = Quaternion.Euler(0, 300f, 0) * Vector3.forward; var mp = center + md * (FeatureR - 3.4f); mp.y = groundY;
            Folk(mp, md, Resident.Pose.Stand, new[] { "We're right in the middle. Everything's a causeway and a tower away.", "I'm trying to find the one with the waterfall." });
            // reading the board
            string[] boardTalk = { "Look how many lands there are now. Every row is somewhere.", "Watch a row flip when its song changes.", "The bright row is the land the jukebox song comes from. Its flag lights up too." };
            for (var i = 0; i < 3; i++) { var d = Quaternion.Euler(0, 100f + i * 120f, 0) * Vector3.forward; var fp = center + d * 25f; fp.y = groundY; Folk(fp, -d, i == 1 ? Resident.Pose.Wave : Resident.Pose.Stand, boardTalk); }
            // two walking laps of the plaza, beyond the stands
            for (var w = 0; w < 2; w++)
            {
                var route = new List<Vector3>(); for (var k = 0; k < 16; k++) { var a = (w == 0 ? k : 16 - k) * Mathf.PI * 2f / 16f + w; var rp = center + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 43.6f; rp.y = ctx.Ground(rp.x, rp.z); route.Add(rp); }
                Folk(route[0], (route[1] - route[0]).normalized, Resident.Pose.Walk, new[] { "One lap of the flags. I like to say hello to every land.", "Walking off the last trip before the next one." }, route.ToArray()).speed = 1f;
            }
            // the kiosk where the board picks a land for you
            var kd = Quaternion.Euler(0, 120f, 0) * Vector3.forward; var kp = center + kd * 25.5f; kp.y = groundY; var kr = Quaternion.LookRotation(-kd);
            var kw = WorldMaterials.Solid(new Color(.5f, .2f, .14f), .4f);
            mk.Box(kw, kp + up * .55f, new Vector3(2.4f, 1.1f, 1f), kr); solid.Box(kw, kp + up * .55f, new Vector3(2.4f, 1.1f, 1f), kr); mk.Box(bronze, kp + up * 1.14f, new Vector3(2.6f, .08f, 1.2f), kr);
            foreach (var e in new[] { -1.1f, 1.1f }) { var pp = kp + Vector3.Cross(up, kd) * e + kd * .3f; mk.Prism(iron, pp, up, .05f, 2.8f, 5); ArtistBits.PostCollider(ctx.Root, pp, 2.8f, .08f); }
            mk.Box(kw, kp + up * 2.85f + kd * .3f, new Vector3(2.8f, .12f, 1.6f), kr);
            mk.Box(WorldMaterials.Glow(Warm, 1.6f), kp + up * 2.55f - kd * .51f, new Vector3(2.2f, .42f, .04f), kr);
            ArtistBits.Label(people, "WHERE NEXT?", kp + up * 2.55f - kd * .54f, -kd, .26f, new Color(.2f, .1f, .05f), TextAnchor.MiddleCenter);
            ArtistBits.PointLight(ctx.Root, kp + up * 2.4f - kd * 1.4f, Warm, 6f, 1.1f);
            Folk(kp + kd * .9f, -kd, Resident.Pose.Work, new[] { "Can't decide? Ask the board. It always knows.", "Every land's playing something right now. Let's pick one." });
            Spot("Where next?", kp - kd * .9f + up * 1.1f, "Ask the board where to go", new Vector3(2.4f, 1.2f, 1f)).onUse = _ => PlaceTools.Say(people, kp + up * 3.4f, departures != null ? departures.Pick() : "Go anywhere!", 6f);
            Shot(ctx, "the kiosk", kp - kd * 7f + Vector3.Cross(up, kd) * 3f + up * 2f, kp + up * 1.5f);
            // the terminal: an old computer on a desk with a green screen you can type into (HubTerminal)
            {
                var td = Quaternion.Euler(0, 255f, 0) * Vector3.forward; var tp = center + td * 25f; tp.y = groundY; var f = new PlaceFrame(tp, td);
                var beige = WorldMaterials.Solid(new Color(.82f, .78f, .68f), .2f); var dkey = WorldMaterials.Solid(new Color(.3f, .29f, .27f));
                mk.Box(iron, f.P(0, .37f, 0), new Vector3(1.7f, .06f, .85f), f.r); foreach (var e in new[] { -.78f, .78f }) foreach (var z in new[] { -.35f, .35f }) mk.Box(iron, f.P(e, .18f, z), new Vector3(.06f, .36f, .06f), f.r);
                mk.Box(WorldMaterials.Solid(new Color(.35f, .26f, .18f)), f.P(0, .74f, 0), new Vector3(1.7f, .06f, .85f), f.r); solid.Box(iron, f.P(0, .4f, 0), new Vector3(1.7f, .8f, .85f), f.r);
                mk.Box(iron, f.P(0, .38f, 0), new Vector3(1.6f, .7f, .8f), f.r);
                // the monitor: a deep beige box with a bezel and a glowing green screen
                mk.Box(beige, f.P(0, 1.2f, -.12f), new Vector3(1f, .82f, .7f), f.r); mk.Box(beige, f.P(0, .82f, -.05f), new Vector3(.5f, .1f, .4f), f.r); solid.Box(beige, f.P(0, 1.2f, -.12f), new Vector3(1f, .82f, .7f), f.r);
                var scr = new Material(WorldMaterials.Glow(new Color(.03f, .12f, .05f), .5f)) { name = "Terminal screen" };
                mk.Box(scr, f.P(0, 1.22f, .235f), new Vector3(.84f, .64f, .02f), f.r); mk.Box(dkey, f.P(.4f, .86f, .235f), new Vector3(.05f, .05f, .02f), f.r); mk.Blob(WorldMaterials.Glow(new Color(.3f, 1f, .4f), 2f), f.P(.33f, .86f, .25f), Vector3.one * .015f, rng, 0f);
                // the keyboard: a beige slab with rows of dark keys
                mk.Box(beige, f.P(0, .79f, .3f), new Vector3(.9f, .04f, .3f), f.r * Quaternion.Euler(6f, 0, 0));
                for (var row = 0; row < 4; row++) for (var k = 0; k < 13; k++) mk.Box(dkey, f.P(-.39f + k * .065f, .815f + row * .006f, .4f - row * .06f), new Vector3(.05f, .02f, .045f), f.r * Quaternion.Euler(6f, 0, 0));
                mk.Box(dkey, f.P(0, .81f, .44f), new Vector3(.4f, .02f, .045f), f.r);
                // the stool, the sign and a lamp
                var stool = f.P(0, 0, .95f); mk.Prism(iron, stool, Vector3.up, .05f, .55f, 6); mk.Prism(WorldMaterials.Solid(new Color(.5f, .12f, .1f), .3f), stool + Vector3.up * .55f, Vector3.up, .22f, .08f, 12); ArtistBits.PostCollider(ctx.Root, stool, .6f, .22f);
                var post = f.P(-1.05f, 0, -.2f); mk.Prism(iron, post, Vector3.up, .05f, 2.4f, 6); ArtistBits.PostCollider(ctx.Root, post, 2.4f, .08f);
                mk.Box(WorldMaterials.Solid(new Color(.06f, .06f, .07f)), post + Vector3.up * 2.25f + f.Fwd * .06f + f.Right * .45f, new Vector3(1.05f, .28f, .04f), f.r);
                ArtistBits.Label(people, "TERMINAL", post + Vector3.up * 2.25f + f.Fwd * .09f + f.Right * .45f, f.Fwd, .17f, new Color(.4f, 1f, .5f), TextAnchor.MiddleCenter);
                ArtistBits.PointLight(ctx.Root, f.P(0, 1.6f, .9f), new Color(.5f, 1f, .6f), 4f, .7f);
                var tgo = new GameObject("Terminal"); tgo.transform.SetParent(people, false); tgo.transform.position = f.P(0, 1.2f, .3f);
                var term = tgo.AddComponent<HubTerminal>(); term.glow = scr; term.glowColor = scr.GetColor("_EmissionColor");
                term.screen = ArtistBits.Plain(ArtistBits.Label(tgo.transform, "", f.P(.39f, 1.52f, .25f), f.Fwd, .046f, new Color(.45f, 1f, .55f), TextAnchor.UpperLeft));
                Interact.Toy(tgo, Toy.Kind.Custom, "Use the terminal", new Vector3(1.1f, 1.1f, .5f)).onUse = _ => term.Begin(Object.FindAnyObjectByType<EverythingWorld.Player.PlayerInteractor>());
                Shot(ctx, "the terminal", f.P(.25f, .95f, 1.75f), f.P(0, 1.25f, .2f));
            }
            // the colour station: a ring of pedestals, each with a floating orb of one colour; E takes that colour
            {
                var cd = Quaternion.Euler(0, 28f, 0) * Vector3.forward; var cc = center + cd * 26.5f; cc.y = groundY;
                var palette = EverythingWorld.Player.PlayerOrb.Palette; var stoneP = WorldMaterials.Solid(new Color(.6f, .57f, .52f));
                mk.Prism(stoneDark, cc - Vector3.up * .2f, Vector3.up, 3.6f, .3f, 28);
                for (var i = 0; i < palette.Length; i++)
                {
                    var a = Mathf.Lerp(-80f, 80f, i / (float)(palette.Length - 1)); var d = Quaternion.Euler(0, a, 0) * cd; var pp = cc + d * 2.8f;
                    mk.Prism(stoneP, pp, Vector3.up, .2f, 1.05f, 10); mk.Prism(bronze, pp + Vector3.up * 1.05f, Vector3.up, .26f, .06f, 12); ArtistBits.PostCollider(ctx.Root, pp, 1.1f, .22f);
                    var col = palette[i].color; var ob = pp + Vector3.up * 1.45f;
                    mk.Blob(WorldMaterials.Glow(col, 1.1f), ob, Vector3.one * .17f, rng, 0f);
                    var go = new GameObject("Colour: " + palette[i].name); go.transform.SetParent(people, false); go.transform.position = ob;
                    var idx = i; var nm = palette[i].name;
                    Interact.Toy(go, Toy.Kind.Custom, "Glow " + nm, new Vector3(.5f, .9f, .5f)).onUse = _ => { if (EverythingWorld.Player.PlayerOrb.Local != null) EverythingWorld.Player.PlayerOrb.Local.Choose(idx); PlaceTools.Say(people, ob + Vector3.up * .5f, "You glow " + nm + " now.", 2.5f); };
                }
                var sp = cc - cd * .6f; mk.Prism(iron, sp, Vector3.up, .05f, 2.6f, 6); ArtistBits.PostCollider(ctx.Root, sp, 2.6f, .08f);
                mk.Box(WorldMaterials.Solid(new Color(.06f, .06f, .07f)), sp + Vector3.up * 2.45f + cd * .05f, new Vector3(2.4f, .34f, .04f), Quaternion.LookRotation(cd));
                ArtistBits.Label(people, "CHOOSE YOUR LIGHT", sp + Vector3.up * 2.45f + cd * .08f, cd, .18f, new Color(1f, .9f, .7f), TextAnchor.MiddleCenter);
                ArtistBits.PointLight(ctx.Root, cc + Vector3.up * 2.5f + cd * 1.5f, Warm, 7f, .8f);
                Shot(ctx, "the colour station", cc + cd * 6.5f + Vector3.up * .95f, cc + Vector3.up * 1.2f);
            }
            // the rocket launcher on the dais
            var rd = Quaternion.Euler(0, 180f, 0) * Vector3.forward; var rp0 = center + rd * (DaisR - 2.2f) + up * 1.5f;
            mk.Box(bronze, rp0 + up * .3f, new Vector3(1f, .6f, 1f), Quaternion.LookRotation(rd)); mk.Prism(iron, rp0 + up * .6f, (up + rd * .2f).normalized, .16f, 1.4f, 10); mk.Prism(WorldMaterials.Glow(new Color(1f, .4f, .3f), 2f), rp0 + up * .6f + (up + rd * .2f).normalized * 1.4f, (up + rd * .2f).normalized, .17f, .05f, 10); PlaceTools.BoxCol(ctx.Root, rp0 + up * .8f, new Vector3(1f, 1.6f, 1f), Quaternion.LookRotation(rd));
            string[] whoosh = { "Whoosh! Everyone looks up.", "Bang! Somebody in the stands cheers.", "Up it goes, over the ring." }; var wn = 0;
            Spot("Rocket launcher", rp0 + up * 1f, "Launch a rocket", new Vector3(1.2f, 1.6f, 1.2f)).onUse = _ => { if (life != null) life.LaunchRocket(rp0 + up * 2f); PlaceTools.Say(people, rp0 + up * 2.4f, whoosh[wn++ % whoosh.Length], 3f); };
            // the park: people playing, and balls and frisbees to throw
            var made = 0;
            for (var i = 0; i < 24 && made < 6; i++)
            {
                var a = (float)rng.NextDouble() * Mathf.PI * 2f; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)); if (NearOpening(d, 14f)) continue;
                var p = center + d * 63f; if (!ctx.IsLand(p.x, p.z, 4f)) continue; p.y = ctx.Ground(p.x, p.z); var t = Vector3.Cross(up, d);
                var m = new MeshKit();
                if (made % 2 == 0) { m.Blob(WorldMaterials.Glow(made % 4 == 0 ? new Color(1f, .4f, .3f) : new Color(.3f, .6f, 1f), .6f), up * .16f, Vector3.one * .16f, rng, 0f); Thing(m, "Ball", "ball", p + t * 1.5f, new Vector3(.32f, .32f, .32f), .4f); }
                else { m.Prism(WorldMaterials.Glow(new Color(1f, .85f, .3f), .7f), Vector3.zero, up, .14f, .03f, 14); Thing(m, "Frisbee", "frisbee", p + t * 1.5f, new Vector3(.3f, .06f, .3f), .15f); }
                Folk(p - t * 2.5f, t, made % 2 == 0 ? Resident.Pose.Wave : Resident.Pose.Stand, new[] { "Throw it here! No, here!", "Pick it up and throw it. Go on." });
                made++;
            }
        }
    }
}
