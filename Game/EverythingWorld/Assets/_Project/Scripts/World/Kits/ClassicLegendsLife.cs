using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The legends' places moving (part of ClassicLife): song glows brighten while their song plays; the red planet
    /// turns; the crystal ball floats; the wind chimes sway; mist curls round the misty mountain.
    public partial class ClassicLife
    {
        public List<(string song, Material m, Color c)> songGlows = new List<(string, Material, Color)>();
        public List<Transform> spinners = new List<Transform>();
        public List<(Transform t, Vector3 home)> floaters = new List<(Transform, Vector3)>();
        public List<(Transform t, float phase)> swingers = new List<(Transform, float)>();
        public List<Vector3> mists = new List<Vector3>();
        readonly Dictionary<string, float> songAmount = new Dictionary<string, float>();
        ParticleSystem mist; float nextMist;

        void LegendsStart()
        {
            mist = Particles("Mountain mist", 300, new Color(.95f, .97f, 1f, .25f), 3f, 6f, false);
            var mm = mist.main; mm.startLifetime = new ParticleSystem.MinMaxCurve(6f, 10f); mm.startSpeed = new ParticleSystem.MinMaxCurve(.1f, .4f);
        }

        void LegendsUpdate(float dt, float t, string id, bool here)
        {
            foreach (var (song, _, _) in songGlows) if (!songAmount.ContainsKey(song)) songAmount[song] = 0f;
            var keys = new List<string>(songAmount.Keys);
            foreach (var s in keys) songAmount[s] = Mathf.MoveTowards(songAmount[s], here && id == s ? 1f : 0f, dt / 2f);
            foreach (var (song, m, c) in songGlows) if (m != null) m.SetColor("_EmissionColor", c * (1f + songAmount[song] * (2.5f + Mathf.Sin(t * 2.2f) * .6f)));
            foreach (var s in spinners) if (s != null) s.Rotate(0, 12f * dt, 0, Space.World);
            foreach (var (tr, home) in floaters) if (tr != null) tr.SetPositionAndRotation(home + Vector3.up * Mathf.Sin(t * .8f) * .3f, Quaternion.Euler(0, t * 20f, 0));
            foreach (var (tr, phase) in swingers) if (tr != null) tr.rotation = Quaternion.Euler(Mathf.Sin(t * 1.3f + phase) * 8f, 0, Mathf.Sin(t * 1.1f + phase * 2f) * 8f);
            nextMist -= dt;
            if (here && mists.Count > 0 && nextMist <= 0f) { nextMist = .3f; mist.transform.position = mists[Random.Range(0, mists.Count)] + new Vector3(Random.Range(-5f, 5f), Random.Range(-1f, 2f), Random.Range(-5f, 5f)); mist.Emit(1); }
        }
    }
}
