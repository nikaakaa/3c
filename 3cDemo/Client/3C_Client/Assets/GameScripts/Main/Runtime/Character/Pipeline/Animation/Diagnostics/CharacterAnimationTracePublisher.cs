using System;
using System.Collections.Generic;
using BTSMTL.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation.Diagnostics
{
    public sealed class CharacterAnimationTracePublisher
    {
        readonly Func<RuntimeDiagnosticsContext> m_DiagnosticsSource;

        public CharacterAnimationTracePublisher(Func<RuntimeDiagnosticsContext> diagnosticsSource = null)
        {
            m_DiagnosticsSource = diagnosticsSource;
        }

        public void PublishPlaybackLifecycle(
            RuntimeDiagnosticsContext diagnostics,
            IReadOnlyList<AnimationPlaybackCommand> commands,
            IReadOnlyList<AnimationPlaybackLifecycleSnapshot> snapshots,
            IReadOnlyList<AnimationMarkerSyncRelationSnapshot> markerSyncSnapshots,
            IReadOnlyList<AnimationPlaybackId> retiredPlaybacks)
        {
            diagnostics ??= m_DiagnosticsSource?.Invoke();
            if (diagnostics == null)
                return;

            if (commands != null)
            {
                for (int i = 0; i < commands.Count; i++)
                    PublishCommand(diagnostics, commands[i]);
            }
            if (snapshots != null)
            {
                for (int i = 0; i < snapshots.Count; i++)
                    PublishSnapshot(diagnostics, snapshots[i]);
            }
            if (markerSyncSnapshots != null)
            {
                for (int i = 0; i < markerSyncSnapshots.Count; i++)
                    PublishMarkerSync(diagnostics, markerSyncSnapshots[i]);
            }
            if (retiredPlaybacks != null)
            {
                for (int i = 0; i < retiredPlaybacks.Count; i++)
                    PublishRetired(diagnostics, retiredPlaybacks[i]);
            }
        }

        public void PublishPresentationInterpolation(
            RuntimeDiagnosticsContext diagnostics,
            bool rootValid,
            bool visualValid,
            float alpha,
            float presentationDeltaSeconds,
            ulong previousLogicTick,
            ulong currentLogicTick,
            Vector3 visualPosition)
        {
            diagnostics ??= m_DiagnosticsSource?.Invoke();
            if (diagnostics == null || !diagnostics.ShouldPublish(RuntimeTraceChannel.Animation, RuntimeTraceEventKind.PresentationInterpolated))
                return;

            diagnostics.Publish(
                RuntimeTraceChannel.Animation,
                RuntimeTraceDomain.Presentation,
                RuntimeTraceEventKind.PresentationInterpolated,
                RuntimeSourceElementHandle.Invalid,
                RuntimeInstanceKey.Character(diagnostics.CharacterRuntimeId),
                new RuntimeTracePayload
                {
                    Status = rootValid && visualValid ? "Valid" : "Invalid",
                    Time = alpha,
                    SecondaryTime = presentationDeltaSeconds,
                    Detail = $"{previousLogicTick}->{currentLogicTick}",
                    Value = DebugValueSnapshot.Capture(visualPosition)
                });
        }

        public void PublishMarkerSyncFailure(
            RuntimeDiagnosticsContext diagnostics,
            AnimationMarkerSyncException failure,
            AnimationMarkerSyncRawSample sample)
        {
            if (failure == null)
                throw new ArgumentNullException(nameof(failure));
            diagnostics ??= m_DiagnosticsSource?.Invoke();
            if (diagnostics == null || !diagnostics.ShouldPublish(RuntimeTraceChannel.Animation, RuntimeTraceEventKind.AnimationMarkerSync))
                return;

            diagnostics.Publish(
                RuntimeTraceChannel.Animation,
                RuntimeTraceDomain.Presentation,
                RuntimeTraceEventKind.AnimationMarkerSync,
                RuntimeSourceElementHandle.Invalid,
                ResolveInstance(diagnostics, failure.PlaybackId),
                new RuntimeTracePayload
                {
                    AnimationChannelId = sample.AnimationChannelId.Value,
                    Name = sample.Binding?.CanonicalGroupId ?? string.Empty,
                    Status = failure.Reason.ToString(),
                    OwnerId = failure.PlaybackId.ToString(),
                    Time = (float)sample.ContinuousTime,
                    Cycle = sample.Cycle,
                    Detail = $"Invalid | Reason={failure.Reason} | Playback={failure.PlaybackId}"
                });
        }

        static void PublishCommand(RuntimeDiagnosticsContext diagnostics, AnimationPlaybackCommand command)
        {
            switch (command.Kind)
            {
                case AnimationPlaybackCommandKind.Selection:
                {
                    if (!diagnostics.ShouldPublish(RuntimeTraceChannel.Animation, RuntimeTraceEventKind.AnimationSelectionSubmitted))
                        break;
                    AnimationChannelSelection selection = command.Selection;
                    diagnostics.Publish(
                        RuntimeTraceChannel.Animation,
                        RuntimeTraceDomain.Lifecycle,
                        RuntimeTraceEventKind.AnimationSelectionSubmitted,
                        RuntimeSourceElementHandle.Invalid,
                        ResolveInstance(diagnostics, selection.PlaybackId),
                        new RuntimeTracePayload
                        {
                            AnimationChannelId = selection.AnimationChannelId.Value,
                            Status = selection.HasPlayback ? "Selected" : "None",
                            OwnerId = selection.PlaybackId.ToString(),
                            Time = selection.LocalLogicTick,
                            SecondaryTime = selection.Sequence,
                            Flag = selection.HasPlayback
                        });
                    break;
                }
                case AnimationPlaybackCommandKind.PoseRequest:
                {
                    if (!diagnostics.ShouldPublish(RuntimeTraceChannel.Animation, RuntimeTraceEventKind.AnimationProducerSampled))
                        break;
                    ResolvedAnimationPoseRequest poseRequest = command.PoseRequest;
                    diagnostics.Publish(
                        RuntimeTraceChannel.Animation,
                        RuntimeTraceDomain.Presentation,
                        RuntimeTraceEventKind.AnimationProducerSampled,
                        RuntimeSourceElementHandle.Invalid,
                        ResolveInstance(diagnostics, poseRequest.SourceId.PlaybackId),
                        new RuntimeTracePayload
                        {
                            AnimationChannelId = poseRequest.AnimationChannelId.Value,
                            Name = poseRequest.SourceId.SourceKind.ToString(),
                            Status = "Output",
                            OwnerId = poseRequest.SourceId.ToString(),
                            Time = poseRequest.VisualSampleTime,
                            SecondaryTime = (float)poseRequest.ContinuousVisualTime,
                            Cycle = poseRequest.Cycle,
                            Detail = $"Slot={poseRequest.PoseSlotId} | Clips={poseRequest.Clips.Count} | Parameters={poseRequest.PoseParameters.Count}"
                        });
                    break;
                }
                case AnimationPlaybackCommandKind.Complete:
                case AnimationPlaybackCommandKind.Release:
                    RuntimeTraceEventKind lifecycleKind = command.Kind == AnimationPlaybackCommandKind.Complete
                        ? RuntimeTraceEventKind.AnimationPlaybackCompleted
                        : RuntimeTraceEventKind.AnimationPlaybackReleased;
                    if (!diagnostics.ShouldPublish(RuntimeTraceChannel.Animation, lifecycleKind))
                        break;
                    diagnostics.Publish(
                        RuntimeTraceChannel.Animation,
                        RuntimeTraceDomain.Lifecycle,
                        lifecycleKind,
                        RuntimeSourceElementHandle.Invalid,
                        ResolveInstance(diagnostics, command.PlaybackId),
                        new RuntimeTracePayload
                        {
                            Status = command.Kind.ToString(),
                            OwnerId = command.PlaybackId.ToString(),
                            Time = command.LocalLogicTick
                        });
                    break;
            }
        }

        static void PublishSnapshot(
            RuntimeDiagnosticsContext diagnostics,
            AnimationPlaybackLifecycleSnapshot snapshot)
        {
            RuntimeTraceEventKind kind;
            switch (snapshot.Phase)
            {
                case AnimationPlaybackLifecyclePhase.PendingFirstSample:
                    kind = RuntimeTraceEventKind.AnimationPlaybackPending;
                    break;
                case AnimationPlaybackLifecyclePhase.Selected:
                    kind = RuntimeTraceEventKind.AnimationPlaybackSelected;
                    break;
                case AnimationPlaybackLifecyclePhase.Retained:
                    kind = RuntimeTraceEventKind.AnimationPlaybackRetained;
                    break;
                default:
                    kind = RuntimeTraceEventKind.AnimationPlaybackRetired;
                    break;
            }

            if (diagnostics.ShouldPublish(RuntimeTraceChannel.Animation, kind))
            {
                diagnostics.Publish(
                    RuntimeTraceChannel.Animation,
                    RuntimeTraceDomain.Presentation,
                    kind,
                    RuntimeSourceElementHandle.Invalid,
                    ResolveInstance(diagnostics, snapshot.PlaybackId),
                    new RuntimeTracePayload
                    {
                        AnimationChannelId = snapshot.AnimationChannelId.Value,
                        Name = snapshot.PoseSlotId.Value,
                        Status = $"{snapshot.Phase}/{snapshot.SlotAvailability}",
                        OwnerId = snapshot.PlaybackId.ToString(),
                        RelatedElementId = snapshot.SourceId.ToString(),
                        Time = snapshot.SampleTime,
                        Weight = snapshot.SlotOutputWeight,
                        Flag = snapshot.HasVisualSample
                    });
            }
        }

        static void PublishRetired(RuntimeDiagnosticsContext diagnostics, AnimationPlaybackId playbackId)
        {
            if (!diagnostics.ShouldPublish(RuntimeTraceChannel.Animation, RuntimeTraceEventKind.AnimationPlaybackRetired))
                return;
            diagnostics.Publish(
                RuntimeTraceChannel.Animation,
                RuntimeTraceDomain.Presentation,
                RuntimeTraceEventKind.AnimationPlaybackRetired,
                RuntimeSourceElementHandle.Invalid,
                ResolveInstance(diagnostics, playbackId),
                new RuntimeTracePayload
                {
                    Status = AnimationPlaybackLifecyclePhase.Retired.ToString(),
                    OwnerId = playbackId.ToString()
                });
        }

        static void PublishMarkerSync(
            RuntimeDiagnosticsContext diagnostics,
            AnimationMarkerSyncRelationSnapshot snapshot)
        {
            if (!diagnostics.ShouldPublish(RuntimeTraceChannel.Animation, RuntimeTraceEventKind.AnimationMarkerSync))
                return;
            diagnostics.Publish(
                RuntimeTraceChannel.Animation,
                RuntimeTraceDomain.Presentation,
                RuntimeTraceEventKind.AnimationMarkerSync,
                RuntimeSourceElementHandle.Invalid,
                ResolveInstance(diagnostics, snapshot.Target),
                new RuntimeTracePayload
                {
                    AnimationChannelId = snapshot.AnimationChannelId.Value,
                    Name = snapshot.SyncGroupId,
                    Status = snapshot.Reason.ToString(),
                    OwnerId = snapshot.Target.ToString(),
                    RelatedElementId = snapshot.Source.ToString(),
                    Time = (float)snapshot.TargetEffectiveTime,
                    SecondaryTime = (float)snapshot.TargetRawTime,
                    NormalizedTime = snapshot.Fraction,
                    Cycle = snapshot.TargetEffectiveCycle,
                    Detail = $"{snapshot.PreviousMarkerId}->{snapshot.NextMarkerId} | Occurrence={snapshot.TargetOccurrenceIndex} | Depth={snapshot.RelationDepth} | Lifecycle={snapshot.TargetLifecyclePhase} | Source={snapshot.SourceRawTime:F4}->{snapshot.SourceEffectiveTime:F4}"
                });
        }

        static RuntimeInstanceKey ResolveInstance(
            RuntimeDiagnosticsContext diagnostics,
            AnimationPlaybackId playbackId)
        {
            return playbackId.IsValid
                ? RuntimeInstanceKey.Timeline(diagnostics.CharacterRuntimeId, playbackId.Generation)
                : RuntimeInstanceKey.Character(diagnostics.CharacterRuntimeId);
        }
    }
}
