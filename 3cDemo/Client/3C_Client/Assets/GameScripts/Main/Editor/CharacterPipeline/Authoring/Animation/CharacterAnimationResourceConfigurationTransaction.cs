using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public sealed class CharacterAnimationResourceConfigurationTransaction
    {
        const string UndoName = "Configure Character Animation Resources";

        sealed class LoadedPrefab : IDisposable
        {
            public LoadedPrefab(
                string assetPath,
                GameObject root,
                CharacterAnimationRigBinding rigBinding,
                SkinnedMeshRenderer renderer)
            {
                AssetPath = assetPath;
                Root = root;
                RigBinding = rigBinding;
                Renderer = renderer;
            }

            public string AssetPath { get; }
            public GameObject Root { get; }
            public CharacterAnimationRigBinding RigBinding { get; }
            public SkinnedMeshRenderer Renderer { get; }

            bool m_Unloaded;

            public void Dispose()
            {
                if (m_Unloaded)
                    return;
                m_Unloaded = true;
                if (Root)
                    PrefabUtility.UnloadPrefabContents(Root);
            }
        }

        sealed class PrefabBackup
        {
            public string AssetPath;
            public byte[] Bytes;
        }

        public void Apply(CharacterAnimationPropertyImportPlan plan)
        {
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));
            if (plan.Diffs.Count == 0)
                return;
            CharacterAnimationMeshContentIdentity.RequireCurrent(
                plan.Mesh,
                plan.MeshContentHash);
            List<LoadedPrefab> prefabs = LoadPrefabs(plan);
            PrefabBackup[] backups = Array.Empty<PrefabBackup>();
            int undoGroup = -1;
            try
            {
                CharacterAnimationPropertyCurveMutation.ValidateClips(plan);
                ValidatePrefabs(plan, prefabs);
                backups = CreateBackups(plan.PrefabTargets);
                CharacterPresentationMutationTransaction graphTransaction =
                    CreateGraphTransaction(plan);
                CharacterPresentationMutationTransaction profileTransaction =
                    CreateProfileTransaction(plan);
                Undo.IncrementCurrentGroup();
                undoGroup = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName(UndoName);
                var owners = new List<UnityEngine.Object>
                {
                    plan.Profile,
                    plan.PoseGraph
                };
                owners.AddRange(plan.SourceClosure.Select(value => (UnityEngine.Object)value.Clip));
                Undo.RegisterCompleteObjectUndo(owners.ToArray(), UndoName);
                for (int i = 0; i < prefabs.Count; i++)
                    Undo.RegisterFullObjectHierarchyUndo(prefabs[i].Root, UndoName);

                CharacterAnimationPropertyCurveMutation.Apply(plan);
                CharacterPresentationMutationService mutationService =
                    new CharacterPresentationMutationService();
                if (graphTransaction.Mutations.Count > 0)
                {
                    mutationService.ApplyWithoutUndo(
                        new CharacterPoseGraphAssetMutationOwner(plan.PoseGraph),
                        graphTransaction);
                }
                if (profileTransaction.Mutations.Count > 0)
                {
                    mutationService.ApplyWithoutUndo(
                        new CharacterPresentationProfileMutationOwner(
                            plan.Profile,
                            AssetDatabase.AssetPathToGUID(plan.ProfileAssetPath)),
                        profileTransaction);
                }
                ApplyPrefabBindings(plan, prefabs);
                EditorUtility.SetDirty(plan.Profile);
                EditorUtility.SetDirty(plan.PoseGraph);
                for (int i = 0; i < plan.SourceClosure.Count; i++)
                    EditorUtility.SetDirty(plan.SourceClosure[i].Clip);
                AssetDatabase.SaveAssets();
                for (int i = 0; i < prefabs.Count; i++)
                {
                    if (!PrefabUtility.SaveAsPrefabAsset(prefabs[i].Root, prefabs[i].AssetPath))
                        throw new InvalidOperationException(
                            $"Prefab '{prefabs[i].AssetPath}' could not be saved.");
                }
                AssetDatabase.SaveAssets();
                Undo.CollapseUndoOperations(undoGroup);
            }
            catch
            {
                if (undoGroup >= 0)
                    Undo.RevertAllDownToGroup(undoGroup);
                for (int i = prefabs.Count - 1; i >= 0; i--)
                    prefabs[i].Dispose();
                RestoreBackups(backups);
                AssetDatabase.SaveAssets();
                throw;
            }
            finally
            {
                for (int i = prefabs.Count - 1; i >= 0; i--)
                    prefabs[i].Dispose();
            }
        }

        static List<LoadedPrefab> LoadPrefabs(CharacterAnimationPropertyImportPlan plan)
        {
            var result = new List<LoadedPrefab>(plan.PrefabTargets.Count);
            try
            {
                for (int i = 0; i < plan.PrefabTargets.Count; i++)
                {
                    CharacterAnimationPropertyImportPrefabTarget target = plan.PrefabTargets[i];
                    GameObject root = PrefabUtility.LoadPrefabContents(target.AssetPath);
                    CharacterAnimationRigBinding[] bindings = root
                        .GetComponentsInChildren<CharacterAnimationRigBinding>(true)
                        .Where(value => value &&
                                        CharacterAnimationRigRendererResolver.IsOwnedByPrefabAsset(
                                            value.gameObject,
                                            target.AssetPath) &&
                                        string.Equals(value.RigId, target.RigId, StringComparison.Ordinal))
                        .ToArray();
                    if (bindings.Length != 1)
                        throw new InvalidOperationException(
                            $"Prefab '{target.AssetPath}' does not have exactly one matching Animation Rig Binding.");
                    CharacterAnimationRigBinding binding = bindings[0];
                    SkinnedMeshRenderer renderer = CharacterAnimationRigRendererResolver.Require(
                        binding.Animator,
                        target.RendererPath,
                        target.AssetPath);
                    result.Add(new LoadedPrefab(
                        target.AssetPath,
                        root,
                        binding,
                        renderer));
                }
                return result;
            }
            catch
            {
                for (int i = result.Count - 1; i >= 0; i--)
                    result[i].Dispose();
                throw;
            }
        }

        static void ValidatePrefabs(
            CharacterAnimationPropertyImportPlan plan,
            IReadOnlyList<LoadedPrefab> prefabs)
        {
            if (prefabs.Count != plan.PrefabTargets.Count)
                throw new InvalidOperationException("Animation resource configuration Prefab closure changed after analyze.");
            for (int i = 0; i < prefabs.Count; i++)
            {
                LoadedPrefab loaded = prefabs[i];
                CharacterAnimationPropertyImportPrefabTarget target =
                    plan.PrefabTargets.Single(value => value.AssetPath == loaded.AssetPath);
                if (!string.Equals(loaded.RigBinding.RigRevision, target.RigRevision, StringComparison.Ordinal) ||
                    loaded.Renderer.sharedMesh != plan.Mesh ||
                    !string.Equals(
                        AnimationUtility.CalculateTransformPath(
                            loaded.Renderer.transform,
                            loaded.RigBinding.Animator.transform),
                        target.RendererPath,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Prefab '{loaded.AssetPath}' changed after analyze.");
                }
            }
        }

        static PrefabBackup[] CreateBackups(
            IReadOnlyList<CharacterAnimationPropertyImportPrefabTarget> targets)
        {
            return targets.Select(target =>
            {
                string path = ProjectAssetPath(target.AssetPath);
                if (!File.Exists(path))
                    throw new InvalidOperationException($"Prefab '{target.AssetPath}' does not exist on disk.");
                return new PrefabBackup
                {
                    AssetPath = target.AssetPath,
                    Bytes = File.ReadAllBytes(path)
                };
            }).ToArray();
        }

        static void RestoreBackups(IReadOnlyList<PrefabBackup> backups)
        {
            for (int i = 0; i < backups.Count; i++)
            {
                PrefabBackup backup = backups[i];
                File.WriteAllBytes(ProjectAssetPath(backup.AssetPath), backup.Bytes);
                AssetDatabase.ImportAsset(
                    backup.AssetPath,
                    ImportAssetOptions.ForceUpdate);
            }
        }

        static string ProjectAssetPath(string assetPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
        }

        static CharacterPresentationMutationTransaction CreateGraphTransaction(
            CharacterAnimationPropertyImportPlan plan)
        {
            var transaction = new CharacterPresentationMutationTransaction(
                Guid.NewGuid().ToString("N"),
                UndoName);
            foreach (KeyValuePair<string, CharacterPoseParameterDeclaration[]> pair in
                     plan.GraphParameters.OrderBy(value => value.Key, StringComparer.Ordinal))
            {
                transaction.Add(new SetPoseGraphParametersMutation(
                    pair.Key,
                    pair.Value.ToArray()));
            }
            transaction.Add(new SetPoseNodeFieldMutation(
                plan.PoseGraph.Graph.GraphId.Value,
                new PoseNodeId(plan.RootResolveNodeId),
                "parameter-policies",
                plan.RootPolicies.ToArray()));
            return transaction;
        }

        static CharacterPresentationMutationTransaction CreateProfileTransaction(
            CharacterAnimationPropertyImportPlan plan)
        {
            string profileId = AssetDatabase.AssetPathToGUID(plan.ProfileAssetPath);
            if (string.IsNullOrWhiteSpace(profileId))
                throw new InvalidOperationException("Animation Presentation Profile has no stable asset GUID.");
            var transaction = new CharacterPresentationMutationTransaction(
                Guid.NewGuid().ToString("N"),
                UndoName);
            transaction.Add(new SetProfileSourceResourceBindingsMutation(
                profileId,
                plan.SourceResourceBindings.ToArray()));
            transaction.Add(new SetProfileAnimationCompressionMutation(
                profileId,
                plan.Compression.Settings));
            transaction.Add(new SetProfileAnimationPropertyBindingsMutation(
                profileId,
                plan.PropertyBindings.ToArray()));
            return transaction;
        }

        static void ApplyPrefabBindings(
            CharacterAnimationPropertyImportPlan plan,
            IReadOnlyList<LoadedPrefab> prefabs)
        {
            for (int i = 0; i < prefabs.Count; i++)
            {
                LoadedPrefab loaded = prefabs[i];
                var bindings = loaded.RigBinding.RendererBindings.ToList();
                var replacement = new CharacterAnimationRendererBinding(
                    plan.RendererBindingId,
                    loaded.Renderer,
                    plan.Mesh,
                    plan.MeshContentHash);
                int index = bindings.FindIndex(value =>
                    value != null &&
                    string.Equals(value.BindingId, plan.RendererBindingId, StringComparison.Ordinal));
                if (index < 0)
                    bindings.Add(replacement);
                else
                    bindings[index] = replacement;
                loaded.RigBinding.ConfigureRendererBindings(bindings.ToArray());
                EditorUtility.SetDirty(loaded.RigBinding);
            }
        }
    }
}
