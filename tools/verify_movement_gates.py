#!/usr/bin/env python3
"""
Static gate check for WASD / look (no Unity Play Mode).

Asserts:
1. GameUi.BlocksGameplay is LIVE panel state (AnyModalPanelOpen), not sticky mask.
2. SwimController Update/FixedUpdate gate move/look on AnyModalPanelOpen / live panels.
3. HandleLook writes Rigidbody.MoveRotation (not transform-only).
4. Shore path writes body.position + transform.position (MovePosition is cancelled by PhysX).
5. SampleMoveInput prefers GetKey over axes.
6. M map toggle must not gate Open on BlocksGameplay.
7. L flashlight must not early-out on BlocksGameplay.
8. Panel IsOpen requires activeInHierarchy.
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

    # --- Update: reconcile before early return; gate on live modal ---
    upd = re.search(r"void Update\(\)\s*\{(.*?)\n        \}", swim, re.S)
    must(upd is not None, "Update not found")
    if upd:
        body = upd.group(1)
        rec_i = body.find("ReconcileUiGate()")
        block_i = body.find("modalOpen")
        if block_i < 0:
            block_i = body.find("AnyModalPanelOpen()")
        must(rec_i >= 0, "Update must call ReconcileUiGate")
        must(block_i >= 0, "Update must gate on live modal (AnyModalPanelOpen)")
        must(rec_i < block_i, "ReconcileUiGate must run before modal early-return")
        must("cachedMoveInput = SampleMoveInput()" in body, "Update must sample move input")
        sample_i = body.find("SampleMoveInput()")
        must(sample_i >= 0 and sample_i < block_i, "SampleMoveInput must run even when map about to block look")
        must("HandleLook()" in body, "Update must call HandleLook when free")

    # --- HandleLook must drive Rigidbody rotation ---
    look = re.search(r"void HandleLook\(\)\s*\{(.*?)\n        \}", swim, re.S)
    must(look is not None, "HandleLook missing")
    if look:
        body = look.group(1)
        must("MoveRotation" in body, "HandleLook must MoveRotation on Rigidbody")
        must("Mouse X" in body and "Mouse Y" in body, "HandleLook must read Mouse X/Y")

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

    # --- Input GetKey first ---
    sample = re.search(r"static Vector3 SampleMoveInput\(\)\s*\{(.*?)\n        \}", swim, re.S)
    must(sample is not None, "SampleMoveInput missing")
    if sample:
        body = sample.group(1)
        getkey_i = body.find("Input.GetKey")
        axis_i = body.find("Input.GetAxisRaw")
        must(getkey_i >= 0, "SampleMoveInput must use GetKey")
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

    # --- Input Manager still legacy ---
    proj = read(ROOT / "ProjectSettings/ProjectSettings.asset")
    must("activeInputHandler: 0" in proj, "activeInputHandler must be Input Manager (0)")
    inp = read(ROOT / "ProjectSettings/InputManager.asset")
    for axis in ("Horizontal", "Vertical", "Mouse X", "Mouse Y"):
        must(f"m_Name: {axis}" in inp, f"InputManager missing axis {axis}")

    print("PATH (no modal → walk+look):")
    print("  BlocksGameplay := AnyModalPanelOpen() (panel.activeInHierarchy), NEVER sticky mask")
    print("  Update: ReconcileUiGate → SampleMoveInput(GetKey) → !modal → HandleLook(MoveRotation)")
    print("  FixedUpdate: !AnyModalPanelOpen → ApplyThrust → shore body+transform.position")
    print("PATH (hotkeys):")
    print("  M: WorldMapPanel toggles Open/Close (no BlocksGameplay gate)")
    print("  L: DiverFlashlight toggles spot (no BlocksGameplay gate)")

    if errors:
        print("FAIL:")
        for e in errors:
            print(" -", e)
        return 1
    print("OK: all movement gate assertions passed (static; not Play Mode).")
    return 0


if __name__ == "__main__":
    sys.exit(main())
