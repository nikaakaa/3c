using TreeDesigner.Authoring;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BTSMTL.Timeline;
using Newtonsoft.Json.Linq;
using ThirdPersonCharacter.Animation.TransitionRouting;
using ThirdPersonCharacter.Editor.CharacterSimulation;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Editor;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;
using TreeDesigner.Editor;
using UnityEditor;
using UnityEngine;
using AnimationClip = UnityEngine.AnimationClip;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public sealed partial class AgentAuthoringPresentationReconciler
    {
        Dictionary<string, AgentDocumentBlendAssetContext> m_BlendCurveCatalog;
        Dictionary<string, AgentDocumentBlendAssetContext> m_BlendProfileCatalog;
        readonly Dictionary<string, UnityEngine.Object> m_LocalAssets =
            new Dictionary<string, UnityEngine.Object>(StringComparer.Ordinal);
        CharacterAnimationRigDefinition m_Rig;

        public bool TryCreatePlan(
            CharacterPipelineDefinition definition,
            AgentDocumentEditable editable,
            AgentDocumentContext context,
            AgentCompileReport report,
            out AgentPresentationMutationPlan plan)
        {
            plan = null;
            m_LocalAssets.Clear();
            AgentDocumentPresentationEditable target =
                editable?.presentation;
            if (!definition || !definition.AnimationPresentationProfile ||
                target?.profile == null)
            {
                report.Error(
                    "editable/presentation",
                    "presentation_owner_missing",
                    "Presentation Reconciler缺少Definition、Profile或目标状态。");
                return false;
            }
            ValidateCrossOwnerReferences(editable, report);
            if (report.HasErrors())
                return false;

            CharacterAnimationPresentationProfile profile =
                definition.AnimationPresentationProfile;
            string profilePath = AssetDatabase.GetAssetPath(profile);
            string profileGuid = AssetDatabase.AssetPathToGUID(profilePath);
            if (!Matches(target.profile.owner, profile, profileGuid) ||
                !string.Equals(
                    target.profile.id,
                    profileGuid,
                    StringComparison.Ordinal))
            {
                report.Error(
                    "editable/presentation/profile.json.owner",
                    "presentation_profile_owner_mismatch",
                    "Presentation Profile owner必须保持当前Definition绑定的稳定identity。");
                return false;
            }

            CharacterPresentationPoseGraphAsset poseGraph =
                Resolve<CharacterPresentationPoseGraphAsset>(
                    target.profile.poseGraph,
                    "editable/presentation/profile.json.poseGraph",
                    report);
            CharacterAnimationRigDefinition rig =
                Resolve<CharacterAnimationRigDefinition>(
                    target.profile.rig,
                    "editable/presentation/profile.json.rig",
                    report);
            if (!poseGraph || !rig)
                return false;
            InitializeBlendCatalog(context, rig, report);
            if (report.HasErrors())
                return false;

            AgentDocumentPresentationEditable current;
            AgentDocumentPresentationEditable graphCurrent;
            try
            {
                var exporter = new AgentAuthoringPresentationExporter();
                current = exporter.Export(definition);
                graphCurrent = poseGraph == profile.PoseGraph
                    ? current
                    : exporter.ExportPoseGraph(poseGraph);
            }
            catch (Exception exception)
            {
                report.Error(
                    "editable/presentation",
                    "presentation_current_export_failed",
                    exception.Message);
                return false;
            }

            var identities = new IdentityMap(
                AssetDatabase.AssetPathToGUID(
                    AssetDatabase.GetAssetPath(definition)),
                CurrentIdentities(current).Concat(
                    CurrentIdentities(graphCurrent)));
            AgentDocumentPresentationEditable normalized =
                AgentAuthoringDocumentCodec.Clone(target);
            try
            {
                Normalize(normalized, identities, report);
            }
            catch (Exception exception)
            {
                report.Error(
                    "editable/presentation",
                    "presentation_identity_planning_failed",
                    exception.Message);
            }
            if (report.HasErrors())
                return false;
            ValidateLinkedPoseCalls(normalized, context, report);
            if (report.HasErrors())
                return false;

            string poseGraphPath = AssetDatabase.GetAssetPath(poseGraph);
            string poseGraphGuid =
                AssetDatabase.AssetPathToGUID(poseGraphPath);
            var graphTransaction = new CharacterPresentationMutationTransaction(
                "document-presentation-graph",
                "Apply Presentation Graph Document");
            var profileTransaction =
                new CharacterPresentationMutationTransaction(
                    "document-presentation-profile",
                    "Apply Presentation Profile Document");
            var builder = new PlanBuilder(
                graphTransaction,
                profileTransaction,
                report);

            IReadOnlyList<AgentAnimationClipCurveMutationPlan> animationClips =
                BuildAnimationClipCurvePlan(current, normalized, builder, report);
            if (report.HasErrors())
                return false;
            CharacterLocomotionSyncGroup[] locomotionSyncGroups =
                BuildLocomotionSyncGroups(normalized.profile, report);
            bool setLocomotionSyncGroups = !JToken.DeepEquals(
                AgentAuthoringDocumentCodec.ToToken(current.profile.locomotionSyncGroups),
                AgentAuthoringDocumentCodec.ToToken(normalized.profile.locomotionSyncGroups));
            if (setLocomotionSyncGroups)
            {
                builder.Direct(
                    "SetLocomotionSyncGroups",
                    profileGuid,
                    "editable/presentation/profile.json.locomotionSyncGroups",
                    $"groups={locomotionSyncGroups.Length}");
            }
            if (report.HasErrors())
                return false;

            PreparePoseSourceSlots(
                current.profile,
                normalized.profile,
                poseGraph,
                poseGraphGuid,
                builder,
                report);
            if (report.HasErrors())
                return false;

            BuildGraphPlan(
                graphCurrent,
                normalized,
                poseGraph,
                poseGraphGuid,
                builder,
                report);
            BuildStateMachinePlan(
                graphCurrent,
                normalized,
                builder,
                report);
            BuildLinkedPosePlan(
                current,
                normalized,
                context,
                profile,
                profileGuid,
                builder,
                report,
                out IReadOnlyList<AgentLinkedPoseGraphMutationPlan>
                    linkedPoseGraphs);
            BuildProfilePlan(
                current.profile,
                normalized.profile,
                profile,
                poseGraph,
                rig,
                builder,
                report);
            if (report.HasErrors())
                return false;

            plan = new AgentPresentationMutationPlan(
                profile,
                poseGraph,
                profileGuid,
                poseGraphGuid,
                graphTransaction,
                profileTransaction,
                linkedPoseGraphs,
                animationClips,
                locomotionSyncGroups,
                setLocomotionSyncGroups);
            return true;
        }

        static void ValidateCrossOwnerReferences(
            AgentDocumentEditable editable,
            AgentCompileReport report) =>
            AgentAuthoringPresentationCrossOwnerValidator.Validate(editable, report);
    }
}
