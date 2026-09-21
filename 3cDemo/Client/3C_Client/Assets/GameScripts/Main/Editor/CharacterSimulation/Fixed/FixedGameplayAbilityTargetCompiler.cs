using ThirdPersonSimulation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;

namespace ThirdPersonSimulation.Fixed
{
    public sealed class FixedGameplayAbilityExecutionCompilationResult
    {
        readonly ReadOnlyCollection<FixedScalarConversion> m_Conversions;

        public FixedGameplayAbilityExecutionCompilationResult(
            FixedGameplayAbilityExecutionData data,
            IEnumerable<FixedScalarConversion> conversions)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
            m_Conversions = new List<FixedScalarConversion>(conversions ?? Array.Empty<FixedScalarConversion>()).AsReadOnly();
        }

        public FixedGameplayAbilityExecutionData Data { get; }
        public IReadOnlyList<FixedScalarConversion> Conversions => m_Conversions;
    }

    public static class FixedGameplayAbilityTargetCompiler
    {
        static readonly HashSet<string> s_SupportedGameplayCapabilities = new HashSet<string>(StringComparer.Ordinal)
        {
            "Action",
            "GameplayEffect",
            "PipelineBlackboard",
            "RunnableTree",
            "StateMachine",
            "Timeline",
            "TimelineScenePresentationParameter"
        };

        public static FixedGameplayAbilityExecutionCompilationResult Compile(ValidatedSemanticIrArtifact artifact)
        {
            if (artifact == null)
                throw new ArgumentNullException(nameof(artifact));
            GameplayAbilitySemanticIr semanticIr = artifact.SemanticIr;
            GameplayAbilitySemanticIrArtifactHeader header = artifact.Header;
            if (!semanticIr.Manifest.Root.IsAbility || !header.Root.IsAbility)
                throw new InvalidOperationException("Fixed Ability Target requires an Ability Semantic IR artifact.");
            if (!semanticIr.Manifest.ProgramId.Equals(header.ProgramId) ||
                !semanticIr.Manifest.SourceRevision.Equals(header.SourceRevision) ||
                !semanticIr.SemanticHash.Equals(header.SemanticHash))
            {
                throw new InvalidOperationException("Validated Semantic IR artifact identity is inconsistent.");
            }
            GameplayAbilityOperationSet.RequireVersion(header.OperationSetVersion);
            GameplayAbilityOperationSet.RequireCompleteBackend(
                header.OperationSetVersion,
                FixedSimulationTarget.Manifest.ExecutionTarget.SupportedOperations,
                FixedSimulationTarget.Manifest.ExecutionTarget.BackendIdentity);
            for (int i = 0; i < header.GameplayCapabilities.Count; i++)
            {
                if (!s_SupportedGameplayCapabilities.Contains(header.GameplayCapabilities[i]))
                    throw new InvalidOperationException($"Fixed Target does not support gameplay capability '{header.GameplayCapabilities[i]}'.");
            }
            ValidateLiteralPrecision(semanticIr);
            FixedGameplayAbilityExecutionCompilationResult result =
                FixedGameplayAbilityExecutionDataLowerer.Lower(semanticIr);
            if (!result.Data.Root.Equals(header.Root) ||
                !string.Equals(result.Data.CompilerVersion, header.CompilerVersion, StringComparison.Ordinal) ||
                !result.Data.OperationSetVersion.Equals(header.OperationSetVersion) ||
                result.Data.TickRate != header.TickRate ||
                !result.Data.SourceRevision.Equals(header.SourceRevision) ||
                !result.Data.SemanticHash.Equals(header.SemanticHash) ||
                result.Data.NumericProfile != FixedSimulationNumericProfile.Value)
            {
                throw new InvalidOperationException("Fixed Ability execution data does not preserve its Semantic IR artifact identity.");
            }
            return result;
        }

        static void ValidateLiteralPrecision(GameplayAbilitySemanticIr semanticIr)
        {
            for (int i = 0; i < semanticIr.Literals.Count; i++)
            {
                SemanticNumericPrecision precision = semanticIr.Literals[i].Precision;
                if (precision != SemanticNumericPrecision.Exact && precision != SemanticNumericPrecision.TargetRounded)
                    throw new InvalidOperationException($"Fixed Target does not support literal precision '{precision}' at '{semanticIr.Literals[i].Identity}'.");
            }
        }
    }

    internal static class FixedGameplayAbilityExecutionDataLowerer
    {
        internal static FixedGameplayAbilityExecutionCompilationResult Lower(GameplayAbilitySemanticIr semanticIr)
        {
            if (semanticIr == null)
                throw new ArgumentNullException(nameof(semanticIr));
            if (!semanticIr.Manifest.Root.IsAbility)
                throw new InvalidOperationException("Fixed Ability execution data requires an Ability root.");
            FixedSimulationTargetManifest target = FixedSimulationTarget.Manifest;
            if (target.Profile != FixedSimulationNumericProfile.Value)
                throw new InvalidOperationException("Fixed Numeric Target manifest is inconsistent.");
            GameplayAbilityOperationSet.RequireVersion(semanticIr.Manifest.OperationSetVersion);
            GameplayAbilityOperationSet.RequireCompleteBackend(
                semanticIr.Manifest.OperationSetVersion,
                target.ExecutionTarget.SupportedOperations,
                target.ExecutionTarget.BackendIdentity);

            var conversions = new List<FixedScalarConversion>();
            var constants = new ProgramConstant[semanticIr.Literals.Count];
            for (int i = 0; i < semanticIr.Literals.Count; i++)
                constants[i] = LowerLiteral(semanticIr.Literals[i], conversions);

            var definitions = new List<SimulationOperationDefinition>();
            var definitionByIdentity = new Dictionary<string, SimulationOperationDefinition>(StringComparer.Ordinal);
            var operations = new SimulationOperation[semanticIr.Operations.Count];
            for (int i = 0; i < semanticIr.Operations.Count; i++)
            {
                SemanticOperation operation = semanticIr.Operations[i];
                GameplayAbilityOperationSet.RequireOperation(operation.Code);
                string sourceIdentity = operation.Number0SourceIdentity.Length == 0
                    ? $"operation:{operation.Handle.Value}/number0"
                    : operation.Number0SourceIdentity;
                FixedScalar scalar0 = LowerNumber(operation.Number0, sourceIdentity, SemanticNumericPrecision.TargetRounded, conversions);
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

            var stateSlots = new ProgramStateSlot[semanticIr.StateDeclarations.Count];
            for (int i = 0; i < stateSlots.Length; i++)
            {
                ProgramStateSlot source = semanticIr.StateDeclarations[i];
                stateSlots[i] = new ProgramStateSlot(
                    source.Index,
                    source.Identity,
                    source.ValueKind,
                    source.OwnerKind,
                    source.Semantic,
                    source.OwnerIdentity,
                    FixedGameplayAbilityStateSchema.CodecIdentity(source.ValueKind),
                    source.DefaultConstantIndex);
            }
            var constantInputs = new ProgramConstantInputBinding[semanticIr.ConstantInputBindings.Count];
            for (int i = 0; i < constantInputs.Length; i++)
            {
                SemanticConstantInputBinding constantBinding = semanticIr.ConstantInputBindings[i];
                constantInputs[i] = new ProgramConstantInputBinding(
                    constantBinding.TargetOperation,
                    constantBinding.TargetPort,
                    constantBinding.ConstantIndex,
                    constantBinding.ResolvedValueKind);
            }
            GameplayAbilityExecutionCatalog catalog = new GameplayAbilityExecutionCatalog(
                semanticIr.CatalogEntries,
                semanticIr.References);
            CharacterSkillId abilityId = RequireAbilityId(semanticIr.Manifest.Root.EntryIdentity);
            GameplayAbilityExecutionBinding binding = catalog.Require(abilityId);
            GameplayAbilityProviderContract providerContract = GameplayAbilityProviderContract.Create(
                semanticIr.CatalogEntries,
                index => constants[index].Int32);
            StableHash stateSchemaHash = StableHash.Compute(
                "fixed-gameplay-ability-execution-layout/1",
                semanticIr.SemanticHash.ToString(),
                semanticIr.StateDeclarations.Count.ToString(CultureInfo.InvariantCulture),
                semanticIr.GraphCallFrames.Count.ToString(CultureInfo.InvariantCulture));
            FixedGameplayAbilityExecutionData data = FixedGameplayAbilityExecutionData.Create(
                abilityId,
                binding,
                providerContract,
                semanticIr.Manifest.CompilerVersion,
                semanticIr.Manifest.OperationSetVersion,
                semanticIr.Manifest.TickRate,
                semanticIr.Manifest.SourceRevision,
                semanticIr.SemanticHash,
                target.Profile,
                semanticIr.Manifest.Capabilities,
                semanticIr.Manifest.Root,
                semanticIr.Manifest.ProgramId.Value,
                stateSchemaHash,
                definitions,
                operations,
                constants,
                constantInputs,
                semanticIr.ControlFlow,
                semanticIr.References,
                semanticIr.GraphCallFrames,
                stateSlots,
                semanticIr.Scopes,
                semanticIr.OutputChannels,
                semanticIr.CatalogEntries,
                semanticIr.SourceMap,
                semanticIr.Producers);
            return new FixedGameplayAbilityExecutionCompilationResult(data, conversions);
        }

        static CharacterSkillId RequireAbilityId(string entryIdentity)
        {
            const string prefix = "ability:";
            if (string.IsNullOrEmpty(entryIdentity) || !entryIdentity.StartsWith(prefix, StringComparison.Ordinal))
                throw new InvalidOperationException("Fixed Ability execution data entry identity is invalid.");
            return new CharacterSkillId(entryIdentity.Substring(prefix.Length));
        }

        static void RequireMatchingDefinition(
            SimulationOperationDefinition definition,
            SemanticOperation operation,
            FixedScalar scalar0)
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

        static ProgramConstant LowerLiteral(SemanticLiteral literal, List<FixedScalarConversion> conversions)
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
                    return ProgramConstant.FromVector2(literal.Index, literal.Identity, new FixedVector2(
                        LowerNumber(literal.X, $"{literal.Identity}.x", literal.Precision, conversions),
                        LowerNumber(literal.Y, $"{literal.Identity}.y", literal.Precision, conversions)));
                case SemanticLiteralKind.Vector3:
                    return ProgramConstant.FromVector3(literal.Index, literal.Identity, new FixedVector3(
                        LowerNumber(literal.X, $"{literal.Identity}.x", literal.Precision, conversions),
                        LowerNumber(literal.Y, $"{literal.Identity}.y", literal.Precision, conversions),
                        LowerNumber(literal.Z, $"{literal.Identity}.z", literal.Precision, conversions)));
                case SemanticLiteralKind.Yaw:
                    return ProgramConstant.FromYaw(literal.Index, literal.Identity, new FixedYaw(LowerNumber(literal.X, literal.Identity, literal.Precision, conversions)));
                case SemanticLiteralKind.String:
                    return ProgramConstant.FromString(literal.Index, literal.Identity, literal.Text);
                case SemanticLiteralKind.Document:
                    return ProgramConstant.FromBytes(literal.Index, literal.Identity, LowerDocument(literal.Document, conversions));
                default:
                    throw new InvalidOperationException($"Semantic literal '{literal.Identity}' kind '{literal.Kind}' is not supported by Fixed Target.");
            }
        }

        static byte[] LowerDocument(SemanticDataDocument document, List<FixedScalarConversion> conversions)
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
                    default: throw new InvalidOperationException($"Semantic document token '{token.Kind}' is not supported by Fixed Target.");
                }
            }
            return writer.ToArray();
        }

        static FixedScalar LowerNumber(
            double value,
            string sourceIdentity,
            SemanticNumericPrecision precision,
            List<FixedScalarConversion> conversions)
        {
            FixedScalarConversion conversion = FixedScalarBoundary.LowerAuthoring(value, sourceIdentity);
            if (precision == SemanticNumericPrecision.Exact && conversion.WasRounded)
                throw new InvalidOperationException($"Semantic number '{sourceIdentity}' requires exact representation in Fixed Target.");
            conversions.Add(conversion);
            return conversion.Value;
        }
    }
}

