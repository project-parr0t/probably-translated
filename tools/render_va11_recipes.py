import csv
import os
import io
from pathlib import Path
from PIL import Image, ImageChops, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / "va11-recipes"
FONT = Path("C:/Windows/Fonts/malgun.ttf")
PANEL_FONT = ASSETS / "fonts/Galmuri9.ttf"
SCALE, WIDTH, HEIGHT = 4, 1260, 856
PANEL_SOURCE = Path(os.environ.get(
    "VA11HALLA_DLL",
    r"C:\Program Files (x86)\Steam\steamapps\workshop\content\4348910\3831732004\Va11HallA.dll",
))
PANEL_RESOURCE = "Va11HallA.Assets.BTC调酒台界面.png"
# Source: installed VA-11 Hall-A README.md §5 recipe catalog. Columns are
# canonical id, price, A/B/D/F/K amounts, age, ice, blended, and optional K.
RECIPES = [
    ("bad_touch",250,0,2,2,2,4,0,1,0,0),
    ("beer",200,1,2,1,2,4,0,0,0,0),
    ("bleeding_jane",200,0,1,3,3,0,0,0,1,0),
    ("bloom_light",230,4,0,1,2,3,1,1,0,0),
    ("blue_fairy",170,4,0,0,1,0,1,0,0,1),
    ("brandtini",250,6,0,3,0,1,1,0,0,0),
    ("cobalt_velvet",280,2,0,0,3,5,0,1,0,0),
    ("crevice_spike",140,0,0,2,4,0,0,0,1,1),
    ("fluffy_dream",170,3,0,3,0,0,1,0,0,1),
    ("fringe_weaver",260,1,0,0,0,9,1,0,0,0),
    ("frothy_water",150,1,1,1,1,0,1,0,0,0),
    ("grizzly_temple",220,3,3,3,0,1,0,0,1,0),
    ("gut_punch",80,0,5,0,1,0,1,0,0,1),
    ("marsblast",170,0,6,1,4,2,0,0,1,0),
    ("mercuryblast",250,1,1,3,3,2,0,1,1,0),
    # Corrected against runtime behavior: Moonblast requires one Flanergide.
    ("moonblast",180,6,0,1,1,2,0,1,1,0),
    ("piano_man",320,2,3,5,5,3,0,1,0,0),
    ("piano_woman",320,5,5,2,3,3,1,0,0,0),
    ("piledriver",160,0,3,0,3,4,0,0,0,0),
    ("sparkle_star",150,2,0,1,0,0,1,0,0,1),
    ("sugar_rush",150,2,0,1,0,0,0,0,0,1),
    ("sunshine_cloud",150,2,2,0,0,0,0,1,1,1),
    ("suplex",160,0,4,0,3,3,1,1,0,0),
    ("zen_star",210,4,4,4,4,4,0,1,1,0),
]
NAME_KEYS = ["A","B","D","F","K"]
INGREDIENTS = ["아델하이드", "브론순 추출액", "델타 파우더", "플래너자이드", "카모트린"]
COLORS = ["#ff6464", "#ffb13b", "#55dcff", "#8ce66a", "#84c9ff"]


def load_csv():
    with (ASSETS.parent / "workshop-overlays/3831732004/Localization/ko.csv").open(encoding="utf-8-sig", newline="") as f:
        return dict(csv.reader(f))


def wrapped(draw, text, font, max_width):
    words = text.split(" ")
    lines, line = [], ""
    for word in words:
        test = word if not line else line + " " + word
        if line and draw.textlength(test, font=font) > max_width:
            lines.append(line)
            line = word
        else:
            line = test
    if line:
        lines.append(line)
    return lines


def render_card(page, entry, price, amounts, aged, iced, blended, optional_k, text):
    image = Image.new("RGB", (WIDTH, HEIGHT), "black")
    draw = ImageDraw.Draw(image)
    regular = ImageFont.truetype(str(FONT), 31)
    bold = ImageFont.truetype("C:/Windows/Fonts/malgunbd.ttf", 34)
    x, y, gap = 32, 24, 44
    name = text[f"item_drink_{entry}_name"]
    quote = text[f"item_drink_{entry}_quote"]
    tags = text[f"item_drink_{entry}_tags"]
    draw.text((x,y), f"{name} · ${price}", font=bold, fill="#ffffff")
    y += gap + 8
    pieces = [f"{INGREDIENTS[i]} {amounts[i]}" for i in range(5) if amounts[i]]
    if optional_k:
        pieces.append("카모트린 선택")
    line = "재료: " + ", ".join(pieces)
    for wrapped_line in wrapped(draw,line,regular,WIDTH-2*x):
        draw.text((x,y),wrapped_line,font=regular,fill="#66dfff")
        y += gap
    for label,value in (("숙성","필요" if aged else "불필요"),("얼음","필요" if iced else "불필요"),("블렌딩","필요" if blended else "불필요")):
        draw.text((x,y),f"{label}: {value}",font=regular,fill="#ffffff")
        y += gap
    y += 8
    for line in wrapped(draw,"“"+quote+"”",regular,WIDTH-2*x):
        draw.text((x,y),line,font=regular,fill="#e7e7e7")
        y += gap
    y += 4
    for line in wrapped(draw,tags,regular,WIDTH-2*x):
        draw.text((x,y),line,font=regular,fill="#e7e7e7")
        y += gap
    image.save(ASSETS / f"recipe_{page:02}.png", optimize=True)


def render_text_page(name, title, body):
    image=Image.new("RGB",(WIDTH,HEIGHT),"black")
    draw=ImageDraw.Draw(image)
    font=ImageFont.truetype(str(FONT),31)
    heading=ImageFont.truetype("C:/Windows/Fonts/malgunbd.ttf",40)
    x,y,gap=54,50,48
    draw.text((x,y),title,font=heading,fill="white"); y+=74
    for paragraph in body:
        for line in wrapped(draw,paragraph,font,WIDTH-2*x):
            draw.text((x,y),line,font=font,fill="#e7e7e7");y+=gap
        y+=20
    image.save(ASSETS/name,optimize=True)
def source_glyph_bounds(image, search_box):
    left, top, right, bottom = search_box
    points = [
        (x, y)
        for y in range(top, bottom)
        for x in range(left, right)
        if image.getpixel((x, y))[:3] != (0, 0, 0)
    ]
    if not points:
        raise ValueError(f"No source glyph found in {search_box}")
    bounds = (
        min(x for x, _ in points),
        min(y for _, y in points),
        max(x for x, _ in points) + 1,
        max(y for _, y in points) + 1,
    )
    if bounds[0] <= left or bounds[1] <= top or bounds[2] >= right or bounds[3] >= bottom:
        raise ValueError(f"Source glyph touches scan boundary: {bounds}")
    return bounds


def draw_pixel_label(mask, area, text, max_size, align="left"):
    draw = ImageDraw.Draw(mask)
    left, top, right, bottom = area
    for size in range(max_size, 17, -1):
        font = ImageFont.truetype(str(PANEL_FONT), size)
        bounds = draw.multiline_textbbox((0, 0), text, font=font, spacing=0, align=align)
        width, height = bounds[2] - bounds[0], bounds[3] - bounds[1]
        if width <= right - left and height <= bottom - top:
            x = (left if align == "left" else left + (right - left - width) // 2) - bounds[0]
            y = top + (bottom - top - height) // 2 - bounds[1]
            draw.multiline_text((x, y), text, font=font, fill=255, spacing=0, align=align)
            return
    raise ValueError(f"Panel label does not fit: {text}")


def render_panel():
    import dnfile

    if not PANEL_SOURCE.is_file():
        raise FileNotFoundError(f"VA-11 source DLL not found: {PANEL_SOURCE}")
    assembly = dnfile.dnPE(str(PANEL_SOURCE))
    resource = next(
        (item for item in assembly.net.resources if str(item.name) == PANEL_RESOURCE),
        None,
    )
    if resource is None:
        raise ValueError(f"Embedded panel resource not found: {PANEL_RESOURCE}")
    source = Image.open(io.BytesIO(resource.data)).convert("RGBA")
    if source.size != (872, 582):
        raise ValueError(f"Unexpected source panel size: {source.size}")

    # See va11-recipes/fonts/OFL-Galmuri9.txt; bundled under SIL OFL 1.1.
    glyphs = [
        ((180, 118, 345, 163), (187, 118, 397, 163), "아델하이드", 30, "left"),
        ((403, 118, 615, 159), (409, 118, 620, 159), "브론순 추출액", 30, "left"),
        ((635, 118, 820, 159), (640, 118, 858, 159), "델타 파우더", 30, "left"),
        ((184, 299, 370, 344), (190, 299, 397, 344), "플래너자이드", 30, "left"),
        ((635, 299, 836, 336), (640, 299, 852, 336), "카모트린", 30, "left"),
        ((75, 299, 154, 340), (54, 295, 165, 343), "숙성", 38, "center"),
        ((65, 118, 150, 160), (57, 117, 160, 159), "얼음 추가", 30, "center"),
    ]
    if not PANEL_FONT.is_file():
        raise FileNotFoundError(f"Bundled Galmuri9 font not found: {PANEL_FONT}")
    cleared = source.copy()
    clear_draw = ImageDraw.Draw(cleared)
    allowed = Image.new("L", source.size, 0)
    allowed_draw = ImageDraw.Draw(allowed)
    text_mask = Image.new("L", source.size, 0)
    for scan_box, label_area, text, font_size, alignment in glyphs:
        bounds = source_glyph_bounds(source, scan_box)
        exact_bounds = (bounds[0], bounds[1], bounds[2] - 1, bounds[3] - 1)
        clear_draw.rectangle(exact_bounds, fill=(0, 0, 0, 255))
        allowed_draw.rectangle(exact_bounds, fill=255)
        draw_pixel_label(text_mask, label_area, text, font_size, alignment)
    text_mask = text_mask.point(lambda value: 255 if value >= 128 else 0)
    allowed = ImageChops.lighter(allowed, text_mask)
    image = cleared.resize((872 * 4, 582 * 4), Image.Resampling.NEAREST)
    pixel_mask = text_mask.resize(image.size, Image.Resampling.NEAREST)
    white = Image.new("RGBA", image.size, (255, 255, 255, 255))
    image.alpha_composite(Image.composite(white, Image.new("RGBA", image.size), pixel_mask))

    original_pixels = source.resize(image.size, Image.Resampling.NEAREST)
    changed = ImageChops.difference(original_pixels, image).convert("RGB").convert("L")
    changed = changed.point(lambda value: 255 if value else 0)
    allowed_pixels = allowed.resize(image.size, Image.Resampling.NEAREST)
    outside = ImageChops.multiply(changed, ImageChops.invert(allowed_pixels))
    if outside.getbbox():
        raise ValueError(f"Panel generation altered pixels outside text areas: {outside.getbbox()}")
    print("Panel pixel diff outside original text areas: 0")
    image.save(ASSETS / "panel_ko.png", optimize=True)






def main():
    text=load_csv()
    for page,recipe in enumerate(RECIPES,1):
        key,price,*rest=recipe
        amounts=rest[:5]; aged,iced,blended,optional_k=rest[5:]
        render_card(page,key,price,amounts,aged,iced,blended,optional_k,text)
    render_text_page("recipe_intro.png","B.T.C · 레시피 북",[
        "다시 오신 걸 환영합니다.","B.T.C 인증 바텐더", "탐색 메뉴를 이용해 레시피를 살펴보세요.",
        "레시피를 따라 음료를 만들고 손님에게 서빙하세요."
    ])
    render_text_page("recipe_hint.png","용어 설명",[
        "혼합: 혼합 버튼을 누르면 셰이커가 작동합니다.",
        "블렌딩: 혼합 버튼을 누르고 5초간 작동한 뒤, 흔들림이 빨라지면 멈추기 버튼을 누릅니다.",
        "얼음: 레시피에 얼음이 필요하면 얼음 버튼을 켭니다.",
        "숙성: 레시피에 숙성이 필요하면 숙성 버튼을 켭니다.",
        "큰 잔: 재료량과 가격이 두 배입니다. 큰 잔을 만들 수 없는 음료도 있습니다.",
        "레시피와 재료량, 얼음, 숙성, 블렌딩 조건을 정확히 맞추세요."
    ])
    render_panel()
    print("Rendered 24 recipe cards, 2 guide pages, and the 4× mixing panel")

if __name__ == "__main__":
    main()
