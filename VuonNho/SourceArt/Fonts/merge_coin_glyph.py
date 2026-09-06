"""Ghep glyph dong tien vao font chu cua game.

Mot UI.Text cua uGUI chi ve duoc bang **mot** font. Icon tien cua HUD la mot glyph
cua Material Symbols, nen moi cho muon dat icon canh mot con so deu phai tu dung
mot hang ngang: [o glyph][o chu]. Cach do lam duoc cho the va nut, nhung khong lam
duoc giua mot cau — "Mua 240 xu de kich hoat robot" thi icon biet nhet vao dau.

Nen thay vi lach quanh gioi han cua font, bo gioi han di: chep dung mot glyph
(local_atm, U+E53E) tu Material Symbols sang tung file National Park, giu nguyen
ma. Sau buoc nay, chu "xu" o BAT KY dau nao — ke ca giua mot cau van xuoi — chi
la mot ky tu "\\ue53e" trong chuoi.

Chay lai khi doi font chu hoac doi icon tien:
    python SourceArt/Fonts/merge_coin_glyph.py
"""
import os

from fontTools.ttLib import TTFont
from fontTools.pens.boundsPen import BoundsPen
from fontTools.pens.ttGlyphPen import TTGlyphPen
from fontTools.pens.transformPen import TransformPen
from fontTools.misc.transform import Transform

FONT_DIR = os.path.join(os.path.dirname(os.path.dirname(os.path.dirname(
    os.path.abspath(__file__)))), "Assets", "Art", "Fonts")
SYMBOLS = os.path.join(FONT_DIR, "MaterialSymbolsRounded.ttf")
COIN = 0xE53E   # local_atm, dung ma goc de UiFactory.Symbols.LocalAtm van dung mot hang so

# Glyph goc cao 640 tren em 960; chu so cua National Park cao 715 tren em 1000. Giu nguyen
# toa do va doi em thanh 1000 la icon con 0,64 em — thap hon chu so mot chut, dung nhu mot
# hinh dac to nen trong canh chu. Ha xuong 150 de day icon nam ngay tren duong co so thay vi
# noi giua dong.
SCALE = 1.0
BASELINE_SHIFT = -150


def coin_pen_source(symbols):
    name = symbols.getBestCmap()[COIN]
    glyph = symbols["glyf"][name]
    if glyph.isComposite():
        raise SystemExit("Glyph tien la glyph ghep; script nay chi chep glyph don.")
    return name


def merge_into(path, symbols, source_name):
    font = TTFont(path)
    if COIN in font.getBestCmap():
        print("  bo qua (da co): " + os.path.basename(path))
        return

    target_name = "coinLocalAtm"
    if target_name in font.getGlyphOrder():
        raise SystemExit("Font da co glyph ten " + target_name)

    pen = TTGlyphPen(None)
    transform = Transform(SCALE, 0, 0, SCALE, 0, BASELINE_SHIFT)
    symbols.getGlyphSet()[source_name].draw(TransformPen(pen, transform))
    font["glyf"][target_name] = pen.glyph()

    advance = int(round(symbols["hmtx"][source_name][0] * SCALE))
    bounds = BoundsPen(font.getGlyphSet({target_name: font["glyf"][target_name]}))
    font["glyf"][target_name].draw(bounds, font["glyf"])
    left = int(round(bounds.bounds[0])) if bounds.bounds else 0
    font["hmtx"][target_name] = (advance, left)

    # glyf["ten"] = ... da tu them ten vao glyphOrder chung; goi setGlyphOrder nua la them lan
    # thu hai va maxp dem lech mot glyph.
    for table in font["cmap"].tables:
        if table.isUnicode():
            table.cmap[COIN] = target_name

    font.save(path)
    print("  da ghep: " + os.path.basename(path) + "  (advance " + str(advance) + ")")


def main():
    symbols = TTFont(SYMBOLS)
    source_name = coin_pen_source(symbols)

    names = sorted(n for n in os.listdir(FONT_DIR) if n.startswith("NationalPark-")
                   and n.endswith(".ttf"))
    if not names:
        raise SystemExit("Khong tim thay font National Park o " + FONT_DIR)

    print("Ghep U+%04X (%s) vao %d file:" % (COIN, source_name, len(names)))
    for name in names:
        merge_into(os.path.join(FONT_DIR, name), symbols, source_name)


if __name__ == "__main__":
    main()
