using System;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonCharacter.Pipeline.Simulation.DeterministicRollback;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public static class CharacterRuntimeProfileRootHierarchyBuilder
    {
        const string RollbackCorinPath = "Assets/Prefabs/Characters/RuntimeProfiles/Rollback/CorinDeterministicRollback.prefab";

        [MenuItem("Tools/3C/Characters/Synchronize Runtime Root Hierarchies")]
        public static void Synchronize()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Character Runtime Profile roots cannot be synchronized in Play Mode.");
            SynchronizeRollbackProfile();
            AssetDatabase.SaveAssets();
        }

        static void SynchronizeRollbackProfile()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(RollbackCorinPath);
            try
            {
                DeterministicRollbackCharacterHost host =
                    root.GetComponent<DeterministicRollbackCharacterHost>() ??
                    throw new InvalidOperationException("Rollback Corin Runtime Profile has no Rollback Host.");
                CharacterRootHierarchyBinding hierarchy = host.RootHierarchy
                    ? host.RootHierarchy
                    : root.GetComponent<CharacterRootHierarchyBinding>();
                if (!hierarchy)
                    throw new InvalidOperationException("Rollback Corin Runtime Profile has no Root Hierarchy Binding.");
                hierarchy.RequireValid();
                if (!host.AnimationRigBinding || host.AnimationRigBinding.transform != hierarchy.PoseRoot)
                    throw new InvalidOperationException("Rollback Corin Animation Rig Binding must belong to PoseRoot.");
                Transform aimAnchor = host.CameraAimAnchor;
                if (aimAnchor)
                    aimAnchor.SetParent(hierarchy.VisualRoot, false);
                var serialized = new SerializedObject(host);
                serialized.FindProperty("m_RootHierarchy").objectReferenceValue = hierarchy;
                serialized.FindProperty("m_CameraFollowAnchor").objectReferenceValue = hierarchy.VisualRoot;
                serialized.FindProperty("m_CameraAimAnchor").objectReferenceValue = aimAnchor;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, RollbackCorinPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
