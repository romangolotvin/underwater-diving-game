# Underwater Diving Game

Unity 6 first-person underwater dive prototype — **Open Ocean** site (spawn underwater, shops on submerged platforms), hunting, and trading.

**Editor:** `6000.0.85f1` (Unity 6)

## Prototype includes (current)

- First-person swimming (buoyancy, drag, look, ascend/descend)
- Open seabed with depth zones (Shallow → Twilight → Abyss); **no rock-wall boundary**
- Soft invisible limit + fog fade only
- Textured surfaces everywhere (albedo + normals): floor, rocks, coral, kelp, suit, gun, fish, artifacts, shops
- Underwater atmosphere (fog/light by depth) + **day/night lighting** (no clock / countdown UI)
- Layered ocean ambience + swim bubbles
- Speargun with line: upgrades for **line length**, **harpoon edge**, **harpoon mass** (pulls you toward the catch / down)
- Fish by zone: reef fish, jack, grouper, **moray**, **turtle**, **ray**, hostile **barracuda** (catchable)
- Artifacts to collect and sell
- Economy: credits, catch bag, **Harbor Market** (sell + gear upgrades) and **Outfitter** (decorations)
- Minimal HUD: depth, zone, air, credits, catch — no meta/prompt text

## Controls

| Input | Action |
|-------|--------|
| Mouse | Look |
| WASD | Swim |
| Space / E | Ascend |
| Ctrl / Q / C | Descend |
| Shift | Sprint |
| Scroll | Fine vertical trim |
| L | Toggle flashlight |
| LMB | Fire speargun (underwater) / lock cursor |
| RMB | Reel line |
| F | Open nearby shop (Harbor Market or Outfitter) |
| M | Open / close world map |
| Esc | Unlock cursor / close shop or map |

## Shops

- **Harbor Market** (east pontoon): sell catch & artifacts; upgrade air, depth suit, fins, buoyancy vest, dive lamp, catch bag, speargun line / edge / mass.
- **Outfitter** (west pontoon): buy and equip decorations (wrist, paint, charm, fins, mask).

## How to run

1. Install **Unity Hub** and editor **6000.0.85f1** (or a close Unity 6 build).
2. Hub → **Open** → select this folder (`UnderwaterDiving`).
3. Open scene `Assets/Scenes/Ocean.unity` (or press Play — `GameBootstrap` auto-builds the world).
4. Click Game view, then swim. Surface near the pontoons to trade.

## Windows build

With the editor installed:

```text
Unity.exe -batchmode -nographics -projectPath <this-folder> -buildWindows64Player <out>\UnderwaterDiving.exe -quit -logFile build.log
```

## Reference

`Reference/abyssal-webgpu-example/` — style inspiration only (not shipped into gameplay code).
See `DESIGN_NOTES.md` for Ro’s standing rules.
