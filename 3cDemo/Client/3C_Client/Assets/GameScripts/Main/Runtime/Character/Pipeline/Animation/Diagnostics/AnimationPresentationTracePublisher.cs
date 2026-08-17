using System.Collections.Generic;
using BTSMTL.Diagnostics;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation.Diagnostics
{
    internal static class AnimationPresentationTracePublisher
    {
        static bool s_LoggedCompletedFootPlacementAvailable;
        static bool s_LoggedCompletedFootPlacementUnavailable;

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
            IReadOnlyList<AnimationPlaybackId> retiredPlaybacks)
        {
            if (diagnostics == null || debugView == null)
                return;
            PublishActionPlaybacks(diagnostics, debugView.ActionPlaybacks);
            PublishMarkerPlaybacks(diagnostics, debugView.ActionMarkerPlaybacks);
            PublishMarkerRelations(diagnostics, debugView.ActionMarkerRelations);
            PublishPoseStateSourceRelations(
                diagnostics,
                debugView.PoseStateSourceSyncRelations);
            PublishRetirements(diagnostics, retiredPlaybacks);
            PublishFootPlacement(diagnostics, debugView.PosePlan.FootPlacement);
        }

        static void PublishFootPlacement(
            RuntimeDiagnosticsContext diagnostics,
            AnimationFootPlacementRuntimeSnapshot snapshot)
        {
            if (!snapshot.IsAvailable ||
                !diagnostics.ShouldPublish(
                    RuntimeTraceChannel.FootPlacement,
                    RuntimeTraceEventKind.FootPlacementSnapshot))
            {
                return;
            }
            RuntimeFootPlacementTraceSnapshot footPlacement =
                BuildFootPlacementSnapshot(snapshot);
            ref readonly CharacterFootLandingPredictionDiagnostics landing =
                ref snapshot.LandingPrediction;
            diagnostics.Publish(
                RuntimeTraceChannel.FootPlacement,
                RuntimeTraceDomain.Presentation,
                RuntimeTraceEventKind.FootPlacementSnapshot,
                RuntimeSourceElementHandle.Invalid,
                RuntimeInstanceKey.Character(diagnostics.CharacterRuntimeId),
                new RuntimeTracePayload
                {
                    Name = "Landing Prediction",
                    Status =
                        $"L:{landing.Left.State}/{landing.Left.RejectReason} " +
                        $"R:{landing.Right.State}/{landing.Right.RejectReason}",
                    OwnerId = landing.CompletionIdentity.ToString(),
                    RelatedElementId = landing.FrameSequence.ToString(),
                    Time = landing.Left.TimeToLandingSeconds,
                    SecondaryTime = landing.Right.TimeToLandingSeconds,
                    Weight = 0f,
                    FinalWeight = 0f,
                    Flag = landing.Left.Accepted || landing.Right.Accepted,
                    FootPlacement = footPlacement
                });
        }

        internal static void PublishCompletedFootPlacement(
            ActorId actorId,
            AnimationFootPlacementRuntimeSnapshot snapshot)
        {
            if (!snapshot.IsAvailable)
            {
                if (!s_LoggedCompletedFootPlacementUnavailable)
                {
                    s_LoggedCompletedFootPlacementUnavailable = true;
                    Debug.Log("GameplayLab Foot Landing Prediction completed snapshot unavailable.");
                }
                return;
            }
            if (!s_LoggedCompletedFootPlacementAvailable)
            {
                s_LoggedCompletedFootPlacementAvailable = true;
                Debug.Log(
                    $"GameplayLab Foot Landing Prediction completed snapshot available for {actorId}.");
            }
            _ = BuildFootPlacementSnapshot(snapshot);
        }

        static RuntimeFootPlacementTraceSnapshot BuildFootPlacementSnapshot(
            AnimationFootPlacementRuntimeSnapshot snapshot)
        {
            ref readonly CharacterFootLandingPredictionDiagnostics landing =
                ref snapshot.LandingPrediction;
            return new RuntimeFootPlacementTraceSnapshot
            {
                IsAvailable = true,
                FrameSequence = landing.FrameSequence,
                CompletionIdentity = landing.CompletionIdentity,
                Left = BuildFootTrace(landing.Left),
                Right = BuildFootTrace(landing.Right)
            };
        }

        static RuntimeFootPlacementFootTraceSnapshot BuildFootTrace(
            CharacterFootLandingPredictionFootDiagnostics landing) =>
            new RuntimeFootPlacementFootTraceSnapshot
            {
                IsAvailable = true,
                Side = landing.Side.ToString(),
                State = landing.State.ToString(),
                RejectReason = landing.RejectReason.ToString(),
                StepSource = landing.StepSource.ToString(),
                LandingEventIdentity = landing.LandingEventIdentity,
                TrajectoryGeneration = landing.TrajectoryGeneration,
                LandingConfidence = landing.LandingConfidence,
                TimeToLandingSeconds = landing.TimeToLandingSeconds,
                RootLocalLanding = landing.RootLocalLanding,
                CurrentAnimatedSole = landing.CurrentAnimatedSole,
                RawLanding = landing.RawLandingCandidate,
                QueryAvailable = landing.Query.MaximumDistance > 0f,
                QueryShape = landing.Query.MaximumDistance > 0f
                    ? landing.Query.Shape.ToString()
                    : string.Empty,
                QueryPurpose = landing.Query.MaximumDistance > 0f
                    ? landing.Query.Purpose.ToString()
                    : string.Empty,
                QueryOrigin = landing.Query.Origin,
                QueryDirection = landing.Query.Direction,
                QueryRadius = landing.Query.Radius,
                QueryMaximumDistance = landing.Query.MaximumDistance,
                QueryLayerMask = landing.Query.LayerMask,
                QueryMinimumGroundNormalDot =
                    landing.Query.MinimumGroundNormalDot,
                Accepted = landing.Accepted,
                SurfaceIdentity = landing.SurfaceIdentity,
                LandingPoint = landing.LandingPoint,
                LandingNormal = landing.LandingNormal,
                QueryDistance = landing.QueryDistance
            };

        static void PublishActionPlaybacks(
            RuntimeDiagnosticsContext diagnostics,
            IReadOnlyList<ActionAnimationPlaybackLifecycleSnapshot> playbacks)
        {
            for (int i = 0; i < playbacks.Count; i++)
            {
                ActionAnimationPlaybackLifecycleSnapshot playback = playbacks[i];
                RuntimeTraceEventKind kind = ResolvePlaybackKind(playback.Phase);
                if (!diagnostics.ShouldPublish(RuntimeTraceChannel.Animation, kind))
                    continue;
                ActionCommittedRawSample sample = playback.LatestCommittedRawSample;
                diagnostics.Publish(
                    RuntimeTraceChannel.Animation,
                    RuntimeTraceDomain.Presentation,
                    kind,
                    RuntimeSourceElementHandle.Invalid,
                    ResolveInstance(diagnostics, playback.PlaybackId),
                    new RuntimeTracePayload
                    {
                        AnimationChannelId = playback.AnimationChannelId.Value,
                        Name = playback.ProgramProducerId,
                        Status = $"{playback.Phase}/{playback.FirstSampleReadiness}",
                        OwnerId = playback.PlaybackId.ToString(),
                        RelatedElementId = playback.ActionInstanceId.ToString(),
                        Time = playback.HasCommittedRawSample ? sample.VisualTime : 0f,
                        SecondaryTime = playback.HasCommittedRawSample
                            ? (float)sample.ContinuousVisualTime
                            : 0f,
                        Cycle = playback.HasCommittedRawSample ? sample.Cycle : 0,
                        Flag = playback.HasCommittedRawSample,
                        Detail =
                            $"Terminal={playback.LogicTerminal} | Usages={playback.SlotUsages.Count} | Release={playback.BackendReleaseRequestIdentity} | PendingSources={playback.PendingBackendSources.Count}"
                    });
            }
        }

        static void PublishMarkerPlaybacks(
            RuntimeDiagnosticsContext diagnostics,
            IReadOnlyList<ActionMarkerPlaybackSnapshot> playbacks)
        {
            if (!diagnostics.ShouldPublish(
                    RuntimeTraceChannel.Animation,
                    RuntimeTraceEventKind.AnimationMarkerSync))
            {
                return;
            }
            for (int i = 0; i < playbacks.Count; i++)
            {
                ActionMarkerPlaybackSnapshot playback = playbacks[i];
                diagnostics.Publish(
                    RuntimeTraceChannel.Animation,
                    RuntimeTraceDomain.Presentation,
                    RuntimeTraceEventKind.AnimationMarkerSync,
                    RuntimeSourceElementHandle.Invalid,
                    ResolveInstance(diagnostics, playback.PlaybackId),
                    new RuntimeTracePayload
                    {
                        Name = "ActionPlayback",
                        Status = playback.Rebased
                            ? "Rebased"
                            : playback.Mapped ? "Mapped" : "Independent",
                        OwnerId = playback.PlaybackId.ToString(),
                        Time = playback.EffectiveSample.SampleTime,
                        SecondaryTime = playback.ProjectedRawSample.SampleTime,
                        NormalizedTime = playback.MarkerSegmentFraction,
                        Cycle = playback.EffectiveSample.Cycle,
                        Flag = playback.Mapped,
                        Detail =
                            $"{playback.PreviousMarkerId}->{playback.NextMarkerId} | Projected={playback.ProjectedRawSample.ContinuousTime:R} | Effective={playback.EffectiveSample.ContinuousTime:R}"
                    });
            }
        }

        static void PublishMarkerRelations(
            RuntimeDiagnosticsContext diagnostics,
            IReadOnlyList<ActionMarkerRelationSnapshot> relations)
        {
            if (!diagnostics.ShouldPublish(
                    RuntimeTraceChannel.Animation,
                    RuntimeTraceEventKind.AnimationMarkerSync))
            {
                return;
            }
            for (int i = 0; i < relations.Count; i++)
            {
                ActionMarkerRelationSnapshot relation = relations[i];
                diagnostics.Publish(
                    RuntimeTraceChannel.Animation,
                    RuntimeTraceDomain.Presentation,
                    RuntimeTraceEventKind.AnimationMarkerSync,
                    RuntimeSourceElementHandle.Invalid,
                    ResolveInstance(diagnostics, relation.TargetPlaybackId),
                    new RuntimeTracePayload
                    {
                        Name = relation.RelationId.ToString(),
                        Status = "Relation",
                        OwnerId = relation.TargetPlaybackId.ToString(),
                        RelatedElementId = relation.SourcePlaybackId.ToString(),
                        Time = relation.TargetEffectiveSample.SampleTime,
                        SecondaryTime = relation.TargetProjectedRawSample.SampleTime,
                        NormalizedTime = relation.FollowerSegmentFraction,
                        Cycle = relation.TargetEffectiveSample.Cycle,
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
            {
                return;
            }
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
            IReadOnlyList<AnimationPlaybackId> retiredPlaybacks)
        {
            if (retiredPlaybacks == null ||
                !diagnostics.ShouldPublish(
                    RuntimeTraceChannel.Animation,
                    RuntimeTraceEventKind.AnimationPlaybackRetired))
            {
                return;
            }
            for (int i = 0; i < retiredPlaybacks.Count; i++)
            {
                AnimationPlaybackId playbackId = retiredPlaybacks[i];
                diagnostics.Publish(
                    RuntimeTraceChannel.Animation,
                    RuntimeTraceDomain.Presentation,
                    RuntimeTraceEventKind.AnimationPlaybackRetired,
                    RuntimeSourceElementHandle.Invalid,
                    ResolveInstance(diagnostics, playbackId),
                    new RuntimeTracePayload
                    {
                        Status = ActionAnimationPlaybackLifecyclePhase.Retired.ToString(),
                        OwnerId = playbackId.ToString()
                    });
            }
        }

        static RuntimeTraceEventKind ResolvePlaybackKind(
            ActionAnimationPlaybackLifecyclePhase phase) =>
            phase switch
            {
                ActionAnimationPlaybackLifecyclePhase.PendingFirstSample =>
                    RuntimeTraceEventKind.AnimationPlaybackPending,
                ActionAnimationPlaybackLifecyclePhase.Selected =>
                    RuntimeTraceEventKind.AnimationPlaybackSelected,
                ActionAnimationPlaybackLifecyclePhase.Retained or
                ActionAnimationPlaybackLifecyclePhase.RetirementPermitted =>
                    RuntimeTraceEventKind.AnimationPlaybackRetained,
                _ => RuntimeTraceEventKind.AnimationPlaybackRetired
            };

        static RuntimeInstanceKey ResolveInstance(
            RuntimeDiagnosticsContext diagnostics,
            AnimationPlaybackId playbackId) =>
            playbackId.IsValid
                ? RuntimeInstanceKey.Timeline(
                    diagnostics.CharacterRuntimeId,
                    playbackId.Generation)
                : RuntimeInstanceKey.Character(diagnostics.CharacterRuntimeId);
    }
}
