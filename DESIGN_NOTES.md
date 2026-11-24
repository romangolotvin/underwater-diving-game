# Design Notes (Ro)

Captured for the Unity underwater diving project. These are planning notes only — not an implementation brief for this save.

## 1. External underwater assets (example / inspiration only)

Ro will send a set of underwater world assets (~200 KB, compressed) made by someone else via ChatGPT.

- Treat them **strictly as an EXAMPLE / inspiration** for quality and style.
- **Do not** drop them into the project as-is.
- Study how they are built (structure, materials, scale, mood) and use that understanding to raise our own bar.

## 2. Expanded world — depth zones

Expand the game world beyond a small map. Add a continuation with greater depth zones:

- At depth: **no more corals**.
- Deeper fauna: deeper fish, larger fish, octopuses, huge squids.
- World should feel like it continues downward into distinct biomes, not a single shallow reef.

## 3. Mythical creatures

Add mythical / apex encounters:

- **Kraken**
- **Megalodon**

## Quality bar

Keep the **15-hour quality bar** — systems and content should feel deliberate and polished relative to that investment, not rushed placeholders.

## Open world (Ro)

**Do not** build a limited/bounded world enclosed by rocks or walls.

- Ro: «не нужно делать тут ограниченный мир, камнями... он там у тебя открытый»
- World must feel **open**.
- If a play-area limit is needed for performance: **invisible soft boundaries** and/or **fog that fades out naturally**.
- **Never** rock walls or a hard visible edge on the ocean floor / perimeter.

## Graphics — textured maximum (Ro)

- Ro: «Графику на максимум, текстуры именно текстурные, без всяких обыкновенных однотонных цветов»
- All surfaces use **real textures** (albedo + normals where possible).
- **No** flat/solid untextured greys or plain colored primitives as final look.
- Ocean floor, rocks, coral, suit, props: lit materials with texture detail.


## Standing rule — no prompts in the game (Ro)

**NEVER** put prompt text, meta-commentary, or developer notes into the game itself.

- Ro gives prompts like “make boars that attack you” — **implement the feature silently**.
- Do **not** display things like “игра похожа на Rust”, “я добавила кабана”, “кабан с клыками как ты сказал” anywhere in the game UI, menus, or in-game text.
- Prompts are **developer-only** communication between Ro and the agent; the game contains **only the finished result**.
- This applies to **all future games and features** too.

## Example reference received (study only — not game content)

Source files from Ro (Downloads), archived under `Reference/abyssal-webgpu-example/` (~147 KB total):

| File | Role |
|------|------|
| `quality.js` | Adaptive quality ladder (High/Med/Low), crash fallback via localStorage |
| `math.js` | Seeded RNG, FBM/noise, surface nets, world grid constants |
| `world.js` | Authored layout primitives (mesas, arches, tunnels, clears) + procedural scatter |
| `shaders.js` | WGSL: density field, lighting, water, leaves, atmosphere |
| `audio.js` | Procedural underwater ambience (Web Audio, no samples) |
| `main.js` | WebGPU renderer, camera, loading pipeline |

### How it’s built (takeaways for Unity quality)

1. **Designed bones + procedural flesh** — hand-placed MESAS / ARCHES / HOLES / CLEARS define readable landmarks; noise and scatter fill the rest. Do not rely on pure random terrain alone.
2. **Depth & zones in the density field** — floor height, plateaus, rings, magenta/purple accent regions, kelp basins; space reads as biomes, not one flat reef.
3. **Instancing + culling** — foliage/props bucketed on an XZ cell grid; quality levels thin instances (every 2nd / 4th leaf) without changing the world seed.
4. **Water as a full stack** — reflection/refraction targets, volumetric feel, light shafts; not a single transparent plane.
5. **Audio sells immersion** — layered rumble, wash, speed-linked rush, bubbles, distant whale; depth and motion modulate mix.
6. **Graceful degradation** — if a load crashes the tab, next visit drops a quality level. Ship a performance story, not only a beauty shot.

**Do not** port these JS/WebGPU files into the Unity player as-is. Use them as a **style and structure bar** when building Unity systems (terrain zones, scatter, water, audio, LOD).
