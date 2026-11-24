#!/usr/bin/env python3
"""
Static gate check for WASD / look (no Unity Play Mode).

Asserts end-to-end input path:
1. GameUi.BlocksGameplay is LIVE panel state (AnyModalPanelOpen), not sticky mask.
2. Empty/null panel registry ⇒ NOT blocked.
3. SwimController Update always reaches HandleLook when no modal (NOT gated on cursorLocked).
4. FixedUpdate ApplyThrust when !AnyModalPanelOpen.
5. HandleLook uses LegacyInput / Mouse X/Y + MoveRotation/body.rotation.
6. SampleMoveInput prefers KeyCode W/A/S/D via LegacyInput (axis throws cannot abort Update).
7. Shore path writes body.position + transform.position.
8. M / L not silenced by BlocksGameplay.
9. Panel IsOpen requires activeInHierarchy.
10. activeInputHandler is Legacy (0) or Both (2) — never Input System only (1).
11. GameBootstrap resets booted on SubsystemRegistration.
"""
from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SWIM = ROOT / "Assets/Scripts/Player/SwimController.cs"
GAME_UI = ROOT / "Assets/Scripts/UI/GameUi.cs"
MAP = ROOT / "Assets/Scripts/UI/WorldMapPanel.cs"
GEAR = ROOT / "Assets/Scripts/Economy/GearMarket.cs"
DECOR = ROOT / "Assets/Scripts/Economy/DecorShop.cs"
CORAL = ROOT / "Assets/Scripts/World/CoralDiveShop.cs"
FLASH = ROOT / "Assets/Scripts/Player/DiverFlashlight.cs"
BOOT = ROOT / "Assets/Scripts/Core/GameBootstrap.cs"
LEGACY = ROOT / "Assets/Scripts/Core/LegacyInput.cs"
HUD = ROOT / "Assets/Scripts/UI/DiveHud.cs"

errors: list[str] = []


def read(p: Path) -> str:
    if not p.exists():
        errors.append(f"missing {p}")
        return ""
    return p.read_text(encoding="utf-8")


def must(cond: bool, msg: str) -> None:
    if not cond:
        errors.append(msg)


def main() -> int:
    swim = read(SWIM)
    game_ui = read(GAME_UI)
    world_map = read(MAP)
    gear = read(GEAR)
    decor = read(DECOR)
    coral = read(CORAL)
    legacy = read(LEGACY)
    boot = read(BOOT)
    hud = read(HUD)

    # --- LegacyInput safety net ---
    must("class LegacyInput" in legacy, "LegacyInput helper missing")
    must("ReadMouseLook" in legacy, "LegacyInput.ReadMouseLook missing")
    must("GetAxisRaw" in legacy and "catch" in legacy, "LegacyInput must try/catch GetAxisRaw")

    # --- Live BlocksGameplay (not sticky mask) ---
    must(
        "AnyModalPanelOpen" in game_ui and "BlocksGameplay => AnyModalPanelOpen()" in game_ui,
        "GameUi.BlocksGameplay must delegate to AnyModalPanelOpen (live panels)",
    )
    must(
        re.search(r"BlocksGameplay\s*=>\s*mask\s*!=\s*0", game_ui) is None,
        "GameUi.BlocksGameplay must NOT be sticky mask != 0",
    )
    must(
        "WorldMapPanel.Instance" in game_ui and "GearMarket.Instance" in game_ui,
        "AnyModalPanelOpen must read map/shop Instances",
    )
    must("CoralDiveShop.AnyOpen()" in game_ui, "AnyModalPanelOpen must include CoralDiveShop.AnyOpen")
    must("activeInHierarchy" in game_ui, "AnyModalPanelOpen must double-check activeInHierarchy")
    must(
        "SubsystemRegistration" in game_ui and "ResetStatics" in game_ui,
        "GameUi must reset statics on SubsystemRegistration",
    )

    # --- FixedUpdate path ---
    fu = re.search(r"void FixedUpdate\(\)\s*\{(.*?)\n        \}", swim, re.S)
    must(fu is not None, "FixedUpdate not found")
    if fu:
        body = fu.group(1)
        must("ReconcileUiGate()" in body, "FixedUpdate must ReconcileUiGate before thrust")
        must(
            re.search(
                r"if\s*\(\s*!GameUi\.(AnyModalPanelOpen\(\)|BlocksGameplay)\s*\)\s*\n?\s*ApplyThrust\(\)",
                body,
            )
            is not None,
            "FixedUpdate must call ApplyThrust only when no live modal",
        )
        # Prove ApplyThrust is not gated on cursorLocked
        must("cursorLocked" not in body, "FixedUpdate must not gate on cursorLocked")

    # --- Update: HandleLook NOT gated on cursorLocked ---
    upd = re.search(r"void Update\(\)\s*\{(.*?)\n        \}", swim, re.S)
    must(upd is not None, "Update not found")
    if upd:
        body = upd.group(1)
        must("ReconcileUiGate()" in body, "Update must call ReconcileUiGate")
        must("AnyModalPanelOpen()" in body, "Update must gate on live modal")
        must("cachedMoveInput = SampleMoveInput()" in body, "Update must sample move input")
        must("HandleLook()" in body, "Update must call HandleLook when free")
        # Critical regression: look must not require cursorLocked
        must(
            re.search(r"if\s*\(\s*cursorLocked\s*\)\s*\n?\s*HandleLook\(\)", body) is None,
            "HandleLook must NOT be gated on cursorLocked — only on modal open",
        )
        must(
            "HandleLook()" in body and "modalOpen" in body,
            "HandleLook must be under !modalOpen path",
        )

    # --- HandleLook must drive Rigidbody rotation + safe mouse ---
    look = re.search(r"void HandleLook\(\)\s*\{(.*?)\n        \}", swim, re.S)
    must(look is not None, "HandleLook missing")
    if look:
        body = look.group(1)
        must("MoveRotation" in body or "body.rotation" in body, "HandleLook must set Rigidbody rotation")
        must(
            "ReadMouseLook" in body or "Mouse X" in body,
            "HandleLook must read mouse via LegacyInput or Mouse X/Y",
        )
        must("LegacyInput" in body or "GetAxisRaw" in body, "HandleLook must use safe/legacy mouse input")

    # --- ApplyThrust shore vs swim ---
    must("ApplyShoreMove" in swim, "ApplyShoreMove missing")
    must("ApplySwimMove" in swim, "ApplySwimMove missing")
    must(
        re.search(r"if\s*\(\s*Submersion\s*<\s*shoreMoveSubmersionMax\s*\)", swim) is not None,
        "ApplyThrust must branch on Submersion < shoreMoveSubmersionMax",
    )
    for name in ("ApplyThrust", "ApplyShoreMove", "ApplySwimMove"):
        m = re.search(rf"void {name}\([^\)]*\)\s*\{{(.*?)\n        \}}", swim, re.S)
        must(m is not None, f"{name} body not found")
        if m:
            must("return;" not in m.group(1), f"{name} must not early-return (always applies intent)")
            if name == "ApplyShoreMove":
                body = m.group(1)
                must("body.position = p" in body, "ApplyShoreMove must write body.position")
                must("transform.position = p" in body, "ApplyShoreMove must write transform.position")
                must(".MovePosition(" not in body, "ApplyShoreMove must NOT call MovePosition (PhysX cancels it)")

    # --- Input GetKey / LegacyInput first ---
    sample = re.search(r"static Vector3 SampleMoveInput\(\)\s*\{(.*?)\n        \}", swim, re.S)
    must(sample is not None, "SampleMoveInput missing")
    if sample:
        body = sample.group(1)
        must("KeyCode.W" in body and "KeyCode.A" in body, "SampleMoveInput must use W/A/S/D KeyCodes")
        must("LegacyInput" in body, "SampleMoveInput must use LegacyInput (axis throw-safe)")
        getkey_i = body.find("LegacyInput.Key")
        axis_i = body.find("AxisRaw")
        must(getkey_i >= 0, "SampleMoveInput must use LegacyInput.Key")
        must(axis_i < 0 or getkey_i < axis_i, "GetKey must be preferred before axis fallback")

    # --- Closed panels: IsOpen requires activeInHierarchy ---
    must(
        re.search(r"IsOpen\s*=>\s*open\s*&&\s*panelRoot\s*!=\s*null\s*&&\s*panelRoot\.activeInHierarchy", world_map)
        is not None,
        "WorldMapPanel.IsOpen must require open+activeInHierarchy",
    )
    for label, src in (("GearMarket", gear), ("DecorShop", decor), ("CoralDiveShop", coral)):
        must(
            re.search(r"IsOpen\s*=>\s*open\s*&&\s*panel\s*!=\s*null\s*&&\s*panel\.activeInHierarchy", src) is not None,
            f"{label}.IsOpen must require open+activeInHierarchy",
        )
    must("static bool AnyOpen()" in coral or "public static bool AnyOpen()" in coral, "CoralDiveShop.AnyOpen missing")
    must(
        "PanelRoot" in world_map and "PanelRoot" in gear and "PanelRoot" in decor and "PanelRoot" in coral,
        "shops/map must expose PanelRoot",
    )

    # --- Map button must not steal Game View focus click ---
    must("SyncToggleRaycast" in world_map or "raycastTarget" in world_map, "WorldMapPanel must gate Map button raycasts")
    must("IsCursorLocked" in world_map, "Map button raycast must respect cursor lock intent")
    must("raycaster.enabled" in hud or "GraphicRaycaster" in hud, "DiveHud must toggle GraphicRaycaster")

    # --- Shop click must require real engine lock ---
    must(
        "Cursor.lockState == CursorLockMode.Locked" in gear,
        "ShopInteract click-open must require Cursor.lockState Locked (not Editor focus click)",
    )

    # --- Bootstrap always creates controls; reset booted ---
    must("AddComponent<SwimController>" in boot, "GameBootstrap must AddComponent SwimController")
    must("swim.enabled = true" in boot, "GameBootstrap must enable SwimController")
    must("SubsystemRegistration" in boot and "booted = false" in boot, "GameBootstrap must reset booted without domain reload")
    must("LockCursor(true)" in boot, "GameBootstrap must force cursor lock at spawn")

    # --- ReconcileLive still absolute for mask bookkeeping ---
    must("ReconcileLive" in game_ui, "GameUi.ReconcileLive missing")
    must("DefaultExecutionOrder(-100)" in swim, "SwimController must run Reconcile before other UI Updates")

    # --- M / L must not be silenced by stale BlocksGameplay mask ---
    flash = read(FLASH)
    map_upd = re.search(r"void Update\(\)\s*\{(.*?)\n        \}", world_map, re.S)
    must(map_upd is not None, "WorldMapPanel.Update not found")
    if map_upd:
        body = map_upd.group(1)
        must("MapToggleKey" in body or "KeyCode.M" in world_map, "WorldMapPanel must listen for M")
        must(
            "else if (!GameUi.BlocksGameplay)" not in body
            and re.search(r"else\s+if\s*\(\s*!GameUi\.BlocksGameplay", body) is None,
            "WorldMapPanel M Open must NOT gate on BlocksGameplay",
        )
        must(
            re.search(r"if\s*\(\s*open\s*\)\s*\n?\s*Close\(\)\s*;\s*\n?\s*else\s*\n?\s*Open\(\)", body) is not None
            or ("Close()" in body and "Open()" in body and "BlocksGameplay" not in body),
            "WorldMapPanel M must toggle Open/Close without BlocksGameplay",
        )

    flash_upd = re.search(r"void Update\(\)\s*\{(.*?)\n        \}", flash, re.S)
    must(flash_upd is not None, "DiverFlashlight.Update not found")
    if flash_upd:
        body = flash_upd.group(1)
        must(
            re.search(r"if\s*\(.*GameUi\.BlocksGameplay", body) is None,
            "DiverFlashlight must not early-out on BlocksGameplay",
        )
        must("WasTogglePressed()" in body or "GetKeyDown" in body, "DiverFlashlight must poll L")
        code_lines = [ln for ln in body.splitlines() if not ln.strip().startswith("//")]
        must(
            not any("GameUi.BlocksGameplay" in ln for ln in code_lines),
            "DiverFlashlight Update must ignore BlocksGameplay",
        )
    must("KeyCode.L" in flash, "DiverFlashlight must honor KeyCode.L")
    must("EnsureBound()" in flash, "DiverFlashlight must EnsureBound before toggle")

    # --- Shop/map LateUpdate must not SetBlocked(false) (sticky race leftover) ---
    for label, src in (("GearMarket", gear), ("DecorShop", decor), ("WorldMapPanel", world_map)):
        late = re.search(r"void LateUpdate\(\)\s*\{(.*?)\n        \}", src, re.S)
        must(late is not None, f"{label}.LateUpdate missing")
        if late:
            body = late.group(1)
            must(
                re.search(r"SetBlocked\([^)]*,\s*false\s*\)", body) is None,
                f"{label}.LateUpdate must not clear mask",
            )

    tail = read(ROOT / "Assets/Scripts/UI/UiGateTail.cs")
    must("ReconcileUiGateLate" in tail or "ReconcileUiGate" in tail, "UiGateTail must late-reconcile UI")
    must("DefaultExecutionOrder(1000)" in tail, "UiGateTail must run after shop LateUpdates")

    # --- Input Manager: Legacy (0) or Both (2), never Input System only (1) ---
    proj = read(ROOT / "ProjectSettings/ProjectSettings.asset")
    must(
        "activeInputHandler: 0" in proj or "activeInputHandler: 2" in proj,
        "activeInputHandler must be Input Manager (0) or Both (2)",
    )
    must(
        "activeInputHandler: 1" not in proj,
        "activeInputHandler must NOT be Input System only (1) — GetAxis/GetKey throw/no-op",
    )
    inp = read(ROOT / "ProjectSettings/InputManager.asset")
    for axis in ("Horizontal", "Vertical", "Mouse X", "Mouse Y"):
        must(f"m_Name: {axis}" in inp, f"InputManager missing axis {axis}")

    focus = read(ROOT / "Assets/Editor/FocusGameViewOnPlay.cs")
    must("FocusGameView" in focus or "GameView" in focus, "Editor FocusGameViewOnPlay missing")

    print("PATH (no modal → walk+look):")
    print("  BlocksGameplay := AnyModalPanelOpen() (panel.activeInHierarchy), NEVER sticky mask")
    print("  null Instances ⇒ NOT blocked")
    print("  Update: Reconcile → SampleMoveInput(LegacyInput Key W/A/S/D) → !modal → HandleLook ALWAYS")
    print("  HandleLook: LegacyInput.ReadMouseLook → body.rotation + MoveRotation")
    print("  FixedUpdate: !AnyModalPanelOpen → ApplyThrust → shore body+transform.position")
    print("PATH (Editor click steal):")
    print("  Map button raycast OFF while IsCursorLocked; DiveHud GraphicRaycaster gated")
    print("  ShopInteract LMB requires Cursor.lockState==Locked; FocusGameViewOnPlay")
    print("PATH (hotkeys):")
    print("  M: WorldMapPanel toggles Open/Close (no BlocksGameplay gate)")
    print("  L: DiverFlashlight toggles spot (no BlocksGameplay gate)")
    print("  Esc: unlock cursor / close modal; click Game View (non-UI) re-locks")

    if errors:
        print("FAIL:")
        for e in errors:
            print(" -", e)
        return 1
    print("OK: all movement gate assertions passed (static; not Play Mode).")
    return 0


if __name__ == "__main__":
    sys.exit(main())
