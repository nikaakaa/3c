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
            CharacterPredictiveFootPlacementDiagnostics predictive =
                snapshot.PredictiveFootPlacement;
            CharacterFullBodyIkSolverDiagnostics solver = snapshot.Solver;
            RuntimeFootIkTraceSnapshot footIk = new RuntimeFootIkTraceSnapshot
            {
                IsAvailable = true,
                FrameSequence = predictive.FrameSequence,
                GoalCompletionIdentity = predictive.CompletionIdentity,
                SolverCompletionIdentity = solver.OutputCompletionIdentity,
                GroundingBackendIdentity = predictive.BackendIdentity,
                SolverBackendIdentity = solver.BackendIdentity,
                SolverFailure = solver.IsCompleted
                    ? solver.Failure.ToString()
                    : "NotCompleted",
                BodyGrounded = predictive.Grounded,
                TargetGrounded = predictive.TargetGrounded,
                GroundedBefore = predictive.GroundedBefore,
                GroundedAfter = predictive.GroundedAfter,
                RootHit = predictive.RootHit.HasHit,
                RootSurfaceIdentity = predictive.RootHit.SurfaceIdentity,
                PelvisTargetOffset = predictive.PelvisPlan.TargetOffset,
                PelvisResolvedOffset = predictive.PelvisPlan.ResolvedOffset,
                RejectLeftGoal = predictive.PelvisPlan.RejectLeftGoal,
                RejectRightGoal = predictive.PelvisPlan.RejectRightGoal,
                PelvisHeightMode = predictive.PelvisPlan.HeightMode.ToString(),
                MovementCompensationMode = predictive.PelvisPlan.MovementCompensationMode.ToString(),
                Left = BuildFootTrace(predictive.Left, snapshot.LeftFoot),
                Right = BuildFootTrace(predictive.Right, snapshot.RightFoot)
            };
            string status = !solver.IsCompleted
                ? "GoalSourceCompleted"
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
                    Name = "FinalIK Grounding -> Predictive Extension -> FullBodyIK",
                    Status = status,
                    OwnerId = predictive.CompletionIdentity.ToString(),
                    RelatedElementId = predictive.FrameSequence.ToString(),
                    Time = predictive.Left.FootFeature.PlantConfidence,
                    SecondaryTime = predictive.PelvisPlan.ResolvedOffset,
                    NormalizedTime = predictive.Right.FootFeature.PlantConfidence,
                    Weight = predictive.Left.Goal.PositionWeight,
                    FinalWeight = predictive.Right.Goal.PositionWeight,
                    Flag = solver.Succeeded,
                    Cause = $"PelvisReject(L={predictive.PelvisPlan.RejectLeftGoal},R={predictive.PelvisPlan.RejectRightGoal})",
                    Detail = $"L confidence {predictive.Left.FootFeature.PlantConfidence:0.###} contactIntent {predictive.Left.PlantContact} placement {predictive.Left.PlacementWeight:0.###} support {predictive.Left.PlantSupportWeight:0.###} contact {predictive.Left.ContactWeight:0.###} residual {snapshot.LeftFoot.PositionResidual:0.###} | " +
                             $"R confidence {predictive.Right.FootFeature.PlantConfidence:0.###} contactIntent {predictive.Right.PlantContact} placement {predictive.Right.PlacementWeight:0.###} support {predictive.Right.PlantSupportWeight:0.###} contact {predictive.Right.ContactWeight:0.###} residual {snapshot.RightFoot.PositionResidual:0.###}",
                    FootIk = footIk
                });
        }

        static RuntimeFootIkLegTraceSnapshot BuildFootTrace(
            CharacterPredictiveFootDiagnostics predictive,
            CharacterFullBodyIkEffectorDiagnostics solved)
        {
            return new RuntimeFootIkLegTraceSnapshot
            {
                IsAvailable = true,
                Grounded = predictive.Grounded,
                CurrentGroundingHit = predictive.CurrentGroundingHit.HasHit,
                SurfaceIdentity = predictive.CurrentGroundingHit.SurfaceIdentity,
                ConstraintState = predictive.ConstraintState.ToString(),
                TransitionReason = predictive.TransitionReason.ToString(),
                LockType = predictive.LockType.ToString(),
                PredictionRejectReason = predictive.PredictionRejectReason.ToString(),
                GoalApplication = predictive.Goal.Application.ToString(),
                GoalSourceKind = predictive.Goal.SourceKind.ToString(),
                SolverResultAvailable = solved.IsAvailable,
                PlantConfidence = predictive.FootFeature.PlantConfidence,
                PlantContact = predictive.PlantContact,
                SoleHeight = predictive.FootFeature.SoleHeight,
                PlacementWeight = predictive.PlacementWeight,
                AnimationFootSpeed = predictive.AnimationFootSpeed,
                SurfaceDistance = predictive.SurfaceDistance,
                PlantSupportWeight = predictive.PlantSupportWeight,
                ContactWeight = predictive.ContactWeight,
                GoalPositionWeight = predictive.Goal.PositionWeight,
                GoalRotationWeight = predictive.Goal.RotationWeight,
                LegExtensionRatio = predictive.LegExtensionRatio,
                AnkleTwistDegrees = predictive.AnkleTwistDegrees,
                QueryCount = predictive.QueryCount,
                RejectedQueryCount = predictive.RejectedQueryCount,
                GroundingComponentPosition = predictive.GroundingComponentPosition,
                GoalComponentPosition = predictive.Goal.ComponentPosition,
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
