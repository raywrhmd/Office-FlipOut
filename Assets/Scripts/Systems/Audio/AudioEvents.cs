using System;
using UnityEngine;

namespace OfficeFlipOut.Systems
{
    public enum AudioBus
    {
        Master,
        Music,
        Sfx,
        Ambience,
        Ui
    }

    public struct AudioPlaybackRequest
    {
        public AudioManager.AudioClipType ClipType;
        public Vector3 Position;
        public float Volume;
        public float SpatialBlend;
        public AudioBus Bus;
        public bool FollowTarget;
        public Transform Target;
        public float Pitch;
    }

    /// <summary>
    /// Event channel for runtime audio requests.
    /// Gameplay systems should send requests here rather than creating ad-hoc AudioSources.
    /// </summary>
    public static class AudioEvents
    {
        public static event Action<AudioPlaybackRequest> OneShotRequested;

        public static void RequestOneShot(
            AudioManager.AudioClipType clipType,
            Vector3 position,
            float volume = 1f,
            float spatialBlend = 1f,
            AudioBus bus = AudioBus.Sfx,
            float pitch = 1f)
        {
            OneShotRequested?.Invoke(new AudioPlaybackRequest
            {
                ClipType = clipType,
                Position = position,
                Volume = Mathf.Clamp01(volume),
                SpatialBlend = Mathf.Clamp01(spatialBlend),
                Bus = bus,
                FollowTarget = false,
                Target = null,
                Pitch = Mathf.Clamp(pitch, 0.5f, 2f)
            });
        }

        public static void RequestAttachedOneShot(
            AudioManager.AudioClipType clipType,
            Transform target,
            float volume = 1f,
            float spatialBlend = 1f,
            AudioBus bus = AudioBus.Sfx,
            float pitch = 1f)
        {
            if (target == null)
            {
                return;
            }

            OneShotRequested?.Invoke(new AudioPlaybackRequest
            {
                ClipType = clipType,
                Position = target.position,
                Volume = Mathf.Clamp01(volume),
                SpatialBlend = Mathf.Clamp01(spatialBlend),
                Bus = bus,
                FollowTarget = true,
                Target = target,
                Pitch = Mathf.Clamp(pitch, 0.5f, 2f)
            });
        }
    }
}
