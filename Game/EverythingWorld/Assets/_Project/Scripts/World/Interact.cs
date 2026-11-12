using System;
using EverythingWorld.Player;
using UnityEngine;

namespace EverythingWorld.World
{
    /// Things you can do something with: look at one within reach and press E.
    /// - Seat: sit down (the camera settles at seated eye height, you can look around and talk); E to stand.
    /// - Toy: it reacts (spins, bobs, swings open, lights up, or whatever its land wants).
    /// - Carryable: pick it up and carry it; E to put it down, click to throw it. It finds its way home if it's
    ///   lost (thrown in the sea or far off).
    /// Each sits on a trigger or a collider of its own; PlayerInteractor (added to the player) finds it by looking.
    /// Kits make them with the helpers below; put moving ones under the land's life object so LandCuller leaves
    /// them alone.
    public abstract class Interactable : MonoBehaviour
    {
        public abstract string Prompt { get; }
        public abstract void Use(PlayerInteractor who);
    }

    public class Seat : Interactable
    {
        public Vector3 facing = Vector3.forward;   // which way you face once seated
        public float seatHeight = HumanScale.SeatHeight;
        public string verb = "Sit";
        public override string Prompt => verb;
        public override void Use(PlayerInteractor who) => who.SitOn(this);
        /// Where your feet go when you stand up: a step in front of the seat.
        public Vector3 StandPoint => transform.position + facing * .9f;
    }

    public class Toy : Interactable
    {
        public enum Kind { Spin, Bob, Swing, Glow, Custom }
        public Kind kind = Kind.Spin;
        public string verb = "Play";
        public Transform moving;          // what spins, bobs or swings (defaults to this)
        public Vector3 axis = Vector3.up; // spin or swing axis, local
        public float amount = 360f;       // degrees for spin and swing, metres for bob
        public float seconds = 1.2f;
        public Material glow; public Color glowColor = Color.white;
        public Action<Toy> onUse;         // anything else the land wants
        public bool On { get; private set; }

        float t = -1f; Quaternion rest; Vector3 restPos;
        public override string Prompt => verb;

        void Awake() { if (moving == null) moving = transform; rest = moving.localRotation; restPos = moving.localPosition; }

        public override void Use(PlayerInteractor who)
        {
            On = !On; onUse?.Invoke(this);
            if (kind == Kind.Glow && glow != null) glow.SetColor("_EmissionColor", glowColor * (On ? 3f : .4f));
            if (kind == Kind.Swing) { t = 0f; return; }
            if (t < 0f || kind != Kind.Spin) t = 0f;
        }

        void Update()
        {
            if (t < 0f || kind == Kind.Glow || kind == Kind.Custom) return;
            t += Time.deltaTime / seconds; var e = Mathf.Clamp01(t);
            switch (kind)
            {
                case Kind.Spin: moving.localRotation = rest * Quaternion.AngleAxis(amount * (1f - Mathf.Pow(1f - e, 3f)), axis); break;
                case Kind.Bob: moving.localPosition = restPos + Vector3.up * Mathf.Sin(e * Mathf.PI) * amount; break;
                case Kind.Swing:
                    var target = On ? amount : 0f; var from = On ? 0f : amount; var s = e * e * (3f - 2f * e);
                    moving.localRotation = rest * Quaternion.AngleAxis(Mathf.Lerp(from, target, s), axis); break;
            }
            if (t >= 1f) { t = -1f; if (kind == Kind.Spin) moving.localRotation = rest; }
        }
    }

    [RequireComponent(typeof(Rigidbody))]
    public class Carryable : Interactable
    {
        public string thing = "it";
        public Vector3 home; public float lowest = -5f;
        public bool Held { get; set; }
        float lostFor;
        public override string Prompt => "Pick up " + thing;
        public override void Use(PlayerInteractor who) => who.PickUp(this);

        void Start() { if (home == Vector3.zero) home = transform.position; }

        void Update()
        {
            if (Held) { lostFor = 0f; return; }
            var lost = transform.position.y < lowest || (transform.position - home).sqrMagnitude > 250f * 250f;
            lostFor = lost ? lostFor + Time.deltaTime : 0f;
            if (lostFor > 4f) GoHome();
        }

        public void GoHome()
        {
            var rb = GetComponent<Rigidbody>(); rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero;
            transform.SetPositionAndRotation(home + Vector3.up * .3f, Quaternion.identity); lostFor = 0f;
        }
    }

    /// Helpers for kits.
    public static class Interact
    {
        /// A seat for one person: 'at' is the middle of the seat surface, 'facing' the way a sitter looks.
        public static Seat Seat(Transform parent, Vector3 at, Vector3 facing, string verb = "Sit")
        {
            facing.y = 0; facing = facing.sqrMagnitude > .001f ? facing.normalized : Vector3.forward;
            var go = new GameObject("Seat"); go.transform.SetParent(parent, false); go.transform.SetPositionAndRotation(at, Quaternion.LookRotation(facing));
            var c = go.AddComponent<BoxCollider>(); c.isTrigger = true; c.size = new Vector3(.6f, .7f, .6f); c.center = new Vector3(0, .25f, 0);
            var s = go.AddComponent<Seat>(); s.facing = facing; s.verb = verb; return s;
        }

        /// Seats along a bench: 'a' and 'b' are the ends of the seat surface, one place every 0.7 m.
        public static void Bench(Transform parent, Vector3 a, Vector3 b, Vector3 facing)
        {
            var n = Mathf.Max(1, Mathf.FloorToInt(Vector3.Distance(a, b) / .7f));
            for (var i = 0; i < n; i++) Seat(parent, Vector3.Lerp(a, b, (i + .5f) / n), facing);
        }

        /// Makes a built object a toy. It needs a collider: give 'size' to add a trigger box round it.
        public static Toy Toy(GameObject go, Toy.Kind kind, string verb, Vector3? size = null)
        {
            if (size.HasValue) { var c = go.AddComponent<BoxCollider>(); c.isTrigger = true; c.size = size.Value; }
            var t = go.AddComponent<Toy>(); t.kind = kind; t.verb = verb; return t;
        }

        /// Makes a built object something you can pick up and throw (a box collider round its mesh).
        public static Carryable Carry(GameObject go, string thing, float mass = 1f, float lowest = -5f)
        {
            var mf = go.GetComponent<MeshFilter>();
            var c = go.AddComponent<BoxCollider>();
            if (mf != null && mf.sharedMesh != null) { c.center = mf.sharedMesh.bounds.center; c.size = mf.sharedMesh.bounds.size; }
            var rb = go.AddComponent<Rigidbody>(); rb.mass = mass; rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            var k = go.AddComponent<Carryable>(); k.thing = thing; k.home = go.transform.position; k.lowest = lowest; return k;
        }
    }
}
