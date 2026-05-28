# Professional Audio Mixing Guide - Office-FlipOut

## Audio Design Philosophy

This audio system follows professional game audio mixing principles to create a clear, non-fatiguing soundscape that supports gameplay without overwhelming the player.

### Key Principles Applied

1. **Frequency Separation** - Different sounds occupy different frequency ranges to avoid muddy/competing audio
2. **Audio Ducking** - Ambient sounds lower when SFX play so important sounds cut through
3. **Volume Hierarchy** - Music > SFX > Ambient (in order of importance)
4. **Spatial Audio** - 3D sounds are positioned in space with realistic falloff
5. **Loudness Compliance** - No clipping (> 1.0 volume), all sounds at safe levels

## Volume Levels (dB Reference)

| Audio Type | Volume | dB Equivalent | Purpose |
|-----------|--------|---------------|---------|
| Background Music | 0.35 | -9dB | Primary focus, clear but not aggressive |
| Microwave Ding | 0.65 | -3.7dB | Important interaction, clear and punchy |
| Spill/Impact | 0.60 | -4.4dB | Important feedback, clear and punchy |
| Player Footsteps | 0.25 | -12dB | Present but subtle, supports immersion |
| NPC Footsteps | 0.20 | -14dB | Subtle, for NPC location awareness |
| Air Conditioner | 0.12 | -18dB | Barely noticeable hum, sets scene ambience |
| Fluorescent Lights | 0.08 | -21.9dB | Subtle high-frequency, adds depth |
| Keyboard Typing | 0.06 | -24.4dB | Very subtle, reduces frequency competition |

## Frequency Ranges

Understanding frequency ranges helps prevent audio from sounding muddy:

**Air Conditioner (AC Hum)**
- Frequency Range: 80-200 Hz (low-mid)
- Type: Broadband noise
- Purpose: Foundation of ambient layer, felt more than heard

**Fluorescent Lights (Hum)**
- Frequency Range: 100-8000 Hz (peaked around 120Hz, 60Hz harmonics)
- Type: Tonal with harmonics
- Purpose: Adds subtle presence, typically 50-60Hz fundamental

**Keyboard Typing**
- Frequency Range: 300-3000 Hz (mid-range)
- Type: Percussive, transient-heavy
- Purpose: Office activity indicator
- **NOTE**: Kept very quiet (0.06) because it occupies music's space

**Background Music**
- Frequency Range: Full spectrum (typically 80-12000 Hz)
- Type: Musical composition
- Purpose: Main audio focus

**Footsteps**
- Frequency Range: 100-3000 Hz (depends on surface)
- Type: Percussive with decay
- Purpose: Movement feedback

**Microwave Ding**
- Frequency Range: 1000-4000 Hz (mid-high, bright)
- Type: Resonant transient
- Purpose: Clear, attention-grabbing

**Spill/Impact**
- Frequency Range: 800-5000 Hz (similar to keyboard, but punchier)
- Type: Percussive transient
- Purpose: Feedback, visceral impact

## Timing & Intervals

### Footsteps
- **Player**: 1.0 second interval
  - Realistic walking pace (~120 steps/minute = 1 step per second)
  - Feels natural with character movement
  
- **NPC**: 1.2 second interval
  - Slightly slower for variation
  - Different from player to distinguish movement

### Ambient Loops
- All ambient sounds loop seamlessly
- No fade-out before loop restarts (seamless looping audio files)
- Different loop lengths prevent phasing artifacts

## Spatial Audio Settings

### 3D Sounds (SFX & Footsteps)
- **Spatial Blend**: 1.0 (fully 3D)
- **Min Distance**: 0.4 units
  - Sound maintains full volume up to this distance
  - Reasonable for office-scale gameplay

- **Max Distance**: 7.0 units
  - Sound fades to zero beyond this distance
  - Encourages proximity-based awareness

- **Rolloff Mode**: Linear
  - Realistic real-world sound falloff
  - Volume decreases linearly with distance

### 2D Sounds (Music & Ambient)
- **Spatial Blend**: 0.0 (fully 2D)
- **Reason**: Consistent throughout gameplay space
- **Benefit**: Always clear and audible

## Audio Ducking System

When SFX are triggered (microwave, spill), the ambient sounds automatically reduce:

```
Normal State:
├─ Music: 0.35
├─ AC Hum: 0.12
├─ Lights: 0.08
└─ Keyboard: 0.06

Ducked State (SFX Playing):
├─ Music: 0.35 (unchanged)
├─ AC Hum: 0.048 (reduced to 40%)
├─ Lights: 0.032 (reduced to 40%)
└─ Keyboard: 0.024 (reduced to 40%)

This ensures SFX cuts through without raising volumes.
```

**Duration**: 0.5 seconds
**Fade Time**: 0.1s in, 0.2s out (asymmetric for natural feel)

## Mixing Strategy by Game State

### During Gameplay
- Music plays at 0.35 (background role)
- Ambient sounds at base level (subtle, atmospheric)
- SFX stand out with ducking support
- Footsteps provide movement feedback

### When Paused
- All sounds pause (Time.timeScale = 0)
- Gives player focus on menu

### When Menu Open
- All sounds stop
- Creates clear separation between states
- Player focuses on UI/text

## Audio Quality Checklist

✅ **No Clipping**: All volumes ≤ 1.0
✅ **Frequency Balance**: No single frequency range dominates
✅ **Dynamic Range**: Ambient ~18-24dB lower than music
✅ **Spatial Accuracy**: 3D audio uses realistic falloff
✅ **Timing Realism**: Footsteps match walking pace
✅ **Professional Ducking**: SFX cut through smoothly
✅ **Seamless Looping**: Ambient sounds loop without artifacts

## Testing the Mix

### Headphones Test
- Put on headphones
- Walk around as player
- Listen for:
  - Footsteps feel natural and present (not overwhelming)
  - Ambient sounds are barely noticeable
  - Music is clear but not aggressive
  - Interactions (microwave, spill) are satisfying

### Speaker Test
- Play on laptop/desktop speakers
- Position player in different areas
- Listen for:
  - Good frequency balance (not muddy, not harsh)
  - Clear spatial effects (SFX come from scene locations)
  - Music doesn't make speech (if any) hard to hear
  - No audio fatigue after 10+ minutes

### Volume Normalization Test
1. Play a 1kHz test tone at -12dB
2. Your game music should feel similar in volume
3. Adjust master volumes if significantly different

## Adjusting the Mix

If audio still feels off, adjust in this order:

1. **Too Muddy?** → Reduce keyboard typing volume further (0.04-0.05)
2. **Too Much Hum?** → Reduce AC volume (0.08-0.10)
3. **SFX Too Quiet?** → Increase SFX by 0.05 increments (avoid > 0.75)
4. **Music Too Loud?** → Reduce music to 0.30-0.32
5. **Footsteps Jarring?** → Increase interval to 1.3-1.5s
6. **Still Clipping?** → Reduce everything by 0.1 uniformly

## Advanced Tweaks

### For More Immersion
- Increase footstep volume to 0.30-0.35
- Add HRTF spatialization (if available)
- Record environment-specific footsteps (carpet, tile, etc.)

### For Arcade Feel
- Increase SFX volumes to 0.70-0.80
- Add brief musical stings on interactions
- Reduce ambient volumes further (0.05-0.08)

### For Horror/Tension
- Increase ambient frequencies to 0.15-0.20
- Use lower-pitched ambient (80Hz focus)
- Reduce SFX volume for subtlety

## Technical Notes

**Audio Compression**: None applied (to preserve dynamics)
**Bit Depth**: 16-bit (standard for games)
**Sample Rate**: 44.1kHz or 48kHz (standard game rates)
**Codec**: MP3 or OGG (compressed for game size)

**Why These Settings?**
- 16-bit is plenty (120dB dynamic range, more than 24-bit needed for games)
- 44.1kHz captures all human-audible frequencies (Nyquist: 22kHz)
- Compression reduces file size without audible loss

## Troubleshooting

**Problem**: Audio sounds thin/tinny
- **Cause**: Frequencies too high or lacking bass
- **Fix**: Increase ambient AC hum slightly (0.15)

**Problem**: Audio sounds muddy/indistinct
- **Cause**: Too many mids/lows competing
- **Fix**: Reduce keyboard typing to 0.04, reduce AC hum to 0.10

**Problem**: SFX gets lost in mix
- **Cause**: Ducking not working or volumes too close
- **Fix**: Verify AudioDucker.DuckAmbience() is called, increase SFX by 0.05

**Problem**: Headphone fatigue
- **Cause**: Frequencies too bright or too loud
- **Fix**: Reduce overall volume by 10% across all sounds

**Problem**: Can't hear music over ambient
- **Cause**: Ambient occupies same frequencies as music
- **Fix**: Reduce keyboard/AC slightly (try 0.08 AC, 0.04 keyboard)

---

**Remember**: Professional audio mixing is about balance and clarity, not loudness. If you're tempted to turn everything up, resist! The current mix is optimized for extended listening without fatigue.

Good audio should feel natural and go unnoticed until it's missing.
