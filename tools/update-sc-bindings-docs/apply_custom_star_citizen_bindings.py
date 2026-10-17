"""Fill joystickDesc on bindings.json from docs/joystick-bindings.md.

1. Read joystick Product strings from actionmaps.xml and map each one to jsN.
2. Read the YAML bindings block. Prefix jsN_ onto each star_citizen value
   using that joystick's star_citizen_device_id, and index the descriptive
   name by the full input.
3. For each action, strip a trailing ' Nx' from the joystick input and look
   that input up. Unknown or unbound inputs become blank.
4. Rewrite bindings.json, keeping the existing generatedAt.
"""
from __future__ import annotations

import re
import xml.etree.ElementTree as ET
from pathlib import Path

from sc_bindings_core import (
    ACTIONMAPS_PATH,
    CUSTOM_JSON_PATH,
    JOYSTICK_BINDINGS_PATH,
    ensure_required_files,
    read_binding_document,
    write_binding_document,
)


def load_joystick_instances(actionmaps_path: Path) -> dict[str, str]:
    instances: dict[str, str] = {}
    for options in ET.parse(actionmaps_path).getroot().findall(".//options"):
        if options.attrib.get("type") != "joystick":
            continue
        product = options.attrib.get("Product", "")
        instance = options.attrib.get("instance", "")
        if not product or not instance:
            continue
        joystick_id = f"js{instance}"
        previous = instances.get(product)
        if previous and previous != joystick_id:
            raise ValueError(f"Product {product!r} is both {previous} and {joystick_id}")
        instances[product] = joystick_id
    if not instances:
        raise ValueError(f"No joystick products in {actionmaps_path}")
    return instances


def load_star_citizen_names(text: str, joystick_instances: dict[str, str]) -> dict[str, str]:
    match = re.search(r"^```yaml\s*\n(.*?)^```\s*$", text, re.M | re.S)
    if not match:
        raise ValueError(f"No yaml bindings block in {JOYSTICK_BINDINGS_PATH}")

    names: dict[str, str] = {}
    current_name: str | None = None
    current_joystick_id: str | None = None
    for line in match.group(1).splitlines():
        if re.match(r"^[A-Za-z0-9-]+:\s*$", line):
            current_name = None
            current_joystick_id = None
            continue
        device = re.match(r'^  star_citizen_device_id: "(.*)"\s*$', line)
        if device:
            product = device.group(1)
            current_joystick_id = joystick_instances.get(product)
            if not current_joystick_id:
                raise ValueError(f"No joystick instance for Star Citizen device {product!r}")
            continue
        binding = re.match(r'^    "([^"]+)":\s*$', line)
        if binding:
            current_name = binding.group(1)
            continue
        star_citizen = re.match(r"^      star_citizen: (\S+)\s*$", line)
        if not star_citizen:
            continue
        if not current_name or not current_joystick_id:
            raise ValueError(f"star_citizen without a joystick or button name: {line}")
        input_name = f"{current_joystick_id}_{star_citizen.group(1)}"
        previous = names.get(input_name)
        if previous and previous != current_name:
            raise ValueError(
                f"Duplicate Star Citizen input {input_name}: {previous} and {current_name}"
            )
        names[input_name] = current_name
        current_name = None

    if not names:
        raise ValueError(f"No bindings parsed from {JOYSTICK_BINDINGS_PATH}")
    return names


def apply_joystick_descriptions(actions: list[dict[str, object]]) -> list[dict[str, object]]:
    star_citizen_names = load_star_citizen_names(
        JOYSTICK_BINDINGS_PATH.read_text(encoding="utf-8"),
        load_joystick_instances(ACTIONMAPS_PATH),
    )

    def map_joystick_input_to_binding_name(input_value: str) -> str:
        base_input_value = re.sub(r"\s+\d+x$", "", input_value)
        return star_citizen_names.get(base_input_value, "")

    for row in actions:
        joystick_input = row.get("joystick", "")
        if not isinstance(joystick_input, str) or not joystick_input:
            row["joystickDesc"] = ""
            continue

        mapped = map_joystick_input_to_binding_name(joystick_input)
        row["joystickDesc"] = mapped

    return actions


def main() -> None:
    ensure_required_files([CUSTOM_JSON_PATH, JOYSTICK_BINDINGS_PATH, ACTIONMAPS_PATH])
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
