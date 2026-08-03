using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
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
        public MotionMatchingProjectionPayload MotionMatching => m_MotionMatching;

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
            CharacterPresentationProducerEntry[] producers,
            AnimationFootAnalysisProjectionIdentity footAnalysis,
            string projectionRevision,
            EquipmentVisualProjectionBinding[] equipmentVisualBindings)
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
                m_Producers = producers ?? Array.Empty<CharacterPresentationProducerEntry>(),
                m_FootAnalysis = footAnalysis
            };
            projection.SetEquipmentProjection(
                projectionRevision,
                equipmentVisualBindings);
            projection.RequireContract(contract);
            projection.RequirePosePayload();
            return projection;
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
            var providers =
                new Dictionary<
                    PresentationPoseSourceProviderId,
                    PoseStateSourceProviderPlan>();
            for (int machineIndex = 0;
                 machineIndex < PosePlan.StateMachines.Count;
                 machineIndex++)
            {
                CharacterPoseStateMachineDescriptor machine =
                    PosePlan.StateMachines[machineIndex];
                for (int stateIndex = 0;
                     stateIndex < machine.States.Count;
                     stateIndex++)
                {
                    IReadOnlyList<PoseStateSourceProviderPlan>
                        usages =
                            machine.States[stateIndex]
                                .SourceProviders;
                    for (int usageIndex = 0;
                         usageIndex < usages.Count;
                         usageIndex++)
                    {
                        PoseStateSourceProviderPlan usage =
                            usages[usageIndex];
                        if (usage.SourceKind !=
                            AnimationPoseSourceKind.MotionMatching)
                        {
                            continue;
                        }
                        if (!providers.TryAdd(
                                usage.ProviderId,
                                usage))
                        {
                            throw new InvalidOperationException(
                                $"Motion Matching provider '{usage.ProviderId}' is duplicated.");
                        }
                    }
                }
            }
            if (providers.Count == 0)
            {
                if (m_MotionMatching != null)
                    throw new InvalidOperationException(
                        "Projection has a Motion Matching payload without a Pose State provider.");
                return;
            }
            if (m_MotionMatching == null)
                throw new InvalidOperationException(
                    "Projection Motion Matching Pose State providers require a payload.");
            if (m_MotionMatching.ProviderBindingCount !=
                providers.Count)
                throw new InvalidOperationException(
                    "Projection Motion Matching payload count does not match Pose State providers.");
            var resolved =
                new HashSet<
                    PresentationPoseSourceProviderId>();
            for (int i = 0; i < m_MotionMatching.ProviderBindingCount; i++)
            {
                MotionMatchingProviderBindingPayload binding = m_MotionMatching.GetProviderBinding(i);
                var providerId =
                    new PresentationPoseSourceProviderId(
                        binding.ProviderId);
                if (!providers.TryGetValue(
                        providerId,
                        out PoseStateSourceProviderPlan usage) ||
                    !resolved.Add(providerId) ||
                    usage.PresentationPoseSourceIndex !=
                        binding.PresentationPoseSourceIndex ||
                    usage.PlayerNodeId != binding.PoseNodeId)
                {
                    throw new InvalidOperationException(
                        $"Motion Matching payload provider binding #{i} does not resolve uniquely to its Pose State source.");
                }
            }
        }
    }
}
