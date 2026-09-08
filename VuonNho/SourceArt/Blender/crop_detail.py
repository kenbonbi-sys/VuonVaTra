"""Detailed, palette-only garden crops and five recognisable tea varieties.

Hook after an existing builder with ``enrich_crop(root, asset_name)``. The root
pivot and FoliageRoot / ReadyAccents contract are preserved. Standalone usage::

    blender --background --python crop_detail.py -- --project PATH --tea-only

Only this module's crop .blend / FBX outputs and optional crop icon PNGs are
written. Existing FBX meta files are never touched.
"""

from __future__ import annotations

import argparse
import json
import math
from pathlib import Path
import sys

import bpy
from mathutils import Vector


TEA_ASSETS = {
    "SM_TeaBush": "crop_tea_xo",
    "SM_TeaMocCau": "crop_tea_moccau",
    "SM_TeaNonTom": "crop_tea_nontom",
    "SM_TeaDinh": "crop_tea_dinh",
    "SM_OrientalBeauty": "crop_oriental_beauty",
}
HERB_ASSETS = ("SM_Seedling", "SM_Mint", "SM_Chamomile", "SM_Strawberry", "SM_Lemongrass", "SM_Jasmine")
VERSION = 2


def generator():
    main = sys.modules.get("__main__")
    if main is not None and hasattr(main, "MATERIALS") and hasattr(main, "save_export"):
        return main
    import generate_garden
    return generate_garden


def bounds(root):
    bpy.context.view_layer.update()
    points = [obj.matrix_world @ vertex.co for obj in root.children_recursive
              if obj.type == "MESH" for vertex in obj.data.vertices]
    if not points:
        return None
    return tuple(min(p[i] for p in points) for i in range(3)), tuple(max(p[i] for p in points) for i in range(3))


def veined_leaf(name, base, length, width, angle, rise=.055, material="M_Leaf",
                serrated=False, veins=True, curl=.055, thickness=.021):
    """Closed eight-sided leaf sections, a folded midrib, tiny edge teeth and veins.

    The modest facet count is intentional: the leaf remains a solid readable
    silhouette, while its rounded shoulder catches soft light from the game sun.
    """
    g = generator()
    ca, sa = math.cos(angle), math.sin(angle)
    def position(x, y, z):
        return (base[0] + x * ca - y * sa, base[1] + x * sa + y * ca, base[2] + z)
    def arch(t):
        return rise * t + curl * math.sin(math.pi * t)
    rings = 9 if serrated else 7
    vertices = [position(0, 0, 0)]
    for section in range(1, rings + 1):
        t = section / (rings + 1)
        profile = math.sin(math.pi * t) ** .78
        teeth = 1.0 + (.055 if section % 2 else -.07) if serrated else 1.0
        half = width * .5 * profile * teeth
        for side in range(8):
            a = side * math.pi / 4
            vertices.append(position(length * t, math.cos(a) * half,
                                     arch(t) + math.sin(a) * thickness * profile * .5))
    vertices.append(position(length, 0, rise))
    tip = len(vertices) - 1
    faces = []
    for side in range(8):
        faces.append((0, 1 + (side + 1) % 8, 1 + side))
    for section in range(rings - 1):
        a = 1 + section * 8
        b = a + 8
        for side in range(8):
            faces.append((a + side, a + (side + 1) % 8, b + (side + 1) % 8, b + side))
    last = 1 + (rings - 1) * 8
    for side in range(8):
        faces.append((last + side, last + (side + 1) % 8, tip))
    leaf = g.mesh_object(name, vertices, faces, material)
    parts = [leaf]
    if veins and length > .13:
        radius = max(.0015, min(.0027, width * .017))
        for segment in range(4):
            t1, t2 = .06 + segment * .205, .06 + (segment + 1) * .205
            p1 = position(length * t1, 0, arch(t1) + thickness * .47 * math.sin(math.pi * t1))
            p2 = position(length * t2, 0, arch(t2) + thickness * .47 * math.sin(math.pi * t2))
            parts.append(g.stem(name + " raised midrib", p1, p2, radius, "M_Grass", vertices=5))
        for t in (.29, .48, .66):
            for sign in (-1, 1):
                end_t = t + .12
                y = sign * width * .35 * math.sin(math.pi * end_t)
                p1 = position(length * t, 0, arch(t) + thickness * .49 * math.sin(math.pi * t))
                p2 = position(length * end_t, y, arch(end_t) + thickness * .28)
                parts.append(g.stem(name + " side vein", p1, p2, radius * .58, "M_Grass", vertices=4))
    return parts


def twig(parts, name, start, bend, end, radius=.012, woody=False):
    g = generator()
    material = "M_Wood" if woody else "M_Leaf"
    parts.append(g.stem(name + " lower", start, bend, radius, material, vertices=8))
    parts.append(g.stem(name + " upper", bend, end, radius * .68, material, vertices=8))


def bud(parts, name, point, angle, size=.10, tint="M_Grass", paired=True):
    g = generator()
    start = (point[0], point[1], point[2] - .022)
    tip = (point[0] + .011 * math.cos(angle), point[1] + .011 * math.sin(angle), point[2] + size * .90)
    parts.append(g.stem(name + " tender shoot", start, tip, .006, "M_Grass", vertices=6))
    parts += veined_leaf(name + " folded spear", point, size * .35, size * .24,
                         angle + .20, size * .93, tint, False, False, .012, .015)
    if paired:
        for side in (-1, 1):
            parts += veined_leaf(name + " young leaf", point, size * .84, size * .36,
                                 angle + side * .87, size * .42, tint,
                                 True, False, .015, .015)


def tea(root, species):
    g = generator()
    foliage, accents = [], []
    variant = list(TEA_ASSETS).index(species)
    trunk_top = (.014, -.009, .37)
    twig(foliage, "Tea woody trunk", (0, 0, .015), (-.012, .004, .18), trunk_top, .029, True)
    # Three forked branches carry staggered pairs of leaves. The silhouette has
    # breathing room between individual leaves instead of becoming a green ball.
    shoots = []
    for branch in range(3):
        angle = branch * math.tau / 3 + .35 + variant * .19
        outward = Vector((math.cos(angle), math.sin(angle), 0))
        reach = .145 + (branch % 2) * .032
        height = .61 + (branch % 3) * .045
        endpoint = outward * reach + Vector((0, 0, height))
        bend = outward * .105 + Vector((0, 0, .36))
        twig(foliage, "Tea fork %d" % branch, (0, 0, .12), bend, endpoint, .015, True)
        for layer in range(3):
            t = .20 + layer * .27
            node = bend.lerp(endpoint, t)
            for side in (-1, 1):
                leaf_angle = angle + side * (1.04 + layer * .075)
                petiole = node + Vector((math.cos(leaf_angle) * .025, math.sin(leaf_angle) * .025, .010))
                foliage.append(g.stem("Tea short petiole", node, petiole, .0055, "M_Leaf", vertices=6))
                length = (.224 - layer * .025) * (1 + .035 * variant)
                leaf_width = length * (.43 if species != "SM_TeaDinh" else .36)
                foliage += veined_leaf("Serrated tea leaf", petiole, length, leaf_width,
                                        leaf_angle, .020 + layer * .012, "M_Leaf", True,
                                        True, .031 + .005 * (branch % 2), .024)
        # Every plant has an immature closed bud; ready accents add the pluckable
        # bud-and-leaf set, so growing and ready have visibly different silhouettes.
        bud(foliage, "Closed tea bud", endpoint, angle, .044, "M_Leaf", False)
        shoots.append((endpoint, angle))
    for lower in range(4):
        angle = lower * math.pi * .5 + variant * .23
        base = (.012 * math.cos(angle), .012 * math.sin(angle), .16 + (lower % 2) * .055)
        foliage += veined_leaf("Lower tea leaf", base, .225, .112, angle, .055,
                               "M_Leaf", True, True, .045, .027)
    for branch, (point, angle) in enumerate(shoots):
        size = .12 if species == "SM_TeaNonTom" else .105
        tint = "M_Red" if species == "SM_OrientalBeauty" and branch != 1 else "M_Grass"
        bud(accents, "Harvest tea tip", point, angle, size, tint,
            paired=species != "SM_TeaDinh")
        if species == "SM_TeaMocCau":
            # Slim curled new leaves imply the hooked tender shoot without
            # depicting processed tea as something already growing on the bush.
            accents += veined_leaf("Curled tender leaf", point, .086, .031,
                                    angle + 1.5, .053, "M_Grass", True, False, .061, .016)
    g.join(foliage, "FoliageRoot", root)
    g.join(accents, "ReadyAccents", root)


def seedling(root):
    g = generator()
    parts = [g.stem("Tender seed stem", (0, 0, .005), (0, 0, .18), .014, "M_Leaf", vertices=8)]
    for side in (0, 1):
        angle = math.radians(9 + side * 166)
        parts += veined_leaf("Cotyledon", (0, 0, .14 + side * .009), .205, .125,
                             angle, .055, "M_Leaf", False, True, .026, .024)
    bud(parts, "New seedling tip", (0, 0, .17), 1.1, .048, "M_Grass", False)
    g.join(parts, "SeedlingMesh", root)


def mint(root):
    g = generator()
    foliage, accents = [], []
    twig(foliage, "Mint square stem", (0, 0, .006), (0, 0, .42), (.01, 0, .82), .019)
    for layer, (height, length, degrees) in enumerate(((.17, .33, 15), (.33, .32, 105), (.49, .28, 25), (.65, .215, 112))):
        for side in range(2):
            angle = math.radians(degrees + side * 180)
            base = (.045 * math.cos(angle), .045 * math.sin(angle), height + .02)
            twig(foliage, "Mint petiole", (0, 0, height - .018),
                 (.018 * math.cos(angle), .018 * math.sin(angle), height), base, .008)
            foliage += veined_leaf("Scalloped mint leaf", base, length, length * .58,
                                    angle, .055, "M_Leaf", True, True, .048, .026)
    for side in range(2):
        angle = .55 + side * math.pi
        endpoint = (.10 * math.cos(angle), .10 * math.sin(angle), .53)
        twig(foliage, "Mint side shoot", (0, 0, .20),
             (endpoint[0] * .7, endpoint[1] * .7, .36), endpoint, .010)
        for direction in (-1, 1):
            foliage += veined_leaf("Small lateral mint leaf", endpoint, .17, .093,
                                    angle + direction * .95, .055, "M_Leaf", True, True, .026, .02)
    bud(accents, "Fresh mint crown", (.01, 0, .81), .4, .10, "M_Grass", True)
    g.join(foliage, "FoliageRoot", root)
    g.join(accents, "ReadyAccents", root)


def daisy(parts, center, size=1., petal_count=11):
    g = generator()
    for petal in range(petal_count):
        angle = petal * math.tau / petal_count
        start = (center[0] + .032 * size * math.cos(angle), center[1] + .032 * size * math.sin(angle), center[2])
        parts += veined_leaf("Chamomile ivory ray", start, .125 * size, .047 * size,
                             angle, -.026 * size, "M_Cream", False, False, .016 * size, .021 * size)
    parts.append(g.sphere("Chamomile gold dome", (center[0], center[1], center[2] + .025 * size),
                          (.119 * size, .119 * size, .072 * size), "M_Yellow", 14, 7))
    for bead in range(9):
        angle = bead * 2.399963
        radius = .032 * size * math.sqrt((bead + 1) / 9)
        parts.append(g.sphere("Chamomile pollen bead", (center[0] + math.cos(angle) * radius,
                              center[1] + math.sin(angle) * radius, center[2] + .056 * size),
                              (.012 * size,) * 3, "M_Yellow", 6, 3))


def chamomile(root):
    g = generator()
    foliage, accents = [], []
    centres = [(-.23, .02, .67), (.20, -.065, .78), (.015, .23, .60)]
    for i, center in enumerate(centres):
        twig(foliage, "Chamomile branched stalk", (0, 0, .008),
             (center[0] * .55, center[1] * .55, .32), center, .013)
        for layer in range(2):
            angle = i * math.tau / 3 + layer * .7
            base = (center[0] * .46, center[1] * .46, .17 + layer * .14)
            axis_end = Vector(base) + Vector((math.cos(angle) * .19, math.sin(angle) * .19, .028))
            foliage.append(g.stem("Feathery chamomile rachis", base, axis_end, .0045, "M_Leaf", vertices=6))
            for segment in range(4):
                start = Vector(base).lerp(axis_end, .18 + segment * .19)
                for side in (-1, 1):
                    foliage += veined_leaf("Fine chamomile leaflet", start, .065 - .007 * segment,
                                            .021, angle + side * .80, .018, "M_Leaf", False, False, .008, .009)
        daisy(accents, center, .90 + i * .055)
    closed = (.10, -.14, .51)
    twig(foliage, "Small chamomile bud stalk", (0, 0, .12), (.05, -.08, .31), closed, .008)
    foliage.append(g.sphere("Closed chamomile bud", closed, (.060, .060, .078), "M_Grass", 9, 5))
    g.join(foliage, "FoliageRoot", root)
    g.join(accents, "ReadyAccents", root)


def strawberry(root):
    g = generator()
    foliage, accents = [], []
    for i, angle in enumerate((.3, 1.8, 3.0, 4.8)):
        base = (.10 * math.cos(angle), .10 * math.sin(angle), .21 + .035 * (i % 2))
        twig(foliage, "Strawberry leaf petiole", (0, 0, .008),
             (base[0] * .65, base[1] * .65, .14), base, .012)
        for fan in (-1, 0, 1):
            foliage += veined_leaf("Trifoliate strawberry leaf", base, .225 if fan == 0 else .17,
                                    .135 if fan == 0 else .103, angle + fan * .85, .045,
                                    "M_Leaf", True, True, .033, .022)
    for i, (x, y, z, size) in enumerate(((-.25, .24, .032, 1.), (.18, .30, .016, .94), (.27, -.11, .048, .80))):
        height, width = .28 * size, .245 * size
        accents.append(g.berry("Ripe heart strawberry", (x, y, z), width, height))
        top = (x, y, z + height * .90)
        for j in range(5):
            accents += veined_leaf("Star strawberry calyx", top, .082 * size, .032 * size,
                                   j * math.tau / 5 + i * .4, -.028, "M_Leaf", False, False, .009, .012)
        # Sparse golden achenes are sunk into the red shoulders; their placement
        # follows the same fruit profile as the original berry mesh.
        for ring, (t, profile, count) in enumerate(((.24, .80, 7), (.46, .98, 9), (.66, .95, 8), (.80, .72, 6))):
            for seed in range(count):
                a = seed * math.tau / count + ring * .27
                pos = (x + math.cos(a) * width * .505 * profile,
                       y + math.sin(a) * width * .465 * profile, z + height * t)
                accents.append(g.sphere("Tiny strawberry achene", pos, (.008 * size, .007 * size, .017 * size),
                                        "M_Yellow", 5, 3))
        twig(accents, "Curved fruit stem", top, (x * .88, y * .88, z + height + .035),
             (x * .67, y * .67, .32), .008)
    g.join(foliage, "FoliageRoot", root)
    g.join(accents, "ReadyAccents", root)


def lemongrass(root):
    g = generator()
    foliage, accents = [], []
    for stalk in range(5):
        angle = stalk * 2.399963
        x, y = .065 * math.cos(angle), .065 * math.sin(angle)
        foliage.append(g.stem("Pale lemongrass sheath", (x, y, .002), (x * 1.08, y * 1.08, .23),
                              .025 if stalk else .03, "M_Cream", vertices=10))
        foliage.append(g.stem("Green lemongrass neck", (x, y, .15), (x * 1.1, y * 1.1, .32),
                              .017, "M_Grass", vertices=8))
    for i in range(14):
        angle = i * 2.399963
        base = (.045 * math.cos(angle), .045 * math.sin(angle), .16 + (i % 3) * .03)
        foliage += veined_leaf("Long folded lemongrass blade", base, .22 + (i % 4) * .042,
                               .031 + (i % 3) * .006, angle, .52 + (i % 3) * .058,
                               "M_Grass" if i % 4 else "M_Leaf", False, True, .045, .014)
    accents.append(g.cylinder("Harvest tie", (0, 0, .215), .105, .028, "M_Accent", 16, .005))
    for i in range(3):
        angle = i * math.tau / 3 + .6
        accents += veined_leaf("Golden mature outer blade", (.06 * math.cos(angle), .06 * math.sin(angle), .20),
                               .27, .035, angle, .37, "M_Yellow", False, True, .028, .012)
    g.join(foliage, "FoliageRoot", root)
    g.join(accents, "ReadyAccents", root)


def jasmine(root):
    g = generator()
    foliage, accents = [], []
    twig(foliage, "Jasmine woody trunk", (0, 0, .009), (.007, 0, .20), (0, 0, .47), .023, True)
    for i in range(6):
        angle = i * math.tau / 6 + .2
        end = (.13 * math.cos(angle), .13 * math.sin(angle), .36 + (i % 3) * .12)
        twig(foliage, "Jasmine branch", (0, 0, .15),
             (end[0] * .6, end[1] * .6, end[2] * .7), end, .010)
        for side in (-1, 1):
            foliage += veined_leaf("Glossy oval jasmine leaf", end, .20 - .015 * (i % 3), .094,
                                   angle + side * .67, .055, "M_Leaf", False, True, .035, .028)
    centres = [(-.165, .10, .69), (.155, .135, .58), (.05, -.185, .75), (.20, -.042, .81)]
    for i, center in enumerate(centres):
        twig(foliage, "Jasmine flower stalk", (0, 0, .34),
             (center[0] * .8, center[1] * .8, center[2] - .08), center, .007)
        for layer in range(2):
            for j in range(5):
                angle = j * math.tau / 5 + layer * .46 + i * .15
                start = (center[0] + .021 * math.cos(angle), center[1] + .021 * math.sin(angle), center[2] + layer * .013)
                accents += veined_leaf("Layered jasmine star petal", start,
                                       .094 if layer == 0 else .065, .044 if layer == 0 else .031,
                                       angle, -.005 + layer * .007, "M_Cream", False, False, .015, .02)
        accents.append(g.sphere("Ivory jasmine centre", (center[0], center[1], center[2] + .022),
                                (.035, .035, .027), "M_Cream", 10, 5))
    for angle in (.8, 3.2):
        point = (.19 * math.cos(angle), .19 * math.sin(angle), .56)
        twig(foliage, "Unopened jasmine bud stem", (0, 0, .26),
             (point[0] * .75, point[1] * .75, .45), point, .007)
        foliage.append(g.sphere("Cream jasmine bud", point, (.041, .041, .066), "M_Cream", 9, 6))
    g.join(foliage, "FoliageRoot", root)
    g.join(accents, "ReadyAccents", root)


def fit_bounds(root, previous, tea_asset=False):
    current = bounds(root)
    if current is None:
        raise RuntimeError("Crop builder produced no mesh")
    lo, hi = current
    if tea_asset:
        target_lo, target_hi = (-.45, -.45, 0.), (.45, .45, .78)
    else:
        target_lo, target_hi = previous
    # Preserve the original crop's occupied envelope and ground contact. Objects
    # remain at the root pivot; only authored vertex coordinates are fitted.
    span = [hi[i] - lo[i] for i in range(3)]
    target_span = [target_hi[i] - target_lo[i] for i in range(3)]
    xy_scale = min(target_span[0] / span[0], target_span[1] / span[1])
    z_scale = target_span[2] / span[2]
    center = [(lo[i] + hi[i]) * .5 for i in range(2)]
    target_center = [(target_lo[i] + target_hi[i]) * .5 for i in range(2)]
    for obj in root.children_recursive:
        if obj.type != "MESH":
            continue
        for vertex in obj.data.vertices:
            world = obj.matrix_world @ vertex.co
            world.x = (world.x - center[0]) * xy_scale + target_center[0]
            world.y = (world.y - center[1]) * xy_scale + target_center[1]
            world.z = (world.z - lo[2]) * z_scale + target_lo[2]
            vertex.co = obj.matrix_world.inverted() @ world
        obj.data.update()


def enrich_crop(root, species):
    """Rebuild a crop with richer detail, preserving its root and render contract."""
    aliases = {value: key for key, value in TEA_ASSETS.items()}
    aliases.update({"crop_" + name[3:].lower(): name for name in HERB_ASSETS})
    species = aliases.get(species, species)
    if species not in TEA_ASSETS and species not in HERB_ASSETS:
        return False
    if root.get("CropDetailVersion") == VERSION:
        return True
    original_bounds = bounds(root)
    if original_bounds is None and species not in TEA_ASSETS:
        g = generator()
        g.BUILDERS[species](root)
        original_bounds = bounds(root)
    for child in list(root.children_recursive):
        bpy.data.objects.remove(child, do_unlink=True)
    builders = {"SM_Seedling": seedling, "SM_Mint": mint, "SM_Chamomile": chamomile,
                "SM_Strawberry": strawberry, "SM_Lemongrass": lemongrass, "SM_Jasmine": jasmine}
    if species in TEA_ASSETS:
        tea(root, species)
    else:
        builders[species](root)
    fit_bounds(root, original_bounds, species in TEA_ASSETS)
    root["CropDetailVersion"] = VERSION
    root["CropSpecies"] = species
    root["DetailNotes"] = "Solid curved leaves, raised midribs and veins, forked stems, distinct harvest tips or flowers."
    return True


def render_icon(root, filepath, resolution=256):
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.samples = 32
    scene.cycles.use_denoising = True
    scene.render.resolution_x = scene.render.resolution_y = resolution
    scene.render.resolution_percentage = 100
    scene.render.film_transparent = True
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.view_settings.view_transform = "Standard"
    world = bpy.data.worlds.new("Crop icon soft world") if scene.world is None else scene.world
    scene.world = world
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (.70, .73, .66, 1)
    world.node_tree.nodes["Background"].inputs[1].default_value = .65
    camera_data = bpy.data.cameras.new("Crop icon camera")
    camera = bpy.data.objects.new("Crop icon camera", camera_data)
    scene.collection.objects.link(camera)
    lo, hi = bounds(root)
    target = Vector((0, 0, (lo[2] + hi[2]) * .50))
    camera.location = target + Vector((1.6, 2.7, 1.9))
    camera.rotation_euler = (target - camera.location).to_track_quat("-Z", "Y").to_euler()
    camera_data.type = "ORTHO"
    bpy.context.view_layer.update()
    view = camera.matrix_world.inverted()
    projected = [view @ (obj.matrix_world @ vertex.co) for obj in root.children_recursive
                 if obj.type == "MESH" for vertex in obj.data.vertices]
    xmin, xmax = min(p.x for p in projected), max(p.x for p in projected)
    ymin, ymax = min(p.y for p in projected), max(p.y for p in projected)
    camera.location += camera.rotation_euler.to_quaternion() @ Vector(((xmin + xmax) * .5, (ymin + ymax) * .5, 0))
    camera_data.ortho_scale = max(xmax - xmin, ymax - ymin) * 1.14
    scene.camera = camera
    lights = []
    for name, loc, energy, size in (("Softbox key", (-2, 2, 3.8), 160, 3),
                                     ("Softbox fill", (2, .2, 2), 75, 2.4)):
        data = bpy.data.lights.new(name, "AREA")
        data.energy, data.shape, data.size = energy, "DISK", size
        obj = bpy.data.objects.new(name, data)
        scene.collection.objects.link(obj)
        obj.location = loc
        obj.rotation_euler = (target - obj.location).to_track_quat("-Z", "Y").to_euler()
        lights.append(obj)
    filepath.parent.mkdir(parents=True, exist_ok=True)
    scene.render.filepath = str(filepath)
    bpy.ops.render.render(write_still=True)
    image = bpy.data.images.load(str(filepath), check_existing=False)
    pixels = list(image.pixels)
    edge = list(range(resolution)) + list(range((resolution - 1) * resolution, resolution * resolution))
    edge += [row * resolution for row in range(resolution)] + [row * resolution + resolution - 1 for row in range(resolution)]
    if any(pixels[pixel * 4 + 3] > .01 for pixel in edge):
        raise RuntimeError("Crop icon touches the image boundary: " + str(filepath))
    bpy.data.images.remove(image)
    for obj in lights + [camera]:
        bpy.data.objects.remove(obj, do_unlink=True)


def main():
    sys.path.insert(0, str(Path(__file__).resolve().parent))
    g = generator()
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project", required=True, type=Path)
    parser.add_argument("--tea-only", action="store_true")
    parser.add_argument("--only", choices=tuple(TEA_ASSETS) + HERB_ASSETS)
    parser.add_argument("--icons", action="store_true")
    parser.add_argument("--icons-only", action="store_true", help="Render saved crop models without changing source or FBX outputs.")
    parser.add_argument("--report", type=Path)
    args = parser.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])
    project = args.project.resolve()
    if not (project / "Assets").is_dir():
        parser.error("--project must contain Assets")
    for i, name in enumerate(TEA_ASSETS):
        g.ASSET_IDS[name] = "T%02d" % (i + 1)
    names = [args.only] if args.only else list(TEA_ASSETS) + ([] if args.tea_only else list(HERB_ASSETS))
    entries = {}
    # Prevent Blender backup copies from modifying unrelated .blend1 artifacts.
    bpy.context.preferences.filepaths.save_version = 0
    for name in names:
        if args.icons_only:
            bpy.ops.wm.open_mainfile(filepath=str(project / "SourceArt" / "Blender" / (name + ".blend")),
                                     load_ui=False, use_scripts=False)
            root = bpy.data.objects[name]
            render_icon(root, project / "Assets" / "Art" / "Icons" / ("ICO_" + name[3:] + ".png"))
            continue
        root = g.reset_scene(name)
        if name in HERB_ASSETS:
            g.BUILDERS[name](root)
        enrich_crop(root, name)
        entries[name] = g.save_export(name, root, project)
        if tuple(root.location) != (0., 0., 0.) or tuple(root.scale) != (1., 1., 1.):
            raise RuntimeError("Crop root transform changed: " + name)
        names_in_root = {child.name for child in root.children}
        required = {"SeedlingMesh"} if name == "SM_Seedling" else {"FoliageRoot", "ReadyAccents"}
        if not required.issubset(names_in_root):
            raise RuntimeError("Crop hierarchy missing: " + name)
        print("CROP_EXPORTED " + json.dumps(entries[name], ensure_ascii=False), flush=True)
        if args.icons:
            render_icon(root, project / "Assets" / "Art" / "Icons" / ("ICO_" + name[3:] + ".png"))
    if args.report:
        args.report.parent.mkdir(parents=True, exist_ok=True)
        args.report.write_text(json.dumps(entries, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
