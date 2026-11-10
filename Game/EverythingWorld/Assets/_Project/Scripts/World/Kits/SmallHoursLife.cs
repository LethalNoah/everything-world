using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Small Hours, alive at 3 am: the laundrette drums turning, Steph.fm's aerial light blinking, the TV
    /// flickering in Fionavair's window. In Noah Gallagher's place: the Twelfth House wheel turning slowly with
    /// its gondolas hanging upright; whichever of his songs is playing, its gondola or its light in the forest
    /// glows bright and pulses; the moon breathes with the song's energy; the aurora band sways.
    public class SmallHoursLife : MonoBehaviour
    {
        public Transform[] drums = new Transform[0];
        public Material aerialLight, tvWindow, moon, auroraMaterial;
        public Transform wheel, aurora;
        public Transform[] gondolas = new Transform[0];
        public Material[] gondolaLamps = new Material[0], songLights = new Material[0];
        public Color[] gondolaColors = new Color[0], songLightColors = new Color[0];
        public string[] gondolaIds = new string[0], songLightIds = new string[0];
        public float wheelRadius = 13f;
        float[] gondolaGlow = new float[0], songGlow = new float[0];
        Quaternion auroraBase; bool auroraSet;

        void Update()
        {
            var t = Time.time; var dt = Time.deltaTime;
            foreach (var d in drums) if (d != null) d.Rotate(0, 0, 160f * dt, Space.Self);
            if (aerialLight != null) aerialLight.SetColor("_EmissionColor", new Color(1f, .15f, .1f) * (Mathf.Repeat(t, 1.6f) < .5f ? 4f : .3f));
            if (tvWindow != null) tvWindow.SetColor("_EmissionColor", new Color(.55f, .7f, 1f) * (1.4f + Mathf.PerlinNoise(t * 3f, 0f) * 2.2f));

            var d0 = MusicDirector.Instance;
            var id = MusicDirector.ForcedTrackId ?? (d0 != null && d0.CurrentSlot.IsValid ? d0.CurrentSlot.track.id : null);
            var energy = SurpriseDirector.Energy;
            if (moon != null) moon.SetColor("_EmissionColor", new Color(1f, .74f, .06f) * (1.5f + .9f * energy + .25f * Mathf.Sin(t * .8f)));

            if (wheel != null)
            {
                wheel.Rotate(0, 0, (2.2f + 3f * energy) * dt, Space.Self);
                if (gondolaGlow.Length != gondolas.Length) gondolaGlow = new float[gondolas.Length];
                for (var i = 0; i < gondolas.Length; i++)
                {
                    var a = i * Mathf.PI * 2f / gondolas.Length;
                    gondolas[i].position = wheel.TransformPoint(new Vector3(Mathf.Cos(a) * wheelRadius, Mathf.Sin(a) * wheelRadius, 0));
                    gondolas[i].rotation = Quaternion.LookRotation(wheel.forward, Vector3.up) * Quaternion.Euler(0, 0, Mathf.Sin(t * 1.1f + i) * 2.5f);
                    gondolaGlow[i] = Mathf.MoveTowards(gondolaGlow[i], gondolaIds[i] == id ? 1f : 0f, dt / 1.2f);
                    gondolaLamps[i].SetColor("_EmissionColor", gondolaColors[i] * (.9f + gondolaGlow[i] * (3.5f + 1.5f * Mathf.Sin(t * 3f))));
                }
            }
            if (songGlow.Length != songLights.Length) songGlow = new float[songLights.Length];
            for (var i = 0; i < songLights.Length; i++)
            {
                songGlow[i] = Mathf.MoveTowards(songGlow[i], songLightIds[i] == id ? 1f : 0f, dt / 1.2f);
                songLights[i].SetColor("_EmissionColor", songLightColors[i] * (1f + .3f * Mathf.Sin(t * 1.3f + i) + songGlow[i] * (4f + 2f * Mathf.Sin(t * 3f))));
            }
            if (auroraMaterial != null)
            {
                var glow = .65f + .55f * energy + .25f * Mathf.Sin(t * .7f) + .15f * Mathf.Sin(t * 2.3f);
                auroraMaterial.SetColor("_BaseColor", new Color(glow, glow, glow, 1f));
                auroraMaterial.SetFloat("_Strength", glow * 1.3f);
            }
            if (aurora != null)
            {
                if (!auroraSet) { auroraBase = aurora.rotation; auroraSet = true; }
                aurora.rotation = auroraBase * Quaternion.Euler(Mathf.Sin(t * .2f) * 4f, Mathf.Sin(t * .13f) * 6f, 0);
            }
        }
    }
}
