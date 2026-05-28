# OfficeScene.unity - Comprehensive Structure Analysis

## Scene Overview
The OfficeScene contains a complete office environment organized into logical areas with structural elements (walls, floors, doors) and interactive props.

---

## Scene Hierarchy & Major Areas

### Top-Level Organization
The scene is divided into the following major groupings:

1. **Main Office Structure** (Room containers at root level)
2. **Break Room** (Area Group at `163910961`)
3. **Dining Area** (Area Group under Break Room)
4. **Bathrooms** (Multiple instances: `Bathroom` and `Bathroom (1)`)
5. **Individual Props** (Root-level instantiated prefabs)

---

## Structural Elements (Do NOT Add RageFlipOutProp)

### Walls
- `wall_standard` (multiple instances)
- `wall_standard_wood_alt_1` (multiple instances with variants)
- `wall_standard_s-deco_wood` (multiple instances - decorative wood variant)
- `wall_standard_door_frame` (door frame structures - NOT a door, just the frame)

### Floors
- `floor_tile` (multiple instances throughout)

### Doors
- `door_a` (multiple instances - actual door objects)

### Miscellaneous Structure
- `whiteboard_wall` (wall-mounted whiteboard)
- `file_cabinet_medium` (large filing cabinet structure)
- `ceilingLight` (lighting fixtures - various numbered instances)

---

## Interactive Props (Should Have RageFlipOutProp)

### Furniture Props
- `(Prb)OfficeChair` - Office chair
- `(Prb)ConferenceTable` - Conference table
- `(Prb)CoffeTable` - Coffee table (in break area)
- `(Prb)Sofa3` - Sofa/couch
- `(Prb)Fatboy` (x2) - Bean bag/fatboy chair

### Storage & Organization
- `Cabinet` - Cabinet (in office)
- `(Prb)BigDrawer` (x2) - Large drawer units
- `(Prb)Shelves2` - Shelving unit
- `(Prb)Shelves3` - Another shelving unit

### Office Equipment
- `(Prb)Printer` - Office printer
- `(Prb)DeskPrinter` - Desktop printer
- `(Prb)VendingMachine` - Vending machine (break room)
- `(Prb)WaterDispenser` - Water cooler/dispenser
- `(Prb)SmokeDetector` - Smoke detector

### Lighting Props
- `(Prb)DeskLight` (x2) - Desk lamps
- `(Prb)CeilingLight1` (x4) - Suspended ceiling lights (light fixtures, separate from structural lights)
- `(Prb)Clock` - Wall clock
- `clock` - Another clock variant

### Food & Beverage
- `(Prb)CoffePot` (x2) - Coffee pot
- `(Prb)KitchenModule` - Kitchen module/counter
- `(Prb)Mug` (x2) - Coffee mug

### Plants & Decoration
- `(Prb)Plant1` (x4) - Indoor plant
- `(Prb)Plant2` (x4) - Another plant variant
- `(Prb)DeskPlant` (x3) - Desk-sized plant
- `(Prb)PlantBox` (x2) - Planter box

### Reading Materials
- `(Prb)BookPile1` (x2) - Stack of books
- `(Prb)BookPile2` (x2) - Another book stack
- `(Prb)BookPile3` (x3) - Yet another book stack
- `(Prb)BookPile4` (x2) - Fourth book stack variant
- `(Prb)BookOpen` - Open book

### Office Accessories
- `(Prb)PenHolder` - Pen holder
- `(Prb)YogaBall` - Exercise/yoga ball
- `(Prb)TrashCan` (x2) - Trash cans

### Post-It Notes & Notes
- `sticky_note_a` (x14) - Yellow/standard sticky notes
- `sticky_note_b` (x14) - Another color sticky notes
- `Pizza Party Cake` - Celebratory cake prop

---

## Current Script Assignments

### Objects WITH RageFlipOutProp Script
Based on the file analysis, the following 67+ objects have `RageFlipOutProp` attached:
- All `(Prb)` prefab instances (props marked as interactive)
- Sticky notes (all instances)
- Pizza Party Cake
- Cabinet
- And other flipable objects

### Objects WITH RageInteractionPropSignal Script
The following objects have `RageInteractionPropSignal` (advanced interaction system):
- `Cabinet` - Has signal for pick-up/interaction events
- `Pizza Party Cake` - Has signal for pick-up events
- `(Prb)BigDrawer` - Has signal
- `(Prb)Shelves2` - Has signal
- `(Prb)Sofa3` - Has signal
- `(Prb)DeskLight` - Has signal
- Various other props

**Note:** Objects with `RageInteractionPropSignal` also typically have:
- `Rigidbody` component (for physics)
- `BoxCollider` component (for collisions)

### Objects WITHOUT Scripts (Structure)
All wall, floor, door, and architectural elements have NO scripts attached - they're purely visual/structural.

---

## Scene Script Summary

### Script Distribution Count
- **RageFlipOutProp**: 67 instances
- **RageInteractionPropSignal**: 10+ instances
- **UniversalAdditionalLightData**: On light objects

---

## Recommendations for Props Needing RageFlipOutProp

### Props That Already Have It ✓
- All items in "Interactive Props" list above
- These are correctly configured as dynamic, physics-enabled objects

### Props That Should NOT Have It (Correctly Excluded) ✓
- All structural walls: `wall_standard*`
- All floor tiles: `floor_tile`
- Doors: `door_a`, door frames
- These are correctly static elements

### Potential Missing RageFlipOutProp
- `whiteboard_wall` - Should probably be flippable if it's a mounted prop
- `file_cabinet_medium` - Should probably have RageFlipOutProp (may already)
- `ceilingLight` instances (if they're meant to be interactive lighting props separate from the scene lights)

---

## Special Notes

1. **Prefabs with (Prb) Prefix**: These are instantiated from prefab files in `Assets/Prefabs/`, indicating they're reusable interactive props

2. **RageInteractionPropSignal**: More advanced than RageFlipOutProp - allows NPCs to react to prop interactions (steal, knock over, etc.)

3. **Area Groupings**: 
   - Break Room contains kitchen props, vending machine, dining furniture
   - Dining Area contains tables and chairs for eating
   - Bathrooms are separate areas

4. **Sticky Notes**: Numerous sticky notes (28 total) scattered throughout - these are likely important environmental storytelling elements

5. **Lighting Strategy**:
   - `ceilingLight` (structural lights for illumination)
   - `(Prb)CeilingLight1` (interactive hanging light fixtures that can be knocked around)
   - `(Prb)DeskLight` (desk lamps that can be flipped)

---

## Summary Statistics

| Category | Count |
|----------|-------|
| Total Props (Prb) | ~50+ |
| Sticky Notes | 28 |
| RageFlipOutProp Objects | 67+ |
| RageInteractionPropSignal Objects | 10+ |
| Structural Walls | 30+ |
| Floor Tiles | 10+ |
| Area Groups | 4 |

---

## Hierarchy File References

The scene uses prefab instances primarily from:
- `Assets/Prefabs/` (office furniture, plants, equipment)
- Props marked with `(Prb)` are instantiated prefabs
- Structures (walls, floors) are likely scene-built or modular building blocks

