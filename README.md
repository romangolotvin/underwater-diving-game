# Underwater Diving Game

Unity 6 first-person underwater dive prototype — open ocean swimming.

**Editor:** `6000.0.85f1` (Unity 6)

## Prototype includes (current)

- First-person swimming (buoyancy, drag, look, ascend/descend)
- Open seabed with depth variation (shallow → mid → deep); **no rock-wall boundary**
- Soft invisible limit + fog fade only
- Textured ocean floor, rocks, shallow coral, water surface, suit materials (albedo + normals)
- Underwater atmosphere (fog/light by depth)
- Layered ocean ambience + swim bubbles
- Minimal HUD: depth (m) and air (s) only — no meta/prompt text

**Not in this build yet:** speargun, fish, artifacts, mythical creatures (Kraken / Megalodon), economy — per Ro, later.

## How to run

1. Install **Unity Hub** and editor **6000.0.85f1** (or a close Unity 6 build).
2. Hub → **Open** → select this folder (`UnderwaterDiving`).
3. Open scene `Assets/Scenes/Ocean.unity` (or press Play — `GameBootstrap` auto-builds the world).
4. Click Game view, then swim.

### Controls

| Input | Action |
|-------|--------|
| Mouse | Look |
| WASD | Swim |
| Space / E | Ascend |
| Ctrl / Q / C | Descend |
| Shift | Sprint |
| Scroll | Fine vertical trim |
| Esc | Unlock cursor |
| LMB | Lock cursor |

## Windows build

With the editor installed:

```text
Unity.exe -batchmode -nographics -projectPath <this-folder> -buildWindows64Player <out>\UnderwaterDiving.exe -quit -logFile build.log
```

## Reference

`Reference/abyssal-webgpu-example/` — style inspiration only (not shipped into gameplay code).
See `DESIGN_NOTES.md` for Ro’s standing rules.
