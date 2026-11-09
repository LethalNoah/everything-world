using UnityEngine;

namespace EverythingWorld.World
{
    /// Makes a light flicker like a fire or candle.
    public class FireFlicker : MonoBehaviour
    {
        public float baseIntensity = 2f, amount = .35f, speed = 7f;
        Light lit;
        float seed;

        void Start()
        {
            lit = GetComponent<Light>();
            seed = Random.value * 100f;
            if (lit != null) baseIntensity = lit.intensity;
        }

        void Update()
        {
            if (lit == null) return;
            var n = Mathf.PerlinNoise(seed, Time.time * speed) * .7f + Mathf.PerlinNoise(seed + 9f, Time.time * speed * 2.7f) * .3f;
            // livelier for high-energy songs, and a flare when a new song starts
            var a = amount * (.6f + .9f * SurpriseDirector.Energy);
            lit.intensity = baseIntensity * (1f - a + n * a * 2f) * (1f + .5f * SurpriseDirector.Pulse);
        }
    }
}
