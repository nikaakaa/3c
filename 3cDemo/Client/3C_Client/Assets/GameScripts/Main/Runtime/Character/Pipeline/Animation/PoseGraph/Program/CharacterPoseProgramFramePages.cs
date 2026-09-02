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
        CharacterPoseSourcePreparationPage m_SourcePreparations;
        CharacterPoseSourceDemand m_SourceDemand;
        CommittedDiagnosticsPage m_CommittedDiagnostics;
        ulong m_NextDiagnosticsIdentity = 1;
        bool m_FrameOpen;
        bool m_Disposed;

        internal CharacterPoseProgramFramePages(
            CharacterPoseProgramImage program,
            int sourcePreparationCapacity,
            AnimationPoseNativeWorkspace workspace)
        {
            if (program == null)
                throw new ArgumentNullException(nameof(program));
            program.RequireValid();
            if (sourcePreparationCapacity <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(sourcePreparationCapacity));
            }
            int stateMachineCount = program.StateMachines.Count;
            int animationSlotCount = program.AnimationSlots.Count;
            int rootOrientationWarpCount =
                program.RootOrientationWarps.Count;
            int linkedPoseCallCount = program.LinkedPoseCalls.Count;
            int linkedPoseFragmentCount =
                program.LinkedPoseFragments.Count;
            m_Workspace = workspace ??
                throw new ArgumentNullException(nameof(workspace));
            m_SourcePreparations = new CharacterPoseSourcePreparationPage(
                sourcePreparationCapacity);
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
                InitializeStateMachineControls(program);
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

        void InitializeStateMachineControls(
            CharacterPoseProgramImage program)
        {
            NativeArray<CharacterPoseStateMachineNativeControl> controls =
                m_Committed.StateMachineControls;
            for (int i = 0; i < program.StateMachines.Count; i++)
            {
                CharacterPoseStateMachineDescriptor machine =
                    program.StateMachines[i];
                int output = machine.States[machine.EntryStateIndex]
                    .OutputPoseValueIndex;
                controls[i] = new CharacterPoseStateMachineNativeControl(
                    output,
                    output,
                    machine.EntryStateIndex,
                    machine.EntryStateIndex,
                    0f,
                    0f,
                    -1,
                    -1,
                    CharacterPoseStateMachineBlendMode.Single,
                    1);
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
        internal AnimationPoseNativeAggregateLayout EvaluationLayout =>
            m_Workspace.Layout;
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

        internal CharacterPoseSourcePreparationView BeginSourceDemand(
            ulong completionIdentity)
        {
            RequireAlive();
            m_SourceDemand = default;
            return m_SourcePreparations.Begin(completionIdentity);
        }

        internal void BindSourceDemand(
            in CharacterPoseSourceDemand demand)
        {
            RequireAlive();
            if (!demand.IsValid ||
                m_SourceDemand.IsValid ||
                !m_SourcePreparations.Matches(
                    demand.Lineage.CompletionIdentity))
            {
                throw new InvalidOperationException(
                    "Character Pose Program source demand cannot be bound.");
            }
            CharacterPoseSourcePreparationView preparations =
                demand.Preparations;
            CharacterPoseSourcePreparationView current =
                new CharacterPoseSourcePreparationView(
                    m_SourcePreparations,
                    demand.Lineage.CompletionIdentity);
            if (!preparations.Matches(in current))
            {
                throw new InvalidOperationException(
                    "Character Pose Program source demand preparation page differs from its owner.");
            }
            m_SourceDemand = demand;
        }

        internal CharacterPoseSourceDemand RequireSourceDemand(
            in CharacterPoseSourceDemand demand)
        {
            RequireAlive();
            CharacterPoseSourceDemand current = m_SourceDemand;
            CharacterPoseSourcePreparationView expected =
                current.Preparations;
            CharacterPoseSourcePreparationView actual =
                demand.Preparations;
            if (!current.IsValid ||
                !demand.IsValid ||
                current.Lineage != demand.Lineage ||
                current.ActionSourceCount != demand.ActionSourceCount ||
                current.ProviderSourceCount != demand.ProviderSourceCount ||
                !ReferenceEquals(
                    current.ProviderDemands,
                    demand.ProviderDemands) ||
                !expected.Matches(in actual))
            {
                throw new InvalidOperationException(
                    "Character Pose Program source demand is stale.");
            }
            return current;
        }

        internal void SetStateMachineControl(
            int stateMachineIndex,
            in CharacterPoseStateMachineNativeControl control)
        {
            NativeArray<CharacterPoseStateMachineNativeControl> controls =
                StateMachineControls;
            if ((uint)stateMachineIndex >= (uint)controls.Length)
                throw new ArgumentOutOfRangeException(nameof(stateMachineIndex));
            controls[stateMachineIndex] = control;
        }

        internal void SetAnimationSlotControl(
            int animationSlotIndex,
            in CharacterAnimationSlotNativeControl control)
        {
            NativeArray<CharacterAnimationSlotNativeControl> controls =
                AnimationSlotControls;
            if ((uint)animationSlotIndex >= (uint)controls.Length)
                throw new ArgumentOutOfRangeException(nameof(animationSlotIndex));
            controls[animationSlotIndex] = control;
        }

        internal void SetRootOrientationWarpControl(
            int rootOrientationWarpIndex,
            in CharacterRootOrientationWarpNativeControl control)
        {
            NativeArray<CharacterRootOrientationWarpNativeControl> controls =
                RootOrientationWarpControls;
            if ((uint)rootOrientationWarpIndex >= (uint)controls.Length ||
                !control.IsValid)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(rootOrientationWarpIndex));
            }
            controls[rootOrientationWarpIndex] = control;
        }

        internal int AddSourcePreparation(
            in CharacterPoseSourcePreparation preparation)
        {
            RequireAlive();
            return m_SourcePreparations.Add(in preparation);
        }

        internal void ClearSourceDemand()
        {
            m_SourceDemand = default;
            m_SourcePreparations?.Clear();
        }

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
            if (m_SourcePreparations == null)
            {
                throw new InvalidOperationException(
                    "Character Pose Program source preparation page is missing.");
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
            ClearSourceDemand();
            m_SourcePreparations = null;
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
