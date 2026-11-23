# Underwater Diving Game

Unity 6 prototype of a first-person underwater diving game: swim the ocean, hunt fish with a speargun, collect seabed artifacts, sell loot, and upgrade your diving suit and backpack.

**Editor version:** `6000.0.85f1` (Unity 6)

## Status

In progress. Current vertical-slice foundation:

- First-person swimming controller with buoyancy, drag, and look controls
- Water volume / submersion queries
- Diver suit air & depth stats (upgrade hooks)

More systems (speargun, fish AI, artifacts, economy, upgrades UI) are being added next.

## Open in Unity

1. Install **Unity Hub** and editor **6000.0.85f1** (or compatible Unity 6).
2. **Add** → **Add project from disk** → select this repository folder.
3. Open the project and press Play once a bootstrap scene is present.

## Controls (swimming prototype)

| Input | Action |
|-------|--------|
| Mouse | Look |
| WASD | Swim / move |
| Space / E | Ascend |
| Ctrl / Q / C | Descend |
| Shift | Sprint swim |
| Scroll wheel | Fine vertical trim |
| Esc | Unlock cursor |
| LMB | Re-lock cursor |

## Repo layout

```
Assets/Scripts/   Gameplay code
Packages/         UPM manifest
ProjectSettings/  Unity 6 project settings
```
