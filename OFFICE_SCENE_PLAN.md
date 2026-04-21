# Office Scene Integration Plan

If the external plan path is hard to open, use this in-repo copy.

## Source

- Canonical plan path: `C:\Users\Josh\.cursor\plans\office_scene_integration_e1105ece.plan.md`

## Current Status

- Implemented in code:
  - Boss gating + sabotage/flip events in `Assets/Scripts/Systems/Rage/Rage_Meter.cs`
  - NPC feedback helper `Assets/Scripts/Gameplay/NPC/NpcSabotageJuice.cs`
  - Progress display-name lookup by `npcId` in `Assets/Scripts/Systems/ProgressTracker.cs`
  - Clipboard fixes in `Assets/Scripts/UI/Clipboard/ClipboardUIToolkitManager.cs`:
    - Personality from profile data
    - Progress tab joins by `npcId` (not index)
    - Live refresh via `ProgressTracker.ProgressChanged`
  - Runtime transient database fallback removed in:
    - `Assets/Scripts/UI/Clipboard/ClipboardUIToolkitManager.cs`
    - `Assets/Scripts/UI/Clipboard/ClipboardManager.cs`
  - Editor generator added:
    - `Assets/Editor/OfficeFlipOutEmployeeDataBuilder.cs`
  - Editor scene assembler added:
    - `Assets/Editor/OfficeSceneAssembler.cs`
    - Menu: `Office Flip Out -> Assemble OfficeScene Baseline`
  - Random ambiguity compile fix in `Assets/Scripts/Systems/Rage/Rage_Meter.cs`

## Next Todo Steps

1. Run `Office Flip Out -> Generate Default Employee Data` in Unity.
2. In `OfficeScene`, assign `EmployeeProfileDatabase` on `ClipboardUIToolkitManager`, and enable boss gating on Da Boss meter.
3. Merge `Map.unity` + `Josh.unity` content into `OfficeScene.unity` (player rig, UI root, tracker, NPC roots, interaction objects).
   - Baseline automation is now available via `Office Flip Out -> Assemble OfficeScene Baseline`.
4. Place and wire per-NPC rage objects (fish/microwave, stapler, spill, boss-office props) with correct `npcId` targeting.
5. Add/tune `NpcSabotageJuice` refs (particles/SFX/shake) on all NPCs.
6. Add remaining digital signals if still in scope: loud breathing, cigarette smoke.
7. Full playtest + build verification.

