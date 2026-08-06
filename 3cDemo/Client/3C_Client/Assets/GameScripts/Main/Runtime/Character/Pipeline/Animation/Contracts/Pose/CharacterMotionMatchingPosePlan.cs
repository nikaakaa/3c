using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    [Serializable]
    public sealed class CharacterPoseHistoryCollectorPlanDescriptor
    {
        [SerializeField] string m_NodeId = string.Empty;
        [SerializeField] string m_HistoryId = string.Empty;
        [SerializeField] int m_InputPoseValueIndex = -1;
        [SerializeField] int m_OutputPoseValueIndex = -1;
        [SerializeField] int m_HistoryValueIndex = -1;
        [SerializeField] int m_HistoryCapacity;
        [SerializeField] int m_ReadOperationIndex = -1;
        [SerializeField] int m_CommitOperationIndex = -1;

        public CharacterPoseHistoryCollectorPlanDescriptor(
            PoseNodeId nodeId,
            CharacterPoseHistoryId historyId,
            int inputPoseValueIndex,
            int outputPoseValueIndex,
            int historyValueIndex,
            int historyCapacity,
            int readOperationIndex,
            int commitOperationIndex)
        {
            if (!nodeId.IsValid || !historyId.IsValid || inputPoseValueIndex < 0 || outputPoseValueIndex < 0 ||
                historyValueIndex < 0 || historyCapacity <= 0 || readOperationIndex < 0 || commitOperationIndex <= readOperationIndex)
            {
                throw new ArgumentException("Pose History Collector plan descriptor is invalid.");
            }
            m_NodeId = nodeId.Value;
            m_HistoryId = historyId.Value;
            m_InputPoseValueIndex = inputPoseValueIndex;
            m_OutputPoseValueIndex = outputPoseValueIndex;
            m_HistoryValueIndex = historyValueIndex;
            m_HistoryCapacity = historyCapacity;
            m_ReadOperationIndex = readOperationIndex;
            m_CommitOperationIndex = commitOperationIndex;
        }

        public PoseNodeId NodeId => new PoseNodeId(m_NodeId);
        public CharacterPoseHistoryId HistoryId => new CharacterPoseHistoryId(m_HistoryId);
        public int InputPoseValueIndex => m_InputPoseValueIndex;
        public int OutputPoseValueIndex => m_OutputPoseValueIndex;
        public int HistoryValueIndex => m_HistoryValueIndex;
        public int HistoryCapacity => m_HistoryCapacity;
        public int ReadOperationIndex => m_ReadOperationIndex;
        public int CommitOperationIndex => m_CommitOperationIndex;
    }

    [Serializable]
    public sealed class CharacterMotionMatchingEntryProgramDescriptor
    {
        [SerializeField] string m_GraphId = string.Empty;
        [SerializeField] int m_OperationStart;
        [SerializeField] int m_OperationCount;
        [SerializeField] int m_StateCapacity;

        public CharacterMotionMatchingEntryProgramDescriptor(
            PoseGraphId graphId,
            int operationStart,
            int operationCount,
            int stateCapacity)
        {
            if (!graphId.IsValid || operationStart < 0 || operationCount < 0 || stateCapacity < 0)
                throw new ArgumentException("Motion Matching entry program descriptor is invalid.");
            m_GraphId = graphId.Value;
            m_OperationStart = operationStart;
            m_OperationCount = operationCount;
            m_StateCapacity = stateCapacity;
        }

        public PoseGraphId GraphId => new PoseGraphId(m_GraphId);
        public int OperationStart => m_OperationStart;
        public int OperationCount => m_OperationCount;
        public int StateCapacity => m_StateCapacity;
    }

    [Serializable]
    public sealed class CharacterMotionMatchingBlendPlanDescriptor
    {
        [SerializeField] string m_PolicyId = string.Empty;
        [SerializeField] string m_PolicyRevision = string.Empty;
        [SerializeField] AnimationBlendStackPolicyPayload m_StackPolicy;
        [SerializeField] float m_JumpDurationSeconds;
        [SerializeField] int m_CurveIndex = -1;
        [SerializeField] int m_ProfileIndex = -1;

        public CharacterMotionMatchingBlendPlanDescriptor(
            string policyId,
            string policyRevision,
            AnimationBlendStackPolicyPayload stackPolicy,
            float jumpDurationSeconds,
            int curveIndex,
            int profileIndex)
        {
            if (string.IsNullOrWhiteSpace(policyId) || string.IsNullOrWhiteSpace(policyRevision) ||
                stackPolicy == null || stackPolicy.StoredPosePolicy != AnimationStoredPosePolicy.CompressOldest ||
                !float.IsFinite(jumpDurationSeconds) || jumpDurationSeconds < 0f ||
                curveIndex < 0 || profileIndex < 0)
            {
                throw new ArgumentException("Motion Matching Blend plan descriptor is invalid.");
            }
            stackPolicy.RequireValid();
            m_PolicyId = policyId;
            m_PolicyRevision = policyRevision;
            m_StackPolicy = stackPolicy;
            m_JumpDurationSeconds = jumpDurationSeconds;
            m_CurveIndex = curveIndex;
            m_ProfileIndex = profileIndex;
        }

        public string PolicyId => m_PolicyId ?? string.Empty;
        public string PolicyRevision => m_PolicyRevision ?? string.Empty;
        public AnimationBlendStackPolicyPayload StackPolicy => m_StackPolicy;
        public float JumpDurationSeconds => m_JumpDurationSeconds;
        public int CurveIndex => m_CurveIndex;
        public int ProfileIndex => m_ProfileIndex;
    }

    [Serializable]
    public sealed class CharacterMotionMatchingPosePlanDescriptor
    {
        [SerializeField] string m_NodeId = string.Empty;
        [SerializeField] string m_BindingId = string.Empty;
        [SerializeField] int m_BindingRevision;
        [SerializeField] string m_ProfileId = string.Empty;
        [SerializeField] int m_ProfileRevision;
        [SerializeField] string m_ChooserId = string.Empty;
        [SerializeField] int m_ChooserRevision;
        [SerializeField] string m_SearchDomainId = string.Empty;
        [SerializeField] int m_FirstDatabaseIndex;
        [SerializeField] int m_DatabaseCount;
        [SerializeField] int m_CollectorIndex = -1;
        [SerializeField] int m_EntryProgramIndex = -1;
        [SerializeField] int m_BlendPlanIndex = -1;
        [SerializeField] int m_OutputPoseValueIndex = -1;
        [SerializeField] int m_CandidateCapacity;
        [SerializeField] int m_FeatureCapacity;
        [SerializeField] int m_LiveEntryCapacity;
        [SerializeField] int m_StoredPoseCapacity;
        [SerializeField] int m_DiagnosticCapacity;
        [SerializeField] CharacterMotionMatchingRelevanceResetPolicy m_RelevanceResetPolicy;
        [SerializeField] CharacterMotionMatchingSearchCadencePolicy m_SearchCadencePolicy;

        public CharacterMotionMatchingPosePlanDescriptor(
            PoseNodeId nodeId,
            CharacterMotionMatchingBindingId bindingId,
            int bindingRevision,
            CharacterMotionMatchingProfileId profileId,
            int profileRevision,
            CharacterMotionMatchingDatabaseChooserId chooserId,
            int chooserRevision,
            CharacterMotionMatchingSearchDomainId searchDomainId,
            int firstDatabaseIndex,
            int databaseCount,
            int collectorIndex,
            int entryProgramIndex,
            int blendPlanIndex,
            int outputPoseValueIndex,
            int candidateCapacity,
            int featureCapacity,
            int liveEntryCapacity,
            int storedPoseCapacity,
            int diagnosticCapacity,
            CharacterMotionMatchingRelevanceResetPolicy relevanceResetPolicy,
            CharacterMotionMatchingSearchCadencePolicy searchCadencePolicy)
        {
            if (!nodeId.IsValid || !bindingId.IsValid || bindingRevision <= 0 || !profileId.IsValid || profileRevision <= 0 ||
                !chooserId.IsValid || chooserRevision <= 0 || !searchDomainId.IsValid || firstDatabaseIndex < 0 || databaseCount <= 0 ||
                collectorIndex < 0 || entryProgramIndex < 0 || blendPlanIndex < 0 || outputPoseValueIndex < 0 ||
                candidateCapacity <= 0 || featureCapacity <= 0 || liveEntryCapacity <= 0 || storedPoseCapacity <= 0 || diagnosticCapacity < 0 ||
                !Enum.IsDefined(typeof(CharacterMotionMatchingRelevanceResetPolicy), relevanceResetPolicy) ||
                !Enum.IsDefined(typeof(CharacterMotionMatchingSearchCadencePolicy), searchCadencePolicy))
            {
                throw new ArgumentException("Motion Matching Pose plan descriptor is invalid.");
            }
            m_NodeId = nodeId.Value;
            m_BindingId = bindingId.Value;
            m_BindingRevision = bindingRevision;
            m_ProfileId = profileId.Value;
            m_ProfileRevision = profileRevision;
            m_ChooserId = chooserId.Value;
            m_ChooserRevision = chooserRevision;
            m_SearchDomainId = searchDomainId.Value;
            m_FirstDatabaseIndex = firstDatabaseIndex;
            m_DatabaseCount = databaseCount;
            m_CollectorIndex = collectorIndex;
            m_EntryProgramIndex = entryProgramIndex;
            m_BlendPlanIndex = blendPlanIndex;
            m_OutputPoseValueIndex = outputPoseValueIndex;
            m_CandidateCapacity = candidateCapacity;
            m_FeatureCapacity = featureCapacity;
            m_LiveEntryCapacity = liveEntryCapacity;
            m_StoredPoseCapacity = storedPoseCapacity;
            m_DiagnosticCapacity = diagnosticCapacity;
            m_RelevanceResetPolicy = relevanceResetPolicy;
            m_SearchCadencePolicy = searchCadencePolicy;
        }

        public PoseNodeId NodeId => new PoseNodeId(m_NodeId);
        public CharacterMotionMatchingBindingId BindingId => new CharacterMotionMatchingBindingId(m_BindingId);
        public int BindingRevision => m_BindingRevision;
        public CharacterMotionMatchingProfileId ProfileId => new CharacterMotionMatchingProfileId(m_ProfileId);
        public int ProfileRevision => m_ProfileRevision;
        public CharacterMotionMatchingDatabaseChooserId ChooserId => new CharacterMotionMatchingDatabaseChooserId(m_ChooserId);
        public int ChooserRevision => m_ChooserRevision;
        public CharacterMotionMatchingSearchDomainId SearchDomainId => new CharacterMotionMatchingSearchDomainId(m_SearchDomainId);
        public int FirstDatabaseIndex => m_FirstDatabaseIndex;
        public int DatabaseCount => m_DatabaseCount;
        public int CollectorIndex => m_CollectorIndex;
        public int EntryProgramIndex => m_EntryProgramIndex;
        public int BlendPlanIndex => m_BlendPlanIndex;
        public int OutputPoseValueIndex => m_OutputPoseValueIndex;
        public int CandidateCapacity => m_CandidateCapacity;
        public int FeatureCapacity => m_FeatureCapacity;
        public int LiveEntryCapacity => m_LiveEntryCapacity;
        public int StoredPoseCapacity => m_StoredPoseCapacity;
        public int DiagnosticCapacity => m_DiagnosticCapacity;
        public CharacterMotionMatchingRelevanceResetPolicy RelevanceResetPolicy => m_RelevanceResetPolicy;
        public CharacterMotionMatchingSearchCadencePolicy SearchCadencePolicy => m_SearchCadencePolicy;
    }

    public sealed partial class CharacterPresentationPosePlan
    {
        [SerializeField] CharacterMotionMatchingPosePlanDescriptor[] m_MotionMatchingNodes = Array.Empty<CharacterMotionMatchingPosePlanDescriptor>();
        [SerializeField] CharacterPoseHistoryCollectorPlanDescriptor[] m_PoseHistoryCollectors = Array.Empty<CharacterPoseHistoryCollectorPlanDescriptor>();
        [SerializeField] CharacterMotionMatchingEntryProgramDescriptor[] m_MotionMatchingEntryPrograms = Array.Empty<CharacterMotionMatchingEntryProgramDescriptor>();
        [SerializeField] CharacterMotionMatchingBlendPlanDescriptor[] m_MotionMatchingBlendPlans = Array.Empty<CharacterMotionMatchingBlendPlanDescriptor>();

        public IReadOnlyList<CharacterMotionMatchingPosePlanDescriptor> MotionMatchingNodes => m_MotionMatchingNodes ?? Array.Empty<CharacterMotionMatchingPosePlanDescriptor>();
        public IReadOnlyList<CharacterPoseHistoryCollectorPlanDescriptor> PoseHistoryCollectors => m_PoseHistoryCollectors ?? Array.Empty<CharacterPoseHistoryCollectorPlanDescriptor>();
        public IReadOnlyList<CharacterMotionMatchingEntryProgramDescriptor> MotionMatchingEntryPrograms => m_MotionMatchingEntryPrograms ?? Array.Empty<CharacterMotionMatchingEntryProgramDescriptor>();
        public IReadOnlyList<CharacterMotionMatchingBlendPlanDescriptor> MotionMatchingBlendPlans => m_MotionMatchingBlendPlans ?? Array.Empty<CharacterMotionMatchingBlendPlanDescriptor>();

        internal void ConfigureMotionMatching(
            CharacterMotionMatchingPosePlanDescriptor[] nodes,
            CharacterPoseHistoryCollectorPlanDescriptor[] collectors,
            CharacterMotionMatchingEntryProgramDescriptor[] entryPrograms,
            CharacterMotionMatchingBlendPlanDescriptor[] blendPlans)
        {
            m_MotionMatchingNodes = nodes ?? Array.Empty<CharacterMotionMatchingPosePlanDescriptor>();
            m_PoseHistoryCollectors = collectors ?? Array.Empty<CharacterPoseHistoryCollectorPlanDescriptor>();
            m_MotionMatchingEntryPrograms = entryPrograms ?? Array.Empty<CharacterMotionMatchingEntryProgramDescriptor>();
            m_MotionMatchingBlendPlans = blendPlans ?? Array.Empty<CharacterMotionMatchingBlendPlanDescriptor>();
            int contributionCapacity = 0;
            var revision = new List<string>
            {
                m_PlanHash,
                "motion-matching-pose-plan/v1"
            };
            for (int i = 0; i < m_MotionMatchingNodes.Length; i++)
            {
                CharacterMotionMatchingPosePlanDescriptor node = m_MotionMatchingNodes[i] ??
                    throw new InvalidOperationException($"Motion Matching Pose plan #{i} is missing.");
                contributionCapacity = checked(contributionCapacity + node.LiveEntryCapacity + node.StoredPoseCapacity);
                revision.Add($"node:{node.NodeId}:{node.BindingId}:{node.BindingRevision}:{node.ProfileId}:{node.ProfileRevision}:{node.ChooserId}:{node.ChooserRevision}:{node.SearchDomainId}:{node.FirstDatabaseIndex}:{node.DatabaseCount}:{node.CollectorIndex}:{node.EntryProgramIndex}:{node.BlendPlanIndex}:{node.OutputPoseValueIndex}:{node.CandidateCapacity}:{node.FeatureCapacity}:{node.LiveEntryCapacity}:{node.StoredPoseCapacity}:{node.DiagnosticCapacity}:{(int)node.RelevanceResetPolicy}:{(int)node.SearchCadencePolicy}");
            }
            for (int i = 0; i < m_MotionMatchingBlendPlans.Length; i++)
            {
                CharacterMotionMatchingBlendPlanDescriptor blend = m_MotionMatchingBlendPlans[i] ??
                    throw new InvalidOperationException($"Motion Matching Blend plan #{i} is missing.");
                revision.Add($"blend:{blend.PolicyId}:{blend.PolicyRevision}:{blend.StackPolicy.MaxActiveSourceEntries}:{(int)blend.StackPolicy.StoredPosePolicy}:{blend.StackPolicy.MaxBlendInTimeToReplaceNewest:R}:{blend.StackPolicy.DepthBlendTimeMultiplier:R}:{blend.JumpDurationSeconds:R}:{blend.CurveIndex}:{blend.ProfileIndex}");
            }
            m_ContributionWorkspaceCount = checked(m_ContributionWorkspaceCount + contributionCapacity);
            m_PlanHash = ThirdPersonSimulation.StableHash.Compute(string.Join("|", revision)).ToString();
            RequireMotionMatchingPlan();
        }

        public void RequireMotionMatchingPlan()
        {
            if (MotionMatchingNodes.Count == 0)
            {
                if (PoseHistoryCollectors.Count != 0 || MotionMatchingEntryPrograms.Count != 0 || MotionMatchingBlendPlans.Count != 0)
                    throw new InvalidOperationException("Pose Plan has Motion Matching support records without a Motion Matching node.");
                return;
            }
            var nodeIds = new HashSet<PoseNodeId>();
            var historyIds = new HashSet<CharacterPoseHistoryId>();
            for (int i = 0; i < PoseHistoryCollectors.Count; i++)
            {
                CharacterPoseHistoryCollectorPlanDescriptor collector = PoseHistoryCollectors[i] ??
                    throw new InvalidOperationException($"Pose History Collector plan #{i} is missing.");
                if (!historyIds.Add(collector.HistoryId))
                    throw new InvalidOperationException($"Pose History identity '{collector.HistoryId}' is duplicated.");
            }
            for (int i = 0; i < MotionMatchingNodes.Count; i++)
            {
                CharacterMotionMatchingPosePlanDescriptor node = MotionMatchingNodes[i] ??
                    throw new InvalidOperationException($"Motion Matching Pose plan #{i} is missing.");
                if (!nodeIds.Add(node.NodeId) || node.CollectorIndex >= PoseHistoryCollectors.Count ||
                    node.EntryProgramIndex >= MotionMatchingEntryPrograms.Count || node.BlendPlanIndex >= MotionMatchingBlendPlans.Count)
                    throw new InvalidOperationException($"Motion Matching Pose plan #{i} has invalid ownership indices.");
            }
        }
    }
}
