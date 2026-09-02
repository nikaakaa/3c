using System;
using System.Collections.Generic;
using BTSMTL.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using ThirdPersonCharacter.Pipeline.Animation.Sources;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal sealed class
        CharacterAnimationPresentationDiagnosticsCoordinator
    {
        readonly ActorId m_ActorId;
        readonly CharacterPoseProgramRuntime m_Program;
        readonly CharacterPoseSourceModule m_Source;
        readonly CharacterPoseConstraintRuntime m_Constraints;
        readonly CharacterFinalPosePublication m_Publication;
        readonly CharacterPoseDiagnosticsRuntime m_Diagnostics;
        readonly List<ActionAnimationPlaybackLifecycleSnapshot>
            m_ActionSnapshots;
        readonly List<ActionPresentationTimeSnapshot> m_ActionTimeSnapshots;
        readonly List<PoseStateSourceSyncSnapshot> m_SourceSyncSnapshots;
        readonly List<AnimationPlaybackId> m_RetiredPlaybacks;
        AnimationPresentationDebugView m_DebugView;

        internal CharacterAnimationPresentationDiagnosticsCoordinator(
            ActorId actorId,
            CharacterPoseRuntimeComposition modules,
            int actionCapacity,
            int sourceSyncCapacity)
        {
            m_ActorId = actorId.IsValid
                ? actorId
                : throw new ArgumentException(
                    "Animation Presentation Actor identity is invalid.",
                    nameof(actorId));
            if (modules == null)
                throw new ArgumentNullException(nameof(modules));
            m_Program = modules.Program;
            m_Source = modules.Source;
            m_Constraints = modules.Constraints;
            m_Publication = modules.Publication;
            m_Diagnostics = modules.Diagnostics;
            m_ActionSnapshots =
                new List<ActionAnimationPlaybackLifecycleSnapshot>(
                    actionCapacity);
            m_ActionTimeSnapshots =
                new List<ActionPresentationTimeSnapshot>(actionCapacity);
            m_SourceSyncSnapshots =
                new List<PoseStateSourceSyncSnapshot>(sourceSyncCapacity);
            m_RetiredPlaybacks =
                new List<AnimationPlaybackId>(actionCapacity);
        }

        internal IReadOnlyList<AnimationPlaybackId> RetiredPlaybacks =>
            m_RetiredPlaybacks;
        internal IReadOnlyList<ActionAnimationPlaybackLifecycleSnapshot>
            ActionSnapshots => m_ActionSnapshots;
        internal bool HasRuntimeSnapshot => m_Diagnostics.HasCurrent;
        internal AnimationPresentationRuntimeSnapshot RuntimeSnapshot =>
            m_Diagnostics.Current;
        internal bool HasDebugView =>
            m_DebugView != null && m_Diagnostics.HasCurrent;
        internal AnimationPresentationDebugView DebugView =>
            HasDebugView
                ? m_DebugView
                : throw new InvalidOperationException(
                    "Animation Presentation Debug View is unavailable.");
        internal AnimationPresentationDiagnosticsInterest Interest =>
            m_Diagnostics.Interest;
        internal ulong NoInterestSkipCount =>
            m_Diagnostics.NoInterestSkipCount;

        internal void SetPoseWatchInterests(
            Guid ownerId,
            IReadOnlyList<AnimationPoseWatchIdentity> interests) =>
            m_Diagnostics.SetPoseWatchInterests(ownerId, interests);

        internal void RemovePoseWatchInterests(Guid ownerId) =>
            m_Diagnostics.RemovePoseWatchInterests(ownerId);

        internal void SetDiagnosticsInterest(
            Guid ownerId,
            AnimationPresentationDiagnosticsInterest interest) =>
            m_Diagnostics.SetDiagnosticsInterest(ownerId, interest);

        internal void RemoveDiagnosticsInterest(Guid ownerId) =>
            m_Diagnostics.RemoveDiagnosticsInterest(ownerId);

        internal AnimationPresentationDiagnosticsInterest ResolveInterest(
            AnimationPresentationDiagnosticsInterest transientInterest) =>
            m_Diagnostics.ResolveFrameInterest(transientInterest);

        internal void ClearWhenUnobserved(
            AnimationPresentationDiagnosticsInterest interest)
        {
            if (interest == AnimationPresentationDiagnosticsInterest.None)
                ClearPublishedDiagnostics();
        }

        internal void PublishCommittedFrame(
            CharacterPoseFrameTransaction transaction,
            CharacterLinkedPoseRuntimeSession linkedPose,
            in CharacterPoseFrameExecutionResult executionResult,
            AnimationPresentationDiagnosticsInterest diagnosticsInterest,
            bool publishStateDiagnostics,
            AnimationPresentationDiagnosticsInterest traceInterest,
            RuntimeDiagnosticsContext diagnostics)
        {
            bool publishRuntimeDiagnostics =
                diagnosticsInterest !=
                AnimationPresentationDiagnosticsInterest.None;
            bool captureFootIk = transaction.CaptureFootIkDiagnostics;
            if (!publishRuntimeDiagnostics && !captureFootIk)
            {
                m_Diagnostics.RecordNoInterestSkip();
                return;
            }
            if (publishStateDiagnostics)
                BuildCommittedSnapshots(transaction);
            CharacterPoseSourceFrameResult committedSourceFrame =
                transaction.SourceFrame;
            m_Diagnostics.BeginCommittedFrame(
                diagnosticsInterest,
                captureFootIk,
                linkedPose,
                m_Program,
                m_Source,
                m_Constraints,
                m_Publication,
                in committedSourceFrame,
                in executionResult);
            if (!publishRuntimeDiagnostics)
                return;
            CharacterFootIkCommittedCaptureViewLease footIkCaptureView =
                m_Diagnostics.Publish();
            if (publishStateDiagnostics)
                PublishCommittedSnapshots(transaction);
            else
                ClearCommittedStateSnapshots();
            PublishCommittedDebugView(publishStateDiagnostics);
            AnimationPresentationTracePublisher.PublishCompletedFootPlacement(
                m_ActorId,
                footIkCaptureView);
            if (traceInterest != AnimationPresentationDiagnosticsInterest.None)
            {
                AnimationPresentationTracePublisher.Publish(
                    diagnostics,
                    m_DebugView,
                    m_RetiredPlaybacks);
            }
        }

        internal void ClearPublishedState()
        {
            m_ActionSnapshots.Clear();
            m_ActionTimeSnapshots.Clear();
            m_SourceSyncSnapshots.Clear();
            m_RetiredPlaybacks.Clear();
            m_DebugView = null;
        }

        void BuildCommittedSnapshots(
            CharacterPoseFrameTransaction transaction)
        {
            CopyActionSnapshots(
                m_Program.BuildCommittedActionLifecycleSnapshot(),
                transaction.ActionSnapshots);
            m_Program.BuildCommittedActionTimeSnapshots(
                transaction.TimeSnapshots);
            foreach (AnimationPlaybackId playbackId in
                     m_Program.RetiredActionPlaybacks)
            {
                transaction.RetiredPlaybacks.Add(playbackId);
            }
            transaction.RetiredPlaybacks.Sort(ComparePlayback);
        }

        void PublishCommittedSnapshots(
            CharacterPoseFrameTransaction transaction)
        {
            CopyActionSnapshots(
                transaction.ActionSnapshots,
                m_ActionSnapshots);
            Copy(transaction.TimeSnapshots, m_ActionTimeSnapshots);
            Copy(transaction.RetiredPlaybacks, m_RetiredPlaybacks);
        }

        void PublishCommittedDebugView(bool includeStateDiagnostics)
        {
            if (!m_Diagnostics.HasCurrent)
            {
                m_SourceSyncSnapshots.Clear();
                m_DebugView = null;
                return;
            }
            if (includeStateDiagnostics)
                m_Program.CopySourceSyncSnapshots(m_SourceSyncSnapshots);
            else
                m_SourceSyncSnapshots.Clear();
            AnimationPresentationRuntimeSnapshot posePlan =
                m_Diagnostics.Current;
            m_DebugView = new AnimationPresentationDebugView(
                in posePlan,
                m_ActionSnapshots,
                m_ActionTimeSnapshots,
                m_SourceSyncSnapshots);
        }

        void ClearCommittedStateSnapshots()
        {
            m_ActionSnapshots.Clear();
            m_ActionTimeSnapshots.Clear();
            m_RetiredPlaybacks.Clear();
            m_SourceSyncSnapshots.Clear();
        }

        void ClearPublishedDiagnostics()
        {
            if (m_DebugView == null &&
                !m_Diagnostics.HasCurrent &&
                m_ActionSnapshots.Count == 0 &&
                m_ActionTimeSnapshots.Count == 0 &&
                m_RetiredPlaybacks.Count == 0 &&
                m_SourceSyncSnapshots.Count == 0)
            {
                return;
            }
            ClearCommittedStateSnapshots();
            m_DebugView = null;
            m_Diagnostics.Invalidate();
        }

        static void CopyActionSnapshots(
            IReadOnlyList<ActionAnimationPlaybackLifecycleSnapshot> source,
            FixedCapacityFrameBuffer<ActionAnimationPlaybackLifecycleSnapshot>
                destination)
        {
            destination.Clear();
            for (int i = 0; i < source.Count; i++)
                destination.Add(source[i]);
        }

        static void CopyActionSnapshots(
            IReadOnlyList<ActionAnimationPlaybackLifecycleSnapshot> source,
            List<ActionAnimationPlaybackLifecycleSnapshot> destination) =>
            Copy(source, destination);

        static void Copy<T>(IReadOnlyList<T> source, List<T> destination)
        {
            destination.Clear();
            for (int i = 0; i < source.Count; i++)
                destination.Add(source[i]);
        }

        static int ComparePlayback(
            AnimationPlaybackId left,
            AnimationPlaybackId right)
        {
            int producer = string.Compare(
                left.ProducerId.ProgramProducerIdentity,
                right.ProducerId.ProgramProducerIdentity,
                StringComparison.Ordinal);
            return producer != 0
                ? producer
                : left.Generation.CompareTo(right.Generation);
        }
    }
}
