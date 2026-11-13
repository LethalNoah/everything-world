using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// Moves a fast-travel tower's parts on its own beat (from the shared clock, so friends see the same thing):
    /// things that spin, speaker cones that pump, arms that swing, lights that pulse and lightning that flickers.
    /// Does nothing when you are far away.
    public class TowerLife : MonoBehaviour
    {
        public float bpm = 120f;
        public readonly List<(Transform t, Vector3 axis, float speed)> spin = new List<(Transform, Vector3, float)>();
        public readonly List<(Transform t, Vector3 home, Vector3 dir, float amount)> pump = new List<(Transform, Vector3, Vector3, float)>();
        public readonly List<(Transform t, Quaternion home, Vector3 axis, float degrees, float rate)> swing = new List<(Transform, Quaternion, Vector3, float, float)>();
        public readonly List<(Material m, Color c, float phase)> pulse = new List<(Material, Color, float)>();
        public readonly List<GameObject> flicker = new List<GameObject>();
        float flickOff;

        void Start() { foreach (var f in flicker) if (f != null) f.SetActive(false); }

        void Update()
        {
            var cam = Camera.main; if (cam == null || (cam.transform.position - transform.position).sqrMagnitude > 900f * 900f) return;
            var dt = Time.deltaTime; var now = (float)(SharedChance.Now % 100000.0);
            var beatT = now * bpm / 60f; var beat = Mathf.Repeat(beatT, 1f); var kick = Mathf.Exp(-beat * 6f) * (.55f + SurpriseDirector.Energy * .7f);
            foreach (var (t, axis, speed) in spin) if (t != null) t.Rotate(axis, speed * dt, Space.Self);
            foreach (var (t, home, dir, amount) in pump) if (t != null) t.localPosition = home + dir * amount * kick;
            foreach (var (t, home, axis, deg, rate) in swing) if (t != null) t.localRotation = home * Quaternion.AngleAxis(Mathf.Sin(now * rate) * deg, axis);
            foreach (var (m, c, ph) in pulse) if (m != null) m.SetColor("_EmissionColor", c * (.75f + .6f * Mathf.Exp(-Mathf.Repeat(beatT + ph, 1f) * 5f)));
            if (flicker.Count > 0)
            {
                if (Time.time > flickOff) foreach (var f in flicker) if (f != null && f.activeSelf) f.SetActive(false);
                if (Random.value < dt * (.4f + SurpriseDirector.Energy)) { var f = flicker[Random.Range(0, flicker.Count)]; if (f != null) { f.SetActive(true); flickOff = Time.time + .12f; } }
            }
        }
    }
}
