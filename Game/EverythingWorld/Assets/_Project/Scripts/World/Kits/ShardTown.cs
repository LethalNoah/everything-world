using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Dubstep Shards' places (part of ShardKit): the Drop Zone (a black stone floor cracked with light in front of the
    /// Speaker Cliff, a wall of speakers the size of a cliff face with great woofers pumping and lasers sweeping from its
    /// top, a mixing desk to hit the sub, giant shards round the edge), the Rift (a road between leaning black slabs veined
    /// with crystal) and the Core (a crater with a glowing pool and a vast crystal turning in the air over it).
    public partial class ShardKit
    {
        ShardTownLife tlife; System.Random prng; Transform rootT; LandContext ctx0;

        static readonly Color[] Tops = { new Color(.08f, .08f, .1f), Violet, Cyan, Magenta, new Color(.3f, .3f, .34f) };
        static readonly Color[] Bottoms = { new Color(.08f, .08f, .1f), new Color(.16f, .16f, .2f) };
        static readonly string[] TownTalk =
        {
            "Feel that in your chest? That's the cliff.", "Every shard here cracked off the Core when the first drop hit.", "Hit the sub on the desk. Then hold on to something.",
            "My ears ring in colours now.", "Walk the Rift to the Core. It hums lower the closer you get.",
        };

        void TownStart(LandContext ctx)
        {
            rootT = ctx.Root; ctx0 = ctx; prng = new System.Random(2010);
            tlife = new GameObject("Shard town life").AddComponent<ShardTownLife>(); tlife.transform.SetParent(life.transform, false);
        }

        Residents.Look Local(Residents.Tool tool = Residents.Tool.None, bool apron = false) { var l = Residents.Random(prng, Tops, Bottoms, new[] { Residents.Hat.Cap, Residents.Hat.None, Residents.Hat.Beanie }, new[] { Cyan, Magenta, Violet }); l.tool = tool; l.apron = apron; return l; }
        Resident Folk(Vector3 feet, Vector3 face, Resident.Pose pose, string[] lines = null, Residents.Look look = null, Vector3[] route = null, float seatH = HumanScale.SeatHeight)
            => Residents.Make(tlife.transform, feet, face, look ?? Local(), pose, prng, lines ?? TownTalk, route, seatH);
        void Col(Vector3 c, Vector3 size, Quaternion rot) => BoxCol(rootT, c, size, rot);
        void Mark(Vector3 at, float r) => noGrass.Add(new Vector3(at.x, r, at.z));
        void Sign(Vector3 at, Vector3 face, string text, float size, Color c) => ArtistBits.Label(rootT, text, at, face, size, c, TextAnchor.MiddleCenter);

        Carryable Thing(MeshKit m, string name, string label, Vector3 at, Quaternion rot, Vector3 size, float mass, bool still = true)
        {
            var go = m.BuildSingle(name, tlife.transform, false); go.transform.SetPositionAndRotation(at, rot); var c = Interact.Carry(go, label, mass, at.y - 30f);
            var bc = c.GetComponent<BoxCollider>(); if (bc != null) { bc.size = size; bc.center = new Vector3(0, size.y / 2f, 0); }
            if (still) { var rb = go.GetComponent<Rigidbody>(); if (rb != null) rb.isKinematic = true; }
            return c;
        }

        /// Something to use that is only a spot in the air.
        Toy Spot(string name, Vector3 at, string verb, Vector3 size)
        {
            var go = new GameObject(name); go.transform.SetParent(tlife.transform, false); go.transform.position = at; return Interact.Toy(go, Toy.Kind.Custom, verb, size);
        }

        // ---------------------------------------------------------------- the Drop Zone and the Speaker Cliff
        void DropZone(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(center.x, center.z); var rng = new System.Random(5);
            mk.Prism(rockD, c - D * 4f - up * .3f, up, 20f, .36f, 32); for (var k = 0; k < 14; k++) { var a = c - D * 4f + Quaternion.Euler(0, k * 26f, 0) * D * (3f + k % 4 * 3f); var b = a + Quaternion.Euler(0, k * 26f + 40f, 0) * D * 3f; mk.Box(k % 2 == 0 ? shardM : shardM2, (a + b) / 2f + up * .07f, new Vector3(.12f, .02f, (b - a).magnitude), Quaternion.LookRotation(b - a)); }
            for (var k = -3; k <= 3; k++) Remember(c + Quaternion.Euler(0, k * 30f, 0) * D * 12f, 0);
            // the Speaker Cliff
            var cf = new PlaceFrame(c - D * 24f, D); const float W = 36f, H = 18f;
            mk.Box(rockD, cf.P(0, H / 2f, -2.5f), new Vector3(W + 6f, H + 2f, 4f), cf.r); solid.Box(rockD, cf.P(0, H / 2f, -1.5f), new Vector3(W + 6f, H + 2f, 4f), cf.r); for (var k = 0; k < 8; k++) mk.Blob(rock, cf.P(-W / 2f - 3f + k * (W + 6f) / 7f, H + 1f + (k % 3), -2.5f), new Vector3(3f, 2f, 3f), rng, .3f);
            for (var x = 0; x < 12; x++) for (var y = 0; y < 6; y++) { var cp = cf.P(-W / 2f + 1.5f + x * 3f, 1.5f + y * 3f, 0); mk.Box(dark, cp, new Vector3(2.9f, 2.9f, .6f), cf.r); if ((x + y) % 5 == 2) continue; mk.Prism(Paint(new Color(.14f, .14f, .18f)), cp + cf.Fwd * .31f, cf.Fwd, 1.2f, .02f, 16); mk.Prism(coneM, cp + cf.Fwd * .33f, cf.Fwd, .4f, .02f, 12); }
            for (var k = 0; k < 4; k++) { var wp = cf.P(-W / 2f + 4.5f + k * 9f, 4.5f + (k % 2) * 6f, .4f); var wm = new MeshKit(); wm.Prism(Paint(new Color(.2f, .2f, .26f)), Vector3.zero, Vector3.forward, 1.9f, .1f, 24); wm.Prism(coneM, Vector3.forward * .1f, Vector3.forward, 1.1f, .1f, 20, .4f); wm.Prism(GlowM(Magenta, 1.4f), Vector3.forward * .2f, Vector3.forward, .4f, .1f, 14); var wg = wm.BuildSingle("Woofer", ctx.Root, false); wg.transform.SetPositionAndRotation(wp, cf.r); life.woofers.Add((wg.transform, wp, k * .4f)); }
            for (var k = 0; k < 6; k++) { var lp = cf.P(-W / 2f + 3f + k * (W - 6f) / 5f, H + .4f, 0); mk.Box(dark, lp, new Vector3(.6f, .5f, .6f), cf.r); var lz = new MeshKit(); lz.Prism(k % 2 == 0 ? shardM : shardM2, Vector3.zero, Vector3.forward, .03f, 90f, 4); var lg = lz.BuildSingle("Laser", ctx.Root, false); lg.transform.position = lp + up * .3f; life.lasers.Add((lg.transform, k * 1.3f)); }
            life.stageFwd = D; halos.Add(cf.P(0, H / 2f, 3f), 16f, Violet, .15f); ArtistBits.PointLight(ctx.Root, cf.P(0, 8f, 8f), Violet, 40f, 2.4f);
            // the mixing desk
            var dk = c - D * 10f; mk.Box(dark, dk + up * .5f, new Vector3(3f, 1f, 1.4f), Quaternion.LookRotation(D)); for (var k = 0; k < 10; k++) mk.Box(k % 3 == 0 ? shardM2 : shardM, dk + up * 1.02f + S * (-1.2f + k * .27f), new Vector3(.08f, .02f, .5f), Quaternion.LookRotation(D)); BoxCol(ctx.Root, dk + up * .5f, new Vector3(3f, 1f, 1.4f), Quaternion.LookRotation(D));
            string[] sub = { "You push the sub fader up. The whole cliff breathes in, then the drop hits and every shard in the canyon flares.", "WUB. The ground moves. Somebody's drink is now somewhere else.", "The woofers punch the air so hard the lasers wobble." }; var sn = 0;
            Spot("The mixing desk", dk + up * 1.2f, "Hit the sub", new Vector3(3f, 1f, 1.4f)).onUse = _ => { life.played = Time.time; Say(tlife.transform, dk + up * 2.6f, sub[sn++ % sub.Length], 5f); };
            // giant shards round the Drop Zone
            for (var k = 0; k < 10; k++) { var a = 30f + k * 30f; if (Mathf.Abs(Mathf.DeltaAngle(a, 0f)) < 30f) continue; var sp = c - D * 4f + Quaternion.Euler(0, a, 0) * D * 21f; Shard(ctx.Root, mk, ctx.OnGround(sp.x, sp.z), 8f + (k % 3) * 4f, 1.4f, (sp - c).normalized * .4f, k % 2 == 0 ? shardM : shardM2, solid); }
            for (var k = 0; k < 4; k++) Folk(c - D * (4f + (k % 2) * 2f) + S * (k * 3f - 4.5f), -D, Resident.Pose.Wave, k == 0 ? new[] { "WAIT FOR IT.", "Here it comes. Here it comes!", "I can see the bass. I can actually see it." } : null, Local());
            Folk(c + D * 6f + S * 4f, (-D - S * .3f).normalized, Resident.Pose.Wave, new[] { "Welcome to the Shards! That cliff is all speaker. All of it.", "The Rift goes to the Core. It's the biggest crystal you'll ever see.", "Go hit the sub on the desk. Everybody should, once." });
            Mark(c - D * 4f, 21f); Mark(cf.o, 20f);
            Shot(ctx, "the speaker cliff", c + D * 14f + S * 6f + up * 3f, cf.P(0, 9f, 0));
        }

        // ---------------------------------------------------------------- the Rift
        void Rift(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var pts = new List<Vector3>(); for (var d = 16f; d <= streetL + 6.01f; d += 2f) pts.Add(G(ctx, d, 0)); if (pts.Count < 2) return;
            Strip(mk, pts, path, RiftW, .1f, ctx); foreach (var p in pts) Remember(p, 1); for (var i = 3; i < pts.Count; i += 4) mk.Box(i % 8 == 3 ? shardM : shardM2, pts[i] + up * .12f, new Vector3(RiftW * .8f, .02f, .06f), Quaternion.LookRotation(D) * Quaternion.Euler(0, (i * 37) % 30 - 15f, 0));
            var n = 0; for (var d = 22f; d <= streetL; d += 10f, n++) foreach (var e in new[] { -1f, 1f })
            {
                var g = G(ctx, d + (e > 0 ? 0f : 5f), e * (RiftW / 2f + 3.4f)); var h = 7f + (n * 37 % 5) * 1.4f; var lean = -S * e * .35f; var dir = (up + lean).normalized;
                mk.Prism(rock, g - up * .5f, dir, 2.4f, h, 4, .2f, 45f + n * 13f); for (var k = 0; k < 3; k++) mk.Box((n + k) % 2 == 0 ? shardM : shardM2, g + dir * (1.5f + k * h * .25f) - S * e * 1.4f, new Vector3(.08f, h * .22f, .08f), Quaternion.LookRotation(D) * Quaternion.Euler(0, 0, (e * 20f) + k * 15f)); solid.Box(rock, g + up * 2f, new Vector3(3.4f, 4f, 3.4f), Quaternion.LookRotation(D) * Quaternion.Euler(0, 45f, 0));
                if (n % 2 == 0) Lamp(ctx, mk, G(ctx, d + 2.5f, e * (RiftW / 2f + .8f)), n % 4 == 0);
            }
            Folk(G(ctx, 26f, 1.4f), D, Resident.Pose.Walk, new[] { "Listen. The slabs hum different notes.", "The closer you get to the Core, the lower it goes." }, Local(), new[] { G(ctx, 26f, 1.4f), G(ctx, streetL, 1.4f), G(ctx, streetL, -1.4f), G(ctx, 26f, -1.4f) }).speed = 1.1f;
            Shot(ctx, "along the rift", G(ctx, 17f, -1f) + up * 2f, G(ctx, streetL + 18f, 0) + up * 12f);
        }

        // ---------------------------------------------------------------- the Core
        void Core(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var cc = G(ctx, streetL + 18f, 0); var rng = new System.Random(8);
            mk.Prism(rockD, cc - up * .4f, up, 13f, .5f, 28); for (var k = 0; k < 20; k++) { var d = Quaternion.Euler(0, k * 18f, 0) * D; mk.Blob(rock, cc + d * 12f + up * .6f, new Vector3(2f, 1.4f, 2f), rng, .3f); if (Mathf.Abs(Mathf.DeltaAngle(k * 18f, 180f)) > 20f) solid.Box(rock, cc + d * 12f + up * .7f, new Vector3(3.6f, 1.4f, 2.4f), Quaternion.LookRotation(d)); Remember(cc + d * 14.5f, 1); }
            var pool = new Material(WorldMaterials.Glow(Violet, 1.2f)) { name = "Shard core pool" }; life.pool = pool; mk.Prism(pool, cc + up * .12f, up, 8f, .02f, 28); mk.Prism(rockD, cc - up * .2f, up, 8.4f, .3f, 28);
            var cm = new MeshKit(); var core = new Material(WorldMaterials.Glow(Cyan, 1.4f)) { name = "Shard core" }; life.coreM = core; cm.Prism(core, Vector3.zero, Vector3.up, 3.4f, 6f, 6, .02f); cm.Prism(core, Vector3.zero, Vector3.down, 3.4f, 6f, 6, .02f); cm.Prism(GlowM(Magenta, 1.4f), Vector3.zero, Vector3.up, 3.5f, .3f, 6);
            var cg = cm.BuildSingle("The Core", ctx.Root, false); var at = cc + up * 14f; cg.transform.position = at; life.core = cg.transform; life.coreHome = at;
            for (var k = 0; k < 8; k++) { var fm = new MeshKit(); fm.Prism(k % 2 == 0 ? shardM : shardM2, Vector3.zero, Vector3.up, .5f, 2f, 5, .02f); fm.Prism(k % 2 == 0 ? shardM : shardM2, Vector3.zero, Vector3.down, .5f, 1f, 5, .02f); var fg = fm.BuildSingle("Orbiting shard", ctx.Root, false); life.orbiters.Add((fg.transform, k * Mathf.PI / 4f)); }
            halos.Add(at, 14f, Cyan, .25f); ArtistBits.PointLight(ctx.Root, at - up * 4f, Cyan, 40f, 2.4f); ArtistBits.PointLight(ctx.Root, cc + up * 2f, Violet, 20f, 1.6f);
            Folk(cc - D * 15f + S * 4f, D, Resident.Pose.Stand, new[] { "Nobody knows what holds it up. My guess is subwoofers.", "When the bass drops it spins faster. I've seen it.", "Every shard in the canyon came off that." }, Local());
            Mark(cc, 14f);
            Shot(ctx, "the core", cc - D * 22f + S * 8f + up * 3f, at);
        }
    }

    /// The Dubstep Shards' holder for things you can use and carry.
    public class ShardTownLife : MonoBehaviour { }
}
