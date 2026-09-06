"""Cat dai anh nam cay thanh nam icon vuong cho HUD.

Nguon: cay-strip.png — anh RGBA rut ra tu nam file SVG nguoi dung dua vao. Ca nam file SVG
deu nhung cung mot dai anh nay va chi khac o vung cat, nen chi giu lai mot ban.

    python slice_icons.py

Ghi ra Assets/Art/Icons/ICO_<Ten>.png, 256 x 256, nen trong suot. Vi tri tung cay do bang
alpha chu khong chia deu chieu cao: cay to nho khac nhau nen chia deu se cat mat la.
"""

import os
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
STRIP = os.path.join(HERE, "cay-strip.png")
OUTPUT = os.path.abspath(os.path.join(HERE, "..", "..", "Assets", "Art", "Icons"))

# Thu tu tren xuong duoi trong dai anh.
NAMES = ["Mint", "Chamomile", "Strawberry", "Lemongrass", "Jasmine"]

SIZE = 256
PADDING = 0.06      # phan chua khong khi moi ben, tinh theo canh cua o vuong
ALPHA_FLOOR = 8     # duoi nguong nay coi nhu trong suot: anh nguon co vien mo rat nhat


def bands(alpha, width, height):
    """Dai hang lien tiep co net ve. Moi dai la mot cay."""
    result = []
    start = None
    for y in range(height):
        row = alpha[y * width:(y + 1) * width]
        used = any(value >= ALPHA_FLOOR for value in row)
        if used and start is None:
            start = y
        elif not used and start is not None:
            result.append((start, y))
            start = None
    if start is not None:
        result.append((start, height))
    return result


def bounds(alpha, width, top, bottom):
    """Khung chu nhat om sat net ve trong mot dai."""
    left, right = width, 0
    for y in range(top, bottom):
        row = alpha[y * width:(y + 1) * width]
        for x in range(width):
            if row[x] >= ALPHA_FLOOR:
                if x < left: left = x
                if x > right: right = x
    return left, right + 1


def main():
    strip = Image.open(STRIP).convert("RGBA")
    width, height = strip.size
    alpha = strip.getchannel("A").tobytes()

    found = bands(alpha, width, height)
    if len(found) != len(NAMES):
        raise SystemExit("Doi %d cay trong dai anh, dem duoc %d." % (len(NAMES), len(found)))

    os.makedirs(OUTPUT, exist_ok=True)
    for name, (top, bottom) in zip(NAMES, found):
        left, right = bounds(alpha, width, top, bottom)

        # O vuong lay theo canh dai hon, tam trung voi tam net ve: cay cao va cay be deu
        # duoc quy ve cung mot khung nen chung nhin cung co trong hang.
        side = int(max(right - left, bottom - top) * (1 + 2 * PADDING))
        cx = (left + right) // 2
        cy = (top + bottom) // 2
        box = (cx - side // 2, cy - side // 2, cx - side // 2 + side, cy - side // 2 + side)

        icon = strip.crop(box).resize((SIZE, SIZE), Image.LANCZOS)
        path = os.path.join(OUTPUT, "ICO_" + name + ".png")
        icon.save(path)
        print("%-12s %4d x %4d -> %s" % (name, right - left, bottom - top, path))


if __name__ == "__main__":
    main()
