using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Animation.TransitionRouting;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public enum PoseInertializationTemporalOwnerKind : byte
    {
        StateMachineTransition = 1,
        DirectPlayerPolicy = 2
    }

    public enum CharacterPoseOperationCode : byte
    {
        ProgramParameterInput = 3,
        SelectedPosePlayer = 4,
        BlendStack = 5,
        Inertialization = 6,
        BlendPose = 7,
        LayeredBoneBlend = 8,
        AdditivePose = 9,
        PoseParameterResolve = 10,
        ModifyBone = 11,
        FootPlacement = 12,
        OutputPose = 13,
        BlendSpacePlayer = 15,
        PoseBoneIKGoals = 16,
        ClipPlayer = 17,
        PoseStateMachine = 18,
        StatePoseOutput = 19,
        AnimationSlot = 20,
        ActionPlaybackInput = 21,
        RootOrientationWarp = 22,
        LocalToComponentPose = 23,
        ComponentToLocalPose = 24,
        FullBodyIK = 25,
        LinkedPoseCall = 26,
        MotionMatchingPose = 28,
        PoseHistoryRead = 29,
        PoseHistoryCommit = 30,
        MotionMatchingChooserResolve = 31,
        MotionMatchingEntrySourceCapture = 32,
        MotionMatchingEntryProcessing = 33,
        MotionMatchingInternalBlend = 34,
        FullBodyIkGoalAssembler = 35
    }

    public enum CharacterPoseOperationFamily : byte
    {
        None = 0,
        ParameterInput = 1,
        ParameterResolve = 2,
        Player = 3,
        StateMachine = 4,
        ActionInput = 5,
        AnimationSlot = 6,
        Blend = 7,
        Inertialization = 8,
        Composition = 9,
        SpaceConversion = 10,
        ComponentControl = 11,
        MotionMatching = 12,
        PoseHistory = 13,
        GoalContribution = 14,
        GoalAssembler = 15,
        FullBodyIk = 16,
        LinkedPose = 17,
        Output = 18
    }

    public static class CharacterPoseOperationFamilies
    {
        public static CharacterPoseOperationFamily RequireFamily(
            CharacterPoseOperationCode code) => code switch
        {
            CharacterPoseOperationCode.ProgramParameterInput =>
                CharacterPoseOperationFamily.ParameterInput,
            CharacterPoseOperationCode.PoseParameterResolve =>
                CharacterPoseOperationFamily.ParameterResolve,
            CharacterPoseOperationCode.SelectedPosePlayer or
            CharacterPoseOperationCode.ClipPlayer or
            CharacterPoseOperationCode.BlendSpacePlayer =>
                CharacterPoseOperationFamily.Player,
            CharacterPoseOperationCode.StatePoseOutput or
            CharacterPoseOperationCode.PoseStateMachine =>
                CharacterPoseOperationFamily.StateMachine,
            CharacterPoseOperationCode.ActionPlaybackInput =>
                CharacterPoseOperationFamily.ActionInput,
            CharacterPoseOperationCode.AnimationSlot =>
                CharacterPoseOperationFamily.AnimationSlot,
            CharacterPoseOperationCode.BlendStack or
            CharacterPoseOperationCode.BlendPose =>
                CharacterPoseOperationFamily.Blend,
            CharacterPoseOperationCode.Inertialization =>
                CharacterPoseOperationFamily.Inertialization,
            CharacterPoseOperationCode.LayeredBoneBlend or
            CharacterPoseOperationCode.AdditivePose =>
                CharacterPoseOperationFamily.Composition,
            CharacterPoseOperationCode.LocalToComponentPose or
            CharacterPoseOperationCode.ComponentToLocalPose =>
                CharacterPoseOperationFamily.SpaceConversion,
            CharacterPoseOperationCode.ModifyBone or
            CharacterPoseOperationCode.RootOrientationWarp =>
                CharacterPoseOperationFamily.ComponentControl,
            CharacterPoseOperationCode.MotionMatchingPose or
            CharacterPoseOperationCode.MotionMatchingChooserResolve or
            CharacterPoseOperationCode.MotionMatchingEntrySourceCapture or
            CharacterPoseOperationCode.MotionMatchingEntryProcessing or
            CharacterPoseOperationCode.MotionMatchingInternalBlend =>
                CharacterPoseOperationFamily.MotionMatching,
            CharacterPoseOperationCode.PoseHistoryRead or
            CharacterPoseOperationCode.PoseHistoryCommit =>
                CharacterPoseOperationFamily.PoseHistory,
            CharacterPoseOperationCode.FootPlacement or
            CharacterPoseOperationCode.PoseBoneIKGoals =>
                CharacterPoseOperationFamily.GoalContribution,
            CharacterPoseOperationCode.FullBodyIkGoalAssembler =>
                CharacterPoseOperationFamily.GoalAssembler,
            CharacterPoseOperationCode.FullBodyIK =>
                CharacterPoseOperationFamily.FullBodyIk,
            CharacterPoseOperationCode.LinkedPoseCall =>
                CharacterPoseOperationFamily.LinkedPose,
            CharacterPoseOperationCode.OutputPose =>
                CharacterPoseOperationFamily.Output,
            _ => throw new ArgumentOutOfRangeException(nameof(code))
        };
    }

    [Serializable]
    public sealed class CharacterPresentationPoseParameterEntry
    {
        [SerializeField] int m_Index;
        [SerializeField] string m_ParameterId = string.Empty;
        [SerializeField] PoseParameterValueType m_ValueType;
        [SerializeField] string m_Unit = string.Empty;
        [SerializeField] float m_DefaultValue;

        public CharacterPresentationPoseParameterEntry(
            int index,
            PoseParameterId parameterId,
            PoseParameterValueType valueType,
            float defaultValue,
            string unit)
        {
            if (index < 0 || !parameterId.IsValid || !Enum.IsDefined(typeof(PoseParameterValueType), valueType) ||
                !float.IsFinite(defaultValue))
                throw new ArgumentException("Compiled Pose Parameter entry is invalid.");
            m_Index = index;
            m_ParameterId = parameterId.Value;
            m_ValueType = valueType;
            m_Unit = unit?.Trim() ?? string.Empty;
            m_DefaultValue = defaultValue;
        }

        public int Index => m_Index;
        public PoseParameterId ParameterId => new PoseParameterId(m_ParameterId);
        public PoseParameterValueType ValueType => m_ValueType;
        public string Unit => m_Unit ?? string.Empty;
        public float DefaultValue => m_DefaultValue;
    }

    [Serializable]
    public sealed class CharacterPresentationDenseBoneMask
    {
        [SerializeField] int m_Index;
        [SerializeField] string m_MaskId = string.Empty;
        [SerializeField] float[] m_Weights = Array.Empty<float>();

        public CharacterPresentationDenseBoneMask(int index, string maskId, float[] weights)
        {
            if (index < 0 || string.IsNullOrWhiteSpace(maskId) || weights == null || weights.Length == 0)
                throw new ArgumentException("Compiled dense Bone Mask is invalid.");
            m_Index = index;
            m_MaskId = maskId.Trim();
            m_Weights = (float[])weights.Clone();
            for (int i = 0; i < m_Weights.Length; i++)
            {
                if (!float.IsFinite(m_Weights[i]) || m_Weights[i] < 0f || m_Weights[i] > 1f)
                    throw new ArgumentOutOfRangeException(nameof(weights));
            }
        }

        public int Index => m_Index;
        public string MaskId => m_MaskId ?? string.Empty;
        public IReadOnlyList<float> Weights => m_Weights ?? Array.Empty<float>();
    }

    [Serializable]
    public sealed class CharacterPresentationAdditiveReferenceDescriptor
    {
        [SerializeField] int m_Index;
        [SerializeField] string m_ReferencePoseId = string.Empty;
        [SerializeField] AdditiveReferenceSpace m_Space;
        [SerializeField] AdditiveScalePolicy m_ScalePolicy;
        [SerializeField] Vector3[] m_Positions = Array.Empty<Vector3>();
        [SerializeField] Quaternion[] m_Rotations = Array.Empty<Quaternion>();
        [SerializeField] Vector3[] m_Scales = Array.Empty<Vector3>();

        public CharacterPresentationAdditiveReferenceDescriptor(
            int index,
            string referencePoseId,
            AdditiveReferenceSpace space,
            AdditiveScalePolicy scalePolicy,
            Vector3[] positions,
            Quaternion[] rotations,
            Vector3[] scales)
        {
            if (index < 0 || !string.Equals(referencePoseId, AnimationAdditiveReferencePoseIds.RigReference, StringComparison.Ordinal) ||
                !Enum.IsDefined(typeof(AdditiveReferenceSpace), space) || !Enum.IsDefined(typeof(AdditiveScalePolicy), scalePolicy) ||
                positions == null || rotations == null || scales == null || positions.Length == 0 ||
                positions.Length != rotations.Length || positions.Length != scales.Length)
                throw new ArgumentException("Compiled Additive reference descriptor is invalid.");
            m_Index = index;
            m_ReferencePoseId = referencePoseId;
            m_Space = space;
            m_ScalePolicy = scalePolicy;
            m_Positions = (Vector3[])positions.Clone();
            m_Rotations = (Quaternion[])rotations.Clone();
            m_Scales = (Vector3[])scales.Clone();
        }

        public int Index => m_Index;
        public string ReferencePoseId => m_ReferencePoseId ?? string.Empty;
        public AdditiveReferenceSpace Space => m_Space;
        public AdditiveScalePolicy ScalePolicy => m_ScalePolicy;
        public IReadOnlyList<Vector3> Positions => m_Positions ?? Array.Empty<Vector3>();
        public IReadOnlyList<Quaternion> Rotations => m_Rotations ?? Array.Empty<Quaternion>();
        public IReadOnlyList<Vector3> Scales => m_Scales ?? Array.Empty<Vector3>();
    }

    [Serializable]
    public sealed class CharacterPresentationModifyBoneDescriptor
    {
        [SerializeField] int m_Index;
        [SerializeField] int m_BoneIndex;
        [SerializeField] int m_ParentBoneIndex;
        [SerializeField] ModifyBoneReferenceSpace m_ReferenceSpace;
        [SerializeField] ModifyBoneOperationMask m_Operations;
        [SerializeField] Vector3 m_Position;
        [SerializeField] Quaternion m_Rotation;
        [SerializeField] Vector3 m_Scale;

        public CharacterPresentationModifyBoneDescriptor(
            int index,
            int boneIndex,
            int parentBoneIndex,
            CharacterModifyBonePosePayload payload)
        {
            if (index < 0 || boneIndex < 0 || parentBoneIndex < -1 || payload == null)
                throw new ArgumentException("Compiled Modify Bone descriptor is invalid.");
            m_Index = index;
            m_BoneIndex = boneIndex;
            m_ParentBoneIndex = parentBoneIndex;
            m_ReferenceSpace = payload.ReferenceSpace;
            m_Operations = payload.Operations;
            m_Position = payload.Position;
            m_Rotation = payload.Rotation;
            m_Scale = payload.Scale;
        }

        public int Index => m_Index;
        public int BoneIndex => m_BoneIndex;
        public int ParentBoneIndex => m_ParentBoneIndex;
        public ModifyBoneReferenceSpace ReferenceSpace => m_ReferenceSpace;
        public ModifyBoneOperationMask Operations => m_Operations;
        public Vector3 Position => m_Position;
        public Quaternion Rotation => m_Rotation;
        public Vector3 Scale => m_Scale;
    }

    [Serializable]
    public sealed class CharacterPresentationRootOrientationWarpDescriptor
    {
        [SerializeField] int m_Index;
        [SerializeField] string m_NodeId = string.Empty;
        [SerializeField] int m_ClipPlayerIndex = -1;
        [SerializeField] int m_RootPhysicalBoneIndex = -1;
        [SerializeField] float m_Duration;
        [SerializeField] float m_TotalYaw;
        [SerializeField] AnimationCurve m_YawCurve = new AnimationCurve();

        public CharacterPresentationRootOrientationWarpDescriptor(
            int index,
            PoseNodeId nodeId,
            int clipPlayerIndex,
            int rootPhysicalBoneIndex,
            float duration,
            float totalYaw,
            AnimationCurve yawCurve)
        {
            if (index < 0 || !nodeId.IsValid || clipPlayerIndex < 0 ||
                rootPhysicalBoneIndex < 0 || !float.IsFinite(duration) || duration <= 0f ||
                !float.IsFinite(totalYaw) || Math.Abs(totalYaw) <= 0.001f ||
                yawCurve == null || yawCurve.length < 2)
            {
                throw new ArgumentException("Compiled Root Orientation Warp descriptor is invalid.");
            }
            m_Index = index;
            m_NodeId = nodeId.Value;
            m_ClipPlayerIndex = clipPlayerIndex;
            m_RootPhysicalBoneIndex = rootPhysicalBoneIndex;
            m_Duration = duration;
            m_TotalYaw = totalYaw;
            m_YawCurve = new AnimationCurve(yawCurve.keys)
            {
                preWrapMode = yawCurve.preWrapMode,
                postWrapMode = yawCurve.postWrapMode
            };
        }

        public int Index => m_Index;
        public PoseNodeId NodeId => new PoseNodeId(m_NodeId);
        public int ClipPlayerIndex => m_ClipPlayerIndex;
        public int RootPhysicalBoneIndex => m_RootPhysicalBoneIndex;
        public float Duration => m_Duration;
        public float TotalYaw => m_TotalYaw;
        public AnimationCurve YawCurve => m_YawCurve;

        public void RequireValid(int clipPlayerCount, int physicalBoneCount)
        {
            if (Index < 0 || !NodeId.IsValid || ClipPlayerIndex < 0 ||
                ClipPlayerIndex >= clipPlayerCount || RootPhysicalBoneIndex < 0 ||
                RootPhysicalBoneIndex >= physicalBoneCount || !float.IsFinite(Duration) || Duration <= 0f ||
                !float.IsFinite(TotalYaw) || Math.Abs(TotalYaw) <= 0.001f ||
                YawCurve == null || YawCurve.length < 2 ||
                Math.Abs(YawCurve.keys[0].time) > 0.0001f ||
                Math.Abs(YawCurve.keys[YawCurve.length - 1].time - Duration) > 0.0001f ||
                Math.Abs(YawCurve.Evaluate(Duration) - TotalYaw) > 0.01f)
            {
                throw new InvalidOperationException($"Root Orientation Warp descriptor #{Index} is invalid.");
            }
        }
    }

    [Serializable]
    public sealed class CharacterPresentationInertializationRuleDescriptor
    {
        [SerializeField] int m_SourceEndpointIndex;
        [SerializeField] int m_TargetEndpointIndex;
        [SerializeField] PoseInertializationMode m_Mode;
        [SerializeField] float m_DurationSeconds;
        [SerializeField] int m_CurveIndex = -1;
        [SerializeField] int m_ProfileIndex = -1;
        [SerializeField] PoseParameterInertializationMode[] m_ParameterModes = Array.Empty<PoseParameterInertializationMode>();

        public CharacterPresentationInertializationRuleDescriptor(
            int sourceEndpointIndex,
            int targetEndpointIndex,
            PoseInertializationMode mode,
            float durationSeconds,
            int curveIndex,
            int profileIndex,
            PoseParameterInertializationMode[] parameterModes)
        {
            if (sourceEndpointIndex < 0 || targetEndpointIndex < 0 ||
                !Enum.IsDefined(typeof(PoseInertializationMode), mode) ||
                !float.IsFinite(durationSeconds) || durationSeconds < 0f ||
                mode == PoseInertializationMode.Inertialize &&
                (durationSeconds <= 0f || curveIndex < 0 || profileIndex < 0) ||
                mode == PoseInertializationMode.HardCut && (curveIndex != -1 || profileIndex != -1) ||
                parameterModes == null || parameterModes.Length == 0)
                throw new ArgumentException("Compiled Inertialization exact rule is invalid.");
            m_SourceEndpointIndex = sourceEndpointIndex;
            m_TargetEndpointIndex = targetEndpointIndex;
            m_Mode = mode;
            m_DurationSeconds = durationSeconds;
            m_CurveIndex = curveIndex;
            m_ProfileIndex = profileIndex;
            m_ParameterModes = parameterModes;
        }

        public int SourceEndpointIndex => m_SourceEndpointIndex;
        public int TargetEndpointIndex => m_TargetEndpointIndex;
        public PoseInertializationMode Mode => m_Mode;
        public float DurationSeconds => m_DurationSeconds;
        public int CurveIndex => m_CurveIndex;
        public int ProfileIndex => m_ProfileIndex;
        public IReadOnlyList<PoseParameterInertializationMode> ParameterModes => m_ParameterModes ?? Array.Empty<PoseParameterInertializationMode>();
    }

    [Serializable]
    public sealed class CharacterPresentationInertializationDescriptor
    {
        [SerializeField] int m_Index;
        [SerializeField] string m_NodeId = string.Empty;
        [SerializeField] PoseInertializationTemporalOwnerKind m_TemporalOwnerKind;
        [SerializeField] string m_InputOwnerNodeId = string.Empty;
        [SerializeField] int m_InputOwnerIndex;
        [SerializeField] string m_PolicyId = string.Empty;
        [SerializeField] string m_PolicyRevision = string.Empty;
        [SerializeField] CharacterPresentationInertializationRuleDescriptor[] m_Rules = Array.Empty<CharacterPresentationInertializationRuleDescriptor>();

        public CharacterPresentationInertializationDescriptor(
            int index,
            PoseNodeId nodeId,
            PoseInertializationTemporalOwnerKind temporalOwnerKind,
            PoseNodeId inputOwnerNodeId,
            int inputOwnerIndex,
            string policyId,
            string policyRevision,
            CharacterPresentationInertializationRuleDescriptor[] rules)
        {
            if (index < 0 || !nodeId.IsValid ||
                !Enum.IsDefined(typeof(PoseInertializationTemporalOwnerKind), temporalOwnerKind) ||
                !inputOwnerNodeId.IsValid || inputOwnerIndex < 0 ||
                string.IsNullOrWhiteSpace(policyId) || string.IsNullOrWhiteSpace(policyRevision) ||
                rules == null || rules.Length == 0)
                throw new ArgumentException("Compiled Inertialization descriptor is invalid.");
            m_Index = index;
            m_NodeId = nodeId.Value;
            m_TemporalOwnerKind = temporalOwnerKind;
            m_InputOwnerNodeId = inputOwnerNodeId.Value;
            m_InputOwnerIndex = inputOwnerIndex;
            m_PolicyId = policyId;
            m_PolicyRevision = policyRevision;
            m_Rules = rules;
        }

        public int Index => m_Index;
        public PoseNodeId NodeId => new PoseNodeId(m_NodeId);
        public PoseInertializationTemporalOwnerKind TemporalOwnerKind => m_TemporalOwnerKind;
        public PoseNodeId InputOwnerNodeId => new PoseNodeId(m_InputOwnerNodeId);
        public int InputOwnerIndex => m_InputOwnerIndex;
        public string PolicyId => m_PolicyId ?? string.Empty;
        public string PolicyRevision => m_PolicyRevision ?? string.Empty;
        public IReadOnlyList<CharacterPresentationInertializationRuleDescriptor> Rules => m_Rules ?? Array.Empty<CharacterPresentationInertializationRuleDescriptor>();
    }

    [Serializable]
    public sealed class CharacterPresentationPoseSourceMapEntry
    {
        [SerializeField] int m_OperationIndex;
        [SerializeField] string m_GraphId = string.Empty;
        [SerializeField] string m_NodeId = string.Empty;
        [SerializeField] string m_CallSite = string.Empty;

        public CharacterPresentationPoseSourceMapEntry(int operationIndex, string graphId, PoseNodeId nodeId, string callSite)
        {
            if (operationIndex < 0 || string.IsNullOrWhiteSpace(graphId) || !nodeId.IsValid)
                throw new ArgumentException("Pose operation source map entry is invalid.");
            m_OperationIndex = operationIndex;
            m_GraphId = graphId.Trim();
            m_NodeId = nodeId.Value;
            m_CallSite = callSite ?? string.Empty;
        }

        public int OperationIndex => m_OperationIndex;
        public string GraphId => m_GraphId ?? string.Empty;
        public PoseNodeId NodeId => new PoseNodeId(m_NodeId);
        public string CallSite => m_CallSite ?? string.Empty;
    }

    [Serializable]
    public sealed class CharacterPresentationPoseStage
    {
        [SerializeField] int m_Index;
        [SerializeField] CharacterPoseExecutionDomain m_ExecutionDomain;
        [SerializeField] CharacterPoseSpace m_InputPoseSpace;
        [SerializeField] CharacterPoseSpace m_OutputPoseSpace;
        [SerializeField] int m_OperationStart;
        [SerializeField] int m_OperationCount;
        [SerializeField] int m_NativeOperationStart;
        [SerializeField] int m_NativeOperationCount;
        [SerializeField] int m_PoseWorkspaceStart;
        [SerializeField] int m_PoseWorkspaceCount;
        [SerializeField] int m_CompletionIndex;
        [SerializeField] int m_DiagnosticIndex;

        public CharacterPresentationPoseStage(
            int index,
            CharacterPoseExecutionDomain executionDomain,
            CharacterPoseSpace inputPoseSpace,
            CharacterPoseSpace outputPoseSpace,
            int operationStart,
            int operationCount,
            int nativeOperationStart,
            int nativeOperationCount,
            int poseWorkspaceStart,
            int poseWorkspaceCount)
        {
            if (index < 0 ||
                !Enum.IsDefined(typeof(CharacterPoseExecutionDomain), executionDomain) ||
                !Enum.IsDefined(typeof(CharacterPoseSpace), inputPoseSpace) ||
                !Enum.IsDefined(typeof(CharacterPoseSpace), outputPoseSpace) ||
                operationStart < 0 || operationCount <= 0 ||
                nativeOperationStart < 0 || nativeOperationCount < 0 ||
                poseWorkspaceStart < 0 || poseWorkspaceCount < 0)
            {
                throw new ArgumentException("Compiled Pose stage is invalid.");
            }
            m_Index = index;
            m_ExecutionDomain = executionDomain;
            m_InputPoseSpace = inputPoseSpace;
            m_OutputPoseSpace = outputPoseSpace;
            m_OperationStart = operationStart;
            m_OperationCount = operationCount;
            m_NativeOperationStart = nativeOperationStart;
            m_NativeOperationCount = nativeOperationCount;
            m_PoseWorkspaceStart = poseWorkspaceStart;
            m_PoseWorkspaceCount = poseWorkspaceCount;
            m_CompletionIndex = index;
            m_DiagnosticIndex = index;
        }

        public int Index => m_Index;
        public CharacterPoseExecutionDomain ExecutionDomain => m_ExecutionDomain;
        public CharacterPoseSpace InputPoseSpace => m_InputPoseSpace;
        public CharacterPoseSpace OutputPoseSpace => m_OutputPoseSpace;
        public int OperationStart => m_OperationStart;
        public int OperationCount => m_OperationCount;
        public int NativeOperationStart => m_NativeOperationStart;
        public int NativeOperationCount => m_NativeOperationCount;
        public int PoseWorkspaceStart => m_PoseWorkspaceStart;
        public int PoseWorkspaceCount => m_PoseWorkspaceCount;
        public int CompletionIndex => m_CompletionIndex;
        public int DiagnosticIndex => m_DiagnosticIndex;
    }

    internal readonly struct CharacterFinalPosePublicationLayoutHandle :
        IEquatable<CharacterFinalPosePublicationLayoutHandle>
    {
        internal CharacterFinalPosePublicationLayoutHandle(
            int layoutSlotIndex,
            int outputOperationIndex,
            int outputValueIndex,
            int poseValueCount,
            int boneCount,
            int parameterCount,
            int contributionCapacity)
        {
            LayoutSlotIndex = layoutSlotIndex;
            OutputOperationIndex = outputOperationIndex;
            OutputValueIndex = outputValueIndex;
            PoseValueCount = poseValueCount;
            BoneCount = boneCount;
            ParameterCount = parameterCount;
            ContributionCapacity = contributionCapacity;
        }

        internal int LayoutSlotIndex { get; }
        internal int OutputOperationIndex { get; }
        internal int OutputValueIndex { get; }
        internal int PoseValueCount { get; }
        internal int BoneCount { get; }
        internal int ParameterCount { get; }
        internal int ContributionCapacity { get; }
        internal bool IsValid =>
            LayoutSlotIndex == 0 &&
            OutputOperationIndex >= 0 &&
            OutputValueIndex == PoseValueCount - 1 &&
            PoseValueCount > 1 &&
            BoneCount > 0 &&
            ParameterCount > 0 &&
            ContributionCapacity > 0;

        public bool Equals(CharacterFinalPosePublicationLayoutHandle other) =>
            LayoutSlotIndex == other.LayoutSlotIndex &&
            OutputOperationIndex == other.OutputOperationIndex &&
            OutputValueIndex == other.OutputValueIndex &&
            PoseValueCount == other.PoseValueCount &&
            BoneCount == other.BoneCount &&
            ParameterCount == other.ParameterCount &&
            ContributionCapacity == other.ContributionCapacity;

        public override bool Equals(object obj) =>
            obj is CharacterFinalPosePublicationLayoutHandle other &&
            Equals(other);

        public override int GetHashCode() => HashCode.Combine(
            LayoutSlotIndex,
            OutputOperationIndex,
            OutputValueIndex,
            PoseValueCount,
            BoneCount,
            ParameterCount,
            ContributionCapacity);

        internal void RequireValid()
        {
            if (!IsValid)
            {
                throw new InvalidOperationException(
                    "Final Pose Publication layout handle is invalid.");
            }
        }
    }

    [Serializable]
    public sealed partial class CharacterPoseProgramImage
    {
        public const string SchemaVersion = "character-presentation-pose-plan/v24";
        public const string RuntimeAbi = "character-presentation-pose-runtime/v27";

        [SerializeField] string m_SchemaVersion = SchemaVersion;
        [SerializeField] string m_RuntimeAbi = RuntimeAbi;
        [SerializeField] string m_PoseGraphId = string.Empty;
        [SerializeField] string m_ContentRevision = string.Empty;
        [SerializeField] string m_PlanHash = string.Empty;
        [SerializeField] string m_RigId = string.Empty;
        [SerializeField] string m_RigRevision = string.Empty;
        [NonSerialized] string m_ProgramId = string.Empty;
        [NonSerialized] string m_ProjectionRevision = string.Empty;
        [SerializeField] int m_PoseBoneCount;
        [SerializeField] int m_LeftFootBoneIndex = -1;
        [SerializeField] int m_RightFootBoneIndex = -1;
        [SerializeField] CharacterPresentationPoseParameterEntry[] m_Parameters = Array.Empty<CharacterPresentationPoseParameterEntry>();
        [SerializeField] AnimationBlendNodePayload[] m_BlendNodes = Array.Empty<AnimationBlendNodePayload>();
        [SerializeField] CharacterPresentationInertializationDescriptor[] m_Inertializations = Array.Empty<CharacterPresentationInertializationDescriptor>();
        [SerializeField] CharacterPresentationDenseBoneMask[] m_BoneMasks = Array.Empty<CharacterPresentationDenseBoneMask>();
        [SerializeField] CharacterPresentationAdditiveReferenceDescriptor[] m_AdditiveReferences = Array.Empty<CharacterPresentationAdditiveReferenceDescriptor>();
        [SerializeField] CharacterPresentationModifyBoneDescriptor[] m_ModifyBones = Array.Empty<CharacterPresentationModifyBoneDescriptor>();
        [SerializeField] CharacterPresentationRootOrientationWarpDescriptor[] m_RootOrientationWarps = Array.Empty<CharacterPresentationRootOrientationWarpDescriptor>();
        [SerializeField] CharacterPresentationPoseBoneIkGoalsDescriptor[] m_PoseBoneIkGoalSources = Array.Empty<CharacterPresentationPoseBoneIkGoalsDescriptor>();
        [SerializeField] CharacterPresentationFootPlacementDescriptor[] m_FootPlacements = Array.Empty<CharacterPresentationFootPlacementDescriptor>();
        [SerializeField] CharacterPresentationFullBodyIkDescriptor[] m_FullBodyIks = Array.Empty<CharacterPresentationFullBodyIkDescriptor>();
        [SerializeField] CharacterPresentationClipPlayerDescriptor[] m_ClipPlayers = Array.Empty<CharacterPresentationClipPlayerDescriptor>();
        [SerializeField] CharacterPoseStateMachineDescriptor[] m_StateMachines =
            Array.Empty<CharacterPoseStateMachineDescriptor>();
        [SerializeField] CharacterAnimationSlotDescriptor[] m_AnimationSlots =
            Array.Empty<CharacterAnimationSlotDescriptor>();
        [SerializeField] ActionPlaybackInputPlan[] m_ActionPlaybackInputs =
            Array.Empty<ActionPlaybackInputPlan>();
        [SerializeField] CharacterPoseOperationPages m_OperationPages;
        [SerializeField] CharacterPresentationPoseSourceMapEntry[] m_SourceMap = Array.Empty<CharacterPresentationPoseSourceMapEntry>();
        [SerializeField] CharacterPresentationPoseStage[] m_Stages = Array.Empty<CharacterPresentationPoseStage>();
        [SerializeField] CharacterPoseWorkerPlan m_WorkerPlan;
        [SerializeField] int m_PoseValueWorkspaceCount;
        [SerializeField] int m_FullBodyIkGoalContributionWorkspaceCount;
        [SerializeField] int m_FullBodyIkGoalSetWorkspaceCount;
        [SerializeField] int m_FullBodyIkGoalContributionGoalWorkspaceCount;
        [SerializeField] int m_ParameterWorkspaceCount;
        [SerializeField] int m_ContributionWorkspaceCount;
        [SerializeField] int m_FrameCacheCount;
        [SerializeField] int m_OutputOperationIndex = -1;

        public CharacterPoseProgramImage(
            string poseGraphId,
            string contentRevision,
            string planHash,
            CharacterAnimationRigDefinition rig,
            CharacterPresentationPoseParameterEntry[] parameters,
            AnimationBlendNodePayload[] blendNodes,
            CharacterPresentationInertializationDescriptor[] inertializations,
            CharacterPresentationDenseBoneMask[] boneMasks,
            CharacterPresentationAdditiveReferenceDescriptor[] additiveReferences,
            CharacterPresentationModifyBoneDescriptor[] modifyBones,
            CharacterPresentationRootOrientationWarpDescriptor[] rootOrientationWarps,
            CharacterPresentationPoseBoneIkGoalsDescriptor[] poseBoneIkGoalSources,
            CharacterPresentationFootPlacementDescriptor[] footPlacements,
            CharacterPresentationFullBodyIkDescriptor[] fullBodyIks,
            CharacterPresentationClipPlayerDescriptor[] clipPlayers,
            CharacterPoseStateMachineDescriptor[] stateMachines,
            CharacterAnimationSlotDescriptor[] animationSlots,
            ActionPlaybackInputPlan[] actionPlaybackInputs,
            CharacterMotionMatchingPosePlanDescriptor[] motionMatchingNodes,
            CharacterPoseHistoryCollectorPlanDescriptor[]
                poseHistoryCollectors,
            CharacterMotionMatchingEntryProgramDescriptor[]
                motionMatchingEntryPrograms,
            CharacterMotionMatchingBlendPlanDescriptor[]
                motionMatchingBlendPlans,
            CharacterLinkedPoseEntryFragmentPlanDescriptor[] linkedPoseFragments,
            CharacterLinkedPoseCallPlanDescriptor[] linkedPoseCalls,
            CharacterPoseOperationPages operationPages,
            CharacterPresentationPoseSourceMapEntry[] sourceMap,
            CharacterPresentationPoseStage[] stages,
            CharacterPoseWorkerPlan workerPlan,
            int poseValueCount,
            int poseValueWorkspaceCount,
            int fullBodyIkGoalContributionWorkspaceCount,
            int fullBodyIkGoalSetWorkspaceCount,
            int fullBodyIkGoalContributionGoalWorkspaceCount,
            int parameterWorkspaceCount,
            int contributionCapacity,
            int frameCacheCount,
            int outputOperationIndex)
        {
            if (!rig)
                throw new ArgumentNullException(nameof(rig));
            rig.RequireValid();
            m_PoseGraphId = PoseIdentity.Require(poseGraphId, nameof(poseGraphId));
            m_ContentRevision = PoseIdentity.Require(contentRevision, nameof(contentRevision));
            m_PlanHash = PoseIdentity.Require(planHash, nameof(planHash));
            m_RigId = rig.RigId;
            m_RigRevision = rig.Revision;
            m_PoseBoneCount = rig.PoseBoneCount;
            m_LeftFootBoneIndex = rig.RequirePhysicalBoneIndex(rig.LeftLeg.AnkleBoneId);
            m_RightFootBoneIndex = rig.RequirePhysicalBoneIndex(rig.RightLeg.AnkleBoneId);
            m_Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
            m_BlendNodes = blendNodes ?? throw new ArgumentNullException(nameof(blendNodes));
            m_Inertializations = inertializations ?? throw new ArgumentNullException(nameof(inertializations));
            m_BoneMasks = boneMasks ?? throw new ArgumentNullException(nameof(boneMasks));
            m_AdditiveReferences = additiveReferences ?? throw new ArgumentNullException(nameof(additiveReferences));
            m_ModifyBones = modifyBones ?? throw new ArgumentNullException(nameof(modifyBones));
            m_RootOrientationWarps = rootOrientationWarps ?? throw new ArgumentNullException(nameof(rootOrientationWarps));
            m_PoseBoneIkGoalSources = poseBoneIkGoalSources ?? throw new ArgumentNullException(nameof(poseBoneIkGoalSources));
            m_FootPlacements = footPlacements ?? throw new ArgumentNullException(nameof(footPlacements));
            m_FullBodyIks = fullBodyIks ?? throw new ArgumentNullException(nameof(fullBodyIks));
            m_ClipPlayers = clipPlayers ?? throw new ArgumentNullException(nameof(clipPlayers));
            m_StateMachines = stateMachines ?? throw new ArgumentNullException(nameof(stateMachines));
            m_AnimationSlots = animationSlots ?? throw new ArgumentNullException(nameof(animationSlots));
            m_ActionPlaybackInputs = actionPlaybackInputs ??
                throw new ArgumentNullException(nameof(actionPlaybackInputs));
            m_MotionMatchingNodes = motionMatchingNodes ??
                throw new ArgumentNullException(nameof(motionMatchingNodes));
            m_PoseHistoryCollectors = poseHistoryCollectors ??
                throw new ArgumentNullException(nameof(poseHistoryCollectors));
            m_MotionMatchingEntryPrograms = motionMatchingEntryPrograms ??
                throw new ArgumentNullException(
                    nameof(motionMatchingEntryPrograms));
            m_MotionMatchingBlendPlans = motionMatchingBlendPlans ??
                throw new ArgumentNullException(nameof(motionMatchingBlendPlans));
            m_LinkedPoseFragments = linkedPoseFragments ?? throw new ArgumentNullException(nameof(linkedPoseFragments));
            m_LinkedPoseCalls = linkedPoseCalls ?? throw new ArgumentNullException(nameof(linkedPoseCalls));
            m_OperationPages = operationPages ??
                throw new ArgumentNullException(nameof(operationPages));
            m_SourceMap = sourceMap ?? throw new ArgumentNullException(nameof(sourceMap));
            m_Stages = stages ?? throw new ArgumentNullException(nameof(stages));
            m_WorkerPlan = workerPlan ??
                throw new ArgumentNullException(nameof(workerPlan));
            if (poseValueCount <= 1 ||
                poseValueWorkspaceCount != poseValueCount - 1 ||
                contributionCapacity <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(poseValueWorkspaceCount));
            }
            m_PoseValueWorkspaceCount = poseValueCount;
            m_FullBodyIkGoalContributionWorkspaceCount =
                fullBodyIkGoalContributionWorkspaceCount;
            m_FullBodyIkGoalSetWorkspaceCount = fullBodyIkGoalSetWorkspaceCount;
            m_FullBodyIkGoalContributionGoalWorkspaceCount =
                fullBodyIkGoalContributionGoalWorkspaceCount;
            m_ParameterWorkspaceCount = parameterWorkspaceCount;
            m_ContributionWorkspaceCount = checked(
                poseValueCount * contributionCapacity);
            m_FrameCacheCount = frameCacheCount;
            m_OutputOperationIndex = outputOperationIndex;
            RequireValid();
            RequireMotionMatchingPlan();
        }

        public string PoseGraphId => m_PoseGraphId ?? string.Empty;
        public string ContentRevision => m_ContentRevision ?? string.Empty;
        public string PlanHash => m_PlanHash ?? string.Empty;
        public string ProgramId => m_ProgramId ?? string.Empty;
        public string ProjectionRevision => m_ProjectionRevision ?? string.Empty;
        public string PoseProgramImageHash => PlanHash;
        public string RigId => m_RigId ?? string.Empty;
        public string RigRevision => m_RigRevision ?? string.Empty;
        public int PoseBoneCount => m_PoseBoneCount;
        public int LeftFootBoneIndex => m_LeftFootBoneIndex;
        public int RightFootBoneIndex => m_RightFootBoneIndex;
        public IReadOnlyList<CharacterPresentationPoseParameterEntry> Parameters => m_Parameters ?? Array.Empty<CharacterPresentationPoseParameterEntry>();
        public IReadOnlyList<AnimationBlendNodePayload> BlendNodes => m_BlendNodes ?? Array.Empty<AnimationBlendNodePayload>();
        public IReadOnlyList<CharacterPresentationInertializationDescriptor> Inertializations => m_Inertializations ?? Array.Empty<CharacterPresentationInertializationDescriptor>();
        public int PlayerCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < OperationHeaders.Count; i++)
                {
                    CharacterPoseOperationCode code = OperationHeaders[i].Code;
                    if (code == CharacterPoseOperationCode.SelectedPosePlayer || code == CharacterPoseOperationCode.BlendStack ||
                        code == CharacterPoseOperationCode.BlendSpacePlayer || code == CharacterPoseOperationCode.ClipPlayer ||
                        code == CharacterPoseOperationCode.AnimationSlot)
                        count++;
                }
                return count;
            }
        }
        public IReadOnlyList<CharacterPresentationDenseBoneMask> BoneMasks => m_BoneMasks ?? Array.Empty<CharacterPresentationDenseBoneMask>();
        public IReadOnlyList<CharacterPresentationAdditiveReferenceDescriptor> AdditiveReferences => m_AdditiveReferences ?? Array.Empty<CharacterPresentationAdditiveReferenceDescriptor>();
        public IReadOnlyList<CharacterPresentationModifyBoneDescriptor> ModifyBones => m_ModifyBones ?? Array.Empty<CharacterPresentationModifyBoneDescriptor>();
        public IReadOnlyList<CharacterPresentationRootOrientationWarpDescriptor> RootOrientationWarps => m_RootOrientationWarps ?? Array.Empty<CharacterPresentationRootOrientationWarpDescriptor>();
        public IReadOnlyList<CharacterPresentationPoseBoneIkGoalsDescriptor> PoseBoneIkGoalSources => m_PoseBoneIkGoalSources ?? Array.Empty<CharacterPresentationPoseBoneIkGoalsDescriptor>();
        public IReadOnlyList<CharacterPresentationFootPlacementDescriptor> FootPlacements => m_FootPlacements ?? Array.Empty<CharacterPresentationFootPlacementDescriptor>();
        public IReadOnlyList<CharacterPresentationFullBodyIkDescriptor> FullBodyIks => m_FullBodyIks ?? Array.Empty<CharacterPresentationFullBodyIkDescriptor>();
        public IReadOnlyList<CharacterPresentationClipPlayerDescriptor> ClipPlayers => m_ClipPlayers ?? Array.Empty<CharacterPresentationClipPlayerDescriptor>();
        public IReadOnlyList<CharacterPoseStateMachineDescriptor> StateMachines =>
            m_StateMachines ?? Array.Empty<CharacterPoseStateMachineDescriptor>();
        public IReadOnlyList<CharacterAnimationSlotDescriptor> AnimationSlots =>
            m_AnimationSlots ?? Array.Empty<CharacterAnimationSlotDescriptor>();
        public IReadOnlyList<ActionPlaybackInputPlan> ActionPlaybackInputs =>
            m_ActionPlaybackInputs ?? Array.Empty<ActionPlaybackInputPlan>();
        public CharacterPoseOperationPages OperationPages => m_OperationPages;
        public IReadOnlyList<CharacterPoseOperationHeader> OperationHeaders =>
            OperationPages?.Headers ?? Array.Empty<CharacterPoseOperationHeader>();
        public IReadOnlyList<CharacterPresentationPoseSourceMapEntry> SourceMap => m_SourceMap ?? Array.Empty<CharacterPresentationPoseSourceMapEntry>();
        public IReadOnlyList<CharacterPresentationPoseStage> Stages => m_Stages ?? Array.Empty<CharacterPresentationPoseStage>();
        public CharacterPoseWorkerPlan WorkerPlan => m_WorkerPlan;
        public int PoseValueCount => m_PoseValueWorkspaceCount;
        public int PoseValueWorkspaceCount => PoseValueCount - 1;
        public int FullBodyIkGoalContributionWorkspaceCount =>
            m_FullBodyIkGoalContributionWorkspaceCount;
        public int FullBodyIkGoalSetWorkspaceCount => m_FullBodyIkGoalSetWorkspaceCount;
        public int FullBodyIkGoalContributionGoalWorkspaceCount =>
            m_FullBodyIkGoalContributionGoalWorkspaceCount;
        public int ParameterWorkspaceCount => m_ParameterWorkspaceCount;
        public int ContributionCapacity =>
            PoseValueCount > 0 &&
            m_ContributionWorkspaceCount > 0 &&
            m_ContributionWorkspaceCount % PoseValueCount == 0
                ? m_ContributionWorkspaceCount / PoseValueCount
                : 0;
        public int ContributionWorkspaceCount => checked(
            PoseValueWorkspaceCount * ContributionCapacity);
        public int FrameCacheCount => m_FrameCacheCount;
        public int OutputOperationIndex => m_OutputOperationIndex;
        internal CharacterFinalPosePublicationLayoutHandle
            FinalPosePublicationLayout
        {
            get
            {
                if ((uint)OutputOperationIndex >= (uint)OperationHeaders.Count ||
                    PoseValueCount <= 1 ||
                    PoseValueWorkspaceCount != PoseValueCount - 1 ||
                    ContributionCapacity <= 0)
                {
                    return default;
                }
                CharacterPoseOperationHeader output =
                    OperationHeaders[OutputOperationIndex];
                return output == null
                    ? default
                    : new CharacterFinalPosePublicationLayoutHandle(
                        0,
                        OutputOperationIndex,
                        OperationPages.FindOutputValueIndex(
                            output,
                            CharacterPoseValueReferenceKind.Pose),
                        PoseValueCount,
                        PoseBoneCount,
                        Parameters.Count,
                        ContributionCapacity);
            }
        }

        internal void BindProjectionIdentity(
            string programId,
            string projectionRevision)
        {
            programId = new ProgramId(programId).Value;
            projectionRevision = PoseIdentity.Require(
                projectionRevision,
                nameof(projectionRevision));
            if (!string.IsNullOrEmpty(m_ProgramId) &&
                (!string.Equals(
                     m_ProgramId,
                     programId,
                     StringComparison.Ordinal) ||
                 !string.Equals(
                     m_ProjectionRevision,
                     projectionRevision,
                     StringComparison.Ordinal)))
            {
                throw new InvalidOperationException(
                    "Pose Program Image projection identity is already bound.");
            }
            m_ProgramId = programId;
            m_ProjectionRevision = projectionRevision;
        }

        internal void RequireProjectionIdentity(
            string programId,
            string projectionRevision,
            string rigId,
            string rigRevision)
        {
            if (string.IsNullOrEmpty(ProgramId) ||
                string.IsNullOrEmpty(ProjectionRevision) ||
                string.IsNullOrEmpty(PoseProgramImageHash) ||
                !string.Equals(
                    ProgramId,
                    programId,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    ProjectionRevision,
                    projectionRevision,
                    StringComparison.Ordinal) ||
                !string.Equals(RigId, rigId, StringComparison.Ordinal) ||
                !string.Equals(
                    RigRevision,
                    rigRevision,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Pose Program Image projection identity is inconsistent.");
            }
        }

        public int RequireParameterIndex(PoseParameterId parameterId)
        {
            for (int i = 0; i < Parameters.Count; i++)
            {
                if (Parameters[i].ParameterId.Equals(parameterId))
                    return i;
            }
            throw new InvalidOperationException($"Pose Plan has no Parameter '{parameterId}'.");
        }

        public AnimationBlendNodePayload RequireBlendNode(PoseNodeId nodeId)
        {
            AnimationBlendNodePayload result = null;
            for (int i = 0; i < BlendNodes.Count; i++)
            {
                AnimationBlendNodePayload candidate = BlendNodes[i];
                if (candidate == null || candidate.NodeId != nodeId)
                    continue;
                if (result != null)
                    throw new InvalidOperationException($"Pose Plan duplicates Blend Stack '{nodeId}'.");
                result = candidate;
            }
            return result ?? throw new InvalidOperationException($"Pose Plan has no Blend Stack '{nodeId}'.");
        }

        public void RequireValid()
        {
            if (!string.Equals(m_SchemaVersion, SchemaVersion, StringComparison.Ordinal) ||
                !string.Equals(m_RuntimeAbi, RuntimeAbi, StringComparison.Ordinal) ||
                string.IsNullOrEmpty(PoseGraphId) || string.IsNullOrEmpty(ContentRevision) || string.IsNullOrEmpty(PlanHash) ||
                string.IsNullOrEmpty(RigId) || string.IsNullOrEmpty(RigRevision) || PoseBoneCount <= 0 ||
                OperationPages == null || OperationHeaders.Count == 0 ||
                SourceMap.Count != OperationHeaders.Count || Stages.Count == 0 ||
                PoseValueCount <= 1 ||
                PoseValueWorkspaceCount != PoseValueCount - 1 ||
                FullBodyIkGoalContributionWorkspaceCount < 0 ||
                FullBodyIkGoalSetWorkspaceCount != 1 ||
                FullBodyIkGoalContributionGoalWorkspaceCount < 0 ||
                ParameterWorkspaceCount < Parameters.Count ||
                ContributionCapacity <= 0 ||
                m_ContributionWorkspaceCount !=
                checked(PoseValueCount * ContributionCapacity) ||
                FrameCacheCount != OperationHeaders.Count || OutputOperationIndex < 0 ||
                OutputOperationIndex >= OperationHeaders.Count)
                throw new InvalidOperationException("Character Presentation Pose Plan header or workspace is invalid.");

            CharacterFinalPosePublicationLayoutHandle publicationLayout =
                FinalPosePublicationLayout;
            publicationLayout.RequireValid();
            OperationPages.RequireValid();
            WorkerPlan?.RequireValid(
                OperationPages,
                Stages,
                PoseValueCount,
                FrameCacheCount,
                RigId,
                RigRevision);
            if (WorkerPlan == null)
                throw new InvalidOperationException(
                    "Character Presentation Pose Plan has no Worker Batch Plan.");
            for (int i = 0; i < OperationPages.ValueReferences.Count; i++)
            {
                CharacterPoseValueReference reference =
                    OperationPages.ValueReferences[i];
                int capacity = reference.Kind switch
                {
                    CharacterPoseValueReferenceKind.Pose => PoseValueCount,
                    CharacterPoseValueReferenceKind.Parameter =>
                        Parameters.Count,
                    CharacterPoseValueReferenceKind.OperationControl =>
                        OperationHeaders.Count,
                    CharacterPoseValueReferenceKind.FullBodyIkGoalContribution =>
                        FullBodyIkGoalContributionWorkspaceCount,
                    CharacterPoseValueReferenceKind.FullBodyIkGoalSet =>
                        FullBodyIkGoalSetWorkspaceCount,
                    _ => 0
                };
                if ((uint)reference.Index >= (uint)capacity)
                {
                    throw new InvalidOperationException(
                        $"Pose Value reference #{i} is outside its typed workspace.");
                }
            }

            var actionProducerIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < ActionPlaybackInputs.Count; i++)
            {
                ActionPlaybackInputPlan input = ActionPlaybackInputs[i];
                input?.RequireValid();
                if (input == null ||
                    input.Index != i ||
                    !actionProducerIds.Add(input.ProgramProducerId) ||
                    (uint)input.SlotIndex >= (uint)AnimationSlots.Count)
                {
                    throw new InvalidOperationException(
                        $"Pose Plan Action Playback input #{i} is invalid or duplicated.");
                }
                CharacterAnimationSlotDescriptor slot = AnimationSlots[input.SlotIndex];
                if (slot.SlotId != input.SlotId ||
                    slot.NodeId != input.SlotNodeId ||
                    slot.AnimationChannelId != input.AnimationChannelId ||
                    slot.ActionPlayer.PlayerIndex != input.ActionPlayerIndex ||
                    slot.ActionPlayer.PlayerNodeId != input.ActionPlayerNodeId)
                {
                    throw new InvalidOperationException(
                        $"Pose Plan Action Playback input #{i} does not match its Slot.");
                }
            }
            var operationNodes = new HashSet<PoseNodeId>();
            var playerIndices = new HashSet<int>();
            var poseValueProducers = new Dictionary<int, CharacterPoseOperationHeader>();
            var goalContributionProducers =
                new Dictionary<int, CharacterPoseOperationHeader>();
            var goalSetProducers = new Dictionary<int, CharacterPoseOperationHeader>();
            int outputCount = 0;
            for (int i = 0; i < OperationHeaders.Count; i++)
            {
                CharacterPoseOperationHeader operation = OperationHeaders[i];
                CharacterPresentationPoseSourceMapEntry source = SourceMap[i];
                if (operation == null || operation.Index != i ||
                    operation.Version != CharacterPoseOperationHeader.PayloadVersion ||
                    CharacterPoseOperationFamilies.RequireFamily(operation.Code) != operation.Family ||
                    !operationNodes.Add(operation.NodeId) || source == null || source.OperationIndex != i || source.NodeId != operation.NodeId)
                    throw new InvalidOperationException($"Pose Plan operation #{i} or source map is invalid.");
                OperationPages.RequirePayload(operation);
                int inputPoseA = OperationPages.FindInputValueIndex(
                    operation,
                    CharacterPoseValueReferenceKind.Pose);
                int inputPoseB = OperationPages.FindInputValueIndex(
                    operation,
                    CharacterPoseValueReferenceKind.Pose,
                    1);
                int outputPose = OperationPages.FindOutputValueIndex(
                    operation,
                    CharacterPoseValueReferenceKind.Pose);
                int outputGoalContribution = OperationPages.FindOutputValueIndex(
                    operation,
                    CharacterPoseValueReferenceKind.FullBodyIkGoalContribution);
                int outputGoalSet = OperationPages.FindOutputValueIndex(
                    operation,
                    CharacterPoseValueReferenceKind.FullBodyIkGoalSet);
                int inputGoalSet = OperationPages.FindInputValueIndex(
                    operation,
                    CharacterPoseValueReferenceKind.FullBodyIkGoalSet);
                RequirePoseInputDependency(operation, inputPoseA, poseValueProducers);
                RequirePoseInputDependency(operation, inputPoseB, poseValueProducers);
                RequireFullBodyIkGoalTopology(
                    operation,
                    goalContributionProducers,
                    goalSetProducers);
                if (outputPose >= 0 &&
                    ((uint)outputPose >= (uint)PoseValueCount ||
                     !poseValueProducers.TryAdd(outputPose, operation)))
                {
                    throw new InvalidOperationException($"Pose Plan operation #{i} has an invalid or duplicated Pose output value.");
                }
                if (outputGoalSet >= 0 &&
                    ((uint)outputGoalSet >= (uint)FullBodyIkGoalSetWorkspaceCount ||
                     !goalSetProducers.TryAdd(outputGoalSet, operation)))
                {
                    throw new InvalidOperationException($"Pose Plan operation #{i} has an invalid or duplicated Full Body IK Goal Set output value.");
                }
                if (outputGoalContribution >= 0 &&
                    ((uint)outputGoalContribution >=
                     (uint)FullBodyIkGoalContributionWorkspaceCount ||
                     !goalContributionProducers.TryAdd(
                         outputGoalContribution,
                         operation)))
                {
                    throw new InvalidOperationException(
                        $"Pose Plan operation #{i} has an invalid or duplicated Full Body IK Goal Contribution output value.");
                }
                if (operation.Code == CharacterPoseOperationCode.FootPlacement &&
                    (operation.ExecutionDomain != CharacterPoseExecutionDomain.WorldAwareValue ||
                     operation.InputPoseSpace != CharacterPoseSpace.Component ||
                     operation.OutputPoseSpace != CharacterPoseSpace.None ||
                     outputGoalContribution < 0 || outputGoalSet >= 0 ||
                     inputGoalSet >= 0 ||
                     OperationPages.CountInputValues(
                         operation,
                         CharacterPoseValueReferenceKind.FullBodyIkGoalContribution) != 0 ||
                     (uint)((CharacterPoseGoalContributionOperationPayload)
                         OperationPages.RequirePayload(operation)).FootPlacementIndex >=
                     (uint)FootPlacements.Count))
                {
                    throw new InvalidOperationException($"Pose Plan Foot Placement operation #{i} boundary is invalid.");
                }
                if (operation.Code == CharacterPoseOperationCode.PoseBoneIKGoals &&
                    (operation.ExecutionDomain != CharacterPoseExecutionDomain.ManagedConstraint ||
                     operation.InputPoseSpace != CharacterPoseSpace.Component ||
                     operation.OutputPoseSpace != CharacterPoseSpace.None ||
                     outputGoalContribution < 0 || outputGoalSet >= 0 ||
                     inputGoalSet >= 0 ||
                     OperationPages.CountInputValues(
                         operation,
                         CharacterPoseValueReferenceKind.FullBodyIkGoalContribution) != 0 ||
                     (uint)((CharacterPoseGoalContributionOperationPayload)
                         OperationPages.RequirePayload(operation)).PoseBoneIkGoalsIndex >=
                     (uint)PoseBoneIkGoalSources.Count))
                {
                    throw new InvalidOperationException($"Pose Plan Pose Bone IK Goals operation #{i} boundary is invalid.");
                }
                if (operation.Code == CharacterPoseOperationCode.FullBodyIkGoalAssembler &&
                    (operation.ExecutionDomain != CharacterPoseExecutionDomain.ManagedConstraint ||
                     operation.InputPoseSpace != CharacterPoseSpace.None ||
                     operation.OutputPoseSpace != CharacterPoseSpace.None ||
                     outputPose >= 0 || outputGoalContribution >= 0 ||
                     outputGoalSet < 0 || inputGoalSet >= 0 ||
                     OperationPages.CountInputValues(
                         operation,
                         CharacterPoseValueReferenceKind.FullBodyIkGoalContribution) == 0))
                {
                    throw new InvalidOperationException(
                        $"Pose Plan Full Body IK Goal Assembler operation #{i} boundary is invalid.");
                }
                if (operation.Code == CharacterPoseOperationCode.FullBodyIK &&
                    (operation.ExecutionDomain != CharacterPoseExecutionDomain.ManagedConstraint ||
                     operation.InputPoseSpace != CharacterPoseSpace.Component ||
                     operation.OutputPoseSpace != CharacterPoseSpace.Component ||
                     outputGoalContribution >= 0 || outputGoalSet >= 0 ||
                     inputGoalSet < 0 ||
                     OperationPages.CountInputValues(
                         operation,
                         CharacterPoseValueReferenceKind.FullBodyIkGoalContribution) != 0 ||
                     OperationPages.CountInputValues(
                         operation,
                         CharacterPoseValueReferenceKind.Parameter) != 0 ||
                     (uint)((CharacterPoseIndexedOperationPayload)
                         OperationPages.RequirePayload(operation)).ValueIndex >=
                     (uint)FullBodyIks.Count))
                {
                    throw new InvalidOperationException($"Pose Plan Full Body IK operation #{i} boundary is invalid.");
                }
                if (operation.Code == CharacterPoseOperationCode.OutputPose)
                {
                    outputCount++;
                    if (i != OutputOperationIndex || operation.ExecutionDomain != CharacterPoseExecutionDomain.FinalPublication ||
                        operation.InputPoseSpace != CharacterPoseSpace.Local || operation.OutputPoseSpace != CharacterPoseSpace.Local ||
                        outputPose != publicationLayout.OutputValueIndex)
                        throw new InvalidOperationException("Pose Plan Output operation boundary is inconsistent.");
                }
                if (operation.Code == CharacterPoseOperationCode.SelectedPosePlayer || operation.Code == CharacterPoseOperationCode.BlendStack ||
                    operation.Code == CharacterPoseOperationCode.BlendSpacePlayer || operation.Code == CharacterPoseOperationCode.ClipPlayer ||
                    operation.Code == CharacterPoseOperationCode.AnimationSlot)
                {
                    int playerIndex = RequirePlayerIndex(operation);
                    if (playerIndex < 0 || !playerIndices.Add(playerIndex))
                        throw new InvalidOperationException($"Pose Plan Player operation #{i} has an invalid runtime index.");
                }
                if (operation.Code == CharacterPoseOperationCode.ClipPlayer)
                {
                    CharacterPosePlayerOperationPayload player =
                        (CharacterPosePlayerOperationPayload)
                        OperationPages.RequirePayload(operation);
                    if ((uint)player.ClipPlayerIndex >= (uint)ClipPlayers.Count)
                        throw new InvalidOperationException($"Pose Plan Clip Player operation #{i} has no descriptor.");
                    CharacterPresentationClipPlayerDescriptor descriptor =
                        ClipPlayers[player.ClipPlayerIndex];
                    descriptor?.RequireValid();
                    if (descriptor == null || descriptor.Index != player.ClipPlayerIndex ||
                        descriptor.NodeId != operation.NodeId ||
                        descriptor.PlayerIndex != player.PlayerIndex)
                    {
                        throw new InvalidOperationException($"Pose Plan Clip Player operation #{i} descriptor ownership is invalid.");
                    }
                }
                if (operation.Code == CharacterPoseOperationCode.RootOrientationWarp)
                {
                    CharacterPoseComponentControlOperationPayload control =
                        (CharacterPoseComponentControlOperationPayload)
                        OperationPages.RequirePayload(operation);
                    if ((uint)control.RootOrientationWarpIndex >=
                        (uint)RootOrientationWarps.Count)
                    {
                        throw new InvalidOperationException(
                            $"Pose Plan Root Orientation Warp operation #{i} has no descriptor.");
                    }
                    CharacterPresentationRootOrientationWarpDescriptor descriptor =
                        RootOrientationWarps[control.RootOrientationWarpIndex];
                    descriptor?.RequireValid(
                        ClipPlayers.Count,
                        PoseBoneCount);
                    if (descriptor == null ||
                        descriptor.Index != control.RootOrientationWarpIndex ||
                        descriptor.NodeId != operation.NodeId ||
                        !poseValueProducers.TryGetValue(
                            inputPoseA,
                            out CharacterPoseOperationHeader rootSource) ||
                        rootSource.Code != CharacterPoseOperationCode.ClipPlayer ||
                        ((CharacterPosePlayerOperationPayload)
                            OperationPages.RequirePayload(rootSource))
                        .ClipPlayerIndex != descriptor.ClipPlayerIndex)
                    {
                        throw new InvalidOperationException(
                            $"Pose Plan Root Orientation Warp operation #{i} descriptor ownership is invalid.");
                    }
                }
                if (operation.Code == CharacterPoseOperationCode.BlendSpacePlayer &&
                    !((CharacterPosePlayerOperationPayload)
                        OperationPages.RequirePayload(operation)).SourceIndex.IsValid)
                {
                    throw new InvalidOperationException(
                        $"Pose Plan Blend Space Player operation #{i} has no Presentation Pose source index.");
                }
                if (operation.Code == CharacterPoseOperationCode.PoseStateMachine)
                {
                    CharacterPoseStateMachineOperationPayload machine =
                        (CharacterPoseStateMachineOperationPayload)
                        OperationPages.RequirePayload(operation);
                    if ((uint)machine.StateMachineIndex >= (uint)StateMachines.Count)
                        throw new InvalidOperationException($"Pose Plan StateMachine operation #{i} has no descriptor.");
                    CharacterPoseStateMachineDescriptor descriptor =
                        StateMachines[machine.StateMachineIndex];
                    descriptor?.RequireValid();
                    if (descriptor == null || descriptor.Index != machine.StateMachineIndex ||
                        descriptor.NodeId != operation.NodeId)
                    {
                        throw new InvalidOperationException(
                            $"Pose Plan StateMachine operation #{i} descriptor ownership is invalid.");
                    }
                    for (int stateIndex = 0; stateIndex < descriptor.States.Count; stateIndex++)
                    {
                        CharacterPoseStateDescriptor poseState = descriptor.States[stateIndex];
                        int stateEnd = checked(poseState.OperationStart + poseState.OperationCount);
                        if (poseState.OperationStart < 0 || stateEnd > i ||
                            !poseValueProducers.TryGetValue(
                                poseState.OutputPoseValueIndex,
                                out CharacterPoseOperationHeader stateOutput) ||
                            stateOutput.Code != CharacterPoseOperationCode.StatePoseOutput ||
                            stateOutput.Index < poseState.OperationStart || stateOutput.Index >= stateEnd)
                        {
                            throw new InvalidOperationException(
                                $"Pose Plan StateMachine '{descriptor.NodeId}' State #{stateIndex} output or operation span is invalid.");
                        }
                        for (int usageIndex = 0; usageIndex < poseState.SourceProviders.Count; usageIndex++)
                        {
                            PoseStateSourceProviderPlan usage = poseState.SourceProviders[usageIndex];
                            if (usage == null || usage.StateIndex != stateIndex ||
                                usage.OperationIndex < poseState.OperationStart ||
                                usage.OperationIndex >= stateEnd ||
                                RequirePlayerIndex(OperationHeaders[usage.OperationIndex]) != usage.PlayerIndex ||
                                OperationHeaders[usage.OperationIndex].NodeId != usage.PlayerNodeId)
                            {
                                throw new InvalidOperationException(
                                    $"Pose Plan StateMachine '{descriptor.NodeId}' State #{stateIndex} source usage #{usageIndex} is invalid.");
                            }
                        }
                    }
                }
                if (operation.Code == CharacterPoseOperationCode.AnimationSlot)
                {
                    CharacterPoseAnimationSlotOperationPayload slotPayload =
                        (CharacterPoseAnimationSlotOperationPayload)
                        OperationPages.RequirePayload(operation);
                    int controlOperationIndex = OperationPages.FindInputValueIndex(
                        operation,
                        CharacterPoseValueReferenceKind.OperationControl);
                    if ((uint)slotPayload.AnimationSlotIndex >= (uint)AnimationSlots.Count)
                        throw new InvalidOperationException($"Pose Plan Animation Slot operation #{i} has no descriptor.");
                    CharacterAnimationSlotDescriptor descriptor =
                        AnimationSlots[slotPayload.AnimationSlotIndex];
                    descriptor?.RequireValid();
                    if (descriptor == null || descriptor.Index != slotPayload.AnimationSlotIndex ||
                        descriptor.NodeId != operation.NodeId ||
                        descriptor.SourceUsage.SourcePoseValueIndex != inputPoseA ||
                        descriptor.ActionPlayer.ActionPlaybackOperationIndex != controlOperationIndex ||
                        descriptor.ActionPlayer.PlayerIndex != slotPayload.PlayerIndex ||
                        descriptor.BlendStackWorkspace.BlendNodeIndex != slotPayload.BlendNodeIndex)
                    {
                        throw new InvalidOperationException(
                            $"Pose Plan Animation Slot operation #{i} descriptor ownership is invalid.");
                    }
                    if ((uint)controlOperationIndex >= (uint)i ||
                        OperationHeaders[controlOperationIndex].Code != CharacterPoseOperationCode.ActionPlaybackInput ||
                        (uint)slotPayload.BlendNodeIndex >= (uint)BlendNodes.Count)
                    {
                        throw new InvalidOperationException(
                            $"Pose Plan Animation Slot '{descriptor.NodeId}' workspace indices are invalid.");
                    }
                    CharacterPoseOperationHeader actionInput =
                        OperationHeaders[controlOperationIndex];
                    CharacterPoseActionInputOperationPayload actionPayload =
                        (CharacterPoseActionInputOperationPayload)
                        OperationPages.RequirePayload(actionInput);
                    if (actionPayload.AnimationChannelId != descriptor.AnimationChannelId ||
                        actionPayload.SelectionAvailability != AnimationSelectionAvailabilityPolicy.AllowEmpty)
                    {
                        throw new InvalidOperationException(
                            $"Pose Plan Animation Slot '{descriptor.NodeId}' Action Playback binding is invalid.");
                    }
                    AnimationBlendNodePayload blendNode =
                        BlendNodes[slotPayload.BlendNodeIndex];
                    if (blendNode == null || blendNode.NodeId != descriptor.NodeId ||
                        blendNode.StackPolicy.MaxActiveSourceEntries != descriptor.BlendStackWorkspace.Capacity)
                    {
                        throw new InvalidOperationException(
                            $"Pose Plan Animation Slot '{descriptor.NodeId}' BlendStack workspace is invalid.");
                    }
                    if (blendNode.Transitions.Count != descriptor.RequestRoutes.Count)
                        throw new InvalidOperationException(
                            $"Pose Plan Animation Slot '{descriptor.NodeId}' exact route count is inconsistent.");
                    var endpoints = descriptor.Endpoints.ToDictionary(value => value.EndpointId);
                    for (int routeIndex = 0; routeIndex < descriptor.RequestRoutes.Count; routeIndex++)
                    {
                        CharacterAnimationSlotRequestRouteDescriptor route = descriptor.RequestRoutes[routeIndex];
                        CharacterAnimationSlotEndpointDescriptor sourceEndpoint = endpoints[route.SourceEndpointId];
                        CharacterAnimationSlotEndpointDescriptor targetEndpoint = endpoints[route.TargetEndpointId];
                        AnimationBlendTransitionPayload transition = blendNode.RequireTransition(
                            sourceEndpoint.ProgramProducerIndex,
                            sourceEndpoint.SourcePose
                                ? AnimationBlendTransitionEndpointKind.SourcePose
                                : AnimationBlendTransitionEndpointKind.SourceOwner,
                            targetEndpoint.ProgramProducerIndex,
                            targetEndpoint.SourcePose
                                ? AnimationBlendTransitionEndpointKind.SourcePose
                                : AnimationBlendTransitionEndpointKind.SourceOwner);
                        if (transition.BlendLogic != route.BlendLogic ||
                            transition.DurationSeconds != route.DurationSeconds ||
                            transition.CurveIndex != route.CurveIndex ||
                            transition.BlendProfileIndex != route.BlendProfileIndex)
                        {
                            throw new InvalidOperationException(
                                $"Pose Plan Animation Slot '{descriptor.NodeId}' exact route #{routeIndex} does not match its Blend Policy.");
                        }
                    }
                }
            }
            if (ClipPlayers.Count != OperationHeaders.Count(value => value.Code == CharacterPoseOperationCode.ClipPlayer) ||
                RootOrientationWarps.Count != OperationHeaders.Count(value => value.Code == CharacterPoseOperationCode.RootOrientationWarp) ||
                StateMachines.Count != OperationHeaders.Count(value => value.Code == CharacterPoseOperationCode.PoseStateMachine) ||
                AnimationSlots.Count != OperationHeaders.Count(value => value.Code == CharacterPoseOperationCode.AnimationSlot) ||
                PoseBoneIkGoalSources.Count != OperationHeaders.Count(value => value.Code == CharacterPoseOperationCode.PoseBoneIKGoals) ||
                FootPlacements.Count != OperationHeaders.Count(value => value.Code == CharacterPoseOperationCode.FootPlacement) ||
                FullBodyIks.Count != OperationHeaders.Count(value => value.Code == CharacterPoseOperationCode.FullBodyIK) ||
                FootPlacements.Count > 1 ||
                FullBodyIks.Count != 1 ||
                OperationHeaders.Count(value => value.Code == CharacterPoseOperationCode.FullBodyIkGoalAssembler) != 1 ||
                PoseBoneIkGoalSources.Count + FootPlacements.Count !=
                FullBodyIkGoalContributionWorkspaceCount ||
                FullBodyIkGoalSetWorkspaceCount != 1 ||
                PoseBoneIkGoalSources.Sum(value => value.GoalCount) +
                FootPlacements.Count * CharacterPresentationFootPlacementDescriptor.GoalCount !=
                FullBodyIkGoalContributionGoalWorkspaceCount ||
                OperationHeaders.Sum(value => OperationPages.CountInputValues(
                    value,
                    CharacterPoseValueReferenceKind.FullBodyIkGoalContribution)) !=
                FullBodyIkGoalContributionWorkspaceCount ||
                outputCount != 1 ||
                playerIndices.Count != PlayerCount ||
                playerIndices.Count > 0 && (playerIndices.Min() != 0 || playerIndices.Max() != playerIndices.Count - 1))
                throw new InvalidOperationException("Pose Plan Full Body IK ownership or workspace layout is invalid.");
            RequireStagesValid();
            for (int i = 0; i < PoseBoneIkGoalSources.Count; i++)
            {
                CharacterPresentationPoseBoneIkGoalsDescriptor descriptor = PoseBoneIkGoalSources[i];
                if (descriptor == null || descriptor.Index != i)
                    throw new InvalidOperationException($"Pose Bone IK Goals descriptor #{i} is invalid.");
                descriptor.RequireValid();
            }
            for (int i = 0; i < FootPlacements.Count; i++)
            {
                CharacterPresentationFootPlacementDescriptor descriptor = FootPlacements[i];
                if (descriptor == null || descriptor.Index != i)
                    throw new InvalidOperationException($"Foot Placement descriptor #{i} is invalid.");
                descriptor.RequireValid(RigId, RigRevision);
            }
            for (int i = 0; i < FullBodyIks.Count; i++)
            {
                CharacterPresentationFullBodyIkDescriptor descriptor = FullBodyIks[i];
                if (descriptor == null || descriptor.Index != i)
                    throw new InvalidOperationException($"Full Body IK descriptor #{i} is invalid.");
                descriptor.RequireValid();
            }
            RequireLinkedPoseValid();
        }

        public void RequireInertializationValid()
        {
            int operationCount = OperationHeaders.Count(value =>
                value.Code == CharacterPoseOperationCode.Inertialization);
            if (operationCount != Inertializations.Count)
                throw new InvalidOperationException("Pose Plan Inertialization operation and descriptor counts are inconsistent.");
            var descriptors = new HashSet<PoseNodeId>();
            for (int index = 0; index < Inertializations.Count; index++)
            {
                CharacterPresentationInertializationDescriptor descriptor = Inertializations[index];
                if (descriptor == null || descriptor.Index != index || !descriptors.Add(descriptor.NodeId) ||
                    !Enum.IsDefined(typeof(PoseInertializationTemporalOwnerKind), descriptor.TemporalOwnerKind) ||
                    !descriptor.InputOwnerNodeId.IsValid || descriptor.InputOwnerIndex < 0 ||
                    string.IsNullOrWhiteSpace(descriptor.PolicyId) || string.IsNullOrWhiteSpace(descriptor.PolicyRevision))
                    throw new InvalidOperationException($"Pose Plan Inertialization descriptor #{index} is invalid or duplicated.");
                CharacterPoseOperationHeader operation = OperationHeaders.SingleOrDefault(value =>
                    value.Code == CharacterPoseOperationCode.Inertialization &&
                    ((CharacterPoseIndexedOperationPayload)
                        OperationPages.RequirePayload(value)).ValueIndex == index);
                if (operation == null || operation.NodeId != descriptor.NodeId)
                    throw new InvalidOperationException($"Pose Plan Inertialization descriptor #{index} has no exact operation owner.");
                int inputPose = OperationPages.FindInputValueIndex(
                    operation,
                    CharacterPoseValueReferenceKind.Pose);
                CharacterPoseOperationHeader inputOwner = OperationHeaders.SingleOrDefault(value =>
                    value.Index < operation.Index &&
                    OperationPages.FindOutputValueIndex(
                        value,
                        CharacterPoseValueReferenceKind.Pose) == inputPose);
                if (inputOwner == null || inputOwner.NodeId != descriptor.InputOwnerNodeId)
                {
                    throw new InvalidOperationException(
                        $"Pose Plan Inertialization '{operation.NodeId}' has no exact direct input owner.");
                }
                HashSet<(int Source, int Target)> expectedPairs;
                if (descriptor.TemporalOwnerKind == PoseInertializationTemporalOwnerKind.StateMachineTransition)
                {
                    if (inputOwner.Code != CharacterPoseOperationCode.PoseStateMachine ||
                        ((CharacterPoseStateMachineOperationPayload)
                            OperationPages.RequirePayload(inputOwner))
                        .StateMachineIndex != descriptor.InputOwnerIndex ||
                        (uint)descriptor.InputOwnerIndex >= (uint)StateMachines.Count)
                    {
                        throw new InvalidOperationException(
                            $"Pose Plan Inertialization '{operation.NodeId}' declares a mismatched StateMachine temporal owner.");
                    }
                    expectedPairs = StateMachines[descriptor.InputOwnerIndex].Transitions
                        .Where(value => value.BlendLogic == AnimationTransitionBlendLogic.Inertialization)
                        .Select(value => (value.SourceStateIndex, value.TargetStateIndex))
                        .ToHashSet();
                }
                else
                {
                    if (!IsDirectInertializationPlayer(inputOwner.Code) ||
                        !((CharacterPosePlayerOperationPayload)
                            OperationPages.RequirePayload(inputOwner))
                        .SourceIndex.IsValid ||
                        ((CharacterPosePlayerOperationPayload)
                            OperationPages.RequirePayload(inputOwner))
                        .SourceIndex.Value != descriptor.InputOwnerIndex)
                    {
                        throw new InvalidOperationException(
                            $"Pose Plan Inertialization '{operation.NodeId}' declares a mismatched direct Player temporal owner.");
                    }
                    expectedPairs = new HashSet<(int Source, int Target)>
                    {
                        (descriptor.InputOwnerIndex, descriptor.InputOwnerIndex)
                    };
                }
                var pairs = new HashSet<(int Source, int Target)>();
                for (int ruleIndex = 0; ruleIndex < descriptor.Rules.Count; ruleIndex++)
                {
                    CharacterPresentationInertializationRuleDescriptor rule = descriptor.Rules[ruleIndex];
                    if (rule == null ||
                        !expectedPairs.Contains((rule.SourceEndpointIndex, rule.TargetEndpointIndex)) ||
                        !Enum.IsDefined(typeof(PoseInertializationMode), rule.Mode) ||
                        !float.IsFinite(rule.DurationSeconds) || rule.DurationSeconds < 0f ||
                        rule.Mode == PoseInertializationMode.Inertialize &&
                        (rule.DurationSeconds <= 0f || rule.CurveIndex < 0 || rule.ProfileIndex < 0) ||
                        rule.Mode == PoseInertializationMode.HardCut &&
                        (rule.DurationSeconds != 0f || rule.CurveIndex != -1 || rule.ProfileIndex != -1) ||
                        rule.ParameterModes.Count != Parameters.Count ||
                        !pairs.Add((rule.SourceEndpointIndex, rule.TargetEndpointIndex)))
                    {
                        throw new InvalidOperationException($"Pose Plan Inertialization '{operation.NodeId}' exact rule #{ruleIndex} is invalid or duplicated.");
                    }
                    for (int parameter = 0; parameter < rule.ParameterModes.Count; parameter++)
                    {
                        if (!Enum.IsDefined(typeof(PoseParameterInertializationMode), rule.ParameterModes[parameter]))
                            throw new InvalidOperationException($"Pose Plan Inertialization '{operation.NodeId}' rule #{ruleIndex} parameter filter #{parameter} is invalid.");
                    }
                }
                if (!pairs.SetEquals(expectedPairs))
                    throw new InvalidOperationException(
                        $"Pose Plan Inertialization '{operation.NodeId}' does not contain every exact temporal-owner transition.");
            }
        }

        static bool IsDirectInertializationPlayer(CharacterPoseOperationCode code) =>
            code == CharacterPoseOperationCode.SelectedPosePlayer ||
            code == CharacterPoseOperationCode.BlendSpacePlayer ||
            code == CharacterPoseOperationCode.ClipPlayer;

        void RequireStagesValid()
        {
            int expectedOperationStart = 0;
            int expectedNativeOperationStart = 0;
            int finalStageCount = 0;
            for (int stageIndex = 0; stageIndex < Stages.Count; stageIndex++)
            {
                CharacterPresentationPoseStage stage = Stages[stageIndex];
                if (stage == null || stage.Index != stageIndex ||
                    stage.OperationStart != expectedOperationStart || stage.OperationCount <= 0 ||
                    stage.NativeOperationStart != expectedNativeOperationStart || stage.NativeOperationCount < 0 ||
                    stage.CompletionIndex != stageIndex || stage.DiagnosticIndex != stageIndex ||
                    stage.PoseWorkspaceStart < 0 || stage.PoseWorkspaceCount < 0 ||
                    stage.OperationStart > OperationHeaders.Count - stage.OperationCount)
                {
                    throw new InvalidOperationException($"Pose Plan stage #{stageIndex} layout is invalid.");
                }

                int nativeCount = 0;
                int minPoseValue = int.MaxValue;
                int maxPoseValue = -1;
                for (int operationIndex = stage.OperationStart;
                     operationIndex < stage.OperationStart + stage.OperationCount;
                     operationIndex++)
                {
                    CharacterPoseOperationHeader operation =
                        OperationHeaders[operationIndex];
                    if (operation.ExecutionDomain != stage.ExecutionDomain ||
                        operation.OutputPoseSpace != CharacterPoseSpace.None &&
                        operation.OutputPoseSpace != stage.OutputPoseSpace)
                    {
                        throw new InvalidOperationException(
                            $"Pose Plan stage #{stageIndex} operation #{operationIndex} domain or Pose space is inconsistent.");
                    }
                    if (IsExecutionViewOperation(operation.Code))
                        nativeCount++;
                    int outputPose = OperationPages.FindOutputValueIndex(
                        operation,
                        CharacterPoseValueReferenceKind.Pose);
                    if (outputPose < 0)
                        continue;
                    minPoseValue = Math.Min(minPoseValue, outputPose);
                    maxPoseValue = Math.Max(maxPoseValue, outputPose);
                }

                int poseStart = maxPoseValue < 0 ? 0 : minPoseValue;
                int poseCount = maxPoseValue < 0 ? 0 : maxPoseValue - minPoseValue + 1;
                if (nativeCount != stage.NativeOperationCount ||
                    poseStart != stage.PoseWorkspaceStart || poseCount != stage.PoseWorkspaceCount)
                {
                    throw new InvalidOperationException($"Pose Plan stage #{stageIndex} workspace layout is inconsistent.");
                }
                if (stage.ExecutionDomain == CharacterPoseExecutionDomain.FinalPublication)
                    finalStageCount++;
                expectedOperationStart += stage.OperationCount;
                expectedNativeOperationStart += stage.NativeOperationCount;
            }

            if (expectedOperationStart != OperationHeaders.Count ||
                expectedNativeOperationStart != OperationHeaders.Count(value => IsExecutionViewOperation(value.Code)) ||
                finalStageCount != 1 ||
                Stages[Stages.Count - 1].ExecutionDomain != CharacterPoseExecutionDomain.FinalPublication)
            {
                throw new InvalidOperationException("Pose Plan ordered stage table does not close the operation topology.");
            }
        }

        internal static bool IsExecutionViewOperation(
            CharacterPoseOperationCode code) => code switch
        {
            CharacterPoseOperationCode.SelectedPosePlayer => true,
            CharacterPoseOperationCode.BlendSpacePlayer => true,
            CharacterPoseOperationCode.ClipPlayer => true,
            CharacterPoseOperationCode.BlendStack => true,
            CharacterPoseOperationCode.AnimationSlot => true,
            CharacterPoseOperationCode.Inertialization => true,
            CharacterPoseOperationCode.BlendPose => true,
            CharacterPoseOperationCode.LayeredBoneBlend => true,
            CharacterPoseOperationCode.AdditivePose => true,
            CharacterPoseOperationCode.PoseParameterResolve => true,
            CharacterPoseOperationCode.ModifyBone => true,
            CharacterPoseOperationCode.RootOrientationWarp => true,
            CharacterPoseOperationCode.FootPlacement => true,
            CharacterPoseOperationCode.PoseBoneIKGoals => true,
            CharacterPoseOperationCode.FullBodyIkGoalAssembler => true,
            CharacterPoseOperationCode.FullBodyIK => true,
            CharacterPoseOperationCode.LinkedPoseCall => true,
            CharacterPoseOperationCode.LocalToComponentPose => true,
            CharacterPoseOperationCode.ComponentToLocalPose => true,
            CharacterPoseOperationCode.StatePoseOutput => true,
            CharacterPoseOperationCode.PoseStateMachine => true,
            CharacterPoseOperationCode.OutputPose => true,
            _ => false
        };

        static void RequirePoseInputDependency(
            CharacterPoseOperationHeader operation,
            int inputValueIndex,
            IReadOnlyDictionary<int, CharacterPoseOperationHeader> producers)
        {
            if (inputValueIndex < 0)
                return;
            if (!producers.TryGetValue(inputValueIndex, out CharacterPoseOperationHeader producer))
                throw new InvalidOperationException($"Pose Plan operation '{operation.NodeId}' reads Pose value '{inputValueIndex}' before it is produced.");
            if (producer.Index >= operation.Index || producer.OutputPoseSpace == CharacterPoseSpace.None ||
                operation.InputPoseSpace == CharacterPoseSpace.None || producer.OutputPoseSpace != operation.InputPoseSpace)
            {
                throw new InvalidOperationException(
                    $"Pose Plan operation '{operation.NodeId}' cannot read {producer.OutputPoseSpace} Pose from '{producer.NodeId}' as {operation.InputPoseSpace} Pose.");
            }
        }

        void RequireFullBodyIkGoalTopology(
            CharacterPoseOperationHeader operation,
            IReadOnlyDictionary<int, CharacterPoseOperationHeader> contributionProducers,
            IReadOnlyDictionary<int, CharacterPoseOperationHeader> goalSetProducers)
        {
            if (operation.Code == CharacterPoseOperationCode.FullBodyIkGoalAssembler)
            {
                ushort occupiedSlots = 0;
                var inputValues = new HashSet<int>();
                int inputCount = OperationPages.CountInputValues(
                    operation,
                    CharacterPoseValueReferenceKind.FullBodyIkGoalContribution);
                for (int localIndex = 0;
                     localIndex < inputCount;
                     localIndex++)
                {
                    int valueIndex = OperationPages.FindInputValueIndex(
                        operation,
                        CharacterPoseValueReferenceKind.FullBodyIkGoalContribution,
                        localIndex);
                    if (!inputValues.Add(valueIndex) ||
                        !contributionProducers.TryGetValue(
                            valueIndex,
                            out CharacterPoseOperationHeader producer) ||
                        producer.Index >= operation.Index ||
                        OperationPages.FindOutputValueIndex(
                            producer,
                            CharacterPoseValueReferenceKind.FullBodyIkGoalContribution) != valueIndex)
                    {
                        throw new InvalidOperationException(
                            $"Pose Plan Goal Assembler '{operation.NodeId}' reads invalid Contribution '{valueIndex}'.");
                    }
                    ushort slots = RequireGoalSourceSlots(producer);
                    if ((occupiedSlots & slots) != 0)
                    {
                        throw new InvalidOperationException(
                            $"Pose Plan Goal Assembler '{operation.NodeId}' receives duplicate Effector Slots from '{producer.NodeId}'.");
                    }
                    occupiedSlots = (ushort)(occupiedSlots | slots);
                }
                return;
            }

            if (operation.Code != CharacterPoseOperationCode.FullBodyIK)
                return;
            int goalSetValue = OperationPages.FindInputValueIndex(
                operation,
                CharacterPoseValueReferenceKind.FullBodyIkGoalSet);
            if (!goalSetProducers.TryGetValue(
                    goalSetValue,
                    out CharacterPoseOperationHeader assembler) ||
                assembler.Index >= operation.Index ||
                assembler.Code != CharacterPoseOperationCode.FullBodyIkGoalAssembler ||
                OperationPages.FindOutputValueIndex(
                    assembler,
                    CharacterPoseValueReferenceKind.FullBodyIkGoalSet) != goalSetValue)
            {
                throw new InvalidOperationException(
                    $"Pose Plan Full Body IK '{operation.NodeId}' does not read the unique Goal Assembler output.");
            }
        }

        ushort RequireGoalSourceSlots(CharacterPoseOperationHeader producer)
        {
            if (producer.Code == CharacterPoseOperationCode.FootPlacement)
            {
                return SlotMask(
                    CharacterFullBodyIkEffectorSlot.PelvisPreSolveTranslation,
                    CharacterFullBodyIkEffectorSlot.LeftFoot,
                    CharacterFullBodyIkEffectorSlot.RightFoot);
            }
            if (producer.Code == CharacterPoseOperationCode.PoseBoneIKGoals &&
                (uint)((CharacterPoseGoalContributionOperationPayload)
                    OperationPages.RequirePayload(producer)).PoseBoneIkGoalsIndex <
                (uint)PoseBoneIkGoalSources.Count)
            {
                int sourceIndex = ((CharacterPoseGoalContributionOperationPayload)
                    OperationPages.RequirePayload(producer)).PoseBoneIkGoalsIndex;
                CharacterPresentationPoseBoneIkGoalsDescriptor descriptor =
                    PoseBoneIkGoalSources[sourceIndex];
                ushort slots = 0;
                for (int bindingIndex = 0;
                     bindingIndex < descriptor.Bindings.Count;
                     bindingIndex++)
                {
                    ushort slot = SlotMask(
                        descriptor.Bindings[bindingIndex].EffectorSlot);
                    if ((slots & slot) != 0)
                    {
                        throw new InvalidOperationException(
                            $"Pose Plan Goal producer '{producer.NodeId}' contains a duplicate Effector Slot.");
                    }
                    slots = (ushort)(slots | slot);
                }
                return slots;
            }
            throw new InvalidOperationException(
                $"Pose Plan Goal producer '{producer.NodeId}' is not a typed Contribution Source.");
        }

        int RequirePlayerIndex(CharacterPoseOperationHeader operation) =>
            operation.Family switch
            {
                CharacterPoseOperationFamily.Player =>
                    ((CharacterPosePlayerOperationPayload)
                        OperationPages.RequirePayload(operation)).PlayerIndex,
                CharacterPoseOperationFamily.Blend =>
                    ((CharacterPoseBlendOperationPayload)
                        OperationPages.RequirePayload(operation)).PlayerIndex,
                CharacterPoseOperationFamily.AnimationSlot =>
                    ((CharacterPoseAnimationSlotOperationPayload)
                        OperationPages.RequirePayload(operation)).PlayerIndex,
                _ => -1
            };

        static ushort SlotMask(params CharacterFullBodyIkEffectorSlot[] slots)
        {
            ushort mask = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                int bit = 1 << ((int)slots[i] - 1);
                mask = (ushort)(mask | bit);
            }
            return mask;
        }
    }
}
