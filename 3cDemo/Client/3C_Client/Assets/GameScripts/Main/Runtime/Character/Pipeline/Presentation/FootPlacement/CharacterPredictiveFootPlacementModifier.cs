using System;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonSimulation;
using Unity.Collections;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal sealed class CharacterPredictiveFootPlacementModifier
    {
        readonly ActorId m_ActorId;
        readonly CharacterFootPlacementPoseRig m_Rig;
        readonly CharacterFootPlacementWorldQueryBackend m_World;
        readonly int m_GroundLayerMask;
        readonly FixedString64Bytes m_RigId;
        readonly FixedString64Bytes m_RigRevision;
        CharacterPredictiveFootPlacementRuntimeSettings m_Settings;
        CharacterPredictiveFootPlacementQuery m_Query;
        CharacterPredictiveFootPlacementModifierDiagnostics m_Diagnostics;

        internal CharacterPredictiveFootPlacementModifier(
            ActorId actorId,
            CharacterFootPlacementPoseRig rig,
            CharacterFootPlacementRuntimeSettings settings,
            PhysicsScene physicsScene)
        {
            if (!actorId.IsValid)
                throw new ArgumentException("Predictive Foot Placement Modifier Actor identity is invalid.", nameof(actorId));
            m_ActorId = actorId;
            m_Rig = rig ?? throw new ArgumentNullException(nameof(rig));
            m_Settings = settings?.PredictiveExtension ??
                throw new ArgumentNullException(nameof(settings));
            m_GroundLayerMask = settings.CurrentGrounding.GroundLayerMask;
            m_RigId = new FixedString64Bytes(rig.Rig.RigId);
            m_RigRevision = new FixedString64Bytes(rig.Rig.RigRevision);
            m_World = new CharacterFootPlacementWorldQueryBackend(
                physicsScene,
                rig,
                settings.CurrentGrounding.HitCapacity);
            m_Query = new CharacterPredictiveFootPlacementQuery(m_World, m_Settings);
        }

        internal CharacterPredictiveFootPlacementModifierDiagnostics Diagnostics => m_Diagnostics;

        internal CharacterFullBodyIkGoalSetHeader Modify(
            in CharacterFootPlacementPlanningFrame frame,
            in CharacterFullBodyIkGoalSetHeader baselineHeader,
            NativeSlice<CharacterFullBodyIkGoal> baselineGoals,
            in CharacterFootGroundingDiagnostics groundingDiagnostics,
            NativeSlice<CharacterFullBodyIkGoal> output,
            int goalWorkspaceOffset,
            int producerOperationIndex,
            int producerCallSiteIndex)
        {
            RequireValidInput(
                in frame,
                in baselineHeader,
                baselineGoals,
                in groundingDiagnostics,
                output,
                goalWorkspaceOffset,
                producerOperationIndex,
                producerCallSiteIndex);
            CharacterFootPlacementAnimatedPose pose = m_Rig.CaptureAnimatedPose(
                frame.RenderFrame,
                frame.UpstreamPose.DenseComponentPoses);
            CharacterFootSide selectedSide = SelectRewriteSide(
                groundingDiagnostics.Left,
                frame.UpstreamPose.LeftFootFeatures,
                groundingDiagnostics.Right,
                frame.UpstreamPose.RightFootFeatures);
            output[0] = baselineGoals[0];
            output[1] = ModifyFoot(
                CharacterFootSide.Left,
                selectedSide == CharacterFootSide.Left,
                pose.Left,
                frame.UpstreamPose.LeftFootFeatures,
                groundingDiagnostics.Left,
                baselineGoals[1],
                frame.Body,
                m_Rig.LeftLegLength,
                out CharacterPredictiveFootPlacementModifierFootDiagnostics leftDiagnostics);
            output[2] = ModifyFoot(
                CharacterFootSide.Right,
                selectedSide == CharacterFootSide.Right,
                pose.Right,
                frame.UpstreamPose.RightFootFeatures,
                groundingDiagnostics.Right,
                baselineGoals[2],
                frame.Body,
                m_Rig.RightLegLength,
                out CharacterPredictiveFootPlacementModifierFootDiagnostics rightDiagnostics);
            m_Diagnostics = new CharacterPredictiveFootPlacementModifierDiagnostics(
                frame.RenderFrame,
                frame.CompletionIdentity,
                selectedSide,
                in baselineHeader,
                leftDiagnostics,
                rightDiagnostics);
            return new CharacterFullBodyIkGoalSetHeader(
                frame.RenderFrame,
                frame.CompletionIdentity,
                m_Rig.Rig.RigId,
                m_Rig.Rig.RigRevision,
                producerOperationIndex,
                producerCallSiteIndex,
                goalWorkspaceOffset,
                3,
                CharacterFullBodyIkGoalSetAvailability.Ready);
        }

        internal void ApplyTuning(CharacterPredictiveFootPlacementRuntimeSettings settings)
        {
            settings.RequireValid();
            if (settings.PathSampleCount != m_Settings.PathSampleCount)
                throw new InvalidOperationException("Predictive Foot Placement tuning cannot change path workspace capacity.");
            m_Settings = settings;
            m_Query = new CharacterPredictiveFootPlacementQuery(m_World, settings);
        }

        internal void Reset() => m_Diagnostics = default;

        CharacterFullBodyIkGoal ModifyFoot(
            CharacterFootSide side,
            bool selectedForRewrite,
            CharacterFootPlacementAnimatedFootPose pose,
            AnimationFootFeatureSample feature,
            CharacterFootGroundingFootDiagnostics grounding,
            CharacterFullBodyIkGoal baseline,
            CharacterBodyPresentationFrame body,
            float legLength,
            out CharacterPredictiveFootPlacementModifierFootDiagnostics diagnostics)
        {
            bool swingEligible = grounding.ContactState == CharacterFootContactState.Swing &&
                                 grounding.AnchorBlendWeight <= 0.0001f;
            FootPredictionRejectReason rejectReason = FootPredictionRejectReason.None;
            FootPlacementSurface futureSupport = default;
            FootPlacementGroundEnvelope groundEnvelope = default;
            int queryCount = 0;
            int rejectedQueryCount = 0;
            float horizon = Mathf.Clamp(
                feature.NextLandingDelaySeconds,
                m_Settings.MinimumLookAheadSeconds,
                m_Settings.MaximumLookAheadSeconds);
            float clearance = 0f;
            CharacterFullBodyIkGoal result = baseline;
            bool rewritten = false;
            if (!swingEligible)
            {
                rejectReason = FootPredictionRejectReason.NotSwing;
            }
            else if (!selectedForRewrite)
            {
                rejectReason = FootPredictionRejectReason.NotSelected;
            }
            else if (feature.NextLandingConfidence < m_Settings.MinimumLandingConfidence)
            {
                rejectReason = FootPredictionRejectReason.LandingConfidenceInsufficient;
            }
            else if (Mathf.Abs(body.VisibleYawVelocityDegreesPerSecond) >
                     m_Settings.MaximumYawVelocityDegreesPerSecond)
            {
                rejectReason = FootPredictionRejectReason.AngularVelocityExceeded;
            }
            else
            {
                Transform root = m_Rig.PoseRoot;
                Vector3 up = root.up.normalized;
                Vector3 localLanding = new Vector3(
                    feature.NextLandingLocalOffset.x,
                    0f,
                    feature.NextLandingLocalOffset.y);
                Vector3 predicted = pose.AnklePosition +
                                    root.rotation * localLanding +
                                    body.VisibleVelocity * horizon;
                if (!IsFinite(predicted))
                {
                    rejectReason = FootPredictionRejectReason.NonFinite;
                }
                else if (Vector3.Distance(pose.AnklePosition, predicted) >
                         m_Settings.MaximumPredictionDistance)
                {
                    rejectReason = FootPredictionRejectReason.DistanceExceeded;
                }
                else
                {
                    CharacterPredictiveFootPlacementQueryResult query = m_Query.Query(
                        side == CharacterFootSide.Left ? 0 : 1,
                        (pose.HeelPosition + pose.ToePosition) * 0.5f,
                        predicted,
                        pose.HipPosition,
                        legLength,
                        m_GroundLayerMask,
                        up);
                    futureSupport = query.FutureLandingSupport;
                    groundEnvelope = query.GroundEnvelope;
                    queryCount = query.QueryCount;
                    rejectedQueryCount = query.RejectedCount;
                    clearance = query.SwingClearance;
                    if (!futureSupport.IsValid)
                    {
                        rejectReason = FootPredictionRejectReason.NoFutureLanding;
                    }
                    else
                    {
                        Vector3 target = futureSupport.Point +
                                         up * (Mathf.Max(0f, feature.SoleHeight) + clearance);
                        float extensionRatio = Vector3.Distance(pose.HipPosition, target) / legLength;
                        if (!float.IsFinite(extensionRatio) ||
                            extensionRatio > m_Settings.MaximumPredictionReachRatio)
                        {
                            rejectReason = FootPredictionRejectReason.ReachExceeded;
                        }
                        else
                        {
                            Quaternion semanticRotation = BuildSemanticRotation(pose, futureSupport.Normal);
                            Quaternion rotation =
                                (semanticRotation * Quaternion.Inverse(pose.SoleFrameLocalRotation)).normalized;
                            Vector3 componentPosition =
                                Quaternion.Inverse(root.rotation) * (target - root.position);
                            Quaternion componentRotation =
                                (Quaternion.Inverse(root.rotation) * rotation).normalized;
                            result = new CharacterFullBodyIkGoal(
                                baseline.Slot,
                                componentPosition,
                                componentRotation,
                                baseline.PositionWeight,
                                baseline.RotationWeight,
                                CharacterFullBodyIkGoalApplication.FootPlacementEffectorTarget,
                                baseline.SourceKind | CharacterFullBodyIkGoalSourceKind.PredictiveExtension,
                                baseline.DiagnosticMetadataIndex);
                            rewritten = true;
                        }
                    }
                }
            }
            diagnostics = new CharacterPredictiveFootPlacementModifierFootDiagnostics(
                side,
                swingEligible,
                selectedForRewrite,
                rewritten,
                rejectReason,
                new CharacterFootGroundingHitDiagnostics(futureSupport),
                groundEnvelope.Count,
                groundEnvelope.RejectReason,
                queryCount,
                rejectedQueryCount,
                horizon,
                clearance,
                baseline,
                result);
            return result;
        }

        void RequireValidInput(
            in CharacterFootPlacementPlanningFrame frame,
            in CharacterFullBodyIkGoalSetHeader baselineHeader,
            NativeSlice<CharacterFullBodyIkGoal> baselineGoals,
            in CharacterFootGroundingDiagnostics groundingDiagnostics,
            NativeSlice<CharacterFullBodyIkGoal> output,
            int goalWorkspaceOffset,
            int producerOperationIndex,
            int producerCallSiteIndex)
        {
            if (frame.ActorId != m_ActorId ||
                !frame.Body.IsValid ||
                !baselineHeader.IsValid ||
                baselineHeader.Availability != CharacterFullBodyIkGoalSetAvailability.Ready ||
                baselineHeader.FrameSequence != frame.RenderFrame ||
                baselineHeader.CompletionIdentity != frame.CompletionIdentity ||
                !baselineHeader.RigId.Equals(m_RigId) ||
                !baselineHeader.RigRevision.Equals(m_RigRevision) ||
                baselineHeader.GoalCount != 3 ||
                baselineGoals.Length != 3 ||
                output.Length != 3 ||
                goalWorkspaceOffset < 0 ||
                producerOperationIndex < 0 ||
                producerCallSiteIndex < 0 ||
                producerOperationIndex == baselineHeader.ProducerOperationIndex ||
                RangesOverlap(baselineHeader.GoalOffset, 3, goalWorkspaceOffset, 3) ||
                !groundingDiagnostics.IsCompleted ||
                groundingDiagnostics.FrameSequence != frame.RenderFrame ||
                groundingDiagnostics.CompletionIdentity != frame.CompletionIdentity ||
                !IsBaselineGoal(baselineGoals[0], CharacterFullBodyIkEffectorSlot.PelvisPreSolveTranslation, CharacterFullBodyIkGoalApplication.PelvisPreSolveTranslation, 0) ||
                !IsBaselineGoal(baselineGoals[1], CharacterFullBodyIkEffectorSlot.LeftFoot, CharacterFullBodyIkGoalApplication.FootPlacementEffectorTarget, 1) ||
                !IsBaselineGoal(baselineGoals[2], CharacterFullBodyIkEffectorSlot.RightFoot, CharacterFullBodyIkGoalApplication.FootPlacementEffectorTarget, 2) ||
                !SameGoal(baselineGoals[1], groundingDiagnostics.Left.Goal) ||
                !SameGoal(baselineGoals[2], groundingDiagnostics.Right.Goal))
            {
                throw new ArgumentException("Predictive Foot Placement Modifier input is invalid.");
            }
        }

        static CharacterFootSide SelectRewriteSide(
            CharacterFootGroundingFootDiagnostics leftGrounding,
            AnimationFootFeatureSample leftFeature,
            CharacterFootGroundingFootDiagnostics rightGrounding,
            AnimationFootFeatureSample rightFeature)
        {
            bool left = IsSwingEligible(leftGrounding);
            bool right = IsSwingEligible(rightGrounding);
            if (!left)
                return right ? CharacterFootSide.Right : 0;
            if (!right)
                return CharacterFootSide.Left;
            if (!Mathf.Approximately(leftFeature.NextLandingDelaySeconds, rightFeature.NextLandingDelaySeconds))
            {
                return leftFeature.NextLandingDelaySeconds < rightFeature.NextLandingDelaySeconds
                    ? CharacterFootSide.Left
                    : CharacterFootSide.Right;
            }
            if (!Mathf.Approximately(leftFeature.NextLandingConfidence, rightFeature.NextLandingConfidence))
            {
                return leftFeature.NextLandingConfidence > rightFeature.NextLandingConfidence
                    ? CharacterFootSide.Left
                    : CharacterFootSide.Right;
            }
            return CharacterFootSide.Left;
        }

        static bool IsSwingEligible(CharacterFootGroundingFootDiagnostics grounding) =>
            grounding.ContactState == CharacterFootContactState.Swing &&
            grounding.AnchorBlendWeight <= 0.0001f;

        static bool IsBaselineGoal(
            CharacterFullBodyIkGoal goal,
            CharacterFullBodyIkEffectorSlot slot,
            CharacterFullBodyIkGoalApplication application,
            int metadataIndex) =>
            goal.IsValid &&
            goal.Slot == slot &&
            goal.Application == application &&
            goal.SourceKind == CharacterFullBodyIkGoalSourceKind.FootGrounding &&
            goal.DiagnosticMetadataIndex == metadataIndex;

        static bool SameGoal(CharacterFullBodyIkGoal left, CharacterFullBodyIkGoal right) =>
            left.Slot == right.Slot &&
            left.ComponentPosition == right.ComponentPosition &&
            left.ComponentRotation == right.ComponentRotation &&
            left.PositionWeight == right.PositionWeight &&
            left.RotationWeight == right.RotationWeight &&
            left.Application == right.Application &&
            left.SourceKind == right.SourceKind &&
            left.DiagnosticMetadataIndex == right.DiagnosticMetadataIndex;

        static bool RangesOverlap(int leftOffset, int leftCount, int rightOffset, int rightCount) =>
            leftOffset < rightOffset + rightCount && rightOffset < leftOffset + leftCount;

        static Quaternion BuildSemanticRotation(
            CharacterFootPlacementAnimatedFootPose pose,
            Vector3 normal)
        {
            Vector3 forward = Vector3.ProjectOnPlane(pose.SoleForward, normal);
            if (forward.sqrMagnitude <= 0.000001f)
                forward = Vector3.ProjectOnPlane(pose.AnkleRotation * Vector3.forward, normal);
            if (forward.sqrMagnitude <= 0.000001f)
                forward = Vector3.Cross(normal, Vector3.right);
            if (forward.sqrMagnitude <= 0.000001f)
                forward = Vector3.Cross(normal, Vector3.forward);
            return Quaternion.LookRotation(forward.normalized, normal).normalized;
        }

        static bool IsFinite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }
}
