"""Run the full Star Citizen bindings-doc pipeline.

Calls the other scripts in order:

1. Normalize defaultProfile.xml, global.ini, and actionmaps.xml into bindings.json.
2. Fill joystickDesc from docs/joystick-bindings.md.
3. Write bindings.md, bindings.tsv, and bindings.html.
"""
from __future__ import annotations

from apply_custom_star_citizen_bindings import main as apply_custom_bindings
from generate_custom_star_citizen_html import main as generate_html
from generate_custom_star_citizen_tsv import main as generate_tsv
from normalize_star_citizen_bindings import main as normalize_bindings
from update_custom_star_citizen_markdown import main as update_markdown


def main() -> None:
    normalize_bindings()
    apply_custom_bindings()
    update_markdown()
    generate_tsv()
    generate_html()


if __name__ == "__main__":
    main()
