# Audio System Setup & Verification Guide

## Overview
The Office-FlipOut audio system consists of three main managers:
1. **AudioManager** - Centralized management of all audio clips
2. **AmbientSoundManager** - Looping ambient sounds (AC, lights, keyboard)
3. **AudioStateManager** - Responds to game state changes (pause, menu)

## Audio Files Location
All audio files are in: `Assets/Audio/Ambience/`

### Available Audio Clips
- `Air_conditioner.mp3` - Ambient background hum
- `Background_music_in_game.mp3` - Main gameplay music
- `Fluorescent_lights.mp3` - Ambient fluorescent hum
- `Keyboard_typing.mp3` - Office keyboard typing sounds
- `Microwave_ding.mp3` - Microwave activation sound
- `Ride_of_the_Valkyries.mp3` - High-intensity music (rage/boss)
- `Title_screen_music.mp3` - Main menu music
- `Walking_sound_effect.mp3` - Footstep sounds

## Setup Instructions

### Step 1: Find the AudioManager in Scene Hierarchy
1. Play the game or look in the scene
2. The AudioManager will be created automatically at runtime in the `AudioManager` gameobject
3. In the Inspector, you'll see serialized fields for each audio clip

### Step 2: Assign Audio Clips to AudioManager
1. Select the `AudioManager` gameobject in the hierarchy
2. In the Inspector, find the "Audio Clip References" section
3. Assign the audio files from `Assets/Audio/Ambience/` to each field:
   - Background Music → `Background_music_in_game.mp3`
   - Microwave Ding → `Microwave_ding.mp3`
   - Walking Steps → `Walking_sound_effect.mp3`
   - Air Conditioner → `Air_conditioner.mp3`
   - Fluorescent Lights → `Fluorescent_lights.mp3`
   - Keyboard Typing → `Keyboard_typing.mp3`
   - Title Screen Music → `Title_screen_music.mp3`
   - Ride Of The Valkyries → `Ride_of_the_Valkyries.mp3`

### Step 3: Configure AudioManager Settings (Optional)
- **Music Volume**: Adjust from 0.0 to 1.0 (default: 0.5)
- **Play Background Music On Start**: Toggle to play music when game starts

### Step 4: Configure AmbientSoundManager (Optional)
1. Find the `AmbientSoundManager` gameobject in hierarchy
2. Adjust volume levels for each ambient sound:
   - Air Conditioner Volume: default 0.3
   - Fluorescent Lights Volume: default 0.2
   - Keyboard Typing Volume: default 0.25

### Step 5: Configure Movement Footstep Audio (Optional)
1. **Player Footsteps**: Select the Player gameobject
   - Find PlayerMovement component
   - Adjust "Footstep Interval" (default: 0.5 seconds)
   - Adjust "Footstep Volume" (default: 0.6)

2. **NPC Footsteps**: Select NPC gameobjects
   - Find NPCMovement component
   - Adjust "Footstep Interval" (default: 0.5 seconds)
   - Adjust "Footstep Volume" (default: 0.5)

## Audio Usage Throughout Game

### Background Music
- Plays automatically when game starts (if enabled)
- Stops when main menu opens
- Pauses when game is paused
- Resumes when game unpauses

### Ambient Sounds
- Play continuously during gameplay
- Stops when main menu opens
- Pauses when game is paused
- Can be controlled via `AmbientSoundManager` static methods

### Interaction Sounds
- **Microwave Ding**: Plays when microwave interaction completes
- **Coffee Spill**: Uses walking sound effect (should be replaced with spill-specific sound)

### Footstep Sounds
- **Player**: Triggered based on input magnitude and timing
- **NPCs**: Triggered based on NavMesh agent velocity

## Testing Checklist

### ✓ Test Background Music
- [ ] Background music plays on game start
- [ ] Music stops when opening main menu
- [ ] Music resumes when closing menu

### ✓ Test Ambient Sounds
- [ ] AC hum plays during gameplay
- [ ] Fluorescent lights sound plays during gameplay
- [ ] Keyboard typing sound plays during gameplay
- [ ] All ambient sounds stop in main menu

### ✓ Test Footstep Sounds
- [ ] Player footsteps play when moving
- [ ] Player footsteps don't play when standing still
- [ ] NPC footsteps play when walking to waypoints

### ✓ Test Interaction Sounds
- [ ] Microwave ding plays when fish is inserted
- [ ] Coffee spill sound plays when coffee spills (verify this uses correct clip)

### ✓ Test Game Pause
- [ ] All sounds pause when game is paused
- [ ] All sounds resume when game is unpaused

### ✓ Volume Levels
- [ ] Ambient sounds don't drown out music
- [ ] SFX effects are clear and audible
- [ ] Footsteps are subtle and realistic

## Troubleshooting

### No Sound at All
1. Check if AudioListener is enabled on Camera
2. Verify audio clips are assigned in AudioManager Inspector
3. Check Volume Settings (not muted to 0)

### Audio Playing Out of Sync
1. Verify Time.timeScale is correct (shouldn't be negative)
2. Check that AudioSource.playOnAwake is False

### Clipping or Distortion
1. Reduce individual volume levels
2. Reduce Music Volume in AudioManager
3. Reduce volume levels in AmbientSoundManager

### Footsteps Playing Too Frequently
1. Increase "Footstep Interval" in PlayerMovement or NPCMovement
2. Increase "Movement Threshold" to require more input

### Coffee Spill Sound Wrong
- Currently uses WalkingSteps audio
- Should be replaced with spill-specific audio clip if available
- Update line in CoffeeSpillJuiceAnimator.cs: `PlaySpillSfx()` method

## Script Reference

### AudioManager.GetClip(AudioClipType)
Static method to retrieve any audio clip by type.

### AudioManager.PlayBackgroundMusic()
Plays the background music clip (stops any currently playing music).

### AudioManager.PlaySoundAtPosition(clipType, position, volume, spatialBlend)
Plays a one-shot sound effect at a specific world position with optional spatial audio.

### AmbientSoundManager.PlayAllAmbient()
Starts playing all ambient sounds.

### AmbientSoundManager.StopAllAmbient()
Stops all ambient sounds immediately.

### AmbientSoundManager.PauseAllAmbient()
Pauses all ambient sounds (used during game pause).

### AmbientSoundManager.ResumeAllAmbient()
Resumes all paused ambient sounds.

## Notes
- All audio managers persist across scene loads (DontDestroyOnLoad)
- AudioListenerEnforcer ensures only one audio listener is active at runtime
- Audio state is automatically synchronized with game state (pause, menus, win condition)
- All audio is spatialized 3D audio except background music (0% spatial blend = 2D)
