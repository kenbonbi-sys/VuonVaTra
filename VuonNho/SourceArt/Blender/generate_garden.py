"""Author the Vườn Nhỏ toy garden asset set in Blender 4.5 LTS.

Usage (from a shell, not Blender's interactive Python console)::

    blender --background --python generate_garden.py -- --project /path/to/VuonNho
    blender --background --python generate_garden.py -- --project /path/to/VuonNho --only SM_Mint --overwrite
    blender --background --python generate_garden.py -- --project /path/to/VuonNho --export-existing SourceArt/Blender/SM_Mint.blend

Each asset is an editable .blend outside Assets and one static FBX inside Assets.
Existing source files are protected unless --overwrite is explicitly supplied.
Geometry is original, deterministic, texture-free, and uses the project's nine
named palette materials. Coordinates in this source: +Z up, +Y camera-facing.
The FBX preset is -Z forward / Y up, one metre per unit, no animation.
"""

from __future__ import annotations

import argparse
import json
import math
from pathlib import Path
import random
import sys

import bpy
import bmesh
from mathutils import Vector


PALETTE = {
    "M_Grass": "A8BC78",
    "M_Leaf": "4F7B50",
    "M_Soil": "916447",
    "M_Wood": "B88858",
    "M_Cream": "F2E3BE",
    "M_Dark": "303C32",
    "M_Yellow": "E9BF5C",
    "M_Red": "D97672",
    "M_Accent": "81B8AB",
}
ASSET_IDS = {
    "SM_Plot": "A01", "SM_Seedling": "A02", "SM_Mint": "A03",
    "SM_Chamomile": "A04", "SM_Strawberry": "A05", "SM_Helper": "A06",
    "SM_TeaStation": "A07", "SM_BackgroundTree": "A08", "SM_Bush": "A09",
    "SM_Fence": "A10", "SM_Rock": "A11", "SM_CalibrationCube": "CAL01",
    "SM_Lemongrass": "A12", "SM_Jasmine": "A13",
}
SEED = 60206
MATERIALS = {}


def linear_channel(value):
    return value / 12.92 if value <= .04045 else ((value + .055) / 1.055) ** 2.4


def reset_scene(name):
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for collection in list(bpy.data.collections):
        bpy.data.collections.remove(collection)
    for material in list(bpy.data.materials):
        bpy.data.materials.remove(material)
    scene = bpy.context.scene
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0
    scene.unit_settings.length_unit = "METERS"
    collection = bpy.data.collections.new(name + "_Editable")
    scene.collection.children.link(collection)
    bpy.context.view_layer.active_layer_collection = bpy.context.view_layer.layer_collection.children[collection.name]
    MATERIALS.clear()
    for mat_name, hex_color in PALETTE.items():
        rgba = tuple(linear_channel(int(hex_color[i:i + 2], 16) / 255) for i in (0, 2, 4)) + (1.,)
        material = bpy.data.materials.new(mat_name)
        material.diffuse_color = rgba
        material.use_nodes = True
        bsdf = material.node_tree.nodes.get("Principled BSDF")
        bsdf.inputs["Base Color"].default_value = rgba
        bsdf.inputs["Metallic"].default_value = 0.
        bsdf.inputs["Roughness"].default_value = .68
        material["PaletteHex"] = "#" + hex_color
        MATERIALS[mat_name] = material
    root = bpy.data.objects.new(name, None)
    collection.objects.link(root)
    root.empty_display_type = "PLAIN_AXES"
    root.empty_display_size = .15
    root["AssetId"] = ASSET_IDS[name]
    root["Authoring"] = "Original procedural toy garden; editable mesh islands; no external art."
    root["Units"] = "1 Blender unit = 1 metre; Blender +Z up; camera-facing +Y."
    return root


def active(obj):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj


def finish(obj, name, material, smooth=False):
    obj.name = name
    obj.data.name = name + "_Mesh"
    obj.data.materials.append(MATERIALS[material])
    for polygon in obj.data.polygons:
        polygon.use_smooth = smooth
    return obj


def apply_transform(obj):
    active(obj)
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)


def cube(name, loc, size, material, bevel=.025, segments=2):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    obj = bpy.context.object
    obj.dimensions = size
    apply_transform(obj)
    if bevel:
        mod = obj.modifiers.new("Soft toy corners", "BEVEL")
        mod.width = bevel
        mod.segments = segments
        mod.affect = "EDGES"
        active(obj)
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return finish(obj, name, material)


def sphere(name, loc, size, material, segments=12, rings=6, smooth=False):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, radius=1, location=loc)
    obj = bpy.context.object
    obj.scale = tuple(v * .5 for v in size)
    apply_transform(obj)
    return finish(obj, name, material, smooth)


def cylinder(name, loc, radius, depth, material, vertices=12, bevel=.012):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc)
    obj = bpy.context.object
    if bevel:
        mod = obj.modifiers.new("Rounded rim", "BEVEL")
        mod.width = bevel
        mod.segments = 2
        active(obj)
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return finish(obj, name, material)


def stem(name, start, end, radius=.022, material="M_Leaf", vertices=8):
    direction = Vector(end) - Vector(start)
    obj = cylinder(name, (Vector(start) + Vector(end)) * .5, radius, direction.length, material, vertices, 0)
    obj.rotation_euler = direction.to_track_quat("Z", "Y").to_euler()
    apply_transform(obj)
    return obj


def mesh_object(name, vertices, faces, material):
    mesh = bpy.data.meshes.new(name + "_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    return finish(obj, name, material)


def leaf(name, base, length, width, angle, rise=.05, material="M_Leaf", thickness=.024, rings=4):
    """A solid curved pointed leaf, with a raised centre and closed underside.

    Eight vertices around each section give a broad, faceted midrib highlight;
    the four longitudinal rings preserve the distinctive leaf outline at scale.
    No negative scales, alpha cards, textures, or double-sided materials.
    """
    coords = [(0, 0, 0)]
    for i in range(1, rings + 1):
        t = i / (rings + 1)
        half_width = width * .5 * (math.sin(math.pi * t) ** .82)
        z_center = rise * t + length * .14 * math.sin(math.pi * t)
        for j in range(8):
            a = 2 * math.pi * j / 8
            coords.append((length * t, math.cos(a) * half_width,
                           z_center + math.sin(a) * thickness * .5))
    coords.append((length, 0, rise))
    tip = len(coords) - 1
    faces = []
    for j in range(8):
        faces.append((0, 1 + (j + 1) % 8, 1 + j))
    for i in range(rings - 1):
        a = 1 + i * 8
        b = a + 8
        for j in range(8):
            faces.append((a + j, a + (j + 1) % 8, b + (j + 1) % 8, b + j))
    last = 1 + (rings - 1) * 8
    for j in range(8):
        faces.append((last + j, last + (j + 1) % 8, tip))
    ca, sa = math.cos(angle), math.sin(angle)
    vertices = [(base[0] + x * ca - y * sa, base[1] + x * sa + y * ca, base[2] + z)
                for x, y, z in coords]
    return mesh_object(name, vertices, faces, material)


def join(parts, name, parent, origin=(0., 0., 0.)):
    parts = [obj for obj in parts if obj is not None]
    if not parts:
        result = bpy.data.objects.new(name, None)
        bpy.context.collection.objects.link(result)
        result.location = origin
    else:
        bpy.ops.object.select_all(action="DESELECT")
        for obj in parts:
            obj.select_set(True)
        bpy.context.view_layer.objects.active = parts[0]
        bpy.ops.object.join()
        result = bpy.context.object
        result.name = name
        result.data.name = name + "_Mesh"
        bpy.context.scene.cursor.location = origin
        bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
        apply_transform(result)
    result.parent = parent
    result["EditingTip"] = "Tab into Edit Mode; hover and press L to select an individual component island."
    return result


def plot(root):
    # Wrapper CropAnchor is already at Unity Y=.17; preserve that shared height.
    join([cube("Rounded soil", (0, 0, .085), (1.4, 1.4, .17), "M_Soil", .045, 3)], "SoilMesh", root)


def seedling(root):
    parts = [stem("Tender stem", (0, 0, .015), (0, 0, .18), .024)]
    parts.append(leaf("Left cotyledon", (0, 0, .15), .22, .16, math.radians(165), .055, thickness=.038))
    parts.append(leaf("Right cotyledon", (0, 0, .16), .22, .16, math.radians(5), .07, thickness=.038))
    join(parts, "SeedlingMesh", root)


def mint(root):
    parts = [stem("Main mint stem", (0, 0, 0), (0, 0, .86), .025, vertices=10)]
    for layer, (height, length, width, rotation) in enumerate([
            (.20, .39, .225, 15), (.38, .37, .215, 103),
            (.56, .31, .19, 23), (.72, .225, .145, 112)]):
        for side in range(2):
            angle = math.radians(rotation + 180 * side)
            base = (.033 * math.cos(angle), .033 * math.sin(angle), height)
            parts.append(stem("Mint petiole", (0, 0, height - .025), base, .013))
            parts.append(leaf("Mint leaf %d-%d" % (layer + 1, side + 1), base, length,
                              width, angle, .055 + layer * .008, thickness=.028))
    join(parts, "FoliageRoot", root)
    # Mint has no imaginary fruit: the small new tip is a growth accent.
    tips = [leaf("Fresh mint tip left", (0, 0, .80), .135, .083, math.radians(27), .10,
                 "M_Grass", .02, rings=3),
            leaf("Fresh mint tip right", (0, 0, .81), .12, .074, math.radians(207), .11,
                 "M_Grass", .02, rings=3)]
    join(tips, "ReadyAccents", root)


def chamomile(root):
    foliage, flowers = [], []
    flower_centers = [(-.23, .02, .68), (.20, -.065, .80), (.015, .23, .61)]
    for i, center in enumerate(flower_centers):
        foliage.append(stem("Chamomile flower stem", (0, 0, .01), center, .022))
        foliage.append(leaf("Chamomile stem leaf", (center[0] * .52, center[1] * .52, .24 + i * .035),
                            .24, .13, math.radians(i * 120 + 20), .02, thickness=.021))
        for j in range(7):
            angle = 2 * math.pi * j / 7 + .2 * i
            start = (center[0] + .035 * math.cos(angle), center[1] + .035 * math.sin(angle), center[2])
            flowers.append(leaf("Cream petal", start, .145, .082, angle, -.021,
                                "M_Cream", .04, rings=2))
        flowers.append(sphere("Golden flower centre", (center[0], center[1], center[2] + .031),
                              (.143, .143, .077), "M_Yellow", 12, 5))
    for i in range(3):
        foliage.append(leaf("Compact basal leaf", (0, 0, .07), .28, .165,
                            math.radians(i * 120 + 65), .045, thickness=.028))
    join(foliage, "FoliageRoot", root)
    join(flowers, "ReadyAccents", root)


def berry(name, center, width=.26, height=.31):
    # Small top shoulder, full upper half, then an unmistakable strawberry tip.
    rings = [(0, .10), (.13, .64), (.39, 1.), (.67, .94), (.89, .57), (1., .05)]
    vertices = []
    segments = 10
    for z, radius in rings:
        for i in range(segments):
            a = 2 * math.pi * i / segments
            vertices.append((center[0] + math.cos(a) * width * .5 * radius,
                             center[1] + math.sin(a) * width * .46 * radius,
                             center[2] + z * height))
    faces = []
    for layer in range(len(rings) - 1):
        for i in range(segments):
            a, b = layer * segments + i, layer * segments + (i + 1) % segments
            faces.append((a, b, b + segments, a + segments))
    faces += [tuple(reversed(range(segments))), tuple((len(rings) - 1) * segments + i for i in range(segments))]
    return mesh_object(name, vertices, faces, "M_Red")


def strawberry(root):
    foliage, fruits = [], []
    # Fan leaves lean away from the visible fruit on the camera-facing side.
    for i, degrees in enumerate([20, 72, 129, 184, 233, 290]):
        angle = math.radians(degrees)
        height = .225 + (i % 3) * .050
        base = (.04 * math.cos(angle), .04 * math.sin(angle), height)
        foliage.append(stem("Strawberry leaf stalk", (0, 0, .01), base, .021))
        foliage.append(leaf("Wide strawberry leaf", base, .36, .245, angle,
                            .085 + .025 * (i % 2), thickness=.038))
    for i, (x, y, z, size) in enumerate([(-.25, .245, .055, 1.), (.18, .31, .025, .96), (.27, -.11, .075, .82)]):
        h, w = .295 * size, .255 * size
        fruits.append(berry("Strawberry %d" % (i + 1), (x, y, z), w, h))
        top = (x, y, z + h * .92)
        for j in range(4):
            fruits.append(leaf("Fruit calyx", top, .095 * size, .050 * size,
                               j * math.pi / 2 + i * .3, -.035, thickness=.015, rings=2))
        fruits.append(stem("Fruit stalk", (x, y, z + h), (x * .78, y * .78, z + h + .055), .012))
    join(foliage, "FoliageRoot", root)
    join(fruits, "ReadyAccents", root)


def lemongrass(root):
    """Sa: a tight clump of narrow blades arching out of pale bulbs.

    Tall and vertical on purpose, so it reads apart from mint at the game camera.
    The harvest signal is a cream tie around the base plus paler outer blades:
    a shape change, not just a colour change.
    """
    foliage, accents = [], []
    # Blades stay short in reach and tall in rise: the clump has to sit inside a
    # 1.4 m plot without leaning over the neighbours.
    for i, degrees in enumerate([8, 52, 96, 140, 184, 228, 272, 316]):
        angle = math.radians(degrees)
        base = (.042 * math.cos(angle), .042 * math.sin(angle), .12 + (i % 3) * .03)
        length = .33 + (i % 4) * .034
        foliage.append(leaf("Lemongrass blade %d" % (i + 1), base, length, .058,
                            angle, .60 + (i % 3) * .07, "M_Grass", .018, rings=3))
    for i, degrees in enumerate([30, 150, 270]):
        angle = math.radians(degrees)
        foliage.append(cylinder("Lemongrass bulb %d" % (i + 1),
                                (.052 * math.cos(angle), .052 * math.sin(angle), .09),
                                .046, .19, "M_Cream", 10, .010))
    foliage.append(cylinder("Lemongrass core", (0, 0, .10), .052, .21, "M_Cream", 10, .010))
    join(foliage, "FoliageRoot", root)

    accents.append(cylinder("Harvest tie", (0, 0, .215), .105, .036, "M_Accent", 12, .008))
    for i, degrees in enumerate([70, 190, 310]):
        angle = math.radians(degrees)
        base = (.072 * math.cos(angle), .072 * math.sin(angle), .20)
        accents.append(leaf("Ripe outer blade %d" % (i + 1), base, .30, .054,
                            angle, .40, "M_Yellow", .016, rings=2))
    join(accents, "ReadyAccents", root)


def jasmine(root):
    """Nhai: a rounded bush that opens small cream stars when ripe.

    Darker foliage than the other crops so the white flowers carry the read.
    """
    foliage, flowers = [], []
    # Tallest crop of the set: it is the last unlock, so it has to look like the prize.
    for i, degrees in enumerate([15, 60, 105, 150, 195, 240, 285, 330]):
        angle = math.radians(degrees)
        height = .24 + (i % 4) * .125
        base = (.05 * math.cos(angle), .05 * math.sin(angle), height)
        foliage.append(stem("Jasmine branch", (0, 0, .02), base, .017))
        foliage.append(leaf("Jasmine leaf %d" % (i + 1), base, .215 + (i % 3) * .032,
                            .138, angle, .075 + (i % 3) * .05, thickness=.026, rings=3))
    foliage.append(stem("Jasmine trunk", (0, 0, 0), (0, 0, .40), .028, "M_Wood", 8))

    blossom_centers = [(-.165, .100, .70), (.155, .135, .58), (.050, -.185, .76), (.200, -.042, .83)]
    for i, center in enumerate(blossom_centers):
        foliage.append(stem("Blossom stalk", (0, 0, .34), center, .013))
        for j in range(5):
            angle = 2 * math.pi * j / 5 + .35 * i
            start = (center[0] + .026 * math.cos(angle), center[1] + .026 * math.sin(angle), center[2])
            flowers.append(leaf("Jasmine petal", start, .105, .070, angle, -.010,
                                "M_Cream", .030, rings=2))
        # Khong co nhuy vang: mat vang la dau rieng cua hoa cuc, de hai cay khong bi nhin lam.
        flowers.append(sphere("Jasmine centre", (center[0], center[1], center[2] + .018),
                              (.038, .038, .026), "M_Cream", 8, 4))
    join(foliage, "FoliageRoot", root)
    join(flowers, "ReadyAccents", root)


def helper(root):
    body = [cube("Cream shell", (0, 0, 0), (.64, .49, .58), "M_Cream", .13, 4),
            cube("Teal side left", (-.315, -.015, -.035), (.075, .29, .30), "M_Accent", .035, 3),
            cube("Teal side right", (.315, -.015, -.035), (.075, .29, .30), "M_Accent", .035, 3),
            cylinder("Hover ring", (0, 0, -.289), .205, .048, "M_Accent", 20, .01),
            cylinder("Hover centre", (0, 0, -.317), .116, .026, "M_Yellow", 16, .006)]
    face = [cube("Dark face plate", (0, .238, .015), (.43, .077, .29), "M_Dark", .067, 4)]
    for x in (-.106, .106):
        face.append(cube("Warm eye", (x, .282, .043), (.052, .025, .092), "M_Yellow", .024, 3))
    face.append(cube("Little smile", (0, .284, -.058), (.080, .017, .018), "M_Cream", .008, 2))
    antenna = [stem("Antenna stalk", (0, -.03, .27), (.045, -.03, .43), .021, "M_Dark", 10),
               sphere("Antenna light", (.045, -.03, .449), (.10, .10, .10), "M_Yellow", 12, 6)]
    join(body, "Body", root)
    join(face, "Face", root, (0, .238, 0))
    join(antenna, "Antenna", root, (0, -.03, .27))


def tea_station(root):
    parts = []
    # Three metres wide, simple closed masses, clear mint/cream shop silhouette.
    for x in (-1.23, 1.23):
        parts.append(cube("Counter leg", (x, 0, .43), (.20, .70, .86), "M_Wood", .025, 2))
    parts += [cube("Lower shelf", (0, -.045, .21), (2.66, .67, .12), "M_Wood", .025, 2),
              cube("Cream counter fascia", (0, .323, .59), (2.68, .12, .50), "M_Cream", .055, 3),
              cube("Wood counter top", (0, .045, .925), (2.93, .91, .15), "M_Wood", .045, 3),
              cube("Inset front accent", (0, .391, .60), (.53, .025, .21), "M_Accent", .060, 3)]
    for x in (-1.29, 1.29):
        parts.append(cube("Canopy post", (x, -.31, 1.43), (.11, .12, .97), "M_Wood", .015, 2))
    parts += [cube("Mint canopy", (0, -.15, 1.91), (3.10, 1.06, .14), "M_Accent", .055, 3),
              cube("Canopy cream edge", (0, .38, 1.86), (3.00, .065, .135), "M_Cream", .025, 2),
              cube("Machine shell", (-.37, .02, 1.28), (.93, .56, .55), "M_Cream", .07, 3),
              cube("Machine top", (-.37, .005, 1.565), (.94, .57, .10), "M_Accent", .035, 3),
              cube("Machine front", (-.37, .309, 1.30), (.66, .035, .31), "M_Accent", .035, 3),
              cube("Drip tray", (-.37, .375, 1.033), (.76, .41, .055), "M_Dark", .018, 2)]
    # Mat truoc cua quay nam o Blender +Y, sang Unity thanh +Z (do bang cube chuan).
    # SceneFactory xoay wrapper de mat nay quay ve phia nguoi choi.
    parts.append(stem("Tea nozzle", (-.49, .30, 1.315), (-.49, .465, 1.315), .038, "M_Dark", 12))
    parts.append(stem("Tea spout lip", (-.49, .465, 1.315), (-.49, .465, 1.257), .038, "M_Dark", 12))
    parts.append(cylinder("Tea cup", (-.49, .46, 1.137), .096, .163, "M_Cream", 16, .012))
    parts.append(cylinder("Tea surface", (-.49, .46, 1.222), .075, .006, "M_Soil", 16, 0))
    parts.append(sphere("Cup handle", (-.379, .46, 1.148), (.074, .060, .105), "M_Cream", 10, 5))
    # A generous cylindrical tea tin on the right balances the espresso unit.
    parts.append(cylinder("Tea tin", (.55, -.015, 1.215), .205, .43, "M_Accent", 16, .022))
    parts.append(cylinder("Tea tin lid", (.55, -.015, 1.445), .225, .060, "M_Cream", 16, .015))
    parts.append(sphere("Tea tin knob", (.55, -.015, 1.486), (.10, .10, .067), "M_Wood", 10, 5))
    parts.append(cube("Tea tin label", (.55, .190, 1.26), (.20, .025, .13), "M_Cream", .020, 2))
    join(parts, "StationMesh", root)
    status = sphere("Status lamp", (-.145, .342, 1.408), (.075, .038, .075), "M_Yellow", 12, 6)
    join([status], "StatusAnchor", root, (-.145, .342, 1.408))
    # The runtime animates this semantic group; leave its authored rest location
    # above the cup. The geometry is intentionally visible only while brewing.
    steam_pos = (-.49, .46, 1.275)
    steam = [sphere("Steam puff lower", steam_pos, (.065, .057, .085), "M_Cream", 10, 5),
             sphere("Steam puff upper", (-.46, .46, 1.355), (.043, .043, .065), "M_Cream", 10, 5)]
    join(steam, "SteamAnchor", root, steam_pos)


def background_tree(root):
    parts = [cylinder("Tree trunk", (0, 0, .52), .14, 1.04, "M_Wood", 10, .025),
             stem("Tree branch left", (0, 0, .61), (-.27, .04, 1.09), .071, "M_Wood", 8),
             stem("Tree branch right", (0, 0, .72), (.25, -.03, 1.18), .067, "M_Wood", 8)]
    for name, pos, size in [
        ("Tall crown", (.02, -.045, 1.52), (.88, .79, 1.10)),
        ("Left crown", (-.34, .03, 1.28), (.80, .73, .82)),
        ("Right crown", (.35, -.055, 1.31), (.73, .73, .88))]:
        parts.append(sphere(name, pos, size, "M_Leaf", 12, 7))
    join(parts, "TreeMesh", root)


def bush(root):
    parts = []
    for name, pos, size in [("Bush centre", (0, -.04, .30), (.65, .55, .60)),
                            ("Bush left", (-.29, .04, .215), (.48, .45, .43)),
                            ("Bush right", (.27, .08, .235), (.50, .45, .47))]:
        parts.append(sphere(name, pos, size, "M_Leaf", 10, 6))
    join(parts, "BushMesh", root)


def fence(root):
    parts = []
    for x in (-.71, .71):
        parts.append(cube("Fence post", (x, 0, .37), (.16, .16, .74), "M_Wood", .025, 2))
        parts.append(cube("Post cap", (x, 0, .74), (.20, .20, .09), "M_Cream", .025, 2))
    for z in (.26, .54):
        parts.append(cube("Fence rail", (0, .015, z), (1.42, .10, .105), "M_Wood", .018, 2))
    join(parts, "FenceMesh", root)


def rock(root):
    rng = random.Random(SEED + 11)
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=3, radius=1)
    obj = bpy.context.object
    for vertex in obj.data.vertices:
        x, y, z = vertex.co
        factor = rng.uniform(.94, 1.065)
        vertex.co = (x * .33 * factor, y * .27 * factor,
                     max(.0, z * .245 * factor + .18))
    obj = finish(obj, "Faceted warm stone", "M_Cream")
    join([obj], "RockMesh", root)


def calibration(root):
    join([cube("Metre cube", (0, 0, .5), (1, 1, 1), "M_Cream", 0)], "CalibrationCube", root)
    # Quy uoc cua bo asset: mat truoc nam o Blender +Y, sang Unity thanh +Z.
    # Mat robot cung dat o +Y, nen cube chuan phai theo cung huong do.
    join([cube("Forward arrow block", (0, .6, .5), (.14, .20, .14), "M_Red", 0)],
         "FrontMarker", root, (0, .6, .5))
    join([cube("Up arrow block", (0, 0, 1.1), (.14, .14, .20), "M_Yellow", 0)],
         "UpMarker", root, (0, 0, 1.1))


BUILDERS = {
    "SM_Plot": plot, "SM_Seedling": seedling, "SM_Mint": mint,
    "SM_Chamomile": chamomile, "SM_Strawberry": strawberry,
    "SM_Lemongrass": lemongrass, "SM_Jasmine": jasmine, "SM_Helper": helper,
    "SM_TeaStation": tea_station, "SM_BackgroundTree": background_tree,
    "SM_Bush": bush, "SM_Fence": fence, "SM_Rock": rock,
    "SM_CalibrationCube": calibration,
}


def asset_summary(name, root, project):
    meshes = [obj for obj in root.children_recursive if obj.type == "MESH"]
    depsgraph = bpy.context.evaluated_depsgraph_get()
    evaluated = [obj.evaluated_get(depsgraph) for obj in meshes]
    points = [obj.matrix_world @ Vector(corner) for obj in evaluated for corner in obj.bound_box]
    mins = [min(p[i] for p in points) for i in range(3)]
    maxs = [max(p[i] for p in points) for i in range(3)]
    triangles = 0
    for obj in evaluated:
        mesh = obj.to_mesh()
        mesh.calc_loop_triangles()
        triangles += len(mesh.loop_triangles)
        obj.to_mesh_clear()
    return {
        "assetId": ASSET_IDS[name], "name": name, "source": f"SourceArt/Blender/{name}.blend",
        "export": f"Assets/Art/Models/{name}.fbx", "triangles": triangles,
        "renderers": len(meshes), "materials": sorted({mat.name for obj in meshes for mat in obj.data.materials}),
        "boundsBlender": {"min": [round(v, 5) for v in mins], "max": [round(v, 5) for v in maxs]},
        "dimensionsUnityMetres": [round(maxs[0] - mins[0], 5), round(maxs[2] - mins[2], 5), round(maxs[1] - mins[1], 5)],
        "rootScale": list(root.scale),
        "parts": [{"name": obj.name, "originBlender": [round(v, 5) for v in obj.location],
                   "materialSlots": [m.name for m in obj.data.materials]} for obj in meshes],
        "license": "Original project art; no third-party model or texture inputs.",
        "status": "Exported; Unity import and gameplay-camera verification required.",
    }


def save_export(name, root, project, save_source=True):
    source = project / "SourceArt" / "Blender" / (name + ".blend")
    target = project / "Assets" / "Art" / "Models" / (name + ".fbx")
    source.parent.mkdir(parents=True, exist_ok=True)
    target.parent.mkdir(parents=True, exist_ok=True)
    bpy.context.scene.cursor.location = (0, 0, 0)
    scene = bpy.context.scene
    scene["Generator"] = "SourceArt/Blender/generate_garden.py"
    scene["Pipeline"] = "FBX -Z forward / Y up, scale 1, bake_space_transform; Unity bakeAxisConversion=true."
    scene["Palette"] = json.dumps(PALETTE, sort_keys=True)
    scene["Seed"] = SEED
    # Material-coloured solid viewport opens usefully even without a render rig.
    for screen in bpy.data.screens:
        for area in screen.areas:
            if area.type == "VIEW_3D":
                area.spaces.active.shading.color_type = "MATERIAL"
                area.spaces.active.shading.light = "STUDIO"
                area.spaces.active.region_3d.view_distance = 4.0 if name == "SM_TeaStation" else 2.3
                area.spaces.active.region_3d.view_location = (0, 0, .55)
    bpy.ops.object.select_all(action="DESELECT")
    root.select_set(True)
    for obj in root.children_recursive:
        if obj.type in {"MESH", "EMPTY"}:
            obj.select_set(True)
    bpy.context.view_layer.objects.active = root
    bpy.context.view_layer.update()
    summary = asset_summary(name, root, project)
    if save_source:
        bpy.ops.wm.save_as_mainfile(filepath=str(source), check_existing=False)
    bpy.ops.export_scene.fbx(filepath=str(target), use_selection=True,
        object_types={"MESH", "EMPTY"}, global_scale=1., apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_UNITS", use_space_transform=True,
        bake_space_transform=True, axis_forward="-Z", axis_up="Y",
        use_mesh_modifiers=True, mesh_smooth_type="FACE", use_triangles=True,
        add_leaf_bones=False, bake_anim=False, path_mode="AUTO", embed_textures=False,
        use_custom_props=False)
    return summary


def main():
    args_after_separator = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project", required=True, type=Path)
    parser.add_argument("--only", choices=tuple(BUILDERS))
    parser.add_argument("--overwrite", action="store_true", help="Explicitly regenerate existing .blend source and FBX files.")
    parser.add_argument("--export-existing", type=Path,
                        help="Re-export an edited .blend to its stable FBX path; never save or regenerate the source.")
    args = parser.parse_args(args_after_separator)
    project = args.project.resolve()
    if not (project / "Assets").is_dir():
        parser.error("--project must be the Unity project directory containing Assets")
    manifest_path = project / "SourceArt" / "Blender" / "asset-manifest.json"
    manifest = {"generator": "generate_garden.py", "blenderVersion": bpy.app.version_string,
                "seed": SEED, "palette": {k: "#" + v for k, v in PALETTE.items()},
                "coordinateConvention": "Blender +Z up; +Y is the model front and maps to Unity +Z (verified by SM_CalibrationCube). FBX -Z Forward, Y Up.",
                "exportSettings": {"axisForward": "-Z", "axisUp": "Y", "globalScale": 1,
                    "applyUnitScale": True, "applyScaleOptions": "FBX_SCALE_UNITS", "bakeSpaceTransform": True,
                    "useSelection": True, "objectTypes": ["MESH", "EMPTY"], "useMeshModifiers": True,
                    "useTriangles": True, "bakeAnimation": False, "embedTextures": False,
                    "unityBakeAxisConversion": True, "unityGlobalScale": 1},
                "assets": {}}
    if manifest_path.exists():
        previous = json.loads(manifest_path.read_text(encoding="utf-8"))
        manifest["assets"] = previous.get("assets", {})
    if args.export_existing:
        if args.overwrite:
            parser.error("--export-existing preserves source files; do not combine it with --overwrite")
        source = args.export_existing
        if not source.is_absolute():
            source = project / source
        source = source.resolve()
        if not source.is_file() or source.suffix.lower() != ".blend":
            parser.error("--export-existing must point to an existing .blend source")
        name = args.only or source.stem
        if name not in BUILDERS:
            parser.error("Source filename must match SM_* asset name, or supply --only with that name")
        bpy.ops.wm.open_mainfile(filepath=str(source), load_ui=False, use_scripts=False)
        root = bpy.data.objects.get(name)
        if root is None or root.type != "EMPTY":
            parser.error(f"Source must contain an EMPTY asset root named {name}")
        if not any(obj.type == "MESH" for obj in root.children_recursive):
            parser.error("Asset root has no mesh descendants")
        if abs(bpy.context.scene.unit_settings.scale_length - 1.) > .00001:
            parser.error("Source unit scale must be 1 metre before export")
        if root.parent or root.location.length > .00001 or any(abs(v) > .00001 for v in root.rotation_euler) or any(abs(v - 1.) > .00001 for v in root.scale):
            parser.error("Asset root must be unparented at origin, rotation 0, scale 1")
        entry = save_export(name, root, project, save_source=False)
        try:
            entry["source"] = source.relative_to(project).as_posix()
        except ValueError:
            entry["source"] = str(source)
        entry["lastExportMode"] = "Existing edited source; source file left unchanged."
        manifest["assets"][name] = entry
        manifest_path.parent.mkdir(parents=True, exist_ok=True)
        manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        print(f"RE-EXPORTED: {name}; source preserved; {entry['triangles']} triangles")
        return
    names = [args.only] if args.only else list(BUILDERS)
    for name in names:
        source = project / "SourceArt" / "Blender" / (name + ".blend")
        target = project / "Assets" / "Art" / "Models" / (name + ".fbx")
        if not args.overwrite and (source.exists() or target.exists()):
            print(f"PROTECTED: {name} already exists; use --overwrite to regenerate.")
            continue
        random.seed(SEED + list(BUILDERS).index(name))
        root = reset_scene(name)
        BUILDERS[name](root)
        manifest["assets"][name] = save_export(name, root, project)
        entry = manifest["assets"][name]
        print(f"EXPORTED: {name}: {entry['triangles']} triangles, {entry['renderers']} renderers, {entry['dimensionsUnityMetres']} m")
    manifest_path.parent.mkdir(parents=True, exist_ok=True)
    manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"Manifest: {manifest_path}")


if __name__ == "__main__":
    main()
