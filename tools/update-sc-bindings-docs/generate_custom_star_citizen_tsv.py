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
TSV_OUTPUT_PATH = DOCS_DIR / "starcitizen" / "bindings.tsv"


def write_tsv(rows: list[dict[str, str]], output_path: Path = TSV_OUTPUT_PATH) -> None:
    def tsv_escape(value: str) -> str:
        return value.replace("\t", " ").replace("\n", " ").replace("\r", " ")

    lines = ["\t".join(COLUMNS)]
    for row in rows:
        lines.append("\t".join(tsv_escape(row[column]) for column in COLUMNS))
    output_path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def main() -> None:
    ensure_required_files([CUSTOM_JSON_PATH])
    actions = read_json(CUSTOM_JSON_PATH)
    tsv_rows = build_output_rows(actions, skip_missing_category_or_description=False)
    write_tsv(tsv_rows, TSV_OUTPUT_PATH)
    print(f"Wrote {len(tsv_rows)} TSV rows to {TSV_OUTPUT_PATH}")


if __name__ == "__main__":
    main()
