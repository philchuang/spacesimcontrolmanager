from __future__ import annotations

import xml.etree.ElementTree as ET
from pathlib import Path

from sc_bindings_core import (
    CUSTOM_JSON_PATH,
    REPO_ROOT,
    ensure_required_files,
    write_binding_document,
)


DEFAULT_PROFILE_PATH = REPO_ROOT / "src" / "SSCM.StarCitizen" / "defaultProfile.xml"
GLOBAL_INI_PATH = Path(
    r"C:\Program Files\Roberts Space Industries\StarCitizen\HOTFIX\data\Localization\english\global.ini"
)
ACTIONMAPS_PATH = Path(
    r"C:\Program Files\Roberts Space Industries\StarCitizen\HOTFIX\USER\Client\0\Profiles\default\actionmaps.xml"
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


def normalize_input_value(input_value: str) -> str:
    if input_value.endswith(" "):
        return "{UNBOUND}"
    return input_value


def apply_multi_tap_suffix(input_value: str, multi_tap: str) -> str:
    if not input_value or not multi_tap.strip() or input_value == "{UNBOUND}":
        return input_value
    return f"{input_value} {multi_tap.strip()}x"


def copy_default_binding(value: object) -> str:
    if not isinstance(value, str):
        return ""
    normalized = "UNBOUND" if value.endswith(" ") else value.strip()
    return f"({normalized})" if normalized else ""


def normalize_actions() -> list[dict[str, object]]:
    actions: dict[str, dict[str, object]] = {}
    default_profile_root = ET.parse(DEFAULT_PROFILE_PATH).getroot()

    for actionmap in default_profile_root.findall(".//actionmap"):
        category_name = actionmap.attrib.get("name", "")
        category_label = actionmap.attrib.get("UILabel", "")
        for action in actionmap.findall("./action"):
            action_name = action.attrib.get("name", "")
            action_label = action.attrib.get("UILabel", "")
            raw_default_keyboard = action.attrib.get("keyboard", "")
            default_keyboard_source = "keyboard"
            if not raw_default_keyboard.strip():
                default_keyboard = action.attrib.get("mouse", "")
                default_keyboard_source = "mouse" if default_keyboard else "keyboard"
            else:
                default_keyboard = raw_default_keyboard
            default_joystick = action.attrib.get("joystick", "")
            action_id = f"{category_name}-{action_name}"
            actions[action_id] = {
                "id": action_id,
                "categoryName": category_name,
                "categoryLabel": category_label,
                "name": action_name,
                "label": action_label,
                "defaultKeyboard": default_keyboard,
                "defaultKeyboardSource": default_keyboard_source,
                "defaultJoystick": default_joystick,
                "keyboard": [],
                "joystick": [],
            }

    global_map: dict[str, str] = {}
    with GLOBAL_INI_PATH.open("r", encoding="utf-8", errors="replace") as stream:
        for raw_line in stream:
            line = raw_line.rstrip("\n\r")
            if not line or line.lstrip().startswith(";") or "=" not in line:
                continue
            key, value = line.split("=", 1)
            global_map[key] = value

    def localize(value: str) -> str:
        if not value:
            return value
        if value in global_map:
            return global_map[value]
        if value.startswith("@") and value[1:] in global_map:
            return global_map[value[1:]]
        return value

    for row in actions.values():
        row["label"] = localize(str(row.get("label", "")))
        row["categoryLabel"] = localize(str(row.get("categoryLabel", "")))

    actionmaps_root = ET.parse(ACTIONMAPS_PATH).getroot()
    for actionmap in actionmaps_root.findall(".//actionmap"):
        category_name = actionmap.attrib.get("name", "")
        for action in actionmap.findall("./action"):
            action_name = action.attrib.get("name", "")
            action_id = f"{category_name}-{action_name}"
            row = actions.get(action_id)
            if row is None:
                continue

            keyboard = row.get("keyboard", [])
            joystick = row.get("joystick", [])
            if not isinstance(keyboard, list) or not isinstance(joystick, list):
                continue

            for rebind in action.findall("./rebind"):
                raw_input_value = rebind.attrib.get("input", "")
                input_value = normalize_input_value(raw_input_value)
                input_value = apply_multi_tap_suffix(input_value, rebind.attrib.get("multiTap", ""))
                if raw_input_value.startswith("kb"):
                    keyboard.append(input_value)
                elif raw_input_value.startswith("js"):
                    joystick.append(input_value)

    for row in actions.values():
        keyboard = row.get("keyboard", [])
        joystick = row.get("joystick", [])
        keyboard_values = dedupe(keyboard) if isinstance(keyboard, list) else []
        joystick_values = dedupe(joystick) if isinstance(joystick, list) else []

        if not keyboard_values:
            keyboard_value = copy_default_binding(row.get("defaultKeyboard", ""))
        else:
            keyboard_value = keyboard_values[0]

        if not joystick_values:
            joystick_value = copy_default_binding(row.get("defaultJoystick", ""))
        else:
            joystick_value = joystick_values[0]

        row["keyboard"] = keyboard_value
        row["joystick"] = joystick_value

    return sorted(actions.values(), key=lambda row: str(row.get("id", "")))


def main() -> None:
    ensure_required_files([DEFAULT_PROFILE_PATH, GLOBAL_INI_PATH, ACTIONMAPS_PATH])
    actions = normalize_actions()
    write_binding_document(actions, CUSTOM_JSON_PATH)
    print(f"Wrote {len(actions)} base actions to {CUSTOM_JSON_PATH}")


if __name__ == "__main__":
    main()
