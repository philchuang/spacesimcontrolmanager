from __future__ import annotations

import re

from sc_bindings_core import (
    CUSTOM_JSON_PATH,
    JOYSTICK_BINDINGS_PATH,
    ensure_required_files,
    read_binding_document,
    write_binding_document,
)


def dedupe(values: list[str]) -> list[str]:
    seen: set[str] = set()
    output: list[str] = []
    for value in values:
        if value in seen:
            continue
        seen.add(value)
        output.append(value)
    return output


def apply_joystick_descriptions(actions: list[dict[str, object]]) -> list[dict[str, object]]:
    binding_names: list[str] = []
    for line in JOYSTICK_BINDINGS_PATH.read_text(encoding="utf-8").splitlines():
        stripped = line.strip()
        if stripped.startswith("* "):
            binding_names.append(stripped[2:].strip())

    button_lookup: dict[tuple[str, int], str] = {}
    literal_lookup: dict[tuple[str, str], str] = {}
    entry_regex = re.compile(r"^(LT|RT)-[^.]+\.(.+)$")
    number_regex = re.compile(r"^(\d+)(?:\(|$)")
    for name in binding_names:
        match = entry_regex.match(name)
        if not match:
            continue
        hand = match.group(1)
        literal = match.group(2)
        literal_lookup.setdefault((hand, literal), name)
        number_match = number_regex.match(literal)
        if number_match:
            button_lookup.setdefault((hand, int(number_match.group(1))), name)

    def map_joystick_input_to_binding_name(input_value: str) -> str:
        base_input_value = re.sub(r"\s+\d+x$", "", input_value)
        match = re.match(r"^js(\d+)_(.+)$", base_input_value)
        if not match:
            return ""
        hand = {"1": "LT", "2": "RT"}.get(match.group(1), "")
        if not hand:
            return ""
        payload = match.group(2)
        button_match = re.match(r"^button(\d+)$", payload)
        if button_match:
            return button_lookup.get((hand, int(button_match.group(1))), "")
        if payload == "UNBOUND":
            return ""
        if payload in {"x", "y", "z", "rotz"}:
            axis_map = {"x": "X_Axis", "y": "Y_Axis", "z": "Z_Axis", "rotz": "Z_Rot"}
            return literal_lookup.get((hand, axis_map[payload]), "")
        hat_match = re.match(r"^hat(\d+)_(up|down|left|right)$", payload)
        if hat_match:
            direction_map = {"up": "UP", "down": "DN", "left": "LT", "right": "RT"}
            direction = direction_map[hat_match.group(2)]
            return literal_lookup.get((hand, f"POV1(A1_{direction})"), "")
        return ""

    for row in actions:
        joystick_input = row.get("joystick", "")
        if not isinstance(joystick_input, str) or not joystick_input:
            row["joystickDesc"] = ""
            continue

        mapped = map_joystick_input_to_binding_name(joystick_input)
        row["joystickDesc"] = mapped

    return actions


def main() -> None:
    ensure_required_files([CUSTOM_JSON_PATH, JOYSTICK_BINDINGS_PATH])
    document = read_binding_document(CUSTOM_JSON_PATH)
    actions = document["actions"]
    if not isinstance(actions, list):
        raise ValueError(f"Expected actions list in JSON file: {CUSTOM_JSON_PATH}")
    actions = apply_joystick_descriptions(actions)
    generated_at = str(document.get("generatedAt", ""))
    write_binding_document(actions, CUSTOM_JSON_PATH, generated_at=generated_at)
    print(f"Wrote {len(actions)} custom actions to {CUSTOM_JSON_PATH}")


if __name__ == "__main__":
    main()
