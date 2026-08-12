using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ThirdPersonRendering.ShapeProjection.Editor
{
    public static class CharacterShapeProjectionSourceInstaller
    {
        public static int InstallAllCompleteRoots(GameObject prefabRoot, CharacterShapeProjectionProfile profile,
            CharacterShapeProjectionArtifact artifact)
        {
            ValidateLineage(profile, artifact);
            List<Transform> completeRoots = FindDeepestCompleteRoots(prefabRoot.transform, artifact);
            if (completeRoots.Count == 0)
                throw new InvalidOperationException($"{prefabRoot.name}没有完整且唯一的Shape Projection Renderer集合");

            CharacterShapeProjectionSource[] existing = prefabRoot.GetComponentsInChildren<CharacterShapeProjectionSource>(true);
            for (int i = existing.Length - 1; i >= 0; i--)
            {
                CharacterShapeProjectionSource source = existing[i];
                if ((source.Profile == profile || source.Artifact == artifact) && !Contains(completeRoots, source.transform))
                    UnityEngine.Object.DestroyImmediate(source);
            }

            for (int i = 0; i < completeRoots.Count; i++)
                InstallExactRoot(completeRoots[i].gameObject, profile, artifact);
            return completeRoots.Count;
        }

        public static void InstallExactRoot(GameObject root, CharacterShapeProjectionProfile profile,
            CharacterShapeProjectionArtifact artifact)
        {
            ValidateLineage(profile, artifact);
            if (!TryResolve(root.transform, artifact, out SkinnedMeshRenderer[] resolved))
                throw new InvalidOperationException($"{root.name}没有唯一匹配Artifact的Renderer集合");

            CharacterShapeProjectionSource source = root.GetComponent<CharacterShapeProjectionSource>();
            bool created = source == null;
            if (source == null)
                source = root.AddComponent<CharacterShapeProjectionSource>();
            source.EnsureIdentity();

            SerializedObject serialized = new SerializedObject(source);
            serialized.FindProperty("profile").objectReferenceValue = profile;
            serialized.FindProperty("artifact").objectReferenceValue = artifact;
            if (created)
            {
                serialized.FindProperty("projectionEnabled").boolValue = false;
                serialized.FindProperty("renderInGameCamera").boolValue = true;
                serialized.FindProperty("debugView").enumValueIndex = (int)ShapeProjectionDebugView.Final;
            }
            SerializedProperty bindings = serialized.FindProperty("rendererBindings");
            bindings.arraySize = resolved.Length;
            for (int i = 0; i < resolved.Length; i++)
            {
                SerializedProperty binding = bindings.GetArrayElementAtIndex(i);
                binding.FindPropertyRelative("slotId").stringValue = artifact.Renderers[i].SlotId;
                binding.FindPropertyRelative("renderer").objectReferenceValue = resolved[i];
                resolved[i].shadowCastingMode = ShadowCastingMode.On;
                EditorUtility.SetDirty(resolved[i]);
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(source);
        }

        public static SkinnedMeshRenderer[] ResolveSingleSet(GameObject root,
            CharacterShapeProjectionArtifact artifact)
        {
            List<Transform> completeRoots = FindDeepestCompleteRoots(root.transform, artifact);
            if (completeRoots.Count != 1 || !TryResolve(completeRoots[0], artifact, out SkinnedMeshRenderer[] resolved))
                throw new InvalidOperationException($"{root.name}必须只包含一套完整Renderer集合");
            return resolved;
        }

        public static SkinnedMeshRenderer[] FindNamedRenderers(GameObject root, IReadOnlyList<string> slotNames)
        {
            SkinnedMeshRenderer[] candidates = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            SkinnedMeshRenderer[] resolved = new SkinnedMeshRenderer[slotNames.Count];
            for (int slot = 0; slot < slotNames.Count; slot++)
            {
                for (int i = 0; i < candidates.Length; i++)
                {
                    if (!string.Equals(candidates[i].name, slotNames[slot], StringComparison.Ordinal))
                        continue;
                    if (resolved[slot] != null)
                        throw new InvalidOperationException($"{root.name}存在重复Renderer Slot：{slotNames[slot]}");
                    resolved[slot] = candidates[i];
                }
                if (resolved[slot] == null)
                    throw new InvalidOperationException($"{root.name}缺少Renderer Slot：{slotNames[slot]}");
            }
            return resolved;
        }

        static List<Transform> FindDeepestCompleteRoots(Transform prefabRoot,
            CharacterShapeProjectionArtifact artifact)
        {
            Transform[] transforms = prefabRoot.GetComponentsInChildren<Transform>(true);
            List<Transform> candidates = new List<Transform>();
            for (int i = 0; i < transforms.Length; i++)
            {
                if (TryResolve(transforms[i], artifact, out _))
                    candidates.Add(transforms[i]);
            }

            List<Transform> deepest = new List<Transform>();
            for (int i = 0; i < candidates.Count; i++)
            {
                bool containsCandidate = false;
                for (int j = 0; j < candidates.Count; j++)
                {
                    if (i != j && candidates[j].IsChildOf(candidates[i]))
                    {
                        containsCandidate = true;
                        break;
                    }
                }
                if (!containsCandidate)
                    deepest.Add(candidates[i]);
            }
            deepest.Sort((left, right) => string.Compare(GetPath(left, prefabRoot), GetPath(right, prefabRoot),
                StringComparison.Ordinal));
            return deepest;
        }

        static bool TryResolve(Transform root, CharacterShapeProjectionArtifact artifact,
            out SkinnedMeshRenderer[] resolved)
        {
            SkinnedMeshRenderer[] candidates = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            resolved = new SkinnedMeshRenderer[artifact.Renderers.Length];
            for (int slot = 0; slot < artifact.Renderers.Length; slot++)
            {
                ShapeProjectionRendererRecord record = artifact.Renderers[slot];
                for (int i = 0; i < candidates.Length; i++)
                {
                    SkinnedMeshRenderer candidate = candidates[i];
                    if (!string.Equals(candidate.name, record.SlotId, StringComparison.Ordinal)
                        || candidate.sharedMesh != record.SourceMesh || !MaterialsEqual(candidate.sharedMaterials, record.SourceMaterials))
                        continue;
                    if (resolved[slot] != null)
                        return false;
                    resolved[slot] = candidate;
                }
                if (resolved[slot] == null)
                    return false;
            }
            return true;
        }

        static void ValidateLineage(CharacterShapeProjectionProfile profile,
            CharacterShapeProjectionArtifact artifact)
        {
            if (profile == null || artifact == null)
                throw new InvalidOperationException("安装Source必须提供Profile和Artifact");
            ShapeProjectionValidationResult validation = artifact.ValidateArtifact();
            if (!validation.IsValid)
                throw new InvalidOperationException(validation.Error);
            if (!profile.ProfileId.Equals(artifact.ProfileId) || profile.Revision != artifact.ProfileRevision
                || profile.ContentHash != artifact.ProfileContentHash)
                throw new InvalidOperationException("Profile与Artifact lineage不一致");
        }

        static bool MaterialsEqual(Material[] left, Material[] right)
        {
            if (left == null || right == null || left.Length != right.Length)
                return false;
            for (int i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i])
                    return false;
            }
            return true;
        }

        static bool Contains(List<Transform> roots, Transform value)
        {
            for (int i = 0; i < roots.Count; i++)
            {
                if (roots[i] == value)
                    return true;
            }
            return false;
        }

        static string GetPath(Transform value, Transform root)
        {
            string path = value.name;
            while (value != root && value.parent != null)
            {
                value = value.parent;
                path = value.name + "/" + path;
            }
            return path;
        }
    }
}
