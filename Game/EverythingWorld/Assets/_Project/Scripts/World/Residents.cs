using System.Collections.Generic;
using EverythingWorld.Player;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The people who live in a land: small low-poly folk placed with purpose at a land's places (the cook at the
    /// diner, a farmer in a field, two neighbours chatting at a fence), so the world feels lived in.
    /// The system is shared; who they are (clothes, hats, tools, poses, what they say) is each land's own.
    /// - They breathe, glance about, and turn their heads to you when you come near; wavers wave.
    /// - Workers work (a broom, a rake, a fishing rod swinging); walkers stroll a route and pause when you're close.
    /// - Look at one and press E to say hi: they wave and say one of their land's lines.
    /// Make them with Residents.Make; put them under the land's life object so LandCuller leaves them alone.
    public class Resident : Interactable
    {
        public enum Pose { Stand, Sit, Lean, Wave, Work, Walk }
        public Pose pose;
        public Transform head, armL, armR, legL, legR, body;
        public Vector3[] route; public float speed = 1.1f;
        public string[] lines;
        public float workRate = 1f;

        SpeechBubble bubble; float bubbleFor, waveFor, glanceAt, glanceYaw, seed; int lineN, leg; Vector3 legBase;
        Quaternion headRest, armRRest, armLRest;
        public override string Prompt => "Say hi";

        void Start()
        {
            seed = Random.value * 100f; lineN = Random.Range(0, 100);
            if (head != null) headRest = head.localRotation;
            if (armR != null) armRRest = armR.localRotation;
            if (armL != null) armLRest = armL.localRotation;
        }

        public override void Use(PlayerInteractor who)
        {
            waveFor = 2.5f;
            if (lines == null || lines.Length == 0) return;
            if (bubble == null) bubble = SpeechBubble.Make(transform, "Speech");
            bubbleFor = 5f;
            bubble.Show(Wrap(lines[lineN++ % lines.Length], 28), (head != null ? head.position : transform.position + Vector3.up * 1.6f) + Vector3.up * .4f, 5f);
        }

        static string Wrap(string s, int width)
        {
            var words = s.Split(' '); var sb = new System.Text.StringBuilder(); var line = 0;
            foreach (var w in words) { if (line > 0 && line + w.Length > width) { sb.Append('\n'); line = 0; } else if (line > 0) { sb.Append(' '); line++; } sb.Append(w); line += w.Length; }
            return sb.ToString();
        }

        void Update()
        {
            var cam = Camera.main; if (cam == null) return;
            var camPos = cam.transform.position; var toCam = camPos - transform.position; var d2 = toCam.sqrMagnitude;
            if (d2 > 250f * 250f) return;
            var t = Time.time + seed; var dt = Time.deltaTime;

            if (pose == Pose.Walk && route != null && route.Length > 1) Walk(dt, d2 < 2.5f * 2.5f);
            if (d2 > 90f * 90f) return;

            // breathing
            if (body != null) body.localScale = new Vector3(1f, 1f + Mathf.Sin(t * 1.7f) * .012f, 1f);
            // the head: to you when you're near, otherwise an idle glance now and then
            if (head != null)
            {
                Quaternion want;
                if (d2 < 9f * 9f)
                {
                    var local = transform.InverseTransformDirection(toCam - (head.position - transform.position)); local.y *= .6f;
                    var yaw = Mathf.Clamp(Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg, -75f, 75f);
                    var pitch = Mathf.Clamp(-Mathf.Atan2(local.y, new Vector2(local.x, local.z).magnitude) * Mathf.Rad2Deg, -25f, 25f);
                    want = Quaternion.Euler(pitch, yaw, 0);
                }
                else
                {
                    if (Time.time > glanceAt) { glanceAt = Time.time + Random.Range(2.5f, 6f); glanceYaw = Random.value < .5f ? 0f : Random.Range(-50f, 50f); }
                    want = Quaternion.Euler(0, glanceYaw, 0);
                }
                head.localRotation = Quaternion.Slerp(head.localRotation, headRest * want, 1f - Mathf.Exp(-4f * dt));
            }
            // arms
            if (pose == Pose.Wave && d2 < 14f * 14f) waveFor = Mathf.Max(waveFor, .2f);
            if (armR != null)
            {
                Quaternion want = armRRest;
                if (waveFor > 0f) { waveFor -= dt; want = Quaternion.Euler(0, 0, 150f + Mathf.Sin(t * 9f) * 22f); }
                else if (pose == Pose.Work) want = armRRest * Quaternion.Euler(Mathf.Sin(t * 2.4f * workRate) * 32f, 0, 0);
                else if (pose == Pose.Walk) want = armRRest * Quaternion.Euler(Mathf.Sin(t * 5.5f) * 20f, 0, 0);
                armR.localRotation = Quaternion.Slerp(armR.localRotation, want, 1f - Mathf.Exp(-10f * dt));
            }
            if (armL != null)
            {
                var want = armLRest;
                if (pose == Pose.Work) want = armLRest * Quaternion.Euler(Mathf.Sin(t * 2.4f * workRate + .6f) * 26f, 0, 0);
                else if (pose == Pose.Walk) want = armLRest * Quaternion.Euler(-Mathf.Sin(t * 5.5f) * 20f, 0, 0);
                armL.localRotation = Quaternion.Slerp(armL.localRotation, want, 1f - Mathf.Exp(-10f * dt));
            }
            if (bubble != null && bubbleFor > 0f) { bubbleFor -= dt; if (bubbleFor <= 0f) bubble.gameObject.SetActive(false); }
        }

        void Walk(float dt, bool pause)
        {
            if (pause) { SwingLegs(0f); return; }
            var target = route[leg]; var p = transform.position; var to = target - p; to.y = 0;
            if (to.magnitude < .4f) { leg = (leg + 1) % route.Length; return; }
            var dir = to.normalized;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 1f - Mathf.Exp(-5f * dt));
            p += transform.forward * speed * dt;
            var a = route[(leg + route.Length - 1) % route.Length]; var along = Mathf.Clamp01(1f - to.magnitude / Mathf.Max(.1f, Vector3.Distance(new Vector3(a.x, 0, a.z), new Vector3(target.x, 0, target.z))));
            p.y = Mathf.Lerp(a.y, target.y, along);
            transform.position = p; SwingLegs(Mathf.Sin((Time.time + seed) * 5.5f));
        }

        void SwingLegs(float s)
        {
            if (legL != null) legL.localRotation = Quaternion.Euler(s * 25f, 0, 0);
            if (legR != null) legR.localRotation = Quaternion.Euler(-s * 25f, 0, 0);
        }
    }

    public static class Residents
    {
        public enum Hat { None, Cap, Cowboy, Straw, Beanie, Bucket }
        public enum Tool { None, Broom, Rake, Mug, Rod, Hose, Spatula }

        public class Look
        {
            public Color skin = new Color(.85f, .65f, .5f), hair = new Color(.25f, .17f, .1f), top = new Color(.8f, .3f, .25f), bottom = new Color(.25f, .3f, .45f);
            public Hat hat; public Color hatColor = new Color(.9f, .85f, .7f);
            public Tool tool; public float height = 1.75f; public bool apron, longHair; public Color apronColor = Color.white;
        }

        static readonly Color[] Skins = { new Color(.96f, .8f, .68f), new Color(.88f, .68f, .52f), new Color(.75f, .55f, .4f), new Color(.55f, .38f, .26f), new Color(.38f, .26f, .18f) };
        static readonly Color[] Hairs = { new Color(.12f, .09f, .07f), new Color(.3f, .2f, .12f), new Color(.6f, .45f, .25f), new Color(.85f, .75f, .5f), new Color(.75f, .75f, .75f), new Color(.5f, .2f, .1f) };

        /// A random look from a land's palette of tops and bottoms (skin and hair vary across everyone).
        public static Look Random(System.Random rng, Color[] tops, Color[] bottoms, Hat[] hats = null, Color[] hatColors = null)
        {
            var l = new Look
            {
                skin = Skins[rng.Next(Skins.Length)], hair = Hairs[rng.Next(Hairs.Length)], top = tops[rng.Next(tops.Length)], bottom = bottoms[rng.Next(bottoms.Length)],
                height = 1.62f + (float)rng.NextDouble() * .25f, longHair = rng.NextDouble() < .4
            };
            if (hats != null) l.hat = hats[rng.Next(hats.Length)];
            if (hatColors != null) l.hatColor = hatColors[rng.Next(hatColors.Length)];
            return l;
        }

        static Material M(Color c) => WorldMaterials.Solid(c);

        static Transform Part(Transform parent, string name, Vector3 at, MeshKit k)
        {
            var pivot = new GameObject(name).transform; pivot.SetParent(parent, false); pivot.localPosition = at;
            var go = k.BuildSingle(name + " mesh", pivot, false); if (go != null) { go.transform.localPosition = Vector3.zero; go.transform.localRotation = Quaternion.identity; }
            return pivot;
        }

        /// A resident. 'feet' is where they stand (for Sit: the middle of the seat surface, 'seatHeight' above the
        /// ground); 'face' the way they look. 'route' (for Walk) is a loop of ground points.
        public static Resident Make(Transform parent, Vector3 feet, Vector3 face, Look look, Resident.Pose pose, System.Random rng,
                                    string[] lines = null, Vector3[] route = null, float seatHeight = HumanScale.SeatHeight)
        {
            face.y = 0; if (face.sqrMagnitude < .001f) face = Vector3.forward; face.Normalize();
            var root = new GameObject("Resident").transform; root.SetParent(parent, false); root.SetPositionAndRotation(feet, Quaternion.LookRotation(face));
            var r = root.gameObject.AddComponent<Resident>(); r.pose = pose; r.lines = lines; r.route = route;
            var h = look.height; var sit = pose == Resident.Pose.Sit;
            Material skin = M(look.skin), top = M(look.top), bottom = M(look.bottom), hair = M(look.hair), shoe = M(new Color(.12f, .1f, .09f));
            var hip = sit ? 0f : h * .5f;          // the hip line, above the root
            var torsoY = hip + h * .17f;

            // the body: torso (and the legs, unless they walk)
            var bk = new MeshKit();
            bk.Box(top, new Vector3(0, torsoY, 0), new Vector3(.36f * h / 1.75f, h * .32f, .21f), Quaternion.identity);
            bk.Box(top, new Vector3(0, torsoY + h * .155f, 0), new Vector3(.4f * h / 1.75f, .06f, .22f), Quaternion.identity);   // shoulders
            if (look.apron) bk.Box(M(look.apronColor), new Vector3(0, torsoY - h * .05f, .112f), new Vector3(.3f, h * .3f, .01f), Quaternion.identity);
            bk.Box(skin, new Vector3(0, torsoY + h * .19f, 0), new Vector3(.08f, .08f, .08f), Quaternion.identity);   // neck
            if (sit)
            {
                foreach (var x in new[] { -.09f, .09f })
                {
                    bk.Box(bottom, new Vector3(x, .06f, .2f), new Vector3(.13f, .13f, .44f), Quaternion.identity);   // thighs
                    bk.Box(bottom, new Vector3(x, (.06f - seatHeight) / 2f, .4f), new Vector3(.12f, seatHeight + .06f, .12f), Quaternion.identity);   // shins
                    bk.Box(shoe, new Vector3(x, -seatHeight + .04f, .45f), new Vector3(.12f, .08f, .22f), Quaternion.identity);
                }
            }
            r.body = Part(root, "Body", Vector3.zero, bk);

            if (!sit)
            {
                foreach (var side in new[] { -1, 1 })
                {
                    var lk = new MeshKit();
                    lk.Box(bottom, new Vector3(0, -hip / 2f + .04f, 0), new Vector3(.13f, hip - .06f, .14f), Quaternion.identity);
                    lk.Box(shoe, new Vector3(0, -hip + .04f, .04f), new Vector3(.12f, .08f, .24f), Quaternion.identity);
                    var leg = Part(root, side < 0 ? "Leg L" : "Leg R", new Vector3(side * .09f, hip, 0), lk);
                    if (side < 0) r.legL = leg; else r.legR = leg;
                }
                if (pose == Resident.Pose.Lean) r.body.localRotation = Quaternion.Euler(-6f, 0, 0);
            }

            // arms, hanging from the shoulders (a tool in the right hand)
            var sh = torsoY + h * .14f; var armLen = h * .36f;
            foreach (var side in new[] { -1, 1 })
            {
                var ak = new MeshKit();
                ak.Box(top, new Vector3(0, -armLen * .28f, 0), new Vector3(.1f, armLen * .56f, .11f), Quaternion.identity);
                ak.Box(skin, new Vector3(0, -armLen * .75f, 0), new Vector3(.085f, armLen * .44f, .09f), Quaternion.identity);
                if (side > 0) AddTool(ak, look.tool, new Vector3(0, -armLen, .04f), rng);
                var arm = Part(root, side < 0 ? "Arm L" : "Arm R", new Vector3(side * (.22f * h / 1.75f), sh, 0), ak);
                if (sit) arm.localRotation = Quaternion.Euler(-50f, 0, 0);
                else if (pose == Resident.Pose.Lean) arm.localRotation = Quaternion.Euler(10f, 0, side * -8f);
                else if (pose == Resident.Pose.Work && look.tool != Residents.Tool.None) arm.localRotation = Quaternion.Euler(-35f, 0, side * -10f);
                else arm.localRotation = Quaternion.Euler(0, 0, side * 4f);
                if (side < 0) r.armL = arm; else r.armR = arm;
            }

            // the head: face, hair, a hat
            var hk = new MeshKit(); var hr = h * .068f;
            hk.Blob(skin, new Vector3(0, hr, 0), new Vector3(hr * .9f, hr * 1.1f, hr), rng, 0f);
            hk.Blob(hair, new Vector3(0, hr * 1.55f, -hr * .15f), new Vector3(hr * .95f, hr * .55f, hr * .95f), rng, .05f);
            if (look.longHair) hk.Box(hair, new Vector3(0, hr * .6f, -hr * .7f), new Vector3(hr * 1.8f, hr * 1.8f, hr * .5f), Quaternion.identity);
            var dark = M(new Color(.08f, .07f, .07f));
            foreach (var e in new[] { -.32f, .32f }) hk.Box(dark, new Vector3(e * hr, hr * 1.1f, hr * .92f), new Vector3(hr * .16f, hr * .2f, .01f), Quaternion.identity);
            AddHat(hk, look, hr, rng);
            r.head = Part(root, "Head", new Vector3(0, torsoY + h * .21f, 0), hk);

            var col = root.gameObject.AddComponent<CapsuleCollider>();
            col.radius = .3f; col.height = sit ? h * .7f : h; col.center = new Vector3(0, sit ? h * .25f : h / 2f, sit ? .15f : 0);
            return r;
        }

        static void AddTool(MeshKit k, Tool tool, Vector3 hand, System.Random rng)
        {
            var wood = M(new Color(.55f, .38f, .2f)); var up = Vector3.up;
            switch (tool)
            {
                case Residents.Tool.Broom: k.Box(wood, hand + up * .2f, new Vector3(.04f, 1.3f, .04f), Quaternion.identity); k.Box(M(new Color(.85f, .7f, .35f)), hand - up * .5f, new Vector3(.3f, .3f, .08f), Quaternion.identity); break;
                case Residents.Tool.Rake: k.Box(wood, hand + up * .2f, new Vector3(.04f, 1.4f, .04f), Quaternion.identity); k.Box(M(new Color(.5f, .5f, .52f)), hand - up * .52f, new Vector3(.45f, .06f, .06f), Quaternion.identity); break;
                case Residents.Tool.Mug: k.Box(M(new Color(.95f, .95f, .92f)), hand + new Vector3(0, .02f, .04f), new Vector3(.08f, .1f, .08f), Quaternion.identity); break;
                case Residents.Tool.Rod: k.Box(wood, hand + new Vector3(0, .9f, .7f), new Vector3(.03f, .03f, 2.2f), Quaternion.Euler(-40f, 0, 0)); break;
                case Residents.Tool.Hose: k.Box(M(new Color(.2f, .55f, .25f)), hand + new Vector3(0, 0, .15f), new Vector3(.05f, .05f, .35f), Quaternion.identity); break;
                case Residents.Tool.Spatula: k.Box(M(new Color(.6f, .6f, .62f)), hand + new Vector3(0, -.1f, .05f), new Vector3(.1f, .2f, .02f), Quaternion.identity); break;
            }
        }

        static void AddHat(MeshKit k, Look look, float hr, System.Random rng)
        {
            var m = M(look.hatColor); var top = hr * 1.85f;
            switch (look.hat)
            {
                case Residents.Hat.Cap: k.Blob(m, new Vector3(0, top - hr * .1f, 0), new Vector3(hr * 1f, hr * .45f, hr * 1f), rng, 0f); k.Box(m, new Vector3(0, top - hr * .3f, hr * 1f), new Vector3(hr * 1.4f, .02f, hr * .9f), Quaternion.identity); break;
                case Residents.Hat.Cowboy: k.Box(m, new Vector3(0, top - hr * .3f, 0), new Vector3(hr * 3.2f, .03f, hr * 3f), Quaternion.identity); k.Box(m, new Vector3(0, top + hr * .15f, 0), new Vector3(hr * 1.5f, hr * .8f, hr * 1.4f), Quaternion.identity); break;
                case Residents.Hat.Straw: k.Box(m, new Vector3(0, top - hr * .3f, 0), new Vector3(hr * 3.6f, .02f, hr * 3.6f), Quaternion.Euler(0, 45f, 0)); k.Blob(m, new Vector3(0, top, 0), new Vector3(hr * .9f, hr * .5f, hr * .9f), rng, 0f); break;
                case Residents.Hat.Beanie: k.Blob(m, new Vector3(0, top - hr * .05f, 0), new Vector3(hr * 1.02f, hr * .6f, hr * 1.02f), rng, 0f); break;
                case Residents.Hat.Bucket: k.Prism(m, new Vector3(0, top - hr * .35f, 0), Vector3.up, hr * 1.5f, .02f, 10); k.Prism(m, new Vector3(0, top - hr * .35f, 0), Vector3.up, hr * 1f, hr * .7f, 10, .85f); break;
            }
        }
    }
}
