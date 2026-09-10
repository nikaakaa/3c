using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Animation.TransitionRouting;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public enum CharacterPoseValueReferenceKind : byte
    {
        Pose = 1,
        Parameter = 2,
        OperationControl = 3,
        FullBodyIkGoalContribution = 4,
        FullBodyIkGoalSet = 5
    }

    [Serializable]
    public sealed class CharacterPoseValueReference
    {
        [SerializeField] CharacterPoseValueReferenceKind m_Kind;
        [SerializeField] int m_Index;

        public CharacterPoseValueReference(
            CharacterPoseValueReferenceKind kind,
            int index)
        {
            if (!Enum.IsDefined(typeof(CharacterPoseValueReferenceKind), kind) ||
                index < 0)
            {
                throw new ArgumentException("Pose Value reference is invalid.");
            }
            m_Kind = kind;
            m_Index = index;
        }

        public CharacterPoseValueReferenceKind Kind => m_Kind;
        public int Index => m_Index;
    }

    [Serializable]
    public sealed class CharacterPoseOperationHeader
    {
        public const int PayloadVersion = 1;

        [SerializeField] int m_Index;
        [SerializeField] CharacterPoseExecutionDomain m_ExecutionDomain;
        [SerializeField] CharacterPoseSpace m_InputPoseSpace;
        [SerializeField] CharacterPoseSpace m_OutputPoseSpace;
        [SerializeField] CharacterPoseOperationCode m_Code;
        [SerializeField] CharacterPoseOperationFamily m_Family;
        [SerializeField] int m_FamilyPayloadIndex;
        [SerializeField] int m_PayloadVersion = PayloadVersion;
        [SerializeField] string m_NodeId = string.Empty;
        [SerializeField] int m_InputValueReferenceStart;
        [SerializeField] int m_InputValueReferenceCount;
        [SerializeField] int m_OutputValueReferenceStart;
        [SerializeField] int m_OutputValueReferenceCount;
        [SerializeField] int m_LinkedPoseFragmentIndex = -1;
        [SerializeField] float m_Weight = 1f;

        public CharacterPoseOperationHeader(
            int index,
            CharacterPoseExecutionDomain executionDomain,
            CharacterPoseSpace inputPoseSpace,
            CharacterPoseSpace outputPoseSpace,
            CharacterPoseOperationCode code,
            CharacterPoseOperationFamily family,
            int familyPayloadIndex,
            PoseNodeId nodeId,
            int inputValueReferenceStart,
            int inputValueReferenceCount,
            int outputValueReferenceStart,
            int outputValueReferenceCount,
            int linkedPoseFragmentIndex,
            float weight)
        {
            if (index < 0 ||
                !Enum.IsDefined(typeof(CharacterPoseExecutionDomain), executionDomain) ||
                !Enum.IsDefined(typeof(CharacterPoseSpace), inputPoseSpace) ||
                !Enum.IsDefined(typeof(CharacterPoseSpace), outputPoseSpace) ||
                CharacterPoseOperationFamilies.RequireFamily(code) != family ||
                familyPayloadIndex < 0 || !nodeId.IsValid ||
                inputValueReferenceStart < 0 || inputValueReferenceCount < 0 ||
                outputValueReferenceStart < 0 || outputValueReferenceCount < 0 ||
                linkedPoseFragmentIndex < -1 ||
                !float.IsFinite(weight) || weight < 0f || weight > 1f)
            {
                throw new ArgumentException("Pose Operation header is invalid.");
            }
            m_Index = index;
            m_ExecutionDomain = executionDomain;
            m_InputPoseSpace = inputPoseSpace;
            m_OutputPoseSpace = outputPoseSpace;
            m_Code = code;
            m_Family = family;
            m_FamilyPayloadIndex = familyPayloadIndex;
            m_NodeId = nodeId.Value;
            m_InputValueReferenceStart = inputValueReferenceStart;
            m_InputValueReferenceCount = inputValueReferenceCount;
            m_OutputValueReferenceStart = outputValueReferenceStart;
            m_OutputValueReferenceCount = outputValueReferenceCount;
            m_LinkedPoseFragmentIndex = linkedPoseFragmentIndex;
            m_Weight = weight;
        }

        public int Index => m_Index;
        public CharacterPoseExecutionDomain ExecutionDomain => m_ExecutionDomain;
        public CharacterPoseSpace InputPoseSpace => m_InputPoseSpace;
        public CharacterPoseSpace OutputPoseSpace => m_OutputPoseSpace;
        public CharacterPoseOperationCode Code => m_Code;
        public CharacterPoseOperationFamily Family => m_Family;
        public int FamilyPayloadIndex => m_FamilyPayloadIndex;
        public int Version => m_PayloadVersion;
        public PoseNodeId NodeId => new PoseNodeId(m_NodeId);
        public int InputValueReferenceStart => m_InputValueReferenceStart;
        public int InputValueReferenceCount => m_InputValueReferenceCount;
        public int OutputValueReferenceStart => m_OutputValueReferenceStart;
        public int OutputValueReferenceCount => m_OutputValueReferenceCount;
        public int LinkedPoseFragmentIndex => m_LinkedPoseFragmentIndex;
        public float Weight => m_Weight;
    }

    [Serializable]
    public abstract class CharacterPoseOperationPayload
    {
        [SerializeField] int m_OperationIndex;

        protected CharacterPoseOperationPayload(int operationIndex)
        {
            if (operationIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(operationIndex));
            m_OperationIndex = operationIndex;
        }

        public int OperationIndex => m_OperationIndex;
    }

    [Serializable]
    public sealed class CharacterPoseMarkerOperationPayload :
        CharacterPoseOperationPayload
    {
        public CharacterPoseMarkerOperationPayload(int operationIndex) :
            base(operationIndex)
        {
        }
    }

    [Serializable]
    public sealed class CharacterPoseParameterResolveOperationPayload :
        CharacterPoseOperationPayload
    {
        [SerializeField] PoseParameterResolvePolicy[] m_Policies =
            Array.Empty<PoseParameterResolvePolicy>();

        public CharacterPoseParameterResolveOperationPayload(
            int operationIndex,
            PoseParameterResolvePolicy[] policies) : base(operationIndex)
        {
            m_Policies = policies ?? throw new ArgumentNullException(nameof(policies));
        }

        public IReadOnlyList<PoseParameterResolvePolicy> Policies =>
            m_Policies ?? Array.Empty<PoseParameterResolvePolicy>();
    }

    [Serializable]
    public sealed class CharacterPosePlayerOperationPayload :
        CharacterPoseOperationPayload
    {
        [SerializeField] string m_SourceProviderId = string.Empty;
        [SerializeField] int m_SourceIndex = -1;
        [SerializeField] AnimationSelectionAvailabilityPolicy m_SelectionAvailability;
        [SerializeField] CharacterAnimationBlendSpaceInputRangePolicy m_InputRangePolicy;
        [SerializeField] int m_PlayerIndex;
        [SerializeField] int m_ClipPlayerIndex = -1;

        public CharacterPosePlayerOperationPayload(
            int operationIndex,
            PresentationPoseSourceProviderId sourceProviderId,
            PresentationPoseSourceIndex sourceIndex,
            AnimationSelectionAvailabilityPolicy selectionAvailability,
            CharacterAnimationBlendSpaceInputRangePolicy inputRangePolicy,
            int playerIndex,
            int clipPlayerIndex) : base(operationIndex)
        {
            if (playerIndex < 0 || clipPlayerIndex < -1 ||
                !Enum.IsDefined(
                    typeof(AnimationSelectionAvailabilityPolicy),
                    selectionAvailability) ||
                !Enum.IsDefined(
                    typeof(CharacterAnimationBlendSpaceInputRangePolicy),
                    inputRangePolicy))
            {
                throw new ArgumentException("Pose Player payload is invalid.");
            }
            m_SourceProviderId = sourceProviderId.Value ?? string.Empty;
            m_SourceIndex = sourceIndex.IsValid ? sourceIndex.Value : -1;
            m_SelectionAvailability = selectionAvailability;
            m_InputRangePolicy = inputRangePolicy;
            m_PlayerIndex = playerIndex;
            m_ClipPlayerIndex = clipPlayerIndex;
        }

        public PresentationPoseSourceProviderId SourceProviderId =>
            string.IsNullOrWhiteSpace(m_SourceProviderId)
                ? default
                : new PresentationPoseSourceProviderId(m_SourceProviderId);
        public PresentationPoseSourceIndex SourceIndex =>
            m_SourceIndex < 0 ? default : new PresentationPoseSourceIndex(m_SourceIndex);
        public AnimationSelectionAvailabilityPolicy SelectionAvailability =>
            m_SelectionAvailability;
        public CharacterAnimationBlendSpaceInputRangePolicy InputRangePolicy =>
            m_InputRangePolicy;
        public int PlayerIndex => m_PlayerIndex;
        public int ClipPlayerIndex => m_ClipPlayerIndex;
    }

    [Serializable]
    public sealed class CharacterPoseStateMachineOperationPayload :
        CharacterPoseOperationPayload
    {
        [SerializeField] int m_StateMachineIndex = -1;

        public CharacterPoseStateMachineOperationPayload(
            int operationIndex,
            int stateMachineIndex) : base(operationIndex)
        {
            if (stateMachineIndex < -1)
                throw new ArgumentOutOfRangeException(nameof(stateMachineIndex));
            m_StateMachineIndex = stateMachineIndex;
        }

        public int StateMachineIndex => m_StateMachineIndex;
    }

    [Serializable]
    public sealed class CharacterPoseActionInputOperationPayload :
        CharacterPoseOperationPayload
    {
        [SerializeField] string m_AnimationChannelId = string.Empty;
        [SerializeField] AnimationSelectionAvailabilityPolicy m_SelectionAvailability;

        public CharacterPoseActionInputOperationPayload(
            int operationIndex,
            AnimationChannelId animationChannelId,
            AnimationSelectionAvailabilityPolicy selectionAvailability) :
            base(operationIndex)
        {
            if (!animationChannelId.IsValid ||
                !Enum.IsDefined(
                    typeof(AnimationSelectionAvailabilityPolicy),
                    selectionAvailability))
            {
                throw new ArgumentException("Pose Action Input payload is invalid.");
            }
            m_AnimationChannelId = animationChannelId.Value;
            m_SelectionAvailability = selectionAvailability;
        }

        public AnimationChannelId AnimationChannelId =>
            new AnimationChannelId(m_AnimationChannelId);
        public AnimationSelectionAvailabilityPolicy SelectionAvailability =>
            m_SelectionAvailability;
    }

    [Serializable]
    public sealed class CharacterPoseAnimationSlotOperationPayload :
        CharacterPoseOperationPayload
    {
        [SerializeField] string m_SourceProviderId = string.Empty;
        [SerializeField] string m_AnimationChannelId = string.Empty;
        [SerializeField] int m_PlayerIndex;
        [SerializeField] int m_BlendNodeIndex;
        [SerializeField] int m_AnimationSlotIndex;

        public CharacterPoseAnimationSlotOperationPayload(
            int operationIndex,
            PresentationPoseSourceProviderId sourceProviderId,
            AnimationChannelId animationChannelId,
            int playerIndex,
            int blendNodeIndex,
            int animationSlotIndex) : base(operationIndex)
        {
            if (!sourceProviderId.IsValid || !animationChannelId.IsValid ||
                playerIndex < 0 || blendNodeIndex < 0 || animationSlotIndex < 0)
            {
                throw new ArgumentException("Pose Animation Slot payload is invalid.");
            }
            m_SourceProviderId = sourceProviderId.Value;
            m_AnimationChannelId = animationChannelId.Value;
            m_PlayerIndex = playerIndex;
            m_BlendNodeIndex = blendNodeIndex;
            m_AnimationSlotIndex = animationSlotIndex;
        }

        public PresentationPoseSourceProviderId SourceProviderId =>
            new PresentationPoseSourceProviderId(m_SourceProviderId);
        public AnimationChannelId AnimationChannelId =>
            new AnimationChannelId(m_AnimationChannelId);
        public int PlayerIndex => m_PlayerIndex;
        public int BlendNodeIndex => m_BlendNodeIndex;
        public int AnimationSlotIndex => m_AnimationSlotIndex;
    }

    [Serializable]
    public sealed class CharacterPoseBlendOperationPayload :
        CharacterPoseOperationPayload
    {
        [SerializeField] string m_SourceProviderId = string.Empty;
        [SerializeField] int m_SourceIndex = -1;
        [SerializeField] AnimationSelectionAvailabilityPolicy m_SelectionAvailability;
        [SerializeField] int m_PlayerIndex = -1;
        [SerializeField] int m_BlendNodeIndex = -1;

        public CharacterPoseBlendOperationPayload(
            int operationIndex,
            PresentationPoseSourceProviderId sourceProviderId,
            PresentationPoseSourceIndex sourceIndex,
            AnimationSelectionAvailabilityPolicy selectionAvailability,
            int playerIndex,
            int blendNodeIndex) : base(operationIndex)
        {
            if (playerIndex < -1 || blendNodeIndex < -1 ||
                !Enum.IsDefined(
                    typeof(AnimationSelectionAvailabilityPolicy),
                    selectionAvailability))
            {
                throw new ArgumentException("Pose Blend payload is invalid.");
            }
            m_SourceProviderId = sourceProviderId.Value ?? string.Empty;
            m_SourceIndex = sourceIndex.IsValid ? sourceIndex.Value : -1;
            m_SelectionAvailability = selectionAvailability;
            m_PlayerIndex = playerIndex;
            m_BlendNodeIndex = blendNodeIndex;
        }

        public PresentationPoseSourceProviderId SourceProviderId =>
            string.IsNullOrWhiteSpace(m_SourceProviderId)
                ? default
                : new PresentationPoseSourceProviderId(m_SourceProviderId);
        public PresentationPoseSourceIndex SourceIndex =>
            m_SourceIndex < 0 ? default : new PresentationPoseSourceIndex(m_SourceIndex);
        public AnimationSelectionAvailabilityPolicy SelectionAvailability =>
            m_SelectionAvailability;
        public int PlayerIndex => m_PlayerIndex;
        public int BlendNodeIndex => m_BlendNodeIndex;
    }

    [Serializable]
    public sealed class CharacterPoseIndexedOperationPayload :
        CharacterPoseOperationPayload
    {
        [SerializeField] int m_ValueIndex;

        public CharacterPoseIndexedOperationPayload(
            int operationIndex,
            int valueIndex) : base(operationIndex)
        {
            if (valueIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(valueIndex));
            m_ValueIndex = valueIndex;
        }

        public int ValueIndex => m_ValueIndex;
    }

    [Serializable]
    public sealed class CharacterPoseCompositionOperationPayload :
        CharacterPoseOperationPayload
    {
        [SerializeField] int m_BoneMaskIndex = -1;
        [SerializeField] int m_AdditiveReferenceIndex = -1;
        [SerializeField] CharacterLayeredBoneBlendSpace m_LayeredBoneBlendSpace = CharacterLayeredBoneBlendSpace.Local;

        public CharacterPoseCompositionOperationPayload(
            int operationIndex,
            int boneMaskIndex,
            int additiveReferenceIndex,
            CharacterLayeredBoneBlendSpace layeredBoneBlendSpace) : base(operationIndex)
        {
            if (boneMaskIndex < -1 || additiveReferenceIndex < -1 ||
                !Enum.IsDefined(
                    typeof(CharacterLayeredBoneBlendSpace),
                    layeredBoneBlendSpace))
                throw new ArgumentOutOfRangeException(nameof(boneMaskIndex));
            m_BoneMaskIndex = boneMaskIndex;
            m_AdditiveReferenceIndex = additiveReferenceIndex;
            m_LayeredBoneBlendSpace = layeredBoneBlendSpace;
        }

        public int BoneMaskIndex => m_BoneMaskIndex;
        public int AdditiveReferenceIndex => m_AdditiveReferenceIndex;
        public CharacterLayeredBoneBlendSpace LayeredBoneBlendSpace => m_LayeredBoneBlendSpace;
    }

    [Serializable]
    public sealed class CharacterPoseComponentControlOperationPayload :
        CharacterPoseOperationPayload
    {
        [SerializeField] int m_ModifyBoneIndex = -1;
        [SerializeField] int m_RootOrientationWarpIndex = -1;

        public CharacterPoseComponentControlOperationPayload(
            int operationIndex,
            int modifyBoneIndex,
            int rootOrientationWarpIndex) : base(operationIndex)
        {
            if (modifyBoneIndex < -1 || rootOrientationWarpIndex < -1)
                throw new ArgumentOutOfRangeException(nameof(modifyBoneIndex));
            m_ModifyBoneIndex = modifyBoneIndex;
            m_RootOrientationWarpIndex = rootOrientationWarpIndex;
        }

        public int ModifyBoneIndex => m_ModifyBoneIndex;
        public int RootOrientationWarpIndex => m_RootOrientationWarpIndex;
    }

    [Serializable]
    public sealed class CharacterPoseGoalContributionOperationPayload :
        CharacterPoseOperationPayload
    {
        [SerializeField] int m_PoseBoneIkGoalsIndex = -1;
        [SerializeField] int m_FootPlacementIndex = -1;

        public CharacterPoseGoalContributionOperationPayload(
            int operationIndex,
            int poseBoneIkGoalsIndex,
            int footPlacementIndex) : base(operationIndex)
        {
            if (poseBoneIkGoalsIndex < -1 || footPlacementIndex < -1)
                throw new ArgumentOutOfRangeException(nameof(poseBoneIkGoalsIndex));
            m_PoseBoneIkGoalsIndex = poseBoneIkGoalsIndex;
            m_FootPlacementIndex = footPlacementIndex;
        }

        public int PoseBoneIkGoalsIndex => m_PoseBoneIkGoalsIndex;
        public int FootPlacementIndex => m_FootPlacementIndex;
    }

    [Serializable]
    public sealed class CharacterPoseOperationPages
    {
        [SerializeField] CharacterPoseOperationHeader[] m_Headers =
            Array.Empty<CharacterPoseOperationHeader>();
        [SerializeField] CharacterPoseValueReference[] m_ValueReferences =
            Array.Empty<CharacterPoseValueReference>();
        [SerializeField] CharacterPoseMarkerOperationPayload[] m_ParameterInputs =
            Array.Empty<CharacterPoseMarkerOperationPayload>();
        [SerializeField] CharacterPoseParameterResolveOperationPayload[] m_ParameterResolves =
            Array.Empty<CharacterPoseParameterResolveOperationPayload>();
        [SerializeField] CharacterPosePlayerOperationPayload[] m_Players =
            Array.Empty<CharacterPosePlayerOperationPayload>();
        [SerializeField] CharacterPoseStateMachineOperationPayload[] m_StateMachines =
            Array.Empty<CharacterPoseStateMachineOperationPayload>();
        [SerializeField] CharacterPoseActionInputOperationPayload[] m_ActionInputs =
            Array.Empty<CharacterPoseActionInputOperationPayload>();
        [SerializeField] CharacterPoseAnimationSlotOperationPayload[] m_AnimationSlots =
            Array.Empty<CharacterPoseAnimationSlotOperationPayload>();
        [SerializeField] CharacterPoseBlendOperationPayload[] m_Blends =
            Array.Empty<CharacterPoseBlendOperationPayload>();
        [SerializeField] CharacterPoseIndexedOperationPayload[] m_Inertializations =
            Array.Empty<CharacterPoseIndexedOperationPayload>();
        [SerializeField] CharacterPoseCompositionOperationPayload[] m_Compositions =
            Array.Empty<CharacterPoseCompositionOperationPayload>();
        [SerializeField] CharacterPoseMarkerOperationPayload[] m_SpaceConversions =
            Array.Empty<CharacterPoseMarkerOperationPayload>();
        [SerializeField] CharacterPoseComponentControlOperationPayload[] m_ComponentControls =
            Array.Empty<CharacterPoseComponentControlOperationPayload>();
        [SerializeField] CharacterPoseMarkerOperationPayload[] m_MotionMatchings =
            Array.Empty<CharacterPoseMarkerOperationPayload>();
        [SerializeField] CharacterPoseMarkerOperationPayload[] m_PoseHistories =
            Array.Empty<CharacterPoseMarkerOperationPayload>();
        [SerializeField] CharacterPoseGoalContributionOperationPayload[] m_GoalContributions =
            Array.Empty<CharacterPoseGoalContributionOperationPayload>();
        [SerializeField] CharacterPoseMarkerOperationPayload[] m_GoalAssemblers =
            Array.Empty<CharacterPoseMarkerOperationPayload>();
        [SerializeField] CharacterPoseIndexedOperationPayload[] m_FullBodyIks =
            Array.Empty<CharacterPoseIndexedOperationPayload>();
        [SerializeField] CharacterPoseIndexedOperationPayload[] m_LinkedPoses =
            Array.Empty<CharacterPoseIndexedOperationPayload>();
        [SerializeField] CharacterPoseMarkerOperationPayload[] m_Outputs =
            Array.Empty<CharacterPoseMarkerOperationPayload>();

        public CharacterPoseOperationPages(
            CharacterPoseOperationHeader[] headers,
            CharacterPoseValueReference[] valueReferences,
            CharacterPoseMarkerOperationPayload[] parameterInputs,
            CharacterPoseParameterResolveOperationPayload[] parameterResolves,
            CharacterPosePlayerOperationPayload[] players,
            CharacterPoseStateMachineOperationPayload[] stateMachines,
            CharacterPoseActionInputOperationPayload[] actionInputs,
            CharacterPoseAnimationSlotOperationPayload[] animationSlots,
            CharacterPoseBlendOperationPayload[] blends,
            CharacterPoseIndexedOperationPayload[] inertializations,
            CharacterPoseCompositionOperationPayload[] compositions,
            CharacterPoseMarkerOperationPayload[] spaceConversions,
            CharacterPoseComponentControlOperationPayload[] componentControls,
            CharacterPoseMarkerOperationPayload[] motionMatchings,
            CharacterPoseMarkerOperationPayload[] poseHistories,
            CharacterPoseGoalContributionOperationPayload[] goalContributions,
            CharacterPoseMarkerOperationPayload[] goalAssemblers,
            CharacterPoseIndexedOperationPayload[] fullBodyIks,
            CharacterPoseIndexedOperationPayload[] linkedPoses,
            CharacterPoseMarkerOperationPayload[] outputs)
        {
            m_Headers = headers ?? throw new ArgumentNullException(nameof(headers));
            m_ValueReferences = valueReferences ?? throw new ArgumentNullException(nameof(valueReferences));
            m_ParameterInputs = parameterInputs ?? throw new ArgumentNullException(nameof(parameterInputs));
            m_ParameterResolves = parameterResolves ?? throw new ArgumentNullException(nameof(parameterResolves));
            m_Players = players ?? throw new ArgumentNullException(nameof(players));
            m_StateMachines = stateMachines ?? throw new ArgumentNullException(nameof(stateMachines));
            m_ActionInputs = actionInputs ?? throw new ArgumentNullException(nameof(actionInputs));
            m_AnimationSlots = animationSlots ?? throw new ArgumentNullException(nameof(animationSlots));
            m_Blends = blends ?? throw new ArgumentNullException(nameof(blends));
            m_Inertializations = inertializations ?? throw new ArgumentNullException(nameof(inertializations));
            m_Compositions = compositions ?? throw new ArgumentNullException(nameof(compositions));
            m_SpaceConversions = spaceConversions ?? throw new ArgumentNullException(nameof(spaceConversions));
            m_ComponentControls = componentControls ?? throw new ArgumentNullException(nameof(componentControls));
            m_MotionMatchings = motionMatchings ?? throw new ArgumentNullException(nameof(motionMatchings));
            m_PoseHistories = poseHistories ?? throw new ArgumentNullException(nameof(poseHistories));
            m_GoalContributions = goalContributions ?? throw new ArgumentNullException(nameof(goalContributions));
            m_GoalAssemblers = goalAssemblers ?? throw new ArgumentNullException(nameof(goalAssemblers));
            m_FullBodyIks = fullBodyIks ?? throw new ArgumentNullException(nameof(fullBodyIks));
            m_LinkedPoses = linkedPoses ?? throw new ArgumentNullException(nameof(linkedPoses));
            m_Outputs = outputs ?? throw new ArgumentNullException(nameof(outputs));
        }

        public IReadOnlyList<CharacterPoseOperationHeader> Headers =>
            m_Headers ?? Array.Empty<CharacterPoseOperationHeader>();
        public IReadOnlyList<CharacterPoseValueReference> ValueReferences =>
            m_ValueReferences ?? Array.Empty<CharacterPoseValueReference>();
        public IReadOnlyList<CharacterPoseMarkerOperationPayload> ParameterInputs => m_ParameterInputs ?? Array.Empty<CharacterPoseMarkerOperationPayload>();
        public IReadOnlyList<CharacterPoseParameterResolveOperationPayload> ParameterResolves => m_ParameterResolves ?? Array.Empty<CharacterPoseParameterResolveOperationPayload>();
        public IReadOnlyList<CharacterPosePlayerOperationPayload> Players => m_Players ?? Array.Empty<CharacterPosePlayerOperationPayload>();
        public IReadOnlyList<CharacterPoseStateMachineOperationPayload> StateMachines => m_StateMachines ?? Array.Empty<CharacterPoseStateMachineOperationPayload>();
        public IReadOnlyList<CharacterPoseActionInputOperationPayload> ActionInputs => m_ActionInputs ?? Array.Empty<CharacterPoseActionInputOperationPayload>();
        public IReadOnlyList<CharacterPoseAnimationSlotOperationPayload> AnimationSlots => m_AnimationSlots ?? Array.Empty<CharacterPoseAnimationSlotOperationPayload>();
        public IReadOnlyList<CharacterPoseBlendOperationPayload> Blends => m_Blends ?? Array.Empty<CharacterPoseBlendOperationPayload>();
        public IReadOnlyList<CharacterPoseIndexedOperationPayload> Inertializations => m_Inertializations ?? Array.Empty<CharacterPoseIndexedOperationPayload>();
        public IReadOnlyList<CharacterPoseCompositionOperationPayload> Compositions => m_Compositions ?? Array.Empty<CharacterPoseCompositionOperationPayload>();
        public IReadOnlyList<CharacterPoseMarkerOperationPayload> SpaceConversions => m_SpaceConversions ?? Array.Empty<CharacterPoseMarkerOperationPayload>();
        public IReadOnlyList<CharacterPoseComponentControlOperationPayload> ComponentControls => m_ComponentControls ?? Array.Empty<CharacterPoseComponentControlOperationPayload>();
        public IReadOnlyList<CharacterPoseMarkerOperationPayload> MotionMatchings => m_MotionMatchings ?? Array.Empty<CharacterPoseMarkerOperationPayload>();
        public IReadOnlyList<CharacterPoseMarkerOperationPayload> PoseHistories => m_PoseHistories ?? Array.Empty<CharacterPoseMarkerOperationPayload>();
        public IReadOnlyList<CharacterPoseGoalContributionOperationPayload> GoalContributions => m_GoalContributions ?? Array.Empty<CharacterPoseGoalContributionOperationPayload>();
        public IReadOnlyList<CharacterPoseMarkerOperationPayload> GoalAssemblers => m_GoalAssemblers ?? Array.Empty<CharacterPoseMarkerOperationPayload>();
        public IReadOnlyList<CharacterPoseIndexedOperationPayload> FullBodyIks => m_FullBodyIks ?? Array.Empty<CharacterPoseIndexedOperationPayload>();
        public IReadOnlyList<CharacterPoseIndexedOperationPayload> LinkedPoses => m_LinkedPoses ?? Array.Empty<CharacterPoseIndexedOperationPayload>();
        public IReadOnlyList<CharacterPoseMarkerOperationPayload> Outputs => m_Outputs ?? Array.Empty<CharacterPoseMarkerOperationPayload>();

        public int FindInputValueIndex(
            CharacterPoseOperationHeader header,
            CharacterPoseValueReferenceKind kind,
            int ordinal = 0) => FindValueIndex(
                header,
                header?.InputValueReferenceStart ?? -1,
                header?.InputValueReferenceCount ?? -1,
                kind,
                ordinal);

        public int FindOutputValueIndex(
            CharacterPoseOperationHeader header,
            CharacterPoseValueReferenceKind kind,
            int ordinal = 0) => FindValueIndex(
                header,
                header?.OutputValueReferenceStart ?? -1,
                header?.OutputValueReferenceCount ?? -1,
                kind,
                ordinal);

        public int CountInputValues(
            CharacterPoseOperationHeader header,
            CharacterPoseValueReferenceKind kind) => CountValues(
                header,
                header?.InputValueReferenceStart ?? -1,
                header?.InputValueReferenceCount ?? -1,
                kind);

        public CharacterPoseOperationPayload RequirePayload(
            CharacterPoseOperationHeader header)
        {
            if (header == null)
                throw new ArgumentNullException(nameof(header));
            CharacterPoseOperationPayload payload = header.Family switch
            {
                CharacterPoseOperationFamily.ParameterInput => ParameterInputs[header.FamilyPayloadIndex],
                CharacterPoseOperationFamily.ParameterResolve => ParameterResolves[header.FamilyPayloadIndex],
                CharacterPoseOperationFamily.Player => Players[header.FamilyPayloadIndex],
                CharacterPoseOperationFamily.StateMachine => StateMachines[header.FamilyPayloadIndex],
                CharacterPoseOperationFamily.ActionInput => ActionInputs[header.FamilyPayloadIndex],
                CharacterPoseOperationFamily.AnimationSlot => AnimationSlots[header.FamilyPayloadIndex],
                CharacterPoseOperationFamily.Blend => Blends[header.FamilyPayloadIndex],
                CharacterPoseOperationFamily.Inertialization => Inertializations[header.FamilyPayloadIndex],
                CharacterPoseOperationFamily.Composition => Compositions[header.FamilyPayloadIndex],
                CharacterPoseOperationFamily.SpaceConversion => SpaceConversions[header.FamilyPayloadIndex],
                CharacterPoseOperationFamily.ComponentControl => ComponentControls[header.FamilyPayloadIndex],
                CharacterPoseOperationFamily.MotionMatching => MotionMatchings[header.FamilyPayloadIndex],
                CharacterPoseOperationFamily.PoseHistory => PoseHistories[header.FamilyPayloadIndex],
                CharacterPoseOperationFamily.GoalContribution => GoalContributions[header.FamilyPayloadIndex],
                CharacterPoseOperationFamily.GoalAssembler => GoalAssemblers[header.FamilyPayloadIndex],
                CharacterPoseOperationFamily.FullBodyIk => FullBodyIks[header.FamilyPayloadIndex],
                CharacterPoseOperationFamily.LinkedPose => LinkedPoses[header.FamilyPayloadIndex],
                CharacterPoseOperationFamily.Output => Outputs[header.FamilyPayloadIndex],
                _ => throw new InvalidOperationException(
                    $"Pose Operation '{header.NodeId}' has no Family payload.")
            };
            if (payload == null || payload.OperationIndex != header.Index)
            {
                throw new InvalidOperationException(
                    $"Pose Operation '{header.NodeId}' Family payload is inconsistent.");
            }
            return payload;
        }

        public void RequireValid()
        {
            if (Headers.Count == 0)
                throw new InvalidOperationException("Pose Operation pages are empty.");
            var familyCounts = new int[19];
            int referenceCursor = 0;
            for (int i = 0; i < Headers.Count; i++)
            {
                CharacterPoseOperationHeader header = Headers[i];
                if (header == null || header.Index != i ||
                    header.Version != CharacterPoseOperationHeader.PayloadVersion ||
                    CharacterPoseOperationFamilies.RequireFamily(header.Code) != header.Family ||
                    header.FamilyPayloadIndex != familyCounts[(int)header.Family]++ ||
                    header.InputValueReferenceStart != referenceCursor ||
                    header.InputValueReferenceStart >
                    ValueReferences.Count - header.InputValueReferenceCount)
                {
                    throw new InvalidOperationException(
                        $"Pose Operation header #{i} is invalid.");
                }
                referenceCursor += header.InputValueReferenceCount;
                if (header.OutputValueReferenceStart != referenceCursor ||
                    header.OutputValueReferenceStart >
                    ValueReferences.Count - header.OutputValueReferenceCount)
                {
                    throw new InvalidOperationException(
                        $"Pose Operation header #{i} output range is invalid.");
                }
                referenceCursor += header.OutputValueReferenceCount;
                RequirePayload(header);
            }
            if (referenceCursor != ValueReferences.Count)
                throw new InvalidOperationException("Pose Value reference table is not packed.");
            for (int i = 0; i < ValueReferences.Count; i++)
            {
                CharacterPoseValueReference reference = ValueReferences[i];
                if (reference == null ||
                    !Enum.IsDefined(typeof(CharacterPoseValueReferenceKind), reference.Kind) ||
                    reference.Index < 0)
                {
                    throw new InvalidOperationException(
                        $"Pose Value reference #{i} is invalid.");
                }
            }
            for (int family = 1; family < familyCounts.Length; family++)
            {
                if (familyCounts[family] != GetFamilyPayloadCount(
                        (CharacterPoseOperationFamily)family))
                {
                    throw new InvalidOperationException(
                        $"Pose Operation Family '{(CharacterPoseOperationFamily)family}' payload page is incomplete.");
                }
            }
        }

        int FindValueIndex(
            CharacterPoseOperationHeader header,
            int start,
            int count,
            CharacterPoseValueReferenceKind kind,
            int ordinal)
        {
            if (header == null || ordinal < 0 || start < 0 || count < 0 ||
                start > ValueReferences.Count - count)
            {
                return -1;
            }
            for (int i = 0; i < count; i++)
            {
                CharacterPoseValueReference reference = ValueReferences[start + i];
                if (reference == null || reference.Kind != kind)
                    continue;
                if (ordinal-- == 0)
                    return reference.Index;
            }
            return -1;
        }

        int CountValues(
            CharacterPoseOperationHeader header,
            int start,
            int count,
            CharacterPoseValueReferenceKind kind)
        {
            if (header == null || start < 0 || count < 0 ||
                start > ValueReferences.Count - count)
            {
                return 0;
            }
            int result = 0;
            for (int i = 0; i < count; i++)
            {
                CharacterPoseValueReference reference = ValueReferences[start + i];
                if (reference != null && reference.Kind == kind)
                    result++;
            }
            return result;
        }

        int GetFamilyPayloadCount(CharacterPoseOperationFamily family) =>
            family switch
            {
                CharacterPoseOperationFamily.ParameterInput => ParameterInputs.Count,
                CharacterPoseOperationFamily.ParameterResolve => ParameterResolves.Count,
                CharacterPoseOperationFamily.Player => Players.Count,
                CharacterPoseOperationFamily.StateMachine => StateMachines.Count,
                CharacterPoseOperationFamily.ActionInput => ActionInputs.Count,
                CharacterPoseOperationFamily.AnimationSlot => AnimationSlots.Count,
                CharacterPoseOperationFamily.Blend => Blends.Count,
                CharacterPoseOperationFamily.Inertialization => Inertializations.Count,
                CharacterPoseOperationFamily.Composition => Compositions.Count,
                CharacterPoseOperationFamily.SpaceConversion => SpaceConversions.Count,
                CharacterPoseOperationFamily.ComponentControl => ComponentControls.Count,
                CharacterPoseOperationFamily.MotionMatching => MotionMatchings.Count,
                CharacterPoseOperationFamily.PoseHistory => PoseHistories.Count,
                CharacterPoseOperationFamily.GoalContribution => GoalContributions.Count,
                CharacterPoseOperationFamily.GoalAssembler => GoalAssemblers.Count,
                CharacterPoseOperationFamily.FullBodyIk => FullBodyIks.Count,
                CharacterPoseOperationFamily.LinkedPose => LinkedPoses.Count,
                CharacterPoseOperationFamily.Output => Outputs.Count,
                _ => 0
            };
    }
}
