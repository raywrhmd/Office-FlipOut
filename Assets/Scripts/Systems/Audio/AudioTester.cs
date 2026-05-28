using UnityEngine;
using OfficeFlipOut.Systems;

namespace OfficeFlipOut.Systems
{
    /// <summary>
    /// Audio testing utility to verify all audio clips are working correctly.
    /// Attach to a gameobject in the scene and use the inspector to test audio.
    /// </summary>
    public class AudioTester : MonoBehaviour
    {
        [Header("Audio Test Controls")]
        [SerializeField] private bool testBackgroundMusic = false;
        [SerializeField] private bool testMicrowaveDing = false;
        [SerializeField] private bool testWalkingSteps = false;
        [SerializeField] private bool testAirConditioner = false;
        [SerializeField] private bool testFluorescentLights = false;
        [SerializeField] private bool testKeyboardTyping = false;
        [SerializeField] private bool testFlipOutValkyrie = false;
        [SerializeField] private bool testAmbientSounds = false;
        [SerializeField] private bool stopAllAudio = false;

        [Header("Test Options")]
        [SerializeField, Range(0f, 1f)] private float testVolume = 0.7f;

        private void OnValidate()
        {
            if (testBackgroundMusic)
            {
                testBackgroundMusic = false;
                PlayAudio(AudioManager.AudioClipType.BackgroundMusic, true);
                Debug.Log("Playing: Background Music");
            }

            if (testMicrowaveDing)
            {
                testMicrowaveDing = false;
                PlayAudio(AudioManager.AudioClipType.MicrowaveDing);
                Debug.Log("Playing: Microwave Ding");
            }

            if (testWalkingSteps)
            {
                testWalkingSteps = false;
                PlayAudio(AudioManager.AudioClipType.WalkingSteps);
                Debug.Log("Playing: Walking Steps");
            }

            if (testAirConditioner)
            {
                testAirConditioner = false;
                PlayAudio(AudioManager.AudioClipType.AirConditioner, true);
                Debug.Log("Playing: Air Conditioner");
            }

            if (testFluorescentLights)
            {
                testFluorescentLights = false;
                PlayAudio(AudioManager.AudioClipType.FluorescentLights, true);
                Debug.Log("Playing: Fluorescent Lights");
            }

            if (testKeyboardTyping)
            {
                testKeyboardTyping = false;
                PlayAudio(AudioManager.AudioClipType.KeyboardTyping, true);
                Debug.Log("Playing: Keyboard Typing");
            }

            if (testFlipOutValkyrie)
            {
                testFlipOutValkyrie = false;
                AudioManager.PlayBackgroundMusicFromTime(43.5f, 0.85f);
                Debug.Log("Playing: Ride of the Valkyries (fanfare entry)");
            }

            if (testAmbientSounds)
            {
                testAmbientSounds = false;
                AmbientSoundManager.PlayAllAmbient();
                Debug.Log("Playing: All Ambient Sounds");
            }

            if (stopAllAudio)
            {
                stopAllAudio = false;
                AudioManager.StopBackgroundMusic();
                AmbientSoundManager.StopAllAmbient();
                Debug.Log("Stopped: All Audio");
            }
        }

        private void PlayAudio(AudioManager.AudioClipType clipType, bool loop = false)
        {
            AudioClip clip = AudioManager.GetClip(clipType);
            if (clip == null)
            {
                Debug.LogWarning($"Audio clip not assigned for {clipType}");
                return;
            }

            if (loop)
            {
                // For testing looping clips, play through background music source
                var audioManager = AudioManager.GetInstance();
                if (audioManager != null)
                {
                    var source = audioManager.GetComponent<AudioSource>();
                    if (source != null)
                    {
                        source.clip = clip;
                        source.loop = true;
                        source.volume = testVolume;
                        source.Play();
                    }
                }
            }
            else
            {
                AudioEvents.RequestOneShot(clipType, Vector3.zero, testVolume, 0f, AudioBus.Sfx);
            }
        }

        /// <summary>
        /// Check if all audio clips are properly assigned.
        /// </summary>
        public static bool VerifyAllClipsAssigned()
        {
            bool allAssigned = true;
            foreach (AudioManager.AudioClipType clipType in System.Enum.GetValues(typeof(AudioManager.AudioClipType)))
            {
                AudioClip clip = AudioManager.GetClip(clipType);
                if (clip == null)
                {
                    Debug.LogError($"Audio clip NOT assigned: {clipType}");
                    allAssigned = false;
                }
                else
                {
                    Debug.Log($"✓ Audio clip assigned: {clipType} ({clip.name})");
                }
            }
            return allAssigned;
        }
    }
}
