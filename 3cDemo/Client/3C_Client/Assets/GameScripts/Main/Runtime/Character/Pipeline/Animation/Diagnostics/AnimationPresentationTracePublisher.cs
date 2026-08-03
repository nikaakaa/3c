using System.Collections.Generic;
using BTSMTL.Diagnostics;

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
                    RuntimeTraceEventKind.AnimationPlaybackPending))
            {
                return AnimationPresentationDiagnosticsInterest.Capture;
            }
            return diagnostics.ShouldPublish(
                    RuntimeTraceChannel.Animation,
                    RuntimeTraceEventKind.AnimationPlaybackPending)
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
