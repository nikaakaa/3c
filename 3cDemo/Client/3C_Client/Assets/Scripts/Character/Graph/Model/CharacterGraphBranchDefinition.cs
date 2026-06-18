using System;

namespace ThirdPersonCharacterGraph
{
    public enum CharacterGraphBranchKind
    {
        None = 0,
        Locomotion = 1,
        Action = 2,
        UpperBody = 3,
        Cue = 4
    }

    public readonly struct CharacterGraphBranchId : IEquatable<CharacterGraphBranchId>
    {
        readonly string value;

        public CharacterGraphBranchId(string value)
        {
            this.value = (value ?? string.Empty).Trim();
        }

        public string Value => value ?? string.Empty;
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(CharacterGraphBranchId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is CharacterGraphBranchId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value);
        }

        public override string ToString()
        {
            return Value;
        }

        public static bool operator ==(CharacterGraphBranchId left, CharacterGraphBranchId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CharacterGraphBranchId left, CharacterGraphBranchId right)
        {
            return !left.Equals(right);
        }
    }

    public readonly struct CharacterGraphBranchDefinition
    {
        public CharacterGraphBranchDefinition(
            CharacterGraphBranchKind kind,
            CharacterGraphBranchId branchId,
            bool enabled)
        {
            Kind = kind;
            BranchId = branchId;
            Enabled = enabled;
        }

        public CharacterGraphBranchKind Kind { get; }
        public CharacterGraphBranchId BranchId { get; }
        public bool Enabled { get; }
        public bool IsDefined => Kind != CharacterGraphBranchKind.None && BranchId.IsValid;
        public bool CanEvaluate => IsDefined && Enabled;

        public CharacterGraphBranchSerializedForm ToSerializedForm()
        {
            return new CharacterGraphBranchSerializedForm(Kind, BranchId.Value, Enabled);
        }

        public static CharacterGraphBranchDefinition Empty(CharacterGraphBranchKind kind)
        {
            return new CharacterGraphBranchDefinition(kind, default, false);
        }

        public static CharacterGraphBranchDefinition Define(
            CharacterGraphBranchKind kind,
            string branchId,
            bool enabled = true)
        {
            return new CharacterGraphBranchDefinition(
                kind,
                new CharacterGraphBranchId(branchId),
                enabled);
        }
    }

    [Serializable]
    public struct CharacterGraphBranchSerializedForm
    {
        public CharacterGraphBranchKind kind;
        public string branchId;
        public bool enabled;

        public CharacterGraphBranchSerializedForm(
            CharacterGraphBranchKind kind,
            string branchId,
            bool enabled)
        {
            this.kind = kind;
            this.branchId = branchId ?? string.Empty;
            this.enabled = enabled;
        }

        public CharacterGraphBranchDefinition ToDefinition()
        {
            return CharacterGraphBranchDefinition.Define(kind, branchId, enabled);
        }
    }

    public readonly struct LocomotionBranchDefinition
    {
        public LocomotionBranchDefinition(CharacterGraphBranchDefinition branch)
        {
            Branch = branch.Kind == CharacterGraphBranchKind.Locomotion
                ? branch
                : CharacterGraphBranchDefinition.Empty(CharacterGraphBranchKind.Locomotion);
        }

        public CharacterGraphBranchDefinition Branch { get; }
        public bool IsDefined => Branch.IsDefined;
        public bool CanEvaluate => Branch.CanEvaluate;

        public static LocomotionBranchDefinition Empty =>
            new LocomotionBranchDefinition(CharacterGraphBranchDefinition.Empty(CharacterGraphBranchKind.Locomotion));

        public static LocomotionBranchDefinition Define(string branchId, bool enabled = true)
        {
            return new LocomotionBranchDefinition(
                CharacterGraphBranchDefinition.Define(CharacterGraphBranchKind.Locomotion, branchId, enabled));
        }
    }

    public readonly struct ActionBranchDefinition
    {
        public ActionBranchDefinition(CharacterGraphBranchDefinition branch)
        {
            Branch = branch.Kind == CharacterGraphBranchKind.Action
                ? branch
                : CharacterGraphBranchDefinition.Empty(CharacterGraphBranchKind.Action);
        }

        public CharacterGraphBranchDefinition Branch { get; }
        public bool IsDefined => Branch.IsDefined;
        public bool CanEvaluate => Branch.CanEvaluate;

        public static ActionBranchDefinition Empty =>
            new ActionBranchDefinition(CharacterGraphBranchDefinition.Empty(CharacterGraphBranchKind.Action));

        public static ActionBranchDefinition Define(string branchId, bool enabled = true)
        {
            return new ActionBranchDefinition(
                CharacterGraphBranchDefinition.Define(CharacterGraphBranchKind.Action, branchId, enabled));
        }
    }

    public readonly struct UpperBodyBranchDefinition
    {
        public UpperBodyBranchDefinition(CharacterGraphBranchDefinition branch)
        {
            Branch = branch.Kind == CharacterGraphBranchKind.UpperBody
                ? branch
                : CharacterGraphBranchDefinition.Empty(CharacterGraphBranchKind.UpperBody);
        }

        public CharacterGraphBranchDefinition Branch { get; }
        public bool IsDefined => Branch.IsDefined;
        public bool CanEvaluate => Branch.CanEvaluate;

        public static UpperBodyBranchDefinition Empty =>
            new UpperBodyBranchDefinition(CharacterGraphBranchDefinition.Empty(CharacterGraphBranchKind.UpperBody));

        public static UpperBodyBranchDefinition Define(string branchId, bool enabled = true)
        {
            return new UpperBodyBranchDefinition(
                CharacterGraphBranchDefinition.Define(CharacterGraphBranchKind.UpperBody, branchId, enabled));
        }
    }

    public readonly struct CueBranchDefinition
    {
        public CueBranchDefinition(CharacterGraphBranchDefinition branch)
        {
            Branch = branch.Kind == CharacterGraphBranchKind.Cue
                ? branch
                : CharacterGraphBranchDefinition.Empty(CharacterGraphBranchKind.Cue);
        }

        public CharacterGraphBranchDefinition Branch { get; }
        public bool IsDefined => Branch.IsDefined;
        public bool CanEvaluate => Branch.CanEvaluate;

        public static CueBranchDefinition Empty =>
            new CueBranchDefinition(CharacterGraphBranchDefinition.Empty(CharacterGraphBranchKind.Cue));

        public static CueBranchDefinition Define(string branchId, bool enabled = true)
        {
            return new CueBranchDefinition(
                CharacterGraphBranchDefinition.Define(CharacterGraphBranchKind.Cue, branchId, enabled));
        }
    }
}
