using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Presentation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public sealed partial class CharacterPresentationProjection
    {
        [SerializeField] CharacterPresentationPosePlan m_PosePlan;
        [SerializeField] AnimationBlendCurveCatalogPayload m_BlendCurveCatalog;
        [SerializeField] AnimationBlendProfileCatalogPayload m_BlendProfileCatalog;
        [SerializeField] CharacterAnimationRigPayload m_Rig;
        [SerializeField] CharacterPresentationPoseSourcePlan[] m_PoseSources = Array.Empty<CharacterPresentationPoseSourcePlan>();
        [SerializeField] CharacterAnimationBlendSpacePlan[] m_BlendSpaces = Array.Empty<CharacterAnimationBlendSpacePlan>();
        [SerializeField] CharacterAnimationBlendSpacePlayerPlan[] m_BlendSpacePlayers = Array.Empty<CharacterAnimationBlendSpacePlayerPlan>();
        [SerializeField] ActionAnimationFootPhaseTimeWarpPlan[] m_ActionFootPhaseWarps =
            Array.Empty<ActionAnimationFootPhaseTimeWarpPlan>();
        [SerializeField] CharacterPoseTuningLayout m_TuningLayout;
        [SerializeField] CharacterPoseTuningParameterBlock m_TuningDefaultBlock;
        [SerializeField] string m_PublishedParameterRevision = string.Empty;
        [NonSerialized] MotionMatchingProjectionPayload m_MotionMatching;
        [SerializeField] byte[] m_MotionMatchingPayload = Array.Empty<byte>();
        [SerializeField] UnityEngine.AnimationClip[] m_MotionMatchingClips = Array.Empty<UnityEngine.AnimationClip>();

        public CharacterPresentationPosePlan PosePlan => m_PosePlan;
        public AnimationBlendCurveCatalogPayload BlendCurveCatalog => m_BlendCurveCatalog;
        public AnimationBlendProfileCatalogPayload BlendProfileCatalog => m_BlendProfileCatalog;
        public CharacterAnimationRigPayload Rig => m_Rig;
        public IReadOnlyList<CharacterPresentationPoseSourcePlan> PoseSources =>
            m_PoseSources ?? Array.Empty<CharacterPresentationPoseSourcePlan>();
        public IReadOnlyList<CharacterAnimationBlendSpacePlan> BlendSpaces => m_BlendSpaces ?? Array.Empty<CharacterAnimationBlendSpacePlan>();
        public IReadOnlyList<CharacterAnimationBlendSpacePlayerPlan> BlendSpacePlayers => m_BlendSpacePlayers ?? Array.Empty<CharacterAnimationBlendSpacePlayerPlan>();
        public IReadOnlyList<ActionAnimationFootPhaseTimeWarpPlan> ActionFootPhaseWarps =>
            m_ActionFootPhaseWarps ?? Array.Empty<ActionAnimationFootPhaseTimeWarpPlan>();
        public MotionMatchingProjectionPayload MotionMatching => m_MotionMatching;
        public CharacterPoseTuningLayout TuningLayout => m_TuningLayout;
        public CharacterPoseTuningParameterBlock TuningDefaultBlock => m_TuningDefaultBlock;
        public string PublishedParameterRevision => m_PublishedParameterRevision ?? string.Empty;

        public bool TryGetPoseSource(
            PresentationPoseSourceIndex sourceIndex,
            out CharacterPresentationPoseSourcePlan source)
        {
            for (int i = 0; i < PoseSources.Count; i++)
            {
                CharacterPresentationPoseSourcePlan candidate = PoseSources[i];
                if (candidate != null && candidate.SourceIndex == sourceIndex)
                {
                    source = candidate;
                    return true;
                }
            }
            source = null;
            return false;
        }

        public bool TryGetPoseSource(
            string sequenceAuthoringId,
            string sequenceContentRevision,
            out CharacterPresentationPoseSourcePlan source)
        {
            for (int i = 0; i < PoseSources.Count; i++)
            {
                CharacterPresentationPoseSourcePlan candidate = PoseSources[i];
                if (candidate != null &&
                    string.Equals(candidate.SequenceAuthoringId, sequenceAuthoringId, StringComparison.Ordinal) &&
                    string.Equals(candidate.SequenceContentRevision, sequenceContentRevision, StringComparison.Ordinal))
                {
                    source = candidate;
                    return true;
                }
            }
            source = null;
            return false;
        }

        public void OnBeforeSerialize()
        {
            m_MotionMatchingPayload = MotionMatchingProjectionPayloadCodec.Encode(
                m_MotionMatching,
                out m_MotionMatchingClips);
        }

        public void OnAfterDeserialize()
        {
            m_MotionMatching = MotionMatchingProjectionPayloadCodec.Decode(
                m_MotionMatchingPayload,
                m_MotionMatchingClips);
        }

        public AnimationBlendNodePayload RequireBlendNode(PoseNodeId nodeId) => PosePlan.RequireBlendNode(nodeId);

        internal static CharacterPresentationProjection Create(
            CharacterPresentationSemanticContract contract,
            CharacterPresentationPosePlan posePlan,
            AnimationBlendCurveCatalogPayload blendCurveCatalog,
            AnimationBlendProfileCatalogPayload blendProfileCatalog,
            CharacterAnimationRigPayload rig,
            MotionMatchingProjectionPayload motionMatching,
            CharacterPresentationPoseSourcePlan[] poseSources,
            CharacterAnimationBlendSpacePlan[] blendSpaces,
            CharacterAnimationBlendSpacePlayerPlan[] blendSpacePlayers,
            ActionAnimationFootPhaseTimeWarpPlan[] actionFootPhaseWarps,
            CharacterPresentationProducerEntry[] producers,
            AnimationFootAnalysisProjectionIdentity footAnalysis,
            string projectionRevision,
            EquipmentVisualProjectionBinding[] equipmentVisualBindings,
            CharacterLinkedPoseProjectionPayload linkedPose,
            CharacterPoseTuningLayout tuningLayout = null,
            CharacterPoseTuningParameterBlock tuningDefaultBlock = null,
            string publishedParameterRevision = "")
        {
            if (contract == null)
                throw new ArgumentNullException(nameof(contract));
            var projection = new CharacterPresentationProjection
            {
                m_AbiVersion = CurrentAbiVersion,
                m_ProgramId = contract.ProgramId.Value,
                m_SourceRevision = contract.SourceRevision.Value,
                m_SemanticHash = contract.SemanticHash.ToString(),
                m_ContractHash = contract.ContractHash.ToString(),
                m_PosePlan = posePlan ?? throw new ArgumentNullException(nameof(posePlan)),
                m_BlendCurveCatalog = blendCurveCatalog ?? throw new ArgumentNullException(nameof(blendCurveCatalog)),
                m_BlendProfileCatalog = blendProfileCatalog ?? throw new ArgumentNullException(nameof(blendProfileCatalog)),
                m_Rig = rig ?? throw new ArgumentNullException(nameof(rig)),
                m_MotionMatching = motionMatching,
                m_PoseSources = poseSources ?? Array.Empty<CharacterPresentationPoseSourcePlan>(),
                m_BlendSpaces = blendSpaces ?? Array.Empty<CharacterAnimationBlendSpacePlan>(),
                m_BlendSpacePlayers = blendSpacePlayers ?? Array.Empty<CharacterAnimationBlendSpacePlayerPlan>(),
                m_ActionFootPhaseWarps = actionFootPhaseWarps ?? Array.Empty<ActionAnimationFootPhaseTimeWarpPlan>(),
                m_Producers = producers ?? Array.Empty<CharacterPresentationProducerEntry>(),
                m_FootAnalysis = footAnalysis,
                m_TuningLayout = tuningLayout,
                m_TuningDefaultBlock = tuningDefaultBlock,
                m_PublishedParameterRevision = publishedParameterRevision ?? string.Empty
            };
            projection.SetEquipmentProjection(
                projectionRevision,
                equipmentVisualBindings);
            projection.SetLinkedPoseProjection(linkedPose);
            projection.RequireContract(contract);
            projection.RequirePosePayload();
            return projection;
        }

        public void RequireTuningPayload()
        {
            if (TuningLayout == null || TuningDefaultBlock == null ||
                string.IsNullOrWhiteSpace(PublishedParameterRevision))
                throw new InvalidOperationException("Character Presentation Projection tuning payload is incomplete.");
            TuningLayout.RequireValid();
            TuningDefaultBlock.RequireValid(TuningLayout);
            if (!string.Equals(TuningLayout.ProgramId, ProgramId, StringComparison.Ordinal) ||
                !string.Equals(TuningLayout.ProjectionRevision, ProjectionRevision, StringComparison.Ordinal) ||
                !string.Equals(TuningLayout.PosePlanHash, PosePlan.PlanHash, StringComparison.Ordinal) ||
                !string.Equals(TuningLayout.RigId, Rig.RigId, StringComparison.Ordinal) ||
                !string.Equals(TuningLayout.RigRevision, Rig.RigRevision, StringComparison.Ordinal))
                throw new InvalidOperationException("Character Presentation Projection tuning identity is stale.");
            RequireStructuralTuningPayload();
        }

        void RequireStructuralTuningPayload()
        {
            for (int profileIndex = 0; profileIndex < PosePlan.FootGroundings.Count; profileIndex++)
            {
                CharacterPresentationFootGroundingDescriptor descriptor =
                    PosePlan.FootGroundings[profileIndex];
                CharacterFootLandingPredictionSettings landing =
                    descriptor.Profile.LandingPrediction.Build();
                string ownerId = $"foot-placement-profile:{descriptor.Profile.ProfileId}";
                for (int entryIndex = 0; entryIndex < TuningLayout.Entries.Count; entryIndex++)
                {
                    CharacterPoseTuningLayoutEntry entry = TuningLayout.Entries[entryIndex];
                    if (!string.Equals(entry.OwnerId, ownerId, StringComparison.Ordinal) ||
                        entry.Interaction != CharacterPoseTuningInteractionPolicy.Structural)
                        continue;
                    CharacterPoseTuningValue value = TuningDefaultBlock.GetValue(entry);
                    if (entry.FieldId.EndsWith("/landing-prediction/hit-capacity", StringComparison.Ordinal) &&
                        value.IntegerValue != landing.HitCapacity)
                        throw new InvalidOperationException("Character Presentation Projection Foot Placement hit capacity is stale.");
                }
            }
        }

        internal void SetTuningPayload(
            CharacterPoseTuningLayout layout,
            CharacterPoseTuningParameterBlock defaultBlock,
            string publishedParameterRevision)
        {
            m_TuningLayout = layout ?? throw new ArgumentNullException(nameof(layout));
            m_TuningDefaultBlock = defaultBlock ?? throw new ArgumentNullException(nameof(defaultBlock));
            m_PublishedParameterRevision = string.IsNullOrWhiteSpace(publishedParameterRevision)
                ? throw new ArgumentException("Published parameter revision is required.", nameof(publishedParameterRevision))
                : publishedParameterRevision;
            RequireTuningPayload();
        }

        public void RequirePosePayload()
        {
            PosePlan?.RequireValid();
            Rig?.RequireValid();
            BlendCurveCatalog?.RequireValid();
            BlendProfileCatalog?.RequireValid(Rig?.PoseBoneCount ?? 0, Rig?.RigId, Rig?.RigRevision);
            if (PosePlan == null || Rig == null || BlendCurveCatalog == null || BlendProfileCatalog == null ||
                !string.Equals(PosePlan.RigId, Rig.RigId, StringComparison.Ordinal) ||
                !string.Equals(PosePlan.RigRevision, Rig.RigRevision, StringComparison.Ordinal) ||
                PosePlan.PoseBoneCount != Rig.PoseBoneCount)
            {
                throw new InvalidOperationException("Character Presentation Projection Pose payload is incomplete or inconsistent.");
            }

            var poseSourceIndices = new HashSet<PresentationPoseSourceIndex>();
            for (int i = 0; i < PoseSources.Count; i++)
            {
                CharacterPresentationPoseSourcePlan source = PoseSources[i];
                source?.RequireValid();
                if (source == null || !poseSourceIndices.Add(source.SourceIndex) ||
                    !string.Equals(source.RigId, Rig.RigId, StringComparison.Ordinal) ||
                    !string.Equals(source.RigRevision, Rig.RigRevision, StringComparison.Ordinal) ||
                    FootAnalysis == null || !FootAnalysis.IsEnabled ||
                    !string.Equals(source.FootAnalysisIdentity, FootAnalysis.AnalysisSourceId, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"Character Presentation Projection Pose source #{i} is inconsistent.");
                }
            }

            RequireLinkedPosePosePlan(poseSourceIndices);

            var blendNodes = new HashSet<PoseNodeId>();
            for (int i = 0; i < PosePlan.BlendNodes.Count; i++)
            {
                AnimationBlendNodePayload blend = PosePlan.BlendNodes[i];
                if (blend == null || !blendNodes.Add(blend.NodeId) || blend.StackPolicy == null)
                    throw new InvalidOperationException($"Character Presentation Projection Blend Stack #{i} is inconsistent.");
                blend.StackPolicy.RequireValid();
                for (int transitionIndex = 0; transitionIndex < blend.Transitions.Count; transitionIndex++)
                {
                    AnimationBlendTransitionPayload transition = blend.Transitions[transitionIndex];
                    if (transition == null)
                        throw new InvalidOperationException($"Character Presentation Projection Blend Stack '{blend.NodeId}' transition #{transitionIndex} is missing.");
                    transition.RequireValid(BlendCurveCatalog.Entries.Count, BlendProfileCatalog.Entries.Count);
                }
            }
            RequireMotionMatchingPayload();
            RequireBlendSpacePayload();
            RequireActionFootPhaseWarpPayload();
        }

        void RequireActionFootPhaseWarpPayload()
        {
            var producerIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < Producers.Count; i++)
            {
                CharacterPresentationProducerEntry producer = Producers[i];
                if (producer.Kind == CharacterPresentationProducerKind.Animation)
                    producerIds.Add(producer.ProgramProducerIdentity);
            }
            var pairs = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < ActionFootPhaseWarps.Count; i++)
            {
                ActionAnimationFootPhaseTimeWarpPlan relation = ActionFootPhaseWarps[i];
                relation?.RequireValid();
                string key = relation == null
                    ? string.Empty
                    : string.Concat(
                        relation.LeaderProgramProducerId,
                        "\n",
                        relation.FollowerProgramProducerId);
                if (relation == null ||
                    !producerIds.Contains(relation.LeaderProgramProducerId) ||
                    !producerIds.Contains(relation.FollowerProgramProducerId) ||
                    !pairs.Add(key))
                    throw new InvalidOperationException(
                        $"Projection Action Foot Phase relation #{i} is invalid or duplicated.");
            }
        }

        void RequireLinkedPosePosePlan(HashSet<PresentationPoseSourceIndex> poseSourceIndices)
        {
            LinkedPose?.RequireValid();
            if (LinkedPose == null ||
                !string.Equals(LinkedPose.RigId, Rig.RigId, StringComparison.Ordinal) ||
                !string.Equals(LinkedPose.RigRevision, Rig.RigRevision, StringComparison.Ordinal) ||
                LinkedPose.Calls.Count != PosePlan.LinkedPoseCalls.Count)
            {
                throw new InvalidOperationException("Projection Linked Pose payload does not match the Pose Plan.");
            }

            var calls = new Dictionary<PoseNodeId, CharacterLinkedPoseCallProjectionDescriptor>();
            for (int i = 0; i < LinkedPose.Calls.Count; i++)
                calls.Add(LinkedPose.Calls[i].NodeId, LinkedPose.Calls[i]);
            var selectors = new Dictionary<LinkedPoseGroupId, CharacterLinkedPoseCompiledSelectorDescriptor>();
            for (int i = 0; i < LinkedPose.Selectors.Count; i++)
                selectors.Add(LinkedPose.Selectors[i].GroupId, LinkedPose.Selectors[i]);
            var implementations = new Dictionary<LinkedPoseImplementationId, CharacterLinkedPoseImplementationProjectionDescriptor>();
            for (int i = 0; i < LinkedPose.Implementations.Count; i++)
                implementations.Add(LinkedPose.Implementations[i].ImplementationId, LinkedPose.Implementations[i]);

            for (int callIndex = 0; callIndex < PosePlan.LinkedPoseCalls.Count; callIndex++)
            {
                CharacterLinkedPoseCallPlanDescriptor planCall = PosePlan.LinkedPoseCalls[callIndex];
                if (!calls.TryGetValue(planCall.NodeId, out CharacterLinkedPoseCallProjectionDescriptor projectionCall) ||
                    projectionCall.GroupId != planCall.GroupId ||
                    projectionCall.InterfaceId != planCall.InterfaceId ||
                    projectionCall.InterfaceSignature != planCall.InterfaceSignature ||
                    projectionCall.EntryId != planCall.EntryId ||
                    !selectors.TryGetValue(planCall.GroupId, out CharacterLinkedPoseCompiledSelectorDescriptor selector) ||
                    selector.CandidateImplementationIds.Count != planCall.FragmentIndices.Count)
                {
                    throw new InvalidOperationException($"Linked Pose Call '{planCall.NodeId}' Projection and Pose Plan contracts differ.");
                }

                var candidates = new HashSet<LinkedPoseImplementationId>();
                for (int candidateIndex = 0; candidateIndex < selector.CandidateImplementationIds.Count; candidateIndex++)
                    candidates.Add(new LinkedPoseImplementationId(selector.CandidateImplementationIds[candidateIndex]));
                for (int fragmentOffset = 0; fragmentOffset < planCall.FragmentIndices.Count; fragmentOffset++)
                {
                    CharacterLinkedPoseEntryFragmentPlanDescriptor fragment =
                        PosePlan.LinkedPoseFragments[planCall.FragmentIndices[fragmentOffset]];
                    if (!candidates.Remove(fragment.ImplementationId) ||
                        !implementations.TryGetValue(fragment.ImplementationId, out CharacterLinkedPoseImplementationProjectionDescriptor implementation) ||
                        implementation.Revision != fragment.ImplementationRevision ||
                        implementation.InterfaceId != fragment.InterfaceId ||
                        implementation.InterfaceSignature != fragment.InterfaceSignature)
                    {
                        throw new InvalidOperationException($"Linked Pose Call '{planCall.NodeId}' candidate fragment '{fragment.ImplementationId}' is stale.");
                    }

                    CharacterLinkedPoseEntryFragmentDescriptor projectedEntry = null;
                    for (int entryIndex = 0; entryIndex < implementation.Entries.Count; entryIndex++)
                    {
                        if (implementation.Entries[entryIndex].EntryId == fragment.EntryId)
                        {
                            projectedEntry = implementation.Entries[entryIndex];
                            break;
                        }
                    }
                    if (projectedEntry == null || projectedEntry.GraphId != fragment.GraphId ||
                        !string.Equals(projectedEntry.GraphContentRevision, fragment.GraphRevision, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException($"Linked Pose fragment '{fragment.ImplementationId}/{fragment.EntryId}' graph revision is stale.");
                    }
                    for (int sourceOffset = 0; sourceOffset < fragment.SourceIndices.Count; sourceOffset++)
                    {
                        if (!poseSourceIndices.Contains(new PresentationPoseSourceIndex(fragment.SourceIndices[sourceOffset])))
                            throw new InvalidOperationException($"Linked Pose fragment '{fragment.ImplementationId}/{fragment.EntryId}' references an absent Pose source.");
                    }
                }
                if (candidates.Count != 0)
                    throw new InvalidOperationException($"Linked Pose Call '{planCall.NodeId}' candidate closure is incomplete.");
            }
        }

        void RequireBlendSpacePayload()
        {
            var referencedPlans = new HashSet<int>();
            int operationCount = 0;
            var playerNodes = new HashSet<PoseNodeId>();
            for (int i = 0; i < PosePlan.Operations.Count; i++)
            {
                if (PosePlan.Operations[i].Code == CharacterPoseOperationCode.BlendSpacePlayer)
                    operationCount++;
            }
            for (int i = 0; i < BlendSpacePlayers.Count; i++)
            {
                CharacterAnimationBlendSpacePlayerPlan player = BlendSpacePlayers[i];
                player?.RequireValid(this);
                if (player == null || !playerNodes.Add(player.NodeId))
                    throw new InvalidOperationException($"Projection Blend Space Player plan #{i} is missing or duplicated.");
                referencedPlans.Add(player.BlendSpacePlanIndex);
                CharacterAnimationBlendSpacePlan plan =
                    BlendSpaces[player.BlendSpacePlanIndex];
                plan?.RequireValid(
                    FootAnalysis != null && FootAnalysis.IsEnabled);
                if (plan == null ||
                    !string.Equals(
                        plan.RigId,
                        Rig.RigId,
                        StringComparison.Ordinal) ||
                    !string.Equals(
                        plan.RigRevision,
                        Rig.RigRevision,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Blend Space Player '{player.NodeId}' plan Rig is inconsistent.");
                }
            }
            if (operationCount != BlendSpacePlayers.Count)
                throw new InvalidOperationException("Projection Blend Space Player operation count is inconsistent.");
            if (referencedPlans.Count != BlendSpaces.Count)
                throw new InvalidOperationException(
                    "Projection retains an unreferenced Blend Space plan.");
        }

        void RequireMotionMatchingPayload()
        {
            PosePlan.RequireMotionMatchingPlan();
            if (PosePlan.MotionMatchingNodes.Count == 0)
            {
                if (m_MotionMatching != null)
                    throw new InvalidOperationException(
                        "Projection has a Motion Matching payload without a MotionMatchingPose node.");
                return;
            }
            if (m_MotionMatching == null)
                throw new InvalidOperationException(
                    "Projection MotionMatchingPose nodes require a Motion Matching payload.");
            if (m_MotionMatching.NodeBindingCount != PosePlan.MotionMatchingNodes.Count)
                throw new InvalidOperationException(
                    "Projection Motion Matching node binding count does not match the Pose Plan.");
            if (!string.Equals(m_MotionMatching.FeatureSchema.RigId, Rig.RigId, StringComparison.Ordinal) ||
                !string.Equals(m_MotionMatching.FeatureSchema.RigRevision, Rig.RigRevision, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Projection Motion Matching Feature Schema Rig does not match the Presentation Rig.");
            }

            var planNodes = new Dictionary<PoseNodeId, CharacterMotionMatchingPosePlanDescriptor>();
            for (int i = 0; i < PosePlan.MotionMatchingNodes.Count; i++)
            {
                CharacterMotionMatchingPosePlanDescriptor node =
                    PosePlan.MotionMatchingNodes[i];
                if (!planNodes.TryAdd(node.NodeId, node))
                {
                    throw new InvalidOperationException(
                        $"Motion Matching Pose Plan node '{node.NodeId}' is duplicated.");
                }
            }

            var resolvedNodes = new HashSet<PoseNodeId>();
            var resolvedDatabases = new HashSet<int>();
            for (int i = 0; i < m_MotionMatching.NodeBindingCount; i++)
            {
                MotionMatchingNodeBindingPayload binding =
                    m_MotionMatching.GetNodeBinding(i);
                if (!planNodes.TryGetValue(
                        binding.PoseNodeId,
                        out CharacterMotionMatchingPosePlanDescriptor node) ||
                    !resolvedNodes.Add(binding.PoseNodeId) ||
                    node.BindingId != binding.BindingId ||
                    node.BindingRevision != binding.BindingRevision ||
                    node.ProfileId != m_MotionMatching.ProfileId ||
                    node.ProfileRevision != m_MotionMatching.ProfileRevision ||
                    node.ChooserId != binding.ChooserId ||
                    node.ChooserRevision != binding.ChooserRevision ||
                    node.SearchDomainId != binding.SearchDomainId ||
                    node.FirstDatabaseIndex != binding.FirstDatabaseIndex ||
                    node.DatabaseCount != binding.DatabaseCount)
                {
                    throw new InvalidOperationException(
                        $"Motion Matching node binding #{i} does not match its Pose Plan node.");
                }
                binding.Chooser.RequireDatabaseRange(
                    binding.FirstDatabaseIndex,
                    binding.DatabaseCount,
                    m_MotionMatching.DatabaseCount);
                for (int databaseOffset = 0; databaseOffset < binding.DatabaseCount; databaseOffset++)
                {
                    int databaseIndex = binding.FirstDatabaseIndex + databaseOffset;
                    MotionMatchingDatabasePayload database =
                        m_MotionMatching.GetDatabase(databaseIndex);
                    if (!resolvedDatabases.Add(databaseIndex) || database == null ||
                        database.SearchDomainId != binding.SearchDomainId ||
                        database.ArtifactIdentity.FeatureSchemaId != m_MotionMatching.FeatureSchema.SchemaId ||
                        database.ArtifactIdentity.FeatureSchemaRevision != m_MotionMatching.FeatureSchema.Revision ||
                        !string.Equals(database.ArtifactIdentity.RigId, Rig.RigId, StringComparison.Ordinal) ||
                        !string.Equals(database.ArtifactIdentity.RigRevision, Rig.RigRevision, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException(
                            $"Motion Matching node '{binding.PoseNodeId}' Database payload #{databaseIndex} is stale or shared by another node binding.");
                    }
                }
            }
            if (resolvedNodes.Count != planNodes.Count ||
                resolvedDatabases.Count != m_MotionMatching.DatabaseCount)
            {
                throw new InvalidOperationException(
                    "Projection Motion Matching closure contains unresolved nodes or Databases.");
            }
        }
    }
}
