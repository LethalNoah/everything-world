using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// Noah Gallagher's exhibit (part of SmallHoursKit): a piece for every song he has made, through his forest.
    /// - Akashic Sanctuary: each song's piece stands in a clearing off the forest path, across from its light.
    /// - The Twelfth House: a ring path round the wheel's clearing, one station per song and sign, Aries to Pisces,
    ///   the first and the last meeting at the way in (SmallHoursExhibitRing).
    /// - The Synchronous World: a long trail of twelve stations branching off the ring (SmallHoursExhibitTrail).
    /// Every piece follows its own song as it plays (SmallHoursExhibitLife). The repo describes the pieces as
    /// images only.
    public partial class SmallHoursKit
    {
        Vector3 exN, exAway, exSide, exClear;
        readonly List<(Vector3 p, float r)> exhibitSpots = new List<(Vector3, float)>();
        readonly Dictionary<string, (Vector3 at, Vector3 face)> exhibitAt = new Dictionary<string, (Vector3, Vector3)>();
        readonly List<Vector3> exhibitCentres = new List<Vector3>();      // every piece, for the ley lines
        readonly Dictionary<string, float> exhibitRadius = new Dictionary<string, float>();

        bool InExhibit(Vector3 p, float pad)
        {
            foreach (var (q, r) in exhibitSpots) if ((new Vector2(p.x, p.z) - new Vector2(q.x, q.z)).sqrMagnitude < (r + pad) * (r + pad)) return true;
            return false;
        }

        Material Cue(string song, Color c, float idle = .5f, float gain = 2.5f, float from = 0f, float to = 1f, float period = 0f, float phase = 0f, float duty = 1f)
        {
            var m = new Material(WorldMaterials.Glow(c, 1f)) { name = "Exhibit glow" };
            m.SetColor("_EmissionColor", c * idle);
            life.cues.Add(new SmallHoursLife.Cue { song = song, m = m, c = c, idle = idle, gain = gain, from = from, to = to, period = period, phase = phase, duty = duty });
            return m;
        }

        /// A moving part: its own object at 'at'; build its geometry round zero in the returned kit, then Done().
        (Transform t, MeshKit mk) Part(Transform root, string name, Vector3 at, Quaternion rot)
        {
            var go = new GameObject(name).transform; go.SetParent(root, false); go.SetPositionAndRotation(at, rot);
            return (go, new MeshKit());
        }
        void Done((Transform t, MeshKit mk) part) => part.mk.Build("Part", part.t, false);
        void Moves(Transform t, string song, SmallHoursLife.Move kind, Vector3 a = default, Vector3 b = default, float speed = 1f, float from = 0f, float to = 1f, float extra = 0f)
        {
            life.movers.Add(new SmallHoursLife.Mover { t = t, song = song, kind = kind, a = a, b = b, speed = speed, from = from, to = to, extra = extra, rest = t.localPosition, restRot = t.localRotation, restScale = t.localScale });
            if (kind == SmallHoursLife.Move.Show) t.localScale = Vector3.one * .0001f;
        }

        void ExMark(LandContext ctx, Transform root, string name, Vector3 target, Vector3 from)
        {
            var m = new GameObject("Shot: " + name).transform; m.SetParent(root, false);
            // stand somewhere clear: not on another piece (a neighbour's floor filled the picture)
            var t2 = new Vector3(target.x, 0, target.z); var d0 = new Vector3(from.x, 0, from.z) - t2; var dist = Mathf.Max(8f, d0.magnitude);
            var dir0 = d0.sqrMagnitude > .01f ? d0.normalized : Vector3.forward;
            foreach (var turn in new[] { 0f, 25f, -25f, 50f, -50f, 80f, -80f, 120f, -120f, 180f })
            {
                var q = t2 + Quaternion.Euler(0, turn, 0) * dir0 * dist;
                var mine = false; foreach (var (p, r) in exhibitSpots) if ((new Vector2(p.x, p.z) - new Vector2(target.x, target.z)).sqrMagnitude < 4f) { mine = (new Vector2(q.x - p.x, q.z - p.z)).magnitude < r; }
                var blocked = false; foreach (var (p, r) in exhibitSpots) if ((new Vector2(p.x, p.z) - new Vector2(target.x, target.z)).sqrMagnitude >= 4f && (new Vector2(q.x - p.x, q.z - p.z)).magnitude < r + 1f) { blocked = true; break; }
                if (!blocked && !mine && ctx.IsLand(q.x, q.z, 2f)) { from = q; break; }
            }
            var cam = ctx.OnGround(from.x, from.z) + Vector3.up * 1.5f;
            m.SetPositionAndRotation(cam, Quaternion.LookRotation((target - cam).normalized));
        }

        Vector3 G(LandContext ctx, Vector3 p) => ctx.OnGround(p.x, p.z);

        /// Where everything goes, worked out before the forest grows so the trees leave room.
        void ExhibitPlan(LandContext ctx, Vector3 N, Vector3 F, Vector3 side, Vector3 clearing, List<Vector3> path)
        {
            exhibitSpots.Clear(); exhibitAt.Clear(); exhibitCentres.Clear();
            exN = N; exAway = -F; exSide = side; exClear = clearing;
            RingPlan(ctx, N, F, side, clearing);
            TrailPlan(ctx, N, F, side, clearing);
            // Akashic Sanctuary: each piece in the clearest spot nearest its song's light on the forest path (the
            // biggest first), clear of the house and yard, the ring, the trail and each other; a side path leads to it
            float[] r = { 10f, 7f, 5f, 7f, 9f, 9f, 6f, 6f };
            var order = new List<int> { 0, 4, 5, 1, 3, 6, 7, 2 };
            var yard = N + F * 10f;
            foreach (var i in order)
            {
                var k = Mathf.Clamp(Mathf.RoundToInt((i + .6f) / (Akashic.Length + .4f) * (path.Count - 1)), 1, path.Count - 2);
                var target = path[k];
                var best = Vector3.positiveInfinity; var bestD = float.MaxValue;
                for (var gx = -100f; gx <= 100f; gx += 3f)
                for (var gz = -100f; gz <= 100f; gz += 3f)
                {
                    var p = new Vector3(N.x + gx, 0, N.z + gz); var rr = r[i];
                    var dN = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(N.x, N.z));
                    if (dN > noahR - 12f - rr) continue;
                    if (Vector2.Distance(new Vector2(p.x, p.z), new Vector2(yard.x, yard.z)) < 26f + rr) continue;
                    if (Vector2.Distance(new Vector2(p.x, p.z), new Vector2(clearing.x, clearing.z)) < RingR + 17f + rr) continue;
                    if (InExhibit(p, rr + 3f) || NearPath(p.x, p.z, rr + 2.5f) || !ctx.IsLand(p.x, p.z, rr)) continue;
                    var d = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(target.x, target.z));
                    if (d < bestD) { bestD = d; best = p; }
                }
                if (float.IsInfinity(best.x)) { Debug.Log("The Small Hours: no room in the exhibit for " + Akashic[i].title); continue; }
                var at = G(ctx, best);
                // the nearest point on any path so far, and a side path from it
                Vector3 join = target; var jd = float.MaxValue;
                foreach (var cell in pathCells.Values) foreach (var q in cell) { if (InExhibit(q, .5f)) continue; var d = Vector2.Distance(new Vector2(q.x, q.z), new Vector2(at.x, at.z)); if (d < jd) { jd = d; join = q; } }   // never from inside another piece
                var face = new Vector3(join.x - at.x, 0, join.z - at.z); face = face.sqrMagnitude > .01f ? face.normalized : -F;
                exhibitAt[Akashic[i].title] = (at, face); exhibitRadius[Akashic[i].title] = r[i];
                exhibitSpots.Add((at, r[i]));
                exhibitCentres.Add(at);
                if (jd > r[i] + 1f)
                {
                    var pts = new List<Vector3>(); var to = at + face * (r[i] - 1f);
                    for (var f = 0f; f <= 1.0001f; f += .05f) pts.Add(G(ctx, Vector3.Lerp(join, to, f)));
                    foreach (var q in pts) Remember(q);
                    exPaths.Add(pts);
                }
            }
        }
        readonly List<List<Vector3>> exPaths = new List<List<Vector3>>();

        void ExhibitBuild(LandContext ctx, Transform root, MeshKit mk, MeshKit solid, Transform labels)
        {
            var trodden = WorldMaterials.Solid(new Color(.16f, .22f, .24f));
            foreach (var p in exPaths) Ribbon(ctx, mk, trodden, p, 1.4f, .06f);
            void A(string title, System.Action<Vector3, Vector3, string> build)
            {
                if (!exhibitAt.TryGetValue(title, out var v)) return;
                var id = System.Array.Find(Akashic, s => s.title == title).id;
                build(v.at, v.face, id);
                var back = (exhibitRadius.TryGetValue(title, out var rr) ? rr : 7f) * 1.3f + 3f;   // closer for the small pieces
                ExMark(ctx, root, "exhibit, " + title, v.at + Vector3.up * 1.8f, v.at + v.face * back + Vector3.Cross(Vector3.up, v.face) * (back * .35f));
            }
            A("Desolate Zenith", (c, f, id) => DesolateZenith(ctx, root, mk, solid, c, f, id));
            A("Periapsis", (c, f, id) => Periapsis(ctx, root, mk, solid, c, f, id));
            A("Doctrine Factorum", (c, f, id) => Lectern(ctx, mk, solid, c, f, id));
            A("Bohr's Reverie", (c, f, id) => BohrsReverie(ctx, root, mk, solid, c, f, id));
            A("Ashes of Corinth", (c, f, id) => AshesOfCorinth(ctx, root, mk, solid, c, f, id));
            A("Formula 0", (c, f, id) => Formula0(ctx, root, mk, solid, c, f, id));
            A("Prometheus Resolute", (c, f, id) => Prometheus(ctx, root, mk, solid, c, f, id));
            A("Blind Retrospect", (c, f, id) => BlindRetrospect(ctx, root, mk, solid, c, f, id));
            RingBuild(ctx, root, mk, solid, labels);
            TrailBuild(ctx, root, mk, solid, labels);
            Debug.Log($"The Small Hours: the exhibit has {exhibitCentres.Count} pieces, {life.cues.Count} cues and {life.movers.Count} moving parts");
            var where = new System.Text.StringBuilder("The Small Hours: exhibit pieces at");
            foreach (var kv in exhibitAt) where.Append($" | {kv.Key} {kv.Value.at.x:0},{kv.Value.at.z:0}");
            for (var i = 0; i < 12; i++) where.Append($" | {TwelfthHouse[i].title} {ringPiece[i].x:0},{ringPiece[i].z:0}");
            for (var j = 0; j < 12; j++) where.Append($" | station {j + 1} {trailStation[j].x:0},{trailStation[j].z:0}");
            Debug.Log(where.ToString());
        }

        static readonly Color Ice = new Color(.75f, .88f, 1f), Ember2 = new Color(1f, .45f, .15f), BloodRed = new Color(1f, .12f, .08f);

        // ---------------------------------------------------------------- Akashic Sanctuary
        /// A lone dark peak rising from a sea of velvet mist; a stair climbs it past things left behind, each dimmer;
        /// a thread of light leads up it; at the summit a black sun set in a wall of ice, weeping; strings of light
        /// from the top; a red crack through the rock.
        void DesolateZenith(LandContext ctx, Transform root, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var rock = WorldMaterials.Solid(new Color(.16f, .15f, .19f)); var rockB = WorldMaterials.Solid(new Color(.22f, .2f, .25f));
            const float H = 11f, R = 6.5f;
            mk.Cone(rock, c - up * .3f, R, H + .3f, 7, 10f); solid.Cone(rock, c - up * .3f, R, H + .3f, 7, 10f);
            for (var k = 0; k < 9; k++) { var a = k * 40f * Mathf.Deg2Rad; var y = 1f + (k % 4) * 2f; var rr = R * (1f - y / H) * .9f; mk.Blob(k % 2 == 0 ? rock : rockB, c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * rr + up * y, new Vector3(1.6f, 1.2f, 1.4f), ctx.Rng, .3f); }
            // the velvet sea of mist round its foot
            // (soft glowing haze: as solid blobs it read as flat lavender slabs)
            for (var k = 0; k < 16; k++) { var a = k * Mathf.PI * 2f / 16f; halos.Add(G(ctx, c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (R + 1.5f)) + up * .5f, 2.6f, new Color(.62f, .5f, .9f), .28f); }
            // the stair, spiralling up the outside of the peak
            const int steps = 36; var stepM = WorldMaterials.Solid(new Color(.3f, .28f, .33f));
            var thread = new List<Vector3>();
            Vector3 Step(int s, out float y) { y = (s + 1) * (H - .4f) / steps; var a = Mathf.Atan2(face.z, face.x) + s * .32f; var rr = R * (1f - y / H) + .75f; return new Vector3(c.x + Mathf.Cos(a) * rr, c.y + y, c.z + Mathf.Sin(a) * rr); }
            for (var s = 0; s < steps; s++)
            {
                var p = Step(s, out var y); var tang = Step(s + 1, out _) - p; tang.y = 0;
                var rot = Quaternion.LookRotation(tang.sqrMagnitude > .01f ? tang : face);
                var below = p.y - c.y + .3f;
                mk.Box(stepM, p - up * (below / 2f), new Vector3(1.3f, below, 1f), rot); solid.Box(stepM, p - up * (below / 2f), new Vector3(1.3f, below, 1f), rot);
                thread.Add(p + up * .9f + (p - new Vector3(c.x, p.y, c.z)).normalized * .5f);
            }
            // the unsung melody: a thread of light up the stair, a pulse climbing it while the song plays
            for (var s = 0; s + 1 < thread.Count; s++)
                mk.Box(Cue(id, new Color(.85f, .8f, 1f), .35f, 3f, 0f, 1f, 2.6f, -s / (float)thread.Count, .18f), (thread[s] + thread[s + 1]) / 2f, new Vector3(.04f, .04f, Vector3.Distance(thread[s], thread[s + 1]) + .02f), Quaternion.LookRotation(thread[s + 1] - thread[s]));
            // things left behind on the way up, each dimmer than the last
            string[] things = { "guitar", "laptop", "party lights", "suitcase", "keys" };
            for (var j = 0; j < things.Length; j++)
            {
                var p = Step(4 + j * 6, out _) + up * .05f; var out_ = (p - new Vector3(c.x, p.y, c.z)).normalized; p += out_ * .2f;
                var glow = Cue(id, Color.Lerp(WindowGlow, new Color(.6f, .65f, .8f), j / 4f), 1.1f - j * .2f, 1.2f);
                switch (j)
                {
                    case 0: mk.Blob(WorldMaterials.Solid(new Color(.5f, .32f, .18f)), p + up * .35f, new Vector3(.22f, .3f, .08f), ctx.Rng, 0f); mk.Box(darkWood, p + up * .8f, new Vector3(.05f, .6f, .04f), Quaternion.LookRotation(out_)); mk.Blob(glow, p + up * .35f, Vector3.one * .06f, ctx.Rng, 0f); break;
                    case 1: mk.Box(iron, p + up * .03f, new Vector3(.4f, .03f, .3f), Quaternion.LookRotation(out_)); mk.Box(glow, p + up * .2f - out_ * .13f, new Vector3(.4f, .26f, .02f), Quaternion.LookRotation(out_) * Quaternion.Euler(-15f, 0, 0)); break;
                    case 2: for (var b = 0; b < 6; b++) mk.Blob(glow, p + up * (.1f + Mathf.Abs(Mathf.Sin(b)) * .15f) + Vector3.Cross(up, out_) * (b - 2.5f) * .12f, Vector3.one * .04f, ctx.Rng, 0f); break;
                    case 3: mk.Box(WorldMaterials.Solid(new Color(.35f, .25f, .3f)), p + up * .25f, new Vector3(.5f, .45f, .2f), Quaternion.LookRotation(out_)); mk.Box(glow, p + up * .5f, new Vector3(.15f, .03f, .03f), Quaternion.LookRotation(out_)); break;
                    case 4: mk.Prism(glow, p + up * .02f, out_, .05f, .02f, 8); mk.Box(glow, p + up * .02f + out_ * .1f, new Vector3(.02f, .02f, .12f), Quaternion.LookRotation(out_)); break;
                }
            }
            // the summit: a black sun set in a wall of ice, weeping
            var top = c + up * H; var sunFace = face;
            var iceM = WorldMaterials.Solid(new Color(.7f, .82f, .92f), .9f);
            mk.Box(iceM, top + up * 1.3f - sunFace * .3f, new Vector3(3f, 3f, .5f), Quaternion.LookRotation(sunFace));
            solid.Box(iceM, top + up * 1.3f - sunFace * .3f, new Vector3(3f, 3f, .5f), Quaternion.LookRotation(sunFace));
            var sun = top + up * 1.6f - sunFace * .02f;
            mk.Prism(WorldMaterials.Solid(new Color(.02f, .02f, .03f)), sun, sunFace, .85f, .06f, 24);
            var corona = Cue(id, Ice, .7f, 2.5f);
            for (var k = 0; k < 28; k++) { var a = k * Mathf.PI * 2f / 28f; var rr = Quaternion.LookRotation(sunFace) * new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0); mk.Box(corona, sun + rr * .93f + sunFace * .05f, new Vector3(.12f, .12f, .03f), Quaternion.LookRotation(sunFace) * Quaternion.Euler(0, 0, a * Mathf.Rad2Deg)); }
            for (var k = 0; k < 6; k++)
            {
                var x = (k - 2.5f) * .22f; var len = .5f + (k % 3) * .35f;
                mk.Box(Cue(id, Ice, .3f, 3f, 0f, 1f, 2.2f, k * .17f, .35f), sun - up * (.95f + len / 2f) + Vector3.Cross(up, sunFace) * x + sunFace * .03f, new Vector3(.05f, len, .02f), Quaternion.LookRotation(sunFace));
            }
            ArtistBits.PointLight(root, top + up * 1.8f + sunFace * 1.4f, Ice, 9f, 1.4f);
            // strings of light from the summit to the ground round it
            var strings = Part(root, "Zenith strings", Vector3.zero, Quaternion.identity);
            var str = Cue(id, new Color(.75f, .7f, 1f), .6f, 2.2f);
            for (var k = 0; k < 6; k++)
            {
                var a = Mathf.Atan2(face.z, face.x) + Mathf.PI + (k - 2.5f) * .45f; var end = G(ctx, c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 11f);
                var from = top + up * .2f; strings.mk.Box(str, (from + end) / 2f, new Vector3(.025f, .025f, Vector3.Distance(from, end)), Quaternion.LookRotation(end - from));
            }
            Done(strings); Moves(strings.t, id, SmallHoursLife.Move.Show, default, default, 1.5f, 0f, 1f);
            // a red crack through the rock, brightest while the song plays
            var crack = Cue(id, BloodRed, .5f, 3f);
            var q = c + face * (R * .97f) + up * .4f; var cs = Vector3.Cross(up, face);
            for (var k = 0; k < 9; k++)
            {
                var y = .4f + k * 1.05f; var rr = R * (1f - y / H) + .05f; var n = c + (face * Mathf.Cos(k * .5f) + cs * Mathf.Sin(k * .5f) * .3f).normalized * rr + up * y + cs * Mathf.Sin(k * 1.9f) * .4f;
                mk.Box(crack, (q + n) / 2f, new Vector3(.12f, .12f, Vector3.Distance(q, n) + .05f), Quaternion.LookRotation(n - q)); q = n;
            }
            halos.Add(c + face * (R + .5f) + up * 3f, 2f, BloodRed, .35f);
        }

        /// A brass orrery: two worlds on long arms circling on two crossing orbits; a beam of light over the dais where
        /// they pass closest; half of each track worked brass, half faint dream-light; a star-map table with a ship.
        void Periapsis(LandContext ctx, Transform root, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var brass = WorldMaterials.Solid(new Color(.75f, .58f, .28f), .7f); var stoneD = WorldMaterials.Solid(new Color(.3f, .3f, .34f));
            mk.Prism(stoneD, c, up, 2.2f, .4f, 12); solid.Prism(stoneD, c, up, 2.2f, .4f, 12);
            mk.Prism(brass, c + up * .4f, up, .2f, 2.6f, 10); ArtistBits.PostCollider(root, c, 3f, .4f);
            var hub = c + up * 3f; mk.Blob(brass, hub, Vector3.one * .35f, ctx.Rng, 0f);
            var dream = Cue(id, new Color(.75f, .7f, 1f), .35f, 2f);
            Color[] worlds = { IndieDownsKit.NoahBlue * 1.4f, IndieDownsKit.SeanGreen * 1.8f };
            for (var w = 0; w < 2; w++)
            {
                var tilt = Quaternion.AngleAxis(w == 0 ? 14f : -14f, Vector3.Cross(up, face).normalized) * Quaternion.AngleAxis(w * 70f, up);
                const float RR = 4.6f; const int segs = 40;
                for (var k = 0; k < segs; k++)   // the track: half brass, half dream-light
                {
                    float a0 = k * Mathf.PI * 2f / segs, a1 = (k + 1) * Mathf.PI * 2f / segs;
                    var p0 = hub + tilt * new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * RR; var p1 = hub + tilt * new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * RR;
                    mk.Box(k < segs / 2 ? brass : dream, (p0 + p1) / 2f, new Vector3(.07f, .07f, Vector3.Distance(p0, p1) + .02f), Quaternion.LookRotation(p1 - p0));
                }
                var arm = Part(root, "Periapsis world " + w, hub, tilt);
                arm.mk.Box(brass, new Vector3(RR / 2f, 0, 0), new Vector3(RR, .08f, .08f), Quaternion.identity);
                arm.mk.Blob(Cue(id, worlds[w], 1f, 2.5f), new Vector3(RR, 0, 0), Vector3.one * .6f, ctx.Rng, .05f);
                Done(arm);
                Moves(arm.t, id, SmallHoursLife.Move.Spin, Vector3.up, default, w == 0 ? 14f : -11f, 0f, 1f, 1.5f);
            }
            mk.Box(Cue(id, new Color(.9f, .95f, 1f), .2f, 3f, 0f, 1f, 3.5f, 0f, .2f), c + up * 1.9f, new Vector3(.1f, 2.8f, .1f), Quaternion.identity);
            // the star-map table, with a little ship on one star
            var tb = c + face * 4f; var tr = Quaternion.LookRotation(face);
            mk.Box(darkWood, tb + up * .8f, new Vector3(1.6f, .08f, 1f), tr); foreach (var s in new[] { -.7f, .7f }) mk.Box(darkWood, tb + up * .4f + Vector3.Cross(up, face) * s, new Vector3(.08f, .8f, .9f), tr);
            solid.Box(darkWood, tb + up * .45f, new Vector3(1.6f, .9f, 1f), tr);
            mk.Box(WorldMaterials.Solid(new Color(.05f, .06f, .12f)), tb + up * .85f, new Vector3(1.5f, .02f, .9f), tr);
            var star = Cue(id, Ice, .8f, 1.5f);
            for (var k = 0; k < 18; k++) mk.Blob(star, tb + up * .87f + tr * new Vector3(ctx.Range(-.68f, .68f), 0, ctx.Range(-.4f, .4f)), Vector3.one * .018f, ctx.Rng, 0f);
            mk.Box(Cue(id, Moon, 1f, 2f), tb + up * .9f + tr * new Vector3(.3f, 0, .1f), new Vector3(.05f, .03f, .1f), tr);
            halos.Add(hub + up * .6f, 3f, new Color(.7f, .8f, 1f), .35f);
        }

        /// A huge stone hand bursting from glowing cracked ground, holding stolen fire; snapped chains at its wrist;
        /// a crag with an empty shackle; an iron eagle circling. The fire beats unevenly, flickers fast at the
        /// bridge, then throws out sparks for the solo.
        void Prometheus(LandContext ctx, Transform root, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var rock = WorldMaterials.Solid(new Color(.28f, .25f, .25f)); var side = Vector3.Cross(up, face);
            mk.Prism(WorldMaterials.Solid(new Color(.14f, .12f, .12f)), c, up, 4f, .08f, 12);
            var crack = Cue(id, Ember2, .5f, 2.6f, 0f, .6f, .875f, 0f, .4f);
            for (var k = 0; k < 10; k++) { var a = k * Mathf.PI / 5f; var p0 = c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * .8f + up * .1f; var p1 = c + new Vector3(Mathf.Cos(a + .3f), 0, Mathf.Sin(a + .3f)) * 3.6f + up * .1f; mk.Box(crack, (p0 + p1) / 2f, new Vector3(.1f, .02f, Vector3.Distance(p0, p1)), Quaternion.LookRotation(p1 - p0)); }
            var dir = (up * 4f + face * .6f).normalized;
            mk.Prism(rock, c - up * .3f, dir, .9f, 5f, 8, .75f); solid.Prism(rock, c - up * .3f, dir, .9f, 5f, 8);
            var wrist = c - up * .3f + dir * 5f; mk.Blob(rock, wrist + up * .7f, new Vector3(1.1f, .9f, 1f), ctx.Rng, .15f);
            for (var k = 0; k < 4; k++) mk.Box(rock, wrist + up * 1.3f + side * (k - 1.5f) * .4f + face * .15f, new Vector3(.32f, .8f, .32f), Quaternion.LookRotation(face) * Quaternion.Euler(-20f, 0, 0));
            var vein = Cue(id, Ember2, .4f, 2f, 0f, 1f, .875f, .1f, .5f);
            for (var k = 0; k < 6; k++) { var p0 = c + dir * (k * .8f) + side * Mathf.Sin(k * 1.7f) * .4f + face * .85f; var p1 = c + dir * ((k + 1) * .8f) + side * Mathf.Sin((k + 1) * 1.7f) * .4f + face * .82f; mk.Box(vein, (p0 + p1) / 2f, new Vector3(.08f, .08f, Vector3.Distance(p0, p1)), Quaternion.LookRotation(p1 - p0)); }
            var fireAt = wrist + up * 2.2f;
            mk.Blob(Cue(id, new Color(1f, .55f, .15f), 1.2f, 3f, 0f, .6f, .875f, 0f, .55f), fireAt, new Vector3(.7f, 1f, .7f), ctx.Rng, .3f);
            mk.Blob(Cue(id, new Color(1f, .85f, .4f), .8f, 3.5f, .6f, .72f, .1f, 0f, .5f), fireAt + up * .4f, new Vector3(.4f, .7f, .4f), ctx.Rng, .3f);
            var sparks = Part(root, "Prometheus sparks", fireAt, Quaternion.identity);
            var sp = Cue(id, new Color(1f, .75f, .3f), 2.5f, 2f);
            for (var k = 0; k < 40; k++) sparks.mk.Blob(sp, new Vector3(ctx.Range(-4f, 4f), ctx.Range(-.5f, 5f), ctx.Range(-4f, 4f)), Vector3.one * ctx.Range(.04f, .09f), ctx.Rng, 0f);
            Done(sparks); Moves(sparks.t, id, SmallHoursLife.Move.Show, default, default, .4f, .72f, .97f);
            var spin = Part(root, "Prometheus sparks turn", fireAt, Quaternion.identity); Done(spin);
            ArtistBits.PointLight(root, fireAt + face * 1.5f, new Color(1f, .55f, .2f), 12f, 2f); halos.Add(fireAt, 2.4f, new Color(1f, .55f, .2f), .5f);
            // the snapped chains
            foreach (var s in new[] { -1f, 1f }) for (var k = 0; k < 5; k++) mk.Box(iron, wrist + side * s * (.9f + k * .05f) - up * (k * .32f) + up * .3f, new Vector3(.08f, .3f, .16f), Quaternion.Euler(0, k * 90f, 0));
            // the crag with its empty shackle, and the eagle circling over it
            var crag = c - face * 5f + side * 3f; mk.Blob(rock, crag + up * 2.5f, new Vector3(2f, 3.5f, 1.8f), ctx.Rng, .25f); solid.Box(rock, crag + up * 2f, new Vector3(3f, 4f, 2.6f), Quaternion.identity);
            mk.Prism(iron, crag + up * 3f + face * 1.6f, face, .25f, .08f, 10);
            var eagle = Part(root, "Prometheus eagle", crag + up * 9f, Quaternion.identity);
            var em = WorldMaterials.Solid(new Color(.2f, .2f, .22f));
            eagle.mk.Box(em, new Vector3(5f, 0, 0), new Vector3(.3f, .25f, .9f), Quaternion.identity);
            foreach (var s in new[] { -1f, 1f }) eagle.mk.Box(em, new Vector3(5f + s * .9f, .05f, 0), new Vector3(1.6f, .05f, .5f), Quaternion.Euler(0, 0, s * 8f));
            Done(eagle); Moves(eagle.t, id, SmallHoursLife.Move.Spin, Vector3.up, default, 18f, 0f, 1f, 1f);
        }

        /// A ruined temple deep in the forest, its pale columns in a bed of ash, ash drifting down. The columns light
        /// in arpeggios, the two colonnades answer each other, all flare in slow hits, embers rise to a burst, the ash
        /// smoulders red, and at the end new green shoots glow up through it.
        void AshesOfCorinth(LandContext ctx, Transform root, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var side = Vector3.Cross(up, face); var marble = WorldMaterials.Solid(new Color(.84f, .82f, .77f)); var ash = WorldMaterials.Solid(new Color(.42f, .41f, .42f));
            mk.Prism(ash, c + up * .01f, up, 8f, .05f, 20);
            mk.Box(marble, c + up * .2f, new Vector3(6f, .4f, 11f), Quaternion.LookRotation(face)); solid.Box(marble, c + up * .2f, new Vector3(6f, .4f, 11f), Quaternion.LookRotation(face));
            var n = 0;
            foreach (var s in new[] { -1f, 1f })
                for (var k = 0; k < 5; k++, n++)
                {
                    var p = c + side * s * 2.4f + face * (k - 2f) * 2.4f + up * .4f;
                    var broken = (k + (s > 0 ? 1 : 0)) % 3 == 2; var h = broken ? 1.6f + k * .3f : 4.6f;
                    mk.Prism(marble, p, up, .38f, h, 10, .9f); ArtistBits.PostCollider(root, p, h, .45f);
                    if (!broken) mk.Box(marble, p + up * (h + .12f), new Vector3(1f, .24f, 1f), Quaternion.LookRotation(face));
                    var y = Mathf.Min(h - .3f, 3.6f);
                    // the arpeggio (start and return), the two colonnades answering, the slow hits
                    mk.Prism(Cue(id, Moon, .15f, 3f, 0f, .18f, 2f, -n / 10f, .12f), p + up * y, up, .4f, .14f, 10);
                    mk.Prism(Cue(id, Moon, .0f, 3f, .88f, 1f, 2f, -n / 10f, .12f), p + up * (y - .3f), up, .4f, .1f, 10);
                    mk.Prism(Cue(id, s < 0 ? Ice : new Color(1f, .85f, .55f), 0f, 2.5f, .18f, .4f, .5f, s < 0 ? 0f : .5f, .45f), p + up * (y - .6f), up, .4f, .1f, 10);
                    mk.Prism(Cue(id, new Color(1f, .9f, .7f), 0f, 3f, .4f, .55f, 2.2f, 0f, .22f), p + up * 1f, up, .4f, .12f, 10);
                }
            for (var k = 0; k < 3; k++) { var p = c + side * (k - 1f) * 3.5f + face * 5.2f + up * .55f; var dd = (side * (k % 2 == 0 ? 1f : -1f) + face * .2f).normalized; mk.Prism(marble, p, dd, .36f, 2.6f, 10); solid.Box(marble, p + dd * 1.3f, new Vector3(.75f, .75f, 2.6f), Quaternion.LookRotation(dd)); }
            // the ash smouldering red after the breaking point
            mk.Prism(Cue(id, new Color(.9f, .15f, .08f), 0f, 2f, .7f, .88f), c + up * .03f, up, 7.6f, .02f, 20, 1f, 9f);
            // embers rising to the breaking point
            var emb = Part(root, "Corinth embers", c + up * .5f, Quaternion.identity); var em = Cue(id, Ember2, 2.4f, 1f);
            for (var k = 0; k < 40; k++) emb.mk.Blob(em, new Vector3(ctx.Range(-5f, 5f), ctx.Range(0f, 2f), ctx.Range(-5f, 5f)), Vector3.one * ctx.Range(.04f, .08f), ctx.Rng, 0f);
            Done(emb); life.movers.Add(new SmallHoursLife.Mover { t = emb.t, song = id, kind = SmallHoursLife.Move.Slide, a = c + up * .5f, b = c + up * 9f, from = .55f, to = .7f });
            var emS = Part(root, "Corinth embers show", c, Quaternion.identity); Done(emS);
            // new green shoots at the end
            var shoots = Part(root, "Corinth shoots", c, Quaternion.identity); var green = Cue(id, new Color(.4f, 1f, .45f), 1.8f, 1f);
            for (var k = 0; k < 30; k++) { var p = new Vector3(ctx.Range(-6f, 6f), .05f, ctx.Range(-6f, 6f)); shoots.mk.Prism(green, p, up, .03f, ctx.Range(.2f, .45f), 4); shoots.mk.Blob(green, p + up * .45f, new Vector3(.08f, .04f, .08f), ctx.Rng, 0f); }
            Done(shoots); Moves(shoots.t, id, SmallHoursLife.Move.Show, default, default, 1.5f, .88f, 1f);
            // ash drifting through the trees
            for (var k = 0; k < 40; k++) mk.Blob(WorldMaterials.Solid(new Color(.6f, .6f, .62f)), c + new Vector3(ctx.Range(-7f, 7f), ctx.Range(1f, 7f), ctx.Range(-7f, 7f)), Vector3.one * .04f, ctx.Rng, 0f);
            ArtistBits.PointLight(root, c + up * 5f, new Color(1f, .85f, .6f), 12f, 1.4f);
        }

        /// A giant atom over a round dance floor: the nucleus pulses with the kick, electrons race round tilted rings;
        /// at the drop they jump and throw out flashes of hydrogen's colours, which then drift up into the trees;
        /// ribbons of chopped voices; a game controller on a stump that blips.
        void BohrsReverie(LandContext ctx, Transform root, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up;
            mk.Prism(Cue(id, new Color(.6f, .45f, 1f), .25f, 1.6f, .05f, 1f, .5f, 0f, .3f), c + up * .03f, up, 5.5f, .06f, 24);
            mk.Prism(WorldMaterials.Solid(new Color(.2f, .2f, .26f)), c, up, 5.7f, .04f, 24);
            var nucleus = c + up * 6.5f;
            mk.Blob(Cue(id, Moon, 1f, 3f, .05f, 1f, .5f, 0f, .35f), nucleus, Vector3.one * .9f, ctx.Rng, .1f);
            halos.Add(nucleus, 3f, Moon, .45f);
            float[] radii = { 2.4f, 3.6f, 4.8f }; Vector3[] tilts = { new Vector3(20f, 0, 0), new Vector3(-25f, 60f, 0), new Vector3(10f, 120f, 30f) };
            var ringM = Cue(id, new Color(.5f, .8f, 1f), .3f, 1.5f);
            for (var r = 0; r < 3; r++)
            {
                var tilt = Quaternion.Euler(tilts[r]);
                for (var k = 0; k < 36; k++) { float a0 = k * Mathf.PI / 18f, a1 = (k + 1) * Mathf.PI / 18f; var p0 = nucleus + tilt * new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * radii[r]; var p1 = nucleus + tilt * new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * radii[r]; mk.Box(ringM, (p0 + p1) / 2f, new Vector3(.04f, .04f, Vector3.Distance(p0, p1)), Quaternion.LookRotation(p1 - p0)); }
                var e = Part(root, "Bohr electron " + r, nucleus, tilt); e.mk.Blob(Cue(id, Ice, 1.5f, 2f), new Vector3(radii[r], 0, 0), Vector3.one * .25f, ctx.Rng, 0f); Done(e);
                Moves(e.t, id, SmallHoursLife.Move.Spin, Vector3.up, default, 40f + r * 15f, 0f, 1f, 3f);
            }
            Color[] lines = { new Color(1f, .15f, .15f), new Color(.2f, .9f, 1f), new Color(.25f, .4f, 1f), new Color(.6f, .25f, 1f) };
            var flash = Part(root, "Bohr flashes", nucleus, Quaternion.identity);
            for (var k = 0; k < 24; k++) flash.mk.Blob(WorldMaterials.Glow(lines[k % 4], 4f), new Vector3(ctx.Range(-1f, 1f), ctx.Range(-1f, 1f), ctx.Range(-1f, 1f)).normalized * ctx.Range(2.5f, 5.5f), Vector3.one * .18f, ctx.Rng, 0f);
            Done(flash); Moves(flash.t, id, SmallHoursLife.Move.Show, default, default, .15f, .42f, .6f);
            var drift = Part(root, "Bohr light drifting", nucleus, Quaternion.identity);
            for (var k = 0; k < 40; k++) drift.mk.Blob(WorldMaterials.Glow(lines[k % 4], 2.6f), new Vector3(ctx.Range(-6f, 6f), ctx.Range(-3f, 3f), ctx.Range(-6f, 6f)), Vector3.one * .1f, ctx.Rng, 0f);
            Done(drift); Moves(drift.t, id, SmallHoursLife.Move.Show, default, default, 2f, .6f, 1f);
            var drift2 = Part(root, "Bohr light rising", Vector3.zero, Quaternion.identity); drift.t.SetParent(drift2.t, true); Done(drift2);
            life.movers.Add(new SmallHoursLife.Mover { t = drift2.t, song = id, kind = SmallHoursLife.Move.Slide, a = Vector3.zero, b = up * 9f, from = .6f, to = 1f });
            var voice = Cue(id, Orchid, 0f, 2.5f, .15f, .45f, .25f, 0f, .4f);
            for (var k = 0; k < 14; k++) { var a = k * .45f; var p0 = nucleus + new Vector3(Mathf.Cos(a) * 3f, -1.5f + k * .2f, Mathf.Sin(a) * 3f); var p1 = nucleus + new Vector3(Mathf.Cos(a + .45f) * 3f, -1.3f + k * .2f, Mathf.Sin(a + .45f) * 3f); mk.Box(voice, (p0 + p1) / 2f, new Vector3(.05f, .05f, Vector3.Distance(p0, p1)), Quaternion.LookRotation(p1 - p0)); }
            var stump = G(ctx, c + face * 6.5f + Vector3.Cross(up, face) * 3f);
            mk.Prism(darkWood, stump, up, .35f, .6f, 8); solid.Prism(darkWood, stump, up, .35f, .6f, 8);
            mk.Box(WorldMaterials.Solid(new Color(.2f, .2f, .24f)), stump + up * .66f, new Vector3(.36f, .08f, .2f), Quaternion.LookRotation(face));
            mk.Box(Cue(id, Lime, .3f, 3f, .3f, .4f, 1.4f, 0f, .25f), stump + up * .71f, new Vector3(.06f, .02f, .06f), Quaternion.LookRotation(face));
            ArtistBits.PointLight(root, c + up * 4f, new Color(.6f, .5f, 1f), 13f, 1.6f);
        }

        /// A cranked amp in an open field, a "0" on its side like a race number, a chequered flag, the guitar on its
        /// stand, a cable through three pedals; stepping stones that light like motifs, a little piano and an upright
        /// bass trading, a ladder of light that climbs and breaks, and one long ribbon of sustain out across the land.
        void Formula0(LandContext ctx, Transform root, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var side = Vector3.Cross(up, face); var rot = Quaternion.LookRotation(face);
            var amp = c - face * 2f; var silver = WorldMaterials.Solid(new Color(.78f, .78f, .8f), .6f);
            mk.Box(silver, amp + up * .65f, new Vector3(1.6f, 1.2f, .6f), rot); solid.Box(silver, amp + up * .65f, new Vector3(1.6f, 1.2f, .6f), rot);
            mk.Box(WorldMaterials.Solid(new Color(.12f, .12f, .13f)), amp + up * .55f + face * .31f, new Vector3(1.4f, .8f, .02f), rot);
            mk.Box(Cue(id, new Color(1f, .9f, .6f), .3f, 3f, .58f, .66f, .3f, 0f, .4f), amp + up * 1.1f + face * .31f, new Vector3(1.3f, .14f, .02f), rot);
            ArtistBits.Label(ctx.Root, "0", amp + up * .7f + side * .81f, side, .55f, new Color(.1f, .1f, .1f), TextAnchor.MiddleCenter);
            var flag = amp + side * 1.4f; mk.Prism(iron, flag, up, .03f, 1.8f, 5); ArtistBits.PostCollider(root, flag, 1.8f, .1f);
            for (var i = 0; i < 4; i++) for (var j = 0; j < 3; j++) mk.Box((i + j) % 2 == 0 ? WorldMaterials.Solid(new Color(.95f, .95f, .95f)) : iron, flag + up * (1.45f + j * .14f) + side * (.08f + i * .14f), new Vector3(.14f, .14f, .02f), rot);
            var gtr = c + face * 1f - side * 1.5f; Languedoc(mk, gtr, -side);
            var cable = WorldMaterials.Solid(new Color(.08f, .08f, .09f));
            Vector3[] chain = { gtr + up * .9f, G(ctx, gtr + side * .8f) + up * .05f, G(ctx, gtr + side * 1.6f) + up * .05f, G(ctx, gtr + side * 2.4f) + up * .05f, amp + up * .3f + side * .6f };
            Color[] pedalC = { new Color(.2f, .4f, .8f), new Color(.9f, .75f, .2f), new Color(.15f, .15f, .15f) };
            for (var k = 0; k < 3; k++) { mk.Box(WorldMaterials.Solid(pedalC[k]), chain[k + 1] + up * .04f, new Vector3(.28f, .08f, .38f), rot); mk.Box(Cue(id, BloodRed, .6f, 2f), chain[k + 1] + up * .1f + face * .12f, new Vector3(.04f, .02f, .04f), rot); }
            for (var k = 0; k + 1 < chain.Length; k++) mk.Box(cable, (chain[k] + chain[k + 1]) / 2f, new Vector3(.02f, .02f, Vector3.Distance(chain[k], chain[k + 1])), Quaternion.LookRotation(chain[k + 1] - chain[k]));
            // stepping stones that light like motifs
            for (var k = 0; k < 8; k++)
            {
                var p = G(ctx, c + face * 4.5f + side * ((k - 3.5f) * 1.3f) + face * Mathf.Sin(k * 1.3f) * .7f);
                mk.Prism(WorldMaterials.Solid(new Color(.45f, .45f, .5f)), p, up, .45f, .12f, 8);
                mk.Prism(Cue(id, Color.Lerp(Moon, Lime, k / 7f), .1f, 3f, 0f, .35f, 1.6f, -k / 8f, .14f), p + up * .125f, up, .3f, .02f, 8);
            }
            // a little piano and an upright bass, trading
            var pia = c + side * 4f + face * 1f; mk.Box(darkWood, pia + up * .65f, new Vector3(1.4f, 1.3f, .5f), Quaternion.LookRotation(-side)); solid.Box(darkWood, pia + up * .65f, new Vector3(1.4f, 1.3f, .5f), Quaternion.LookRotation(-side));
            mk.Box(Cue(id, WindowGlow, .2f, 3f, .3f, .45f, 2f, 0f, .5f), pia + up * .85f - side * .26f, new Vector3(1.2f, .06f, .02f), Quaternion.LookRotation(-side));
            var bass = c + side * 2f + face * 2.6f; mk.Blob(WorldMaterials.Solid(new Color(.5f, .3f, .15f)), bass + up * .7f, new Vector3(.4f, .65f, .2f), ctx.Rng, 0f); mk.Box(darkWood, bass + up * 1.6f, new Vector3(.06f, 1f, .05f), rot);
            mk.Box(Cue(id, WindowGlow, .2f, 3f, .3f, .45f, 2f, .5f, .5f), bass + up * .7f + face * .21f, new Vector3(.2f, .3f, .02f), rot);
            ArtistBits.PostCollider(root, bass, 1.9f, .4f);
            // the ladder of light that climbs faster and breaks
            for (var k = 0; k < 10; k++) mk.Box(Cue(id, Lime, 0f, 3f, .45f, .55f, .9f - k * .06f, -k / 10f, .3f), amp + up * (1.6f + k * .45f), new Vector3(.9f, .05f, .05f), rot);
            var brk = Part(root, "Formula 0 sparks", amp + up * 5f, Quaternion.identity); var bs = Cue(id, Lime, 2.5f, 1f);
            for (var k = 0; k < 30; k++) brk.mk.Blob(bs, new Vector3(ctx.Range(-2.5f, 2.5f), ctx.Range(-2.5f, 2.5f), ctx.Range(-2.5f, 2.5f)), Vector3.one * .07f, ctx.Rng, 0f);
            Done(brk); Moves(brk.t, id, SmallHoursLife.Move.Show, default, default, .1f, .55f, .6f);
            // the sustain: one long ribbon of light out across the land, its colour sweeping like the wah; it is
            // only there while it plays (unlit, it read as a dark wire across everything)
            var rib = Part(root, "Formula 0 sustain", Vector3.zero, Quaternion.identity);
            var outward = new Vector3(c.x - exN.x, 0, c.z - exN.z).normalized;
            var room = Mathf.Max(6f, noahR - 6f - Vector2.Distance(new Vector2(c.x, c.z), new Vector2(exN.x, exN.z)));
            var segs = Mathf.Clamp(Mathf.FloorToInt(room / 1.4f), 4, 22);
            var prev = amp + up * .9f;
            for (var k = 1; k <= segs; k++)
            {
                var p = G(ctx, amp + outward * (k * 1.4f) + Vector3.Cross(up, outward) * Mathf.Sin(k * .35f) * 2f) + up * (.9f + Mathf.Sin(k * .5f) * .25f);
                var deep = Cue(id, new Color(1f, .35f, .1f), 0f, 3f, .66f, 1f, 3f, -k / 26f * .5f, .5f);
                var bright = Cue(id, new Color(1f, .9f, .4f), 0f, 3f, .66f, 1f, 3f, .5f - k / 26f * .5f, .5f);
                rib.mk.Box(deep, (prev + p) / 2f, new Vector3(.08f, .08f, Vector3.Distance(prev, p)), Quaternion.LookRotation(p - prev));
                rib.mk.Box(bright, (prev + p) / 2f + up * .1f, new Vector3(.05f, .05f, Vector3.Distance(prev, p)), Quaternion.LookRotation(p - prev));
                prev = p;
            }
            Done(rib); Moves(rib.t, id, SmallHoursLife.Move.Show, default, default, .6f, .66f, 1f);
            ArtistBits.PointLight(root, amp + up * 2f + face * 2f, WindowGlow, 12f, 1.6f);
        }

        /// A roofless room at the end of the path under its own rain: a warm gold wall, a cold frosted stone one, a
        /// fogged window between; a desk with a reel-to-reel and a half-open drawer of letters and tapes, a telephone
        /// with its message light blinking. The reels turn faster; beams stream from them; at the end the drawer shuts.
        void BlindRetrospect(LandContext ctx, Transform root, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var side = Vector3.Cross(up, face); var rot = Quaternion.LookRotation(face);
            mk.Box(darkWood, c + up * .1f, new Vector3(5f, .2f, 4f), rot); solid.Box(darkWood, c + up * .1f, new Vector3(5f, .2f, 4f), rot);
            var back = c - face * 2f;
            mk.Box(Cue(id, new Color(1f, .78f, .35f), .45f, 1.5f), back + side * 1.6f + up * 1.6f, new Vector3(1.8f, 3f, .25f), rot); solid.Box(darkWood, back + side * 1.6f + up * 1.6f, new Vector3(1.8f, 3f, .25f), rot);
            var cold = WorldMaterials.Solid(new Color(.45f, .47f, .52f)); mk.Box(cold, back - side * 1.6f + up * 1.6f, new Vector3(1.8f, 3f, .25f), rot); solid.Box(cold, back - side * 1.6f + up * 1.6f, new Vector3(1.8f, 3f, .25f), rot);
            for (var k = 0; k < 12; k++) mk.Box(WorldMaterials.Solid(new Color(.88f, .92f, .97f)), back - side * (1f + (k % 4) * .35f) + up * (.4f + k * .22f) + face * .14f, new Vector3(.3f, .05f, .02f), rot * Quaternion.Euler(0, 0, k * 23f));
            mk.Box(darkWood, back + up * 1.6f, new Vector3(1.4f, 3f, .25f), rot);
            mk.Box(WorldMaterials.Solid(new Color(.8f, .84f, .88f), .2f), back + up * 1.8f + face * .14f, new Vector3(1f, 1.1f, .03f), rot);
            var desk = c - face * 1f; mk.Box(darkWood, desk + up * .78f, new Vector3(2f, .07f, .8f), rot);
            foreach (var s in new[] { -.9f, .9f }) mk.Box(darkWood, desk + up * .4f + side * s, new Vector3(.07f, .78f, .7f), rot);
            solid.Box(darkWood, desk + up * .42f, new Vector3(2f, .84f, .8f), rot);
            var deck = desk + up * .82f - side * .3f; mk.Box(WorldMaterials.Solid(new Color(.3f, .28f, .26f)), deck + up * .2f, new Vector3(.9f, .4f, .35f), rot);
            foreach (var s in new[] { -.22f, .22f })
            {
                var reel = Part(root, "Tape reel", deck + up * .45f + side * s + face * .19f, rot);
                reel.mk.Prism(WorldMaterials.Solid(new Color(.75f, .75f, .78f)), Vector3.zero, Vector3.forward, .17f, .02f, 12);
                for (var k = 0; k < 3; k++) reel.mk.Box(Cue(id, WindowGlow, .2f, 2.5f, 0f, .95f), Quaternion.Euler(0, 0, k * 120f) * new Vector3(0, .1f, .025f), new Vector3(.03f, .12f, .01f), Quaternion.Euler(0, 0, k * 120f));
                Done(reel); Moves(reel.t, id, SmallHoursLife.Move.Spin, Vector3.forward, default, 25f, 0f, 1f, 6f);
                var beams = Part(root, "Tape beams", deck + up * .45f + side * s + face * .2f, Quaternion.identity); var bm = Cue(id, new Color(1f, .85f, .55f), 2.5f, 1f);
                for (var k = 0; k < 7; k++) { var d = Quaternion.Euler(ctx.Range(-50f, 10f), ctx.Range(-60f, 60f), 0) * face; beams.mk.Box(bm, d * 3f, new Vector3(.04f, .04f, 6f), Quaternion.LookRotation(d)); }
                Done(beams); Moves(beams.t, id, SmallHoursLife.Move.Show, default, default, .3f, .55f, .8f);
            }
            var drawer = Part(root, "The drawer", desk + up * .6f + face * .45f, rot);
            drawer.mk.Box(darkWood, Vector3.zero, new Vector3(.7f, .18f, .6f), Quaternion.identity);
            for (var k = 0; k < 4; k++) drawer.mk.Box(WorldMaterials.Solid(new Color(.92f, .88f, .78f)), new Vector3((k - 1.5f) * .14f, .1f, 0), new Vector3(.1f, .02f, .2f), Quaternion.Euler(0, k * 17f, 0));
            Done(drawer);
            life.movers.Add(new SmallHoursLife.Mover { t = drawer.t, song = id, kind = SmallHoursLife.Move.Slide, a = desk + up * .6f + face * .45f, b = desk + up * .6f + face * .05f, from = .95f, to = 1f });
            var phone = desk + up * .82f + side * .6f; mk.Box(WorldMaterials.Solid(new Color(.15f, .14f, .14f)), phone + up * .06f, new Vector3(.3f, .12f, .22f), rot);
            mk.Box(Cue(id, BloodRed, .4f, 3f, 0f, 1f, 1.2f, 0f, .5f), phone + up * .13f + face * .08f, new Vector3(.03f, .02f, .03f), rot);
            var rain = Cue(id, Ice, .12f, 1.4f, 0f, .95f);
            for (var k = 0; k < 70; k++) mk.Box(rain, c + new Vector3(ctx.Range(-2.6f, 2.6f), ctx.Range(1f, 5f), ctx.Range(-2.2f, 2.2f)), new Vector3(.01f, .35f, .01f), Quaternion.identity);
            ArtistBits.PointLight(root, desk + up * 1.6f + face * .8f, WindowGlow, 7f, 1.2f);
        }
    }
}
