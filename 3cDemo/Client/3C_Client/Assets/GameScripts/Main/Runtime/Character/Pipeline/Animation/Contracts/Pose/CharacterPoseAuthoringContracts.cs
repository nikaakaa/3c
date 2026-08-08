using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    static class PoseIdentity
    {
        internal static string Require(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Pose identity is missing.", parameterName);
            string normalized = value.Trim();
            for (int i = 0; i < normalized.Length; i++)
            {
                char c = normalized[i];
                if (!(char.IsLetterOrDigit(c) || c == '.' || c == '_' || c == '-' || c == '/'))
                    throw new ArgumentException($"Pose identity '{normalized}' contains an unsupported character.", parameterName);
            }
            return normalized;
        }
    }

    public readonly struct PoseNodeId : IEquatable<PoseNodeId>, IComparable<PoseNodeId>
    {
        public PoseNodeId(string value) { Value = PoseIdentity.Require(value, nameof(value)); }
        public string Value { get; }
        public bool IsValid => !string.IsNullOrEmpty(Value);
        public int CompareTo(PoseNodeId other) => string.CompareOrdinal(Value, other.Value);
        public bool Equals(PoseNodeId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is PoseNodeId other && Equals(other);
        public override int GetHashCode() => Value?.GetHashCode() ?? 0;
        public override string ToString() => Value ?? string.Empty;
        public static bool operator ==(PoseNodeId left, PoseNodeId right) => left.Equals(right);
        public static bool operator !=(PoseNodeId left, PoseNodeId right) => !left.Equals(right);
        internal static string Require(string value, string parameterName) => PoseIdentity.Require(value, parameterName);
    }

    public readonly struct PoseGraphId : IEquatable<PoseGraphId>, IComparable<PoseGraphId>
    {
        public PoseGraphId(string value) { Value = PoseIdentity.Require(value, nameof(value)); }
        public string Value { get; }
        public bool IsValid => !string.IsNullOrEmpty(Value);
        public int CompareTo(PoseGraphId other) => string.CompareOrdinal(Value, other.Value);
        public bool Equals(PoseGraphId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is PoseGraphId other && Equals(other);
        public override int GetHashCode() => Value?.GetHashCode() ?? 0;
        public override string ToString() => Value ?? string.Empty;
        public static bool operator ==(PoseGraphId left, PoseGraphId right) => left.Equals(right);
        public static bool operator !=(PoseGraphId left, PoseGraphId right) => !left.Equals(right);
    }

    public readonly struct AnimationSlotId : IEquatable<AnimationSlotId>, IComparable<AnimationSlotId>
    {
        public AnimationSlotId(string value) { Value = PoseIdentity.Require(value, nameof(value)); }
        public string Value { get; }
        public bool IsValid => !string.IsNullOrEmpty(Value);
        public int CompareTo(AnimationSlotId other) => string.CompareOrdinal(Value, other.Value);
        public bool Equals(AnimationSlotId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is AnimationSlotId other && Equals(other);
        public override int GetHashCode() => Value?.GetHashCode() ?? 0;
        public override string ToString() => Value ?? string.Empty;
        public static bool operator ==(AnimationSlotId left, AnimationSlotId right) => left.Equals(right);
        public static bool operator !=(AnimationSlotId left, AnimationSlotId right) => !left.Equals(right);
    }

    public readonly struct PosePortId : IEquatable<PosePortId>, IComparable<PosePortId>
    {
        public PosePortId(string value) { Value = PoseIdentity.Require(value, nameof(value)); }
        public string Value { get; }
        public bool IsValid => !string.IsNullOrEmpty(Value);
        public int CompareTo(PosePortId other) => string.CompareOrdinal(Value, other.Value);
        public bool Equals(PosePortId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is PosePortId other && Equals(other);
        public override int GetHashCode() => Value?.GetHashCode() ?? 0;
        public override string ToString() => Value ?? string.Empty;
    }

    public readonly struct PoseInterfacePortId : IEquatable<PoseInterfacePortId>, IComparable<PoseInterfacePortId>
    {
        public PoseInterfacePortId(string value) { Value = PoseIdentity.Require(value, nameof(value)); }
        public string Value { get; }
        public bool IsValid => !string.IsNullOrEmpty(Value);
        public int CompareTo(PoseInterfacePortId other) => string.CompareOrdinal(Value, other.Value);
        public bool Equals(PoseInterfacePortId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is PoseInterfacePortId other && Equals(other);
        public override int GetHashCode() => Value?.GetHashCode() ?? 0;
        public override string ToString() => Value ?? string.Empty;
        public static bool operator ==(PoseInterfacePortId left, PoseInterfacePortId right) => left.Equals(right);
        public static bool operator !=(PoseInterfacePortId left, PoseInterfacePortId right) => !left.Equals(right);
    }

    public readonly struct PoseParameterId : IEquatable<PoseParameterId>, IComparable<PoseParameterId>
    {
        public PoseParameterId(string value) { Value = PoseIdentity.Require(value, nameof(value)); }
        public string Value { get; }
        public bool IsValid => !string.IsNullOrEmpty(Value);
        public int CompareTo(PoseParameterId other) => string.CompareOrdinal(Value, other.Value);
        public bool Equals(PoseParameterId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is PoseParameterId other && Equals(other);
        public override int GetHashCode() => Value?.GetHashCode() ?? 0;
        public override string ToString() => Value ?? string.Empty;
    }

    public static class AnimationPoseParameterIds
    {
        public static readonly PoseParameterId ActionWeight =
            new PoseParameterId("animation.action-weight");

        public static readonly PoseParameterId FootPlacementWeight =
            new PoseParameterId("animation.foot-placement-weight");

        public static readonly PoseParameterId MotorPlanarSpeed =
            new PoseParameterId("character.motor.planar-speed");

        public static readonly PoseParameterId MotorLocalVelocityX =
            new PoseParameterId("character.motor.local-velocity-x");

        public static readonly PoseParameterId MotorLocalVelocityY =
            new PoseParameterId("character.motor.local-velocity-y");
    }

    public static class AnimationAdditiveReferencePoseIds
    {
        public const string RigReference = "animation.rig-reference";
    }

    public readonly struct AnimationBoneId : IEquatable<AnimationBoneId>, IComparable<AnimationBoneId>
    {
        public AnimationBoneId(string value) { Value = PoseIdentity.Require(value, nameof(value)); }
        public string Value { get; }
        public bool IsValid => !string.IsNullOrEmpty(Value);
        public int CompareTo(AnimationBoneId other) => string.CompareOrdinal(Value, other.Value);
        public bool Equals(AnimationBoneId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is AnimationBoneId other && Equals(other);
        public override int GetHashCode() => Value?.GetHashCode() ?? 0;
        public override string ToString() => Value ?? string.Empty;
    }

    public enum AnimationSelectionAvailabilityPolicy : byte
    {
        RequireSelection = 1,
        AllowEmpty = 2
    }

    public enum CharacterAnimationBlendSpaceInputRangePolicy : byte
    {
        Clamp = 1,
        Reject = 2
    }

    public enum CharacterPoseNodeKind : byte
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
        PoseSubgraph = 13,
        OutputPose = 14,
        GraphInput = 15,
        GraphOutput = 16,
        BlendSpacePlayer = 18,
        SequencePlayer = 20,
        PoseStateMachine = 21,
        AnimationSlot = 22,
        ActionPlaybackInput = 23,
        RootOrientationWarp = 24,
        LocalToComponentPose = 25,
        ComponentToLocalPose = 26,
        FootGrounding = 28,
        PoseBoneIKGoals = 29,
        FullBodyIK = 30,
        LinkedPoseCall = 31,
        EmptyFullBodyIkGoals = 32,
        MotionMatchingPose = 33,
        PoseHistoryCollector = 34,
        EntryPoseInput = 35,
        PredictiveFootPlacementModifier = 36
    }

    public enum CharacterPosePortKind : byte
    {
        Parameter = 3,
        PoseDiscontinuity = 4,
        ActionPlayback = 5,
        LocalPose = 6,
        ComponentPose = 7,
        FullBodyIkGoals = 9,
        PoseHistory = 10,
        Trajectory = 11,
        PresentationFacts = 12,
        MotionMatchingBinding = 13
    }

    public enum CharacterPoseSpace : byte
    {
        None = 0,
        Local = 1,
        Component = 2
    }

    public enum CharacterPoseExecutionDomain : byte
    {
        FactAndDemand = 1,
        SourceCapture = 2,
        PureValue = 3,
        WorldAwareValue = 4,
        PurePose = 5,
        FinalPublication = 6
    }

    public enum CharacterPosePortDirection : byte
    {
        Input = 1,
        Output = 2
    }

    public enum PoseParameterResolvePolicy : byte
    {
        Base = 1,
        Overlay = 2,
        Weighted = 3,
        Max = 4,
        Min = 5
    }

    public enum AdditiveReferenceSpace : byte
    {
        Local = 1,
        Mesh = 2
    }

    public enum AdditiveScalePolicy : byte
    {
        Multiply = 1,
        AddDelta = 2,
        Ignore = 3
    }

    public enum PoseParameterValueType : byte
    {
        Float = 1,
        Int = 2,
        Bool = 3
    }

    public enum ModifyBoneReferenceSpace : byte
    {
        Local = 1,
        Mesh = 2
    }

    [Flags]
    public enum ModifyBoneOperationMask : byte
    {
        None = 0,
        Position = 1,
        Rotation = 2,
        Scale = 4
    }

    [Serializable]
    public sealed class CharacterPoseParameterDeclaration
    {
        [SerializeField] string m_ParameterId = string.Empty;
        [SerializeField] PoseParameterValueType m_ValueType = PoseParameterValueType.Float;
        [SerializeField] string m_Unit = string.Empty;
        [SerializeField] float m_DefaultValue;

        public PoseParameterId ParameterId => string.IsNullOrWhiteSpace(m_ParameterId) ? default : new PoseParameterId(m_ParameterId);
        public PoseParameterValueType ValueType => m_ValueType;
        public string Unit => m_Unit ?? string.Empty;
        public float DefaultValue => m_DefaultValue;

        public CharacterPoseParameterDeclaration() { }

        public CharacterPoseParameterDeclaration(
            PoseParameterId parameterId,
            PoseParameterValueType valueType,
            float defaultValue,
            string unit = "")
        {
            if (!parameterId.IsValid)
                throw new ArgumentException("Pose Parameter identity is invalid.", nameof(parameterId));
            if (!Enum.IsDefined(typeof(PoseParameterValueType), valueType))
                throw new ArgumentOutOfRangeException(nameof(valueType));
            if (!float.IsFinite(defaultValue))
                throw new ArgumentOutOfRangeException(nameof(defaultValue));
            m_ParameterId = parameterId.Value;
            m_ValueType = valueType;
            m_Unit = unit?.Trim() ?? string.Empty;
            m_DefaultValue = defaultValue;
        }
    }

    [Serializable]
    public sealed class CharacterPoseParameterPolicy
    {
        [SerializeField] string m_ParameterId = string.Empty;
        [SerializeField] PoseParameterResolvePolicy m_Policy = PoseParameterResolvePolicy.Weighted;

        public PoseParameterId ParameterId => string.IsNullOrWhiteSpace(m_ParameterId) ? default : new PoseParameterId(m_ParameterId);
        public PoseParameterResolvePolicy Policy => m_Policy;

        public CharacterPoseParameterPolicy() { }

        public CharacterPoseParameterPolicy(PoseParameterId parameterId, PoseParameterResolvePolicy policy)
        {
            if (!parameterId.IsValid)
                throw new ArgumentException("Pose Parameter identity is invalid.", nameof(parameterId));
            if (!Enum.IsDefined(typeof(PoseParameterResolvePolicy), policy))
                throw new ArgumentOutOfRangeException(nameof(policy));
            m_ParameterId = parameterId.Value;
            m_Policy = policy;
        }
    }

    [Serializable]
    public sealed class CharacterPosePortDefinition
    {
        [SerializeField] string m_PortId = string.Empty;
        [SerializeField] string m_InterfacePortId = string.Empty;
        [SerializeField] string m_Name = string.Empty;
        [SerializeField] CharacterPosePortKind m_Kind = CharacterPosePortKind.LocalPose;
        [SerializeField] CharacterPosePortDirection m_Direction = CharacterPosePortDirection.Input;
        [SerializeField] bool m_Required = true;

        public PosePortId PortId => string.IsNullOrWhiteSpace(m_PortId) ? default : new PosePortId(m_PortId);
        public PoseInterfacePortId InterfacePortId => string.IsNullOrWhiteSpace(m_InterfacePortId) ? default : new PoseInterfacePortId(m_InterfacePortId);
        public string Name => m_Name ?? string.Empty;
        public CharacterPosePortKind Kind => m_Kind;
        public CharacterPosePortDirection Direction => m_Direction;
        public bool Required => m_Required;

        public CharacterPosePortDefinition() { }

        public CharacterPosePortDefinition(
            PosePortId portId,
            string name,
            CharacterPosePortKind kind,
            CharacterPosePortDirection direction,
            bool required,
            PoseInterfacePortId interfacePortId = default)
        {
            if (!portId.IsValid)
                throw new ArgumentException("Pose Port identity is invalid.", nameof(portId));
            if (!Enum.IsDefined(typeof(CharacterPosePortKind), kind))
                throw new ArgumentOutOfRangeException(nameof(kind));
            if (!Enum.IsDefined(typeof(CharacterPosePortDirection), direction))
                throw new ArgumentOutOfRangeException(nameof(direction));
            m_PortId = portId.Value;
            m_InterfacePortId = interfacePortId.Value ?? string.Empty;
            m_Name = name ?? string.Empty;
            m_Kind = kind;
            m_Direction = direction;
            m_Required = required;
        }
    }

    [Serializable]
    public sealed class CharacterPoseEdge
    {
        [SerializeField] string m_EdgeId = string.Empty;
        [SerializeField] string m_SourceNodeId = string.Empty;
        [SerializeField] string m_SourcePortId = string.Empty;
        [SerializeField] string m_TargetNodeId = string.Empty;
        [SerializeField] string m_TargetPortId = string.Empty;

        public string EdgeId => m_EdgeId ?? string.Empty;
        public PoseNodeId SourceNodeId => string.IsNullOrWhiteSpace(m_SourceNodeId) ? default : new PoseNodeId(m_SourceNodeId);
        public PosePortId SourcePortId => string.IsNullOrWhiteSpace(m_SourcePortId) ? default : new PosePortId(m_SourcePortId);
        public PoseNodeId TargetNodeId => string.IsNullOrWhiteSpace(m_TargetNodeId) ? default : new PoseNodeId(m_TargetNodeId);
        public PosePortId TargetPortId => string.IsNullOrWhiteSpace(m_TargetPortId) ? default : new PosePortId(m_TargetPortId);

        public CharacterPoseEdge() { }

        public CharacterPoseEdge(string edgeId, PoseNodeId sourceNodeId, PosePortId sourcePortId, PoseNodeId targetNodeId, PosePortId targetPortId)
        {
            m_EdgeId = PoseIdentity.Require(edgeId, nameof(edgeId));
            m_SourceNodeId = sourceNodeId.IsValid ? sourceNodeId.Value : throw new ArgumentException("Source node is invalid.", nameof(sourceNodeId));
            m_SourcePortId = sourcePortId.IsValid ? sourcePortId.Value : throw new ArgumentException("Source port is invalid.", nameof(sourcePortId));
            m_TargetNodeId = targetNodeId.IsValid ? targetNodeId.Value : throw new ArgumentException("Target node is invalid.", nameof(targetNodeId));
            m_TargetPortId = targetPortId.IsValid ? targetPortId.Value : throw new ArgumentException("Target port is invalid.", nameof(targetPortId));
        }
    }

    [Serializable]
    public sealed class CharacterPoseSubgraphReference
    {
        [SerializeField] string m_PoseGraphId = string.Empty;

        public PoseGraphId PoseGraphId => string.IsNullOrWhiteSpace(m_PoseGraphId)
            ? default
            : new PoseGraphId(m_PoseGraphId);

        public void Assign(PoseGraphId poseGraphId)
        {
            if (!poseGraphId.IsValid)
                throw new ArgumentException("Pose Subgraph identity is invalid.", nameof(poseGraphId));
            m_PoseGraphId = poseGraphId.Value;
        }

        public void Clear() => m_PoseGraphId = string.Empty;
    }

}
