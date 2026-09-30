import os
import re
import unreal

LOG_PREFIX = "PROJECT1864 Livgarden import"
PROJECT_DIR = unreal.Paths.convert_relative_path_to_full(unreal.Paths.project_dir())
SOURCE_ROOT = os.path.join(
    PROJECT_DIR,
    "SourceAssets",
    "Danish",
    "Livgarden1864",
)

MESH_SOURCE = os.path.join(
    SOURCE_ROOT,
    "Mesh",
    "DK_Livgarden_1864_Apose_textured_skeleton.fbx",
)

ANIMATION_SOURCE_DIR = os.path.join(
    SOURCE_ROOT,
    "Animations",
)

RIFLE_SOURCE = os.path.join(
    SOURCE_ROOT,
    "Weapons",
    "Rifle_1_textured.glb",
)

RIFLE_BAYONET_SOURCE = os.path.join(
    SOURCE_ROOT,
    "Weapons",
    "Rifle_Bayonet_1_textured.glb",
)

MESH_DEST = "/Game/Units/Danish/Livgarden1864/Mesh"
ANIMATION_DEST = "/Game/Units/Danish/Livgarden1864/Animations"
WEAPON_DEST = "/Game/Units/Danish/Livgarden1864/Weapons"
SHARED_SKELETON_DEST = "/Game/Units/Human/Skeletons"

MESH_ASSET = "SK_DK_Livgarden_1864"
SHARED_SKELETON_ASSET = "SK_Human_1864"
RIFLE_ASSET = "SM_Rifle_1"
RIFLE_BAYONET_ASSET = "SM_Rifle_Bayonet_1"


def log(message):
    unreal.log("{}: {}".format(LOG_PREFIX, message))


def warn(message):
    unreal.log_warning("{}: {}".format(LOG_PREFIX, message))


def fail(message):
    raise RuntimeError("{}: {}".format(LOG_PREFIX, message))


def asset_safe_name(value):
    value = re.sub(r"[^A-Za-z0-9_]+", "_", value)
    value = value.strip("_")
    return value or "Unnamed"


def require_file(path):
    if not os.path.isfile(path):
        fail("Missing source file: {}".format(path))


def ensure_source_layout():
    require_file(MESH_SOURCE)
    require_file(RIFLE_SOURCE)
    require_file(RIFLE_BAYONET_SOURCE)

    if not os.path.isdir(ANIMATION_SOURCE_DIR):
        fail("Missing animation folder: {}".format(ANIMATION_SOURCE_DIR))

    animation_files = sorted(
        os.path.join(ANIMATION_SOURCE_DIR, name)
        for name in os.listdir(ANIMATION_SOURCE_DIR)
        if name.lower().endswith(".fbx")
    )

    if not animation_files:
        fail("No FBX animations found in {}".format(ANIMATION_SOURCE_DIR))

    log("Found {} animation FBX files".format(len(animation_files)))
    return animation_files


def delete_asset_if_present(asset_path):
    if unreal.EditorAssetLibrary.does_asset_exist(asset_path):
        unreal.EditorAssetLibrary.delete_asset(asset_path)


def rename_imported_asset(imported_paths, target_path, expected_class):
    candidate = None

    for object_path in imported_paths:
        obj = unreal.EditorAssetLibrary.load_asset(object_path)
        if obj and isinstance(obj, expected_class):
            candidate = object_path
            break

    if candidate is None:
        return None

    if candidate != target_path:
        delete_asset_if_present(target_path)

        if not unreal.EditorAssetLibrary.rename_asset(candidate, target_path):
            fail("Could not rename {} to {}".format(candidate, target_path))

    return unreal.EditorAssetLibrary.load_asset(target_path)


def run_import_task(task):
    unreal.AssetToolsHelpers.get_asset_tools().import_asset_tasks([task])
    return list(task.get_editor_property("imported_object_paths"))


def make_task(filename, destination_path, options=None):
    task = unreal.AssetImportTask()
    task.set_editor_property("filename", filename)
    task.set_editor_property("destination_path", destination_path)
    task.set_editor_property("automated", True)
    task.set_editor_property("replace_existing", True)
    task.set_editor_property("replace_existing_settings", True)
    task.set_editor_property("save", True)

    if options is not None:
        task.set_editor_property("options", options)

    return task


def import_skeletal_mesh():
    target_path = "{}/{}".format(MESH_DEST, MESH_ASSET)
    delete_asset_if_present(target_path)

    options = unreal.FbxImportUI()
    options.set_editor_property("import_as_skeletal", True)
    options.set_editor_property("import_mesh", True)
    options.set_editor_property("import_animations", False)
    options.set_editor_property("import_materials", True)
    options.set_editor_property("import_textures", True)
    options.set_editor_property("create_physics_asset", False)
    options.set_editor_property("automated_import_should_detect_type", False)

    try:
        options.set_editor_property(
            "mesh_type_to_import",
            unreal.FBXImportType.FBXIT_SKELETAL_MESH,
        )
    except Exception:
        pass

    task = make_task(MESH_SOURCE, MESH_DEST, options)

    try:
        task.set_editor_property("factory", unreal.FbxFactory())
    except Exception:
        pass

    imported_paths = run_import_task(task)

    mesh = rename_imported_asset(
        imported_paths,
        target_path,
        unreal.SkeletalMesh,
    )

    if mesh is None:
        mesh = unreal.EditorAssetLibrary.load_asset(target_path)

    if mesh is None:
        fail("Skeletal mesh import did not create {}".format(target_path))

    skeleton = mesh.get_editor_property("skeleton")

    if skeleton is None:
        fail("Imported skeletal mesh has no Skeleton asset")

    shared_skeleton_path = "{}/{}".format(
        SHARED_SKELETON_DEST,
        SHARED_SKELETON_ASSET,
    )

    current_skeleton_path = skeleton.get_path_name()

    if current_skeleton_path != shared_skeleton_path:
        delete_asset_if_present(shared_skeleton_path)

        if not unreal.EditorAssetLibrary.rename_asset(
            current_skeleton_path,
            shared_skeleton_path,
        ):
            fail(
                "Could not rename shared Skeleton {} to {}".format(
                    current_skeleton_path,
                    shared_skeleton_path,
                )
            )

        skeleton = unreal.EditorAssetLibrary.load_asset(
            shared_skeleton_path
        )

    if skeleton is None:
        fail("Shared skeleton could not be loaded after rename")

    log("Skeletal mesh imported: {}".format(target_path))
    log("Shared skeleton: {}".format(skeleton.get_path_name()))
    return mesh, skeleton


def import_animation(filename, skeleton):
    source_stem = os.path.splitext(os.path.basename(filename))[0]
    asset_name = "A_{}".format(asset_safe_name(source_stem))
    target_path = "{}/{}".format(ANIMATION_DEST, asset_name)

    delete_asset_if_present(target_path)

    options = unreal.FbxImportUI()
    options.set_editor_property("import_as_skeletal", True)
    options.set_editor_property("import_mesh", False)
    options.set_editor_property("import_animations", True)
    options.set_editor_property("import_materials", False)
    options.set_editor_property("import_textures", False)
    options.set_editor_property("automated_import_should_detect_type", False)
    options.set_editor_property("skeleton", skeleton)

    try:
        options.set_editor_property(
            "mesh_type_to_import",
            unreal.FBXImportType.FBXIT_ANIMATION,
        )
    except Exception:
        pass

    try:
        options.set_editor_property("override_animation_name", asset_name)
    except Exception:
        pass

    task = make_task(filename, ANIMATION_DEST, options)

    try:
        task.set_editor_property("factory", unreal.FbxFactory())
    except Exception:
        pass

    imported_paths = run_import_task(task)

    sequence = rename_imported_asset(
        imported_paths,
        target_path,
        unreal.AnimSequence,
    )

    if sequence is None:
        sequence = unreal.EditorAssetLibrary.load_asset(target_path)

    if sequence is None:
        warn("Animation import failed: {}".format(filename))
        return None

    sequence_skeleton = sequence.get_editor_property("skeleton")
    if sequence_skeleton != skeleton:
        warn(
            "Skeleton mismatch after import for {}: {}".format(
                asset_name,
                sequence_skeleton.get_path_name()
                if sequence_skeleton
                else "None",
            )
        )
        return None

    return sequence


def import_static_mesh(filename, asset_name):
    target_path = "{}/{}".format(WEAPON_DEST, asset_name)
    delete_asset_if_present(target_path)

    task = make_task(filename, WEAPON_DEST)
    imported_paths = run_import_task(task)

    mesh = rename_imported_asset(
        imported_paths,
        target_path,
        unreal.StaticMesh,
    )

    if mesh is None:
        mesh = unreal.EditorAssetLibrary.load_asset(target_path)

    if mesh is None:
        warn("Static mesh import failed: {}".format(filename))
        return None

    return mesh


def main():
    animation_files = ensure_source_layout()

    unreal.EditorAssetLibrary.make_directory(MESH_DEST)
    unreal.EditorAssetLibrary.make_directory(ANIMATION_DEST)
    unreal.EditorAssetLibrary.make_directory(WEAPON_DEST)
    unreal.EditorAssetLibrary.make_directory(SHARED_SKELETON_DEST)

    mesh, skeleton = import_skeletal_mesh()

    imported_animation_count = 0

    with unreal.ScopedSlowTask(
        len(animation_files),
        "PROJECT 1864 - importing Livgarden animations",
    ) as slow_task:
        slow_task.make_dialog(True)

        for filename in animation_files:
            if slow_task.should_cancel():
                warn("Import cancelled by user")
                break

            slow_task.enter_progress_frame(
                1,
                os.path.basename(filename),
            )

            if import_animation(filename, skeleton):
                imported_animation_count += 1

    rifle = import_static_mesh(
        RIFLE_SOURCE,
        RIFLE_ASSET,
    )

    rifle_bayonet = import_static_mesh(
        RIFLE_BAYONET_SOURCE,
        RIFLE_BAYONET_ASSET,
    )

    unreal.EditorAssetLibrary.save_directory(
        "/Game/Units/Danish/Livgarden1864",
        only_if_is_dirty=False,
        recursive=True,
    )

    log(
        "DONE - mesh={}, animations={}/{}, rifle={}, rifle+bayonet={}".format(
            mesh is not None,
            imported_animation_count,
            len(animation_files),
            rifle is not None,
            rifle_bayonet is not None,
        )
    )

    if imported_animation_count != len(animation_files):
        warn(
            "{} animation(s) failed skeleton/import validation".format(
                len(animation_files) - imported_animation_count
            )
        )


if __name__ == "__main__":
    main()
