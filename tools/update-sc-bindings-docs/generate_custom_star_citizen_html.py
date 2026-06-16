from __future__ import annotations

import json
from pathlib import Path

from sc_bindings_core import (
    CUSTOM_JSON_PATH,
    DOCS_DIR,
    SCRIPT_DIR,
    ensure_required_files,
    read_binding_document,
)


HTML_OUTPUT_PATH = DOCS_DIR / "starcitizen" / "bindings.html"
HTML_TEMPLATE_PATH = SCRIPT_DIR / "star-citizen-bindings.template.html"
BINDINGS_JSON_PLACEHOLDER = "__BINDINGS_JSON__"


def build_embedded_document(document: dict[str, object]) -> dict[str, object]:
    actions = document.get("actions", [])
    if not isinstance(actions, list):
        raise ValueError(f"Expected actions list in JSON file: {CUSTOM_JSON_PATH}")

    return {
        "generatedAt": str(document.get("generatedAt", "")),
        "actions": [
            {
                "id": action.get("id", ""),
                "categoryLabel": action.get("categoryLabel", ""),
                "label": action.get("label", ""),
                "keyboard": action.get("keyboard", ""),
                "joystick": action.get("joystick", ""),
                "joystickDesc": action.get("joystickDesc", ""),
            }
            for action in actions
            if isinstance(action, dict)
        ],
    }


def write_interactive_html(
    document: dict[str, object],
    output_path: Path = HTML_OUTPUT_PATH,
) -> None:
    ensure_required_files([HTML_TEMPLATE_PATH])
    data_json = json.dumps(build_embedded_document(document), ensure_ascii=False).replace("</", "<\\/")
    html_template = HTML_TEMPLATE_PATH.read_text(encoding="utf-8")

    if BINDINGS_JSON_PLACEHOLDER not in html_template:
        raise ValueError(f"Missing {BINDINGS_JSON_PLACEHOLDER} placeholder in {HTML_TEMPLATE_PATH}")

    output_path.write_text(
        html_template.replace(BINDINGS_JSON_PLACEHOLDER, data_json),
        encoding="utf-8",
    )


def main() -> None:
    ensure_required_files([CUSTOM_JSON_PATH])
    document = read_binding_document(CUSTOM_JSON_PATH)
    actions = document["actions"]
    if not isinstance(actions, list):
        raise ValueError(f"Expected actions list in JSON file: {CUSTOM_JSON_PATH}")
    write_interactive_html(document, HTML_OUTPUT_PATH)
    print(f"Wrote {len(actions)} HTML actions to {HTML_OUTPUT_PATH}")


if __name__ == "__main__":
    main()
