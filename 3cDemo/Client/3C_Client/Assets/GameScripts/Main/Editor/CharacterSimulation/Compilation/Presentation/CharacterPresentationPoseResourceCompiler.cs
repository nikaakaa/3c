using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Motion.RootMotion;
using ThirdPersonCharacter.Pipeline.Presentation;
using UnityEditor;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    internal sealed class CharacterPresentationPoseResourceCompilationCatalog
    {
        readonly Dictionary<CharacterPoseResourceSlot, UnityEngine.Object> m_Resources;

        internal CharacterPresentationPoseResourceCompilationCatalog(
            IDictionary<CharacterPoseResourceSlot, UnityEngine.Object> resources)
        {
            m_Resources = new Dictionary<CharacterPoseResourceSlot, UnityEngine.Object>(resources);
        }

        internal T Require<T>(
            CharacterPoseResourceSlot slot,
            CharacterPoseResourceKind kind,
            string sourcePath)
            where T : UnityEngine.Object
        {
            if (!slot || slot.Kind != kind ||
                !m_Resources.TryGetValue(slot, out UnityEngine.Object resource) ||
                resource is not T typed)
            {
                throw new InvalidOperationException(
                    $"{sourcePath}: Pose Resource Slot '{slot?.name ?? "<missing>"}' does not provide '{kind}'.");
            }
            return typed;
        }

        internal CharacterAnimationBlendPolicy BlendPolicy(
            CharacterPoseResourceSlot slot,
            string sourcePath) =>
            Require<CharacterAnimationBlendPolicy>(slot, CharacterPoseResourceKind.BlendPolicy, sourcePath);

        internal CharacterPoseInertializationPolicy InertializationPolicy(
            CharacterPoseResourceSlot slot,
            string sourcePath) =>
            Require<CharacterPoseInertializationPolicy>(slot, CharacterPoseResourceKind.InertializationPolicy, sourcePath);

        internal CharacterAnimationBoneMaskAsset BoneMask(
            CharacterPoseResourceSlot slot,
            string sourcePath) =>
            Require<CharacterAnimationBoneMaskAsset>(slot, CharacterPoseResourceKind.BoneMask, sourcePath);

        internal RootMotionCurveAsset RootMotionCurve(
            CharacterPoseResourceSlot slot,
            string sourcePath) =>
            Require<RootMotionCurveAsset>(slot, CharacterPoseResourceKind.RootMotionCurve, sourcePath);

        internal CharacterFootPlacementProfile FootProfile(
            CharacterPoseResourceSlot slot,
            string sourcePath) =>
            Require<CharacterFootPlacementProfile>(slot, CharacterPoseResourceKind.FootPlacementProfile, sourcePath);

        internal CharacterFootPlacementRigCalibration FootCalibration(
            CharacterPoseResourceSlot slot,
            string sourcePath) =>
            Require<CharacterFootPlacementRigCalibration>(slot, CharacterPoseResourceKind.FootPlacementCalibration, sourcePath);
    }

    internal static class CharacterPresentationPoseResourceCompiler
    {
        internal static CharacterPresentationPoseResourceCompilationCatalog Compile(
            CharacterAnimationPresentationProfile profile,
            List<string> diagnostics)
        {
            if (!profile || !profile.PoseGraph)
            {
                diagnostics.Add("Presentation Profile has no Pose Graph for Pose Resource compilation.");
                return new CharacterPresentationPoseResourceCompilationCatalog(
                    new Dictionary<CharacterPoseResourceSlot, UnityEngine.Object>());
            }

            string graphPath = AssetDatabase.GetAssetPath(profile.PoseGraph);
            var ownedSlots = new HashSet<CharacterPoseResourceSlot>();
            foreach (CharacterPoseResourceSlot slot in profile.PoseGraph.ResourceSlots)
            {
                if (!slot || !ownedSlots.Add(slot) ||
                    !string.Equals(AssetDatabase.GetAssetPath(slot), graphPath, StringComparison.Ordinal))
                {
                    diagnostics.Add("Pose Graph Resource Slot is missing, duplicated or not owned by the Graph asset.");
                    continue;
                }
                try
                {
                    slot.RequireValid();
                }
                catch (Exception exception)
                {
                    diagnostics.Add($"Pose Resource Slot '{slot.name}' is invalid: {exception.Message}");
                }
            }

            var resources = new Dictionary<CharacterPoseResourceSlot, UnityEngine.Object>();
            var boundSlots = new HashSet<CharacterPoseResourceSlot>();
            for (int i = 0; i < profile.PoseResourceBindings.Count; i++)
            {
                CharacterPoseResourceBinding binding = profile.PoseResourceBindings[i];
                if (binding == null || !binding.Slot || !ownedSlots.Contains(binding.Slot) ||
                    !boundSlots.Add(binding.Slot))
                {
                    diagnostics.Add($"Presentation Profile Pose Resource binding #{i} is missing, duplicated or foreign.");
                    continue;
                }
                try
                {
                    binding.RequireValid();
                    resources.Add(binding.Slot, binding.Resource);
                    ValidateResource(binding, profile.RigDefinition);
                }
                catch (Exception exception)
                {
                    diagnostics.Add($"Pose Resource binding '{binding.Slot.name}' is invalid: {exception.Message}");
                }
            }

            foreach (CharacterPoseResourceSlot slot in ownedSlots)
                if (!boundSlots.Contains(slot))
                    diagnostics.Add($"Pose Resource Slot '{slot.name}' has no Profile binding.");
            foreach (CharacterPoseResourceBinding binding in profile.PoseResourceBindings)
                if (binding?.Slot && !ownedSlots.Contains(binding.Slot))
                    diagnostics.Add($"Pose Resource binding '{binding.Slot.name}' is orphaned from the Profile Pose Graph.");
            return new CharacterPresentationPoseResourceCompilationCatalog(resources);
        }

        static void ValidateResource(
            CharacterPoseResourceBinding binding,
            CharacterAnimationRigDefinition rig)
        {
            switch (binding.Slot.Kind)
            {
                case CharacterPoseResourceKind.BlendPolicy:
                    ((CharacterAnimationBlendPolicy)binding.Resource).RequireValid(rig);
                    break;
                case CharacterPoseResourceKind.InertializationPolicy:
                    ((CharacterPoseInertializationPolicy)binding.Resource).RequireValid(rig);
                    break;
                case CharacterPoseResourceKind.BoneMask:
                    ((CharacterAnimationBoneMaskAsset)binding.Resource).BuildDense(rig);
                    break;
                case CharacterPoseResourceKind.RootMotionCurve:
                    if (!((RootMotionCurveAsset)binding.Resource).TryValidate(out string error))
                        throw new InvalidOperationException(error);
                    break;
                case CharacterPoseResourceKind.FootPlacementProfile:
                    ((CharacterFootPlacementProfile)binding.Resource).RequireValid();
                    break;
                case CharacterPoseResourceKind.FootPlacementCalibration:
                    ((CharacterFootPlacementRigCalibration)binding.Resource).RequireRig(rig);
                    break;
            }
        }
    }
}
