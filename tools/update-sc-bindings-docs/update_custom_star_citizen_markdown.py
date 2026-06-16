from __future__ import annotations

from pathlib import Path

from sc_bindings_core import (
    CUSTOM_JSON_PATH,
    DOCS_DIR,
    build_output_rows,
    ensure_required_files,
    read_json,
)


COLUMNS = ["id", "category", "description", "keyboard", "joystick", "joystickDesc"]
MARKDOWN_OUTPUT_PATH = DOCS_DIR / "starcitizen" / "bindings.md"


def write_markdown(rows: list[dict[str, str]], output_path: Path = MARKDOWN_OUTPUT_PATH) -> None:
    def md_escape(value: str) -> str:
        return value.replace("|", r"\|").replace("\n", " ").replace("\r", " ")

    lines = ["# Star Citizen Bindings", "", "## GENERATED BINDINGS", ""]
    lines.append("|" + "|".join(COLUMNS) + "|")
    lines.append("|" + "|".join(["---"] * len(COLUMNS)) + "|")
    for row in rows:
        lines.append("|" + "|".join(md_escape(row[column]) for column in COLUMNS) + "|")
    output_path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def main() -> None:
    ensure_required_files([CUSTOM_JSON_PATH])
    actions = read_json(CUSTOM_JSON_PATH)
    markdown_rows = build_output_rows(actions, skip_missing_category_or_description=True)
    write_markdown(markdown_rows, MARKDOWN_OUTPUT_PATH)
    print(f"Wrote {len(markdown_rows)} markdown rows to {MARKDOWN_OUTPUT_PATH}")


if __name__ == "__main__":
    main()
