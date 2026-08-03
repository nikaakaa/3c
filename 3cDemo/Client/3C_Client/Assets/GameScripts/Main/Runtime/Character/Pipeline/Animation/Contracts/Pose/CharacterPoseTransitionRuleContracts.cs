using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public readonly struct PoseTransitionRuleGraphId : IEquatable<PoseTransitionRuleGraphId>, IComparable<PoseTransitionRuleGraphId>
    {
        public PoseTransitionRuleGraphId(string value) { Value = PoseIdentity.Require(value, nameof(value)); }
        public string Value { get; }
        public bool IsValid => !string.IsNullOrEmpty(Value);
        public int CompareTo(PoseTransitionRuleGraphId other) => string.CompareOrdinal(Value, other.Value);
        public bool Equals(PoseTransitionRuleGraphId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is PoseTransitionRuleGraphId other && Equals(other);
        public override int GetHashCode() => Value?.GetHashCode() ?? 0;
        public override string ToString() => Value ?? string.Empty;
        public static bool operator ==(PoseTransitionRuleGraphId left, PoseTransitionRuleGraphId right) => left.Equals(right);
        public static bool operator !=(PoseTransitionRuleGraphId left, PoseTransitionRuleGraphId right) => !left.Equals(right);
    }

    public readonly struct PoseTransitionRuleOperationId : IEquatable<PoseTransitionRuleOperationId>, IComparable<PoseTransitionRuleOperationId>
    {
        public PoseTransitionRuleOperationId(string value) { Value = PoseIdentity.Require(value, nameof(value)); }
        public string Value { get; }
        public bool IsValid => !string.IsNullOrEmpty(Value);
        public int CompareTo(PoseTransitionRuleOperationId other) => string.CompareOrdinal(Value, other.Value);
        public bool Equals(PoseTransitionRuleOperationId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is PoseTransitionRuleOperationId other && Equals(other);
        public override int GetHashCode() => Value?.GetHashCode() ?? 0;
        public override string ToString() => Value ?? string.Empty;
        public static bool operator ==(PoseTransitionRuleOperationId left, PoseTransitionRuleOperationId right) => left.Equals(right);
        public static bool operator !=(PoseTransitionRuleOperationId left, PoseTransitionRuleOperationId right) => !left.Equals(right);
    }

    public enum PoseTransitionRuleValueKind : byte
    {
        Bool = 1,
        Float = 2,
        Enum = 3,
        Identity = 4
    }

    public enum PoseTransitionRuleOperationKind : byte
    {
        FactInput = 1,
        BoolLiteral = 2,
        FloatLiteral = 3,
        EnumLiteral = 4,
        Not = 5,
        And = 6,
        Or = 7,
        Equal = 8,
        NotEqual = 9,
        Greater = 10,
        GreaterOrEqual = 11,
        Less = 12,
        LessOrEqual = 13,
        TimeInState = 14,
        StatePoseRemainingTime = 15,
        IdentityLiteral = 16
    }

    public static class PoseTransitionRuleEnumTypes
    {
        public const string CharacterPresentationMotionPhase = "presentation.motion-phase";

        internal static bool IsDefined(string enumTypeId, int value) =>
            string.Equals(enumTypeId, CharacterPresentationMotionPhase, StringComparison.Ordinal) &&
            value >= (int)global::ThirdPersonCharacter.Pipeline.Animation.CharacterPresentationMotionPhase.GroundedStationary &&
            value <= (int)global::ThirdPersonCharacter.Pipeline.Animation.CharacterPresentationMotionPhase.AirborneFalling;
    }

    [Serializable]
    public sealed class CharacterPoseTransitionRuleOperation
    {
        [SerializeField] string m_OperationId = string.Empty;
        [SerializeField] PoseTransitionRuleOperationKind m_Kind;
        [SerializeField] string m_InputA = string.Empty;
        [SerializeField] string m_InputB = string.Empty;
        [SerializeField] string m_FactId = string.Empty;
        [SerializeField] bool m_BoolLiteral;
        [SerializeField] float m_FloatLiteral;
        [SerializeField] string m_EnumTypeId = string.Empty;
        [SerializeField] int m_EnumLiteral;
        [SerializeField] string m_IdentityLiteral = string.Empty;

        public PoseTransitionRuleOperationId OperationId => string.IsNullOrWhiteSpace(m_OperationId)
            ? default
            : new PoseTransitionRuleOperationId(m_OperationId);
        public PoseTransitionRuleOperationKind Kind => m_Kind;
        public PoseTransitionRuleOperationId InputA => string.IsNullOrWhiteSpace(m_InputA)
            ? default
            : new PoseTransitionRuleOperationId(m_InputA);
        public PoseTransitionRuleOperationId InputB => string.IsNullOrWhiteSpace(m_InputB)
            ? default
            : new PoseTransitionRuleOperationId(m_InputB);
        public PresentationFactId FactId => string.IsNullOrWhiteSpace(m_FactId)
            ? default
            : new PresentationFactId(m_FactId);
        public bool BoolLiteral => m_BoolLiteral;
        public float FloatLiteral => m_FloatLiteral;
        public string EnumTypeId => m_EnumTypeId ?? string.Empty;
        public int EnumLiteral => m_EnumLiteral;
        public string IdentityLiteral => m_IdentityLiteral ?? string.Empty;

        public CharacterPoseTransitionRuleOperation() { }

        public CharacterPoseTransitionRuleOperation(
            PoseTransitionRuleOperationId operationId,
            PoseTransitionRuleOperationKind kind,
            PoseTransitionRuleOperationId inputA = default,
            PoseTransitionRuleOperationId inputB = default,
            PresentationFactId factId = default,
            bool boolLiteral = false,
            float floatLiteral = 0f,
            string enumTypeId = null,
            int enumLiteral = 0,
            string identityLiteral = null)
        {
            if (!operationId.IsValid)
                throw new ArgumentException("Pose Transition Rule operation identity is invalid.", nameof(operationId));
            if (!Enum.IsDefined(typeof(PoseTransitionRuleOperationKind), kind))
                throw new ArgumentOutOfRangeException(nameof(kind));
            if (!float.IsFinite(floatLiteral))
                throw new ArgumentOutOfRangeException(nameof(floatLiteral));
            m_OperationId = operationId.Value;
            m_Kind = kind;
            m_InputA = inputA.Value ?? string.Empty;
            m_InputB = inputB.Value ?? string.Empty;
            m_FactId = factId.Value ?? string.Empty;
            m_BoolLiteral = boolLiteral;
            m_FloatLiteral = floatLiteral;
            m_EnumTypeId = enumTypeId ?? string.Empty;
            m_EnumLiteral = enumLiteral;
            m_IdentityLiteral = identityLiteral ?? string.Empty;
        }
    }

    [Serializable]
    public sealed class CharacterPoseTransitionRuleGraph
    {
        [SerializeField] string m_GraphId = string.Empty;
        [SerializeField] string m_ContentRevision = string.Empty;
        [SerializeField] CharacterPoseTransitionRuleOperation[] m_Operations =
            Array.Empty<CharacterPoseTransitionRuleOperation>();
        [SerializeField] string m_OutputOperationId = string.Empty;

        public PoseTransitionRuleGraphId GraphId => string.IsNullOrWhiteSpace(m_GraphId)
            ? default
            : new PoseTransitionRuleGraphId(m_GraphId);
        public string ContentRevision => m_ContentRevision ?? string.Empty;
        public IReadOnlyList<CharacterPoseTransitionRuleOperation> Operations =>
            m_Operations ?? Array.Empty<CharacterPoseTransitionRuleOperation>();
        public PoseTransitionRuleOperationId OutputOperationId => string.IsNullOrWhiteSpace(m_OutputOperationId)
            ? default
            : new PoseTransitionRuleOperationId(m_OutputOperationId);

        public CharacterPoseTransitionRuleGraph()
        {
            RegenerateGraphIdentity();
        }

        public CharacterPoseTransitionRuleGraph(
            PoseTransitionRuleGraphId graphId,
            string contentRevision,
            CharacterPoseTransitionRuleOperation[] operations,
            PoseTransitionRuleOperationId outputOperationId)
        {
            m_GraphId = graphId.IsValid
                ? graphId.Value
                : throw new ArgumentException(
                    "Pose Transition Rule graph identity is invalid.",
                    nameof(graphId));
            m_ContentRevision = PoseIdentity.Require(
                contentRevision,
                nameof(contentRevision));
            m_Operations =
                operations ?? Array.Empty<CharacterPoseTransitionRuleOperation>();
            m_OutputOperationId = outputOperationId.IsValid
                ? outputOperationId.Value
                : throw new ArgumentException(
                    "Pose Transition Rule output identity is invalid.",
                    nameof(outputOperationId));
        }

        public void Configure(
            CharacterPoseTransitionRuleOperation[] operations,
            PoseTransitionRuleOperationId outputOperationId)
        {
            m_Operations = operations ?? Array.Empty<CharacterPoseTransitionRuleOperation>();
            m_OutputOperationId = outputOperationId.Value ?? string.Empty;
            Touch();
        }

        public void RegenerateGraphIdentity()
        {
            m_GraphId = Guid.NewGuid().ToString("N");
            Touch();
        }

        public void Touch() => m_ContentRevision = Guid.NewGuid().ToString("N");
    }

    public enum PoseTransitionRuleOperationCode : byte
    {
        ReadFact = 1,
        BoolLiteral = 2,
        FloatLiteral = 3,
        EnumLiteral = 4,
        Not = 5,
        And = 6,
        Or = 7,
        Equal = 8,
        NotEqual = 9,
        Greater = 10,
        GreaterOrEqual = 11,
        Less = 12,
        LessOrEqual = 13,
        TimeInState = 14,
        StatePoseRemainingTime = 15,
        IdentityLiteral = 16
    }

    [Serializable]
    public sealed class CharacterPoseTransitionRuleCompiledOperation
    {
        [SerializeField] PoseTransitionRuleOperationCode m_Code;
        [SerializeField] PoseTransitionRuleValueKind m_ValueKind;
        [SerializeField] int m_InputA = -1;
        [SerializeField] int m_InputB = -1;
        [SerializeField] string m_FactId = string.Empty;
        [SerializeField] bool m_BoolLiteral;
        [SerializeField] float m_FloatLiteral;
        [SerializeField] string m_EnumTypeId = string.Empty;
        [SerializeField] int m_EnumLiteral;
        [SerializeField] string m_IdentityLiteral = string.Empty;

        public PoseTransitionRuleOperationCode Code => m_Code;
        public PoseTransitionRuleValueKind ValueKind => m_ValueKind;
        public int InputA => m_InputA;
        public int InputB => m_InputB;
        public PresentationFactId FactId => string.IsNullOrWhiteSpace(m_FactId)
            ? default
            : new PresentationFactId(m_FactId);
        public bool BoolLiteral => m_BoolLiteral;
        public float FloatLiteral => m_FloatLiteral;
        public string EnumTypeId => m_EnumTypeId ?? string.Empty;
        public int EnumLiteral => m_EnumLiteral;
        public string IdentityLiteral => m_IdentityLiteral ?? string.Empty;

        internal CharacterPoseTransitionRuleCompiledOperation(
            PoseTransitionRuleOperationCode code,
            PoseTransitionRuleValueKind valueKind,
            int inputA,
            int inputB,
            PresentationFactId factId,
            bool boolLiteral,
            float floatLiteral,
            string enumTypeId,
            int enumLiteral,
            string identityLiteral)
        {
            m_Code = code;
            m_ValueKind = valueKind;
            m_InputA = inputA;
            m_InputB = inputB;
            m_FactId = factId.Value ?? string.Empty;
            m_BoolLiteral = boolLiteral;
            m_FloatLiteral = floatLiteral;
            m_EnumTypeId = enumTypeId ?? string.Empty;
            m_EnumLiteral = enumLiteral;
            m_IdentityLiteral = identityLiteral ?? string.Empty;
        }
    }

    [Serializable]
    public sealed class CharacterPoseTransitionRuleProgram
    {
        public const string SchemaVersion = "character-pose-transition-rule/v2";

        [SerializeField] string m_GraphId = string.Empty;
        [SerializeField] string m_ContentRevision = string.Empty;
        [SerializeField] CharacterPoseTransitionRuleCompiledOperation[] m_Operations =
            Array.Empty<CharacterPoseTransitionRuleCompiledOperation>();
        [SerializeField] int m_OutputOperationIndex = -1;

        public PoseTransitionRuleGraphId GraphId => string.IsNullOrWhiteSpace(m_GraphId)
            ? default
            : new PoseTransitionRuleGraphId(m_GraphId);
        public string ContentRevision => m_ContentRevision ?? string.Empty;
        public IReadOnlyList<CharacterPoseTransitionRuleCompiledOperation> Operations =>
            m_Operations ?? Array.Empty<CharacterPoseTransitionRuleCompiledOperation>();
        public int OutputOperationIndex => m_OutputOperationIndex;

        internal CharacterPoseTransitionRuleProgram(
            PoseTransitionRuleGraphId graphId,
            string contentRevision,
            CharacterPoseTransitionRuleCompiledOperation[] operations,
            int outputOperationIndex)
        {
            m_GraphId = graphId.Value ?? string.Empty;
            m_ContentRevision = contentRevision ?? string.Empty;
            m_Operations = operations ?? Array.Empty<CharacterPoseTransitionRuleCompiledOperation>();
            m_OutputOperationIndex = outputOperationIndex;
            RequireValid();
        }

        public void RequireValid()
        {
            if (!GraphId.IsValid || string.IsNullOrWhiteSpace(ContentRevision) ||
                Operations.Count == 0 || (uint)OutputOperationIndex >= (uint)Operations.Count ||
                Operations[OutputOperationIndex] == null ||
                Operations[OutputOperationIndex].ValueKind != PoseTransitionRuleValueKind.Bool)
            {
                throw new InvalidOperationException("Compiled Pose Transition Rule is invalid.");
            }
        }
    }
}
