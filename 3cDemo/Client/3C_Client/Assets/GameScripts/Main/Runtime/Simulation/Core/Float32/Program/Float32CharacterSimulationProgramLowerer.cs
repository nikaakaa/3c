using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ThirdPersonSimulation
{
    public sealed class Float32ProgramLoweringResult
    {
        readonly ReadOnlyCollection<Float32ScalarConversion> m_Conversions;

        public Float32ProgramLoweringResult(CharacterSimulationProgram program, IEnumerable<Float32ScalarConversion> conversions)
        {
            Program = program ?? throw new ArgumentNullException(nameof(program));
            m_Conversions = new List<Float32ScalarConversion>(conversions ?? Array.Empty<Float32ScalarConversion>()).AsReadOnly();
        }

        public CharacterSimulationProgram Program { get; }
        public IReadOnlyList<Float32ScalarConversion> Conversions => m_Conversions;
    }

    public static class Float32CharacterSimulationTargetCompiler
    {
        static readonly HashSet<string> s_SupportedGameplayCapabilities = new HashSet<string>(StringComparer.Ordinal)
        {
            "Action",
            "GameplayEffect",
            "PipelineBlackboard",
            "RunnableTree",
            "StateMachine",
            "Timeline",
            "TimelineMotionCurve",
            "TimelineMotionWarp"
        };

        public static Float32ProgramLoweringResult Compile(ValidatedSemanticIrArtifact artifact)
        {
            if (artifact == null)
                throw new ArgumentNullException(nameof(artifact));
            CharacterGameplaySemanticIr semanticIr = artifact.SemanticIr;
            CharacterGameplaySemanticIrArtifactHeader header = artifact.Header;
            if (!semanticIr.Manifest.ProgramId.Equals(header.ProgramId) ||
                !semanticIr.Manifest.SourceRevision.Equals(header.SourceRevision) ||
                !semanticIr.SemanticHash.Equals(header.SemanticHash))
            {
                throw new InvalidOperationException("Validated Semantic IR artifact identity is inconsistent.");
            }
            CharacterGameplayOperationSet.RequireVersion(header.OperationSetVersion);
            CharacterGameplayOperationSet.RequireCompleteBackend(
                header.OperationSetVersion,
                Float32SimulationTarget.Manifest.KernelSpecialization.SupportedOperations,
                Float32SimulationTarget.Manifest.KernelSpecialization.BackendIdentity);
            for (int i = 0; i < header.GameplayCapabilities.Count; i++)
            {
                if (!s_SupportedGameplayCapabilities.Contains(header.GameplayCapabilities[i]))
                    throw new InvalidOperationException($"Float32 Target does not support gameplay capability '{header.GameplayCapabilities[i]}'.");
            }
            ValidateLiteralPrecision(semanticIr);
            Float32ProgramLoweringResult result = Float32CharacterSimulationProgramLowerer.Lower(semanticIr);
            CharacterSimulationProgramManifest manifest = result.Program.Manifest;
            if (!manifest.ProgramId.Equals(header.ProgramId) ||
                !string.Equals(manifest.CompilerVersion, header.CompilerVersion, StringComparison.Ordinal) ||
                !manifest.OperationSetVersion.Equals(header.OperationSetVersion) ||
                manifest.TickRate != header.TickRate ||
                !manifest.SourceRevision.Equals(header.SourceRevision) ||
                !manifest.SemanticHash.Equals(header.SemanticHash) ||
                manifest.NumericProfile != Float32SimulationNumericProfile.Value)
            {
                throw new InvalidOperationException("Float32 Program manifest does not preserve its Semantic IR artifact identity.");
            }
            return result;
        }

        static void ValidateLiteralPrecision(CharacterGameplaySemanticIr semanticIr)
        {
            for (int i = 0; i < semanticIr.Literals.Count; i++)
            {
                SemanticNumericPrecision precision = semanticIr.Literals[i].Precision;
                if (precision != SemanticNumericPrecision.Exact && precision != SemanticNumericPrecision.TargetRounded)
                    throw new InvalidOperationException($"Float32 Target does not support literal precision '{precision}' at '{semanticIr.Literals[i].Identity}'.");
            }
        }
    }

    static class Float32CharacterSimulationProgramLowerer
    {
        internal static Float32ProgramLoweringResult Lower(CharacterGameplaySemanticIr semanticIr)
        {
            if (semanticIr == null)
                throw new ArgumentNullException(nameof(semanticIr));
            Float32SimulationTargetManifest target = Float32SimulationTarget.Manifest;
            if (target.Profile != Float32SimulationNumericProfile.Value)
                throw new InvalidOperationException("Float32 Numeric Target manifest is inconsistent.");
            CharacterGameplayOperationSet.RequireVersion(semanticIr.Manifest.OperationSetVersion);
            CharacterGameplayOperationSet.RequireCompleteBackend(
                semanticIr.Manifest.OperationSetVersion,
                target.KernelSpecialization.SupportedOperations,
                target.KernelSpecialization.BackendIdentity);

            var conversions = new List<Float32ScalarConversion>();
            var constants = new ProgramConstant[semanticIr.Literals.Count];
            for (int i = 0; i < semanticIr.Literals.Count; i++)
                constants[i] = LowerLiteral(semanticIr.Literals[i], conversions);

            var definitions = new List<SimulationOperationDefinition>();
            var definitionByIdentity = new Dictionary<string, SimulationOperationDefinition>(StringComparer.Ordinal);
            var operations = new SimulationOperation[semanticIr.Operations.Count];
            for (int i = 0; i < semanticIr.Operations.Count; i++)
            {
                SemanticOperation operation = semanticIr.Operations[i];
                CharacterGameplayOperationSet.RequireOperation(operation.Code);
                string sourceIdentity = operation.Number0SourceIdentity.Length == 0
                    ? $"operation:{operation.Handle.Value}/number0"
                    : operation.Number0SourceIdentity;
                Float32Scalar scalar0 = LowerNumber(operation.Number0, sourceIdentity, SemanticNumericPrecision.TargetRounded, conversions);
                if (!definitionByIdentity.TryGetValue(operation.TemplateIdentity, out SimulationOperationDefinition definition))
                {
                    definition = new SimulationOperationDefinition(
                        definitions.Count,
                        operation.TemplateIdentity,
                        operation.Code,
                        operation.LiteralReferences,
                        operation.Integer0,
                        operation.Integer1,
                        operation.Unsigned0,
                        scalar0,
                        operation.Text0,
                        operation.Flags);
                    definitions.Add(definition);
                    definitionByIdentity.Add(operation.TemplateIdentity, definition);
                }
                else
                {
                    RequireMatchingDefinition(definition, operation, scalar0);
                }
                operations[i] = new SimulationOperation(
                    operation.Handle,
                    definition,
                    operation.Operands,
                    operation.StateSlots);
            }

            var manifest = new CharacterSimulationProgramManifest(
                semanticIr.Manifest.ProgramId,
                semanticIr.Manifest.CompilerVersion,
                semanticIr.Manifest.OperationSetVersion,
                semanticIr.Manifest.TickRate,
                semanticIr.Manifest.SourceRevision,
                semanticIr.SemanticHash,
                target.Profile,
                semanticIr.Manifest.Capabilities);
            var bodyMotion = new ProgramBodyMotionDescriptor(
                semanticIr.BodyMotion.SourceIdentity,
                semanticIr.BodyMotion.ContentRevision,
                semanticIr.BodyMotion.SemanticVersion,
                LowerNumber(
                    semanticIr.BodyMotion.GravityAcceleration,
                    $"{semanticIr.BodyMotion.SourceIdentity}/gravity-acceleration",
                    SemanticNumericPrecision.TargetRounded,
                    conversions),
                LowerNumber(
                    semanticIr.BodyMotion.MaximumFallSpeed,
                    $"{semanticIr.BodyMotion.SourceIdentity}/maximum-fall-speed",
                    SemanticNumericPrecision.TargetRounded,
                    conversions));
            var constantInputs = new ProgramConstantInputBinding[semanticIr.ConstantInputBindings.Count];
            for (int i = 0; i < constantInputs.Length; i++)
            {
                SemanticConstantInputBinding binding = semanticIr.ConstantInputBindings[i];
                constantInputs[i] = new ProgramConstantInputBinding(
                    binding.TargetOperation,
                    binding.TargetPort,
                    binding.ConstantIndex,
                    binding.ResolvedValueKind);
            }
            var program = new CharacterSimulationProgram(
                manifest,
                bodyMotion,
                definitions,
                operations,
                constants,
                constantInputs,
                semanticIr.ControlFlow,
                semanticIr.References,
                semanticIr.StateDeclarations,
                semanticIr.Scopes,
                semanticIr.WorldRequests,
                semanticIr.OutputChannels,
                semanticIr.CatalogEntries,
                ProgramMotionModifierCompiler.Compile(semanticIr),
                semanticIr.SourceMap,
                semanticIr.Producers);
            return new Float32ProgramLoweringResult(program, conversions);
        }

        static void RequireMatchingDefinition(
            SimulationOperationDefinition definition,
            SemanticOperation operation,
            Float32Scalar scalar0)
        {
            bool matches = definition.Code == operation.Code &&
                           definition.Integer0 == operation.Integer0 &&
                           definition.Integer1 == operation.Integer1 &&
                           definition.Unsigned0 == operation.Unsigned0 &&
                           definition.Scalar0 == scalar0 &&
                           string.Equals(definition.Text0, operation.Text0, StringComparison.Ordinal) &&
                           definition.Flags == operation.Flags &&
                           definition.ConstantReferences.Count == operation.LiteralReferences.Count;
            for (int i = 0; matches && i < definition.ConstantReferences.Count; i++)
                matches = definition.ConstantReferences[i] == operation.LiteralReferences[i];
            if (!matches)
                throw new InvalidOperationException($"Operation template '{operation.TemplateIdentity}' has conflicting immutable data across playback instances.");
        }

        static ProgramConstant LowerLiteral(SemanticLiteral literal, List<Float32ScalarConversion> conversions)
        {
            switch (literal.Kind)
            {
                case SemanticLiteralKind.Boolean:
                    return ProgramConstant.FromBoolean(literal.Index, literal.Identity, literal.Boolean);
                case SemanticLiteralKind.Int32:
                    return ProgramConstant.FromInt32(literal.Index, literal.Identity, literal.Int32);
                case SemanticLiteralKind.UInt64:
                    return ProgramConstant.FromUInt64(literal.Index, literal.Identity, literal.UInt64);
                case SemanticLiteralKind.Number:
                    return ProgramConstant.FromScalar(literal.Index, literal.Identity, LowerNumber(literal.X, literal.Identity, literal.Precision, conversions));
                case SemanticLiteralKind.Vector2:
                    return ProgramConstant.FromVector2(literal.Index, literal.Identity, new Float32Vector2(
                        LowerNumber(literal.X, $"{literal.Identity}.x", literal.Precision, conversions),
                        LowerNumber(literal.Y, $"{literal.Identity}.y", literal.Precision, conversions)));
                case SemanticLiteralKind.Vector3:
                    return ProgramConstant.FromVector3(literal.Index, literal.Identity, new Float32Vector3(
                        LowerNumber(literal.X, $"{literal.Identity}.x", literal.Precision, conversions),
                        LowerNumber(literal.Y, $"{literal.Identity}.y", literal.Precision, conversions),
                        LowerNumber(literal.Z, $"{literal.Identity}.z", literal.Precision, conversions)));
                case SemanticLiteralKind.Yaw:
                    return ProgramConstant.FromYaw(literal.Index, literal.Identity, new Float32Yaw(LowerNumber(literal.X, literal.Identity, literal.Precision, conversions)));
                case SemanticLiteralKind.String:
                    return ProgramConstant.FromString(literal.Index, literal.Identity, literal.Text);
                case SemanticLiteralKind.Document:
                    return ProgramConstant.FromBytes(literal.Index, literal.Identity, LowerDocument(literal.Document, conversions));
                default:
                    throw new InvalidOperationException($"Semantic literal '{literal.Identity}' kind '{literal.Kind}' is not supported by Float32 Target.");
            }
        }

        static byte[] LowerDocument(SemanticDataDocument document, List<Float32ScalarConversion> conversions)
        {
            using var writer = new CanonicalWriter();
            for (int i = 0; i < document.Tokens.Count; i++)
            {
                SemanticDataToken token = document.Tokens[i];
                switch (token.Kind)
                {
                    case SemanticDataTokenKind.Boolean: writer.WriteBoolean(token.Boolean); break;
                    case SemanticDataTokenKind.Int32: writer.WriteInt32(token.Int32); break;
                    case SemanticDataTokenKind.UInt32: writer.WriteUInt32(token.UInt32); break;
                    case SemanticDataTokenKind.UInt64: writer.WriteUInt64(token.UInt64); break;
                    case SemanticDataTokenKind.String: writer.WriteString(token.Text); break;
                    case SemanticDataTokenKind.Number: writer.WriteScalar(LowerNumber(token.Number, token.SourceIdentity, token.Precision, conversions)); break;
                    case SemanticDataTokenKind.Bytes: writer.WriteBytes(token.Bytes.ToArray()); break;
                    default: throw new InvalidOperationException($"Semantic document token '{token.Kind}' is not supported by Float32 Target.");
                }
            }
            return writer.ToArray();
        }

        static Float32Scalar LowerNumber(
            double value,
            string sourceIdentity,
            SemanticNumericPrecision precision,
            List<Float32ScalarConversion> conversions)
        {
            Float32ScalarConversion conversion = Float32ScalarBoundary.LowerAuthoring(value, sourceIdentity);
            if (precision == SemanticNumericPrecision.Exact && conversion.WasRounded)
                throw new InvalidOperationException($"Semantic number '{sourceIdentity}' requires exact representation in Float32 Target.");
            conversions.Add(conversion);
            return conversion.Value;
        }
    }
}
