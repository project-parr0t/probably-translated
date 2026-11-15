from __future__ import annotations

import json
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[1]
FONT = ROOT / "va11-recipes" / "fonts" / "Galmuri9.ttf"
OUTPUT = ROOT / "loading-ui"
FONT_SIZE = 26
PADDING = 2
ATLAS_WIDTH = 1024
CHARACTERS = "".join(chr(code) for code in range(32, 127)) + "·한국어패치적용중진행"


def main() -> None:
    font = ImageFont.truetype(str(FONT), FONT_SIZE)
    entries: dict[str, list[int]] = {}
    measured: list[tuple[str, tuple[int, int, int, int], int]] = []
    for character in dict.fromkeys(CHARACTERS):
        bbox = font.getbbox(character)
        advance = max(1, round(font.getlength(character)))
        measured.append((character, bbox, advance))

    x = PADDING
    y = PADDING
    row_height = 0
    placements: list[tuple[str, int, int, tuple[int, int, int, int], int]] = []
    for character, bbox, advance in measured:
        width = max(1, bbox[2] - bbox[0])
        height = max(1, bbox[3] - bbox[1])
        if x + width + PADDING > ATLAS_WIDTH:
            x = PADDING
            y += row_height + PADDING
            row_height = 0
        placements.append((character, x, y, bbox, advance))
        x += width + PADDING
        row_height = max(row_height, height)

    atlas_height = y + row_height + PADDING
    atlas = Image.new("RGBA", (ATLAS_WIDTH, atlas_height), (0, 0, 0, 0))
    draw = ImageDraw.Draw(atlas)
    for character, glyph_x, glyph_y, bbox, advance in placements:
        width = max(1, bbox[2] - bbox[0])
        height = max(1, bbox[3] - bbox[1])
        draw.text((glyph_x - bbox[0], glyph_y - bbox[1]), character, font=font, fill=(255, 255, 255, 255))
        entries[character] = [glyph_x, glyph_y, width, height, advance, bbox[0], bbox[1]]

    OUTPUT.mkdir(parents=True, exist_ok=True)
    atlas.save(OUTPUT / "galmuri9-atlas.png", optimize=True)
    payload = {"schema": "probably-translated.bitmap-font/v1", "line_height": FONT_SIZE + 6, "glyphs": entries}
    (OUTPUT / "galmuri9-atlas.json").write_text(
        json.dumps(payload, ensure_ascii=False, separators=(",", ":")) + "\n",
        encoding="utf-8",
    )
    print(f"glyphs={len(entries)} atlas={ATLAS_WIDTH}x{atlas_height}")


if __name__ == "__main__":
    main()
