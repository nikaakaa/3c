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
        AnimationSelectionInput = 1,
        MotionMatchingSelectionInput = 2,
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
        PoseSubgraph = 13,
        OutputPose = 14,
        GraphInput = 15,
        GraphOutput = 16,
        MarkerSync = 17,
        BlendSpacePlayer = 18
    }

    public enum CharacterPosePortKind : byte
    {
        AnimationSelection = 1,
        Pose = 2,
        Parameter = 3,
        PoseDiscontinuity = 4
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
        [SerializeField] CharacterPosePortKind m_Kind = CharacterPosePortKind.Pose;
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
        [SerializeReference] CharacterPoseGraphData m_Inline;
        [SerializeField] CharacterPresentationPoseGraphAsset m_Shared;

        public CharacterPoseGraphData Inline => m_Inline;
        public CharacterPresentationPoseGraphAsset Shared => m_Shared;
        public bool HasInline => m_Inline != null;
        public bool HasShared => m_Shared;
        public bool IsExclusive => HasInline != HasShared;

        public void CreateInline(CharacterPoseGraphData data)
        {
            m_Inline = data ?? throw new ArgumentNullException(nameof(data));
            m_Shared = null;
        }

        public void UseShared(CharacterPresentationPoseGraphAsset shared)
        {
            m_Shared = shared ? shared : throw new ArgumentNullException(nameof(shared));
            m_Inline = null;
        }

        public void Clear()
        {
            m_Inline = null;
            m_Shared = null;
        }
    }

    [Serializable]
    public sealed class CharacterPoseNodeDefinition
    {
        [SerializeField] string m_NodeId = string.Empty;
        [SerializeField] CharacterPoseNodeKind m_Kind;
        [SerializeField] string m_DisplayName = string.Empty;
        [SerializeField] Vector2 m_Position;
        [SerializeField] CharacterPosePortDefinition[] m_Ports = Array.Empty<CharacterPosePortDefinition>();
        [SerializeField] string m_AnimationChannelId = string.Empty;
        [SerializeField] string m_ProgramProducerId = string.Empty;
        [SerializeField] string m_ParameterId = string.Empty;
        [SerializeField] AnimationSelectionAvailabilityPolicy m_SelectionAvailability = AnimationSelectionAvailabilityPolicy.RequireSelection;
        [SerializeField] CharacterAnimationBlendSpaceInputRangePolicy m_BlendSpaceInputRangePolicy = CharacterAnimationBlendSpaceInputRangePolicy.Clamp;
        [SerializeField] CharacterAnimationBlendPolicy m_BlendPolicy;
        [SerializeField] CharacterPoseInertializationPolicy m_InertializationPolicy;
        [SerializeField] CharacterAnimationBoneMaskAsset m_BoneMask;
        [SerializeField, Range(0f, 1f)] float m_Weight = 1f;
        [SerializeField] CharacterPoseParameterPolicy[] m_ParameterPolicies = Array.Empty<CharacterPoseParameterPolicy>();
        [SerializeField] string m_AdditiveReferencePoseId = AnimationAdditiveReferencePoseIds.RigReference;
        [SerializeField] AdditiveReferenceSpace m_AdditiveReferenceSpace = AdditiveReferenceSpace.Local;
        [SerializeField] AdditiveScalePolicy m_AdditiveScalePolicy = AdditiveScalePolicy.Multiply;
        [SerializeField] string m_BoneId = string.Empty;
        [SerializeField] ModifyBoneReferenceSpace m_ModifyBoneReferenceSpace = ModifyBoneReferenceSpace.Local;
        [SerializeField] ModifyBoneOperationMask m_ModifyBoneOperations;
        [SerializeField] Vector3 m_ModifyPosition;
        [SerializeField] Vector3 m_ModifyRotationEuler;
        [SerializeField] Vector3 m_ModifyScale = Vector3.one;
        [SerializeField] CharacterFootPlacementProfile m_FootPlacementProfile;
        [SerializeField] CharacterFootPlacementRigCalibration m_FootPlacementCalibration;
        [SerializeField] CharacterPoseSubgraphReference m_Subgraph;

        public PoseNodeId NodeId => string.IsNullOrWhiteSpace(m_NodeId) ? default : new PoseNodeId(m_NodeId);
        public CharacterPoseNodeKind Kind => m_Kind;
        public string DisplayName => m_DisplayName ?? string.Empty;
        public Vector2 Position => m_Position;
        public IReadOnlyList<CharacterPosePortDefinition> Ports => m_Ports ?? Array.Empty<CharacterPosePortDefinition>();
        public AnimationChannelId AnimationChannelId => string.IsNullOrWhiteSpace(m_AnimationChannelId) ? default : new AnimationChannelId(m_AnimationChannelId);
        public string ProgramProducerId => m_ProgramProducerId ?? string.Empty;
        public PoseParameterId ParameterId => string.IsNullOrWhiteSpace(m_ParameterId) ? default : new PoseParameterId(m_ParameterId);
        public AnimationSelectionAvailabilityPolicy SelectionAvailability => m_SelectionAvailability;
        public CharacterAnimationBlendSpaceInputRangePolicy BlendSpaceInputRangePolicy => m_BlendSpaceInputRangePolicy;
        public CharacterAnimationBlendPolicy BlendPolicy => m_BlendPolicy;
        public CharacterPoseInertializationPolicy InertializationPolicy => m_InertializationPolicy;
        public CharacterAnimationBoneMaskAsset BoneMask => m_BoneMask;
        public float Weight => m_Weight;
        public IReadOnlyList<CharacterPoseParameterPolicy> ParameterPolicies => m_ParameterPolicies ?? Array.Empty<CharacterPoseParameterPolicy>();
        public string AdditiveReferencePoseId => m_AdditiveReferencePoseId ?? string.Empty;
        public AdditiveReferenceSpace AdditiveReferenceSpace => m_AdditiveReferenceSpace;
        public AdditiveScalePolicy AdditiveScalePolicy => m_AdditiveScalePolicy;
        public AnimationBoneId BoneId => string.IsNullOrWhiteSpace(m_BoneId) ? default : new AnimationBoneId(m_BoneId);
        public ModifyBoneReferenceSpace ModifyBoneReferenceSpace => m_ModifyBoneReferenceSpace;
        public ModifyBoneOperationMask ModifyBoneOperations => m_ModifyBoneOperations;
        public Vector3 ModifyPosition => m_ModifyPosition;
        public Quaternion ModifyRotation => Quaternion.Euler(m_ModifyRotationEuler);
        public Vector3 ModifyScale => m_ModifyScale;
        public CharacterFootPlacementProfile FootPlacementProfile => m_FootPlacementProfile;
        public CharacterFootPlacementRigCalibration FootPlacementCalibration => m_FootPlacementCalibration;
        public CharacterPoseSubgraphReference Subgraph => m_Subgraph;

        public CharacterPoseNodeDefinition() { }

        public CharacterPoseNodeDefinition(
            PoseNodeId nodeId,
            CharacterPoseNodeKind kind,
            string displayName,
            Vector2 position,
            CharacterPosePortDefinition[] ports,
            AnimationChannelId animationChannelId = default,
            string programProducerId = null,
            PoseParameterId parameterId = default,
            AnimationSelectionAvailabilityPolicy selectionAvailability = AnimationSelectionAvailabilityPolicy.RequireSelection,
            CharacterAnimationBlendSpaceInputRangePolicy blendSpaceInputRangePolicy = CharacterAnimationBlendSpaceInputRangePolicy.Clamp,
            CharacterAnimationBlendPolicy blendPolicy = null,
            CharacterPoseInertializationPolicy inertializationPolicy = null,
            CharacterAnimationBoneMaskAsset boneMask = null,
            float weight = 1f,
            CharacterPoseParameterPolicy[] parameterPolicies = null,
            string additiveReferencePoseId = AnimationAdditiveReferencePoseIds.RigReference,
            AdditiveReferenceSpace additiveReferenceSpace = AdditiveReferenceSpace.Local,
            AdditiveScalePolicy additiveScalePolicy = AdditiveScalePolicy.Multiply,
            AnimationBoneId boneId = default,
            ModifyBoneReferenceSpace modifyBoneReferenceSpace = ModifyBoneReferenceSpace.Local,
            ModifyBoneOperationMask modifyBoneOperations = ModifyBoneOperationMask.None,
            Vector3 modifyPosition = default,
            Vector3 modifyRotationEuler = default,
            Vector3? modifyScale = null,
            CharacterFootPlacementProfile footPlacementProfile = null,
            CharacterFootPlacementRigCalibration footPlacementCalibration = null,
            CharacterPoseSubgraphReference subgraph = null)
        {
            if (!nodeId.IsValid)
                throw new ArgumentException("Pose Node identity is invalid.", nameof(nodeId));
            if (!Enum.IsDefined(typeof(CharacterPoseNodeKind), kind))
                throw new ArgumentOutOfRangeException(nameof(kind));
            if (!float.IsFinite(weight) || weight < 0f || weight > 1f)
                throw new ArgumentOutOfRangeException(nameof(weight));
            if (!Enum.IsDefined(typeof(CharacterAnimationBlendSpaceInputRangePolicy), blendSpaceInputRangePolicy))
                throw new ArgumentOutOfRangeException(nameof(blendSpaceInputRangePolicy));
            m_NodeId = nodeId.Value;
            m_Kind = kind;
            m_DisplayName = displayName ?? string.Empty;
            m_Position = position;
            m_Ports = ports ?? Array.Empty<CharacterPosePortDefinition>();
            m_AnimationChannelId = animationChannelId.Value ?? string.Empty;
            m_ProgramProducerId = programProducerId ?? string.Empty;
            m_ParameterId = parameterId.Value ?? string.Empty;
            m_SelectionAvailability = selectionAvailability;
            m_BlendSpaceInputRangePolicy = blendSpaceInputRangePolicy;
            m_BlendPolicy = blendPolicy;
            m_InertializationPolicy = inertializationPolicy;
            m_BoneMask = boneMask;
            m_Weight = weight;
            m_ParameterPolicies = parameterPolicies ?? Array.Empty<CharacterPoseParameterPolicy>();
            m_AdditiveReferencePoseId = additiveReferencePoseId ?? string.Empty;
            m_AdditiveReferenceSpace = additiveReferenceSpace;
            m_AdditiveScalePolicy = additiveScalePolicy;
            m_BoneId = boneId.Value ?? string.Empty;
            m_ModifyBoneReferenceSpace = modifyBoneReferenceSpace;
            m_ModifyBoneOperations = modifyBoneOperations;
            m_ModifyPosition = modifyPosition;
            m_ModifyRotationEuler = modifyRotationEuler;
            m_ModifyScale = modifyScale ?? Vector3.one;
            m_FootPlacementProfile = footPlacementProfile;
            m_FootPlacementCalibration = footPlacementCalibration;
            m_Subgraph = subgraph;
        }

        public void RegenerateIdentity()
        {
            m_NodeId = Guid.NewGuid().ToString("N");
            CharacterPosePortDefinition[] source = m_Ports ?? Array.Empty<CharacterPosePortDefinition>();
            var remapped = new CharacterPosePortDefinition[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                CharacterPosePortDefinition port = source[i];
                remapped[i] = new CharacterPosePortDefinition(
                    new PosePortId(Guid.NewGuid().ToString("N")),
                    port?.Name,
                    port?.Kind ?? CharacterPosePortKind.Pose,
                    port?.Direction ?? CharacterPosePortDirection.Input,
                    port?.Required ?? true,
                    port?.InterfacePortId ?? default);
            }
            m_Ports = remapped;
        }
    }

    [Serializable]
    public sealed class CharacterPoseGraphData
    {
        [SerializeField] string m_GraphId = string.Empty;
        [SerializeField] string m_ContentRevision = string.Empty;
        [SerializeField] CharacterPoseParameterDeclaration[] m_Parameters = Array.Empty<CharacterPoseParameterDeclaration>();
        [SerializeField] CharacterPoseNodeDefinition[] m_Nodes = Array.Empty<CharacterPoseNodeDefinition>();
        [SerializeField] CharacterPoseEdge[] m_Edges = Array.Empty<CharacterPoseEdge>();

        public string GraphId => m_GraphId ?? string.Empty;
        public string ContentRevision => m_ContentRevision ?? string.Empty;
        public IReadOnlyList<CharacterPoseParameterDeclaration> Parameters => m_Parameters ?? Array.Empty<CharacterPoseParameterDeclaration>();
        public IReadOnlyList<CharacterPoseNodeDefinition> Nodes => m_Nodes ?? Array.Empty<CharacterPoseNodeDefinition>();
        public IReadOnlyList<CharacterPoseEdge> Edges => m_Edges ?? Array.Empty<CharacterPoseEdge>();

        public CharacterPoseGraphData()
        {
            RegenerateGraphIdentity();
        }

        public void Configure(
            CharacterPoseParameterDeclaration[] parameters,
            CharacterPoseNodeDefinition[] nodes,
            CharacterPoseEdge[] edges)
        {
            m_Parameters = parameters ?? Array.Empty<CharacterPoseParameterDeclaration>();
            m_Nodes = nodes ?? Array.Empty<CharacterPoseNodeDefinition>();
            m_Edges = edges ?? Array.Empty<CharacterPoseEdge>();
            Touch();
        }

        public void RegenerateGraphIdentity()
        {
            m_GraphId = Guid.NewGuid().ToString("N");
            Touch();
        }

        public void Touch() => m_ContentRevision = Guid.NewGuid().ToString("N");
    }

}
