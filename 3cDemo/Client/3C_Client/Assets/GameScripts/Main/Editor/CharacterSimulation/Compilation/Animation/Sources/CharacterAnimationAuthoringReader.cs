using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Editor;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Editor.CharacterSimulation.Compilation.Animation
{
    internal static class CharacterAnimationAuthoringReader
    {
        static readonly string[] TransformProperties =
        {
            "m_LocalPosition.x",
            "m_LocalPosition.y",
            "m_LocalPosition.z",
            "m_LocalRotation.x",
            "m_LocalRotation.y",
            "m_LocalRotation.z",
            "m_LocalRotation.w",
            "m_LocalScale.x",
            "m_LocalScale.y",
            "m_LocalScale.z"
        };

        internal static CharacterAnimationAuthoringSource Read(
            CharacterAnimationAuthoringReadRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            request.SourceRig.RequireValid();
            if (!float.IsFinite(request.ClipIdentity.SourceDurationSeconds) ||
                request.ClipIdentity.SourceDurationSeconds <= 0f ||
                request.Clip.isLooping != request.ClipIdentity.Loop)
                throw new InvalidOperationException("Animation Clip content identity does not match the authoring Clip.");
            Transform root = request.SourceRig.Animator.transform;
            CharacterAnimationAuthoringTransformTrack[] transformTracks =
                request.Channels.HasFlag(CharacterAnimationBuildChannel.Transform)
                    ? ReadTransformTracks(
                        request.Clip,
                        request.SourceRig,
                        root)
                    : Array.Empty<CharacterAnimationAuthoringTransformTrack>();
            CharacterAnimationAuthoringScalarTrack[] scalarTracks =
                request.Channels.HasFlag(CharacterAnimationBuildChannel.AnimatedProperty)
                    ? ReadScalarTracks(
                        request.Clip,
                        request.SourceRig,
                        request.ParameterLayout,
                        request.PropertyBindings,
                        request.ParameterCurveBindings,
                        root)
                    : Array.Empty<CharacterAnimationAuthoringScalarTrack>();
            ValidateCurveClosure(request.Clip, transformTracks, scalarTracks, request.Channels, root);
            return new CharacterAnimationAuthoringSource(
                request.ClipIdentity,
                request.SourceIdentity,
                request.SourceRig,
                request.ParameterLayout,
                transformTracks,
                scalarTracks,
                request.Channels);
        }

        static CharacterAnimationAuthoringTransformTrack[] ReadTransformTracks(
            AnimationClip clip,
            CharacterAnimationSourceRig sourceRig,
            Transform root)
        {
            var result = new CharacterAnimationAuthoringTransformTrack[sourceRig.PhysicalBoneCount];
            for (int i = 0; i < result.Length; i++)
            {
                Transform bone = sourceRig.PhysicalTransforms[i];
                string path = AnimationUtility.CalculateTransformPath(bone, root);
                var curves = new AnimationCurve[TransformProperties.Length];
                for (int propertyIndex = 0; propertyIndex < curves.Length; propertyIndex++)
                {
                    curves[propertyIndex] = ReadCurve(
                        clip,
                        EditorCurveBinding.FloatCurve(
                            path,
                            typeof(Transform),
                            TransformProperties[propertyIndex]),
                        $"Transform '{path}:{TransformProperties[propertyIndex]}'");
                }
                result[i] = new CharacterAnimationAuthoringTransformTrack(
                    sourceRig.PhysicalBones[i].BoneId.Value,
                    i,
                    sourceRig.PhysicalBones[i].ParentPhysicalIndex,
                    sourceRig.GetReferenceLocalPose(i),
                    curves,
                    path);
            }
            return result;
        }

        static CharacterAnimationAuthoringScalarTrack[] ReadScalarTracks(
            AnimationClip clip,
            CharacterAnimationSourceRig sourceRig,
            CharacterAnimationParameterLayout parameterLayout,
            IReadOnlyList<CharacterAnimationPropertyAuthoringBinding> propertyBindings,
            IReadOnlyList<CharacterAnimationParameterCurveSourceBinding> parameterCurveBindings,
            Transform root)
        {
            var result = new List<CharacterAnimationAuthoringScalarTrack>(
                propertyBindings.Count + parameterCurveBindings.Count);
            var parameterIds = new HashSet<PoseParameterId>();
            var curveBindings = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < propertyBindings.Count; i++)
            {
                CharacterAnimationPropertyAuthoringBinding binding = propertyBindings[i] ??
                    throw new InvalidOperationException($"Animation property binding #{i} is missing.");
                int parameterIndex = parameterLayout.RequireIndex(binding.ParameterId);
                CharacterPoseParameterDeclaration parameter = parameterLayout.Declarations[parameterIndex];
                if (parameter.ValueType != PoseParameterValueType.Float ||
                    parameter.Usage != CharacterPoseParameterUsage.AnimatedProperty)
                    throw new InvalidOperationException(
                        $"Animation property '{binding.ParameterId}' is not an AnimatedProperty float parameter.");
                if (binding.ExpectedMesh.GetBlendShapeName(binding.BlendShapeIndex) != binding.BlendShapeName)
                    throw new InvalidOperationException(
                        $"Animation property '{binding.ParameterId}' BlendShape identity does not match its Mesh.");
                CharacterAnimationRendererBinding renderer = sourceRig.FindRendererBinding(
                    binding.RendererBindingId);
                if (renderer == null)
                    throw new InvalidOperationException(
                        $"Animation property '{binding.ParameterId}' references missing Renderer binding '{binding.RendererBindingId}'.");
                renderer.RequireValid(root);
                CharacterAnimationMeshContentIdentity.RequireCurrent(
                    renderer.ExpectedMesh,
                    renderer.MeshContentHash);
                if (renderer.ExpectedMesh != binding.ExpectedMesh ||
                    !string.Equals(renderer.MeshContentHash, binding.MeshContentHash, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException(
                        $"Animation property '{binding.ParameterId}' Renderer binding does not match its Mesh identity.");
                string rendererPath = AnimationUtility.CalculateTransformPath(renderer.Renderer.transform, root);
                if (!string.Equals(rendererPath, binding.AnimationCurvePath, StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        $"Animation property '{binding.ParameterId}' curve path does not match its assembled Renderer.");
                string propertyName = "blendShape." + binding.BlendShapeName;
                EditorCurveBinding curveBinding = EditorCurveBinding.FloatCurve(
                    binding.AnimationCurvePath,
                    typeof(SkinnedMeshRenderer),
                    propertyName);
                RequireScalarBindingIdentity(
                    parameterIds,
                    curveBindings,
                    binding.ParameterId,
                    curveBinding);
                AnimationCurve curve = ReadCurve(
                    clip,
                    curveBinding,
                    $"BlendShape '{binding.AnimationCurvePath}:{propertyName}'");
                result.Add(new CharacterAnimationAuthoringScalarTrack(
                    binding.ParameterId,
                    parameterIndex,
                    parameter.DefaultValue,
                    parameter.Unit,
                    curveBinding,
                    curve));
            }
            for (int i = 0; i < parameterCurveBindings.Count; i++)
            {
                CharacterAnimationParameterCurveSourceBinding binding =
                    parameterCurveBindings[i] ??
                    throw new InvalidOperationException(
                        $"Animation parameter curve binding #{i} is missing.");
                int parameterIndex = parameterLayout.RequireIndex(binding.ParameterId);
                CharacterPoseParameterDeclaration parameter = parameterLayout.Declarations[parameterIndex];
                if (parameter.ValueType != PoseParameterValueType.Float ||
                    parameter.Usage != CharacterPoseParameterUsage.Control)
                {
                    throw new InvalidOperationException(
                        $"Animation parameter '{binding.ParameterId}' is not a Control float parameter.");
                }
                RequireScalarBindingIdentity(
                    parameterIds,
                    curveBindings,
                    binding.ParameterId,
                    binding.CurveBinding);
                AnimationCurve curve = ReadCurve(
                    clip,
                    binding.CurveBinding,
                    $"Parameter '{binding.CurveBinding.path}:{binding.CurveBinding.propertyName}'");
                result.Add(new CharacterAnimationAuthoringScalarTrack(
                    binding.ParameterId,
                    parameterIndex,
                    parameter.DefaultValue,
                    parameter.Unit,
                    binding.CurveBinding,
                    curve));
            }
            return result.ToArray();
        }

        static void RequireScalarBindingIdentity(
            ISet<PoseParameterId> parameterIds,
            ISet<string> curveBindings,
            PoseParameterId parameterId,
            EditorCurveBinding curveBinding)
        {
            if (!parameterIds.Add(parameterId))
                throw new InvalidOperationException(
                    $"Animation scalar parameter '{parameterId}' is declared more than once.");
            string identity = string.Concat(
                curveBinding.path ?? string.Empty,
                "|",
                curveBinding.type?.AssemblyQualifiedName ?? string.Empty,
                "|",
                curveBinding.propertyName ?? string.Empty,
                "|",
                curveBinding.isPPtrCurve ? "pptr" : "float");
            if (!curveBindings.Add(identity))
                throw new InvalidOperationException(
                    $"Animation scalar curve binding '{identity}' is declared more than once.");
        }

        static AnimationCurve ReadCurve(
            AnimationClip clip,
            EditorCurveBinding binding,
            string identity)
        {
            AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
            if (curve == null)
                return null;
            if (curve.length == 0)
                throw new InvalidOperationException($"Animation curve '{identity}' is empty.");
            return CopyCurve(curve);
        }

        static void ValidateCurveClosure(
            AnimationClip clip,
            IReadOnlyList<CharacterAnimationAuthoringTransformTrack> transformTracks,
            IReadOnlyList<CharacterAnimationAuthoringScalarTrack> scalarTracks,
            CharacterAnimationBuildChannel channels,
            Transform root)
        {
            var supported = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < transformTracks.Count; i++)
            {
                CharacterAnimationAuthoringTransformTrack track = transformTracks[i];
                for (int propertyIndex = 0; propertyIndex < TransformProperties.Length; propertyIndex++)
                    supported.Add(CurveKey(track.Path, typeof(Transform), TransformProperties[propertyIndex]));
            }
            for (int i = 0; i < scalarTracks.Count; i++)
                supported.Add(CurveKey(
                    scalarTracks[i].CurveBinding.path,
                    scalarTracks[i].CurveBinding.type,
                    scalarTracks[i].CurveBinding.propertyName));
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
            CharacterAnimationRootCurveClassifier.AllowNoneOrRequireExact(clip);
            for (int i = 0; i < bindings.Length; i++)
            {
                EditorCurveBinding binding = bindings[i];
                if (CharacterAnimationRootCurveClassifier.IsEvidence(binding) ||
                    CharacterAnimationClipRegisteredCurveCatalog.IsRegistered(binding) ||
                    supported.Contains(CurveKey(binding.path, binding.type, binding.propertyName)) ||
                    IsIgnorableUnboundTransformCurve(clip, binding, root) ||
                    !channels.HasFlag(CharacterAnimationBuildChannel.Transform) &&
                    binding.type == typeof(Transform))
                    continue;
                throw new InvalidOperationException(
                    $"Animation Clip '{clip.name}' contains an unsupported curve '{binding.path}:{binding.type?.FullName}:{binding.propertyName}'.");
            }
            EditorCurveBinding[] objectBindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
            if (objectBindings.Length > 0)
                throw new InvalidOperationException(
                    $"Animation Clip '{clip.name}' contains unsupported object-reference animation curves.");
        }

        static bool IsIgnorableUnboundTransformCurve(
            AnimationClip clip,
            EditorCurveBinding binding,
            Transform root)
        {
            if (binding.type != typeof(Transform) ||
                !IsSupportedTransformProperty(binding.propertyName))
                return false;
            Transform target = string.IsNullOrEmpty(binding.path)
                ? root
                : root.Find(binding.path);
            if (!target)
                return true;
            float referenceValue = GetTransformPropertyValue(target, binding.propertyName);
            AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
            if (curve != null && curve.length > 0)
            {
                Keyframe[] keys = curve.keys;
                bool matchesReference = true;
                for (int i = 0; i < keys.Length; i++)
                {
                    if (Mathf.Abs(keys[i].value - referenceValue) > 0.0000001f ||
                        keys[i].inTangent != 0f || keys[i].outTangent != 0f)
                    {
                        matchesReference = false;
                        break;
                    }
                }
                if (matchesReference)
                    return true;
            }
            if (target.childCount != 0 || target.GetComponents<Component>().Length != 1)
                return false;
            SkinnedMeshRenderer[] renderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                SkinnedMeshRenderer renderer = renderers[i];
                if (!renderer)
                    continue;
                if (renderer.rootBone == target)
                    return false;
                Transform[] bones = renderer.bones;
                for (int boneIndex = 0; boneIndex < bones.Length; boneIndex++)
                {
                    if (bones[boneIndex] == target)
                        return false;
                }
            }
            return true;
        }

        static bool IsSupportedTransformProperty(string propertyName)
        {
            for (int i = 0; i < TransformProperties.Length; i++)
            {
                if (string.Equals(TransformProperties[i], propertyName, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        static float GetTransformPropertyValue(Transform target, string propertyName)
        {
            switch (propertyName)
            {
                case "m_LocalPosition.x": return target.localPosition.x;
                case "m_LocalPosition.y": return target.localPosition.y;
                case "m_LocalPosition.z": return target.localPosition.z;
                case "m_LocalRotation.x": return target.localRotation.x;
                case "m_LocalRotation.y": return target.localRotation.y;
                case "m_LocalRotation.z": return target.localRotation.z;
                case "m_LocalRotation.w": return target.localRotation.w;
                case "m_LocalScale.x": return target.localScale.x;
                case "m_LocalScale.y": return target.localScale.y;
                case "m_LocalScale.z": return target.localScale.z;
                default: throw new InvalidOperationException($"Unsupported Transform property '{propertyName}'.");
            }
        }

        static string CurveKey(string path, Type type, string propertyName) =>
            string.Concat(path ?? string.Empty, "|", type?.AssemblyQualifiedName ?? string.Empty, "|", propertyName ?? string.Empty);

        static AnimationCurve CopyCurve(AnimationCurve source)
        {
            var result = new AnimationCurve(source.keys)
            {
                preWrapMode = source.preWrapMode,
                postWrapMode = source.postWrapMode
            };
            return result;
        }
    }
}
