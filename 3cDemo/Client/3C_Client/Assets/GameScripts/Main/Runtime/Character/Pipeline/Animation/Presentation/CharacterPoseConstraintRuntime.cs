using System;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonCharacter.Pipeline.Presentation.Animancer;
using Unity.Collections;

namespace ThirdPersonCharacter.Pipeline.Animation.Presentation
{
    internal readonly struct CharacterFullBodyIkSolverOutcome
    {
        internal CharacterFullBodyIkSolverOutcome(
            ulong frameSequence,
            ulong completionIdentity,
            FixedString64Bytes rigId,
            FixedString64Bytes rigRevision,
            CharacterFullBodyIkResult result)
        {
            Produced = true;
            FrameSequence = frameSequence;
            CompletionIdentity = completionIdentity;
            RigId = rigId;
            RigRevision = rigRevision;
            Result = result;
        }

        internal bool Produced { get; }
        internal ulong FrameSequence { get; }
        internal ulong CompletionIdentity { get; }
        internal FixedString64Bytes RigId { get; }
        internal FixedString64Bytes RigRevision { get; }
        internal CharacterFullBodyIkResult Result { get; }

        internal bool Matches(
            ulong frameSequence,
            ulong completionIdentity,
            FixedString64Bytes rigId,
            FixedString64Bytes rigRevision) =>
            Produced &&
            FrameSequence == frameSequence &&
            CompletionIdentity == completionIdentity &&
            RigId.Equals(rigId) &&
            RigRevision.Equals(rigRevision);
    }

    internal sealed class CharacterPoseConstraintRuntime : IDisposable
    {
        sealed class Bank
        {
            internal Bank(
                int contributionCount,
                int contributionGoalCount,
                CharacterFootPlacementModule footPlacement)
            {
                SolverEffectors = new CharacterFullBodyIkEffectorDiagnostics[
                    CharacterFullBodyIkGoalSetHeader.MaximumGoalCount];
                SolverLimbs = new CharacterFullBodyIkLimbDiagnostics[4];
                Goals = new NativeArray<CharacterFullBodyIkGoal>(
                    CharacterFullBodyIkGoalSetHeader.MaximumGoalCount,
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

            internal CharacterFullBodyIkSolverOutcome SolverOutcome;
            internal CharacterFullBodyIkBendHistory BendHistory;
            internal CharacterFullBodyIkSolverDiagnostics SolverDiagnostics;
            internal int SolverEffectorCount;
            internal readonly CharacterFullBodyIkEffectorDiagnostics[] SolverEffectors;
            internal int SolverLimbCount;
            internal readonly CharacterFullBodyIkLimbDiagnostics[] SolverLimbs;
            internal CharacterFullBodyIkGoalSetHeader GoalSet;
            internal NativeArray<CharacterFullBodyIkGoal> Goals;
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
                SolverOutcome = default;
                SolverDiagnostics = default;
                SolverEffectorCount = 0;
                Array.Clear(SolverEffectors, 0, SolverEffectors.Length);
                SolverLimbCount = 0;
                Array.Clear(SolverLimbs, 0, SolverLimbs.Length);
                GoalSet = default;
                for (int i = 0; i < Goals.Length; i++)
                    Goals[i] = default;
                for (int i = 0; i < GoalContributions.Length; i++)
                    GoalContributions[i] = default;
                for (int i = 0; i < ContributionGoals.Length; i++)
                    ContributionGoals[i] = default;
                if (committed == null)
                    BendHistory = default;
                else
                    BendHistory = committed.BendHistory;
                FootPlacement?.Begin(
                    committed?.FootPlacement,
                    RequiresFootDiagnostics(diagnosticsInterest));
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
                SolverOutcome = default;
                SolverDiagnostics = default;
                SolverEffectorCount = 0;
                Array.Clear(SolverEffectors, 0, SolverEffectors.Length);
                SolverLimbCount = 0;
                Array.Clear(SolverLimbs, 0, SolverLimbs.Length);
                GoalSet = default;
                for (int i = 0; i < Goals.Length; i++)
                    Goals[i] = default;
                for (int i = 0; i < GoalContributions.Length; i++)
                    GoalContributions[i] = default;
                for (int i = 0; i < ContributionGoals.Length; i++)
                    ContributionGoals[i] = default;
                BendHistory = default;
                FootPlacement?.ClearPending();
            }

            internal void Dispose()
            {
                if (ContributionGoals.IsCreated)
                    ContributionGoals.Dispose();
                if (GoalContributions.IsCreated)
                    GoalContributions.Dispose();
                if (Goals.IsCreated)
                    Goals.Dispose();
            }
        }

        readonly CharacterFootPlacementModule m_FootPlacement;
        readonly CharacterFinalIkFullBodySolver m_Solver;
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
            CharacterFinalIkFullBodySolver solver,
            AnimationFinalPosePhysicalWriter finalWriter,
            int contributionCount,
            int contributionGoalCount,
            string rigId,
            string rigRevision)
        {
            m_FootPlacement = footPlacement;
            m_Solver = solver ?? throw new ArgumentNullException(nameof(solver));
            m_GoalAssembler = new CharacterFullBodyIkGoalAssembler();
            m_FinalWriter = finalWriter ?? throw new ArgumentNullException(nameof(finalWriter));
            m_RigId = new FixedString64Bytes(rigId ?? string.Empty);
            m_RigRevision = new FixedString64Bytes(rigRevision ?? string.Empty);
            if (m_RigId.Length == 0 || m_RigRevision.Length == 0)
                throw new ArgumentException("Pose Constraint Rig lineage is invalid.");
            if (contributionCount < 0 || contributionGoalCount < 0)
                throw new ArgumentOutOfRangeException(nameof(contributionCount));
            m_First = new Bank(
                contributionCount,
                contributionGoalCount,
                m_FootPlacement);
            m_Second = new Bank(
                contributionCount,
                contributionGoalCount,
                m_FootPlacement);
        }

        internal bool HasFootPlacement => m_FootPlacement != null;
        internal bool IsFullBodyIkPrepared => m_Solver.IsPrepared;
        internal int FullBodyIkGoalContributionCount =>
            m_First.GoalContributions.Length;
        internal int FullBodyIkContributionGoalCount =>
            m_First.ContributionGoals.Length;
        internal CharacterFullBodyIkSolverDiagnostics GetSolverDiagnostics() =>
            m_HasCommitted
                ? m_Committed.SolverDiagnostics
                : default;
        internal int GetSolverEffectorCount() =>
            m_HasCommitted
                ? m_Committed.SolverEffectorCount
                : 0;
        internal CharacterFullBodyIkEffectorDiagnostics GetSolverEffector(
            int effectorIndex) => m_Committed.SolverEffectors[effectorIndex];
        internal int GetSolverLimbCount() =>
            m_HasCommitted
                ? m_Committed.SolverLimbCount
                : 0;
        internal CharacterFullBodyIkLimbDiagnostics GetSolverLimb(
            int limbIndex) => m_Committed.SolverLimbs[limbIndex];
        internal CharacterFullBodyIkGoalSetHeader GetCommittedAssembledGoalSet() =>
            m_HasCommitted
                ? m_Committed.GoalSet
                : default;
        internal CharacterFullBodyIkGoal GetCommittedAssembledGoal(
            int goalIndex)
        {
            CharacterFullBodyIkGoalSetHeader header =
                GetCommittedAssembledGoalSet();
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
            m_HasPending && m_Pending.GoalSet.IsValid;
        internal ulong CommittedBankIdentity => m_HasCommitted ? m_Committed.Identity : 0;
        internal ulong CommittedRenderFrame =>
            m_HasCommitted ? m_Committed.RenderFrame : 0;
        internal bool HasCommittedFootDiagnostics =>
            m_HasCommitted &&
            m_Committed.FootPlacement?.Diagnostics.HasValue == true;
        internal CharacterFootPlacementDiagnosticsPage CommittedFootDiagnostics =>
            HasCommittedFootDiagnostics
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
                m_FootPlacement.EvaluateFrame(
                    in frame,
                    m_HasCommitted ? m_Committed.FootPlacement : null,
                    m_Pending.FootPlacement);
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
            if (m_Pending.GoalSet.IsValid)
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
                m_Pending.Goals,
                out m_Pending.GoalSet);
            if (!result.Succeeded)
            {
                goalSet = default;
                return result;
            }
            goalSet = m_Pending.GoalSet;
            return result;
        }

        internal CharacterFullBodyIkResult SolveFullBodyIk(
            NativeSlice<AnimationLocalBonePose> pendingOutputComponentPose,
            int producerOperationIndex,
            int producerCallSiteIndex,
            ulong frameSequence,
            ulong completionIdentity)
        {
            RequireRenderFrame(frameSequence, completionIdentity);
            bool recordDiagnostics =
                RequiresFullBodyIkDiagnostics(m_Pending.DiagnosticsInterest);
            if (!m_Pending.GoalSet.IsValid)
                throw new InvalidOperationException("Full Body IK requires the unique assembled Goal Set.");
            CharacterFullBodyIkResult result = m_Solver.SolvePrepared(
                pendingOutputComponentPose,
                in m_Pending.GoalSet,
                m_Pending.Goals,
                ref m_Pending.BendHistory,
                frameSequence,
                completionIdentity,
                recordDiagnostics);
            m_Pending.SolverOutcome = new CharacterFullBodyIkSolverOutcome(
                frameSequence,
                completionIdentity,
                m_RigId,
                m_RigRevision,
                result);
            if (recordDiagnostics)
            {
                int effectorCount = m_Solver.DiagnosticEffectorCount;
                int limbCount = m_Solver.DiagnosticLimbCount;
                if (effectorCount < 0 ||
                    effectorCount > CharacterFullBodyIkGoalSetHeader.MaximumGoalCount ||
                    limbCount < 0 || limbCount > 4)
                {
                    throw new InvalidOperationException(
                        "Full Body IK diagnostics exceeded the root Bank capacity.");
                }
                m_Pending.SolverDiagnostics = m_Solver.Diagnostics;
                m_Pending.SolverEffectorCount = effectorCount;
                for (int i = 0; i < effectorCount; i++)
                    m_Pending.SolverEffectors[i] = m_Solver.GetDiagnosticEffector(i);
                m_Pending.SolverLimbCount = limbCount;
                for (int i = 0; i < limbCount; i++)
                    m_Pending.SolverLimbs[i] = m_Solver.GetDiagnosticLimb(i);
            }
            return result;
        }

        internal string ApplyTuning(
            CharacterPoseTuningLayout layout,
            CharacterPoseTuningParameterBlock block,
            bool resetOwnerState)
        {
            RequireAlive();
            string solverError = m_Solver.ApplyTuning(
                layout,
                block,
                resetOwnerState);
            if (!string.IsNullOrEmpty(solverError))
                return solverError;
            if (resetOwnerState)
                ClearBendHistories();
            return m_FootPlacement?.ApplyTuning(layout, block, resetOwnerState) ?? string.Empty;
        }

        internal bool TryGetFullBodyIkFailure(
            ulong completionIdentity,
            out CharacterFullBodyIkResult result)
        {
            RequireAlive();
            result = default;
            Bank bank = m_HasPending &&
                        m_Pending.CompletionIdentity == completionIdentity
                ? m_Pending
                : m_HasCommitted &&
                  m_Committed.CompletionIdentity == completionIdentity
                    ? m_Committed
                    : null;
            if (bank == null)
                return false;
            CharacterFullBodyIkSolverOutcome outcome = bank.SolverOutcome;
            if (!outcome.Produced || outcome.CompletionIdentity != completionIdentity)
                return false;
            result = outcome.Result;
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
            if (!m_Pending.GoalSet.IsValid ||
                m_Pending.GoalSet.FrameSequence != renderFrame ||
                m_Pending.GoalSet.CompletionIdentity != completionIdentity ||
                !m_Pending.GoalSet.RigId.Equals(m_RigId) ||
                !m_Pending.GoalSet.RigRevision.Equals(m_RigRevision))
            {
                throw new InvalidOperationException(
                    "Pose Constraint Goal Set is incomplete before the Physical Writer.");
            }
            if (!m_Pending.SolverOutcome.Matches(
                    renderFrame,
                    completionIdentity,
                    m_RigId,
                    m_RigRevision) ||
                !m_Pending.SolverOutcome.Result.Succeeded)
            {
                throw new InvalidOperationException(
                    "Pose Constraint Solver Outcome is invalid before the Physical Writer.");
            }
            m_FootPlacement?.ValidateFrame(
                m_Pending.FootPlacement,
                renderFrame,
                completionIdentity);
        }

        internal void SealFrame()
        {
            if (m_Pending.FootPlacement != null)
                m_Pending.FootPlacement.HasFrame = false;
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
            m_FootPlacement?.ReleasePendingPages(
                m_HasCommitted ? m_Committed.FootPlacement : null,
                m_Pending.FootPlacement);
            m_Pending.ClearPending();
            m_Pending = null;
            m_HasPending = false;
        }

        internal void ResetSolvers()
        {
            RequireAlive();
            m_Solver.Reset();
            ClearBendHistories();
        }

        internal void ResetFootPlacement(in CharacterFootPlacementReset reset)
        {
            RequireAlive();
            if (m_FootPlacement == null)
                return;
            if (m_HasPending)
                DiscardFrame();
            m_First.FootPlacement.Reset(
                CharacterFootCorrectionResponseInitializationReason
                    .FootPlacementReset);
            m_Second.FootPlacement.Reset(
                CharacterFootCorrectionResponseInitializationReason
                    .FootPlacementReset);
            m_FootPlacement.ResetShared(in reset);
        }

        internal void RetargetFootPlacement(ulong resetSequence)
        {
            RequireAlive();
            if (m_FootPlacement == null)
                return;
            if (m_HasPending)
                DiscardFrame();
            m_First.FootPlacement.Reset(
                CharacterFootCorrectionResponseInitializationReason.Retarget);
            m_Second.FootPlacement.Reset(
                CharacterFootCorrectionResponseInitializationReason.Retarget);
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
            bank.BendHistory = default;
            bank.SolverOutcome = default;
            bank.SolverDiagnostics = default;
            bank.SolverEffectorCount = 0;
            Array.Clear(bank.SolverEffectors, 0, bank.SolverEffectors.Length);
            bank.SolverLimbCount = 0;
            Array.Clear(bank.SolverLimbs, 0, bank.SolverLimbs.Length);
        }

        internal static bool RequiresFootDiagnostics(
            AnimationPresentationDiagnosticsInterest interest) =>
            (interest &
             (AnimationPresentationDiagnosticsInterest.LiveState |
              AnimationPresentationDiagnosticsInterest.Capture |
              AnimationPresentationDiagnosticsInterest.PoseWatch)) != 0;

        internal static bool RequiresFullBodyIkDiagnostics(
            AnimationPresentationDiagnosticsInterest interest) =>
            (interest &
             (AnimationPresentationDiagnosticsInterest.LiveState |
              AnimationPresentationDiagnosticsInterest.Capture |
              AnimationPresentationDiagnosticsInterest.OperationDetail |
              AnimationPresentationDiagnosticsInterest.PoseWatch)) != 0;

        internal static bool RequiresPhysicalDiagnostics(
            AnimationPresentationDiagnosticsInterest interest) =>
            (interest &
             (AnimationPresentationDiagnosticsInterest.LiveState |
              AnimationPresentationDiagnosticsInterest.Capture |
              AnimationPresentationDiagnosticsInterest.FinalPoseDetail |
              AnimationPresentationDiagnosticsInterest.PoseWatch)) != 0;

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
            {
                m_FootPlacement?.ReleasePendingPages(
                    m_HasCommitted ? m_Committed.FootPlacement : null,
                    m_Pending.FootPlacement);
            }
            m_FootPlacement?.Dispose();
            m_Solver.Reset();
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
