"""Randomly pick 4 Hololive generations and 1 bonus member.

Gen1 and Gamers cannot appear in the same round because Fubuki is in both.
Press any key to shuffle; Esc or Ctrl+C to exit.
"""

from __future__ import annotations

import json
import msvcrt
import os
import random
import sys
from pathlib import Path

PICK_COUNT = 4
MAX_GENS_PER_SIZE = 2
SEPARATOR = "=" * 31
ESC = b"\x1b"
CTRL_C = b"\x03"


def data_path() -> Path:
    if getattr(sys, "frozen", False):
        return Path(sys._MEIPASS) / "members.json"
    return Path(__file__).with_name("members.json")


def load_data() -> dict:
    with data_path().open(encoding="utf-8") as f:
        return json.load(f)


def pick_generations(
    generations: dict[str, list[str]],
    exclusive_pairs: list[list[str]],
    count: int = PICK_COUNT,
) -> list[str]:
    remaining = list(generations)
    picked: list[str] = []
    size_counts: dict[int, int] = {}

    while remaining and len(picked) < count:
        choice = random.choice(remaining)
        remaining.remove(choice)
        picked.append(choice)

        size = len(generations[choice])
        size_counts[size] = size_counts.get(size, 0) + 1
        if size != 4 and size_counts[size] >= MAX_GENS_PER_SIZE:
            remaining = [name for name in remaining if len(generations[name]) != size]

        blocked = set()
        for left, right in exclusive_pairs:
            if choice == left:
                blocked.add(right)
            elif choice == right:
                blocked.add(left)

        remaining = [name for name in remaining if name not in blocked]

    if len(picked) < count:
        raise RuntimeError(
            f"Could only pick {len(picked)} generations after exclusive-pair and size filters."
        )

    return picked


def format_members(members: list[str]) -> str:
    return ", ".join(name.lower() for name in members)


def wait_for_key(action_prompt: str, exit_prompt: str) -> bool:
    print()
    print(action_prompt)
    print(exit_prompt)
    key = msvcrt.getch()
    if key in (b"\x00", b"\xe0"):
        msvcrt.getch()
        return True
    return key not in (ESC, CTRL_C)


def print_round(generations: dict[str, list[str]], exclusive_pairs: list[list[str]]) -> None:
    order = list(generations)
    picked_gens = sorted(
        pick_generations(generations, exclusive_pairs),
        key=lambda name: order.index(name) if name in order else len(order),
    )
    bonus_pool = [member for gen in picked_gens for member in generations[gen]]
    bonus_member = random.choice(bonus_pool)

    print(SEPARATOR)
    for gen in picked_gens:
        print(f"{gen}: {format_members(generations[gen])}")
    print(SEPARATOR)
    print(f"Bonus member: {bonus_member.lower()}")


def main() -> None:
    data = load_data()
    generations: dict[str, list[str]] = data["generations"]
    exclusive_pairs: list[list[str]] = data.get("exclusive_pairs", [])

    os.system("cls")
    print("Pokajan Randomizer")
    print()
    if not wait_for_key("Press any key to start shuffle", "Esc or Ctrl+C to exit"):
        return

    while True:
        os.system("cls")
        print_round(generations, exclusive_pairs)
        if not wait_for_key("Press any key for next shuffle", "Esc or Ctrl+C to exit"):
            return


if __name__ == "__main__":
    try:
        main()
    except Exception as exc:
        print(f"Error: {exc}")
        print()
        print("Press any key to close...")
        msvcrt.getch()
