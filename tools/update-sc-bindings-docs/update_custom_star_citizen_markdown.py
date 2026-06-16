from __future__ import annotations

from pathlib import Path

from sc_bindings_core import (
    CUSTOM_JSON_PATH,
    MARKDOWN_OUTPUT_PATH,
    build_output_rows,
    ensure_required_files,
    read_json,
    write_text,
)


COLUMNS = ["id", "category", "description", "keyboard", "joystick", "joystickDesc"]


def write_markdown(rows: list[dict[str, str]], output_path: Path = MARKDOWN_OUTPUT_PATH) -> None:
    def md_escape(value: str) -> str:
        return value.replace("|", r"\|").replace("\n", " ").replace("\r", " ")

    lines = ["# Star Citizen Bindings", "", "## GENERATED BINDINGS", ""]
    lines.append("|" + "|".join(COLUMNS) + "|")
    lines.append("|" + "|".join(["---"] * len(COLUMNS)) + "|")
    for row in rows:
        lines.append("|" + "|".join(md_escape(row[column]) for column in COLUMNS) + "|")
    write_text(output_path, "\n".join(lines) + "\n")


def main() -> None:
    ensure_required_files([CUSTOM_JSON_PATH])
    actions = read_json(CUSTOM_JSON_PATH)
    markdown_rows = build_output_rows(actions, skip_missing_category_or_description=True)
    write_markdown(markdown_rows, MARKDOWN_OUTPUT_PATH)
    print(f"Wrote {len(markdown_rows)} markdown rows to {MARKDOWN_OUTPUT_PATH}")


if __name__ == "__main__":
    main()
