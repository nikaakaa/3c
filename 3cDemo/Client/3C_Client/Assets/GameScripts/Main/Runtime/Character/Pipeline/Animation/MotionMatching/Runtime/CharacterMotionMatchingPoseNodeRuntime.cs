using System;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation.MotionMatching
{
    internal enum CharacterMotionMatchingPoseNodeInvalidReason : byte
    {
        None = 0,
        FrameContextInvalid = 1,
        HistoryViewInvalid = 2,
        ChooserInvalid = 3,
        ChooserDatabaseOutsideBinding = 4,
        SearchDisabledWithoutSelection = 5,
        SearchInvalid = 6,
        SourcePlanInvalid = 7,
        RelevanceInactive = 8,
        BlendInvalid = 9
    }

    internal readonly struct CharacterMotionMatchingEntryIdentity
    {
        internal CharacterMotionMatchingEntryIdentity(
            PoseNodeId nodeId,
            PoseGraphId entryGraphId,
            CharacterMotionMatchingSourceLineage sourceLineage,
            CharacterMotionMatchingPlanId planId,
            CharacterMotionMatchingRigLineage rigLineage)
        {
            if (!nodeId.IsValid || !entryGraphId.IsValid || !sourceLineage.IsValid ||
                !planId.IsValid || !rigLineage.IsValid)
            {
                throw new ArgumentException("Motion Matching entry identity is incomplete.");
            }
            NodeId = nodeId;
            EntryGraphId = entryGraphId;
            SourceLineage = sourceLineage;
            PlanId = planId;
            RigLineage = rigLineage;
        }

        internal PoseNodeId NodeId { get; }
        internal PoseGraphId EntryGraphId { get; }
        internal CharacterMotionMatchingSourceLineage SourceLineage { get; }
        internal CharacterMotionMatchingPlanId PlanId { get; }
        internal CharacterMotionMatchingRigLineage RigLineage { get; }
        internal bool IsValid => NodeId.IsValid && EntryGraphId.IsValid && SourceLineage.IsValid &&
                                 PlanId.IsValid && RigLineage.IsValid;
    }

    internal readonly struct CharacterMotionMatchingEntrySourcePlan
    {
        internal CharacterMotionMatchingEntrySourcePlan(
            CharacterMotionMatchingEntryIdentity identity,
            ulong frameIdentity,
            MotionMatchingClipSamplePlan clipSample,
            MotionMatchingPoseParameterSample footPlacementWeight,
            AnimationFootPlacementSample footPlacement)
        {
            if (!identity.IsValid || frameIdentity == 0 || !clipSample.IsValid ||
                !footPlacementWeight.IsValid || !footPlacement.IsValid)
            {
                throw new ArgumentException("Motion Matching entry source plan is incomplete.");
            }
            Identity = identity;
            FrameIdentity = frameIdentity;
            ClipSample = clipSample;
            FootPlacementWeight = footPlacementWeight;
            FootPlacement = footPlacement;
        }

        internal CharacterMotionMatchingEntryIdentity Identity { get; }
        internal ulong FrameIdentity { get; }
        internal MotionMatchingClipSamplePlan ClipSample { get; }
        internal MotionMatchingPoseParameterSample FootPlacementWeight { get; }
        internal AnimationFootPlacementSample FootPlacement { get; }
        internal bool IsValid => Identity.IsValid && FrameIdentity != 0 &&
                                 ClipSample.IsValid && FootPlacementWeight.IsValid && FootPlacement.IsValid;
    }

    internal readonly struct CharacterMotionMatchingPoseNodeEvaluation
    {
        internal CharacterMotionMatchingPoseNodeEvaluation(
            CharacterMotionMatchingEntrySourcePlan entrySource,
            MotionMatchingSelectionDecision selection,
            CharacterMotionMatchingDatabaseChooserResolution chooser,
            MotionMatchingSearchResult search,
            MotionMatchingPlanEvaluationResult plan,
            CharacterMotionMatchingBlendFramePlan blend)
        {
            if (!entrySource.IsValid || !selection.IsValid || !chooser.IsValid || !blend.IsValid)
                throw new ArgumentException("Motion Matching Pose node evaluation is incomplete.");
            EntrySource = entrySource;
            Selection = selection;
            Chooser = chooser;
            Search = search;
            Plan = plan;
            Blend = blend;
            InvalidReason = CharacterMotionMatchingPoseNodeInvalidReason.None;
        }

        internal CharacterMotionMatchingPoseNodeEvaluation(
            CharacterMotionMatchingPoseNodeInvalidReason invalidReason,
            CharacterMotionMatchingDatabaseChooserResolution chooser = default)
        {
            if (invalidReason == CharacterMotionMatchingPoseNodeInvalidReason.None)
                throw new ArgumentOutOfRangeException(nameof(invalidReason));
            EntrySource = default;
            Selection = default;
            Chooser = chooser;
            Search = default;
            Plan = default;
            Blend = default;
            InvalidReason = invalidReason;
        }

        internal CharacterMotionMatchingEntrySourcePlan EntrySource { get; }
        internal MotionMatchingSelectionDecision Selection { get; }
        internal CharacterMotionMatchingDatabaseChooserResolution Chooser { get; }
        internal MotionMatchingSearchResult Search { get; }
        internal MotionMatchingPlanEvaluationResult Plan { get; }
        internal CharacterMotionMatchingBlendFramePlan Blend { get; }
        internal CharacterMotionMatchingPoseNodeInvalidReason InvalidReason { get; }
        internal bool IsValid => InvalidReason == CharacterMotionMatchingPoseNodeInvalidReason.None &&
                                 EntrySource.IsValid && Selection.IsValid && Chooser.IsValid && Blend.IsValid;
    }

    internal sealed class CharacterMotionMatchingPoseNodeRuntime : IDisposable
    {
        sealed class DatabaseRuntime
        {
            internal DatabaseRuntime(
                CharacterMotionMatchingRuntimeDatabase database,
                int globalDatabaseIndex)
            {
                Database = database ?? throw new ArgumentNullException(nameof(database));
                GlobalDatabaseIndex = globalDatabaseIndex;
                QueryBuilder = new MotionMatchingQueryBuilder(database);
                Selection = new CharacterMotionMatchingSelectionRuntime(database);
            }

            internal CharacterMotionMatchingRuntimeDatabase Database { get; }
            internal int GlobalDatabaseIndex { get; }
            internal MotionMatchingQueryBuilder QueryBuilder { get; }
            internal CharacterMotionMatchingSelectionRuntime Selection { get; }
        }

        readonly CharacterPresentationProjection m_Projection;
        readonly CharacterMotionMatchingPosePlanDescriptor m_Plan;
        readonly MotionMatchingNodeBindingPayload m_Binding;
        readonly CharacterPoseHistoryCollectorRuntime m_History;
        readonly CharacterMotionMatchingBlendStackRuntime m_Blend;
        readonly CharacterMotionMatchingRigLineage m_RigLineage;
        readonly PoseGraphId m_EntryGraphId;
        readonly DatabaseRuntime[] m_Databases;
        readonly int[] m_ChooserDatabaseIndices;
        readonly int[] m_SearchLocalIndices;
        readonly CharacterMotionMatchingRuntimeDatabase[] m_SearchDatabases;
        readonly MotionMatchingQuery[] m_SearchQueries;

        CharacterMotionMatchingFrameContext m_FrameContext;
        CharacterPoseHistoryReadView m_HistoryView;
        CharacterMotionMatchingDatabaseChooserResolution m_ChooserResolution;
        MotionMatchingSelectionDecision m_CurrentSelection;
        CharacterMotionMatchingPoseNodeEvaluation m_CurrentEvaluation;
        int m_CurrentDatabaseIndex = -1;
        ulong m_QuerySequence;
        ulong m_GenerationSequence;
        ulong m_ChooserSignature;
        bool m_HasChooserSignature;
        bool m_Relevant;
        bool m_FrameOpen;
        bool m_Disposed;

        internal CharacterMotionMatchingPoseNodeRuntime(
            CharacterPresentationProjection projection,
            CharacterMotionMatchingPosePlanDescriptor plan,
            CharacterPoseHistoryCollectorRuntime history)
        {
            m_Projection = projection ?? throw new ArgumentNullException(nameof(projection));
            m_Plan = plan ?? throw new ArgumentNullException(nameof(plan));
            m_History = history ?? throw new ArgumentNullException(nameof(history));
            projection.RequirePosePayload();
            m_Binding = ResolveBinding(projection.MotionMatching, plan.NodeId);
            if (plan.BindingId != m_Binding.BindingId || plan.BindingRevision != m_Binding.BindingRevision ||
                plan.ProfileId != projection.MotionMatching.ProfileId || plan.ProfileRevision != projection.MotionMatching.ProfileRevision ||
                plan.ChooserId != m_Binding.ChooserId || plan.ChooserRevision != m_Binding.ChooserRevision ||
                plan.SearchDomainId != m_Binding.SearchDomainId || plan.FirstDatabaseIndex != m_Binding.FirstDatabaseIndex ||
                plan.DatabaseCount != m_Binding.DatabaseCount || plan.CollectorIndex < 0 ||
                plan.EntryProgramIndex < 0 || plan.EntryProgramIndex >= projection.PosePlan.MotionMatchingEntryPrograms.Count)
            {
                throw new InvalidOperationException($"Motion Matching Pose node '{plan.NodeId}' plan and Projection binding differ.");
            }
            m_RigLineage = new CharacterMotionMatchingRigLineage(
                projection.Rig.RigId,
                projection.Rig.RigRevision,
                projection.Rig.PoseBoneCount);
            if (!m_RigLineage.Equals(history.RigLineage) ||
                history.BoneCount != projection.MotionMatching.FeatureSchema.BoneCount)
            {
                throw new InvalidOperationException($"Motion Matching Pose node '{plan.NodeId}' History layout is incompatible.");
            }
            m_EntryGraphId = projection.PosePlan.MotionMatchingEntryPrograms[plan.EntryProgramIndex].GraphId;
            if (plan.BlendPlanIndex < 0 || plan.BlendPlanIndex >= projection.PosePlan.MotionMatchingBlendPlans.Count)
                throw new InvalidOperationException($"Motion Matching Pose node '{plan.NodeId}' has no compiled internal Blend plan.");
            m_Blend = new CharacterMotionMatchingBlendStackRuntime(
                plan.NodeId,
                projection.PosePlan.MotionMatchingBlendPlans[plan.BlendPlanIndex],
                projection.BlendCurveCatalog,
                projection.BlendProfileCatalog,
                projection.Rig,
                plan.LiveEntryCapacity);
            m_Databases = new DatabaseRuntime[m_Binding.DatabaseCount];
            m_ChooserDatabaseIndices = new int[m_Binding.DatabaseCount];
            m_SearchLocalIndices = new int[m_Binding.DatabaseCount];
            m_SearchDatabases = new CharacterMotionMatchingRuntimeDatabase[m_Binding.DatabaseCount];
            m_SearchQueries = new MotionMatchingQuery[m_Binding.DatabaseCount];
            try
            {
                for (int localIndex = 0; localIndex < m_Databases.Length; localIndex++)
                {
                    int globalIndex = m_Binding.FirstDatabaseIndex + localIndex;
                    m_Databases[localIndex] = new DatabaseRuntime(
                        new CharacterMotionMatchingRuntimeDatabase(
                            projection.MotionMatching,
                            globalIndex),
                        globalIndex);
                }
            }
            catch
            {
                DisposeDatabases();
                throw;
            }
        }

        internal PoseNodeId NodeId => m_Plan.NodeId;
        internal MotionMatchingSelectionDecision CurrentSelection => m_CurrentSelection;
        internal CharacterMotionMatchingPoseNodeEvaluation CurrentEvaluation => m_CurrentEvaluation;
        internal int CurrentDatabaseIndex => m_CurrentDatabaseIndex;

        internal void BeginFrame(
            in CharacterMotionMatchingFrameContext frameContext,
            CharacterPoseHistoryReadView historyView,
            bool relevant)
        {
            RequireAlive();
            if (m_FrameOpen || !frameContext.IsValid || !historyView.IsValid ||
                frameContext.FrameIdentity != historyView.ReadFrameIdentity ||
                !frameContext.RigLineage.Equals(m_RigLineage) ||
                !historyView.RigLineage.Equals(m_RigLineage))
            {
                throw new InvalidOperationException($"Motion Matching Pose node '{NodeId}' frame inputs are stale or incompatible.");
            }
            for (int i = 0; i < m_Databases.Length; i++)
                m_Databases[i].Selection.BeginFrame();
            if (!relevant && m_Plan.RelevanceResetPolicy == CharacterMotionMatchingRelevanceResetPolicy.ResetOnRelevanceLoss)
                ResetMutableState(frameContext.ResetSequence, true);
            m_Blend.BeginFrame(frameContext.FrameIdentity, frameContext.DeltaTime);
            m_FrameContext = frameContext;
            m_HistoryView = historyView;
            m_Relevant = relevant;
            m_CurrentEvaluation = default;
            m_FrameOpen = true;
        }

        internal CharacterMotionMatchingPoseNodeEvaluation Evaluate()
        {
            RequireOpenFrame();
            if (!m_Relevant)
            {
                m_CurrentEvaluation = new CharacterMotionMatchingPoseNodeEvaluation(
                    CharacterMotionMatchingPoseNodeInvalidReason.RelevanceInactive);
                return m_CurrentEvaluation;
            }
            if (!m_HistoryView.IsValid || m_HistoryView.ReadFrameIdentity != m_FrameContext.FrameIdentity)
            {
                m_CurrentEvaluation = new CharacterMotionMatchingPoseNodeEvaluation(
                    CharacterMotionMatchingPoseNodeInvalidReason.HistoryViewInvalid);
                return m_CurrentEvaluation;
            }
            CharacterPresentationFactFrame facts = m_FrameContext.Facts;
            if (!m_Binding.Chooser.TryResolve(
                    in facts,
                    m_ChooserDatabaseIndices,
                    out m_ChooserResolution))
            {
                m_CurrentEvaluation = new CharacterMotionMatchingPoseNodeEvaluation(
                    CharacterMotionMatchingPoseNodeInvalidReason.ChooserInvalid,
                    m_ChooserResolution);
                return m_CurrentEvaluation;
            }

            int searchCount;
            try
            {
                searchCount = ResolveSearchDatabases(m_ChooserResolution.DatabaseCount);
            }
            catch
            {
                m_CurrentEvaluation = new CharacterMotionMatchingPoseNodeEvaluation(
                    CharacterMotionMatchingPoseNodeInvalidReason.ChooserDatabaseOutsideBinding,
                    m_ChooserResolution);
                return m_CurrentEvaluation;
            }
            int currentSearchIndex = FindSearchIndex(m_CurrentDatabaseIndex, searchCount);
            ulong chooserSignature = ComputeChooserSignature(m_ChooserResolution, searchCount);
            bool chooserChanged = m_HasChooserSignature && chooserSignature != m_ChooserSignature;
            if (chooserChanged && m_ChooserResolution.InterruptMode == CharacterMotionMatchingChooserInterruptMode.ResetEntry)
            {
                ResetMutableState(m_FrameContext.ResetSequence, true);
                currentSearchIndex = -1;
            }

            MotionMatchingSearchTriggerReason triggerReason =
                MotionMatchingSearchTriggerReason.DomainActivated;
            bool cadenceRequiresSearch = currentSearchIndex < 0;
            if (currentSearchIndex >= 0)
            {
                cadenceRequiresSearch = m_Databases[m_CurrentDatabaseIndex].Selection.RequiresSearch(
                    m_FrameContext.DeltaTime,
                    m_FrameContext.ResetSequence,
                    true,
                    out triggerReason);
            }
            bool interruptRequiresSearch = chooserChanged &&
                m_ChooserResolution.InterruptMode != CharacterMotionMatchingChooserInterruptMode.PreserveEntry;
            bool requiresSearch = cadenceRequiresSearch || interruptRequiresSearch ||
                                  m_Plan.SearchCadencePolicy == CharacterMotionMatchingSearchCadencePolicy.EveryPresentationFrame;
            if (!m_ChooserResolution.ShouldSearch)
                requiresSearch = false;

            if (!requiresSearch)
            {
                if (currentSearchIndex < 0 || !m_CurrentSelection.IsValid)
                {
                    m_CurrentEvaluation = new CharacterMotionMatchingPoseNodeEvaluation(
                        CharacterMotionMatchingPoseNodeInvalidReason.SearchDisabledWithoutSelection,
                        m_ChooserResolution);
                    return m_CurrentEvaluation;
                }
                m_CurrentSelection = m_Databases[m_CurrentDatabaseIndex].Selection.GetContinuationDecision();
                return PublishEvaluation(default, default);
            }

            MotionMatchingSelectionIdentity currentIdentity =
                m_CurrentSelection.IsValid ? m_CurrentSelection.SelectionIdentity : default;
            float secondsSinceLastJump = m_CurrentDatabaseIndex >= 0
                ? m_Databases[m_CurrentDatabaseIndex].Selection.SecondsSinceLastJump
                : 0f;
            MotionMatchingContactProtection contactProtection = BuildContactProtection();
            CharacterMotionMatchingQueryId queryId = NextQueryId();
            for (int searchIndex = 0; searchIndex < searchCount; searchIndex++)
            {
                int localIndex = m_SearchLocalIndices[searchIndex];
                DatabaseRuntime database = m_Databases[localIndex];
                database.Selection.PrepareDomain(m_FrameContext.ResetSequence);
                m_SearchQueries[searchIndex] = database.QueryBuilder.Build(
                    queryId,
                    m_Projection.MotionMatching.ProfileId,
                    m_FrameContext.Trajectory.Envelope,
                    m_HistoryView,
                    contactProtection,
                    currentIdentity,
                    secondsSinceLastJump,
                    m_FrameContext.ResetSequence);
            }
            if (triggerReason == default)
                triggerReason = MotionMatchingSearchTriggerReason.Cadence;
            MotionMatchingSelectionGeneration jumpGeneration = NextGenerationCandidate();
            CharacterMotionMatchingSearchKernelResult kernel =
                CharacterMotionMatchingSearchKernel.Evaluate(
                    m_SearchDatabases,
                    m_SearchQueries,
                    searchCount,
                    currentSearchIndex,
                    m_CurrentSelection.IsValid ? m_CurrentSelection.Generation : default,
                    jumpGeneration,
                    triggerReason);
            if (!kernel.IsValid)
            {
                ReleaseAllDatabases();
                m_CurrentDatabaseIndex = -1;
                m_CurrentSelection = new MotionMatchingSelectionDecision(
                    kernel.InvalidReason,
                    triggerReason);
                m_CurrentEvaluation = new CharacterMotionMatchingPoseNodeEvaluation(
                    CharacterMotionMatchingPoseNodeInvalidReason.SearchInvalid,
                    m_ChooserResolution);
                return m_CurrentEvaluation;
            }

            int winnerLocalIndex = m_SearchLocalIndices[kernel.DatabaseIndex];
            for (int localIndex = 0; localIndex < m_Databases.Length; localIndex++)
            {
                if (localIndex != winnerLocalIndex)
                    m_Databases[localIndex].Selection.ReleaseDomain();
            }
            DatabaseRuntime winner = m_Databases[winnerLocalIndex];
            m_CurrentSelection = winner.Selection.CommitKernelSelection(
                kernel.Query,
                kernel.TriggerReason,
                kernel.Search,
                kernel.Evaluation,
                kernel.Generation,
                kernel.DecisionKind);
            m_CurrentDatabaseIndex = winnerLocalIndex;
            if (kernel.Generation.Value > m_GenerationSequence)
                m_GenerationSequence = kernel.Generation.Value;
            m_ChooserSignature = chooserSignature;
            m_HasChooserSignature = true;
            return PublishEvaluation(kernel.Search, kernel.Evaluation);
        }

        internal void CompleteFrame(
            ulong frameIdentity,
            float outputWeight,
            float[] denseBoneOutputWeights)
        {
            RequireFrameIdentity(frameIdentity);
            if (!m_CurrentEvaluation.IsValid && m_Relevant)
                throw new InvalidOperationException($"Motion Matching Pose node '{NodeId}' cannot commit an invalid evaluation.");
            for (int i = 0; i < m_Databases.Length; i++)
                m_Databases[i].Selection.CommitFrame();
            if (m_Relevant)
                m_Blend.CompleteFrame(frameIdentity, outputWeight, denseBoneOutputWeights);
            else
                m_Blend.DiscardFrame(frameIdentity);
            ClearFrame();
        }

        internal void DiscardFrame(ulong frameIdentity)
        {
            RequireFrameIdentity(frameIdentity);
            for (int i = 0; i < m_Databases.Length; i++)
                m_Databases[i].Selection.DiscardFrame();
            m_Blend.DiscardFrame(frameIdentity);
            ClearFrame();
        }

        internal void Reset(ulong resetSequence)
        {
            RequireAlive();
            if (m_FrameOpen)
                throw new InvalidOperationException($"Motion Matching Pose node '{NodeId}' cannot reset while a frame is open.");
            ResetMutableState(resetSequence, true);
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            if (m_FrameOpen)
            {
                for (int i = 0; i < m_Databases.Length; i++)
                    m_Databases[i].Selection.DiscardFrame();
                m_Blend.DiscardFrame(m_FrameContext.FrameIdentity);
                ClearFrame();
            }
            DisposeDatabases();
            m_Disposed = true;
        }

        CharacterMotionMatchingPoseNodeEvaluation PublishEvaluation(
            MotionMatchingSearchResult search,
            MotionMatchingPlanEvaluationResult plan)
        {
            CharacterMotionMatchingEntrySourcePlan entry;
            try
            {
                entry = BuildEntrySourcePlan(
                    m_Databases[m_CurrentDatabaseIndex],
                    m_CurrentSelection,
                    m_FrameContext.FrameIdentity);
            }
            catch
            {
                m_CurrentEvaluation = new CharacterMotionMatchingPoseNodeEvaluation(
                    CharacterMotionMatchingPoseNodeInvalidReason.SourcePlanInvalid,
                    m_ChooserResolution);
                return m_CurrentEvaluation;
            }
            try
            {
                CharacterMotionMatchingBlendFramePlan blend = m_Blend.Apply(
                    in entry,
                    m_CurrentSelection.Kind);
                m_CurrentEvaluation = new CharacterMotionMatchingPoseNodeEvaluation(
                    entry,
                    m_CurrentSelection,
                    m_ChooserResolution,
                    search,
                    plan,
                    blend);
            }
            catch
            {
                m_CurrentEvaluation = new CharacterMotionMatchingPoseNodeEvaluation(
                    CharacterMotionMatchingPoseNodeInvalidReason.BlendInvalid,
                    m_ChooserResolution);
            }
            return m_CurrentEvaluation;
        }

        CharacterMotionMatchingEntrySourcePlan BuildEntrySourcePlan(
            DatabaseRuntime runtime,
            MotionMatchingSelectionDecision selection,
            ulong frameIdentity)
        {
            MotionMatchingSamplePayload sample = runtime.Database.GetSample(selection.SampleIndex);
            MotionMatchingClipBindingPayload clip = runtime.Database.GetClipBinding(sample.ClipBindingIndex);
            if (clip == null || !clip.RootLocked || !clip.Clip ||
                clip.FootPlacementWeightCurve == null ||
                !clip.FootPlacementWeightCurve.ParameterId.Equals(MotionMatchingPoseSourceParameterContract.FootPlacementWeightId))
            {
                throw new InvalidOperationException("Motion Matching selected sample has no valid root-locked Clip and Foot parameter binding.");
            }
            MotionMatchingClipDependencyIdentity dependency = RequireClipDependency(
                runtime.Database.ArtifactIdentity,
                clip.SourceClipId);
            var clipSample = new MotionMatchingClipSamplePlan(
                clip.SourceClipId,
                sample.ClipBindingIndex,
                clip.Clip,
                selection.PoseTime,
                true);
            var footWeight = new MotionMatchingPoseParameterSample(
                MotionMatchingPoseSourceParameterContract.FootPlacementWeightId,
                clip.FootPlacementWeightCurve.Sample(clipSample.NormalizedTime));
            var footPlacement = new AnimationFootPlacementSample(
                footWeight.Value,
                sample.LeftFoot.BindPredictionSource(
                    AnimationPredictedFootStepSample.SourceIdentity(clip.SourceClipId.Value),
                    selection.PoseTime.Cycle),
                sample.RightFoot.BindPredictionSource(
                    AnimationPredictedFootStepSample.SourceIdentity(clip.SourceClipId.Value),
                    selection.PoseTime.Cycle));
            var sourceLineage = new CharacterMotionMatchingSourceLineage(
                NodeId,
                m_Binding.BindingId,
                m_Binding.BindingRevision,
                m_Projection.MotionMatching.ProfileId,
                m_Projection.MotionMatching.ProfileRevision,
                m_Binding.ChooserId,
                m_Binding.ChooserRevision,
                runtime.Database.ArtifactIdentity,
                dependency.SourceSetId,
                dependency.SourceSetRevision,
                clip.SourceClipId,
                sample.SegmentId,
                sample.SampleId,
                selection.PoseTime.SampleTime,
                selection.Generation);
            var identity = new CharacterMotionMatchingEntryIdentity(
                NodeId,
                m_EntryGraphId,
                sourceLineage,
                selection.Plan.PlanId,
                m_RigLineage);
            return new CharacterMotionMatchingEntrySourcePlan(
                identity,
                frameIdentity,
                clipSample,
                footWeight,
                footPlacement);
        }

        int ResolveSearchDatabases(int chooserDatabaseCount)
        {
            if (chooserDatabaseCount <= 0 || chooserDatabaseCount > m_ChooserDatabaseIndices.Length)
                throw new InvalidOperationException("Motion Matching Chooser result exceeds the node workspace.");
            for (int searchIndex = 0; searchIndex < chooserDatabaseCount; searchIndex++)
            {
                int globalIndex = m_ChooserDatabaseIndices[searchIndex];
                int localIndex = globalIndex - m_Binding.FirstDatabaseIndex;
                if ((uint)localIndex >= (uint)m_Databases.Length ||
                    m_Databases[localIndex].GlobalDatabaseIndex != globalIndex)
                {
                    throw new InvalidOperationException("Motion Matching Chooser selected a Database outside the node binding.");
                }
                m_SearchLocalIndices[searchIndex] = localIndex;
                m_SearchDatabases[searchIndex] = m_Databases[localIndex].Database;
            }
            return chooserDatabaseCount;
        }

        int FindSearchIndex(int localDatabaseIndex, int searchCount)
        {
            if (localDatabaseIndex < 0)
                return -1;
            for (int searchIndex = 0; searchIndex < searchCount; searchIndex++)
            {
                if (m_SearchLocalIndices[searchIndex] == localDatabaseIndex)
                    return searchIndex;
            }
            return -1;
        }

        MotionMatchingContactProtection BuildContactProtection()
        {
            if (!m_CurrentSelection.IsValid || m_CurrentDatabaseIndex < 0)
                return new MotionMatchingContactProtection(
                    MotionMatchingFootContactMask.None,
                    Vector3.zero,
                    Vector3.zero,
                    Vector3.zero,
                    Vector3.zero);
            MotionMatchingSamplePayload sample =
                m_Databases[m_CurrentDatabaseIndex].Database.GetSample(
                    m_CurrentSelection.SampleIndex);
            return new MotionMatchingContactProtection(
                sample.ContactMask,
                sample.LeftFootRootPosition,
                sample.RightFootRootPosition,
                sample.LeftFoot.SoleLocalVelocity,
                sample.RightFoot.SoleLocalVelocity);
        }

        void ResetMutableState(ulong resetSequence, bool clearChooser)
        {
            for (int i = 0; i < m_Databases.Length; i++)
                m_Databases[i].Selection.Reset(resetSequence);
            m_CurrentSelection = default;
            m_CurrentDatabaseIndex = -1;
            if (m_FrameOpen)
                m_Blend.ResetFrame();
            else
                m_Blend.Reset();
            if (clearChooser)
            {
                m_HasChooserSignature = false;
                m_ChooserSignature = 0;
            }
        }

        void ReleaseAllDatabases()
        {
            for (int i = 0; i < m_Databases.Length; i++)
                m_Databases[i].Selection.ReleaseDomain();
        }

        CharacterMotionMatchingQueryId NextQueryId()
        {
            if (m_QuerySequence == ulong.MaxValue)
                throw new InvalidOperationException($"Motion Matching Pose node '{NodeId}' Query identity was exhausted.");
            return new CharacterMotionMatchingQueryId(++m_QuerySequence);
        }

        MotionMatchingSelectionGeneration NextGenerationCandidate()
        {
            if (m_GenerationSequence == ulong.MaxValue)
                throw new InvalidOperationException($"Motion Matching Pose node '{NodeId}' Selection generation was exhausted.");
            return new MotionMatchingSelectionGeneration(m_GenerationSequence + 1);
        }

        ulong ComputeChooserSignature(
            in CharacterMotionMatchingDatabaseChooserResolution resolution,
            int searchCount)
        {
            ulong hash = 1469598103934665603UL;
            Mix((ulong)(uint)resolution.FirstRuleIndex);
            Mix((ulong)(uint)resolution.MatchedRuleCount);
            Mix(resolution.ShouldSearch ? 1UL : 0UL);
            Mix((ulong)resolution.InterruptMode);
            for (int i = 0; i < searchCount; i++)
                Mix((ulong)(uint)m_ChooserDatabaseIndices[i]);
            return hash;

            void Mix(ulong value)
            {
                hash ^= value;
                hash *= 1099511628211UL;
            }
        }

        void ClearFrame()
        {
            m_FrameContext = default;
            m_HistoryView = default;
            m_Relevant = false;
            m_FrameOpen = false;
        }

        void RequireFrameIdentity(ulong frameIdentity)
        {
            RequireOpenFrame();
            if (frameIdentity == 0 || frameIdentity != m_FrameContext.FrameIdentity)
                throw new InvalidOperationException($"Motion Matching Pose node '{NodeId}' frame identity is stale.");
        }

        void RequireOpenFrame()
        {
            RequireAlive();
            if (!m_FrameOpen)
                throw new InvalidOperationException($"Motion Matching Pose node '{NodeId}' has no open frame.");
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(CharacterMotionMatchingPoseNodeRuntime));
        }

        void DisposeDatabases()
        {
            if (m_Databases == null)
                return;
            for (int i = m_Databases.Length - 1; i >= 0; i--)
                m_Databases[i]?.Database.Dispose();
        }

        static MotionMatchingNodeBindingPayload ResolveBinding(
            MotionMatchingProjectionPayload payload,
            PoseNodeId nodeId)
        {
            if (payload == null)
                throw new ArgumentNullException(nameof(payload));
            MotionMatchingNodeBindingPayload result = default;
            int count = 0;
            for (int i = 0; i < payload.NodeBindingCount; i++)
            {
                MotionMatchingNodeBindingPayload candidate = payload.GetNodeBinding(i);
                if (candidate.PoseNodeId != nodeId)
                    continue;
                result = candidate;
                count++;
            }
            return count == 1
                ? result
                : throw new InvalidOperationException($"Motion Matching Pose node '{nodeId}' does not resolve to one Projection binding.");
        }

        static MotionMatchingClipDependencyIdentity RequireClipDependency(
            CharacterMotionMatchingDatabaseArtifactIdentity artifact,
            CharacterMotionMatchingSourceClipId sourceClipId)
        {
            for (int i = 0; i < artifact.ClipDependencyCount; i++)
            {
                MotionMatchingClipDependencyIdentity dependency = artifact.GetClipDependency(i);
                if (dependency.SourceClipId.Equals(sourceClipId))
                    return dependency;
            }
            throw new InvalidOperationException($"Motion Matching Artifact has no Clip dependency '{sourceClipId}'.");
        }
    }
}
