using System;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonCharacter.Pipeline.Presentation.Animancer;
using Unity.Collections;

namespace ThirdPersonCharacter.Pipeline.Animation.Presentation
{
    internal sealed class CharacterPoseConstraintRuntime : IDisposable
    {
        sealed class Bank
        {
            internal Bank(
                int solverCount,
                int contributionCount,
                int contributionGoalCount,
                CharacterFootPlacementModule footPlacement)
            {
                SolverOutcomes = new CharacterFullBodyIkResult[solverCount];
                BendHistories = new CharacterFullBodyIkBendHistory[solverCount];
                SolverDiagnostics =
                    new CharacterFullBodyIkSolverDiagnostics[solverCount];
                SolverEffectorCounts = new int[solverCount];
                SolverEffectors = new CharacterFullBodyIkEffectorDiagnostics[
                    checked(solverCount *
                            CharacterFullBodyIkGoalSetHeader.MaximumGoalCount)];
                SolverLimbCounts = new int[solverCount];
                SolverLimbs = new CharacterFullBodyIkLimbDiagnostics[
                    checked(solverCount * 4)];
                GoalSets = new NativeArray<CharacterFullBodyIkGoalSetHeader>(
                    1,
                    Allocator.Persistent,
                    NativeArrayOptions.ClearMemory);
                Goals = new NativeArray<CharacterFullBodyIkGoal>(
                    CharacterFullBodyIkGoalSetHeader.MaximumGoalCount,
                    Allocator.Persistent,
                    NativeArrayOptions.ClearMemory);
                GoalSetIndices = new NativeArray<int>(
                    1,
                    Allocator.Persistent,
                    NativeArrayOptions.ClearMemory);
                GoalContributions =
                    new NativeArray<CharacterFullBodyIkGoalContributionHeader>(
                        contributionCount,
                        Allocator.Persistent,
                        NativeArrayOptions.ClearMemory);
                ContributionGoals = new NativeArray<CharacterFullBodyIkGoal>(
                    contributionGoalCount,
                    Allocator.Persistent,
                    NativeArrayOptions.ClearMemory);
                FootPlacement = footPlacement?.CreateBank();
            }

            internal readonly CharacterFullBodyIkResult[] SolverOutcomes;
            internal readonly CharacterFullBodyIkBendHistory[] BendHistories;
            internal readonly CharacterFullBodyIkSolverDiagnostics[] SolverDiagnostics;
            internal readonly int[] SolverEffectorCounts;
            internal readonly CharacterFullBodyIkEffectorDiagnostics[] SolverEffectors;
            internal readonly int[] SolverLimbCounts;
            internal readonly CharacterFullBodyIkLimbDiagnostics[] SolverLimbs;
            internal NativeArray<CharacterFullBodyIkGoalSetHeader> GoalSets;
            internal NativeArray<CharacterFullBodyIkGoal> Goals;
            internal NativeArray<int> GoalSetIndices;
            internal NativeArray<CharacterFullBodyIkGoalContributionHeader>
                GoalContributions;
            internal NativeArray<CharacterFullBodyIkGoal> ContributionGoals;
            internal readonly CharacterFootPlacementBank FootPlacement;
            internal ulong Identity;
            internal ulong FrameIdentity;
            internal ulong RenderFrame;
            internal ulong CompletionIdentity;
            internal FixedString64Bytes RigId;
            internal FixedString64Bytes RigRevision;
            internal AnimationPhysicalBoneWriteDiagnostics PhysicalWrite;
            internal AnimationPresentationDiagnosticsInterest DiagnosticsInterest;

            internal void Begin(
                ulong frameIdentity,
                ulong renderFrame,
                AnimationPresentationDiagnosticsInterest diagnosticsInterest,
                Bank committed,
                FixedString64Bytes rigId,
                FixedString64Bytes rigRevision)
            {
                FrameIdentity = frameIdentity;
                RenderFrame = renderFrame;
                CompletionIdentity = 0;
                DiagnosticsInterest = diagnosticsInterest;
                RigId = rigId;
                RigRevision = rigRevision;
                PhysicalWrite = default;
                Array.Clear(SolverOutcomes, 0, SolverOutcomes.Length);
                Array.Clear(SolverDiagnostics, 0, SolverDiagnostics.Length);
                Array.Clear(SolverEffectorCounts, 0, SolverEffectorCounts.Length);
                Array.Clear(SolverEffectors, 0, SolverEffectors.Length);
                Array.Clear(SolverLimbCounts, 0, SolverLimbCounts.Length);
                Array.Clear(SolverLimbs, 0, SolverLimbs.Length);
                GoalSets[0] = default;
                GoalSetIndices[0] = 0;
                for (int i = 0; i < Goals.Length; i++)
                    Goals[i] = default;
                for (int i = 0; i < GoalContributions.Length; i++)
                    GoalContributions[i] = default;
                for (int i = 0; i < ContributionGoals.Length; i++)
                    ContributionGoals[i] = default;
                if (committed == null)
                    Array.Clear(BendHistories, 0, BendHistories.Length);
                else
                    Array.Copy(
                        committed.BendHistories,
                        BendHistories,
                        BendHistories.Length);
            }

            internal void ClearPending()
            {
                FrameIdentity = 0;
                RenderFrame = 0;
                CompletionIdentity = 0;
                DiagnosticsInterest = AnimationPresentationDiagnosticsInterest.None;
                RigId = default;
                RigRevision = default;
                PhysicalWrite = default;
                Array.Clear(SolverOutcomes, 0, SolverOutcomes.Length);
                Array.Clear(SolverDiagnostics, 0, SolverDiagnostics.Length);
                Array.Clear(SolverEffectorCounts, 0, SolverEffectorCounts.Length);
                Array.Clear(SolverEffectors, 0, SolverEffectors.Length);
                Array.Clear(SolverLimbCounts, 0, SolverLimbCounts.Length);
                Array.Clear(SolverLimbs, 0, SolverLimbs.Length);
                GoalSets[0] = default;
                GoalSetIndices[0] = 0;
                for (int i = 0; i < Goals.Length; i++)
                    Goals[i] = default;
                for (int i = 0; i < GoalContributions.Length; i++)
                    GoalContributions[i] = default;
                for (int i = 0; i < ContributionGoals.Length; i++)
                    ContributionGoals[i] = default;
                Array.Clear(BendHistories, 0, BendHistories.Length);
            }

            internal void Dispose()
            {
                if (ContributionGoals.IsCreated)
                    ContributionGoals.Dispose();
                if (GoalContributions.IsCreated)
                    GoalContributions.Dispose();
                if (GoalSetIndices.IsCreated)
                    GoalSetIndices.Dispose();
                if (Goals.IsCreated)
                    Goals.Dispose();
                if (GoalSets.IsCreated)
                    GoalSets.Dispose();
            }
        }

        readonly CharacterFootPlacementModule m_FootPlacement;
        readonly CharacterFinalIkFullBodySolver[] m_Solvers;
        readonly CharacterFullBodyIkGoalAssembler m_GoalAssembler;
        readonly AnimationFinalPosePhysicalWriter m_FinalWriter;
        readonly FixedString64Bytes m_RigId;
        readonly FixedString64Bytes m_RigRevision;
        readonly CharacterFootPlacementDiagnosticsPage m_EmptyFootDiagnostics =
            new CharacterFootPlacementDiagnosticsPage();
        readonly Bank m_First;
        readonly Bank m_Second;

        Bank m_Committed;
        Bank m_Pending;
        ulong m_NextBankIdentity = 1;
        bool m_HasCommitted;
        bool m_HasPending;
        bool m_Disposed;

        internal CharacterPoseConstraintRuntime(
            CharacterFootPlacementModule footPlacement,
            CharacterFinalIkFullBodySolver[] solvers,
            AnimationFinalPosePhysicalWriter finalWriter,
            int contributionCount,
            int contributionGoalCount,
            string rigId,
            string rigRevision)
        {
            m_FootPlacement = footPlacement;
            m_Solvers = solvers ?? throw new ArgumentNullException(nameof(solvers));
            m_GoalAssembler = new CharacterFullBodyIkGoalAssembler();
            m_FinalWriter = finalWriter ?? throw new ArgumentNullException(nameof(finalWriter));
            m_RigId = new FixedString64Bytes(rigId ?? string.Empty);
            m_RigRevision = new FixedString64Bytes(rigRevision ?? string.Empty);
            if (m_RigId.Length == 0 || m_RigRevision.Length == 0)
                throw new ArgumentException("Pose Constraint Rig lineage is invalid.");
            if (contributionCount < 0 || contributionGoalCount < 0)
                throw new ArgumentOutOfRangeException(nameof(contributionCount));
            for (int i = 0; i < m_Solvers.Length; i++)
            {
                if (m_Solvers[i] == null)
                    throw new ArgumentException("Pose Constraint solver collection is incomplete.", nameof(solvers));
            }
            if (m_Solvers.Length != 1)
                throw new ArgumentException("Pose Constraint requires exactly one Full Body IK solver.", nameof(solvers));
            m_First = new Bank(
                m_Solvers.Length,
                contributionCount,
                contributionGoalCount,
                m_FootPlacement);
            m_Second = new Bank(
                m_Solvers.Length,
                contributionCount,
                contributionGoalCount,
                m_FootPlacement);
        }

        internal bool HasFootPlacement => m_FootPlacement != null;
        internal int FullBodyIkSolverCount => m_Solvers.Length;
        internal int FullBodyIkGoalContributionCount =>
            m_First.GoalContributions.Length;
        internal int FullBodyIkContributionGoalCount =>
            m_First.ContributionGoals.Length;
        internal bool IsFullBodyIkPrepared(int solverIndex) =>
            (uint)solverIndex < (uint)m_Solvers.Length &&
            m_Solvers[solverIndex].IsPrepared;
        internal CharacterFullBodyIkSolverDiagnostics GetSolverDiagnostics(
            int solverIndex) =>
            m_HasCommitted && (uint)solverIndex < (uint)m_Solvers.Length
                ? m_Committed.SolverDiagnostics[solverIndex]
                : default;
        internal int GetSolverEffectorCount(int solverIndex) =>
            m_HasCommitted && (uint)solverIndex < (uint)m_Solvers.Length
                ? m_Committed.SolverEffectorCounts[solverIndex]
                : 0;
        internal CharacterFullBodyIkEffectorDiagnostics GetSolverEffector(
            int solverIndex,
            int effectorIndex) =>
            m_Committed.SolverEffectors[
                solverIndex * CharacterFullBodyIkGoalSetHeader.MaximumGoalCount +
                effectorIndex];
        internal int GetSolverLimbCount(int solverIndex) =>
            m_HasCommitted && (uint)solverIndex < (uint)m_Solvers.Length
                ? m_Committed.SolverLimbCounts[solverIndex]
                : 0;
        internal CharacterFullBodyIkLimbDiagnostics GetSolverLimb(
            int solverIndex,
            int limbIndex) =>
            m_Committed.SolverLimbs[solverIndex * 4 + limbIndex];
        internal CharacterFullBodyIkGoalSetHeader GetCommittedAssembledGoalSet(
            int solverIndex) =>
            m_HasCommitted && (uint)solverIndex < (uint)m_Solvers.Length
                ? m_Committed.GoalSets[0]
                : default;
        internal CharacterFullBodyIkGoal GetCommittedAssembledGoal(
            int solverIndex,
            int goalIndex)
        {
            CharacterFullBodyIkGoalSetHeader header =
                GetCommittedAssembledGoalSet(solverIndex);
            if ((uint)goalIndex >= (uint)header.GoalCount)
                throw new ArgumentOutOfRangeException(nameof(goalIndex));
            return m_Committed.Goals[header.GoalOffset + goalIndex];
        }
        internal CharacterFullBodyIkGoalContributionHeader
            GetPendingGoalContribution(int index) =>
            m_HasPending && (uint)index < (uint)m_Pending.GoalContributions.Length
                ? m_Pending.GoalContributions[index]
                : default;
        internal CharacterFullBodyIkGoalContributionHeader
            GetCommittedGoalContribution(int index) =>
            m_HasCommitted && (uint)index < (uint)m_Committed.GoalContributions.Length
                ? m_Committed.GoalContributions[index]
                : default;
        internal CharacterFullBodyIkGoal GetCommittedContributionGoal(int index)
        {
            if (!m_HasCommitted ||
                (uint)index >= (uint)m_Committed.ContributionGoals.Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            return m_Committed.ContributionGoals[index];
        }
        internal bool HasPendingFrame => m_HasPending;
        internal bool HasPendingAssembledGoalSet =>
            m_HasPending && m_Pending.GoalSets[0].IsValid;
        internal ulong CommittedBankIdentity => m_HasCommitted ? m_Committed.Identity : 0;
        internal ulong CommittedRenderFrame =>
            m_HasCommitted ? m_Committed.RenderFrame : 0;
        internal CharacterFootPlacementDiagnosticsPage CommittedFootDiagnostics =>
            m_HasCommitted && m_Committed.FootPlacement != null
                ? m_Committed.FootPlacement.Diagnostics
                : m_EmptyFootDiagnostics;
        internal AnimationPhysicalBoneWriteDiagnostics PhysicalWriteDiagnostics =>
            m_HasCommitted ? m_Committed.PhysicalWrite : default;

        internal void BeginFrame(
            ulong frameIdentity,
            ulong renderFrame,
            AnimationPresentationDiagnosticsInterest diagnosticsInterest)
        {
            RequireAlive();
            if (frameIdentity == 0 || renderFrame == 0)
                throw new ArgumentOutOfRangeException(nameof(frameIdentity));
            if (m_HasPending)
                throw new InvalidOperationException("Pose Constraint frame is already open.");
            m_Pending = m_HasCommitted && ReferenceEquals(m_Committed, m_First)
                ? m_Second
                : m_First;
            m_Pending.Begin(
                frameIdentity,
                renderFrame,
                diagnosticsInterest,
                m_HasCommitted ? m_Committed : null,
                m_RigId,
                m_RigRevision);
            m_FootPlacement?.BeginFrame(
                m_HasCommitted ? m_Committed.FootPlacement : null,
                m_Pending.FootPlacement,
                diagnosticsInterest != AnimationPresentationDiagnosticsInterest.None);
            m_HasPending = true;
        }

        internal CharacterFullBodyIkGoalContributionHeader PrepareFootPlacement(
            in CharacterFootPlacementFrameInput frame,
            int contributionIndex,
            int goalOffset,
            int producerOperationIndex,
            int producerCallSiteIndex)
        {
            RequireRenderFrame(frame.RenderFrame, frame.Pose.CompletionIdentity);
            if (m_FootPlacement == null)
                throw new InvalidOperationException("Pose Constraint Foot Placement module is unavailable.");
            if ((uint)contributionIndex >=
                    (uint)m_Pending.GoalContributions.Length ||
                goalOffset < 0 ||
                goalOffset > m_Pending.ContributionGoals.Length -
                CharacterPresentationFootPlacementDescriptor.GoalCount)
            {
                throw new ArgumentOutOfRangeException(nameof(contributionIndex));
            }
            CharacterFootPlacementResult result =
                m_FootPlacement.EvaluateFrame(in frame);
            m_Pending.ContributionGoals[goalOffset] = result.PelvisGoal;
            m_Pending.ContributionGoals[goalOffset + 1] = result.LeftGoal;
            m_Pending.ContributionGoals[goalOffset + 2] = result.RightGoal;
            var contribution = new CharacterFullBodyIkGoalContributionHeader(
                result.FrameSequence,
                result.CompletionIdentity,
                result.RigId,
                result.RigRevision,
                producerOperationIndex,
                producerCallSiteIndex,
                goalOffset,
                CharacterPresentationFootPlacementDescriptor.GoalCount,
                CharacterFullBodyIkGoalContributionAvailability.Ready);
            m_Pending.GoalContributions[contributionIndex] = contribution;
            return contribution;
        }

        internal void RecordUnavailableGoalContribution(
            int contributionIndex,
            in CharacterFullBodyIkGoalContributionHeader contribution)
        {
            RequireRenderFrame(
                contribution.FrameSequence,
                contribution.CompletionIdentity);
            if ((uint)contributionIndex >=
                    (uint)m_Pending.GoalContributions.Length ||
                !contribution.IsValid ||
                contribution.Availability !=
                CharacterFullBodyIkGoalContributionAvailability.WorldContextUnavailable ||
                contribution.GoalCount != 0 ||
                !contribution.RigId.Equals(m_RigId) ||
                !contribution.RigRevision.Equals(m_RigRevision))
            {
                throw new ArgumentException(
                    "Unavailable Goal Contribution is invalid.");
            }
            m_Pending.GoalContributions[contributionIndex] = contribution;
        }

        internal CharacterFullBodyIkGoalContributionHeader ProducePoseBoneIkGoals(
            int contributionIndex,
            int goalOffset,
            NativeSlice<AnimationLocalBonePose> componentPose,
            NativeSlice<CharacterPoseBoneIkGoalDescriptor> descriptors,
            int producerOperationIndex,
            int producerCallSiteIndex,
            ulong frameSequence,
            ulong completionIdentity)
        {
            RequireRenderFrame(frameSequence, completionIdentity);
            if ((uint)contributionIndex >=
                    (uint)m_Pending.GoalContributions.Length ||
                goalOffset < 0 ||
                goalOffset > m_Pending.ContributionGoals.Length - descriptors.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(contributionIndex));
            }
            var goals = new NativeSlice<CharacterFullBodyIkGoal>(
                m_Pending.ContributionGoals,
                goalOffset,
                descriptors.Length);
            CharacterFullBodyIkGoalContributionHeader contribution =
                CharacterPoseBoneIkGoalSource.Produce(
                    componentPose,
                    descriptors,
                    goals,
                    goalOffset,
                    frameSequence,
                    completionIdentity,
                    m_RigId,
                    m_RigRevision,
                    producerOperationIndex,
                    producerCallSiteIndex);
            m_Pending.GoalContributions[contributionIndex] = contribution;
            return contribution;
        }

        internal CharacterFullBodyIkResult AssembleFullBodyIkGoals(
            NativeSlice<int> contributionValueIndices,
            int producerOperationIndex,
            int producerCallSiteIndex,
            ulong frameSequence,
            ulong completionIdentity,
            out CharacterFullBodyIkGoalSetHeader goalSet)
        {
            RequireRenderFrame(frameSequence, completionIdentity);
            if (m_Pending.GoalSets[0].IsValid)
                throw new InvalidOperationException("Full Body IK Goals were already assembled for this frame.");
            CharacterFullBodyIkResult result = m_GoalAssembler.Assemble(
                contributionValueIndices,
                m_Pending.GoalContributions,
                m_Pending.ContributionGoals,
                frameSequence,
                completionIdentity,
                m_RigId,
                m_RigRevision,
                producerOperationIndex,
                producerCallSiteIndex,
                m_Pending.GoalSets,
                m_Pending.Goals,
                m_Pending.GoalSetIndices);
            if (!result.Succeeded)
            {
                m_Pending.SolverOutcomes[0] = result;
                goalSet = default;
                return result;
            }
            goalSet = m_Pending.GoalSets[0];
            return result;
        }

        internal CharacterFullBodyIkResult SolveFullBodyIk(
            int solverIndex,
            NativeSlice<AnimationLocalBonePose> pendingOutputComponentPose,
            int producerOperationIndex,
            int producerCallSiteIndex,
            ulong frameSequence,
            ulong completionIdentity,
            bool recordDiagnostics)
        {
            RequireRenderFrame(frameSequence, completionIdentity);
            if (solverIndex != 0 || !m_Pending.GoalSets[0].IsValid)
                throw new InvalidOperationException("Full Body IK requires the unique assembled Goal Set.");
            CharacterFullBodyIkResult result = m_Solvers[solverIndex].SolvePrepared(
                pendingOutputComponentPose,
                new NativeSlice<int>(m_Pending.GoalSetIndices),
                m_Pending.GoalSets,
                m_Pending.Goals,
                ref m_Pending.BendHistories[solverIndex],
                frameSequence,
                completionIdentity,
                recordDiagnostics);
            m_Pending.SolverOutcomes[solverIndex] = result;
            if (recordDiagnostics)
            {
                CharacterFinalIkFullBodySolver solver = m_Solvers[solverIndex];
                int effectorCount = solver.DiagnosticEffectorCount;
                int limbCount = solver.DiagnosticLimbCount;
                if (effectorCount < 0 ||
                    effectorCount > CharacterFullBodyIkGoalSetHeader.MaximumGoalCount ||
                    limbCount < 0 || limbCount > 4)
                {
                    throw new InvalidOperationException(
                        "Full Body IK diagnostics exceeded the root Bank capacity.");
                }
                m_Pending.SolverDiagnostics[solverIndex] = solver.Diagnostics;
                m_Pending.SolverEffectorCounts[solverIndex] = effectorCount;
                int effectorOffset = solverIndex *
                                     CharacterFullBodyIkGoalSetHeader.MaximumGoalCount;
                for (int i = 0; i < effectorCount; i++)
                {
                    m_Pending.SolverEffectors[effectorOffset + i] =
                        solver.GetDiagnosticEffector(i);
                }
                m_Pending.SolverLimbCounts[solverIndex] = limbCount;
                int limbOffset = solverIndex * 4;
                for (int i = 0; i < limbCount; i++)
                {
                    m_Pending.SolverLimbs[limbOffset + i] =
                        solver.GetDiagnosticLimb(i);
                }
            }
            return result;
        }

        internal string ApplyTuning(
            CharacterPoseTuningLayout layout,
            CharacterPoseTuningParameterBlock block,
            bool resetOwnerState)
        {
            RequireAlive();
            for (int i = 0; i < m_Solvers.Length; i++)
            {
                string error = m_Solvers[i].ApplyTuning(
                    layout,
                    block,
                    resetOwnerState);
                if (!string.IsNullOrEmpty(error))
                    return error;
            }
            if (resetOwnerState)
                ClearBendHistories();
            return m_FootPlacement?.ApplyTuning(layout, block, resetOwnerState) ?? string.Empty;
        }

        internal bool TryGetFullBodyIkFailure(
            int solverIndex,
            ulong completionIdentity,
            out CharacterFullBodyIkResult result)
        {
            RequireAlive();
            result = default;
            if ((uint)solverIndex >= (uint)m_Solvers.Length)
                return false;
            Bank bank = m_HasPending &&
                        m_Pending.CompletionIdentity == completionIdentity
                ? m_Pending
                : m_HasCommitted &&
                  m_Committed.CompletionIdentity == completionIdentity
                    ? m_Committed
                    : null;
            if (bank == null)
                return false;
            result = bank.SolverOutcomes[solverIndex];
            return !result.Succeeded;
        }

        internal void ValidateWriterBeforeEvaluate(
            in AnimationFinalPoseNativeReadBinding pending,
            bool hasCommitted,
            in AnimationFinalPoseNativeReadBinding committed) =>
            m_FinalWriter.ValidateBindingsBeforeEvaluate(
                in pending,
                hasCommitted,
                in committed);

        internal void WritePhysicalPose(
            in AnimationFinalPoseNativeReadBinding pending,
            bool hasCommitted,
            in AnimationFinalPoseNativeReadBinding committed)
        {
            m_FinalWriter.Write(
                in pending,
                hasCommitted,
                in committed);
            m_Pending.PhysicalWrite = m_FinalWriter.Diagnostics;
        }

        internal void ValidateCompletedFrameBeforeWrite(
            ulong frameIdentity,
            ulong renderFrame,
            ulong completionIdentity)
        {
            RequireRenderFrame(renderFrame, completionIdentity);
            if (m_Pending.FrameIdentity != frameIdentity)
                throw new InvalidOperationException(
                    "Pose Constraint transaction identity is inconsistent before the Physical Writer.");
            if (!m_Pending.GoalSets[0].IsValid ||
                m_Pending.GoalSets[0].FrameSequence != renderFrame ||
                m_Pending.GoalSets[0].CompletionIdentity != completionIdentity ||
                !m_Pending.GoalSets[0].RigId.Equals(m_RigId) ||
                !m_Pending.GoalSets[0].RigRevision.Equals(m_RigRevision))
            {
                throw new InvalidOperationException(
                    "Pose Constraint Goal Set is incomplete before the Physical Writer.");
            }
            for (int i = 0; i < m_Pending.SolverOutcomes.Length; i++)
            {
                if (!m_Pending.SolverOutcomes[i].Succeeded)
                {
                    throw new InvalidOperationException(
                        "Pose Constraint Solver Outcome is invalid before the Physical Writer.");
                }
            }
            m_FootPlacement?.ValidateFrame(renderFrame, completionIdentity);
        }

        internal void SealFrame()
        {
            m_FootPlacement?.CompleteFrame();
            m_Pending.Identity = m_NextBankIdentity++;
            m_Committed = m_Pending;
            m_HasCommitted = true;
            m_Pending = null;
            m_HasPending = false;
            m_FootPlacement?.PublishCommittedDiagnostics(m_Committed.FootPlacement);
        }

        internal void DiscardFrame()
        {
            RequireAlive();
            if (!m_HasPending)
                return;
            m_FootPlacement?.DiscardFrame();
            m_Pending.ClearPending();
            m_Pending = null;
            m_HasPending = false;
        }

        internal void ResetSolvers()
        {
            RequireAlive();
            for (int i = 0; i < m_Solvers.Length; i++)
                m_Solvers[i].Reset();
            ClearBendHistories();
        }

        internal void ResetFootPlacement(in CharacterFootPlacementReset reset)
        {
            RequireAlive();
            if (m_FootPlacement == null)
                return;
            if (m_HasPending)
                DiscardFrame();
            m_First.FootPlacement.Reset();
            m_Second.FootPlacement.Reset();
            m_FootPlacement.ResetShared(in reset);
        }

        internal void RetargetFootPlacement(ulong resetSequence)
        {
            RequireAlive();
            if (m_FootPlacement == null)
                return;
            if (m_HasPending)
                DiscardFrame();
            m_First.FootPlacement.Reset();
            m_Second.FootPlacement.Reset();
            m_FootPlacement.RetargetShared(resetSequence);
        }

        void RequireRenderFrame(ulong renderFrame, ulong completionIdentity)
        {
            RequireAlive();
            if (!m_HasPending || m_Pending.RenderFrame != renderFrame)
                throw new InvalidOperationException("Pose Constraint pending frame identity is inconsistent.");
            BindCompletion(completionIdentity);
        }

        void RequireTransactionFrame(ulong frameIdentity, ulong completionIdentity)
        {
            RequireAlive();
            if (!m_HasPending || m_Pending.FrameIdentity != frameIdentity)
                throw new InvalidOperationException("Pose Constraint pending transaction identity is inconsistent.");
            BindCompletion(completionIdentity);
        }

        void BindCompletion(ulong completionIdentity)
        {
            if (completionIdentity == 0)
                throw new ArgumentOutOfRangeException(nameof(completionIdentity));
            if (m_Pending.CompletionIdentity == 0)
                m_Pending.CompletionIdentity = completionIdentity;
            else if (m_Pending.CompletionIdentity != completionIdentity)
                throw new InvalidOperationException("Pose Constraint pending completion identity is inconsistent.");
        }

        void ClearBendHistories()
        {
            ClearSolverBank(m_First);
            ClearSolverBank(m_Second);
        }

        static void ClearSolverBank(Bank bank)
        {
            Array.Clear(bank.BendHistories, 0, bank.BendHistories.Length);
            Array.Clear(bank.SolverOutcomes, 0, bank.SolverOutcomes.Length);
            Array.Clear(bank.SolverDiagnostics, 0, bank.SolverDiagnostics.Length);
            Array.Clear(bank.SolverEffectorCounts, 0, bank.SolverEffectorCounts.Length);
            Array.Clear(bank.SolverEffectors, 0, bank.SolverEffectors.Length);
            Array.Clear(bank.SolverLimbCounts, 0, bank.SolverLimbCounts.Length);
            Array.Clear(bank.SolverLimbs, 0, bank.SolverLimbs.Length);
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(CharacterPoseConstraintRuntime));
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            if (m_HasPending)
                m_FootPlacement?.DiscardFrame();
            m_FootPlacement?.Dispose();
            for (int i = m_Solvers.Length - 1; i >= 0; i--)
                m_Solvers[i].Reset();
            m_First.FootPlacement?.Reset();
            m_Second.FootPlacement?.Reset();
            m_First.ClearPending();
            m_Second.ClearPending();
            m_First.Dispose();
            m_Second.Dispose();
            m_Committed = null;
            m_Pending = null;
            m_HasCommitted = false;
            m_HasPending = false;
        }
    }
}
