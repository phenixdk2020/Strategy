using UnityEditor;
using UnityEngine;

/// <summary>
/// Automatically configures the Mixamo-rigged 1864 infantry base model
/// as a Unity Humanoid avatar when the FBX is imported into the test branch.
/// </summary>
public sealed class Infantry1864HumanoidImporter : AssetPostprocessor
{
    private const string TargetFileName = "Infantry_shape_T-Pose.fbx";

    private void OnPreprocessModel()
    {
        if (!assetPath.EndsWith(TargetFileName, System.StringComparison.OrdinalIgnoreCase))
            return;

        var importer = (ModelImporter)assetImporter;

        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;

        // Keep the source hierarchy intact while we validate the Mixamo rig.
        importer.optimizeGameObjects = false;

        // T-pose file is primarily the base character/rig.
        importer.importAnimation = true;

        Debug.Log($"[1864 Rig Import] Configured '{assetPath}' as Humanoid / Create From This Model.");
    }
}
