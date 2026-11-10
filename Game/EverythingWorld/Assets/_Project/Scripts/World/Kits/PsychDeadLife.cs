using UnityEngine;

namespace EverythingWorld.World
{
    /// The Dead's world and the generator party, moving (part of PsychLife):
    /// - the ship of fools rocks on the sand, its lantern swinging bright for "Ship of Fools"; the palace windows warm
    ///   up for "Brokedown Palace"; the wolf's eyes burn and the moon brightens for "Dire Wolf"; rings spread across the
    ///   still pool for "Ripple"; the rowboat bobs on it, rowing round for "Row Jimmy"; the blue star blazes and the
    ///   sky goes blue for "Stella Blue"; the cracked pillar glows for "Samson and Delilah"; the motel neon fades in and
    ///   out (slowly fading away for "West L.A. Fadeaway"); a red flicker crosses the sky for "Friend of the Devil";
    /// - the generator party's bulbs swing with the music; the slot reels spin, stopping on 3 and 7 for "3's & 7's".
    public partial class PsychLife
    {
        public System.Collections.Generic.List<TextMesh> reels = new System.Collections.Generic.List<TextMesh>();
        float ripple, shipF, palaceF, wolf, stellaF, samsonF, fade, devil, row, sevens;

        void DeadUpdate(float dt, float t, string id, bool here, float energy)
        {
            float To(float v, string song, float secs) => Mathf.MoveTowards(v, here && id == song ? 1f : 0f, dt / secs);
            ripple = To(ripple, PsychKit.Ripple, 2f); shipF = To(shipF, PsychKit.ShipOfFools, 2f); palaceF = To(palaceF, PsychKit.Brokedown, 3f); wolf = To(wolf, PsychKit.DireWolf, 2f);
            stellaF = To(stellaF, PsychKit.StellaBlue, 3f); samsonF = To(samsonF, PsychKit.Samson, 1f); fade = To(fade, PsychKit.Fadeaway, 2f); devil = To(devil, PsychKit.FriendOfTheDevil, 1f);
            row = To(row, PsychKit.RowJimmy, 2f); sevens = To(sevens, PsychKit.ThreesAndSevens, 1f);
            if (stellaF > 0f) SkyDirector.SetSongTint("The Psychedelic Wilds", Color.Lerp(Color.white, new Color(.4f, .55f, 1f), stellaF), .45f * stellaF);
            if (devil > 0f && Mathf.PerlinNoise(t * 2f, 4.4f) > .6f) SkyDirector.SetSongTint("The Psychedelic Wilds", new Color(1f, .3f, .25f), .4f * devil);
            if (ship != null) ship.rotation = Quaternion.Euler(Mathf.Sin(t * .7f) * (1.5f + shipF * 4f), ship.rotation.eulerAngles.y, Mathf.Sin(t * .5f) * (2f + shipF * 5f) + 8f);
            if (shipLantern != null) shipLantern.SetColor("_EmissionColor", new Color(1f, .75f, .35f) * (1.4f + shipF * (3f + Mathf.Sin(t * 4f))));
            if (palace != null) palace.SetColor("_EmissionColor", new Color(1f, .8f, .5f) * (.6f + palaceF * 3f));
            if (wolfEyes != null) wolfEyes.SetColor("_EmissionColor", new Color(1f, .85f, .3f) * (1f + wolf * 5f));
            if (wolfMoon != null) wolfMoon.SetColor("_EmissionColor", new Color(.95f, .95f, 1f) * (1.2f + wolf * 2.5f));
            for (var i = 0; i < ripples.Count; i++)
            {
                var k = Mathf.Repeat(t * (.12f + ripple * .25f) + i / (float)ripples.Count, 1f);
                ripples[i].localScale = Vector3.one * (.5f + k * 5.3f); ripples[i].gameObject.SetActive(here && (ripple > .05f || i == 0));
            }
            if (rippleM != null) rippleM.SetColor("_EmissionColor", new Color(.7f, .9f, 1f) * (.3f + ripple * 2.5f));
            if (rowboat != null && ripplePool != Vector3.zero)
            {
                var a = t * (.05f + row * .35f); var p = ripplePool + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 3.2f + Vector3.up * (Mathf.Sin(t * 1.3f) * .05f - .07f);
                rowboat.SetPositionAndRotation(p, Quaternion.LookRotation(new Vector3(-Mathf.Sin(a), 0, Mathf.Cos(a))) * Quaternion.Euler(0, 0, Mathf.Sin(t * 1.1f) * 3f));
            }
            if (stella != null) stella.SetColor("_EmissionColor", new Color(.35f, .55f, 1f) * (1.4f + stellaF * 4f + .3f * Mathf.Sin(t * 1.2f)));
            if (samson != null) samson.SetColor("_EmissionColor", new Color(1f, .7f, .3f) * (.3f + samsonF * (3f + 2f * Mathf.Sin(t * 8f))));
            if (fadeaway != null) { var neon = fade > .3f ? Mathf.Clamp01(1f - Mathf.Repeat(t * .1f, 1f)) : (Mathf.PerlinNoise(t * 1.5f, 2.2f) > .35f ? 1f : .1f); fadeaway.SetColor("_EmissionColor", new Color(1f, .3f, .55f) * (.3f + neon * 2.4f)); }
            if (partyBulbs != null) partyBulbs.SetColor("_EmissionColor", new Color(1f, .8f, .45f) * (1.6f + energy * 1.5f + .4f * Mathf.Sin(t * 3f)));
            for (var i = 0; i < reels.Count; i++) if (reels[i] != null) reels[i].text = sevens > .5f ? (i == 1 ? "7" : "3") : ((Mathf.FloorToInt(t * 9f + i * 3.7f) % 9) + 1).ToString();
        }
    }
}
