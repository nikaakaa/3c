using System;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using Unity.Collections;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class CharacterPoseProgramFramePages : IDisposable
    {
        internal sealed class CommittedDiagnosticsPage
        {
            internal CommittedDiagnosticsPage(
                in AnimationPoseNativeAggregateLayout layout,
                int stateMachineCount)
            {
                layout.RequireValid();
                if (stateMachineCount < 0)
                    throw new ArgumentOutOfRangeException(nameof(stateMachineCount));
                Layout = layout;
                StateMachineCount = stateMachineCount;
                OperationCompletions =
                    new CharacterPoseOperationCompletion[layout.OperationCount];
                StateMachineBoneWeights =
                    new float[checked(stateMachineCount * layout.BoneCount)];
                SlotRanges =
                    new AnimationPlayerPoseNativeRange[layout.PlayerCount];
                SlotContributions =
                    new AnimationPrimitivePoseContribution[
                        layout.TotalPlayerContributionCapacity];
                SlotDenseContributionWeights =
                    new float[layout.PlayerDenseContributionWeightCapacity];
                SlotContributionCounts = new int[layout.PlayerCount];
                ValueDenseLocalPoses =
                    new AnimationLocalBonePose[layout.PoseValuePoseCapacity];
                ValueContributions =
                    new AnimationPrimitivePoseContribution[
                        layout.PoseValueContributionCapacity];
                ValueDenseContributionWeights =
                    new float[layout.PoseValueDenseContributionWeightCapacity];
                ValueContributionCounts = new int[layout.PoseValueCount];
                ValueOutputWeights = new float[layout.PoseValueCount];
                ValueAvailability =
                    new AnimationPoseAvailability[layout.PoseValueCount];
                ValueContinuityIdentities = new ulong[layout.PoseValueCount];
                ValueInvalidReasons =
                    new AnimationPoseNativeInvalidReason[layout.PoseValueCount];
            }

            internal ulong Identity;
            internal CharacterPoseProgramResult Result;
            internal AnimationPresentationDiagnosticsInterest Interest;
            internal readonly AnimationPoseNativeAggregateLayout Layout;
            internal readonly int StateMachineCount;
            internal readonly CharacterPoseOperationCompletion[]
                OperationCompletions;
            internal readonly float[] StateMachineBoneWeights;
            internal readonly AnimationPlayerPoseNativeRange[] SlotRanges;
            internal readonly AnimationPrimitivePoseContribution[]
                SlotContributions;
            internal readonly float[] SlotDenseContributionWeights;
            internal readonly int[] SlotContributionCounts;
            internal readonly AnimationLocalBonePose[] ValueDenseLocalPoses;
            internal readonly AnimationPrimitivePoseContribution[]
                ValueContributions;
            internal readonly float[] ValueDenseContributionWeights;
            internal readonly int[] ValueContributionCounts;
            internal readonly float[] ValueOutputWeights;
            internal readonly AnimationPoseAvailability[] ValueAvailability;
            internal readonly ulong[] ValueContinuityIdentities;
            internal readonly AnimationPoseNativeInvalidReason[]
                ValueInvalidReasons;
            internal AnimationPoseNativeInvalidReason PoseGraphInvalidReason;
        }

        sealed class Page
        {
            internal NativeArray<CharacterPoseStateMachineNativeControl>
                StateMachineControls;
            internal NativeArray<CharacterAnimationSlotNativeControl>
                AnimationSlotControls;
            internal NativeArray<CharacterRootOrientationWarpNativeControl>
                RootOrientationWarpControls;
            internal NativeArray<AnimationPoseGraphNativeLinkedPoseCallControl>
                LinkedPoseCallControls;
            internal NativeArray<byte> LinkedPoseActiveFragments;
        }

        Page m_Committed;
        Page m_Pending;
        Page m_Active;
        AnimationPoseNativeWorkspace m_Workspace;
        CommittedDiagnosticsPage m_CommittedDiagnostics;
        ulong m_NextDiagnosticsIdentity = 1;
        bool m_FrameOpen;
        bool m_Disposed;

        internal CharacterPoseProgramFramePages(
            int stateMachineCount,
            int animationSlotCount,
            int rootOrientationWarpCount,
            int linkedPoseCallCount,
            int linkedPoseFragmentCount,
            AnimationPoseNativeWorkspace workspace)
        {
            if (stateMachineCount < 0 ||
                animationSlotCount < 0 ||
                rootOrientationWarpCount < 0 ||
                linkedPoseCallCount < 0 ||
                linkedPoseFragmentCount < 0)
            {
                throw new ArgumentOutOfRangeException();
            }
            m_Workspace = workspace ??
                throw new ArgumentNullException(nameof(workspace));
            try
            {
                m_Committed = AllocatePage(
                    stateMachineCount,
                    animationSlotCount,
                    rootOrientationWarpCount,
                    linkedPoseCallCount,
                    linkedPoseFragmentCount);
                m_Pending = AllocatePage(
                    stateMachineCount,
                    animationSlotCount,
                    rootOrientationWarpCount,
                    linkedPoseCallCount,
                    linkedPoseFragmentCount);
                m_Active = m_Committed;
                AnimationPoseNativeAggregateLayout diagnosticsLayout =
                    m_Workspace.Layout;
                m_CommittedDiagnostics = new CommittedDiagnosticsPage(
                    in diagnosticsLayout,
                    stateMachineCount);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        internal NativeArray<CharacterPoseStateMachineNativeControl>
            StateMachineControls => RequireActive().StateMachineControls;
        internal NativeArray<CharacterAnimationSlotNativeControl>
            AnimationSlotControls => RequireActive().AnimationSlotControls;
        internal NativeArray<CharacterRootOrientationWarpNativeControl>
            RootOrientationWarpControls =>
                RequireActive().RootOrientationWarpControls;
        internal NativeArray<AnimationPoseGraphNativeLinkedPoseCallControl>
            LinkedPoseCallControls => RequireActive().LinkedPoseCallControls;
        internal NativeArray<byte> LinkedPoseActiveFragments =>
            RequireActive().LinkedPoseActiveFragments;
        internal bool HasOpenFrame => m_FrameOpen;
        internal bool HasPendingEvaluationFrame =>
            m_Workspace.HasPendingFrame;
        internal ulong PendingEvaluationCompletionIdentity =>
            m_Workspace.PendingCompletionIdentity;
        internal long DenseDoublePageResidentPayloadBytes =>
            m_Workspace.DenseDoublePageResidentPayloadBytes;
        internal CommittedDiagnosticsPage CommittedDiagnostics =>
            m_CommittedDiagnostics ??
            throw new InvalidOperationException(
                "Character Pose Program committed diagnostics page is missing.");

        internal void BeginFrame()
        {
            RequireAlive();
            if (m_FrameOpen)
            {
                throw new InvalidOperationException(
                    "Character Pose Program frame pages are already open.");
            }
            m_CommittedDiagnostics.Identity = 0;
            m_Active = m_Pending;
            ClearLinkedPose(m_Active);
            m_FrameOpen = true;
        }

        internal void CommitFrame()
        {
            RequireAlive();
            RequireOpenFrame();
            Page previousCommitted = m_Committed;
            m_Committed = m_Pending;
            m_Pending = previousCommitted;
            m_Active = m_Committed;
            m_FrameOpen = false;
        }

        internal void DiscardFrame()
        {
            RequireAlive();
            RequireOpenFrame();
            m_Active = m_Committed;
            m_FrameOpen = false;
        }

        internal CharacterPoseGraphNativeBinding BeginEvaluationFrame(
            ulong completionIdentity) =>
            m_Workspace.BeginFrame(completionIdentity);

        internal void RequireEvaluationStagesCompleted(
            ulong completionIdentity) =>
            m_Workspace.RequireStagesCompleted(completionIdentity);

        internal void CommitEvaluationFrame(ulong completionIdentity) =>
            m_Workspace.CommitFrame(completionIdentity);

        internal void DiscardEvaluationFrame(ulong completionIdentity) =>
            m_Workspace.DiscardFrame(completionIdentity);

        internal bool TryGetCommittedFinalReadBinding(
            out AnimationFinalPoseNativeReadBinding binding) =>
            m_Workspace.TryGetCommittedFinalReadBinding(out binding);

        internal AnimationPlayerPoseNativeWriteBinding
            RequirePlayerWriteBinding(
                int physicalSlotIndex,
                ulong completionIdentity) =>
            m_Workspace.RequirePlayerWriteBinding(
                physicalSlotIndex,
                completionIdentity);

        internal CharacterPoseGraphNativeBinding RequirePoseGraphBinding(
            ulong completionIdentity) =>
            m_Workspace.RequirePoseGraphBinding(completionIdentity);

        internal AnimationPoseValueNativeReadBinding RequirePoseValueReadBinding(
            int valueIndex,
            ulong completionIdentity) =>
            m_Workspace.RequirePoseValueReadBinding(
                valueIndex,
                completionIdentity);

        internal AnimationFinalPoseNativeReadBinding RequireFinalReadBinding(
            ulong completionIdentity) =>
            m_Workspace.RequireFinalReadBinding(completionIdentity);

        internal AnimationFinalPoseWriteOutcome RequireFinalWriteOutcome(
            ulong completionIdentity) =>
            m_Workspace.RequireFinalWriteOutcome(completionIdentity);

        internal PoseNodeId RequirePoseNodeId(int physicalSlotIndex) =>
            m_Workspace.RequirePoseNodeId(physicalSlotIndex);

        internal ulong PublishCommittedDiagnostics()
        {
            RequireAlive();
            if (m_FrameOpen || m_CommittedDiagnostics.Identity != 0)
            {
                throw new InvalidOperationException(
                    "Character Pose Program committed diagnostics cannot be published.");
            }
            ulong identity = m_NextDiagnosticsIdentity++;
            if (identity == 0)
            {
                identity = m_NextDiagnosticsIdentity++;
            }
            m_CommittedDiagnostics.Identity = identity;
            return identity;
        }

        internal void RequireValid()
        {
            RequireAlive();
            if (m_Workspace == null)
            {
                throw new InvalidOperationException(
                    "Character Pose Program evaluation workspace is missing.");
            }
            AnimationPoseNativeAggregateLayout workspaceLayout =
                m_Workspace.Layout;
            workspaceLayout.RequireValid();
            Page committed = m_Committed ??
                throw new InvalidOperationException(
                    "Character Pose Program committed frame page is missing.");
            RequirePage(
                committed,
                committed.StateMachineControls.Length,
                committed.AnimationSlotControls.Length,
                committed.RootOrientationWarpControls.Length,
                committed.LinkedPoseCallControls.Length,
                committed.LinkedPoseActiveFragments.Length);
            RequirePage(
                m_Pending,
                committed.StateMachineControls.Length,
                committed.AnimationSlotControls.Length,
                committed.RootOrientationWarpControls.Length,
                committed.LinkedPoseCallControls.Length,
                committed.LinkedPoseActiveFragments.Length);
            if (!ReferenceEquals(m_Active, m_Committed) &&
                !ReferenceEquals(m_Active, m_Pending))
            {
                throw new InvalidOperationException(
                    "Character Pose Program active frame page is detached.");
            }
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            if (m_CommittedDiagnostics != null)
            {
                m_CommittedDiagnostics.Identity = 0;
                m_CommittedDiagnostics.Result = default;
                m_CommittedDiagnostics = null;
            }
            m_Workspace?.Dispose();
            m_Workspace = null;
            DisposePage(m_Pending);
            DisposePage(m_Committed);
            m_Active = null;
            m_Pending = null;
            m_Committed = null;
            m_FrameOpen = false;
        }

        Page RequireActive()
        {
            RequireAlive();
            return m_Active ??
                throw new InvalidOperationException(
                    "Character Pose Program active frame page is missing.");
        }

        void RequireAlive()
        {
            if (m_Disposed)
            {
                throw new ObjectDisposedException(
                    nameof(CharacterPoseProgramFramePages));
            }
        }

        void RequireOpenFrame()
        {
            if (!m_FrameOpen)
            {
                throw new InvalidOperationException(
                    "Character Pose Program frame pages are not open.");
            }
        }

        static Page AllocatePage(
            int stateMachineCount,
            int animationSlotCount,
            int rootOrientationWarpCount,
            int linkedPoseCallCount,
            int linkedPoseFragmentCount)
        {
            var page = new Page();
            try
            {
                page.StateMachineControls = Allocate<
                    CharacterPoseStateMachineNativeControl>(
                    stateMachineCount);
                page.AnimationSlotControls = Allocate<
                    CharacterAnimationSlotNativeControl>(
                    animationSlotCount);
                page.RootOrientationWarpControls = AllocateClear<
                    CharacterRootOrientationWarpNativeControl>(
                    rootOrientationWarpCount);
                page.LinkedPoseCallControls = Allocate<
                    AnimationPoseGraphNativeLinkedPoseCallControl>(
                    linkedPoseCallCount);
                page.LinkedPoseActiveFragments = AllocateClear<byte>(
                    linkedPoseFragmentCount);
                ClearLinkedPose(page);
                return page;
            }
            catch
            {
                DisposePage(page);
                throw;
            }
        }

        static void ClearLinkedPose(Page page)
        {
            for (int i = 0; i < page.LinkedPoseCallControls.Length; i++)
            {
                page.LinkedPoseCallControls[i] =
                    AnimationPoseGraphNativeLinkedPoseCallControl.Inactive;
            }
            for (int i = 0; i < page.LinkedPoseActiveFragments.Length; i++)
                page.LinkedPoseActiveFragments[i] = 0;
        }

        static void RequirePage(
            Page page,
            int stateMachineCount,
            int animationSlotCount,
            int rootOrientationWarpCount,
            int linkedPoseCallCount,
            int linkedPoseFragmentCount)
        {
            if (page == null ||
                !page.StateMachineControls.IsCreated ||
                page.StateMachineControls.Length != stateMachineCount ||
                !page.AnimationSlotControls.IsCreated ||
                page.AnimationSlotControls.Length != animationSlotCount ||
                !page.RootOrientationWarpControls.IsCreated ||
                page.RootOrientationWarpControls.Length !=
                    rootOrientationWarpCount ||
                !page.LinkedPoseCallControls.IsCreated ||
                page.LinkedPoseCallControls.Length != linkedPoseCallCount ||
                !page.LinkedPoseActiveFragments.IsCreated ||
                page.LinkedPoseActiveFragments.Length !=
                    linkedPoseFragmentCount)
            {
                throw new InvalidOperationException(
                    "Character Pose Program frame page layout is invalid.");
            }
        }

        static NativeArray<T> Allocate<T>(int length)
            where T : struct =>
            new NativeArray<T>(
                length,
                Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);

        static NativeArray<T> AllocateClear<T>(int length)
            where T : struct =>
            new NativeArray<T>(
                length,
                Allocator.Persistent,
                NativeArrayOptions.ClearMemory);

        static void DisposePage(Page page)
        {
            if (page == null)
                return;
            DisposeArray(ref page.LinkedPoseActiveFragments);
            DisposeArray(ref page.LinkedPoseCallControls);
            DisposeArray(ref page.RootOrientationWarpControls);
            DisposeArray(ref page.AnimationSlotControls);
            DisposeArray(ref page.StateMachineControls);
        }

        static void DisposeArray<T>(ref NativeArray<T> values)
            where T : struct
        {
            if (values.IsCreated)
                values.Dispose();
            values = default;
        }
    }
}
