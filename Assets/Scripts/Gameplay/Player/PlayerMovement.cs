using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using OfficeFlipOut.UI;
using OfficeFlipOut.Systems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    public float speed = 8f;

    [Header("Footstep Audio")]
    [SerializeField] private float footstepInterval = 0.45f;
    [SerializeField] private float movementThreshold = 0.1f;
    [SerializeField, Range(0f, 1f)] private float footstepVolume = 0.25f;
    [Tooltip("Max duration of a single footstep sound. Clips the audio file so only one step plays.")]
    [SerializeField] private float singleStepDuration = 0.35f;

    private CharacterController _controller;
    private float footstepTimer = 0f;
    private float stepPlaybackTimer = -1f;
    private AudioSource footstepSource;

    void Awake()
    {
        _controller = GetComponent<CharacterController>();

        GameObject footstepObj = new GameObject("FootstepSource");
        footstepObj.transform.SetParent(transform);
        footstepObj.transform.localPosition = Vector3.zero;
        footstepSource = footstepObj.AddComponent<AudioSource>();
        footstepSource.playOnAwake = false;
        footstepSource.spatialBlend = 0f;
        footstepSource.loop = false;
    }

    void Update()
    {
        if (ClipboardUIState.ShouldBlockGameplayInput)
        {
            footstepTimer = 0f;
            return;
        }

        float horizontalInput = 0f;
        float verticalInput = 0f;
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) horizontalInput -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) horizontalInput += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) verticalInput -= 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) verticalInput += 1f;
        }
#endif

        Vector3 move = transform.right * horizontalInput + transform.forward * verticalInput;

        _controller.SimpleMove(move * speed);

        UpdateFootstepAudio(move.magnitude);

        if (stepPlaybackTimer >= 0f)
        {
            stepPlaybackTimer += Time.deltaTime;
            if (stepPlaybackTimer >= singleStepDuration)
            {
                footstepSource.Stop();
                stepPlaybackTimer = -1f;
            }
        }
    }

    private void UpdateFootstepAudio(float inputMagnitude)
    {
        if (inputMagnitude < movementThreshold)
        {
            footstepTimer = 0f;
            return;
        }

        footstepTimer += Time.deltaTime;

        if (footstepTimer >= footstepInterval)
        {
            PlayFootstep();
            footstepTimer = 0f;
        }
    }

    private void PlayFootstep()
    {
        AudioClip clip = AudioManager.GetClip(AudioManager.AudioClipType.WalkingSteps);
        if (clip == null) return;

        footstepSource.Stop();
        footstepSource.clip = clip;
        footstepSource.time = 0f;
        footstepSource.volume = footstepVolume * AudioManager.SfxVolume;
        footstepSource.Play();
        stepPlaybackTimer = 0f;
    }
}
