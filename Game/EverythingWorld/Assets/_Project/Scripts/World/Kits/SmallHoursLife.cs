using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Small Hours, alive at 3 am: the laundrette drums turning, Steph.fm's aerial light blinking, the TV
    /// flickering in Fionavair's window. In Noah Gallagher's place: the Twelfth House wheel turning slowly with
    /// its gondolas hanging upright; whichever of his songs is playing, its gondola or its light in the forest
    /// glows bright and pulses; the moon breathes with the song's energy; the aurora band sways.
    public partial class SmallHoursLife : MonoBehaviour
    {
        public Transform[] drums = new Transform[0];
        // the Moonhunter house (SmallHoursMoonhunter)
        public Transform[] moonGems = new Transform[0]; public int[] moonGemLanes = new int[0];
        public Material[] moonFrets = new Material[0], moonWindows = new Material[0]; public Color[] moonFretColors = new Color[0];
        public Vector3 moonA, moonB, moonSide; public float moonLane = .7f; public Light moonSpot; public Material moonTv; public string moonId;
        float moonOn; float[] fretFlash = new float[5]; float[] gemPrev = new float[0];
        public Material aerialLight, tvWindow, moon, auroraMaterial;
        public Transform wheel, aurora;
        public Transform[] gondolas = new Transform[0];
        public Material[] gondolaLamps = new Material[0], songLights = new Material[0];
        public Color[] gondolaColors = new Color[0], songLightColors = new Color[0];
        public string[] gondolaIds = new string[0], songLightIds = new string[0];
        public float wheelRadius = 13f;
        float[] gondolaGlow = new float[0], songGlow = new float[0];
        Quaternion auroraBase; bool auroraSet;

        /// While "Moonhunter" plays: notes stream down the garden's highway, each fret flashing as a note hits
        /// it; every dark window lights up; the spotlight over the karaoke mic comes on; the TV brightens.
        void Moonhunter(string id, float t, float dt, float energy)
        {
            if (moonGems.Length == 0) return;
            moonOn = Mathf.MoveTowards(moonOn, id != null && id == moonId ? 1f : 0f, dt / 1.5f);
            if (gemPrev.Length != moonGems.Length) gemPrev = new float[moonGems.Length];
            var len = Vector3.Distance(moonA, moonB); const float speed = 5f;
            for (var i = 0; i < moonGems.Length; i++)
            {
                var g = moonGems[i]; var on = moonOn > .01f; g.gameObject.SetActive(on);
                if (!on) continue;
                var ph = Mathf.Repeat(t * speed / Mathf.Max(len, 1f) + i * (1f / moonGems.Length) + (i % 3) * .013f, 1f);
                var lane = moonGemLanes[i];
                g.position = Vector3.Lerp(moonA, moonB, ph) + moonSide * ((lane - 2f) * moonLane) + Vector3.up * .14f;
                g.localScale = Vector3.one * Mathf.Min(moonOn, Mathf.Min(1f, (1f - ph) * 25f));
                if (gemPrev[i] < .97f && ph >= .97f && lane < fretFlash.Length) fretFlash[lane] = 1f;
                gemPrev[i] = ph;
            }
            for (var l = 0; l < moonFrets.Length && l < fretFlash.Length; l++)
            {
                fretFlash[l] = Mathf.MoveTowards(fretFlash[l], 0f, dt * 4f);
                moonFrets[l].SetColor("_EmissionColor", moonFretColors[l] * (.9f + moonOn * 1f + fretFlash[l] * 5f));
            }
            foreach (var w in moonWindows) w.SetColor("_EmissionColor", Color.Lerp(new Color(.12f, .16f, .26f) * .25f, new Color(1f, .45f, .12f) * 1.5f, moonOn));
            if (moonSpot != null) moonSpot.intensity = moonOn * (4f + Mathf.Sin(t * 2f) * .5f);
            if (moonTv != null) moonTv.SetColor("_EmissionColor", new Color(.25f, .3f, .55f) * (1.2f + moonOn * (1.5f + Mathf.PerlinNoise(t * 4f, 0f))));
        }

        void Update()
        {
            var t = Time.time; var dt = Time.deltaTime;
            foreach (var d in drums) if (d != null) d.Rotate(0, 0, 160f * dt, Space.Self);
            if (aerialLight != null) aerialLight.SetColor("_EmissionColor", new Color(1f, .15f, .1f) * (Mathf.Repeat(t, 1.6f) < .5f ? 4f : .3f));
            if (tvWindow != null) tvWindow.SetColor("_EmissionColor", new Color(.55f, .7f, 1f) * (1.4f + Mathf.PerlinNoise(t * 3f, 0f) * 2.2f));

            var d0 = MusicDirector.Instance;
            var id = MusicDirector.ForcedTrackId ?? (d0 != null && d0.CurrentSlot.IsValid ? d0.CurrentSlot.track.id : null);
            SongPlacesUpdate(dt, id);
            ExhibitUpdate(dt);
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
            Moonhunter(id, t, dt, energy);
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
