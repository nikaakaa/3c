using System.Collections.Generic;
using BTSMTL.Diagnostics;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation.Diagnostics
{
    internal static class AnimationPresentationTracePublisher
    {
        static bool s_LoggedCompletedFootIkAvailable;
        static bool s_LoggedCompletedFootIkUnavailable;
        internal static AnimationPresentationDiagnosticsInterest ResolveInterest(
            RuntimeDiagnosticsContext diagnostics)
        {
            if (diagnostics == null)
                return AnimationPresentationDiagnosticsInterest.None;
            if (diagnostics.ShouldCapture(
                    RuntimeTraceChannel.Animation,
                    RuntimeTraceEventKind.AnimationPlaybackPending) ||
                diagnostics.ShouldCapture(
                    RuntimeTraceChannel.FootPlacement,
                    RuntimeTraceEventKind.FootPlacementSnapshot))
            {
                return AnimationPresentationDiagnosticsInterest.Capture;
            }
            return diagnostics.ShouldPublish(
                       RuntimeTraceChannel.Animation,
                       RuntimeTraceEventKind.AnimationPlaybackPending) ||
                   diagnostics.ShouldPublish(
                       RuntimeTraceChannel.FootPlacement,
                       RuntimeTraceEventKind.FootPlacementSnapshot)
                ? AnimationPresentationDiagnosticsInterest.LiveState
                : AnimationPresentationDiagnosticsInterest.None;
        }

        internal static void Publish(
            RuntimeDiagnosticsContext diagnostics,
            AnimationPresentationDebugView debugView,
            IReadOnlyList<AnimationPlaybackId>
                retiredPlaybacks)
        {
            if (diagnostics == null || debugView == null)
                return;
            PublishActionPlaybacks(
                diagnostics,
                debugView.ActionPlaybacks);
            PublishMarkerPlaybacks(
                diagnostics,
                debugView.ActionMarkerPlaybacks);
            PublishMarkerRelations(
                diagnostics,
                debugView.ActionMarkerRelations);
            PublishPoseStateSourceRelations(
                diagnostics,
                debugView.PoseStateSourceSyncRelations);
            PublishRetirements(
                diagnostics,
                retiredPlaybacks);
            PublishFootIk(diagnostics, debugView.PosePlan.FootIk);
        }

        static void PublishFootIk(
            RuntimeDiagnosticsContext diagnostics,
            AnimationFootIkRuntimeSnapshot snapshot)
        {
            if (!snapshot.IsAvailable ||
                !diagnostics.ShouldPublish(
                    RuntimeTraceChannel.FootPlacement,
                    RuntimeTraceEventKind.FootPlacementSnapshot))
            {
                return;
            }
            RuntimeFootIkTraceSnapshot footIk = BuildFootIkSnapshot(snapshot, out bool hasModifier);
            ref readonly CharacterFootGroundingDiagnostics grounding = ref snapshot.Grounding;
            ref readonly CharacterFullBodyIkSolverDiagnostics solver = ref snapshot.Solver;
            string status = !solver.IsCompleted
                ? "FootGroundingCompleted"
                : solver.Succeeded
                    ? "Solved"
                    : $"SolverFailed/{solver.Failure}";
            diagnostics.Publish(
                RuntimeTraceChannel.FootPlacement,
                RuntimeTraceDomain.Presentation,
                RuntimeTraceEventKind.FootPlacementSnapshot,
                RuntimeSourceElementHandle.Invalid,
                RuntimeInstanceKey.Character(diagnostics.CharacterRuntimeId),
                new RuntimeTracePayload
                {
                    Name = hasModifier
                        ? "FootPlacement -> FullBodyIK"
                        : "FootGrounding -> FullBodyIK",
                    Status = status,
                    OwnerId = grounding.CompletionIdentity.ToString(),
                    RelatedElementId = grounding.FrameSequence.ToString(),
                    Time = grounding.Left.FootFeature.PlantConfidence,
                    SecondaryTime = grounding.CurrentPelvisOffset,
                    NormalizedTime = grounding.Right.FootFeature.PlantConfidence,
                    Weight = footIk.Left.FinalGoalPositionWeight,
                    FinalWeight = footIk.Right.FinalGoalPositionWeight,
                    Flag = solver.Succeeded,
                    Cause = $"PelvisReach({grounding.PelvisPlan.LyraCurrentOffset:0.###}->{grounding.PelvisPlan.ResolvedOffset:0.###})",
                    Detail = $"L contact {grounding.Left.ContactState} placement {grounding.Left.PlacementWeight:0.###} anchor {grounding.Left.AnchorBlendWeight:0.###} soleTarget {grounding.Left.SoleClearanceTarget:0.###} constraint {grounding.Left.SoleConstraintOffset:0.###} groundingResidual {grounding.Left.ResidualSolePenetration:0.###} final {footIk.Left.FinalGoalPositionWeight:0.###} residual {snapshot.LeftFoot.PositionResidual:0.###} | " +
                             $"R contact {grounding.Right.ContactState} placement {grounding.Right.PlacementWeight:0.###} anchor {grounding.Right.AnchorBlendWeight:0.###} soleTarget {grounding.Right.SoleClearanceTarget:0.###} constraint {grounding.Right.SoleConstraintOffset:0.###} groundingResidual {grounding.Right.ResidualSolePenetration:0.###} final {footIk.Right.FinalGoalPositionWeight:0.###} residual {snapshot.RightFoot.PositionResidual:0.###}",
                    FootIk = footIk
                });
        }

        internal static void PublishCompletedFootIk(
            ActorId actorId,
            AnimationFootIkRuntimeSnapshot snapshot)
        {
            if (!snapshot.IsAvailable)
            {
                if (!s_LoggedCompletedFootIkUnavailable)
                {
                    s_LoggedCompletedFootIkUnavailable = true;
                    Debug.Log("GameplayLab Foot IK completed snapshot unavailable.");
                }
                return;
            }
            if (!s_LoggedCompletedFootIkAvailable)
            {
                s_LoggedCompletedFootIkAvailable = true;
                Debug.Log($"GameplayLab Foot IK completed snapshot available for {actorId}.");
            }
            CharacterFootIkCompletedFrameStream.Publish(
                actorId,
                BuildFootIkSnapshot(snapshot, out _));
        }

        static RuntimeFootIkTraceSnapshot BuildFootIkSnapshot(
            AnimationFootIkRuntimeSnapshot snapshot,
            out bool hasModifier)
        {
            ref readonly CharacterFootGroundingDiagnostics grounding = ref snapshot.Grounding;
            ref readonly CharacterPredictiveFootPlacementDiagnostics modifier = ref snapshot.Prediction;
            hasModifier = modifier.IsCompleted &&
                          modifier.FrameSequence == grounding.FrameSequence &&
                          modifier.CompletionIdentity == grounding.CompletionIdentity;
            ref readonly CharacterFullBodyIkSolverDiagnostics solver = ref snapshot.Solver;
            CharacterFootGroundingFootDiagnostics leftGrounding = grounding.Left;
            CharacterFootGroundingFootDiagnostics rightGrounding = grounding.Right;
            CharacterPredictiveFootPlacementFootDiagnostics leftModifier = hasModifier
                ? modifier.Left
                : default;
            CharacterPredictiveFootPlacementFootDiagnostics rightModifier = hasModifier
                ? modifier.Right
                : default;
            CharacterFullBodyIkEffectorDiagnostics leftSolved = snapshot.LeftFoot;
            CharacterFullBodyIkEffectorDiagnostics rightSolved = snapshot.RightFoot;
            return new RuntimeFootIkTraceSnapshot
            {
                IsAvailable = true,
                FrameSequence = grounding.FrameSequence,
                ResetSequence = grounding.ResetSequence,
                GroundingCompletionIdentity = grounding.CompletionIdentity,
                ModifierCompletionIdentity = hasModifier ? modifier.CompletionIdentity : 0,
                SolverCompletionIdentity = solver.OutputCompletionIdentity,
                HasPredictiveModifier = hasModifier,
                SolverBackendIdentity = solver.BackendIdentity,
                SolverFailure = solver.IsCompleted ? solver.Failure.ToString() : "NotCompleted",
                NodeExecuted = grounding.NodeExecuted,
                BodyGrounded = grounding.BodyGrounded,
                PlacementAlpha = grounding.PlacementAlpha,
                PresentationDeltaSeconds = grounding.PresentationDeltaSeconds,
                PoseRootVerticalDelta = grounding.PoseRootVerticalDelta,
                PoseRootWorldPosition = grounding.PoseRootWorldPosition,
                PoseRootWorldRotation = grounding.PoseRootWorldRotation,
                PelvisLyraTargetOffset = grounding.PelvisPlan.LyraTargetOffset,
                PelvisResolvedTargetOffset = grounding.PelvisPlan.ResolvedOffset,
                CurrentPelvisOffset = grounding.CurrentPelvisOffset,
                PelvisSpringVelocity = grounding.PelvisSpringVelocity,
                PreviousPelvisTarget = grounding.PreviousPelvisTarget,
                PelvisSpringInitialized = grounding.PelvisSpringInitialized,
                PelvisPreSolveTranslation = grounding.PelvisPreSolveTranslation,
                PelvisGoalPositionWeight = grounding.PelvisGoal.PositionWeight,
                PelvisGoalApplication = grounding.PelvisGoal.Application.ToString(),
                PelvisGoalSourceKind = grounding.PelvisGoal.SourceKind.ToString(),
                PelvisSupportAvailable = grounding.PelvisSupport.HasSelectedSupport,
                PelvisSupportSide = grounding.PelvisSupport.HasSelectedSupport
                    ? grounding.PelvisSupport.SelectedSide.ToString()
                    : string.Empty,
                PelvisSupportSwitched = grounding.PelvisSupport.SupportSwitched,
                PelvisSupportPlanSequence = grounding.PelvisSupport.SelectedPlanSequence,
                PelvisCurrentSupportTarget = grounding.PelvisSupport.CurrentTarget,
                PelvisSelectedSupportTarget = grounding.PelvisSupport.ResolvedTarget,
                LeftPelvisHasActionConstraint = grounding.PelvisSupport.LeftHasActionConstraint,
                LeftPelvisConstraintMode = grounding.PelvisSupport.LeftConstraintMode.ToString(),
                LeftPelvisSupportPhase = grounding.PelvisSupport.LeftSupportPhase.ToString(),
                LeftPelvisBodyPivotMode = grounding.PelvisSupport.LeftBodyPivotMode.ToString(),
                LeftPelvisCandidate = grounding.PelvisSupport.LeftCandidate,
                LeftPelvisPlanSequence = grounding.PelvisSupport.LeftPlanSequence,
                LeftPelvisDisplacement = grounding.PelvisSupport.LeftDisplacement,
                RightPelvisHasActionConstraint = grounding.PelvisSupport.RightHasActionConstraint,
                RightPelvisConstraintMode = grounding.PelvisSupport.RightConstraintMode.ToString(),
                RightPelvisSupportPhase = grounding.PelvisSupport.RightSupportPhase.ToString(),
                RightPelvisBodyPivotMode = grounding.PelvisSupport.RightBodyPivotMode.ToString(),
                RightPelvisCandidate = grounding.PelvisSupport.RightCandidate,
                RightPelvisPlanSequence = grounding.PelvisSupport.RightPlanSequence,
                RightPelvisDisplacement = grounding.PelvisSupport.RightDisplacement,
                LyraSourceIdentity = grounding.LyraSourceIdentity.ToString(),
                SpringIdentity = grounding.SpringIdentity.ToString(),
                RigId = grounding.RigId.ToString(),
                RigRevision = grounding.RigRevision.ToString(),
                ProfileId = grounding.ProfileId.ToString(),
                ProfileRevision = grounding.ProfileRevision.ToString(),
                PosePlanHash = grounding.PosePlanHash.ToString(),
                CalibrationId = grounding.CalibrationId.ToString(),
                CalibrationRevision = grounding.CalibrationRevision.ToString(),
                PhysicsSceneIdentity = grounding.PhysicsSceneIdentity,
                SelfFilterIdentity = grounding.SelfFilterIdentity,
                BaselineProducerOperationIndex = hasModifier ? modifier.BaselineProducerOperationIndex : -1,
                BaselineProducerCallSiteIndex = hasModifier ? modifier.BaselineProducerCallSiteIndex : -1,
                BaselineGoalOffset = hasModifier ? modifier.BaselineGoalOffset : -1,
                BaselineGoalCount = hasModifier ? modifier.BaselineGoalCount : 0,
                BaselineRigId = hasModifier ? modifier.BaselineRigId.ToString() : string.Empty,
                BaselineRigRevision = hasModifier ? modifier.BaselineRigRevision.ToString() : string.Empty,
                Left = BuildFootTrace(
                    in leftGrounding,
                    in leftModifier,
                    hasModifier,
                    in leftSolved,
                    grounding.PoseRootWorldPosition,
                    grounding.PoseRootWorldRotation,
                    grounding.PoseRootWorldScale),
                Right = BuildFootTrace(
                    in rightGrounding,
                    in rightModifier,
                    hasModifier,
                    in rightSolved,
                    grounding.PoseRootWorldPosition,
                    grounding.PoseRootWorldRotation,
                    grounding.PoseRootWorldScale)
            };
        }

        static RuntimeFootIkLegTraceSnapshot BuildFootTrace(
            in CharacterFootGroundingFootDiagnostics grounding,
            in CharacterPredictiveFootPlacementFootDiagnostics modifier,
            bool hasModifier,
            in CharacterFullBodyIkEffectorDiagnostics solved,
            Vector3 poseRootWorldPosition,
            Quaternion poseRootWorldRotation,
            Vector3 poseRootWorldScale)
        {
            CharacterFullBodyIkGoal finalGoal = hasModifier
                ? modifier.FinalGoal
                : grounding.Goal;
            Vector3 baselineGoalWorldPosition = grounding.SoleAnklePosition;
            Vector3 finalGoalWorldPosition = TransformComponentPoint(
                poseRootWorldPosition,
                poseRootWorldRotation,
                poseRootWorldScale,
                finalGoal.ComponentPosition);
            Quaternion baselineGoalWorldRotation =
                (poseRootWorldRotation * grounding.Goal.ComponentRotation).normalized;
            Quaternion finalGoalWorldRotation =
                (poseRootWorldRotation * finalGoal.ComponentRotation).normalized;
            Quaternion inverseBaselineRotation = Quaternion.Inverse(baselineGoalWorldRotation);
            Vector3 heelOffset = inverseBaselineRotation *
                                 (grounding.SoleHeelPosition - baselineGoalWorldPosition);
            Vector3 toeOffset = inverseBaselineRotation *
                                (grounding.SoleToePosition - baselineGoalWorldPosition);
            Vector3 finalGoalHeelPosition = finalGoalWorldPosition + finalGoalWorldRotation * heelOffset;
            Vector3 finalGoalToePosition = finalGoalWorldPosition + finalGoalWorldRotation * toeOffset;

            string finalPhysicalSupportKind = string.Empty;
            int finalPhysicalSupportSurfaceIdentity = 0;
            Vector3 finalPhysicalSupportPoint = default;
            Vector3 finalPhysicalSupportNormal = default;
            if (grounding.SoleSupport.HasHit)
            {
                finalPhysicalSupportKind = "CurrentGrounding";
                finalPhysicalSupportSurfaceIdentity = grounding.SoleSupport.SurfaceIdentity;
                finalPhysicalSupportPoint = grounding.SoleSupport.Point;
                finalPhysicalSupportNormal = grounding.SoleSupport.Normal;
            }

            bool finalPhysicalEvaluationAvailable = solved.IsAvailable &&
                                                    finalPhysicalSupportSurfaceIdentity != 0 &&
                                                    finalPhysicalSupportNormal.sqrMagnitude > 0.000001f;
            Vector3 solvedAnklePosition = default;
            Vector3 solvedHeelPosition = default;
            Vector3 solvedToePosition = default;
            float finalPhysicalHeelPlaneDistance = 0f;
            float finalPhysicalToePlaneDistance = 0f;
            float finalPhysicalResidualPenetration = 0f;
            if (solved.IsAvailable)
            {
                solvedAnklePosition = TransformComponentPoint(
                    poseRootWorldPosition,
                    poseRootWorldRotation,
                    poseRootWorldScale,
                    solved.SolvedComponentPosition);
                Quaternion solvedWorldRotation =
                    (poseRootWorldRotation * solved.SolvedComponentRotation).normalized;
                solvedHeelPosition = solvedAnklePosition + solvedWorldRotation * heelOffset;
                solvedToePosition = solvedAnklePosition + solvedWorldRotation * toeOffset;
            }
            if (finalPhysicalEvaluationAvailable)
            {
                finalPhysicalSupportNormal.Normalize();
                finalPhysicalHeelPlaneDistance = Vector3.Dot(
                    solvedHeelPosition - finalPhysicalSupportPoint,
                    finalPhysicalSupportNormal);
                finalPhysicalToePlaneDistance = Vector3.Dot(
                    solvedToePosition - finalPhysicalSupportPoint,
                    finalPhysicalSupportNormal);
                finalPhysicalResidualPenetration = Mathf.Max(
                    0f,
                    -Mathf.Min(
                        finalPhysicalHeelPlaneDistance,
                        finalPhysicalToePlaneDistance));
            }
            return new RuntimeFootIkLegTraceSnapshot
            {
                IsAvailable = true,
                DidCurrentTraceHit = grounding.DidTraceHit,
                CurrentSurfaceIdentity = grounding.CurrentHit.SurfaceIdentity,
                CurrentQueryShape = grounding.Query.IsAvailable ? grounding.Query.Shape.ToString() : string.Empty,
                CurrentQueryPurpose = grounding.Query.IsAvailable ? grounding.Query.Purpose.ToString() : string.Empty,
                CurrentQueryFootIndex = grounding.Query.IsAvailable ? grounding.Query.FootIndex : -1,
                CurrentQueryOrigin = grounding.Query.Origin,
                CurrentQueryCapsuleEnd = grounding.Query.CapsuleEnd,
                CurrentQueryDirection = grounding.Query.Direction,
                CurrentQueryRadius = grounding.Query.Radius,
                CurrentQueryMaximumDistance = grounding.Query.MaximumDistance,
                CurrentQueryLayerMask = grounding.Query.LayerMask,
                CurrentQueryMinimumGroundNormalDot = grounding.Query.MinimumGroundNormalDot,
                CurrentHitLocation = grounding.CurrentHit.Location,
                CurrentImpactPoint = grounding.CurrentHit.Point,
                CurrentHitNormal = grounding.CurrentHit.Normal,
                CurrentHitDistance = grounding.CurrentHit.Distance,
                ContactState = grounding.ContactState.ToString(),
                TransitionReason = grounding.TransitionReason.ToString(),
                ContactDecision = grounding.ContactDecision.ToString(),
                ContactSurfaceValid = grounding.ContactSurfaceValid,
                ContactSurfaceDistanceAccepted = grounding.ContactSurfaceDistanceAccepted,
                ContactCaptureSpeedAccepted = grounding.ContactCaptureSpeedAccepted,
                ContactRetentionSpeedAccepted = grounding.ContactRetentionSpeedAccepted,
                ContactConfidenceAccepted = grounding.ContactConfidenceAccepted,
                MaximumContactSurfaceDistance = grounding.MaximumContactSurfaceDistance,
                PlantSpeedThreshold = grounding.PlantSpeedThreshold,
                UnalignmentSpeedThreshold = grounding.UnalignmentSpeedThreshold,
                PlantConfidenceEnter = grounding.PlantConfidenceEnter,
                PlantConfidenceExit = grounding.PlantConfidenceExit,
                AnchorDistance = grounding.AnchorDistance,
                AnchorDistanceAccepted = grounding.AnchorDistanceAccepted,
                MaximumAnchorDistance = grounding.MaximumAnchorDistance,
                AnchorBlendSpeed = grounding.AnchorBlendSpeed,
                HasSurfaceAnchor = grounding.HasSurfaceAnchor,
                SurfaceLocalAnchor = grounding.SurfaceLocalAnchor,
                SurfaceLocalRotation = grounding.SurfaceLocalRotation,
                AnchorWorldPosition = grounding.AnchorWorldPosition,
                AnchorWorldRotation = grounding.AnchorWorldRotation,
                PredictiveRewritten = hasModifier && modifier.Rewritten,
                PredictionRejectReason = hasModifier
                    ? modifier.RejectReason.ToString()
                    : "PredictionUnavailable",
                FutureSurfaceIdentity = hasModifier ? modifier.FutureSupport.SurfaceIdentity : 0,
                FutureSupportPoint = hasModifier ? modifier.FutureSupport.Point : default,
                FutureSupportNormal = hasModifier ? modifier.FutureSupport.Normal : default,
                GroundEnvelopeSegmentCount = hasModifier ? modifier.GroundEnvelopeSegmentCount : 0,
                GroundEnvelopeRejectReason = hasModifier
                    ? modifier.GroundEnvelopeRejectReason.ToString()
                    : "PredictionUnavailable",
                PredictiveQueryCount = hasModifier ? modifier.QueryCount : 0,
                PredictiveRejectedQueryCount = hasModifier ? modifier.RejectedQueryCount : 0,
                PredictiveRawHitCount = hasModifier ? modifier.RawHitCount : 0,
                PredictiveRejectNoCandidateCount = hasModifier ? modifier.QueryRejectCounts.NoCandidate : 0,
                PredictiveRejectHeightDiscontinuityCount = hasModifier ? modifier.QueryRejectCounts.HeightDiscontinuity : 0,
                PredictiveRejectEdgeGapCount = hasModifier ? modifier.QueryRejectCounts.EdgeGap : 0,
                PredictiveRejectSurfaceDiscontinuityCount = hasModifier ? modifier.QueryRejectCounts.SurfaceDiscontinuity : 0,
                PredictiveRejectReachExceededCount = hasModifier ? modifier.QueryRejectCounts.ReachExceeded : 0,
                PredictiveRejectSlopeExceededCount = hasModifier ? modifier.QueryRejectCounts.SlopeExceeded : 0,
                PredictiveRejectStepExceededCount = hasModifier ? modifier.QueryRejectCounts.StepExceeded : 0,
                PredictiveRejectInvalidCandidateCount = hasModifier ? modifier.QueryRejectCounts.InvalidCandidate : 0,
                PredictiveRejectUnsupportedCenterCount = hasModifier ? modifier.QueryRejectCounts.UnsupportedCenter : 0,
                FutureLandingQueryAvailable = hasModifier && modifier.FutureLandingQuery.IsAvailable,
                FutureLandingQueryShape = hasModifier && modifier.FutureLandingQuery.IsAvailable
                    ? modifier.FutureLandingQuery.Shape.ToString()
                    : string.Empty,
                FutureLandingQueryPurpose = hasModifier && modifier.FutureLandingQuery.IsAvailable
                    ? modifier.FutureLandingQuery.Purpose.ToString()
                    : string.Empty,
                FutureLandingQueryOrigin = hasModifier ? modifier.FutureLandingQuery.Origin : default,
                FutureLandingQueryDirection = hasModifier ? modifier.FutureLandingQuery.Direction : default,
                FutureLandingQueryRadius = hasModifier ? modifier.FutureLandingQuery.Radius : 0f,
                FutureLandingQueryMaximumDistance = hasModifier ? modifier.FutureLandingQuery.MaximumDistance : 0f,
                FutureLandingQueryMinimumGroundNormalDot = hasModifier ? modifier.FutureLandingQuery.MinimumGroundNormalDot : 0f,
                FootFeatureValid = hasModifier && modifier.CurrentEvent.FootFeatureValid,
                PredictedStepValid = hasModifier && modifier.CurrentEvent.PredictedStepValid,
                PredictedStepHasLandingEvent = hasModifier && modifier.CurrentEvent.HasLandingEvent,
                PredictedStepSourceBound = hasModifier && modifier.CurrentEvent.IsSourceBound,
                HasAuthoritativeLandingEvent = hasModifier && modifier.HasAuthoritativeLandingEvent,
                ExpectedLandingEventIdentity = hasModifier ? modifier.CurrentEvent.ExpectedLandingEventIdentity : 0,
                LandingEventIdentityValid = hasModifier && modifier.CurrentEvent.LandingEventIdentityValid,
                CurrentEventIsPreSwing = hasModifier && modifier.CurrentEvent.IsPreSwing,
                CurrentEventIsSwing = hasModifier && modifier.CurrentEvent.IsSwing,
                LandingEventIdentity = hasModifier ? modifier.LandingEventIdentity : 0,
                SourceSampleIdentity = hasModifier ? modifier.SourceSampleIdentity : 0,
                SourceSampleCycle = hasModifier ? modifier.SourceSampleCycle : 0,
                EventOrdinal = hasModifier ? modifier.EventOrdinal : 0,
                ContributionContinuityIdentity = hasModifier
                    ? modifier.ContributionContinuityIdentity
                    : 0,
                CurrentEventFootPoseWeight = hasModifier
                    ? modifier.CurrentEventFootPoseWeight
                    : 0f,
                PlanPredictionBlend = hasModifier
                    ? modifier.PlanPredictionBlend
                    : 0f,
                AuthoritativePredictionBlend = hasModifier
                    ? modifier.AuthoritativePredictionBlend
                    : 0f,
                HasPlanRevision = hasModifier && modifier.HasPlanRevision,
                RevisionPlanSequence = hasModifier ? modifier.RevisionPlanSequence : 0,
                PlanRevisionBlendWeight = hasModifier ? modifier.PlanRevisionBlendWeight : 0f,
                PlanFadingOut = hasModifier && modifier.PlanFadingOut,
                PlanRetentionWeight = hasModifier ? modifier.PlanRetentionWeight : 0f,
                IntentLandingDisplacementError = hasModifier
                    ? modifier.IntentLandingDisplacementError
                    : 0f,
                IntentLandingDisplacementThreshold = hasModifier
                    ? modifier.IntentLandingDisplacementThreshold
                    : 0f,
                LandingConfidence = hasModifier ? modifier.LandingConfidence : 0f,
                AuthoredLandingDelaySeconds = hasModifier ? modifier.AuthoredLandingDelaySeconds : 0f,
                LandingEventPhase = hasModifier ? modifier.EventPhase : 0f,
                LandingLiftOffPhase = hasModifier ? modifier.LiftOffPhase : 0f,
                RootLocalLanding = hasModifier ? modifier.RootLocalLanding : default,
                RootLocalRouteSample0 = BuildRootLocalRouteSample(in modifier, hasModifier, 0),
                RootLocalRouteSample1 = BuildRootLocalRouteSample(in modifier, hasModifier, 1),
                RootLocalRouteSample2 = BuildRootLocalRouteSample(in modifier, hasModifier, 2),
                RootLocalRouteSample3 = BuildRootLocalRouteSample(in modifier, hasModifier, 3),
                RootLocalRouteSample4 = BuildRootLocalRouteSample(in modifier, hasModifier, 4),
                RootLocalRouteSample5 = BuildRootLocalRouteSample(in modifier, hasModifier, 5),
                RootLocalRouteSample6 = BuildRootLocalRouteSample(in modifier, hasModifier, 6),
                RootLocalRouteSample7 = BuildRootLocalRouteSample(in modifier, hasModifier, 7),
                RootLocalRouteSample8 = BuildRootLocalRouteSample(in modifier, hasModifier, 8),
                RootLocalRouteSample9 = BuildRootLocalRouteSample(in modifier, hasModifier, 9),
                RootLocalRouteSample10 = BuildRootLocalRouteSample(in modifier, hasModifier, 10),
                RootLocalRouteSample11 = BuildRootLocalRouteSample(in modifier, hasModifier, 11),
                RootLocalRouteSample12 = BuildRootLocalRouteSample(in modifier, hasModifier, 12),
                RootLocalRouteSample13 = BuildRootLocalRouteSample(in modifier, hasModifier, 13),
                RootLocalRouteSample14 = BuildRootLocalRouteSample(in modifier, hasModifier, 14),
                RootLocalRouteSample15 = BuildRootLocalRouteSample(in modifier, hasModifier, 15),
                RootLocalRouteSample16 = BuildRootLocalRouteSample(in modifier, hasModifier, 16),
                RootLocalRouteSample17 = BuildRootLocalRouteSample(in modifier, hasModifier, 17),
                RootLocalRouteSample18 = BuildRootLocalRouteSample(in modifier, hasModifier, 18),
                RootLocalRouteSample19 = BuildRootLocalRouteSample(in modifier, hasModifier, 19),
                RootLocalRouteSample20 = BuildRootLocalRouteSample(in modifier, hasModifier, 20),
                RootLocalRouteSample21 = BuildRootLocalRouteSample(in modifier, hasModifier, 21),
                RootLocalRouteSample22 = BuildRootLocalRouteSample(in modifier, hasModifier, 22),
                RootLocalRouteSample23 = BuildRootLocalRouteSample(in modifier, hasModifier, 23),
                RootLocalRouteSample24 = BuildRootLocalRouteSample(in modifier, hasModifier, 24),
                AuthoredFootRouteStart = hasModifier
                    ? modifier.CurrentEvent.AuthoredFootRouteStart
                    : default,
                AuthoredFootRouteLanding = hasModifier
                    ? modifier.CurrentEvent.AuthoredFootRouteLanding
                    : default,
                PredictionDistance = hasModifier ? modifier.PredictionDistance : 0f,
                PredictivePlanSequence = hasModifier ? modifier.PlanSequence : 0,
                PredictivePlanGeneratedFrame = hasModifier ? modifier.PlanGeneratedFrame : 0,
                PredictivePlanGenerationPhase = hasModifier ? modifier.PlanGenerationPhase : 0f,
                IncomingPredictedStepValid = hasModifier && modifier.IncomingEvent.PredictedStepValid,
                IncomingLandingEventIdentityValid = hasModifier && modifier.IncomingEvent.LandingEventIdentityValid,
                IncomingLandingEventIdentity = hasModifier ? modifier.IncomingEvent.LandingEventIdentity : 0,
                IncomingEventPhase = hasModifier ? modifier.IncomingEvent.EventPhase : 0f,
                IncomingLiftOffPhase = hasModifier ? modifier.IncomingEvent.LiftOffPhase : 0f,
                PredictivePlanState = hasModifier ? modifier.PlanState.ToString() : "PredictionUnavailable",
                PredictivePlanTransitionReason = hasModifier
                    ? modifier.PlanTransitionReason.ToString()
                    : "PredictionUnavailable",
                PredictivePlanEndReason = hasModifier
                    ? modifier.PlanEndReason.ToString()
                    : "PredictionUnavailable",
                PredictiveExecutionProgress = hasModifier ? modifier.PlanExecutionProgress : 0f,
                PlanLandingEventIdentity = hasModifier ? modifier.Plan.LandingEventIdentity : 0,
                PlanSourceSampleIdentity = hasModifier ? modifier.Plan.SourceSampleIdentity : 0,
                PlanSourceSampleCycle = hasModifier ? modifier.Plan.SourceSampleCycle : 0,
                PlanEventOrdinal = hasModifier ? modifier.Plan.EventOrdinal : 0,
                PlanContributionContinuityIdentity = hasModifier ? modifier.Plan.ContributionContinuityIdentity : 0,
                PlanElapsedSeconds = hasModifier ? modifier.Plan.ElapsedSeconds : 0f,
                PlanSecondsToLiftOff = hasModifier ? modifier.Plan.SecondsToLiftOff : 0f,
                PlanSwingDuration = hasModifier ? modifier.Plan.SwingDuration : 0f,
                PlanHasPathGeometry = hasModifier && modifier.Plan.HasPathGeometry,
                PlanHasExecutablePath = hasModifier && modifier.Plan.HasExecutablePath,
                FrozenPlanarVelocity = hasModifier ? modifier.FrozenPlanarVelocity : default,
                FrozenYawVelocityDegreesPerSecond = hasModifier
                    ? modifier.FrozenYawVelocityDegreesPerSecond
                    : 0f,
                FrozenMaximumYawVelocityDegreesPerSecond = hasModifier
                    ? modifier.FrozenMaximumYawVelocityDegreesPerSecond
                    : 0f,
                MotionLinearLandingError = hasModifier ? modifier.MotionLinearLandingError : 0f,
                MotionAngularLandingError = hasModifier ? modifier.MotionAngularLandingError : 0f,
                MotionLandingError = hasModifier ? modifier.MotionLandingError : 0f,
                MotionLandingTolerance = hasModifier ? modifier.MotionLandingTolerance : 0f,
                CurrentSoleWorldPosition = hasModifier ? modifier.CurrentSoleWorldPosition : default,
                FixedPathStartWorldPosition = hasModifier
                    ? modifier.FixedPathStartWorldPosition
                    : default,
                FixedLandingWorldPosition = hasModifier
                    ? modifier.FixedLandingWorldPosition
                    : default,
                CurrentPathWorldPosition = hasModifier ? modifier.CurrentPathWorldPosition : default,
                CurrentPathRootWorldPosition = hasModifier
                    ? modifier.CurrentPathRootWorldPosition
                    : default,
                CurrentPathHipWorldPosition = hasModifier
                    ? modifier.CurrentPathHipWorldPosition
                    : default,
                PredictedHipWorldPosition = hasModifier ? modifier.PredictedHipWorldPosition : default,
                FrozenRootStartWorldPosition = hasModifier ? modifier.FrozenRootStartWorldPosition : default,
                FrozenRootStartWorldRotation = hasModifier ? modifier.FrozenRootStartWorldRotation : default,
                FrozenRootLandingWorldPosition = hasModifier ? modifier.FrozenRootLandingWorldPosition : default,
                FrozenRootLandingWorldRotation = hasModifier ? modifier.FrozenRootLandingWorldRotation : default,
                PredictionUp = hasModifier ? modifier.PredictionUp : default,
                MinimumLandingConfidence = hasModifier ? modifier.MinimumLandingConfidence : 0f,
                MaximumPredictionReachRatio = hasModifier ? modifier.MaximumPredictionReachRatio : 0f,
                PredictionReachRatio = hasModifier ? modifier.PredictionReachRatio : 0f,
                CastAbove = hasModifier ? modifier.CastAbove : 0f,
                CastBelow = hasModifier ? modifier.CastBelow : 0f,
                PredictiveRouteSampleCount = hasModifier ? modifier.RouteSampleCount : 0,
                PredictiveAcceptedHitCount = hasModifier ? modifier.AcceptedHitCount : 0,
                PredictiveEdgePlaneCandidateCount = hasModifier ? modifier.EdgePlaneCandidateCount : 0,
                PredictiveAcceptedEdgePlaneCount = hasModifier ? modifier.AcceptedEdgePlaneCount : 0,
                PathSphereRadius = hasModifier ? modifier.PathSphereRadius : 0f,
                SwingCapsuleRadius = hasModifier ? modifier.SwingCapsuleRadius : 0f,
                SoleSupportRadius = hasModifier ? modifier.SoleSupportRadius : 0f,
                CurrentPathSurfaceIdentity = hasModifier
                    ? modifier.CurrentPathSupport.SurfaceIdentity
                    : 0,
                CurrentPathSupportPoint = hasModifier
                    ? modifier.CurrentPathSupport.Point
                    : default,
                CurrentPathSupportNormal = hasModifier
                    ? modifier.CurrentPathSupport.Normal
                    : default,
                PreClearanceHeelPathDistance = hasModifier
                    ? modifier.PreClearanceHeelPathDistance
                    : 0f,
                PreClearanceToePathDistance = hasModifier
                    ? modifier.PreClearanceToePathDistance
                    : 0f,
                PostClearanceHeelPathDistance = hasModifier
                    ? modifier.PostClearanceHeelPathDistance
                    : 0f,
                PostClearanceToePathDistance = hasModifier
                    ? modifier.PostClearanceToePathDistance
                    : 0f,
                PredictiveClearanceEvaluated = hasModifier && modifier.ClearanceEvaluated,
                PredictiveResidualPenetration = hasModifier
                    ? modifier.PredictiveResidualPenetration
                    : 0f,
                AuthoredAnimationClearance = hasModifier
                    ? modifier.AuthoredAnimationClearance
                    : 0f,
                AnimationClearanceContinuityOffset = hasModifier
                    ? modifier.AnimationClearanceContinuityOffset
                    : 0f,
                AnimationClearanceContinuityContribution = hasModifier
                    ? modifier.AnimationClearanceContinuityContribution
                    : 0f,
                ReachClearance = hasModifier
                    ? modifier.ReachClearance
                    : 0f,
                CompositeAnimationClearance = hasModifier
                    ? modifier.CompositeAnimationClearance
                    : 0f,
                PlannedFootRouteWorldSampleCount = hasModifier
                    ? modifier.PlannedFootRouteWorld.Length
                    : 0,
                PlannedFootRouteWorldSample0 = BuildPlannedFootRouteSample(in modifier, hasModifier, 0),
                PlannedFootRouteWorldSample1 = BuildPlannedFootRouteSample(in modifier, hasModifier, 1),
                PlannedFootRouteWorldSample2 = BuildPlannedFootRouteSample(in modifier, hasModifier, 2),
                PlannedFootRouteWorldSample3 = BuildPlannedFootRouteSample(in modifier, hasModifier, 3),
                PlannedFootRouteWorldSample4 = BuildPlannedFootRouteSample(in modifier, hasModifier, 4),
                PlannedFootRouteWorldSample5 = BuildPlannedFootRouteSample(in modifier, hasModifier, 5),
                PlannedFootRouteWorldSample6 = BuildPlannedFootRouteSample(in modifier, hasModifier, 6),
                PredictivePathDiagnosticSampleCount = hasModifier
                    ? Mathf.Min(modifier.PathSamples.Length, 8)
                    : 0,
                PredictivePathSample0 = BuildPathSample(in modifier, hasModifier, 0),
                PredictivePathSample1 = BuildPathSample(in modifier, hasModifier, 1),
                PredictivePathSample2 = BuildPathSample(in modifier, hasModifier, 2),
                PredictivePathSample3 = BuildPathSample(in modifier, hasModifier, 3),
                PredictivePathSample4 = BuildPathSample(in modifier, hasModifier, 4),
                PredictivePathSample5 = BuildPathSample(in modifier, hasModifier, 5),
                PredictivePathSample6 = BuildPathSample(in modifier, hasModifier, 6),
                PredictivePathSample7 = BuildPathSample(in modifier, hasModifier, 7),
                RequiredLift = hasModifier ? modifier.RequiredLift : 0f,
                AppliedLift = hasModifier ? modifier.AppliedLift : 0f,
                BaselineGoalWorldPosition = baselineGoalWorldPosition,
                FinalGoalWorldPosition = finalGoalWorldPosition,
                BaselineGoalApplication = grounding.Goal.Application.ToString(),
                FinalGoalSourceKind = finalGoal.SourceKind.ToString(),
                SolverResultAvailable = solved.IsAvailable,
                PlantConfidence = grounding.FootFeature.PlantConfidence,
                PlantContact = grounding.PlantContact,
                SoleHeight = grounding.FootFeature.SoleHeight,
                PlacementWeight = grounding.PlacementWeight,
                AnimationFootSpeed = grounding.AnimationFootSpeed,
                SurfaceDistance = grounding.SurfaceDistance,
                SoleSupportSurfaceIdentity = grounding.SoleSupport.SurfaceIdentity,
                SoleSupportPoint = grounding.SoleSupport.Point,
                SoleSupportNormal = grounding.SoleSupport.Normal,
                SoleClearanceTarget = grounding.SoleClearanceTarget,
                SoleClearanceTargetTranslation = grounding.SoleClearanceTargetTranslation,
                SoleAnklePosition = grounding.SoleAnklePosition,
                SoleHeelPosition = grounding.SoleHeelPosition,
                SoleToePosition = grounding.SoleToePosition,
                SoleHeelPlaneDistance = grounding.SoleHeelPlaneDistance,
                SoleToePlaneDistance = grounding.SoleToePlaneDistance,
                ResidualSolePenetration = grounding.ResidualSolePenetration,
                FinalGoalSoleHeelPosition = finalGoalHeelPosition,
                FinalGoalSoleToePosition = finalGoalToePosition,
                SolvedSoleAnklePosition = solvedAnklePosition,
                SolvedSoleHeelPosition = solvedHeelPosition,
                SolvedSoleToePosition = solvedToePosition,
                FinalPhysicalEvaluationAvailable = finalPhysicalEvaluationAvailable,
                FinalPhysicalSupportKind = finalPhysicalSupportKind,
                FinalPhysicalSupportSurfaceIdentity = finalPhysicalSupportSurfaceIdentity,
                FinalPhysicalSupportPoint = finalPhysicalSupportPoint,
                FinalPhysicalSupportNormal = finalPhysicalSupportNormal,
                FinalPhysicalHeelPlaneDistance = finalPhysicalHeelPlaneDistance,
                FinalPhysicalToePlaneDistance = finalPhysicalToePlaneDistance,
                FinalPhysicalResidualPenetration = finalPhysicalResidualPenetration,
                AnimatedAnkleComponentY = grounding.AnimatedAnkleComponentY,
                AnchorBlendWeight = grounding.AnchorBlendWeight,
                BaselineGoalPositionWeight = grounding.Goal.PositionWeight,
                BaselineGoalRotationWeight = grounding.Goal.RotationWeight,
                FinalGoalPositionWeight = finalGoal.PositionWeight,
                FinalGoalRotationWeight = finalGoal.RotationWeight,
                TargetOffset = grounding.TargetOffset,
                OffsetTarget = grounding.OffsetTarget,
                UnconstrainedOffset = grounding.UnconstrainedOffset,
                SoleConstraintOffset = grounding.SoleConstraintOffset,
                CurrentOffset = grounding.CurrentOffset,
                OffsetSpringVelocity = grounding.OffsetSpringVelocity,
                PreviousOffsetTarget = grounding.PreviousOffsetTarget,
                OffsetSpringInitialized = grounding.OffsetSpringInitialized,
                TargetNormal = grounding.TargetNormal,
                CurrentNormal = grounding.CurrentNormal,
                NormalSpringVelocity = grounding.NormalSpringVelocity,
                PreviousNormalTarget = grounding.PreviousNormalTarget,
                NormalSpringInitialized = grounding.NormalSpringInitialized,
                PredictionHorizon = hasModifier ? modifier.PredictionHorizon : 0f,
                CurrentGroundingComponentPosition = grounding.CurrentGroundingComponentPosition,
                BaselineGoalComponentPosition = grounding.Goal.ComponentPosition,
                FinalGoalComponentPosition = finalGoal.ComponentPosition,
                SolvedComponentPosition = solved.SolvedComponentPosition,
                PositionResidual = solved.PositionResidual,
                RotationResidualDegrees = solved.RotationResidualDegrees
            };
        }

        static Vector3 TransformComponentPoint(
            Vector3 position,
            Quaternion rotation,
            Vector3 scale,
            Vector3 point) =>
            position + rotation * Vector3.Scale(scale, point);

        static Vector3 BuildRootLocalRouteSample(
            in CharacterPredictiveFootPlacementFootDiagnostics modifier,
            bool hasModifier,
            int index) =>
            hasModifier && index >= 0 && index < modifier.RootLocalFootRoute.Length
                ? modifier.RootLocalFootRoute[index]
                : default;

        static Vector3 BuildPlannedFootRouteSample(
            in CharacterPredictiveFootPlacementFootDiagnostics modifier,
            bool hasModifier,
            int index) =>
            hasModifier && index >= 0 && index < modifier.PlannedFootRouteWorld.Length
                ? modifier.PlannedFootRouteWorld[index]
                : default;

        static RuntimeFootIkPathSampleSnapshot BuildPathSample(
            in CharacterPredictiveFootPlacementFootDiagnostics modifier,
            bool hasModifier,
            int index)
        {
            if (!hasModifier || index < 0 || index >= 8 || modifier.PathSamples.Length <= 0)
            {
                return default;
            }
            int available = Mathf.Min(modifier.PathSamples.Length, 8);
            if (index >= available)
                return default;
            int sourceIndex = modifier.PathSamples.Length <= 8 || available <= 1
                ? index
                : Mathf.RoundToInt(index * (modifier.PathSamples.Length - 1f) / (available - 1f));
            CharacterPredictiveFootPathSampleDiagnostics sample = modifier.PathSamples[sourceIndex];
            return new RuntimeFootIkPathSampleSnapshot
            {
                Fraction = sample.Fraction,
                Position = sample.Position,
                Normal = sample.Normal,
                SurfaceIdentity = sample.SurfaceInstanceId,
                AnimationRootPosition = sample.AnimationRootPosition,
                HipPosition = sample.HipPosition
            };
        }

        static void PublishActionPlaybacks(
            RuntimeDiagnosticsContext diagnostics,
            IReadOnlyList<
                ActionAnimationPlaybackLifecycleSnapshot>
                playbacks)
        {
            for (int i = 0; i < playbacks.Count; i++)
            {
                ActionAnimationPlaybackLifecycleSnapshot
                    playback = playbacks[i];
                RuntimeTraceEventKind kind =
                    ResolvePlaybackKind(playback.Phase);
                if (!diagnostics.ShouldPublish(
                        RuntimeTraceChannel.Animation,
                        kind))
                {
                    continue;
                }
                ActionCommittedRawSample sample =
                    playback.LatestCommittedRawSample;
                diagnostics.Publish(
                    RuntimeTraceChannel.Animation,
                    RuntimeTraceDomain.Presentation,
                    kind,
                    RuntimeSourceElementHandle.Invalid,
                    ResolveInstance(
                        diagnostics,
                        playback.PlaybackId),
                    new RuntimeTracePayload
                    {
                        AnimationChannelId =
                            playback.AnimationChannelId.Value,
                        Name = playback.ProgramProducerId,
                        Status =
                            $"{playback.Phase}/{playback.FirstSampleReadiness}",
                        OwnerId =
                            playback.PlaybackId.ToString(),
                        RelatedElementId =
                            playback.ActionInstanceId.ToString(),
                        Time = playback.HasCommittedRawSample
                            ? sample.VisualTime
                            : 0f,
                        SecondaryTime =
                            playback.HasCommittedRawSample
                                ? (float)sample
                                    .ContinuousVisualTime
                                : 0f,
                        Cycle =
                            playback.HasCommittedRawSample
                                ? sample.Cycle
                                : 0,
                        Flag =
                            playback.HasCommittedRawSample,
                        Detail =
                            $"Terminal={playback.LogicTerminal} | Usages={playback.SlotUsages.Count} | Release={playback.BackendReleaseRequestIdentity} | PendingSources={playback.PendingBackendSources.Count}"
                    });
            }
        }

        static void PublishMarkerPlaybacks(
            RuntimeDiagnosticsContext diagnostics,
            IReadOnlyList<ActionMarkerPlaybackSnapshot>
                playbacks)
        {
            if (!diagnostics.ShouldPublish(
                    RuntimeTraceChannel.Animation,
                    RuntimeTraceEventKind.AnimationMarkerSync))
            {
                return;
            }
            for (int i = 0; i < playbacks.Count; i++)
            {
                ActionMarkerPlaybackSnapshot playback =
                    playbacks[i];
                diagnostics.Publish(
                    RuntimeTraceChannel.Animation,
                    RuntimeTraceDomain.Presentation,
                    RuntimeTraceEventKind.AnimationMarkerSync,
                    RuntimeSourceElementHandle.Invalid,
                    ResolveInstance(
                        diagnostics,
                        playback.PlaybackId),
                    new RuntimeTracePayload
                    {
                        Name = "ActionPlayback",
                        Status = playback.Rebased
                            ? "Rebased"
                            : playback.Mapped
                                ? "Mapped"
                                : "Independent",
                        OwnerId =
                            playback.PlaybackId.ToString(),
                        Time =
                            playback.EffectiveSample.SampleTime,
                        SecondaryTime =
                            playback.ProjectedRawSample
                                .SampleTime,
                        NormalizedTime =
                            playback.MarkerSegmentFraction,
                        Cycle =
                            playback.EffectiveSample.Cycle,
                        Flag = playback.Mapped,
                        Detail =
                            $"{playback.PreviousMarkerId}->{playback.NextMarkerId} | Projected={playback.ProjectedRawSample.ContinuousTime:R} | Effective={playback.EffectiveSample.ContinuousTime:R}"
                    });
            }
        }

        static void PublishMarkerRelations(
            RuntimeDiagnosticsContext diagnostics,
            IReadOnlyList<ActionMarkerRelationSnapshot>
                relations)
        {
            if (!diagnostics.ShouldPublish(
                    RuntimeTraceChannel.Animation,
                    RuntimeTraceEventKind.AnimationMarkerSync))
            {
                return;
            }
            for (int i = 0; i < relations.Count; i++)
            {
                ActionMarkerRelationSnapshot relation =
                    relations[i];
                diagnostics.Publish(
                    RuntimeTraceChannel.Animation,
                    RuntimeTraceDomain.Presentation,
                    RuntimeTraceEventKind.AnimationMarkerSync,
                    RuntimeSourceElementHandle.Invalid,
                    ResolveInstance(
                        diagnostics,
                        relation.TargetPlaybackId),
                    new RuntimeTracePayload
                    {
                        Name = relation.RelationId.ToString(),
                        Status = "Relation",
                        OwnerId =
                            relation.TargetPlaybackId.ToString(),
                        RelatedElementId =
                            relation.SourcePlaybackId.ToString(),
                        Time = relation.TargetEffectiveSample
                            .SampleTime,
                        SecondaryTime =
                            relation.TargetProjectedRawSample
                                .SampleTime,
                        NormalizedTime =
                            relation.FollowerSegmentFraction,
                        Cycle = relation.TargetEffectiveSample
                            .Cycle,
                        Flag = true,
                        Detail =
                            $"{relation.PreviousMarkerId}->{relation.NextMarkerId} | Mapping={relation.TimeMapping} | Plan={relation.PlanIdentity} | LeaderFraction={relation.LeaderSegmentFraction:R}#{relation.LeaderOccurrenceIndex} | FollowerFraction={relation.FollowerSegmentFraction:R}#{relation.FollowerOccurrenceIndex} | Source={relation.SourceEffectiveSample.ContinuousTime:R} | Target={relation.TargetEffectiveSample.ContinuousTime:R}"
                    });
            }
        }

        static void PublishPoseStateSourceRelations(
            RuntimeDiagnosticsContext diagnostics,
            IReadOnlyList<PoseStateSourceSyncSnapshot> relations)
        {
            if (!diagnostics.ShouldPublish(
                    RuntimeTraceChannel.Animation,
                    RuntimeTraceEventKind.AnimationMarkerSync))
                return;
            for (int i = 0; i < relations.Count; i++)
            {
                PoseStateSourceSyncSnapshot relation = relations[i];
                diagnostics.Publish(
                    RuntimeTraceChannel.Animation,
                    RuntimeTraceDomain.Presentation,
                    RuntimeTraceEventKind.AnimationMarkerSync,
                    RuntimeSourceElementHandle.Invalid,
                    RuntimeInstanceKey.Character(diagnostics.CharacterRuntimeId),
                    new RuntimeTracePayload
                    {
                        Name = relation.RelationId,
                        Status = relation.Initialized ? "Relation" : "Pending",
                        Time = relation.FollowerEffectiveTime <= float.MaxValue
                            ? (float)relation.FollowerEffectiveTime
                            : float.MaxValue,
                        SecondaryTime = relation.LeaderFraction,
                        NormalizedTime = relation.FollowerFraction,
                        Cycle = 0,
                        Flag = relation.Initialized,
                        Detail =
                            $"Mapping={relation.TimeMapping} | Plan={relation.PlanIdentity} | LeaderFraction={relation.LeaderFraction:R}#{relation.LeaderOccurrenceIndex}@{relation.LeaderOrdinal} | FollowerFraction={relation.FollowerFraction:R}#{relation.FollowerOccurrenceIndex}@{relation.FollowerOrdinal} | Effective={relation.FollowerEffectiveTime:R}"
                    });
            }
        }

        static void PublishRetirements(
            RuntimeDiagnosticsContext diagnostics,
            IReadOnlyList<AnimationPlaybackId>
                retiredPlaybacks)
        {
            if (retiredPlaybacks == null ||
                !diagnostics.ShouldPublish(
                    RuntimeTraceChannel.Animation,
                    RuntimeTraceEventKind
                        .AnimationPlaybackRetired))
            {
                return;
            }
            for (int i = 0;
                 i < retiredPlaybacks.Count;
                 i++)
            {
                AnimationPlaybackId playbackId =
                    retiredPlaybacks[i];
                diagnostics.Publish(
                    RuntimeTraceChannel.Animation,
                    RuntimeTraceDomain.Presentation,
                    RuntimeTraceEventKind
                        .AnimationPlaybackRetired,
                    RuntimeSourceElementHandle.Invalid,
                    ResolveInstance(
                        diagnostics,
                        playbackId),
                    new RuntimeTracePayload
                    {
                        Status =
                            ActionAnimationPlaybackLifecyclePhase
                                .Retired.ToString(),
                        OwnerId = playbackId.ToString()
                    });
            }
        }

        static RuntimeTraceEventKind ResolvePlaybackKind(
            ActionAnimationPlaybackLifecyclePhase phase)
        {
            return phase switch
            {
                ActionAnimationPlaybackLifecyclePhase
                    .PendingFirstSample =>
                    RuntimeTraceEventKind
                        .AnimationPlaybackPending,
                ActionAnimationPlaybackLifecyclePhase
                    .Selected =>
                    RuntimeTraceEventKind
                        .AnimationPlaybackSelected,
                ActionAnimationPlaybackLifecyclePhase
                    .Retained or
                ActionAnimationPlaybackLifecyclePhase
                    .RetirementPermitted =>
                    RuntimeTraceEventKind
                        .AnimationPlaybackRetained,
                _ =>
                    RuntimeTraceEventKind
                        .AnimationPlaybackRetired
            };
        }

        static RuntimeInstanceKey ResolveInstance(
            RuntimeDiagnosticsContext diagnostics,
            AnimationPlaybackId playbackId)
        {
            return playbackId.IsValid
                ? RuntimeInstanceKey.Timeline(
                    diagnostics.CharacterRuntimeId,
                    playbackId.Generation)
                : RuntimeInstanceKey.Character(
                    diagnostics.CharacterRuntimeId);
        }
    }
}
