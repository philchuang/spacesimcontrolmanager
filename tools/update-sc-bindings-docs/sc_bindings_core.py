from __future__ import annotations

import json
import os
from datetime import datetime
from pathlib import Path


SCRIPT_DIR = Path(__file__).resolve().parent
REPO_ROOT = SCRIPT_DIR.parent.parent
DOCS_DIR = REPO_ROOT / "docs"
STAR_CITIZEN_ENVIRONMENT = "LIVE"
STAR_CITIZEN_ROOT = Path(r"C:\Program Files\Roberts Space Industries\StarCitizen")
STAR_CITIZEN_ENV_DIR = STAR_CITIZEN_ROOT / STAR_CITIZEN_ENVIRONMENT
BINDINGS_OUTPUT_DIR = DOCS_DIR / "starcitizen" / "bindings"

DEFAULT_PROFILE_PATH = REPO_ROOT / "src" / "SSCM.StarCitizen" / "defaultProfile.xml"
GLOBAL_INI_PATH = STAR_CITIZEN_ENV_DIR / "data" / "Localization" / "english" / "global.ini"
ACTIONMAPS_PATH = STAR_CITIZEN_ENV_DIR / "USER" / "Client" / "0" / "Profiles" / "default" / "actionmaps.xml"
JOYSTICK_BINDINGS_PATH = DOCS_DIR / "joystick-bindings.md"
HTML_TEMPLATE_PATH = SCRIPT_DIR / "star-citizen-bindings.template.html"
CUSTOM_JSON_PATH = BINDINGS_OUTPUT_DIR / "bindings.json"
MARKDOWN_OUTPUT_PATH = BINDINGS_OUTPUT_DIR / "bindings.md"
TSV_OUTPUT_PATH = BINDINGS_OUTPUT_DIR / "bindings.tsv"
HTML_OUTPUT_PATH = BINDINGS_OUTPUT_DIR / "bindings.html"


def ensure_required_files(paths: list[Path]) -> None:
    for path in paths:
        if not path.exists():
            raise FileNotFoundError(f"Required file not found: {path}")


def build_output_rows(
    actions: list[dict[str, object]], *, skip_missing_category_or_description: bool
) -> list[dict[str, str]]:
    def display_binding(value: object) -> str:
        if isinstance(value, list):
            return "<br>".join(str(item) for item in value)
        return str(value or "")

    rows: list[dict[str, str]] = []
    sorted_actions = sorted(actions, key=lambda row: str(row.get("id", "")))

    for action in sorted_actions:
        output = {
            "id": str(action.get("id", "")),
            "category": str(action.get("categoryLabel", "")),
            "description": str(action.get("label", "")),
            "keyboard": display_binding(action.get("keyboard", "")),
            "joystick": display_binding(action.get("joystick", "")),
            "joystickDesc": display_binding(action.get("joystickDesc", "")),
        }

        if skip_missing_category_or_description and (
            not output["category"].strip() or not output["description"].strip()
        ):
            continue

        rows.append(output)

    return rows


def current_timestamp() -> str:
    return datetime.now().astimezone().isoformat(timespec="seconds")


def write_text(output_path: Path, content: str) -> None:
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(content, encoding="utf-8")


def write_binding_document(
    actions: list[dict[str, object]],
    output_path: Path,
    *,
    generated_at: str | None = None,
) -> None:
    payload = {
        "generatedAt": generated_at or current_timestamp(),
        "actions": actions,
    }
    write_text(output_path, json.dumps(payload, indent=2, ensure_ascii=False) + "\n")


def read_binding_document(path: Path) -> dict[str, object]:
    data = json.loads(path.read_text(encoding="utf-8"))
    if isinstance(data, list):
        return {
            "generatedAt": "",
            "actions": data,
        }
    if not isinstance(data, dict) or not isinstance(data.get("actions"), list):
        raise ValueError(f"Expected binding document in JSON file: {path}")
    return data


def write_json(actions: list[dict[str, object]], output_path: Path) -> None:
    write_binding_document(actions, output_path)


def read_json(path: Path) -> list[dict[str, object]]:
    return read_binding_document(path)["actions"]  # type: ignore[return-value]
