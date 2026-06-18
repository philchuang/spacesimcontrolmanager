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
