using UnityEngine;

namespace OfficeFlipOut.Systems
{
    /// <summary>
    /// Plays the comedic Valkyrie sting when an NPC flips out.
    /// </summary>
    [DisallowMultipleComponent]
    public class NpcFlipOutAudioDirector : MonoBehaviour
    {
        [SerializeField] private Rage_Meter rageMeter;
        [SerializeField, Min(0f)] private float valkyrieBrassFanfareStartSeconds = 43.5f;
        [SerializeField, Min(0.05f)] private float valkyrieFadeInSeconds = 0.85f;
        [SerializeField] private bool duckAmbienceOnFlipOut = true;
        [SerializeField, Min(0.1f)] private float ambienceDuckDuration = 1.2f;

        private void Awake()
        {
            if (rageMeter == null)
            {
                rageMeter = GetComponent<Rage_Meter>();
                if (rageMeter == null)
                {
                    rageMeter = GetComponentInParent<Rage_Meter>();
                }
            }
        }

        private void OnEnable()
        {
            if (rageMeter != null)
            {
                rageMeter.FlippedOut += HandleFlippedOut;
            }
        }

        private void OnDisable()
        {
            if (rageMeter != null)
            {
                rageMeter.FlippedOut -= HandleFlippedOut;
            }
        }

        private void HandleFlippedOut(Rage_Meter meter)
        {
            if (meter != rageMeter)
            {
                return;
            }

            if (duckAmbienceOnFlipOut)
            {
                AudioDucker.DuckAmbience(ambienceDuckDuration);
            }

            AudioManager.PlayBackgroundMusicFromTime(
                valkyrieBrassFanfareStartSeconds,
                valkyrieFadeInSeconds);
        }
    }
}
