using System;
using System.Collections.Generic;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation.MotionMatching
{
    public sealed class CharacterMotionMatchingProducerRuntime : IDisposable
    {
        readonly MotionMatchingProjectionPayload m_Projection;
        readonly MotionMatchingProducerBindingPayload m_Binding;
        readonly DatabaseRuntime[] m_Databases;
        readonly CharacterMotionMatchingTrajectoryRuntime m_Trajectory;
        readonly MotionMatchingTrajectoryEnvelope m_Envelope;
        readonly CharacterMotionMatchingPoseHistory m_History;
        readonly int[] m_FeatureBoneIndices;
        readonly Vector3[] m_FeatureBonePositions;

        MotionMatchingSelectionDecision m_CurrentDecision;
        AnimationPlaybackId m_CurrentPlaybackId;
        MotionMatchingQuery m_LastWinnerQuery;
        int m_CurrentDatabaseIndex = -1;
        ulong m_QuerySequence;
        ulong m_SelectionGenerationSequence;
        ulong m_ResetSequence;
        ulong m_LastResolvedFrame;
        ulong m_LastHistoryFrame;
        float m_PresentationTime;
        bool m_Disposed;

        public CharacterMotionMatchingProducerRuntime(
            MotionMatchingProjectionPayload projection,
            MotionMatchingProducerBindingPayload binding,
            CharacterAnimationRigPayload rig)
        {
            m_Projection = projection ?? throw new ArgumentNullException(nameof(projection));
            m_Binding = binding;
            if (!binding.AnimationChannelId.IsValid || !binding.PoseSlotId.IsValid ||
                !binding.SearchDomainId.IsValid || binding.DatabaseCount <= 0 || rig == null)
                throw new ArgumentException("Motion Matching producer runtime binding is invalid.");
            rig.RequireValid();
            m_Databases = new DatabaseRuntime[binding.DatabaseCount];
            try
            {
                for (int i = 0; i < m_Databases.Length; i++)
                {
                    int projectionDatabaseIndex = binding.FirstDatabaseIndex + i;
                    MotionMatchingDatabasePayload payload = projection.GetDatabase(projectionDatabaseIndex);
                    if (payload == null || !payload.SearchDomainId.Equals(binding.SearchDomainId))
                        throw new InvalidOperationException("Motion Matching producer Database range is invalid.");
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
        }

        public string ProgramProducerId => m_Binding.ProgramProducerId;
        public AnimationChannelId AnimationChannelId => m_Binding.AnimationChannelId;
        public PoseSlotId PoseSlotId => m_Binding.PoseSlotId;
        public int FeatureBoneCount => m_FeatureBoneIndices.Length;
        public MotionMatchingSelectionDecision CurrentDecision => m_CurrentDecision;
        public MotionMatchingQuery LastWinnerQuery => m_LastWinnerQuery;
        public CharacterMotionMatchingPoseHistory History => m_History;
        public CharacterMotionMatchingDatabaseArtifactIdentity CurrentDatabaseIdentity =>
            m_CurrentDatabaseIndex >= 0 ? m_Databases[m_CurrentDatabaseIndex].Database.ArtifactIdentity : null;
        public int GetFeatureRigBoneIndex(int index) => m_FeatureBoneIndices[index];
        internal int[] FeatureRigBoneIndices => m_FeatureBoneIndices;
        public Vector3[] FeatureBonePositionWorkspace => m_FeatureBonePositions;

        public MotionMatchingPoseSourceOutput Resolve(
            ulong presentationFrame,
            float presentationDeltaSeconds,
            MotionMatchingTrajectorySourceFrame trajectorySource,
            AnimationPlaybackId playbackId,
            ulong presentationRequestSequence,
            int programProducerIndex)
        {
            RequireAlive();
            if (presentationFrame == 0 || presentationFrame == m_LastResolvedFrame)
                throw new InvalidOperationException("Motion Matching producer cannot resolve twice in one Presentation frame.");
            if (!float.IsFinite(presentationDeltaSeconds) || presentationDeltaSeconds < 0f ||
                !playbackId.IsValid || presentationRequestSequence == 0 || programProducerIndex < 0 ||
                trajectorySource.ResetSequence != m_ResetSequence)
                throw new ArgumentException("Motion Matching producer frame input is invalid.");

            m_LastResolvedFrame = presentationFrame;
            if (m_CurrentPlaybackId.IsValid && !m_CurrentPlaybackId.Equals(playbackId))
                ReleaseDomain();
            m_CurrentPlaybackId = playbackId;
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
                SearchAndSelect(triggerReason);
            else
                m_CurrentDecision = m_Databases[m_CurrentDatabaseIndex].Selection.GetContinuationDecision();
            if (!m_CurrentDecision.IsValid || m_CurrentDatabaseIndex < 0)
                throw new InvalidOperationException($"Motion Matching producer '{ProgramProducerId}' has no valid cross-Database Selection: {m_CurrentDecision.InvalidReason}.");

            return m_Databases[m_CurrentDatabaseIndex].PoseSource.Resolve(
                m_CurrentDecision,
                AnimationChannelId,
                PoseSlotId,
                playbackId,
                presentationRequestSequence,
                programProducerIndex,
                ProgramProducerId);
        }

        public void AppendBasePose(
            ulong presentationFrame,
            AnimationPlaybackId playbackId,
            AnimationFootPlacementSample footPlacement)
        {
            RequireAlive();
            if (presentationFrame == 0 || presentationFrame != m_LastResolvedFrame || presentationFrame == m_LastHistoryFrame ||
                !m_CurrentDecision.IsValid || m_CurrentDatabaseIndex < 0 || !playbackId.Equals(m_CurrentPlaybackId) || !footPlacement.IsValid)
                throw new InvalidOperationException("Motion Matching Base Pose History append does not match the resolved Presentation frame.");
            if (m_History.Count > 0 && m_PresentationTime <= m_History.LatestPresentationTime)
            {
                m_LastHistoryFrame = presentationFrame;
                return;
            }
            m_History.Append(
                new MotionMatchingBasePoseFrameInput(
                    m_PresentationTime,
                    new MotionMatchingBasePoseContinuityIdentity(
                        playbackId,
                        m_CurrentDecision.Generation,
                        m_Databases[m_CurrentDatabaseIndex].Database.ArtifactIdentity),
                    m_FeatureBonePositions,
                    footPlacement),
                m_ResetSequence);
            m_LastHistoryFrame = presentationFrame;
        }

        public void ReleaseDomain()
        {
            RequireAlive();
            if (m_CurrentDatabaseIndex >= 0)
                m_Databases[m_CurrentDatabaseIndex].Selection.ReleaseDomain();
            m_CurrentDatabaseIndex = -1;
            m_CurrentDecision = default;
            m_CurrentPlaybackId = default;
            m_LastWinnerQuery = default;
        }

        public void Reset(ulong resetSequence)
        {
            RequireAlive();
            for (int i = 0; i < m_Databases.Length; i++)
                m_Databases[i].Selection.Reset(resetSequence);
            m_Trajectory.Reset(resetSequence);
            m_Envelope.Clear();
            m_History.Reset(resetSequence);
            Array.Clear(m_FeatureBonePositions, 0, m_FeatureBonePositions.Length);
            m_CurrentDatabaseIndex = -1;
            m_CurrentDecision = default;
            m_CurrentPlaybackId = default;
            m_LastWinnerQuery = default;
            m_ResetSequence = resetSequence;
            m_LastResolvedFrame = 0;
            m_LastHistoryFrame = 0;
            m_PresentationTime = 0f;
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            m_Envelope.Clear();
            m_History.Reset(m_ResetSequence);
            Array.Clear(m_FeatureBonePositions, 0, m_FeatureBonePositions.Length);
            DisposeDatabases();
        }

        void SearchAndSelect(MotionMatchingSearchTriggerReason triggerReason)
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
                candidate.LastQuery = query;
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
        }

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
            for (int i = 0; i < rig.Bones.Count; i++)
            {
                if (rig.Bones[i].BoneId.Equals(boneId))
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
                throw new ObjectDisposedException(nameof(CharacterMotionMatchingProducerRuntime));
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
            public MotionMatchingQuery LastQuery { get; set; }
        }
    }
}
