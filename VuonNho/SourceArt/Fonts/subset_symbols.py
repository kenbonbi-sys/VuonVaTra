"""Cut Material Symbols down to the icons the HUD actually uses.

The full variable font is 15 MB and 4383 glyphs; the game needs 19. It is also
instanced to a fixed weight first, so Unity gets a plain static font rather than
a variable one whose axes it cannot drive anyway.
"""
import io, os
from fontTools.ttLib import TTFont
from fontTools import subset
from fontTools.varLib import instancer

SOURCE = r"C:\Users\PC\Desktop\Game\.tools\icons\MaterialSymbolsRounded.ttf"
CODEPOINTS = r"C:\Users\PC\Desktop\Game\.tools\icons\symbols.codepoints"
LICENSE = r"C:\Users\PC\Desktop\Game\.tools\icons\LICENSE-material-symbols.txt"
DEST_DIR = r"C:\Users\PC\Desktop\Game\VuonNho\Assets\Art\Fonts"
DEST = os.path.join(DEST_DIR, "MaterialSymbolsRounded.ttf")

# Ten icon tren fonts.google.com/icons, theo dung nut dang dung chung.
ICONS = [
    "inventory_2", "upgrade", "format_paint", "settings", "close", "sell",
    "shopping_cart", "add", "delete", "play_arrow", "download", "restart_alt",
    "undo", "delete_forever", "fast_forward", "logout", "volume_up", "volume_off",
    "check", "local_atm", "schedule",
]


def main():
    codepoints = {}
    for line in io.open(CODEPOINTS, encoding="utf-8"):
        parts = line.split()
        if len(parts) == 2:
            codepoints[parts[0]] = int(parts[1], 16)

    missing = [name for name in ICONS if name not in codepoints]
    if missing:
        raise SystemExit("Khong co icon: " + ", ".join(missing))

    wanted = sorted({codepoints[name] for name in ICONS})

    font = TTFont(SOURCE)
    # Chot truc bien thien: net 400, khong to dac, do dam 0, kich thuoc quang hoc 24.
    font = instancer.instantiateVariableFont(font, {"wght": 400, "FILL": 0, "GRAD": 0, "opsz": 24})

    options = subset.Options()
    options.layout_features = []
    options.name_IDs = ["*"]
    options.notdef_outline = True
    options.recalc_bounds = True
    subsetter = subset.Subsetter(options=options)
    subsetter.populate(unicodes=wanted)
    subsetter.subset(font)

    os.makedirs(DEST_DIR, exist_ok=True)
    font.save(DEST)

    import shutil
    shutil.copyfile(LICENSE, os.path.join(DEST_DIR, "LICENSE-MaterialSymbols.txt"))

    print("Da cat %d icon, con %d KB (goc %d MB)"
          % (len(wanted), os.path.getsize(DEST) // 1024, os.path.getsize(SOURCE) // (1024 * 1024)))
    for name in ICONS:
        print("  %-16s U+%04X" % (name, codepoints[name]))


if __name__ == "__main__":
    main()
