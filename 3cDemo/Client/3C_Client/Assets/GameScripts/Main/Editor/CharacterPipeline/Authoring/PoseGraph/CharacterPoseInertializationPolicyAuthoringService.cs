using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonSimulation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal static class CharacterPoseInertializationPolicyAuthoringService
    {
        const string CorinDefinitionPath =
            "Assets/Configs/Character/Corin/Pipeline/Definition/CorinCharacterPipelineDefinition.asset";
        const string CorinPolicyPath =
            "Assets/Configs/Character/Corin/Pipeline/Presentation/Blend/Locomotion/CorinPoseInertializationPolicy.asset";
        const string CorinPolicyId = "corin.pose.locomotion-inertialization";

        [MenuItem("Tools/3C/Pose Canvas/Create Corin Locomotion Inertialization Policy")]
        static void CreateCorinLocomotionInertializationPolicy()
        {
            CharacterPipelineDefinition definition =
                AssetDatabase.LoadAssetAtPath<CharacterPipelineDefinition>(
                    CorinDefinitionPath);
            if (!definition ||
                !definition.AnimationPresentationProfile ||
                !definition.AnimationPresentationProfile.PoseGraph ||
                !definition.AnimationPresentationProfile.RigDefinition)
            {
                throw new InvalidOperationException(
                    "Corin Definition requires one Animation Presentation Profile, Pose Graph and Rig Definition.");
            }
            CharacterPoseInertializationPolicy policy =
                AssetDatabase.LoadAssetAtPath<CharacterPoseInertializationPolicy>(CorinPolicyPath);
            if (policy == null)
            {
                policy = Create(
                    CorinPolicyPath,
                    CorinPolicyId,
                    StableHash.Compute(CorinPolicyId + ":" + CharacterPoseInertializationPolicy.SchemaVersion).ToString(),
                    definition.AnimationPresentationProfile.RigDefinition,
                    definition.AnimationPresentationProfile.PoseGraph.Graph.Parameters
                        .Select(value => value.ParameterId)
                        .ToArray());
            }
            RepairDirectPlayerRule(policy, definition.AnimationPresentationProfile.RigDefinition);
            Selection.activeObject =
                policy;
        }

        static void RepairDirectPlayerRule(
            CharacterPoseInertializationPolicy policy,
            CharacterAnimationRigDefinition rig)
        {
            CharacterAnimationBlendProfile blendProfile =
                AssetDatabase.LoadAssetAtPath<CharacterAnimationBlendProfile>(
                    "Assets/Configs/Character/Corin/Pipeline/Presentation/Blend/Locomotion/CorinLocomotionBlendProfile.asset");
            if (!policy || !rig || !blendProfile)
                throw new InvalidOperationException(
                    "Corin Locomotion Inertialization Policy repair requires the policy, Rig and Locomotion Blend Profile assets.");

            CharacterPoseDirectInertializationRule directRule =
                policy.DirectPlayerRule ?? new CharacterPoseDirectInertializationRule();
            directRule.Configure(
                PoseInertializationMode.Inertialize,
                0.2f,
                CharacterAnimationBlendMode.EaseInOut,
                null,
                blendProfile);
            policy.Configure(
                policy.PolicyId,
                policy.Revision,
                policy.Response,
                directRule,
                rig);
            EditorUtility.SetDirty(policy);
            AssetDatabase.SaveAssetIfDirty(policy);
            Debug.Log(
                $"Repaired Corin Locomotion Inertialization Policy '{policy.name}' " +
                $"with direct rule profile '{blendProfile.name}'.");
        }

        internal static CharacterPoseInertializationPolicy Create(
            string assetPath,
            string policyId,
            string revision,
            CharacterAnimationRigDefinition rig,
            IReadOnlyList<PoseParameterId> parameters)
        {
            if (string.IsNullOrWhiteSpace(assetPath) ||
                string.IsNullOrWhiteSpace(policyId) ||
                string.IsNullOrWhiteSpace(revision) ||
                !rig ||
                parameters == null)
            {
                throw new ArgumentException(
                    "Pose Inertialization Policy creation input is incomplete.");
            }
            if (AssetDatabase.LoadAssetAtPath<CharacterPoseInertializationPolicy>(
                    assetPath))
            {
                throw new InvalidOperationException(
                    $"Inertialization Policy already exists at '{assetPath}'.");
            }

            var filters = new CharacterPoseParameterInertializationFilter[parameters.Count];
            var identities = new HashSet<PoseParameterId>();
            for (int i = 0; i < parameters.Count; i++)
            {
                PoseParameterId parameter = parameters[i];
                if (!parameter.IsValid || !identities.Add(parameter))
                {
                    throw new InvalidOperationException(
                        $"Pose Inertialization Policy parameter '{parameter}' is invalid or duplicated.");
                }
                filters[i] = new CharacterPoseParameterInertializationFilter(
                    parameter,
                    PoseParameterInertializationMode.Snap);
            }

            var response = new CharacterPoseInertializationResponse();
            response.Configure(filters);
            CharacterPoseInertializationPolicy policy =
                ScriptableObject.CreateInstance<CharacterPoseInertializationPolicy>();
            policy.Configure(
                policyId,
                revision,
                response,
                null,
                rig);
            AssetDatabase.CreateAsset(policy, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            return policy;
        }
    }
}
