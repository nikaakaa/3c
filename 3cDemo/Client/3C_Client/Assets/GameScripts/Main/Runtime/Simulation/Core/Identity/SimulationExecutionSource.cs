using System;

namespace ThirdPersonSimulation
{
    public enum SimulationExecutionSourceKind : byte
    {
        SkillOperation = 1,
        CharacterControl = 2,
        BlackboardCommand = 3
    }

    public readonly struct SimulationExecutionSource : IEquatable<SimulationExecutionSource>
    {
        SimulationExecutionSource(
            SimulationExecutionSourceKind kind,
            OperationHandle operation,
            string executionPath,
            CharacterControlModuleId moduleId,
            CharacterControlStateId stateId,
            CharacterControlTransitionId transitionId,
            string blackboardDeclaration = null)
        {
            if (kind != SimulationExecutionSourceKind.SkillOperation &&
                kind != SimulationExecutionSourceKind.CharacterControl &&
                kind != SimulationExecutionSourceKind.BlackboardCommand)
                throw new ArgumentOutOfRangeException(nameof(kind));
            if (kind == SimulationExecutionSourceKind.SkillOperation &&
                (!operation.IsValid || string.IsNullOrWhiteSpace(executionPath) || moduleId.IsValid || stateId.IsValid || transitionId.IsValid))
            {
                throw new ArgumentException("Skill execution source is incomplete.");
            }
            if (kind == SimulationExecutionSourceKind.CharacterControl &&
                (!moduleId.IsValid || !stateId.IsValid && !transitionId.IsValid || operation.IsValid || !string.IsNullOrEmpty(executionPath)))
            {
                throw new ArgumentException("Character control execution source is incomplete.");
            }
            if (kind == SimulationExecutionSourceKind.BlackboardCommand &&
                (string.IsNullOrWhiteSpace(blackboardDeclaration) || operation.IsValid || !string.IsNullOrEmpty(executionPath) ||
                 moduleId.IsValid || stateId.IsValid || transitionId.IsValid))
                throw new ArgumentException("Blackboard command execution source is incomplete.");
            Kind = kind;
            Operation = operation;
            ExecutionPath = executionPath ?? string.Empty;
            ModuleId = moduleId;
            StateId = stateId;
            TransitionId = transitionId;
            BlackboardDeclaration = blackboardDeclaration ?? string.Empty;
        }

        public static SimulationExecutionSource FromSkillOperation(OperationHandle operation, string executionPath) =>
            new SimulationExecutionSource(
                SimulationExecutionSourceKind.SkillOperation,
                operation,
                SimulationIdentity.Require(executionPath, nameof(executionPath)),
                default,
                default,
                default);

        public static SimulationExecutionSource FromCharacterControl(
            CharacterControlModuleId moduleId,
            CharacterControlStateId stateId,
            CharacterControlTransitionId transitionId) =>
            new SimulationExecutionSource(
                SimulationExecutionSourceKind.CharacterControl,
                OperationHandle.Invalid,
                string.Empty,
                moduleId,
                stateId,
                transitionId);

        public static SimulationExecutionSource FromBlackboardCommand(string declarationIdentity) =>
            new SimulationExecutionSource(SimulationExecutionSourceKind.BlackboardCommand,
                OperationHandle.Invalid, string.Empty, default, default, default, declarationIdentity);

        public SimulationExecutionSourceKind Kind { get; }
        public OperationHandle Operation { get; }
        public string ExecutionPath { get; }
        public CharacterControlModuleId ModuleId { get; }
        public CharacterControlStateId StateId { get; }
        public CharacterControlTransitionId TransitionId { get; }
        public string BlackboardDeclaration { get; }
        public bool IsBlackboardCommand => Kind == SimulationExecutionSourceKind.BlackboardCommand;
        public bool IsSkillOperation => Kind == SimulationExecutionSourceKind.SkillOperation;
        public bool IsCharacterControl => Kind == SimulationExecutionSourceKind.CharacterControl;
        public bool IsValid => IsSkillOperation
            ? Operation.IsValid && !string.IsNullOrEmpty(ExecutionPath)
            : IsBlackboardCommand
                ? !string.IsNullOrEmpty(BlackboardDeclaration)
                : IsCharacterControl && ModuleId.IsValid && (StateId.IsValid || TransitionId.IsValid);

        public string Identity => IsSkillOperation
            ? $"skill-operation:{ExecutionPath}"
            : IsBlackboardCommand
                ? $"blackboard-command:{BlackboardDeclaration}"
                : $"character-control:{ModuleId}:{StateId}:{TransitionId}";

        public bool Equals(SimulationExecutionSource other) =>
            Kind == other.Kind &&
            Operation.Equals(other.Operation) &&
            string.Equals(ExecutionPath, other.ExecutionPath, StringComparison.Ordinal) &&
            ModuleId == other.ModuleId &&
            StateId == other.StateId &&
            TransitionId == other.TransitionId &&
            string.Equals(BlackboardDeclaration, other.BlackboardDeclaration, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is SimulationExecutionSource other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Kind, Operation, ExecutionPath, ModuleId, StateId, TransitionId, BlackboardDeclaration);
        public override string ToString() => Identity;
        public static bool operator ==(SimulationExecutionSource left, SimulationExecutionSource right) => left.Equals(right);
        public static bool operator !=(SimulationExecutionSource left, SimulationExecutionSource right) => !left.Equals(right);
    }

    public static class SimulationExecutionSourceCodec
    {
        public static void Write(CanonicalWriter writer, SimulationExecutionSource source)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));
            if (!source.IsValid)
                throw new ArgumentException("Execution source is invalid.", nameof(source));
            writer.WriteByte((byte)source.Kind);
            if (source.IsSkillOperation)
            {
                writer.WriteInt32(source.Operation.Value);
                writer.WriteString(source.ExecutionPath);
                return;
            }
            if (source.IsBlackboardCommand)
            {
                writer.WriteString(source.BlackboardDeclaration);
                return;
            }
            writer.WriteString(source.ModuleId.Value);
            writer.WriteString(source.StateId.Value);
            writer.WriteString(source.TransitionId.Value);
        }

        public static SimulationExecutionSource Read(CanonicalReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));
            SimulationExecutionSourceKind kind = (SimulationExecutionSourceKind)reader.ReadByte();
            return kind switch
            {
                SimulationExecutionSourceKind.SkillOperation =>
                    SimulationExecutionSource.FromSkillOperation(
                        new OperationHandle(reader.ReadInt32()),
                        reader.ReadString()),
                SimulationExecutionSourceKind.BlackboardCommand =>
                    SimulationExecutionSource.FromBlackboardCommand(reader.ReadString()),
                SimulationExecutionSourceKind.CharacterControl =>
                    SimulationExecutionSource.FromCharacterControl(
                        new CharacterControlModuleId(reader.ReadString()),
                        ReadOptionalState(reader),
                        ReadOptionalTransition(reader)),
                _ => throw new InvalidOperationException($"Execution source kind '{kind}' is invalid.")
            };
        }

        static CharacterControlStateId ReadOptionalState(CanonicalReader reader)
        {
            string value = reader.ReadString();
            return string.IsNullOrEmpty(value) ? default : new CharacterControlStateId(value);
        }

        static CharacterControlTransitionId ReadOptionalTransition(CanonicalReader reader)
        {
            string value = reader.ReadString();
            return string.IsNullOrEmpty(value) ? default : new CharacterControlTransitionId(value);
        }
    }
}
