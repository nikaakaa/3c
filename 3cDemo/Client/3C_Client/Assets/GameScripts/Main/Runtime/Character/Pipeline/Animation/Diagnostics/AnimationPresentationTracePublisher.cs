using System.Collections.Generic;
using BTSMTL.Diagnostics;
using ThirdPersonCharacter.Pipeline.Presentation;

namespace ThirdPersonCharacter.Pipeline.Animation.Diagnostics
{
    internal static class AnimationPresentationTracePublisher
    {
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
            CharacterFootGroundingDiagnostics grounding = snapshot.Grounding;
            CharacterPredictiveFootPlacementModifierDiagnostics modifier = snapshot.Modifier;
            bool hasModifier = modifier.IsCompleted &&
                               modifier.FrameSequence == grounding.FrameSequence &&
                               modifier.CompletionIdentity == grounding.CompletionIdentity;
            CharacterFullBodyIkSolverDiagnostics solver = snapshot.Solver;
            RuntimeFootIkTraceSnapshot footIk = new RuntimeFootIkTraceSnapshot
            {
                IsAvailable = true,
                FrameSequence = grounding.FrameSequence,
                ResetSequence = grounding.ResetSequence,
                GroundingCompletionIdentity = grounding.CompletionIdentity,
                ModifierCompletionIdentity = hasModifier ? modifier.CompletionIdentity : 0,
                SolverCompletionIdentity = solver.OutputCompletionIdentity,
                HasPredictiveModifier = hasModifier,
                SolverBackendIdentity = solver.BackendIdentity,
                SolverFailure = solver.IsCompleted
                    ? solver.Failure.ToString()
                    : "NotCompleted",
                NodeExecuted = grounding.NodeExecuted,
                BodyGrounded = grounding.BodyGrounded,
                PlacementAlpha = grounding.PlacementAlpha,
                PresentationDeltaSeconds = grounding.PresentationDeltaSeconds,
                PoseRootVerticalDelta = grounding.PoseRootVerticalDelta,
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
                ModifierSelectedSide = hasModifier ? modifier.SelectedSide.ToString() : "ModifierNotCompiled",
                BaselineProducerOperationIndex = hasModifier ? modifier.BaselineProducerOperationIndex : -1,
                BaselineProducerCallSiteIndex = hasModifier ? modifier.BaselineProducerCallSiteIndex : -1,
                BaselineGoalOffset = hasModifier ? modifier.BaselineGoalOffset : -1,
                BaselineGoalCount = hasModifier ? modifier.BaselineGoalCount : 0,
                BaselineRigId = hasModifier ? modifier.BaselineRigId.ToString() : string.Empty,
                BaselineRigRevision = hasModifier ? modifier.BaselineRigRevision.ToString() : string.Empty,
                Left = BuildFootTrace(
                    grounding.Left,
                    hasModifier ? modifier.Left : default,
                    hasModifier,
                    snapshot.LeftFoot),
                Right = BuildFootTrace(
                    grounding.Right,
                    hasModifier ? modifier.Right : default,
                    hasModifier,
                    snapshot.RightFoot)
            };
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
                        ? "FootGrounding -> PredictiveFootPlacementModifier -> FullBodyIK"
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
                    Detail = $"L contact {grounding.Left.ContactState} placement {grounding.Left.PlacementWeight:0.###} anchor {grounding.Left.AnchorBlendWeight:0.###} soleTarget {grounding.Left.SoleClearanceTarget:0.###} constraint {grounding.Left.SoleConstraintOffset:0.###}/{grounding.Left.ContinuousSoleContact} soleResidual {grounding.Left.ResidualSolePenetration:0.###} final {footIk.Left.FinalGoalPositionWeight:0.###} residual {snapshot.LeftFoot.PositionResidual:0.###} | " +
                             $"R contact {grounding.Right.ContactState} placement {grounding.Right.PlacementWeight:0.###} anchor {grounding.Right.AnchorBlendWeight:0.###} soleTarget {grounding.Right.SoleClearanceTarget:0.###} constraint {grounding.Right.SoleConstraintOffset:0.###}/{grounding.Right.ContinuousSoleContact} soleResidual {grounding.Right.ResidualSolePenetration:0.###} final {footIk.Right.FinalGoalPositionWeight:0.###} residual {snapshot.RightFoot.PositionResidual:0.###}",
                    FootIk = footIk
                });
        }

        static RuntimeFootIkLegTraceSnapshot BuildFootTrace(
            CharacterFootGroundingFootDiagnostics grounding,
            CharacterPredictiveFootPlacementModifierFootDiagnostics modifier,
            bool hasModifier,
            CharacterFullBodyIkEffectorDiagnostics solved)
        {
            CharacterFullBodyIkGoal finalGoal = hasModifier
                ? modifier.FinalGoal
                : grounding.Goal;
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
                HasSurfaceAnchor = grounding.HasSurfaceAnchor,
                SurfaceLocalAnchor = grounding.SurfaceLocalAnchor,
                SurfaceLocalRotation = grounding.SurfaceLocalRotation,
                AnchorWorldPosition = grounding.AnchorWorldPosition,
                AnchorWorldRotation = grounding.AnchorWorldRotation,
                SwingEligible = hasModifier && modifier.SwingEligible,
                SelectedForPredictiveRewrite = hasModifier && modifier.SelectedForRewrite,
                PredictiveRewritten = hasModifier && modifier.Rewritten,
                PredictionRejectReason = hasModifier
                    ? modifier.RejectReason.ToString()
                    : "ModifierNotCompiled",
                FutureSurfaceIdentity = hasModifier ? modifier.FutureSupport.SurfaceIdentity : 0,
                FutureSupportPoint = hasModifier ? modifier.FutureSupport.Point : default,
                FutureSupportNormal = hasModifier ? modifier.FutureSupport.Normal : default,
                GroundEnvelopeSegmentCount = hasModifier ? modifier.GroundEnvelopeSegmentCount : 0,
                GroundEnvelopeRejectReason = hasModifier
                    ? modifier.GroundEnvelopeRejectReason.ToString()
                    : "ModifierNotCompiled",
                PredictiveQueryCount = hasModifier ? modifier.QueryCount : 0,
                PredictiveRejectedQueryCount = hasModifier ? modifier.RejectedQueryCount : 0,
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
                AnimatedAnkleComponentY = grounding.AnimatedAnkleComponentY,
                HasPreviousSoleSample = grounding.HasPreviousSoleSample,
                PreviousSoleSurfaceIdentity = grounding.PreviousSoleSurfaceIdentity,
                PreviousSoleHeelPlaneDistance = grounding.PreviousSoleHeelPlaneDistance,
                PreviousSoleToePlaneDistance = grounding.PreviousSoleToePlaneDistance,
                ContinuousSoleContact = grounding.ContinuousSoleContact,
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
                SwingClearance = hasModifier ? modifier.SwingClearance : 0f,
                CurrentGroundingComponentPosition = grounding.CurrentGroundingComponentPosition,
                BaselineGoalComponentPosition = grounding.Goal.ComponentPosition,
                FinalGoalComponentPosition = finalGoal.ComponentPosition,
                SolvedComponentPosition = solved.SolvedComponentPosition,
                PositionResidual = solved.PositionResidual,
                RotationResidualDegrees = solved.RotationResidualDegrees
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
                            relation.MarkerSegmentFraction,
                        Cycle = relation.TargetEffectiveSample
                            .Cycle,
                        Flag = true,
                        Detail =
                            $"{relation.PreviousMarkerId}->{relation.NextMarkerId} | Source={relation.SourceEffectiveSample.ContinuousTime:R} | Target={relation.TargetEffectiveSample.ContinuousTime:R}"
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
