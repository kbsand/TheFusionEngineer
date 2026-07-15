using TheFusionEngineer.Core;
using UnityEditor;
using UnityEngine;

namespace TheFusionEngineer.Editor
{
    [InitializeOnLoad]
    internal static class GameSfxLibraryConfigurator
    {
        private const string LibraryPath = "Assets/_Project/Resources/GameSfxLibrary.asset";
        private const string FootstepPath = "Assets/Footsteps - Essentials/Footsteps_Metal/Footsteps_Metal_Run/Footsteps_MetalV1_Run_06.wav";
        private const string HoldPath = "Assets/Casual Game Sounds U6/CasualGameSounds/DM-CGS-11.wav";
        private const string FirstMissionPath = "Assets/Casual Game Sounds U6/CasualGameSounds/DM-CGS-12.wav";
        private const string StageCompletePath = "Assets/Casual Game Sounds U6/CasualGameSounds/DM-CGS-18.wav";
        private const string PortalPath = "Assets/Casual Game Sounds U6/CasualGameSounds/DM-CGS-33.wav";
        private const string ResourceFolder = "Assets/_Project/Resources/SFX";

        static GameSfxLibraryConfigurator()
        {
            EditorApplication.delayCall += ConfigureMissingReferences;
        }

        private static void ConfigureMissingReferences()
        {
            EnsureResourceCopies();

            GameSfxLibrary library = AssetDatabase.LoadAssetAtPath<GameSfxLibrary>(LibraryPath);
            if (library == null)
            {
                Debug.LogError($"[Game SFX] Library not found at '{LibraryPath}'.");
                return;
            }

            SerializedObject serializedLibrary = new(library);
            bool changed = false;
            changed |= AssignIfMissing(serializedLibrary, "footstep", FootstepPath);
            changed |= AssignIfMissing(serializedLibrary, "holdReverse", HoldPath);
            changed |= AssignIfMissing(serializedLibrary, "firstMissionComplete", FirstMissionPath);
            changed |= AssignIfMissing(serializedLibrary, "stageComplete", StageCompletePath);
            changed |= AssignIfMissing(serializedLibrary, "portalEnter", PortalPath);

            if (!changed)
            {
                return;
            }

            serializedLibrary.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
            Debug.Log("[Game SFX] Missing audio references were repaired and saved by Unity.", library);
        }

        private static void EnsureResourceCopies()
        {
            EnsureFolder("Assets/_Project/Resources", "SFX");
            CopyIfMissing(FootstepPath, $"{ResourceFolder}/Footstep.wav");
            CopyIfMissing(HoldPath, $"{ResourceFolder}/HoldReverse.wav");
            CopyIfMissing(FirstMissionPath, $"{ResourceFolder}/FirstMissionComplete.wav");
            CopyIfMissing(StageCompletePath, $"{ResourceFolder}/StageComplete.wav");
            CopyIfMissing(PortalPath, $"{ResourceFolder}/PortalEnter.wav");
            AssetDatabase.SaveAssets();
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = $"{parent}/{child}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static void CopyIfMissing(string sourcePath, string destinationPath)
        {
            if (AssetDatabase.LoadAssetAtPath<AudioClip>(destinationPath) != null)
            {
                return;
            }

            if (!AssetDatabase.CopyAsset(sourcePath, destinationPath))
            {
                Debug.LogError($"[Game SFX] Could not copy '{sourcePath}' to '{destinationPath}'.");
            }
        }

        private static bool AssignIfMissing(SerializedObject target, string propertyName, string audioPath)
        {
            SerializedProperty property = target.FindProperty(propertyName);
            if (property == null || property.objectReferenceValue != null)
            {
                return false;
            }

            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(audioPath);
            if (clip == null)
            {
                Debug.LogError($"[Game SFX] Audio clip not found at '{audioPath}'.");
                return false;
            }

            property.objectReferenceValue = clip;
            return true;
        }
    }
}
