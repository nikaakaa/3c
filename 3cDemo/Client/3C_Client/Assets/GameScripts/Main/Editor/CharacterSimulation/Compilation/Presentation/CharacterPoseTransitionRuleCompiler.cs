using System;
using System.Collections.Generic;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal static class CharacterPoseTransitionRuleCompiler
    {
        internal static CharacterPoseTransitionRuleProgram Compile(CharacterPoseTransitionRuleGraph graph)
        {
            if (graph == null)
                throw new ArgumentNullException(nameof(graph));
            if (!graph.GraphId.IsValid || string.IsNullOrWhiteSpace(graph.ContentRevision))
                throw new InvalidOperationException("Pose Transition Rule graph identity is invalid.");
            if (!graph.OutputOperationId.IsValid)
                throw new InvalidOperationException($"Pose Transition Rule '{graph.GraphId}' has no Bool output.");

            var byId = new Dictionary<PoseTransitionRuleOperationId, CharacterPoseTransitionRuleOperation>();
            var sourceOrder = new List<CharacterPoseTransitionRuleOperation>(graph.Operations.Count);
            for (int i = 0; i < graph.Operations.Count; i++)
            {
                CharacterPoseTransitionRuleOperation operation = graph.Operations[i] ??
                    throw new InvalidOperationException(
                        $"Pose Transition Rule '{graph.GraphId}' operation #{i} is missing.");
                if (!operation.OperationId.IsValid || !byId.TryAdd(operation.OperationId, operation))
                {
                    throw new InvalidOperationException(
                        $"Pose Transition Rule '{graph.GraphId}' has a missing or duplicate operation identity.");
                }
                if (!Enum.IsDefined(typeof(PoseTransitionRuleOperationKind), operation.Kind))
                {
                    throw new InvalidOperationException(
                        $"Pose Transition Rule operation '{operation.OperationId}' has an unsupported kind.");
                }
                sourceOrder.Add(operation);
            }
            if (!byId.ContainsKey(graph.OutputOperationId))
            {
                throw new InvalidOperationException(
                    $"Pose Transition Rule '{graph.GraphId}' output '{graph.OutputOperationId}' does not exist.");
            }

            var visitState = new Dictionary<PoseTransitionRuleOperationId, byte>();
            var ordered = new List<CharacterPoseTransitionRuleOperation>(sourceOrder.Count);
            for (int i = 0; i < sourceOrder.Count; i++)
                Visit(sourceOrder[i], byId, visitState, ordered);

            var compiledIndexById = new Dictionary<PoseTransitionRuleOperationId, int>(ordered.Count);
            var signatures = new Dictionary<PoseTransitionRuleOperationId, ValueSignature>(ordered.Count);
            var compiled = new CharacterPoseTransitionRuleCompiledOperation[ordered.Count];
            for (int i = 0; i < ordered.Count; i++)
            {
                CharacterPoseTransitionRuleOperation operation = ordered[i];
                ValueSignature signature = InferSignature(operation, signatures);
                int inputA = ResolveCompiledInput(operation.InputA, compiledIndexById);
                int inputB = ResolveCompiledInput(operation.InputB, compiledIndexById);
                ValidateInputShape(operation, inputA, inputB);
                compiled[i] = new CharacterPoseTransitionRuleCompiledOperation(
                    ResolveCode(operation.Kind),
                    signature.Kind,
                    inputA,
                    inputB,
                    operation.FactId,
                    operation.BoolLiteral,
                    operation.FloatLiteral,
                    signature.EnumTypeId,
                    operation.EnumLiteral,
                    operation.IdentityLiteral);
                compiledIndexById.Add(operation.OperationId, i);
                signatures.Add(operation.OperationId, signature);
            }

            int outputIndex = compiledIndexById[graph.OutputOperationId];
            if (compiled[outputIndex].ValueKind != PoseTransitionRuleValueKind.Bool)
            {
                throw new InvalidOperationException(
                    $"Pose Transition Rule '{graph.GraphId}' output must be Bool.");
            }
            return new CharacterPoseTransitionRuleProgram(
                graph.GraphId,
                graph.ContentRevision,
                compiled,
                outputIndex);
        }

        static void Visit(
            CharacterPoseTransitionRuleOperation operation,
            IReadOnlyDictionary<PoseTransitionRuleOperationId, CharacterPoseTransitionRuleOperation> byId,
            Dictionary<PoseTransitionRuleOperationId, byte> visitState,
            List<CharacterPoseTransitionRuleOperation> ordered)
        {
            if (visitState.TryGetValue(operation.OperationId, out byte state))
            {
                if (state == 1)
                    throw new InvalidOperationException(
                        $"Pose Transition Rule contains a cycle at operation '{operation.OperationId}'.");
                return;
            }
            visitState.Add(operation.OperationId, 1);
            VisitInput(operation, operation.InputA, byId, visitState, ordered);
            VisitInput(operation, operation.InputB, byId, visitState, ordered);
            visitState[operation.OperationId] = 2;
            ordered.Add(operation);
        }

        static void VisitInput(
            CharacterPoseTransitionRuleOperation owner,
            PoseTransitionRuleOperationId inputId,
            IReadOnlyDictionary<PoseTransitionRuleOperationId, CharacterPoseTransitionRuleOperation> byId,
            Dictionary<PoseTransitionRuleOperationId, byte> visitState,
            List<CharacterPoseTransitionRuleOperation> ordered)
        {
            if (!inputId.IsValid)
                return;
            if (!byId.TryGetValue(inputId, out CharacterPoseTransitionRuleOperation input))
            {
                throw new InvalidOperationException(
                    $"Pose Transition Rule operation '{owner.OperationId}' references missing input '{inputId}'.");
            }
            Visit(input, byId, visitState, ordered);
        }

        static ValueSignature InferSignature(
            CharacterPoseTransitionRuleOperation operation,
            IReadOnlyDictionary<PoseTransitionRuleOperationId, ValueSignature> signatures)
        {
            switch (operation.Kind)
            {
                case PoseTransitionRuleOperationKind.FactInput:
                    return ResolveFactSignature(operation.FactId);
                case PoseTransitionRuleOperationKind.BoolLiteral:
                    return ValueSignature.Bool;
                case PoseTransitionRuleOperationKind.FloatLiteral:
                    if (!float.IsFinite(operation.FloatLiteral))
                        throw new InvalidOperationException(
                            $"Pose Transition Rule float literal '{operation.OperationId}' is not finite.");
                    return ValueSignature.Float;
                case PoseTransitionRuleOperationKind.EnumLiteral:
                    if (!PoseTransitionRuleEnumTypes.IsDefined(operation.EnumTypeId, operation.EnumLiteral))
                    {
                        throw new InvalidOperationException(
                            $"Pose Transition Rule enum literal '{operation.OperationId}' is invalid.");
                    }
                    return ValueSignature.Enum(operation.EnumTypeId);
                case PoseTransitionRuleOperationKind.IdentityLiteral:
                    if (string.IsNullOrWhiteSpace(operation.IdentityLiteral))
                    {
                        throw new InvalidOperationException(
                            $"Pose Transition Rule identity literal '{operation.OperationId}' is missing.");
                    }
                    return ValueSignature.Identity;
                case PoseTransitionRuleOperationKind.TimeInState:
                case PoseTransitionRuleOperationKind.StatePoseRemainingTime:
                    return ValueSignature.Float;
                case PoseTransitionRuleOperationKind.Not:
                    RequireSignature(operation, operation.InputA, signatures, ValueSignature.Bool);
                    return ValueSignature.Bool;
                case PoseTransitionRuleOperationKind.And:
                case PoseTransitionRuleOperationKind.Or:
                    RequireSignature(operation, operation.InputA, signatures, ValueSignature.Bool);
                    RequireSignature(operation, operation.InputB, signatures, ValueSignature.Bool);
                    return ValueSignature.Bool;
                case PoseTransitionRuleOperationKind.Equal:
                case PoseTransitionRuleOperationKind.NotEqual:
                {
                    ValueSignature left = RequireSignature(operation, operation.InputA, signatures);
                    ValueSignature right = RequireSignature(operation, operation.InputB, signatures);
                    if (!left.Equals(right))
                    {
                        throw new InvalidOperationException(
                            $"Pose Transition Rule comparison '{operation.OperationId}' input types do not match.");
                    }
                    return ValueSignature.Bool;
                }
                case PoseTransitionRuleOperationKind.Greater:
                case PoseTransitionRuleOperationKind.GreaterOrEqual:
                case PoseTransitionRuleOperationKind.Less:
                case PoseTransitionRuleOperationKind.LessOrEqual:
                    RequireSignature(operation, operation.InputA, signatures, ValueSignature.Float);
                    RequireSignature(operation, operation.InputB, signatures, ValueSignature.Float);
                    return ValueSignature.Bool;
                default:
                    throw new InvalidOperationException(
                        $"Pose Transition Rule operation '{operation.OperationId}' has no pure compiler mapping.");
            }
        }

        static ValueSignature ResolveFactSignature(PresentationFactId factId)
        {
            PresentationFactValueKind kind = CharacterPresentationFactSchema.RequireValueKind(factId);
            switch (kind)
            {
                case PresentationFactValueKind.Bool:
                    return ValueSignature.Bool;
                case PresentationFactValueKind.Float:
                    return ValueSignature.Float;
                case PresentationFactValueKind.Enum:
                    if (factId == CharacterPresentationFactSchema.MotionPhase)
                        return ValueSignature.Enum(PoseTransitionRuleEnumTypes.CharacterPresentationMotionPhase);
                    break;
                case PresentationFactValueKind.Identity:
                    return ValueSignature.Identity;
            }
            throw new InvalidOperationException(
                $"Presentation Fact '{factId}' is not a Bool, Float, or Enum Transition Rule input.");
        }

        static ValueSignature RequireSignature(
            CharacterPoseTransitionRuleOperation operation,
            PoseTransitionRuleOperationId inputId,
            IReadOnlyDictionary<PoseTransitionRuleOperationId, ValueSignature> signatures)
        {
            if (!inputId.IsValid || !signatures.TryGetValue(inputId, out ValueSignature signature))
            {
                throw new InvalidOperationException(
                    $"Pose Transition Rule operation '{operation.OperationId}' has no compiled input '{inputId}'.");
            }
            return signature;
        }

        static void RequireSignature(
            CharacterPoseTransitionRuleOperation operation,
            PoseTransitionRuleOperationId inputId,
            IReadOnlyDictionary<PoseTransitionRuleOperationId, ValueSignature> signatures,
            ValueSignature expected)
        {
            ValueSignature actual = RequireSignature(operation, inputId, signatures);
            if (!actual.Equals(expected))
            {
                throw new InvalidOperationException(
                    $"Pose Transition Rule operation '{operation.OperationId}' expected {expected} but received {actual}.");
            }
        }

        static int ResolveCompiledInput(
            PoseTransitionRuleOperationId inputId,
            IReadOnlyDictionary<PoseTransitionRuleOperationId, int> compiledIndexById) =>
            inputId.IsValid ? compiledIndexById[inputId] : -1;

        static void ValidateInputShape(
            CharacterPoseTransitionRuleOperation operation,
            int inputA,
            int inputB)
        {
            switch (operation.Kind)
            {
                case PoseTransitionRuleOperationKind.FactInput:
                case PoseTransitionRuleOperationKind.BoolLiteral:
                case PoseTransitionRuleOperationKind.FloatLiteral:
                case PoseTransitionRuleOperationKind.EnumLiteral:
                case PoseTransitionRuleOperationKind.IdentityLiteral:
                case PoseTransitionRuleOperationKind.TimeInState:
                case PoseTransitionRuleOperationKind.StatePoseRemainingTime:
                    if (inputA >= 0 || inputB >= 0)
                        throw InvalidShape(operation);
                    return;
                case PoseTransitionRuleOperationKind.Not:
                    if (inputA < 0 || inputB >= 0)
                        throw InvalidShape(operation);
                    return;
                default:
                    if (inputA < 0 || inputB < 0)
                        throw InvalidShape(operation);
                    return;
            }
        }

        static InvalidOperationException InvalidShape(CharacterPoseTransitionRuleOperation operation) =>
            new InvalidOperationException(
                $"Pose Transition Rule operation '{operation.OperationId}' has an invalid input shape.");

        static PoseTransitionRuleOperationCode ResolveCode(PoseTransitionRuleOperationKind kind)
        {
            switch (kind)
            {
                case PoseTransitionRuleOperationKind.FactInput:
                    return PoseTransitionRuleOperationCode.ReadFact;
                case PoseTransitionRuleOperationKind.BoolLiteral:
                    return PoseTransitionRuleOperationCode.BoolLiteral;
                case PoseTransitionRuleOperationKind.FloatLiteral:
                    return PoseTransitionRuleOperationCode.FloatLiteral;
                case PoseTransitionRuleOperationKind.EnumLiteral:
                    return PoseTransitionRuleOperationCode.EnumLiteral;
                case PoseTransitionRuleOperationKind.IdentityLiteral:
                    return PoseTransitionRuleOperationCode.IdentityLiteral;
                case PoseTransitionRuleOperationKind.Not:
                    return PoseTransitionRuleOperationCode.Not;
                case PoseTransitionRuleOperationKind.And:
                    return PoseTransitionRuleOperationCode.And;
                case PoseTransitionRuleOperationKind.Or:
                    return PoseTransitionRuleOperationCode.Or;
                case PoseTransitionRuleOperationKind.Equal:
                    return PoseTransitionRuleOperationCode.Equal;
                case PoseTransitionRuleOperationKind.NotEqual:
                    return PoseTransitionRuleOperationCode.NotEqual;
                case PoseTransitionRuleOperationKind.Greater:
                    return PoseTransitionRuleOperationCode.Greater;
                case PoseTransitionRuleOperationKind.GreaterOrEqual:
                    return PoseTransitionRuleOperationCode.GreaterOrEqual;
                case PoseTransitionRuleOperationKind.Less:
                    return PoseTransitionRuleOperationCode.Less;
                case PoseTransitionRuleOperationKind.LessOrEqual:
                    return PoseTransitionRuleOperationCode.LessOrEqual;
                case PoseTransitionRuleOperationKind.TimeInState:
                    return PoseTransitionRuleOperationCode.TimeInState;
                case PoseTransitionRuleOperationKind.StatePoseRemainingTime:
                    return PoseTransitionRuleOperationCode.StatePoseRemainingTime;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        readonly struct ValueSignature : IEquatable<ValueSignature>
        {
            ValueSignature(PoseTransitionRuleValueKind kind, string enumTypeId)
            {
                Kind = kind;
                EnumTypeId = enumTypeId ?? string.Empty;
            }

            internal static ValueSignature Bool =>
                new ValueSignature(PoseTransitionRuleValueKind.Bool, string.Empty);
            internal static ValueSignature Float =>
                new ValueSignature(PoseTransitionRuleValueKind.Float, string.Empty);
            internal static ValueSignature Enum(string enumTypeId) =>
                new ValueSignature(PoseTransitionRuleValueKind.Enum, enumTypeId);
            internal static ValueSignature Identity =>
                new ValueSignature(PoseTransitionRuleValueKind.Identity, string.Empty);

            internal PoseTransitionRuleValueKind Kind { get; }
            internal string EnumTypeId { get; }

            public bool Equals(ValueSignature other) =>
                Kind == other.Kind && string.Equals(EnumTypeId, other.EnumTypeId, StringComparison.Ordinal);
            public override bool Equals(object obj) => obj is ValueSignature other && Equals(other);
            public override int GetHashCode() => HashCode.Combine((int)Kind, EnumTypeId);
            public override string ToString() =>
                Kind == PoseTransitionRuleValueKind.Enum ? $"{Kind}/{EnumTypeId}" : Kind.ToString();
        }
    }
}
