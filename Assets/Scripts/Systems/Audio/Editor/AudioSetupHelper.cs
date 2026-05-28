using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif

namespace OfficeFlipOut.Systems
{
#if UNITY_EDITOR
    /// <summary>
    /// Editor utility to automatically assign audio clips to the AudioManager.
    /// Loads all audio files from Assets/Audio/Ambience/ and assigns them.
    /// </summary>
    public class AudioSetupHelper
    {
        [MenuItem("Office Flip Out/Audio/Setup Audio Clips")]
        public static void SetupAudioClips()
        {
            // Try to find AudioManager in scene
            AudioManager audioManager = ProjectBootstrap.FindFirst<AudioManager>();
            
            if (audioManager == null)
            {
                // Create AudioManager if it doesn't exist
                GameObject go = new GameObject("AudioManager");
                audioManager = go.AddComponent<AudioManager>();
                Debug.Log("[AudioSetupHelper] Created new AudioManager in scene.");
            }

            // Load audio clips from the Ambience folder
            string ambiencePath = "Assets/Audio/Ambience";
            
            if (!Directory.Exists(Path.Combine(Application.dataPath, "../" + ambiencePath)))
            {
                EditorUtility.DisplayDialog("Error", $"Audio folder not found at {ambiencePath}", "OK");
                return;
            }

            // Dictionary mapping serialized field names to audio file names
            var clipMappings = new System.Collections.Generic.Dictionary<string, string>
            {
                { "backgroundMusic", "Background_music_in _game.mp3" },
                { "microwaveDing", "Microwave_ding.mp3" },
                { "walkingSteps", "Walking_sound_effect.mp3" },
                { "airConditioner", "Air_conditioner.mp3" },
                { "fluorescentLights", "Flourescent_lights.mp3" },
                { "keyboardTyping", "Keyboard_typing.mp3" },
                { "titleScreenMusic", "Title_screen_music.mp3" },
                { "rideOfTheValkyries", "Ride_of_the_Valkyries.mp3" }
            };

            int successCount = 0;
            var serializedObject = new SerializedObject(audioManager);

            foreach (var mapping in clipMappings)
            {
                string fieldName = mapping.Key;
                string fileName = mapping.Value;
                string clipPath = $"{ambiencePath}/{fileName}";

                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
                
                if (clip != null)
                {
                    SerializedProperty prop = serializedObject.FindProperty(fieldName);
                    if (prop != null)
                    {
                        prop.objectReferenceValue = clip;
                        successCount++;
                        Debug.Log($"✓ Assigned {fileName} to {fieldName}");
                    }
                    else
                    {
                        Debug.LogWarning($"Could not find serialized property: {fieldName}");
                    }
                }
                else
                {
                    Debug.LogWarning($"Could not load audio clip: {clipPath}");
                }
            }

            serializedObject.ApplyModifiedProperties();
            EditorUtility.DisplayDialog("Audio Setup Complete", $"Successfully assigned {successCount}/{clipMappings.Count} audio clips.", "OK");
        }

        [MenuItem("Office Flip Out/Audio/Wire Flip Out Audio Directors")]
        public static void SetupFlipOutAudioDirectors()
        {
            Rage_Meter[] rageMeters = Object.FindObjectsByType<Rage_Meter>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int addedCount = 0;

            for (int i = 0; i < rageMeters.Length; i++)
            {
                Rage_Meter meter = rageMeters[i];
                if (meter == null)
                {
                    continue;
                }

                NpcFlipOutAudioDirector existing = meter.GetComponent<NpcFlipOutAudioDirector>();
                if (existing != null)
                {
                    continue;
                }

                meter.gameObject.AddComponent<NpcFlipOutAudioDirector>();
                addedCount++;
            }

            if (addedCount > 0)
            {
                Debug.Log($"[AudioSetupHelper] Added NpcFlipOutAudioDirector to {addedCount} Rage_Meter object(s).");
            }
            else
            {
                Debug.Log("[AudioSetupHelper] All Rage_Meter objects already had NpcFlipOutAudioDirector.");
            }
        }
    }
#endif
}
