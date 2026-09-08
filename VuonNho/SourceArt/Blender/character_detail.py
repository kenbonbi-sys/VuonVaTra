"""Detail the existing farm characters without changing their animation contract.

Regeneration hook: call enrich_character(root, "gardener" or "worker") after
generate_garden.gardener/worker and before save_export. This module also upgrades
the existing .blend files directly with --project PATH. Existing palette remaps,
mesh-group names, shoulder/hip pivots, FBX axes and outer bounds are preserved.
"""
from __future__ import annotations

import argparse
import json
import math
from pathlib import Path
import sys

import bpy
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
import generate_garden as garden

VERSION = 1


def _merge(group, parts):
    """Join detail into the original renderer, retaining its name and pivot."""
    bpy.ops.object.select_all(action="DESELECT")
    group.select_set(True)
    for part in parts:
        part.select_set(True)
    bpy.context.view_layer.objects.active = group
    bpy.ops.object.join()


def _replace(group, parts):
    replacement = garden.join(parts, "DetailedHeadTemporary", group.parent, tuple(group.location))
    old_mesh = group.data
    group.data = replacement.data
    bpy.data.objects.remove(replacement, do_unlink=True)
    if old_mesh.users == 0:
        bpy.data.meshes.remove(old_mesh)
    group.data.name = group.name + "_Mesh"


def _ring(name, radius, height, material, tube=.0035, vertices=32):
    bpy.ops.mesh.primitive_torus_add(major_segments=vertices, minor_segments=4,
        location=(0, 0, height), major_radius=radius, minor_radius=tube)
    return garden.finish(bpy.context.object, name, material)


def _head():
    sphere, cube = garden.sphere, garden.cube
    parts = [sphere("Face volume", (0, 0, 1.045), (.30, .285, .30), "M_Wood", 20, 12, True),
             sphere("Hair volume", (0, -.045, 1.055), (.305, .255, .29), "M_Dark", 16, 8, True)]
    for side in (-1, 1):
        x = side * .066
        parts += [sphere("Ear", (side * .145, -.003, 1.034), (.060, .054, .080), "M_Wood", 12, 6, True),
                  sphere("Inner ear", (side * .163, .022, 1.034), (.020, .012, .037), "M_Soil", 10, 5, True),
                  sphere("Eye white", (x, .126, 1.048), (.051, .031, .068), "M_Cream", 12, 8, True),
                  sphere("Eye pupil", (x - side * .003, .145, 1.045), (.030, .020, .048), "M_Dark", 12, 8, True),
                  sphere("Eye glint", (x - .006, .155, 1.057), (.009, .005, .012), "M_Cream", 8, 4, True),
                  cube("Eyebrow", (x, .126, 1.094), (.047, .017, .012), "M_Dark", .005, 2),
                  sphere("Cheek", (side * .091, .116, 1.004), (.030, .012, .019), "M_Red", 10, 5, True)]
    parts += [sphere("Small nose", (0, .150, 1.024), (.040, .052, .042), "M_Wood", 12, 8, True),
              cube("Smile", (0, .132, .978), (.050, .022, .009), "M_Dark", .004, 2)]
    for i, x in enumerate((-.091, -.043, .010, .064, .108)):
        tuft = cube("Swept fringe", (x, .105, 1.123 + .007 * (i % 2)),
                    (.060, .069, .059), "M_Dark", .017, 3)
        tuft.rotation_euler.y = math.radians(-14 + i * 5)
        parts.append(tuft)
    return parts


def enrich_character(root, kind):
    kind = kind.lower().removeprefix("sm_")
    if kind not in {"gardener", "worker"}:
        raise ValueError("Character kind must be gardener or worker")
    if root.get("CharacterDetailVersion") == VERSION:
        return
    garden.MATERIALS.clear()
    for name in garden.PALETTE:
        material = bpy.data.materials.get(name)
        if material is None:
            # Blender removes unused slots on save; reuse the canonical palette for new accents.
            value = garden.PALETTE[name]
            rgba = tuple(garden.linear_channel(int(value[i:i + 2], 16) / 255) for i in (0, 2, 4)) + (1.,)
            material = bpy.data.materials.new(name)
            material.diffuse_color = rgba
            material.use_nodes = True
            shader = material.node_tree.nodes.get("Principled BSDF")
            shader.inputs["Base Color"].default_value = rgba
            shader.inputs["Roughness"].default_value = .68
            material["PaletteHex"] = "#" + value
        garden.MATERIALS[name] = material
    groups = {obj.name: obj for obj in root.children_recursive if obj.type == "MESH"}
    required = {"Body", "Head", "Hat", "LegLeft", "LegRight"}
    if kind == "worker":
        required |= {"ArmLeft", "ArmRight"}
    if not required.issubset(groups):
        raise ValueError("Missing character pivots: " + str(required - groups.keys()))
    pivots = {name: obj.matrix_world.copy() for name, obj in groups.items()}
    before = garden.asset_summary(root.name, root, Path("."))
    _replace(groups["Head"], _head())

    cube, sphere, cylinder = garden.cube, garden.sphere, garden.cylinder
    apron = "M_Cream" if kind == "gardener" else "M_Accent"
    contrast = "M_Accent" if kind == "gardener" else "M_Cream"
    body = [cube("Apron pocket", (0, .174, .641), (.208, .022, .108), contrast, .016, 3),
            cube("Pocket opening", (0, .187, .680), (.165, .006, .008), "M_Dark", .003, 2),
            cube("Apron bottom hem", (0, .169, .437), (.255, .009, .014), contrast, .006, 2),
            cube("Waist belt", (0, 0, .474), (.389, .276, .035), "M_Wood", .014, 2),
            cube("Belt buckle", (0, .176, .477), (.044, .017, .032), "M_Yellow", .006, 2),
            cube("Pocket tea badge", (.054, .190, .628), (.042, .007, .038), apron, .005, 2)]
    for side in (-1, 1):
        collar = cube("Shirt collar", (side * .055, .119, .875), (.081, .036, .055), contrast, .010, 2)
        collar.rotation_euler.y = math.radians(side * 23)
        body.append(collar)
        body.append(sphere("Strap button", (side * .10, .162, .844), (.023, .012, .023), "M_Yellow", 10, 6, True))
        body.append(cube("Apron side piping", (side * .146, .169, .610), (.008, .009, .285), contrast, .003, 2))
        # The farmer's arms are part of Body; the worker's detail follows shoulder pivots.
        cuff = [cube("Folded sleeve cuff", (side * .245, .006, .618), (.111, .139, .036), contrast, .012, 2),
                sphere("Thumb", (side * .204, .060, .460), (.035, .043, .046), "M_Wood", 10, 6, True)]
        if kind == "worker":
            _merge(groups["ArmLeft" if side < 0 else "ArmRight"], cuff)
        else:
            body.extend(cuff)
        leg = groups["LegLeft" if side < 0 else "LegRight"]
        _merge(leg, [cube("Boot sole", (side * .10, .045, .011), (.172, .252, .020), "M_Dark", .009, 2),
                     cube("Boot tongue", (side * .10, .093, .084), (.095, .074, .022), "M_Soil", .009, 2),
                     cube("Trouser cuff", (side * .10, -.002, .120), (.164, .180, .028), contrast, .012, 2)])
    _merge(groups["Body"], body)

    hat = []
    if kind == "gardener":
        hat.append(_ring("Bound straw rim", .266, 1.160, "M_Cream", .004))
        for height in (1.192, 1.237, 1.285):
            radius = .27 * (1.3775 - height) / .225
            hat.append(_ring("Woven straw ring", radius, height, "M_Cream", .0023))
        for i in range(8):
            angle = 2 * math.pi * i / 8
            hat.append(garden.stem("Straw rib",
                (.035 * math.cos(angle), .035 * math.sin(angle), 1.350),
                (.255 * math.cos(angle), .255 * math.sin(angle), 1.165), .0026, "M_Wood", 6))
        for side in (-1, 1):
            hat.append(garden.stem("Chin cord", (side * .17, .012, 1.155),
                (side * .091, .069, .946), .0045, "M_Cream", 6))
    else:
        hat += [_ring("Cap lower piping", .257, 1.175, "M_Leaf", .004),
                _ring("Cap crown seam", .227, 1.307, "M_Cream", .0024),
                cylinder("Cap covered button", (0, 0, 1.306), .026, .014, "M_Leaf", 12, .003),
                cube("Cap front badge", (0, .263, 1.241), (.070, .011, .039), "M_Cream", .009, 3)]
        for side in (-1, 1):
            hat.append(sphere("Cap eyelet", (side * .215, .117, 1.258), (.015, .017, .015), "M_Leaf", 8, 4))
    _merge(groups["Hat"], hat)
    bpy.context.view_layer.update()
    for name, matrix in pivots.items():
        error = max(abs(a - b) for row_a, row_b in zip(matrix, groups[name].matrix_world) for a, b in zip(row_a, row_b))
        if error > 1e-6:
            raise AssertionError("Animation pivot moved: " + name)
    after = garden.asset_summary(root.name, root, Path("."))
    for axis in range(3):
        for edge in ("min", "max"):
            if abs(before["boundsBlender"][edge][axis] - after["boundsBlender"][edge][axis]) > .0001:
                raise AssertionError("Outer character bounds changed: " + json.dumps({"before": before["boundsBlender"], "after": after["boundsBlender"]}))
    if after["renderers"] != before["renderers"]:
        raise AssertionError("Detail must use existing character renderers")
    root["CharacterDetailVersion"] = VERSION
    root["CharacterDetail"] = "Tailored clothing, expressive face, bound hat; original animation pivots and palette."


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project", required=True, type=Path)
    args = parser.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])
    project = args.project.resolve()
    for kind in ("gardener", "worker"):
        name = "SM_" + kind.title()
        source = project / "SourceArt" / "Blender" / (name + ".blend")
        bpy.ops.wm.open_mainfile(filepath=str(source))
        root = bpy.data.objects.get(name)
        if root is None:
            raise ValueError("Missing character root in " + str(source))
        enrich_character(root, kind)
        bpy.context.preferences.filepaths.save_version = 0
        summary = garden.save_export(name, root, project)
        print("CHARACTER_DETAIL " + json.dumps(summary, ensure_ascii=False))


if __name__ == "__main__":
    main()
