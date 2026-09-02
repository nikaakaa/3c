using System;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Presentation;
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

    internal readonly struct CharacterPoseConstraintCommittedDiagnosticsView
    {
        internal CharacterPoseConstraintCommittedDiagnosticsView(
            CharacterPoseConstraintRuntime.CommittedDiagnosticsPage page)
        {
            m_Page = page ?? throw new ArgumentNullException(nameof(page));
            m_Identity = page.Identity;
            if (!IsValid)
                throw new ArgumentException(
                    "Pose Constraint committed diagnostics are invalid.",
                    nameof(page));
        }

        readonly CharacterPoseConstraintRuntime.CommittedDiagnosticsPage m_Page;
        readonly ulong m_Identity;
        internal bool IsValid =>
            m_Page != null &&
            m_Identity != 0 &&
            m_Page.Identity == m_Identity &&
            m_Page.Result.IsCompleted;
        internal CharacterPoseConstraintResult Result
        {
            get
            {
                RequireValid();
                return m_Page.Result;
            }
        }
        internal CharacterFootLandingPredictionDiagnostics FootLandingPrediction
        {
            get
            {
                RequireValid();
                return m_Page.FootLandingPrediction;
            }
        }
        internal int FullBodyIkGoalContributionCount
        {
            get
            {
                RequireValid();
                return m_Page.GoalContributionCount;
            }
        }
        internal int FullBodyIkContributionGoalCount
        {
            get
            {
                RequireValid();
                return m_Page.ContributionGoalCount;
            }
        }
        internal CharacterFullBodyIkGoalContributionHeader
            GetGoalContribution(int index)
        {
            RequireValid();
            if ((uint)index >= (uint)m_Page.GoalContributionCount)
                throw new ArgumentOutOfRangeException(nameof(index));
            return m_Page.GoalContributions[index];
        }
        internal CharacterFullBodyIkGoal GetContributionGoal(int index)
        {
            RequireValid();
            if ((uint)index >= (uint)m_Page.ContributionGoalCount)
                throw new ArgumentOutOfRangeException(nameof(index));
            return m_Page.ContributionGoals[index];
        }
        internal CharacterFullBodyIkGoalSetHeader GoalSet
        {
            get
            {
                RequireValid();
                return m_Page.GoalSet;
            }
        }
        internal CharacterFullBodyIkGoal GetGoal(int index)
        {
            RequireValid();
            if ((uint)index >= (uint)m_Page.GoalCount)
                throw new ArgumentOutOfRangeException(nameof(index));
            return m_Page.Goals[index];
        }
        internal CharacterFullBodyIkSolverDiagnostics Solver
        {
            get
            {
                RequireValid();
                return m_Page.Solver;
            }
        }
        internal int SolverEffectorCount
        {
            get
            {
                RequireValid();
                return m_Page.SolverEffectorCount;
            }
        }
        internal CharacterFullBodyIkEffectorDiagnostics GetSolverEffector(
            int index)
        {
            RequireValid();
            if ((uint)index >= (uint)m_Page.SolverEffectorCount)
                throw new ArgumentOutOfRangeException(nameof(index));
            return m_Page.SolverEffectors[index];
        }
        internal int SolverLimbCount
        {
            get
            {
                RequireValid();
                return m_Page.SolverLimbCount;
            }
        }
        internal CharacterFullBodyIkLimbDiagnostics GetSolverLimb(int index)
        {
            RequireValid();
            if ((uint)index >= (uint)m_Page.SolverLimbCount)
                throw new ArgumentOutOfRangeException(nameof(index));
            return m_Page.SolverLimbs[index];
        }

        void RequireValid()
        {
            if (!IsValid)
            {
                throw new InvalidOperationException(
                    "Pose Constraint committed diagnostics lease is stale.");
            }
        }
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
            internal CharacterPoseConstraintFrameLease Lease;
            internal AnimationPresentationDiagnosticsInterest DiagnosticsInterest;
            internal CharacterFootIkCaptureInterest FootIkCaptureInterest;

            internal void Begin(
                in CharacterPoseConstraintFrameLease lease,
                AnimationPresentationDiagnosticsInterest diagnosticsInterest,
                CharacterFootIkCaptureInterest footIkCaptureInterest,
                Bank committed)
            {
                Lease = lease;
                DiagnosticsInterest = diagnosticsInterest;
                FootIkCaptureInterest = footIkCaptureInterest;
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
                    RequiresFootDiagnostics(diagnosticsInterest) ||
                    footIkCaptureInterest.IsEnabled);
            }

            internal void ClearPending()
            {
                Lease = default;
                DiagnosticsInterest = AnimationPresentationDiagnosticsInterest.None;
                FootIkCaptureInterest = default;
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

        internal sealed class CommittedDiagnosticsPage
        {
            internal CommittedDiagnosticsPage(
                int contributionCount,
                int contributionGoalCount)
            {
                GoalContributions =
                    new CharacterFullBodyIkGoalContributionHeader[
                        contributionCount];
                ContributionGoals = new CharacterFullBodyIkGoal[
                    contributionGoalCount];
                Goals = new CharacterFullBodyIkGoal[
                    CharacterFullBodyIkGoalSetHeader.MaximumGoalCount];
                SolverEffectors =
                    new CharacterFullBodyIkEffectorDiagnostics[
                        CharacterFullBodyIkGoalSetHeader.MaximumGoalCount];
                SolverLimbs = new CharacterFullBodyIkLimbDiagnostics[4];
            }

            internal ulong Identity;
            internal CharacterPoseConstraintResult Result;
            internal CharacterFootLandingPredictionDiagnostics
                FootLandingPrediction;
            internal CharacterFullBodyIkGoalContributionHeader[]
                GoalContributions;
            internal CharacterFullBodyIkGoal[] ContributionGoals;
            internal int GoalContributionCount;
            internal int ContributionGoalCount;
            internal CharacterFullBodyIkGoalSetHeader GoalSet;
            internal CharacterFullBodyIkGoal[] Goals;
            internal int GoalCount;
            internal CharacterFullBodyIkSolverDiagnostics Solver;
            internal CharacterFullBodyIkEffectorDiagnostics[] SolverEffectors;
            internal int SolverEffectorCount;
            internal CharacterFullBodyIkLimbDiagnostics[] SolverLimbs;
            internal int SolverLimbCount;
        }

        readonly CharacterFootPlacementModule m_FootPlacement;
        readonly CharacterPoseBoneContributionCatalog m_PoseBoneContributions;
        readonly CharacterFullBodyIkGoalAssemblerCatalog m_GoalAssemblers;
        readonly CharacterFinalIkFullBodySolver m_Solver;
        readonly CharacterFullBodyIkGoalAssembler m_GoalAssembler;
        readonly FixedString64Bytes m_RigId;
        readonly FixedString64Bytes m_RigRevision;
        readonly Bank m_First;
        readonly Bank m_Second;
        readonly CommittedDiagnosticsPage m_CommittedDiagnostics;

        Bank m_Committed;
        Bank m_Pending;
        CharacterPoseConstraintResult m_CommittedResult;
        CharacterPoseConstraintResult m_PendingResult;
        ulong m_NextBankIdentity = 1;
        ulong m_NextDiagnosticsIdentity = 1;
        ulong m_TuningGeneration = 1;
        ulong m_CandidateTuningGeneration;
        bool m_CandidateResetOwnerState;
        bool m_HasTuningCandidate;
        bool m_HasCommitted;
        bool m_HasPending;
        bool m_Disposed;

        internal CharacterPoseConstraintRuntime(
            CharacterFootPlacementModule footPlacement,
            CharacterPoseBoneContributionCatalog poseBoneContributions,
            CharacterFullBodyIkGoalAssemblerCatalog goalAssemblers,
            CharacterFinalIkFullBodySolver solver,
            int contributionCount,
            int contributionGoalCount,
            string rigId,
            string rigRevision)
        {
            m_FootPlacement = footPlacement;
            m_PoseBoneContributions = poseBoneContributions;
            m_GoalAssemblers = goalAssemblers;
            m_Solver = solver ?? throw new ArgumentNullException(nameof(solver));
            m_GoalAssembler = new CharacterFullBodyIkGoalAssembler();
            m_RigId = new FixedString64Bytes(rigId ?? string.Empty);
            m_RigRevision = new FixedString64Bytes(rigRevision ?? string.Empty);
            if (m_RigId.Length == 0 || m_RigRevision.Length == 0)
                throw new ArgumentException("Pose Constraint Rig lineage is invalid.");
            if (!m_PoseBoneContributions.IsValid)
                throw new ArgumentException(
                    "Pose Bone Contribution catalog is invalid.",
                    nameof(poseBoneContributions));
            if (!m_GoalAssemblers.IsValid)
                throw new ArgumentException(
                    "Full Body IK Goal Assembler catalog is invalid.",
                    nameof(goalAssemblers));
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
            m_CommittedDiagnostics = new CommittedDiagnosticsPage(
                contributionCount,
                contributionGoalCount);
        }

        internal bool HasFootPlacement => m_FootPlacement != null;
        internal bool IsFullBodyIkPrepared => m_Solver.IsPrepared;
        internal CharacterPoseConstraintTuningView RequireTuning(
            ulong generation)
        {
            RequireAlive();
            if (generation != m_TuningGeneration)
            {
                throw new InvalidOperationException(
                    "Pose Constraint tuning generation is stale.");
            }
            return new CharacterPoseConstraintTuningView(generation);
        }
        internal bool MatchesCompiledLayout(
            int contributionCount,
            int contributionGoalCount) =>
            contributionCount >= 0 &&
            contributionGoalCount >= 0 &&
            m_First.GoalContributions.Length == contributionCount &&
            m_Second.GoalContributions.Length == contributionCount &&
            m_First.ContributionGoals.Length == contributionGoalCount &&
            m_Second.ContributionGoals.Length == contributionGoalCount;
        internal bool HasPendingFrame => m_HasPending;
        internal bool HasPendingAssembledGoalSet =>
            m_HasPending && m_Pending.GoalSet.IsValid;
        internal bool MatchesCommittedResult(
            in CharacterPoseConstraintResult result) =>
            m_HasCommitted &&
            m_CommittedResult.IsCompleted &&
            result.IsCompleted &&
            m_CommittedResult.Lineage == result.Lineage &&
            m_CommittedResult.GoalCount == result.GoalCount &&
            m_CommittedResult.SolverProduced == result.SolverProduced &&
            m_CommittedResult.FullBodyIk.AppliedGoalCount ==
            result.FullBodyIk.AppliedGoalCount;
        internal void RequireCommittedFootIkCapture(
            in CharacterPoseConstraintResult result)
        {
            RequireAlive();
            if (!MatchesCommittedResult(in result) ||
                !m_Committed.FootIkCaptureInterest.IsEnabled ||
                m_Committed.FootPlacement?.Diagnostics.HasValue != true)
            {
                throw new InvalidOperationException(
                    "Committed Foot IK capture facts are unavailable.");
            }
        }
        internal CharacterFootLandingPredictionDiagnostics
            CommittedFootLandingPrediction =>
                m_Committed.FootPlacement.Diagnostics.Value;
        internal CharacterFullBodyIkSolverDiagnostics
            CommittedFullBodyIkSolver =>
                m_Committed.SolverDiagnostics;
        internal int CommittedSolverEffectorCount =>
            m_Committed.SolverEffectorCount;
        internal CharacterFullBodyIkEffectorDiagnostics
            GetCommittedSolverEffector(int index) =>
                m_Committed.SolverEffectors[index];
        internal int CommittedSolverLimbCount =>
            m_Committed.SolverLimbCount;
        internal CharacterFullBodyIkLimbDiagnostics
            GetCommittedSolverLimb(int index) =>
                m_Committed.SolverLimbs[index];
        internal CharacterPoseConstraintCommittedDiagnosticsView
            CaptureCommittedDiagnostics(
            in CharacterPoseConstraintResult result,
            AnimationPresentationDiagnosticsInterest interest,
            CharacterFootIkCaptureInterest footIkCaptureInterest)
        {
            RequireAlive();
            if (interest == AnimationPresentationDiagnosticsInterest.None &&
                    !footIkCaptureInterest.IsEnabled ||
                !MatchesCommittedResult(in result) ||
                (interest & ~m_Committed.DiagnosticsInterest) != 0 ||
                footIkCaptureInterest.IsEnabled &&
                !m_Committed.FootIkCaptureInterest.IsEnabled)
            {
                throw new InvalidOperationException(
                    "Pose Constraint committed diagnostics request is invalid.");
            }
            CommittedDiagnosticsPage page = m_CommittedDiagnostics;
            page.Identity = 0;
            page.Result = result;
            page.FootLandingPrediction = default;
            page.GoalContributionCount = 0;
            page.ContributionGoalCount = 0;
            page.GoalSet = default;
            page.GoalCount = 0;
            page.Solver = default;
            page.SolverEffectorCount = 0;
            page.SolverLimbCount = 0;
            if ((RequiresFootDiagnostics(interest) ||
                 footIkCaptureInterest.IsEnabled) &&
                m_Committed.FootPlacement?.Diagnostics.HasValue == true)
            {
                page.FootLandingPrediction =
                    m_Committed.FootPlacement.Diagnostics.Value;
            }
            if ((interest &
                 AnimationPresentationDiagnosticsInterest.PoseWatch) != 0)
            {
                page.GoalContributionCount =
                    m_Committed.GoalContributions.Length;
                for (int i = 0; i < page.GoalContributionCount; i++)
                {
                    page.GoalContributions[i] =
                        m_Committed.GoalContributions[i];
                }
                page.ContributionGoalCount =
                    m_Committed.ContributionGoals.Length;
                for (int i = 0; i < page.ContributionGoalCount; i++)
                {
                    page.ContributionGoals[i] =
                        m_Committed.ContributionGoals[i];
                }
                page.GoalSet = m_Committed.GoalSet;
                page.GoalCount = page.GoalSet.IsValid
                    ? page.GoalSet.GoalCount
                    : 0;
                for (int i = 0; i < page.GoalCount; i++)
                {
                    page.Goals[i] = m_Committed.Goals[
                        page.GoalSet.GoalOffset + i];
                }
            }
            if (RequiresFullBodyIkDiagnostics(interest) ||
                footIkCaptureInterest.IsEnabled)
            {
                page.Solver = m_Committed.SolverDiagnostics;
                page.SolverEffectorCount =
                    m_Committed.SolverEffectorCount;
                for (int i = 0; i < page.SolverEffectorCount; i++)
                    page.SolverEffectors[i] = m_Committed.SolverEffectors[i];
                page.SolverLimbCount = m_Committed.SolverLimbCount;
                for (int i = 0; i < page.SolverLimbCount; i++)
                    page.SolverLimbs[i] = m_Committed.SolverLimbs[i];
            }
            page.Identity = m_NextDiagnosticsIdentity++;
            return new CharacterPoseConstraintCommittedDiagnosticsView(page);
        }
        internal CharacterPoseConstraintFrameLease BeginFrame(
            in CharacterPoseFrameLineage lineage,
            AnimationPresentationDiagnosticsInterest diagnosticsInterest,
            CharacterFootIkCaptureInterest footIkCaptureInterest)
        {
            RequireAlive();
            if (m_HasPending)
                throw new InvalidOperationException("Pose Constraint frame is already open.");
            if (lineage.TuningGeneration != m_TuningGeneration)
            {
                throw new InvalidOperationException(
                    "Pose Constraint tuning generation differs from the root frame.");
            }
            var lease = new CharacterPoseConstraintFrameLease(in lineage);
            m_Pending = m_HasCommitted && ReferenceEquals(m_Committed, m_First)
                ? m_Second
                : m_First;
            m_Pending.Begin(
                in lease,
                diagnosticsInterest,
                footIkCaptureInterest,
                m_HasCommitted ? m_Committed : null);
            m_PendingResult = default;
            m_HasPending = true;
            return lease;
        }

        internal CharacterFootPlacementConstraintOperationResult
            EvaluateFootPlacement(
                in CharacterFootPlacementConstraintHandle handle,
                in CharacterFootPlacementFrameInput frame)
        {
            RequireRenderFrame(frame.RenderFrame, frame.Pose.CompletionIdentity);
            if (m_FootPlacement == null)
                throw new InvalidOperationException("Pose Constraint Foot Placement module is unavailable.");
            if (!handle.IsValid ||
                (uint)handle.ContributionValueIndex >=
                    (uint)m_Pending.GoalContributions.Length ||
                handle.ContributionGoalOffset >
                m_Pending.ContributionGoals.Length -
                CharacterPresentationFootPlacementDescriptor.GoalCount)
            {
                throw new ArgumentOutOfRangeException(nameof(handle));
            }
            CharacterFootPlacementResult result =
                m_FootPlacement.EvaluateFrame(
                    in frame,
                    m_HasCommitted ? m_Committed.FootPlacement : null,
                    m_Pending.FootPlacement);
            int goalOffset = handle.ContributionGoalOffset;
            m_Pending.ContributionGoals[goalOffset] = result.PelvisGoal;
            m_Pending.ContributionGoals[goalOffset + 1] = result.LeftGoal;
            m_Pending.ContributionGoals[goalOffset + 2] = result.RightGoal;
            var contribution = new CharacterFullBodyIkGoalContributionHeader(
                result.FrameSequence,
                result.CompletionIdentity,
                result.RigId,
                result.RigRevision,
                handle.OperationIndex,
                handle.CallSiteIndex,
                goalOffset,
                CharacterPresentationFootPlacementDescriptor.GoalCount,
                CharacterFullBodyIkGoalContributionAvailability.Ready);
            m_Pending.GoalContributions[
                handle.ContributionValueIndex] = contribution;
            return new CharacterFootPlacementConstraintOperationResult(
                in handle,
                in contribution);
        }

        internal CharacterFootPlacementConstraintOperationResult
            RecordUnavailableFootPlacement(
                in CharacterFootPlacementConstraintHandle handle,
                ulong frameSequence,
                ulong completionIdentity)
        {
            RequireRenderFrame(
                frameSequence,
                completionIdentity);
            if (!handle.IsValid ||
                (uint)handle.ContributionValueIndex >=
                (uint)m_Pending.GoalContributions.Length ||
                handle.ContributionGoalOffset >
                m_Pending.ContributionGoals.Length)
            {
                throw new ArgumentException(
                    "Unavailable Foot Placement Constraint handle is invalid.");
            }
            var contribution =
                new CharacterFullBodyIkGoalContributionHeader(
                    frameSequence,
                    completionIdentity,
                    m_RigId,
                    m_RigRevision,
                    handle.OperationIndex,
                    handle.CallSiteIndex,
                    handle.ContributionGoalOffset,
                    0,
                    CharacterFullBodyIkGoalContributionAvailability
                        .WorldContextUnavailable);
            m_Pending.GoalContributions[
                handle.ContributionValueIndex] = contribution;
            return new CharacterFootPlacementConstraintOperationResult(
                in handle,
                in contribution);
        }

        internal CharacterPoseBoneContributionOperationResult
            ExecutePoseBoneContribution(
            in CharacterPoseBoneContributionConstraintHandle handle,
            NativeSlice<AnimationLocalBonePose> componentPose,
            ulong frameSequence,
            ulong completionIdentity)
        {
            RequireRenderFrame(frameSequence, completionIdentity);
            if (!handle.IsValid ||
                (uint)handle.ContributionValueIndex >=
                    (uint)m_Pending.GoalContributions.Length ||
                handle.ContributionGoalOffset >
                m_Pending.ContributionGoals.Length - handle.GoalCount)
            {
                throw new ArgumentOutOfRangeException(nameof(handle));
            }
            int goalOffset = handle.ContributionGoalOffset;
            NativeSlice<CharacterPoseBoneIkGoalDescriptor> descriptors =
                m_PoseBoneContributions.Resolve(in handle);
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
                    handle.OperationIndex,
                    handle.CallSiteIndex);
            m_Pending.GoalContributions[
                handle.ContributionValueIndex] = contribution;
            return new CharacterPoseBoneContributionOperationResult(
                in handle,
                in contribution);
        }

        internal CharacterFullBodyIkGoalAssemblerOperationResult
            ExecuteGoalAssembler(
            in CharacterFullBodyIkGoalAssemblerConstraintHandle handle,
            ulong frameSequence,
            ulong completionIdentity)
        {
            RequireRenderFrame(frameSequence, completionIdentity);
            if (m_Pending.GoalSet.IsValid)
                throw new InvalidOperationException("Full Body IK Goals were already assembled for this frame.");
            NativeSlice<int> contributionValueIndices =
                m_GoalAssemblers.Resolve(in handle);
            CharacterFullBodyIkResult result = m_GoalAssembler.Assemble(
                contributionValueIndices,
                m_Pending.GoalContributions,
                m_Pending.ContributionGoals,
                frameSequence,
                completionIdentity,
                m_RigId,
                m_RigRevision,
                handle.OperationIndex,
                handle.CallSiteIndex,
                m_Pending.Goals,
                out m_Pending.GoalSet);
            return new CharacterFullBodyIkGoalAssemblerOperationResult(
                in handle,
                in result,
                in m_Pending.GoalSet);
        }

        internal CharacterFullBodyIkConstraintOperationResult ExecuteFullBodyIk(
            in CharacterFullBodyIkConstraintHandle handle,
            NativeSlice<AnimationLocalBonePose> pendingOutputComponentPose,
            ulong frameSequence,
            ulong completionIdentity)
        {
            RequireRenderFrame(frameSequence, completionIdentity);
            if (!handle.IsValid || handle.FullBodyIkIndex != 0)
                throw new ArgumentOutOfRangeException(nameof(handle));
            bool recordDiagnostics =
                RequiresFullBodyIkDiagnostics(
                    m_Pending.DiagnosticsInterest) ||
                m_Pending.FootIkCaptureInterest.IsEnabled;
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
            return new CharacterFullBodyIkConstraintOperationResult(
                in handle,
                in result,
                frameSequence,
                completionIdentity);
        }

        internal string PrepareTuningCandidate(
            CharacterPoseTuningLayout layout,
            CharacterPoseTuningParameterBlock block,
            ulong generation,
            bool resetOwnerState)
        {
            RequireAlive();
            if (m_HasPending)
                return "Pose Constraint tuning cannot change during an open frame.";
            if (m_HasTuningCandidate)
                return "Pose Constraint tuning candidate is already prepared.";
            if (generation != checked(m_TuningGeneration + 1))
                return "Pose Constraint tuning generation is not consecutive.";
            string solverError = m_Solver.PrepareTuningCandidate(
                layout,
                block);
            if (!string.IsNullOrEmpty(solverError))
                return solverError;
            string footError = m_FootPlacement?.ValidateTuningCandidate(
                layout,
                block) ?? string.Empty;
            if (!string.IsNullOrEmpty(footError))
            {
                m_Solver.DiscardTuningCandidate();
                return footError;
            }
            m_CandidateTuningGeneration = generation;
            m_CandidateResetOwnerState = resetOwnerState;
            m_HasTuningCandidate = true;
            return string.Empty;
        }

        internal void CommitTuningCandidate(ulong generation)
        {
            RequireAlive();
            if (!m_HasTuningCandidate ||
                m_CandidateTuningGeneration != generation)
            {
                throw new InvalidOperationException(
                    "Pose Constraint tuning candidate is not prepared.");
            }
            bool resetOwnerState = m_CandidateResetOwnerState;
            m_Solver.CommitTuningCandidate(resetOwnerState);
            if (resetOwnerState)
                ClearBendHistories();
            m_TuningGeneration = generation;
            m_CandidateTuningGeneration = 0;
            m_CandidateResetOwnerState = false;
            m_HasTuningCandidate = false;
        }

        internal void DiscardTuningCandidate()
        {
            m_Solver.DiscardTuningCandidate();
            m_CandidateTuningGeneration = 0;
            m_CandidateResetOwnerState = false;
            m_HasTuningCandidate = false;
        }

        internal CharacterPoseConstraintResult CompleteFrame(
            CharacterPoseConstraintFrameLease lease,
            in CharacterPoseFrameLineage lineage,
            AnimationPoseAvailability programAvailability,
            AnimationPoseNativeInvalidReason outputInvalidReason,
            AnimationPoseNativeInvalidReason graphInvalidReason)
        {
            RequireAlive();
            if (!lineage.IsValid ||
                !m_HasPending ||
                m_Pending.Lease.Lineage != lease.Lineage ||
                !lease.Matches(lineage))
            {
                throw new InvalidOperationException(
                    "Pose Constraint lineage is inconsistent at completion.");
            }
            bool goalSetCompleted =
                m_Pending.GoalSet.IsValid &&
                m_Pending.GoalSet.FrameSequence ==
                    lineage.PresentationFrame &&
                m_Pending.GoalSet.CompletionIdentity ==
                    lineage.CompletionIdentity &&
                m_Pending.GoalSet.RigId.Equals(m_RigId) &&
                m_Pending.GoalSet.RigRevision.Equals(m_RigRevision);
            bool solverProduced = m_Pending.SolverOutcome.Matches(
                lineage.PresentationFrame,
                lineage.CompletionIdentity,
                m_RigId,
                m_RigRevision);
            CharacterFullBodyIkResult solverResult = solverProduced
                ? m_Pending.SolverOutcome.Result
                : default;
            if (goalSetCompleted &&
                solverProduced &&
                solverResult.Succeeded)
            {
                m_FootPlacement?.ValidateFrame(
                    m_Pending.FootPlacement,
                    lineage.PresentationFrame,
                    lineage.CompletionIdentity);
                return StorePendingResult(
                    new CharacterPoseConstraintResult(
                        in lineage,
                        AnimationPresentationFrameOutcome.Committed,
                        AnimationPoseAvailability.Pose,
                        AnimationPoseNativeInvalidReason.None,
                        m_Pending.GoalSet.GoalCount,
                        true,
                        in solverResult));
            }
            if (solverProduced && solverResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Pose Constraint Goal Set is incomplete at completion.");
            }
            AnimationPoseNativeInvalidReason invalidReason =
                solverProduced
                    ? AnimationPoseNativeInvalidReason.FullBodyIkSolverInvalid
                    : graphInvalidReason != AnimationPoseNativeInvalidReason.None
                        ? graphInvalidReason
                        : outputInvalidReason !=
                          AnimationPoseNativeInvalidReason.None
                            ? outputInvalidReason
                            : AnimationPoseNativeInvalidReason.PoseConstraintInvalid;
            return StorePendingResult(
                new CharacterPoseConstraintResult(
                    in lineage,
                    AnimationPresentationFrameOutcome.TypedInvalid,
                    programAvailability == AnimationPoseAvailability.NoPose &&
                    !solverProduced
                        ? AnimationPoseAvailability.NoPose
                        : AnimationPoseAvailability.Invalid,
                    invalidReason,
                    goalSetCompleted ? m_Pending.GoalSet.GoalCount : -1,
                    solverProduced,
                    in solverResult));
        }

        CharacterPoseConstraintResult StorePendingResult(
            in CharacterPoseConstraintResult result)
        {
            if (!result.IsValid || m_PendingResult.IsValid)
            {
                throw new InvalidOperationException(
                    "Pose Constraint result was already completed.");
            }
            m_PendingResult = result;
            return result;
        }

        internal void SealFrame(
            CharacterPoseConstraintFrameLease lease)
        {
            RequirePendingLease(lease);
            if (!m_PendingResult.IsCompleted ||
                !lease.Matches(m_PendingResult.Lineage))
            {
                throw new InvalidOperationException(
                    "Pose Constraint result is incomplete at seal.");
            }
            if (m_Pending.FootPlacement != null)
                m_Pending.FootPlacement.IsPendingFrameOpen = false;
            m_Pending.Identity = m_NextBankIdentity++;
            m_Committed = m_Pending;
            m_CommittedResult = m_PendingResult;
            m_HasCommitted = true;
            m_Pending = null;
            m_PendingResult = default;
            m_HasPending = false;
            m_FootPlacement?.PublishCommittedDiagnostics(m_Committed.FootPlacement);
        }

        internal void DiscardFrame(
            CharacterPoseConstraintFrameLease lease)
        {
            RequireAlive();
            RequirePendingLease(lease);
            DiscardPending();
        }

        void DiscardPending()
        {
            m_FootPlacement?.ReleasePendingPages(
                m_HasCommitted ? m_Committed.FootPlacement : null,
                m_Pending.FootPlacement);
            m_Pending.ClearPending();
            m_Pending = null;
            m_PendingResult = default;
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
                DiscardPending();
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
                DiscardPending();
            m_First.FootPlacement.Reset(
                CharacterFootCorrectionResponseInitializationReason.Retarget);
            m_Second.FootPlacement.Reset(
                CharacterFootCorrectionResponseInitializationReason.Retarget);
            m_FootPlacement.RetargetShared(resetSequence);
        }

        void RequireRenderFrame(ulong renderFrame, ulong completionIdentity)
        {
            RequireAlive();
            if (!m_HasPending ||
                m_Pending.Lease.PresentationFrame != renderFrame ||
                completionIdentity == 0)
                throw new InvalidOperationException("Pose Constraint pending frame identity is inconsistent.");
        }

        void RequirePendingLease(
            CharacterPoseConstraintFrameLease lease)
        {
            RequireAlive();
            if (!m_HasPending ||
                !lease.IsValid ||
                !m_Pending.Lease.IsValid ||
                m_Pending.Lease.Lineage != lease.Lineage)
            {
                throw new InvalidOperationException(
                    "Pose Constraint Pending lease is stale.");
            }
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
            DiscardTuningCandidate();
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
