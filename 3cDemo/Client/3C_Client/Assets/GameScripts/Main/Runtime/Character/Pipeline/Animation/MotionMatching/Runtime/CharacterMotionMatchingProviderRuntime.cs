using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using BTSMTL.Diagnostics;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation.MotionMatching
{
    internal enum MotionMatchingHistoryCompletionOutcome : byte
    {
        Skipped = 0,
        Appended = 1,
        Gap = 2
    }

    internal sealed class CharacterMotionMatchingProviderRuntime : IDisposable
    {
        readonly struct FramePage
        {
            internal FramePage(
                MotionMatchingSelectionDecision currentDecision,
                MotionMatchingQuery lastWinnerQuery,
                int currentDatabaseIndex,
                ulong resetSequence,
                ulong lastResolvedFrame,
                ulong lastHistoryFrame,
                float presentationTime,
                ulong pendingResetPreviousSequence,
                bool hasPendingReset)
            {
                CurrentDecision = currentDecision;
                LastWinnerQuery = lastWinnerQuery;
                CurrentDatabaseIndex = currentDatabaseIndex;
                ResetSequence = resetSequence;
                LastResolvedFrame = lastResolvedFrame;
                LastHistoryFrame = lastHistoryFrame;
                PresentationTime = presentationTime;
                PendingResetPreviousSequence =
                    pendingResetPreviousSequence;
                HasPendingReset = hasPendingReset;
            }

            internal MotionMatchingSelectionDecision CurrentDecision { get; }
            internal MotionMatchingQuery LastWinnerQuery { get; }
            internal int CurrentDatabaseIndex { get; }
            internal ulong ResetSequence { get; }
            internal ulong LastResolvedFrame { get; }
            internal ulong LastHistoryFrame { get; }
            internal float PresentationTime { get; }
            internal ulong PendingResetPreviousSequence { get; }
            internal bool HasPendingReset { get; }
        }

        readonly MotionMatchingProjectionPayload m_Projection;
        readonly string m_ProjectionIdentity;
        readonly MotionMatchingNodeBindingPayload m_Binding;
        readonly string m_ProviderId;
        readonly PresentationPoseSourceIndex m_PresentationPoseSourceIndex;
        readonly DatabaseRuntime[] m_Databases;
        readonly CharacterMotionMatchingTrajectoryRuntime m_Trajectory;
        readonly MotionMatchingTrajectoryEnvelope m_Envelope;
        readonly CharacterMotionMatchingPoseHistory m_History;
        readonly int[] m_FeatureBoneIndices;
        readonly Vector3[] m_FeatureBonePositions;
        MotionMatchingRuntimeSnapshot m_RuntimeSnapshot;

        MotionMatchingSelectionDecision m_CurrentDecision;
        MotionMatchingQuery m_LastWinnerQuery;
        int m_CurrentDatabaseIndex = -1;
        ulong m_QuerySequence;
        ulong m_SelectionGenerationSequence;
        ulong m_ResetSequence;
        ulong m_LastResolvedFrame;
        ulong m_LastHistoryFrame;
        float m_PresentationTime;
        ulong m_PendingResetPreviousSequence;
        bool m_HasPendingReset;
        ulong m_PreparedHistoryFrame;
        bool m_HistoryCompletionPrepared;
        bool m_PreparedHistoryWrites;
        FramePage m_CommittedPage;
        bool m_FrameOpen;
        bool m_Disposed;

        public CharacterMotionMatchingProviderRuntime(
            string projectionIdentity,
            MotionMatchingProjectionPayload projection,
            MotionMatchingNodeBindingPayload binding,
            string providerId,
            PresentationPoseSourceIndex presentationPoseSourceIndex,
            CharacterAnimationRigPayload rig)
        {
            m_ProjectionIdentity = MotionMatchingIdentity.Require(projectionIdentity, nameof(projectionIdentity));
            m_Projection = projection ?? throw new ArgumentNullException(nameof(projection));
            m_Binding = binding;
            m_ProviderId = MotionMatchingIdentity.Require(providerId, nameof(providerId));
            m_PresentationPoseSourceIndex = presentationPoseSourceIndex;
            if (!m_PresentationPoseSourceIndex.IsValid ||
                !binding.BindingId.IsValid ||
                binding.BindingRevision <= 0 ||
                !binding.PoseNodeId.IsValid ||
                !binding.SearchDomainId.IsValid || binding.DatabaseCount <= 0 || rig == null)
                throw new ArgumentException("Motion Matching provider runtime binding is invalid.");
            rig.RequireValid();
            m_Databases = new DatabaseRuntime[binding.DatabaseCount];
            try
            {
                for (int i = 0; i < m_Databases.Length; i++)
                {
                    int projectionDatabaseIndex = binding.FirstDatabaseIndex + i;
                    MotionMatchingDatabasePayload payload = projection.GetDatabase(projectionDatabaseIndex);
                    if (payload == null || !payload.SearchDomainId.Equals(binding.SearchDomainId))
                        throw new InvalidOperationException("Motion Matching provider Database range is invalid.");
                    var database = new CharacterMotionMatchingRuntimeDatabase(projection, projectionDatabaseIndex);
                    m_Databases[i] = new DatabaseRuntime(database);
                }
            }
            catch
            {
                DisposeDatabases();
                throw;
            }
            m_Trajectory = new CharacterMotionMatchingTrajectoryRuntime(projection.TrajectoryPolicy);
            m_Envelope = new MotionMatchingTrajectoryEnvelope(projection.TrajectoryPolicy.PointCount);
            m_History = new CharacterMotionMatchingPoseHistory(
                projection.FeatureSchema.BoneCount,
                projection.SearchPolicy.HistoryCapacity);
            m_FeatureBoneIndices = new int[projection.FeatureSchema.BoneCount];
            m_FeatureBonePositions = new Vector3[projection.FeatureSchema.BoneCount];
            for (int featureBoneIndex = 0; featureBoneIndex < m_FeatureBoneIndices.Length; featureBoneIndex++)
            {
                var boneId = new AnimationBoneId(projection.FeatureSchema.GetBoneId(featureBoneIndex));
                m_FeatureBoneIndices[featureBoneIndex] = RequireRigBoneIndex(rig, boneId);
            }
            m_CommittedPage = ReadPage();
        }

        public string ProviderId => m_ProviderId;
        public PresentationPoseSourceIndex PresentationPoseSourceIndex =>
            m_PresentationPoseSourceIndex;
        public PoseNodeId PoseNodeId => m_Binding.PoseNodeId;
        public int FeatureBoneCount => m_FeatureBoneIndices.Length;
        public MotionMatchingSelectionDecision CurrentDecision => m_CurrentDecision;
        public MotionMatchingQuery LastWinnerQuery => m_LastWinnerQuery;
        public CharacterMotionMatchingPoseHistory History => m_History;
        public CharacterMotionMatchingDatabaseArtifactIdentity CurrentDatabaseIdentity =>
            m_CurrentDatabaseIndex >= 0 ? m_Databases[m_CurrentDatabaseIndex].Database.ArtifactIdentity : null;
        public int GetFeatureRigBoneIndex(int index) => m_FeatureBoneIndices[index];
        internal int[] FeatureRigBoneIndices => m_FeatureBoneIndices;
        public Vector3[] FeatureBonePositionWorkspace => m_FeatureBonePositions;

        FramePage ReadPage() =>
            new FramePage(
                m_CurrentDecision,
                m_LastWinnerQuery,
                m_CurrentDatabaseIndex,
                m_ResetSequence,
                m_LastResolvedFrame,
                m_LastHistoryFrame,
                m_PresentationTime,
                m_PendingResetPreviousSequence,
                m_HasPendingReset);

        void LoadPage(in FramePage page)
        {
            m_CurrentDecision = page.CurrentDecision;
            m_LastWinnerQuery = page.LastWinnerQuery;
            m_CurrentDatabaseIndex = page.CurrentDatabaseIndex;
            m_ResetSequence = page.ResetSequence;
            m_LastResolvedFrame = page.LastResolvedFrame;
            m_LastHistoryFrame = page.LastHistoryFrame;
            m_PresentationTime = page.PresentationTime;
            m_PendingResetPreviousSequence =
                page.PendingResetPreviousSequence;
            m_HasPendingReset = page.HasPendingReset;
        }

        void ClearPreparedHistoryCompletion()
        {
            m_PreparedHistoryFrame = 0;
            m_HistoryCompletionPrepared = false;
            m_PreparedHistoryWrites = false;
        }

        internal void BeginFrame()
        {
            RequireAlive();
            if (m_FrameOpen)
                throw new InvalidOperationException("Motion Matching provider frame is already open.");
            LoadPage(in m_CommittedPage);
            ClearPreparedHistoryCompletion();
            bool trajectoryBegun = false;
            bool envelopeBegun = false;
            bool historyBegun = false;
            int begunSelections = 0;
            try
            {
                m_Trajectory.BeginFrame();
                trajectoryBegun = true;
                m_Envelope.BeginFrame();
                envelopeBegun = true;
                m_History.BeginFrame();
                historyBegun = true;
                for (; begunSelections < m_Databases.Length; begunSelections++)
                    m_Databases[begunSelections].Selection.BeginFrame();
                m_FrameOpen = true;
            }
            catch
            {
                for (int i = begunSelections - 1; i >= 0; i--)
                    m_Databases[i].Selection.DiscardFrame();
                if (historyBegun)
                    m_History.DiscardFrame();
                if (envelopeBegun)
                    m_Envelope.DiscardFrame();
                if (trajectoryBegun)
                    m_Trajectory.DiscardFrame();
                throw;
            }
        }

        internal void CommitFrame()
        {
            RequireOpenFrame();
            for (int i = 0; i < m_Databases.Length; i++)
                m_Databases[i].Selection.CommitFrame();
            m_History.CommitFrame();
            m_Envelope.CommitFrame();
            m_Trajectory.CommitFrame();
            m_CommittedPage = ReadPage();
            ClearPreparedHistoryCompletion();
            m_FrameOpen = false;
        }

        internal void DiscardFrame()
        {
            RequireOpenFrame();
            for (int i = m_Databases.Length - 1; i >= 0; i--)
                m_Databases[i].Selection.DiscardFrame();
            m_History.DiscardFrame();
            m_Envelope.DiscardFrame();
            m_Trajectory.DiscardFrame();
            LoadPage(in m_CommittedPage);
            ClearPreparedHistoryCompletion();
            m_FrameOpen = false;
        }

        public MotionMatchingPoseSourceOutput Resolve(
            ulong presentationFrame,
            float presentationDeltaSeconds,
            MotionMatchingTrajectorySourceFrame trajectorySource,
            RuntimeDiagnosticsContext diagnostics)
        {
            RequireAlive();
            RequireOpenFrame();
            if (presentationFrame == 0 || presentationFrame == m_LastResolvedFrame)
                throw new InvalidOperationException("Motion Matching provider cannot resolve twice in one Presentation frame.");
            if (!float.IsFinite(presentationDeltaSeconds) || presentationDeltaSeconds < 0f ||
                trajectorySource.ResetSequence != m_ResetSequence)
                throw new ArgumentException("Motion Matching provider frame input is invalid.");

            m_LastResolvedFrame = presentationFrame;
            PublishPendingReset(diagnostics);
            m_PresentationTime += presentationDeltaSeconds;
            m_Trajectory.Build(trajectorySource, m_Envelope);
            MotionMatchingSearchTriggerReason triggerReason;
            bool requiresSearch;
            if (m_CurrentDatabaseIndex < 0)
            {
                triggerReason = MotionMatchingSearchTriggerReason.DomainActivated;
                requiresSearch = true;
            }
            else
            {
                requiresSearch = m_Databases[m_CurrentDatabaseIndex].Selection.RequiresSearch(
                    presentationDeltaSeconds,
                    m_ResetSequence,
                    true,
                    out triggerReason);
            }

            if (requiresSearch)
                SearchAndSelect(triggerReason, diagnostics);
            else
                m_CurrentDecision = m_Databases[m_CurrentDatabaseIndex].Selection.GetContinuationDecision();
            if (!m_CurrentDecision.IsValid || m_CurrentDatabaseIndex < 0)
                throw new InvalidOperationException($"Motion Matching provider '{ProviderId}' has no valid cross-Database Selection: {m_CurrentDecision.InvalidReason}.");

            MotionMatchingPoseSourceOutput output = m_Databases[m_CurrentDatabaseIndex].PoseSource.Resolve(
                m_CurrentDecision,
                new PresentationPoseSourceProviderId(ProviderId),
                PresentationPoseSourceIndex,
                PoseNodeId,
                presentationFrame);
            PublishPoseSourceDiagnostics(diagnostics, output);
            return output;
        }

        internal MotionMatchingPoseSourceOutput ResolvePreviewQuery(
            ulong presentationFrame,
            float presentationDeltaSeconds,
            MotionMatchingSearchReplayArtifact fixture,
            RuntimeDiagnosticsContext diagnostics)
        {
            RequireAlive();
            RequireOpenFrame();
            if (fixture == null || presentationFrame == 0 || presentationFrame == m_LastResolvedFrame ||
                !float.IsFinite(presentationDeltaSeconds) || presentationDeltaSeconds < 0f ||
                !string.Equals(fixture.ProjectionIdentity, m_ProjectionIdentity, StringComparison.Ordinal) ||
                !fixture.ProfileId.Equals(m_Projection.ProfileId) ||
                !fixture.SearchDomainId.Equals(m_Binding.SearchDomainId))
                throw new ArgumentException("Motion Matching fixture frame input is invalid.", nameof(fixture));

            int databaseIndex = -1;
            for (int i = 0; i < m_Databases.Length; i++)
            {
                DatabaseRuntime candidate = m_Databases[i];
                if (!candidate.Database.ArtifactIdentity.EqualsExact(fixture.DatabaseIdentity))
                    continue;
                if (!string.Equals(candidate.Database.SearchPolicy.PolicyId, fixture.SearchPolicyId, StringComparison.Ordinal) ||
                    candidate.Database.SearchPolicy.Revision != fixture.SearchPolicyRevision)
                    throw new InvalidOperationException("Motion Matching fixture Search Policy identity is stale.");
                databaseIndex = i;
                break;
            }
            if (databaseIndex < 0)
                throw new InvalidOperationException("Motion Matching preview Database identity is not owned by the selected provider.");
            if (fixture.ResetSequence != m_ResetSequence)
                Reset(fixture.ResetSequence);

            m_Envelope.RestoreIdentity(
                fixture.TrajectorySourceIdentity,
                fixture.TrajectorySourceTick,
                fixture.TrajectorySourceSequence,
                fixture.TrajectorySourceAge,
                fixture.ResetSequence);
            for (int i = 0; i < fixture.TrajectoryPointCount; i++)
                m_Envelope.Add(fixture.GetTrajectoryPoint(i));
            var features = new float[fixture.NormalizedFeatureCount];
            for (int i = 0; i < features.Length; i++)
                features[i] = fixture.GetNormalizedFeature(i);
            var query = new MotionMatchingQuery(
                fixture.QueryId,
                fixture.ProfileId,
                fixture.DatabaseIdentity,
                fixture.SearchDomainId,
                fixture.TrajectorySourceIdentity,
                m_Envelope,
                new MotionMatchingFloatBuffer(features, 0, features.Length),
                fixture.ContactProtection,
                fixture.CurrentSelection,
                fixture.Initialization,
                fixture.SecondsSinceLastJump,
                fixture.ResetSequence);
            DatabaseRuntime database = m_Databases[databaseIndex];
            database.Selection.PrepareDomain(m_ResetSequence);
            MotionMatchingPlanEvaluationResult plan = database.Selection.SearchAndEvaluate(query);
            if (!plan.IsValid)
                throw new InvalidOperationException($"Motion Matching fixture produced no valid plan: {plan.InvalidReason}.");
            StableHash digest = MotionMatchingSearchDigest.Compute(
                database.Database,
                database.Selection.LastSearchResult,
                plan);
            if (!digest.Equals(fixture.ExpectedDigest))
                throw new InvalidOperationException("Motion Matching fixture Search result does not match its exact replay digest.");

            MotionMatchingSelectionDecisionKind kind;
            MotionMatchingSelectionGeneration generation;
            if (!fixture.CurrentSelection.IsValid || fixture.Initialization)
            {
                generation = NextSelectionGeneration();
                kind = MotionMatchingSelectionDecisionKind.Initialize;
            }
            else if (plan.Plan.ContinueCurrent)
            {
                generation = fixture.CurrentSelection.Generation;
                kind = MotionMatchingSelectionDecisionKind.Continue;
            }
            else
            {
                generation = NextSelectionGeneration();
                kind = MotionMatchingSelectionDecisionKind.Jump;
            }
            for (int i = 0; i < m_Databases.Length; i++)
            {
                if (i != databaseIndex)
                    m_Databases[i].Selection.ReleaseDomain();
            }
            m_CurrentDecision = database.Selection.CommitSelection(
                query,
                MotionMatchingSearchTriggerReason.QueryFixture,
                plan,
                generation,
                kind);
            m_CurrentDatabaseIndex = databaseIndex;
            m_LastWinnerQuery = query;
            m_LastResolvedFrame = presentationFrame;
            m_PresentationTime += presentationDeltaSeconds;
            PublishSearchDiagnostics(diagnostics, database);
            MotionMatchingPoseSourceOutput output = database.PoseSource.Resolve(
                m_CurrentDecision,
                new PresentationPoseSourceProviderId(ProviderId),
                PresentationPoseSourceIndex,
                PoseNodeId,
                presentationFrame);
            PublishPoseSourceDiagnostics(diagnostics, output);
            return output;
        }

        internal void PrepareBasePoseCompletion(
            ulong presentationFrame,
            PresentationPoseSourceIndex sourceIndex,
            MotionMatchingSelectionGeneration sourceGeneration)
        {
            RequireAlive();
            RequireOpenFrame();
            if (presentationFrame == 0 || presentationFrame != m_LastResolvedFrame || presentationFrame == m_LastHistoryFrame ||
                !m_CurrentDecision.IsValid || m_CurrentDatabaseIndex < 0 ||
                sourceIndex != PresentationPoseSourceIndex ||
                sourceGeneration != m_CurrentDecision.Generation ||
                m_HistoryCompletionPrepared ||
                m_FeatureBonePositions.Length != m_History.BoneCount)
            {
                throw new InvalidOperationException(
                    "Motion Matching Base Pose History completion does not match the resolved Presentation frame.");
            }
            var continuity =
                new MotionMatchingBasePoseContinuityIdentity(
                    new PresentationPoseSourceProviderId(ProviderId),
                    sourceIndex,
                    m_CurrentDecision.Generation,
                    m_Databases[m_CurrentDatabaseIndex]
                        .Database.ArtifactIdentity);
            m_PreparedHistoryWrites =
                m_History.PrepareCompletion(
                    m_PresentationTime,
                    in continuity,
                    m_ResetSequence);
            m_PreparedHistoryFrame = presentationFrame;
            m_HistoryCompletionPrepared = true;
        }

        internal MotionMatchingHistoryCompletionOutcome
            CompletePreparedBasePose(
                bool poseAvailable,
                in AnimationFootPlacementSample footPlacement)
        {
            if (!m_PreparedHistoryWrites)
            {
                m_LastHistoryFrame = m_PreparedHistoryFrame;
                return MotionMatchingHistoryCompletionOutcome.Skipped;
            }
            if (poseAvailable)
            {
                m_History.CompletePreparedAppend(
                    m_FeatureBonePositions,
                    in footPlacement);
                m_LastHistoryFrame = m_PreparedHistoryFrame;
                return MotionMatchingHistoryCompletionOutcome.Appended;
            }
            m_History.CompletePreparedGap();
            m_LastHistoryFrame = m_PreparedHistoryFrame;
            return MotionMatchingHistoryCompletionOutcome.Gap;
        }

        public void ReleaseDomain()
        {
            RequireAlive();
            if (m_CurrentDatabaseIndex >= 0)
                m_Databases[m_CurrentDatabaseIndex].Selection.ReleaseDomain();
            m_CurrentDatabaseIndex = -1;
            m_CurrentDecision = default;
            m_LastWinnerQuery = default;
            if (!m_FrameOpen)
                m_CommittedPage = ReadPage();
        }

        public void Reset(ulong resetSequence)
        {
            RequireAlive();
            if (m_FrameOpen)
                throw new InvalidOperationException("Motion Matching provider cannot reset while a frame is open.");
            m_PendingResetPreviousSequence = m_ResetSequence;
            m_HasPendingReset = resetSequence != m_ResetSequence;
            for (int i = 0; i < m_Databases.Length; i++)
                m_Databases[i].Selection.Reset(resetSequence);
            m_Trajectory.Reset(resetSequence);
            m_Envelope.Clear();
            m_History.Reset(resetSequence);
            Array.Clear(m_FeatureBonePositions, 0, m_FeatureBonePositions.Length);
            m_CurrentDatabaseIndex = -1;
            m_CurrentDecision = default;
            m_LastWinnerQuery = default;
            m_ResetSequence = resetSequence;
            m_LastResolvedFrame = 0;
            m_LastHistoryFrame = 0;
            m_PresentationTime = 0f;
            ClearPreparedHistoryCompletion();
            m_CommittedPage = ReadPage();
        }

        public void RetargetBodyBranch(ulong resetSequence)
        {
            RequireAlive();
            if (m_FrameOpen)
                throw new InvalidOperationException("Motion Matching provider cannot retarget while a frame is open.");
            if (resetSequence == m_ResetSequence)
                return;
            m_PendingResetPreviousSequence = m_ResetSequence;
            m_HasPendingReset = true;
            for (int i = 0; i < m_Databases.Length; i++)
                m_Databases[i].Selection.RetargetResetSequence(resetSequence);
            m_Trajectory.RetargetResetSequence(resetSequence);
            m_History.RetargetResetSequence(resetSequence);
            m_ResetSequence = resetSequence;
            m_CommittedPage = ReadPage();
        }

        public bool TryCaptureSearchReplay(out MotionMatchingSearchReplayArtifact artifact)
        {
            RequireAlive();
            artifact = null;
            if (m_CurrentDatabaseIndex < 0 || !m_LastWinnerQuery.QueryId.IsValid)
                return false;
            DatabaseRuntime runtime = m_Databases[m_CurrentDatabaseIndex];
            if (!runtime.Selection.LastPlanResult.IsValid)
                return false;
            artifact = MotionMatchingSearchReplayArtifact.Capture(
                m_ProjectionIdentity,
                runtime.Database,
                m_LastWinnerQuery,
                runtime.Selection.LastSearchResult,
                runtime.Selection.LastPlanResult);
            return true;
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            m_Envelope.Clear();
            m_History.Reset(m_ResetSequence);
            Array.Clear(m_FeatureBonePositions, 0, m_FeatureBonePositions.Length);
            ClearPreparedHistoryCompletion();
            DisposeDatabases();
        }

        void SearchAndSelect(MotionMatchingSearchTriggerReason triggerReason, RuntimeDiagnosticsContext diagnostics)
        {
            MotionMatchingSelectionIdentity currentSelection = m_CurrentDecision.SelectionIdentity;
            MotionMatchingContactProtection contactProtection = BuildContactProtection();
            CharacterMotionMatchingQueryId queryId = NextQueryId();
            float secondsSinceLastJump = m_CurrentDatabaseIndex >= 0
                ? m_Databases[m_CurrentDatabaseIndex].Selection.SecondsSinceLastJump
                : 0f;
            int winnerIndex = -1;
            MotionMatchingPlanEvaluationResult winnerPlan = default;
            MotionMatchingQuery winnerQuery = default;
            for (int i = 0; i < m_Databases.Length; i++)
            {
                DatabaseRuntime candidate = m_Databases[i];
                candidate.Selection.PrepareDomain(m_ResetSequence);
                MotionMatchingQuery query = candidate.QueryBuilder.Build(
                    queryId,
                    m_Projection.ProfileId,
                    m_Envelope,
                    m_History,
                    contactProtection,
                    currentSelection,
                    secondsSinceLastJump,
                    m_ResetSequence);
                MotionMatchingPlanEvaluationResult plan = candidate.Selection.SearchAndEvaluate(query);
                if (!plan.IsValid || winnerPlan.IsValid && MotionMatchingPlanEvaluator.Compare(plan.Plan, winnerPlan.Plan) >= 0)
                    continue;
                winnerIndex = i;
                winnerPlan = plan;
                winnerQuery = query;
            }
            if (winnerIndex < 0)
            {
                for (int i = 0; i < m_Databases.Length; i++)
                    m_Databases[i].Selection.ReleaseDomain();
                m_CurrentDatabaseIndex = -1;
                m_CurrentDecision = new MotionMatchingSelectionDecision(MotionMatchingInvalidReason.NoValidPlan, triggerReason);
                PublishInvalidSelectionDiagnostics(diagnostics);
                return;
            }

            bool sameDatabase = winnerIndex == m_CurrentDatabaseIndex;
            MotionMatchingSelectionDecisionKind kind;
            MotionMatchingSelectionGeneration generation;
            if (!currentSelection.IsValid || winnerQuery.Initialization)
            {
                generation = NextSelectionGeneration();
                kind = MotionMatchingSelectionDecisionKind.Initialize;
            }
            else if (sameDatabase && winnerPlan.Plan.ContinueCurrent)
            {
                generation = currentSelection.Generation;
                kind = MotionMatchingSelectionDecisionKind.Continue;
            }
            else
            {
                generation = NextSelectionGeneration();
                kind = MotionMatchingSelectionDecisionKind.Jump;
            }

            for (int i = 0; i < m_Databases.Length; i++)
            {
                if (i != winnerIndex)
                    m_Databases[i].Selection.ReleaseDomain();
            }
            DatabaseRuntime winner = m_Databases[winnerIndex];
            m_CurrentDecision = winner.Selection.CommitSelection(
                winnerQuery,
                triggerReason,
                winnerPlan,
                generation,
                kind);
            m_CurrentDatabaseIndex = winnerIndex;
            m_LastWinnerQuery = winnerQuery;
            PublishSearchDiagnostics(diagnostics, winner);
        }

        void PublishSearchDiagnostics(RuntimeDiagnosticsContext diagnostics, DatabaseRuntime winner)
        {
            MotionMatchingDiagnosticsInterest interest = ResolveDiagnosticsInterest(diagnostics);
            if (interest == MotionMatchingDiagnosticsInterest.None)
                return;
            m_RuntimeSnapshot ??= CreateRuntimeSnapshot();
            m_RuntimeSnapshot.Capture(
                interest,
                m_LastWinnerQuery,
                m_History,
                winner.Database,
                winner.Selection.LastSearchResult,
                winner.Selection.LastPlanResult,
                m_CurrentDecision);
            RuntimeInstanceKey instance = RuntimeInstanceKey.Character(diagnostics.CharacterRuntimeId);
            CharacterMotionMatchingDatabaseArtifactIdentity artifact = winner.Database.ArtifactIdentity;
            if ((interest & MotionMatchingDiagnosticsInterest.QuerySummary) != 0)
            {
                MotionMatchingContactProtection contact = m_LastWinnerQuery.ContactProtection;
                PublishDiagnostics(diagnostics, instance, RuntimeTraceEventKind.MotionMatchingQuery, new RuntimeTracePayload
                {
                    Status = m_LastWinnerQuery.Initialization ? "Initialization" : "Update",
                    Name = m_LastWinnerQuery.QueryId.ToString(),
                    AnimationChannelId = string.Empty,
                    OwnerId = ProviderId,
                    RelatedElementId = artifact.DatabaseId.ToString(),
                    Detail = $"projection={m_ProjectionIdentity};profile={m_Projection.ProfileId};artifact={artifact.ContentHash};domain={m_LastWinnerQuery.SearchDomainId};trajectory={m_LastWinnerQuery.TrajectorySourceIdentity};sourceTick={m_LastWinnerQuery.TrajectoryEnvelope.SourceTick};sourceAge={F(m_LastWinnerQuery.TrajectoryEnvelope.SourceAge)};protected={contact.ProtectedMask};leftPosition={contact.LeftRootPosition};rightPosition={contact.RightRootPosition};leftVelocity={contact.LeftRootVelocity};rightVelocity={contact.RightRootVelocity};reset={m_LastWinnerQuery.ResetSequence}"
                });
            }
            if ((interest & MotionMatchingDiagnosticsInterest.TrajectoryEnvelope) != 0)
            {
                var detail = new StringBuilder();
                for (int i = 0; i < m_RuntimeSnapshot.TrajectoryPointCount; i++)
                {
                    MotionMatchingTrajectoryEnvelopePoint point = m_RuntimeSnapshot.GetTrajectoryPoint(i);
                    AppendSeparator(detail);
                    detail.Append(i).Append(":t=").Append(F(point.TimeOffset))
                        .Append(",position=").Append(point.LocalPositionCenter)
                        .Append(",facing=").Append(point.LocalFacingCenter)
                        .Append(",positionTolerance=").Append(F(point.PositionToleranceRadius))
                        .Append(",facingTolerance=").Append(F(point.FacingToleranceDegrees))
                        .Append(",confidence=").Append(F(point.Confidence));
                }
                PublishDiagnostics(diagnostics, instance, RuntimeTraceEventKind.MotionMatchingTrajectory, new RuntimeTracePayload
                {
                    Status = "Envelope",
                    Name = m_LastWinnerQuery.QueryId.ToString(),
                    AnimationChannelId = string.Empty,
                    OwnerId = ProviderId,
                    Priority = m_RuntimeSnapshot.TrajectoryPointCount,
                    Detail = detail.ToString()
                });
            }
            if ((interest & MotionMatchingDiagnosticsInterest.PoseHistory) != 0)
            {
                MotionMatchingPoseHistoryTrace history = m_RuntimeSnapshot.PoseHistory;
                PublishDiagnostics(diagnostics, instance, RuntimeTraceEventKind.MotionMatchingPoseHistory, new RuntimeTracePayload
                {
                    Status = history.HasGap ? "Gap" : "Continuous",
                    Name = m_LastWinnerQuery.QueryId.ToString(),
                    AnimationChannelId = string.Empty,
                    OwnerId = ProviderId,
                    Priority = history.Count,
                    Cycle = history.Capacity,
                    Time = history.LatestPresentationTime,
                    Detail = $"database={history.LatestDatabaseIdentity?.DatabaseId};artifact={history.LatestDatabaseIdentity?.ContentHash}"
                });
            }
            if ((interest & MotionMatchingDiagnosticsInterest.AdmissionAggregate) != 0)
            {
                PublishDiagnostics(diagnostics, instance, RuntimeTraceEventKind.MotionMatchingAdmission, new RuntimeTracePayload
                {
                    Status = "Completed",
                    Name = m_LastWinnerQuery.QueryId.ToString(),
                    AnimationChannelId = string.Empty,
                    OwnerId = ProviderId,
                    Priority = m_RuntimeSnapshot.Admission.Admitted,
                    Cycle = m_RuntimeSnapshot.Admission.Rejected,
                    Detail = $"admitted={m_RuntimeSnapshot.Admission.Admitted};rejected={m_RuntimeSnapshot.Admission.Rejected}"
                });
            }
            if ((interest & MotionMatchingDiagnosticsInterest.CandidateRejectDetail) != 0)
            {
                var detail = new StringBuilder();
                for (int i = 0; i < m_RuntimeSnapshot.RejectDetailCount; i++)
                {
                    MotionMatchingCandidateRejectTrace trace = m_RuntimeSnapshot.GetRejectDetail(i);
                    AppendSeparator(detail);
                    detail.Append(trace.SampleId).Append(':').Append(trace.Detail.Reason)
                        .Append(" value=").Append(F(trace.Detail.Value))
                        .Append(" limit=").Append(F(trace.Detail.Limit))
                        .Append(" secondaryValue=").Append(F(trace.Detail.SecondaryValue))
                        .Append(" secondaryLimit=").Append(F(trace.Detail.SecondaryLimit));
                }
                PublishDiagnostics(diagnostics, instance, RuntimeTraceEventKind.MotionMatchingCandidateRejected, new RuntimeTracePayload
                {
                    Status = "Rejected",
                    Name = m_LastWinnerQuery.QueryId.ToString(),
                    AnimationChannelId = string.Empty,
                    OwnerId = ProviderId,
                    Priority = m_RuntimeSnapshot.RejectDetailCount,
                    Detail = detail.ToString()
                });
            }
            if ((interest & MotionMatchingDiagnosticsInterest.SearchTraversal) != 0)
            {
                MotionMatchingSearchTraversalTrace traversal = m_RuntimeSnapshot.Traversal;
                PublishDiagnostics(diagnostics, instance, RuntimeTraceEventKind.MotionMatchingSearchTraversal, new RuntimeTracePayload
                {
                    Status = "Exact",
                    Name = m_LastWinnerQuery.QueryId.ToString(),
                    AnimationChannelId = string.Empty,
                    OwnerId = ProviderId,
                    Priority = traversal.NodesVisited,
                    Cycle = traversal.NodesPruned,
                    Detail = $"visited={traversal.NodesVisited};pruned={traversal.NodesPruned};exactSamples={traversal.ExactSampleCount}"
                });
            }
            if ((interest & MotionMatchingDiagnosticsInterest.TopKCosts) != 0)
            {
                var detail = new StringBuilder();
                for (int i = 0; i < m_RuntimeSnapshot.TopKCount; i++)
                {
                    MotionMatchingTopKCostTrace trace = m_RuntimeSnapshot.GetTopK(i);
                    MotionMatchingExactCostComponents cost = trace.Cost;
                    AppendSeparator(detail);
                    detail.Append(trace.SampleId).Append(":total=").Append(F(cost.Total))
                        .Append(",trajectoryPosition=").Append(F(cost.TrajectoryPosition))
                        .Append(",trajectoryFacing=").Append(F(cost.TrajectoryFacing))
                        .Append(",trajectoryVelocity=").Append(F(cost.TrajectoryVelocity))
                        .Append(",posePosition=").Append(F(cost.PosePosition))
                        .Append(",poseVelocity=").Append(F(cost.PoseVelocity))
                        .Append(",contact=").Append(F(cost.ContactSoft))
                        .Append(",continuation=").Append(F(cost.Continuation))
                        .Append(",jump=").Append(F(cost.Jump));
                }
                PublishDiagnostics(diagnostics, instance, RuntimeTraceEventKind.MotionMatchingTopK, new RuntimeTracePayload
                {
                    Status = "Exact",
                    Name = m_LastWinnerQuery.QueryId.ToString(),
                    AnimationChannelId = string.Empty,
                    OwnerId = ProviderId,
                    Priority = m_RuntimeSnapshot.TopKCount,
                    Detail = detail.ToString()
                });
            }
            if ((interest & MotionMatchingDiagnosticsInterest.PlanCosts) != 0)
            {
                MotionMatchingPlanCostTrace plan = m_RuntimeSnapshot.Plan;
                MotionMatchingPlanCostComponents cost = plan.Cost;
                PublishDiagnostics(diagnostics, instance, RuntimeTraceEventKind.MotionMatchingPlan, new RuntimeTracePayload
                {
                    Status = "Selected",
                    Name = plan.PlanId.ToString(),
                    AnimationChannelId = string.Empty,
                    OwnerId = ProviderId,
                    RelatedElementId = plan.EntrySampleId.ToString(),
                    Weight = cost.Total,
                    Detail = $"trajectoryPosition={F(cost.TrajectoryPosition)};trajectoryFacing={F(cost.TrajectoryFacing)};contact={F(cost.Contact)};segmentEnd={F(cost.SegmentEnd)};velocityChange={F(cost.VelocityChange)}"
                });
            }
            if ((interest & MotionMatchingDiagnosticsInterest.Selection) != 0)
                PublishSelectionDiagnostics(diagnostics, instance, m_RuntimeSnapshot.Selection);
        }

        void PublishSelectionDiagnostics(
            RuntimeDiagnosticsContext diagnostics,
            RuntimeInstanceKey instance,
            MotionMatchingSelectionTrace selection)
        {
            PublishDiagnostics(diagnostics, instance, RuntimeTraceEventKind.MotionMatchingSelection, new RuntimeTracePayload
            {
                Status = selection.Kind.ToString(),
                Name = selection.PlanId.ToString(),
                AnimationChannelId = string.Empty,
                OwnerId = ProviderId,
                RelatedElementId = selection.DatabaseIdentity?.DatabaseId.ToString() ?? string.Empty,
                Priority = selection.SampleIndex,
                Cycle = (int)selection.Generation.Value,
                Cause = selection.TriggerReason.ToString(),
                Detail = $"generation={selection.Generation};sampleIndex={selection.SampleIndex};invalid={selection.InvalidReason}"
            });
        }

        void PublishInvalidSelectionDiagnostics(RuntimeDiagnosticsContext diagnostics)
        {
            if (diagnostics == null || !diagnostics.ShouldPublish(RuntimeTraceChannel.Animation, RuntimeTraceEventKind.MotionMatchingSelection))
                return;
            PublishDiagnostics(diagnostics, RuntimeInstanceKey.Character(diagnostics.CharacterRuntimeId), RuntimeTraceEventKind.MotionMatchingSelection, new RuntimeTracePayload
            {
                Status = "Invalid",
                AnimationChannelId = string.Empty,
                OwnerId = ProviderId,
                Cause = m_CurrentDecision.TriggerReason.ToString(),
                Detail = m_CurrentDecision.InvalidReason.ToString()
            });
        }

        void PublishPoseSourceDiagnostics(RuntimeDiagnosticsContext diagnostics, MotionMatchingPoseSourceOutput output)
        {
            if (diagnostics == null || !diagnostics.ShouldPublish(RuntimeTraceChannel.Animation, RuntimeTraceEventKind.MotionMatchingPoseSource))
                return;
            var trace = new MotionMatchingPoseSourceTrace(output);
            PublishDiagnostics(diagnostics, RuntimeInstanceKey.Character(diagnostics.CharacterRuntimeId), RuntimeTraceEventKind.MotionMatchingPoseSource, new RuntimeTracePayload
            {
                Status = m_CurrentDecision.Kind.ToString(),
                Name = trace.PlayerNodeId.ToString(),
                AnimationChannelId = string.Empty,
                OwnerId = trace.ProviderId.ToString(),
                RelatedElementId = trace.SourceClipId.ToString(),
                Time = trace.SampleTime,
                SecondaryTime = trace.VisualTimeScale,
                Cycle = trace.Cycle,
                Priority = (int)trace.SelectionGeneration.Value,
                Detail = $"sourceIndex={trace.SourceIndex};database={trace.DatabaseIdentity.DatabaseId};artifact={trace.DatabaseIdentity.ContentHash};frame={trace.FrameSequence};continuousTime={trace.ContinuousVisualTime.ToString("R", CultureInfo.InvariantCulture)};footWeight={trace.FootPlacementWeightParameterId}"
            });
        }

        void PublishPendingReset(RuntimeDiagnosticsContext diagnostics)
        {
            if (!m_HasPendingReset)
                return;
            if (diagnostics != null && diagnostics.ShouldPublish(RuntimeTraceChannel.Animation, RuntimeTraceEventKind.MotionMatchingReset))
            {
                PublishDiagnostics(diagnostics, RuntimeInstanceKey.Character(diagnostics.CharacterRuntimeId), RuntimeTraceEventKind.MotionMatchingReset, new RuntimeTracePayload
                {
                    Status = "Initialization",
                    AnimationChannelId = string.Empty,
                    OwnerId = ProviderId,
                    Cause = MotionMatchingSearchTriggerReason.PresentationReset.ToString(),
                    Detail = $"previous={m_PendingResetPreviousSequence};current={m_ResetSequence}"
                });
            }
            m_HasPendingReset = false;
        }

        MotionMatchingDiagnosticsInterest ResolveDiagnosticsInterest(RuntimeDiagnosticsContext diagnostics)
        {
            if (diagnostics == null)
                return MotionMatchingDiagnosticsInterest.None;
            MotionMatchingDiagnosticsInterest interest = MotionMatchingDiagnosticsInterest.None;
            AddInterest(diagnostics, RuntimeTraceEventKind.MotionMatchingQuery, MotionMatchingDiagnosticsInterest.QuerySummary, ref interest);
            AddInterest(diagnostics, RuntimeTraceEventKind.MotionMatchingTrajectory, MotionMatchingDiagnosticsInterest.TrajectoryEnvelope, ref interest);
            AddInterest(diagnostics, RuntimeTraceEventKind.MotionMatchingPoseHistory, MotionMatchingDiagnosticsInterest.PoseHistory, ref interest);
            AddInterest(diagnostics, RuntimeTraceEventKind.MotionMatchingAdmission, MotionMatchingDiagnosticsInterest.AdmissionAggregate, ref interest);
            AddInterest(diagnostics, RuntimeTraceEventKind.MotionMatchingCandidateRejected, MotionMatchingDiagnosticsInterest.CandidateRejectDetail, ref interest);
            AddInterest(diagnostics, RuntimeTraceEventKind.MotionMatchingSearchTraversal, MotionMatchingDiagnosticsInterest.SearchTraversal, ref interest);
            AddInterest(diagnostics, RuntimeTraceEventKind.MotionMatchingTopK, MotionMatchingDiagnosticsInterest.TopKCosts, ref interest);
            AddInterest(diagnostics, RuntimeTraceEventKind.MotionMatchingPlan, MotionMatchingDiagnosticsInterest.PlanCosts, ref interest);
            AddInterest(diagnostics, RuntimeTraceEventKind.MotionMatchingSelection, MotionMatchingDiagnosticsInterest.Selection, ref interest);
            return interest;
        }

        MotionMatchingRuntimeSnapshot CreateRuntimeSnapshot()
        {
            int rejectCapacity = 0;
            int topKCapacity = 0;
            for (int i = 0; i < m_Databases.Length; i++)
            {
                rejectCapacity = Math.Max(rejectCapacity, m_Databases[i].Database.Capacities.DiagnosticDetailCapacity);
                topKCapacity = Math.Max(topKCapacity, m_Databases[i].Database.Capacities.TopK);
            }
            return new MotionMatchingRuntimeSnapshot(m_Envelope.Capacity, rejectCapacity, topKCapacity);
        }

        static void AddInterest(
            RuntimeDiagnosticsContext diagnostics,
            RuntimeTraceEventKind kind,
            MotionMatchingDiagnosticsInterest value,
            ref MotionMatchingDiagnosticsInterest interest)
        {
            if (diagnostics.ShouldPublish(RuntimeTraceChannel.Animation, kind))
                interest |= value;
        }

        static void PublishDiagnostics(
            RuntimeDiagnosticsContext diagnostics,
            RuntimeInstanceKey instance,
            RuntimeTraceEventKind kind,
            RuntimeTracePayload payload)
        {
            diagnostics.Publish(
                RuntimeTraceChannel.Animation,
                RuntimeTraceDomain.Presentation,
                kind,
                RuntimeSourceElementHandle.Invalid,
                instance,
                payload);
        }

        static void AppendSeparator(StringBuilder builder)
        {
            if (builder.Length > 0)
                builder.Append(" | ");
        }

        static string F(float value) => value.ToString("R", CultureInfo.InvariantCulture);

        MotionMatchingContactProtection BuildContactProtection()
        {
            if (!m_CurrentDecision.IsValid || m_CurrentDatabaseIndex < 0)
                return new MotionMatchingContactProtection(MotionMatchingFootContactMask.None, Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero);
            MotionMatchingSamplePayload sample = m_Databases[m_CurrentDatabaseIndex].Database.GetSample(m_CurrentDecision.SampleIndex);
            return new MotionMatchingContactProtection(
                sample.ContactMask,
                sample.LeftFootRootPosition,
                sample.RightFootRootPosition,
                sample.LeftFoot.SoleLocalVelocity,
                sample.RightFoot.SoleLocalVelocity);
        }

        CharacterMotionMatchingQueryId NextQueryId()
        {
            if (m_QuerySequence == ulong.MaxValue)
                throw new InvalidOperationException("Motion Matching Query identity was exhausted.");
            return new CharacterMotionMatchingQueryId(++m_QuerySequence);
        }

        MotionMatchingSelectionGeneration NextSelectionGeneration()
        {
            if (m_SelectionGenerationSequence == ulong.MaxValue)
                throw new InvalidOperationException("Motion Matching Selection generation was exhausted.");
            return new MotionMatchingSelectionGeneration(++m_SelectionGenerationSequence);
        }

        static int RequireRigBoneIndex(CharacterAnimationRigPayload rig, AnimationBoneId boneId)
        {
            for (int i = 0; i < rig.PhysicalBoneCount; i++)
            {
                if (rig.PhysicalBones[i].BoneId.Equals(boneId))
                    return i;
            }
            throw new InvalidOperationException($"Motion Matching Feature Bone '{boneId}' is absent from the compiled Animation Rig.");
        }

        void DisposeDatabases()
        {
            for (int i = m_Databases.Length - 1; i >= 0; i--)
                m_Databases[i]?.Database.Dispose();
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(CharacterMotionMatchingProviderRuntime));
        }

        void RequireOpenFrame()
        {
            if (!m_FrameOpen)
                throw new InvalidOperationException("Motion Matching provider has no open frame.");
        }

        sealed class DatabaseRuntime
        {
            public DatabaseRuntime(CharacterMotionMatchingRuntimeDatabase database)
            {
                Database = database ?? throw new ArgumentNullException(nameof(database));
                QueryBuilder = new MotionMatchingQueryBuilder(database);
                Selection = new CharacterMotionMatchingSelectionRuntime(database);
                PoseSource = new MotionMatchingPoseSourceRuntime(database);
            }

            public CharacterMotionMatchingRuntimeDatabase Database { get; }
            public MotionMatchingQueryBuilder QueryBuilder { get; }
            public CharacterMotionMatchingSelectionRuntime Selection { get; }
            public MotionMatchingPoseSourceRuntime PoseSource { get; }
        }
    }
}
