#!/usr/bin/env python3
"""
Static gate check for WASD / ApplyThrust (no Unity Play Mode).

Asserts:
1. FixedUpdate calls ApplyThrust only behind !GameUi.BlocksGameplay.
2. Update reconciles UI before the BlocksGameplay early-return.
3. Shore path uses Rigidbody.MovePosition (PhysX cannot cancel).
4. Closed map/shop IsOpen expressions cannot force BlocksGameplay true alone.
5. SampleMoveInput prefers GetKey over axes.
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

    # --- FixedUpdate path ---
    fu = re.search(r"void FixedUpdate\(\)\s*\{(.*?)\n        \}", swim, re.S)
    must(fu is not None, "FixedUpdate not found")
    if fu:
        body = fu.group(1)
        must("ReconcileUiGate()" in body, "FixedUpdate must ReconcileUiGate before thrust")
        must(
            re.search(r"if\s*\(\s*!GameUi\.BlocksGameplay\s*\)\s*\n?\s*ApplyThrust\(\)", body) is not None,
            "FixedUpdate must call ApplyThrust only when !BlocksGameplay",
        )
        must("ApplyThrust()" in body, "FixedUpdate must call ApplyThrust")

    # --- Update: reconcile before early return ---
    upd = re.search(r"void Update\(\)\s*\{(.*?)\n        \}", swim, re.S)
    must(upd is not None, "Update not found")
    if upd:
        body = upd.group(1)
        rec_i = body.find("ReconcileUiGate()")
        block_i = body.find("if (GameUi.BlocksGameplay)")
        must(rec_i >= 0, "Update must call ReconcileUiGate")
        must(block_i >= 0, "Update must gate on BlocksGameplay")
        must(rec_i < block_i, "ReconcileUiGate must run before BlocksGameplay early-return")
        must("cachedMoveInput = SampleMoveInput()" in body, "Update must sample move input")
        # Sample is allowed before or after reconcile; must exist before early-return uses smoothedMove
        sample_i = body.find("SampleMoveInput()")
        must(sample_i >= 0 and sample_i < block_i, "SampleMoveInput must run even when map about to block look")

    # --- ApplyThrust shore vs swim ---
    must("ApplyShoreMove" in swim, "ApplyShoreMove missing")
    must("ApplySwimMove" in swim, "ApplySwimMove missing")
    must("body.MovePosition" in swim, "Shore move must use Rigidbody.MovePosition")
    must(
        re.search(r"if\s*\(\s*Submersion\s*<\s*shoreMoveSubmersionMax\s*\)", swim) is not None,
        "ApplyThrust must branch on Submersion < shoreMoveSubmersionMax",
    )
    # No early-return inside ApplyThrust / ApplyShoreMove / ApplySwimMove
    for name in ("ApplyThrust", "ApplyShoreMove", "ApplySwimMove"):
        m = re.search(rf"void {name}\([^\)]*\)\s*\{{(.*?)\n        \}}", swim, re.S)
        must(m is not None, f"{name} body not found")
        if m:
            must("return;" not in m.group(1), f"{name} must not early-return (always applies intent)")

    # --- Input GetKey first ---
    sample = re.search(r"static Vector3 SampleMoveInput\(\)\s*\{(.*?)\n        \}", swim, re.S)
    must(sample is not None, "SampleMoveInput missing")
    if sample:
        body = sample.group(1)
        getkey_i = body.find("Input.GetKey")
        axis_i = body.find("Input.GetAxisRaw")
        must(getkey_i >= 0, "SampleMoveInput must use GetKey")
        must(axis_i < 0 or getkey_i < axis_i, "GetKey must be preferred before axis fallback")

    # --- Closed panels cannot assert block ---
    must("public bool IsOpen => open && panelRoot != null && panelRoot.activeSelf" in world_map
         or re.search(r"IsOpen\s*=>\s*open\s*&&\s*panelRoot", world_map) is not None,
         "WorldMapPanel.IsOpen must require open+active")
    for label, src in (("GearMarket", gear), ("DecorShop", decor), ("CoralDiveShop", coral)):
        must(
            re.search(r"IsOpen\s*=>\s*open\s*&&\s*panel", src) is not None,
            f"{label}.IsOpen must require open+active panel",
        )

    # --- ReconcileLive absolute mask ---
    must("ReconcileLive" in game_ui, "GameUi.ReconcileLive missing")
    must("mask = desired" in game_ui, "ReconcileLive must assign absolute mask")

    # --- Path summary when map closed ---
    print("PATH (map closed → walk):")
    print("  Update: ReconcileUiGate() → mask=0 → SampleMoveInput(GetKey)")
    print("  FixedUpdate: ReconcileUiGate() → !BlocksGameplay → ApplyThrust()")
    print("  ApplyThrust: Submersion < shoreMoveSubmersionMax → ApplyShoreMove → MovePosition(XZ)")
    print("  ApplyThrust: else → ApplySwimMove → AddForce")

    if errors:
        print("FAIL:")
        for e in errors:
            print(" -", e)
        return 1
    print("OK: all movement gate assertions passed (static; not Play Mode).")
    return 0


if __name__ == "__main__":
    sys.exit(main())
