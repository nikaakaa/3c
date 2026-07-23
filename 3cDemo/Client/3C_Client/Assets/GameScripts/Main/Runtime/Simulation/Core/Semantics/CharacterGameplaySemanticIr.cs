using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace ThirdPersonSimulation
{
    public enum SemanticNumericPrecision : byte
    {
        Exact = 1,
        TargetRounded = 2
    }

    public enum SemanticLiteralKind : byte
    {
        Boolean = 1,
        Int32 = 2,
        UInt64 = 3,
        Number = 4,
        Vector2 = 5,
        Vector3 = 6,
        Yaw = 7,
        String = 8,
        Document = 9
    }

    public enum SemanticDataTokenKind : byte
    {
        Boolean = 1,
        Int32 = 2,
        UInt32 = 3,
        UInt64 = 4,
        String = 5,
        Number = 6,
        Bytes = 7
    }

    public readonly struct SemanticDataToken
    {
        readonly byte[] m_Bytes;

        SemanticDataToken(
            SemanticDataTokenKind kind,
            bool boolean,
            int int32,
            uint uint32,
            ulong uint64,
            string text,
            double number,
            byte[] bytes,
            string sourceIdentity,
            SemanticNumericPrecision precision)
        {
            Kind = kind;
            Boolean = boolean;
            Int32 = int32;
            UInt32 = uint32;
            UInt64 = uint64;
            Text = text ?? string.Empty;
            Number = RequireFinite(number, nameof(number));
            m_Bytes = bytes == null ? Array.Empty<byte>() : (byte[])bytes.Clone();
            SourceIdentity = sourceIdentity ?? string.Empty;
            Precision = precision;
            if (kind == SemanticDataTokenKind.Number && SourceIdentity.Length == 0)
                throw new ArgumentException("Semantic numeric token requires a source identity.", nameof(sourceIdentity));
        }

        public SemanticDataTokenKind Kind { get; }
        public bool Boolean { get; }
        public int Int32 { get; }
        public uint UInt32 { get; }
        public ulong UInt64 { get; }
        public string Text { get; }
        public double Number { get; }
        public ReadOnlyMemory<byte> Bytes => m_Bytes ?? Array.Empty<byte>();
        public string SourceIdentity { get; }
        public SemanticNumericPrecision Precision { get; }

        public static SemanticDataToken FromBoolean(bool value) => new SemanticDataToken(SemanticDataTokenKind.Boolean, value, 0, 0, 0, null, 0, null, null, SemanticNumericPrecision.Exact);
        public static SemanticDataToken FromInt32(int value) => new SemanticDataToken(SemanticDataTokenKind.Int32, false, value, 0, 0, null, 0, null, null, SemanticNumericPrecision.Exact);
        public static SemanticDataToken FromUInt32(uint value) => new SemanticDataToken(SemanticDataTokenKind.UInt32, false, 0, value, 0, null, 0, null, null, SemanticNumericPrecision.Exact);
        public static SemanticDataToken FromUInt64(ulong value) => new SemanticDataToken(SemanticDataTokenKind.UInt64, false, 0, 0, value, null, 0, null, null, SemanticNumericPrecision.Exact);
        public static SemanticDataToken FromString(string value) => new SemanticDataToken(SemanticDataTokenKind.String, false, 0, 0, 0, value, 0, null, null, SemanticNumericPrecision.Exact);
        public static SemanticDataToken FromNumber(double value, string sourceIdentity, SemanticNumericPrecision precision = SemanticNumericPrecision.TargetRounded) => new SemanticDataToken(SemanticDataTokenKind.Number, false, 0, 0, 0, null, value, null, SimulationIdentity.Require(sourceIdentity, nameof(sourceIdentity)), precision);
        public static SemanticDataToken FromBytes(byte[] value) => new SemanticDataToken(SemanticDataTokenKind.Bytes, false, 0, 0, 0, null, 0, value, null, SemanticNumericPrecision.Exact);

        static double RequireFinite(double value, string parameter)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentOutOfRangeException(parameter);
            return value == 0d ? 0d : value;
        }
    }

    public sealed class SemanticDataDocument
    {
        readonly ReadOnlyCollection<SemanticDataToken> m_Tokens;

        public SemanticDataDocument(IEnumerable<SemanticDataToken> tokens)
        {
            m_Tokens = new List<SemanticDataToken>(tokens ?? Array.Empty<SemanticDataToken>()).AsReadOnly();
        }

        public IReadOnlyList<SemanticDataToken> Tokens => m_Tokens;
        public static SemanticDataDocument Empty { get; } = new SemanticDataDocument(Array.Empty<SemanticDataToken>());
    }

    public sealed class SemanticDataWriter
    {
        readonly List<SemanticDataToken> m_Tokens = new List<SemanticDataToken>();

        public void WriteBoolean(bool value) => m_Tokens.Add(SemanticDataToken.FromBoolean(value));
        public void WriteInt32(int value) => m_Tokens.Add(SemanticDataToken.FromInt32(value));
        public void WriteUInt32(uint value) => m_Tokens.Add(SemanticDataToken.FromUInt32(value));
        public void WriteUInt64(ulong value) => m_Tokens.Add(SemanticDataToken.FromUInt64(value));
        public void WriteString(string value) => m_Tokens.Add(SemanticDataToken.FromString(value));
        public void WriteNumber(double value, string sourceIdentity, SemanticNumericPrecision precision = SemanticNumericPrecision.TargetRounded) => m_Tokens.Add(SemanticDataToken.FromNumber(value, sourceIdentity, precision));
        public void WriteBytes(byte[] value) => m_Tokens.Add(SemanticDataToken.FromBytes(value));
        public SemanticDataDocument Build() => new SemanticDataDocument(m_Tokens);
    }

    public sealed class SemanticLiteral
    {
        SemanticLiteral(
            int index,
            string identity,
            SemanticLiteralKind kind,
            bool boolean,
            int int32,
            ulong uint64,
            double x,
            double y,
            double z,
            string text,
            SemanticDataDocument document,
            SemanticNumericPrecision precision)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            Index = index;
            Identity = SimulationIdentity.Require(identity, nameof(identity));
            Kind = kind;
            Boolean = boolean;
            Int32 = int32;
            UInt64 = uint64;
            X = RequireFinite(x, nameof(x));
            Y = RequireFinite(y, nameof(y));
            Z = RequireFinite(z, nameof(z));
            Text = text ?? string.Empty;
            Document = document;
            Precision = precision;
            if (kind == SemanticLiteralKind.Document && document == null)
                throw new ArgumentNullException(nameof(document));
        }

        public int Index { get; }
        public string Identity { get; }
        public SemanticLiteralKind Kind { get; }
        public bool Boolean { get; }
        public int Int32 { get; }
        public ulong UInt64 { get; }
        public double X { get; }
        public double Y { get; }
        public double Z { get; }
        public string Text { get; }
        public SemanticDataDocument Document { get; }
        public SemanticNumericPrecision Precision { get; }

        public static SemanticLiteral FromBoolean(int index, string identity, bool value) => new SemanticLiteral(index, identity, SemanticLiteralKind.Boolean, value, 0, 0, 0, 0, 0, null, null, SemanticNumericPrecision.Exact);
        public static SemanticLiteral FromInt32(int index, string identity, int value) => new SemanticLiteral(index, identity, SemanticLiteralKind.Int32, false, value, 0, 0, 0, 0, null, null, SemanticNumericPrecision.Exact);
        public static SemanticLiteral FromUInt64(int index, string identity, ulong value) => new SemanticLiteral(index, identity, SemanticLiteralKind.UInt64, false, 0, value, 0, 0, 0, null, null, SemanticNumericPrecision.Exact);
        public static SemanticLiteral FromNumber(int index, string identity, double value, SemanticNumericPrecision precision = SemanticNumericPrecision.TargetRounded) => new SemanticLiteral(index, identity, SemanticLiteralKind.Number, false, 0, 0, value, 0, 0, null, null, precision);
        public static SemanticLiteral FromVector2(int index, string identity, double x, double y, SemanticNumericPrecision precision = SemanticNumericPrecision.TargetRounded) => new SemanticLiteral(index, identity, SemanticLiteralKind.Vector2, false, 0, 0, x, y, 0, null, null, precision);
        public static SemanticLiteral FromVector3(int index, string identity, double x, double y, double z, SemanticNumericPrecision precision = SemanticNumericPrecision.TargetRounded) => new SemanticLiteral(index, identity, SemanticLiteralKind.Vector3, false, 0, 0, x, y, z, null, null, precision);
        public static SemanticLiteral FromYaw(int index, string identity, double value, SemanticNumericPrecision precision = SemanticNumericPrecision.TargetRounded) => new SemanticLiteral(index, identity, SemanticLiteralKind.Yaw, false, 0, 0, value, 0, 0, null, null, precision);
        public static SemanticLiteral FromString(int index, string identity, string value) => new SemanticLiteral(index, identity, SemanticLiteralKind.String, false, 0, 0, 0, 0, 0, value, null, SemanticNumericPrecision.Exact);
        public static SemanticLiteral FromDocument(int index, string identity, SemanticDataDocument value) => new SemanticLiteral(index, identity, SemanticLiteralKind.Document, false, 0, 0, 0, 0, 0, null, value, SemanticNumericPrecision.Exact);

        static double RequireFinite(double value, string parameter)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentOutOfRangeException(parameter);
            return value == 0d ? 0d : value;
        }
    }

    public sealed class SemanticOperation
    {
        readonly ReadOnlyCollection<int> m_Operands;
        readonly ReadOnlyCollection<int> m_LiteralReferences;
        readonly ReadOnlyCollection<int> m_StateSlots;

        public SemanticOperation(
            OperationHandle handle,
            string templateIdentity,
            SimulationOperationCode code,
            IEnumerable<int> operands,
            IEnumerable<int> literalReferences,
            IEnumerable<int> stateSlots,
            int integer0,
            int integer1,
            ulong unsigned0,
            double number0,
            string number0SourceIdentity,
            string text0,
            uint flags)
        {
            if (!handle.IsValid)
                throw new ArgumentException("Semantic operation handle is invalid.", nameof(handle));
            if (double.IsNaN(number0) || double.IsInfinity(number0))
                throw new ArgumentOutOfRangeException(nameof(number0));
            Handle = handle;
            TemplateIdentity = SimulationIdentity.Require(templateIdentity, nameof(templateIdentity));
            Code = code;
            m_Operands = ReadOnly(operands);
            m_LiteralReferences = ReadOnly(literalReferences);
            m_StateSlots = ReadOnly(stateSlots);
            Integer0 = integer0;
            Integer1 = integer1;
            Unsigned0 = unsigned0;
            Number0 = number0 == 0d ? 0d : number0;
            Number0SourceIdentity = number0SourceIdentity ?? string.Empty;
            Text0 = text0 ?? string.Empty;
            Flags = flags;
        }

        public OperationHandle Handle { get; }
        public string TemplateIdentity { get; }
        public SimulationOperationCode Code { get; }
        public IReadOnlyList<int> Operands => m_Operands;
        public IReadOnlyList<int> LiteralReferences => m_LiteralReferences;
        public IReadOnlyList<int> StateSlots => m_StateSlots;
        public int Integer0 { get; }
        public int Integer1 { get; }
        public ulong Unsigned0 { get; }
        public double Number0 { get; }
        public string Number0SourceIdentity { get; }
        public string Text0 { get; }
        public uint Flags { get; }

        static ReadOnlyCollection<int> ReadOnly(IEnumerable<int> source) => new List<int>(source ?? Array.Empty<int>()).AsReadOnly();
    }

    public sealed class SemanticConstantInputBinding
    {
        public SemanticConstantInputBinding(
            OperationHandle targetOperation,
            string targetPort,
            int constantIndex,
            SemanticValueKind resolvedValueKind)
        {
            if (!targetOperation.IsValid)
                throw new ArgumentException("Target operation is invalid.", nameof(targetOperation));
            if (constantIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(constantIndex));
            if (!Enum.IsDefined(typeof(SemanticValueKind), resolvedValueKind))
                throw new ArgumentOutOfRangeException(nameof(resolvedValueKind));
            TargetOperation = targetOperation;
            TargetPort = SimulationIdentity.Require(targetPort, nameof(targetPort));
            ConstantIndex = constantIndex;
            ResolvedValueKind = resolvedValueKind;
        }

        public OperationHandle TargetOperation { get; }
        public string TargetPort { get; }
        public int ConstantIndex { get; }
        public SemanticValueKind ResolvedValueKind { get; }
    }

    public sealed class CharacterGameplaySemanticIrManifest
    {
        public CharacterGameplaySemanticIrManifest(
            ProgramId programId,
            string compilerVersion,
            OperationSetVersion operationSetVersion,
            int tickRate,
            ProgramRevision sourceRevision,
            ProgramCapabilityManifest capabilities)
        {
            if (!programId.IsValid || !operationSetVersion.IsValid || tickRate <= 0 || string.IsNullOrEmpty(sourceRevision.Value))
                throw new ArgumentException("Semantic IR manifest is incomplete.");
            ProgramId = programId;
            CompilerVersion = SimulationIdentity.Require(compilerVersion, nameof(compilerVersion));
            OperationSetVersion = operationSetVersion;
            TickRate = tickRate;
            SourceRevision = sourceRevision;
            Capabilities = capabilities ?? throw new ArgumentNullException(nameof(capabilities));
        }

        public ProgramId ProgramId { get; }
        public string CompilerVersion { get; }
        public OperationSetVersion OperationSetVersion { get; }
        public int TickRate { get; }
        public ProgramRevision SourceRevision { get; }
        public ProgramCapabilityManifest Capabilities { get; }
    }

    public sealed class CharacterGameplaySemanticIr
    {
        readonly ReadOnlyCollection<SemanticOperation> m_Operations;
        readonly ReadOnlyCollection<SemanticLiteral> m_Literals;
        readonly ReadOnlyCollection<SemanticConstantInputBinding> m_ConstantInputBindings;
        readonly ReadOnlyCollection<ProgramControlFlowEdge> m_ControlFlow;
        readonly ReadOnlyCollection<ProgramReference> m_References;
        readonly ReadOnlyCollection<ProgramStateSlot> m_StateDeclarations;
        readonly ReadOnlyCollection<ProgramScopeLayout> m_Scopes;
        readonly ReadOnlyCollection<ProgramWorldRequestLayout> m_WorldRequests;
        readonly ReadOnlyCollection<ProgramOutputChannelLayout> m_OutputChannels;
        readonly ReadOnlyCollection<ProgramCatalogEntry> m_CatalogEntries;
        readonly ReadOnlyCollection<ProgramSourceMapEntry> m_SourceMap;
        readonly ReadOnlyCollection<ProgramProducer> m_Producers;

        public CharacterGameplaySemanticIr(
            CharacterGameplaySemanticIrManifest manifest,
            CharacterBodyMotionSemanticDescriptor bodyMotion,
            IEnumerable<SemanticOperation> operations,
            IEnumerable<SemanticLiteral> literals,
            IEnumerable<SemanticConstantInputBinding> constantInputBindings,
            IEnumerable<ProgramControlFlowEdge> controlFlow,
            IEnumerable<ProgramReference> references,
            IEnumerable<ProgramStateSlot> stateDeclarations,
            IEnumerable<ProgramScopeLayout> scopes,
            IEnumerable<ProgramWorldRequestLayout> worldRequests,
            IEnumerable<ProgramOutputChannelLayout> outputChannels,
            IEnumerable<ProgramCatalogEntry> catalogEntries,
            IEnumerable<ProgramSourceMapEntry> sourceMap,
            IEnumerable<ProgramProducer> producers)
        {
            Manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
            BodyMotion = bodyMotion ?? throw new ArgumentNullException(nameof(bodyMotion));
            if ((Manifest.Capabilities.RequiredWorldCapabilities & BodyMotion.RequiredWorldCapability) != BodyMotion.RequiredWorldCapability)
                throw new InvalidDataException("Semantic IR Body Motion capability is missing from the manifest.");
            m_Operations = Indexed(operations, value => value.Handle.Value, "semantic operation");
            m_Literals = Indexed(literals, value => value.Index, "semantic literal");
            m_ConstantInputBindings = SortConstantInputs(constantInputBindings, m_Operations);
            m_ControlFlow = ByIdentity(controlFlow, value => value.Identity, "control-flow edge");
            m_References = ByIdentity(references, value => value.Identity, "reference");
            m_StateDeclarations = Indexed(stateDeclarations, value => value.Index, "state declaration");
            m_Scopes = ByIdentity(scopes, value => value.Identity, "scope");
            m_WorldRequests = Indexed(worldRequests, value => value.Index, "world request");
            m_OutputChannels = Indexed(outputChannels, value => value.Index, "output channel");
            m_CatalogEntries = Indexed(catalogEntries, value => value.Index, "catalog entry");
            m_SourceMap = SortSourceMap(sourceMap);
            m_Producers = Indexed(producers, value => value.Index, "producer");
            ValidateReferences();
            SemanticHash = CharacterGameplaySemanticIrCodec.ComputeHash(this);
        }

        public CharacterGameplaySemanticIrManifest Manifest { get; }
        public CharacterBodyMotionSemanticDescriptor BodyMotion { get; }
        public IReadOnlyList<SemanticOperation> Operations => m_Operations;
        public IReadOnlyList<SemanticLiteral> Literals => m_Literals;
        public IReadOnlyList<SemanticConstantInputBinding> ConstantInputBindings => m_ConstantInputBindings;
        public IReadOnlyList<ProgramControlFlowEdge> ControlFlow => m_ControlFlow;
        public IReadOnlyList<ProgramReference> References => m_References;
        public IReadOnlyList<ProgramStateSlot> StateDeclarations => m_StateDeclarations;
        public IReadOnlyList<ProgramScopeLayout> Scopes => m_Scopes;
        public IReadOnlyList<ProgramWorldRequestLayout> WorldRequests => m_WorldRequests;
        public IReadOnlyList<ProgramOutputChannelLayout> OutputChannels => m_OutputChannels;
        public IReadOnlyList<ProgramCatalogEntry> CatalogEntries => m_CatalogEntries;
        public IReadOnlyList<ProgramSourceMapEntry> SourceMap => m_SourceMap;
        public IReadOnlyList<ProgramProducer> Producers => m_Producers;
        public SemanticHash SemanticHash { get; }

        public SemanticValueKind ResolveLinkedValueKind(ProgramControlFlowEdge edge)
        {
            if (edge == null || edge.Kind != ProgramControlFlowKind.Value)
                throw new ArgumentException("A Value control-flow edge is required.", nameof(edge));
            if (edge.Source.Value < 0 || edge.Source.Value >= m_Operations.Count ||
                edge.Target.Value < 0 || edge.Target.Value >= m_Operations.Count)
                throw new InvalidDataException($"Value edge '{edge.Identity}' references an operation outside the table.");
            SemanticOperation source = m_Operations[edge.Source.Value];
            OperationValuePortDefinition sourcePort = CharacterGameplayValuePortContracts
                .Require(source.Code)
                .RequireSelection(edge.SourcePort);
            return ResolveOutputKind(source, sourcePort);
        }

        void ValidateReferences()
        {
            CharacterGameplayOperationSet.RequireVersion(Manifest.OperationSetVersion);
            for (int i = 0; i < m_Operations.Count; i++)
            {
                SemanticOperation operation = m_Operations[i];
                CharacterGameplayOperationSet.RequireOperation(operation.Code);
                ValidateIndexes(operation.Operands, m_Operations.Count, $"Operation '{operation.Handle}' operand");
                ValidateIndexes(operation.LiteralReferences, m_Literals.Count, $"Operation '{operation.Handle}' literal");
                ValidateIndexes(operation.StateSlots, m_StateDeclarations.Count, $"Operation '{operation.Handle}' state");
                CameraProgramOperationSchema.Validate(operation, m_Literals);
            }
            for (int i = 0; i < m_StateDeclarations.Count; i++)
            {
                int index = m_StateDeclarations[i].DefaultConstantIndex;
                if (index < -1 || index >= m_Literals.Count)
                    throw new InvalidDataException($"State declaration '{m_StateDeclarations[i].Identity}' has invalid default literal '{index}'.");
            }
            var motionWarpSourceCounts = new int[m_Operations.Count];
            for (int i = 0; i < m_References.Count; i++)
            {
                ProgramReference reference = m_References[i];
                if (reference.HasSourceOperation &&
                    (reference.SourceOperation.Value < 0 || reference.SourceOperation.Value >= m_Operations.Count))
                {
                    throw new InvalidDataException($"Reference '{reference.Identity}' has an unknown source operation.");
                }
                if (reference.Kind != ProgramReferenceKind.MotionSourceOperation)
                    continue;
                if (!reference.HasSourceOperation || reference.TargetIndex < 0 || reference.TargetIndex >= m_Operations.Count)
                    throw new InvalidDataException($"Motion source reference '{reference.Identity}' is incomplete.");
                if (m_Operations[reference.SourceOperation.Value].Code != SimulationOperationCode.TimelineMotionWarp ||
                    m_Operations[reference.TargetIndex].Code != SimulationOperationCode.TimelineMotionCurve)
                {
                    throw new InvalidDataException($"Motion source reference '{reference.Identity}' must connect TimelineMotionWarp to TimelineMotionCurve.");
                }
                motionWarpSourceCounts[reference.SourceOperation.Value]++;
            }
            for (int i = 0; i < m_Operations.Count; i++)
            {
                if (m_Operations[i].Code != SimulationOperationCode.TimelineMotionWarp)
                    continue;
                if (!Manifest.Capabilities.HasGameplayCapability("TimelineMotionWarp"))
                    throw new InvalidDataException($"MotionWarp operation '{i}' is missing TimelineMotionWarp capability.");
                if (motionWarpSourceCounts[i] != 1)
                    throw new InvalidDataException($"MotionWarp operation '{i}' requires exactly one typed Motion source reference.");
            }
            var valueSources = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < m_ControlFlow.Count; i++)
            {
                ProgramControlFlowEdge edge = m_ControlFlow[i];
                if (edge.Source.Value < 0 || edge.Source.Value >= m_Operations.Count ||
                    edge.Target.Value < 0 || edge.Target.Value >= m_Operations.Count)
                    throw new InvalidDataException($"Control-flow edge '{edge.Identity}' references an operation outside the table.");
                SemanticOperation source = m_Operations[edge.Source.Value];
                if (edge.Kind != ProgramControlFlowKind.Value)
                    continue;
                SemanticOperation target = m_Operations[edge.Target.Value];
                OperationValuePortDefinition sourcePort = CharacterGameplayValuePortContracts.Require(source.Code).RequireSelection(edge.SourcePort);
                OperationValuePortDefinition targetPort = CharacterGameplayValuePortContracts.Require(target.Code).RequireInput(edge.TargetPort);
                SemanticValueKind sourceKind = ResolveOutputKind(source, sourcePort);
                SemanticValueKind targetKind = ResolveInputKind(target, targetPort, sourceKind);
                if (!targetPort.Accepts(sourceKind) || !KindsCanFlow(sourceKind, targetKind, targetPort.Constraint))
                    throw new InvalidDataException($"Value edge '{edge.Identity}' cannot assign '{sourceKind}' to '{target.Code}.{targetPort.Identity}' ({targetKind}).");
                string key = InputKey(edge.Target.Value, edge.TargetPort);
                if (!valueSources.Add(key))
                    throw new InvalidDataException($"Operation '{edge.Target}' input port '{edge.TargetPort}' has multiple linked Value sources.");
            }
            for (int i = 0; i < m_ConstantInputBindings.Count; i++)
            {
                SemanticConstantInputBinding binding = m_ConstantInputBindings[i];
                if (binding.TargetOperation.Value < 0 || binding.TargetOperation.Value >= m_Operations.Count)
                    throw new InvalidDataException($"Constant input binding targets unknown operation '{binding.TargetOperation}'.");
                if (binding.ConstantIndex < 0 || binding.ConstantIndex >= m_Literals.Count)
                    throw new InvalidDataException($"Constant input binding targets unknown literal '{binding.ConstantIndex}'.");
                SemanticOperation target = m_Operations[binding.TargetOperation.Value];
                OperationValuePortDefinition port = CharacterGameplayValuePortContracts.Require(target.Code).RequireInput(binding.TargetPort);
                SemanticValueKind literalKind = CharacterGameplayValuePortContracts.FromLiteral(m_Literals[binding.ConstantIndex].Kind);
                SemanticValueKind expectedKind = ResolveInputKind(target, port, binding.ResolvedValueKind);
                if (binding.ResolvedValueKind != literalKind ||
                    !port.Accepts(binding.ResolvedValueKind) ||
                    !KindsCanFlow(binding.ResolvedValueKind, expectedKind, port.Constraint))
                    throw new InvalidDataException($"Constant input '{target.Code}.{binding.TargetPort}' has incompatible kind '{binding.ResolvedValueKind}'.");
                string key = InputKey(binding.TargetOperation.Value, binding.TargetPort);
                if (!valueSources.Add(key))
                    throw new InvalidDataException($"Operation '{binding.TargetOperation}' input port '{binding.TargetPort}' has both linked and constant Value sources.");
            }
        }

        SemanticValueKind ResolveOutputKind(SemanticOperation operation, OperationValuePortDefinition port)
        {
            if (port.Constraint == OperationValuePortConstraint.Fixed)
                return port.FixedKind;
            if (operation.Code == SimulationOperationCode.BlackboardGet)
                return ResolveBlackboardKind(operation);
            if (operation.Code == SimulationOperationCode.Constant && operation.LiteralReferences.Count > 0)
                return CharacterGameplayValuePortContracts.FromLiteral(m_Literals[operation.LiteralReferences[0]].Kind);
            if (operation.Code == SimulationOperationCode.ReadEquipmentParameter)
            {
                return (EquipmentParameterValueKind)operation.Integer0 switch
                {
                    EquipmentParameterValueKind.Boolean => SemanticValueKind.Boolean,
                    EquipmentParameterValueKind.Int32 => SemanticValueKind.Int32,
                    EquipmentParameterValueKind.Scalar => SemanticValueKind.Number,
                    EquipmentParameterValueKind.Vector2 => SemanticValueKind.Vector2,
                    EquipmentParameterValueKind.Vector3 => SemanticValueKind.Vector3,
                    EquipmentParameterValueKind.Yaw => SemanticValueKind.Yaw,
                    EquipmentParameterValueKind.GameplayTag => SemanticValueKind.Identity,
                    EquipmentParameterValueKind.GameplayEffect => SemanticValueKind.Identity,
                    EquipmentParameterValueKind.AnimationProducer => SemanticValueKind.Identity,
                    _ => throw new InvalidDataException($"Equipment Parameter operation '{operation.Handle}' has an invalid value kind.")
                };
            }
            throw new InvalidDataException($"Value output '{operation.Code}.{port.Identity}' cannot resolve a concrete kind.");
        }

        SemanticValueKind ResolveInputKind(
            SemanticOperation operation,
            OperationValuePortDefinition port,
            SemanticValueKind actualKind)
        {
            if (port.Constraint == OperationValuePortConstraint.Dynamic)
            {
                if (operation.Code == SimulationOperationCode.BlackboardSet)
                    return ResolveBlackboardKind(operation);
                throw new InvalidDataException($"Value input '{operation.Code}.{port.Identity}' cannot resolve a concrete kind.");
            }
            return port.Resolve(actualKind);
        }

        SemanticValueKind ResolveBlackboardKind(SemanticOperation operation)
        {
            for (int i = 0; i < m_References.Count; i++)
            {
                ProgramReference reference = m_References[i];
                if (reference.Kind == ProgramReferenceKind.StateSlot && reference.SourceOperation.Equals(operation.Handle))
                    return CharacterGameplayValuePortContracts.FromState(m_StateDeclarations[reference.TargetIndex].ValueKind);
            }
            throw new InvalidDataException($"Blackboard operation '{operation.Handle}' has no compiled state address.");
        }

        static bool KindsCanFlow(
            SemanticValueKind source,
            SemanticValueKind target,
            OperationValuePortConstraint constraint)
        {
            if (source == target)
                return true;
            return constraint switch
            {
                OperationValuePortConstraint.BooleanLike => OperationValuePortDefinition.IsBooleanLike(source),
                OperationValuePortConstraint.NumericLike => OperationValuePortDefinition.IsNumericLike(source) && OperationValuePortDefinition.IsNumericLike(target),
                _ => false
            };
        }

        static string InputKey(int operation, string port) => operation.ToString() + ":" + (port ?? string.Empty);

        static void ValidateIndexes(IReadOnlyList<int> values, int count, string label)
        {
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i] < 0 || values[i] >= count)
                    throw new InvalidDataException($"{label} reference '{values[i]}' is outside the table.");
            }
        }

        static ReadOnlyCollection<T> Indexed<T>(IEnumerable<T> source, Func<T, int> index, string label)
        {
            var values = new List<T>(source ?? Array.Empty<T>());
            values.Sort((left, right) => index(left).CompareTo(index(right)));
            for (int i = 0; i < values.Count; i++)
            {
                if (index(values[i]) != i)
                    throw new InvalidDataException($"{label} table must contain contiguous canonical indexes.");
            }
            return values.AsReadOnly();
        }

        static ReadOnlyCollection<T> ByIdentity<T>(IEnumerable<T> source, Func<T, string> identity, string label)
        {
            var values = new List<T>(source ?? Array.Empty<T>());
            values.Sort((left, right) => string.CompareOrdinal(identity(left), identity(right)));
            for (int i = 1; i < values.Count; i++)
            {
                if (string.Equals(identity(values[i - 1]), identity(values[i]), StringComparison.Ordinal))
                    throw new InvalidDataException($"{label} identity '{identity(values[i])}' is duplicated.");
            }
            return values.AsReadOnly();
        }

        static ReadOnlyCollection<SemanticConstantInputBinding> SortConstantInputs(
            IEnumerable<SemanticConstantInputBinding> source,
            IReadOnlyList<SemanticOperation> operations)
        {
            var values = new List<SemanticConstantInputBinding>(source ?? Array.Empty<SemanticConstantInputBinding>());
            values.Sort((left, right) =>
            {
                int byOperation = left.TargetOperation.Value.CompareTo(right.TargetOperation.Value);
                if (byOperation != 0)
                    return byOperation;
                if (left.TargetOperation.Value < 0 || left.TargetOperation.Value >= operations.Count ||
                    right.TargetOperation.Value < 0 || right.TargetOperation.Value >= operations.Count)
                    return string.CompareOrdinal(left.TargetPort, right.TargetPort);
                OperationValuePortDefinition leftPort = CharacterGameplayValuePortContracts.Require(operations[left.TargetOperation.Value].Code).RequireInput(left.TargetPort);
                OperationValuePortDefinition rightPort = CharacterGameplayValuePortContracts.Require(operations[right.TargetOperation.Value].Code).RequireInput(right.TargetPort);
                int byOrder = leftPort.Order.CompareTo(rightPort.Order);
                return byOrder != 0 ? byOrder : string.CompareOrdinal(left.TargetPort, right.TargetPort);
            });
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i] == null)
                    throw new InvalidDataException("Constant input binding table contains null.");
                if (i > 0 && values[i - 1].TargetOperation.Equals(values[i].TargetOperation) &&
                    string.Equals(values[i - 1].TargetPort, values[i].TargetPort, StringComparison.Ordinal))
                    throw new InvalidDataException($"Constant input binding '{values[i].TargetOperation}/{values[i].TargetPort}' is duplicated.");
            }
            return values.AsReadOnly();
        }

        static ReadOnlyCollection<ProgramSourceMapEntry> SortSourceMap(IEnumerable<ProgramSourceMapEntry> source)
        {
            var values = new List<ProgramSourceMapEntry>(source ?? Array.Empty<ProgramSourceMapEntry>());
            values.Sort((left, right) =>
            {
                int result = left.TargetKind.CompareTo(right.TargetKind);
                if (result != 0) return result;
                result = left.TargetIndex.CompareTo(right.TargetIndex);
                if (result != 0) return result;
                return string.CompareOrdinal(left.DisplayPath, right.DisplayPath);
            });
            return values.AsReadOnly();
        }
    }

    public static class ProgramMotionModifierCompiler
    {
        static readonly ProgramStateSemantic[] s_MotionWarpStateSemantics =
        {
            ProgramStateSemantic.MotionWarpActive,
            ProgramStateSemantic.MotionWarpInitialized,
            ProgramStateSemantic.MotionWarpPlaybackGeneration,
            ProgramStateSemantic.MotionWarpActionInstance,
            ProgramStateSemantic.MotionWarpStartBodyPosition,
            ProgramStateSemantic.MotionWarpStartBodyYaw,
            ProgramStateSemantic.MotionWarpSourceWindowStartPosition,
            ProgramStateSemantic.MotionWarpSourceWindowStartYaw,
            ProgramStateSemantic.MotionWarpResolvedTargetPosition,
            ProgramStateSemantic.MotionWarpResolvedTargetYaw,
            ProgramStateSemantic.MotionWarpLimitResult,
            ProgramStateSemantic.MotionWarpPreviousWarpedPosition,
            ProgramStateSemantic.MotionWarpPreviousWarpedYaw,
            ProgramStateSemantic.MotionWarpLastPositionProgress,
            ProgramStateSemantic.MotionWarpLastYawProgress,
            ProgramStateSemantic.MotionWarpSourceOperation
        };

        public static IReadOnlyList<ProgramMotionModifierDescriptor> Compile(CharacterGameplaySemanticIr semanticIr)
        {
            if (semanticIr == null)
                throw new ArgumentNullException(nameof(semanticIr));
            var descriptors = new List<ProgramMotionModifierDescriptor>();
            for (int operationIndex = 0; operationIndex < semanticIr.Operations.Count; operationIndex++)
            {
                SemanticOperation operation = semanticIr.Operations[operationIndex];
                if (operation.Code != SimulationOperationCode.TimelineMotionWarp)
                    continue;
                descriptors.Add(CompileMotionWarp(semanticIr, operation, descriptors.Count));
            }
            return descriptors.AsReadOnly();
        }

        static ProgramMotionModifierDescriptor CompileMotionWarp(
            CharacterGameplaySemanticIr semanticIr,
            SemanticOperation operation,
            int descriptorIndex)
        {
            ProgramReference sourceReference = RequireSingleReference(
                semanticIr,
                operation.Handle,
                ProgramReferenceKind.MotionSourceOperation,
                "Motion source");
            ProgramCatalogEntry catalog = RequireOperationCatalog(
                semanticIr,
                operation.Handle,
                ProgramCatalogEntryKind.TimelineClip,
                "MotionWarp");
            if (sourceReference.TargetIndex < 0 || sourceReference.TargetIndex >= semanticIr.Operations.Count ||
                semanticIr.Operations[sourceReference.TargetIndex].Code != SimulationOperationCode.TimelineMotionCurve)
            {
                throw new InvalidDataException($"MotionWarp operation '{operation.Handle}' source is not a MotionCurve operation.");
            }

            var sourceOperation = new OperationHandle(sourceReference.TargetIndex);
            ProgramCatalogEntry sourceCatalog = RequireOperationCatalog(
                semanticIr,
                sourceOperation,
                ProgramCatalogEntryKind.MotionCurve,
                "MotionWarp source");

            int timelineOwner = RequireInt32Literal(semanticIr, catalog, "TimelineOwnerOperation");
            if (timelineOwner < 0 || timelineOwner >= semanticIr.Operations.Count ||
                semanticIr.Operations[timelineOwner].Code != SimulationOperationCode.Timeline)
            {
                throw new InvalidDataException($"MotionWarp operation '{operation.Handle}' has an invalid Timeline owner operation.");
            }
            ValidateSourceAndOwner(semanticIr, operation, catalog, sourceCatalog, new OperationHandle(timelineOwner));
            string actionContext = RequireIdentity(catalog, "ActionContext");
            ProgramMotionWarpTranslationMode translationMode = RequireTranslationMode(operation, semanticIr, catalog);
            ProgramMotionWarpTargetOffsetSpace targetOffsetSpace = RequireEnumLiteral<ProgramMotionWarpTargetOffsetSpace>(semanticIr, catalog, "TargetOffsetSpace");
            ProgramMotionWarpRotationMode rotationMode = RequireRotationMode(operation, semanticIr, catalog);
            ProgramMotionWarpRotationMethod rotationMethod = RequireEnumLiteral<ProgramMotionWarpRotationMethod>(semanticIr, catalog, "RotationMethod");
            ProgramMotionWarpLimitPolicy limitPolicy = RequireEnumLiteral<ProgramMotionWarpLimitPolicy>(semanticIr, catalog, "LimitPolicy");
            if (translationMode == ProgramMotionWarpTranslationMode.Disabled && rotationMode == ProgramMotionWarpRotationMode.Disabled)
                throw new InvalidDataException($"MotionWarp operation '{operation.Handle}' disables both correction modes.");
            int stateSlotStart = RequireMotionWarpStateLayout(semanticIr, operation);
            bool hasTranslation = translationMode != ProgramMotionWarpTranslationMode.Disabled;
            bool hasRotation = rotationMode != ProgramMotionWarpRotationMode.Disabled;
            bool usesPositionProgress = translationMode is ProgramMotionWarpTranslationMode.SkewToTarget or ProgramMotionWarpTranslationMode.LinearToTarget;
            bool usesYawProgress = hasRotation && rotationMethod == ProgramMotionWarpRotationMethod.ProgressCurve;
            bool usesYawRate = hasRotation && rotationMethod == ProgramMotionWarpRotationMethod.ConstantRate;
            ValidateScaleSource(
                semanticIr,
                operation,
                catalog,
                sourceCatalog,
                translationMode,
                rotationMode,
                rotationMethod);

            return new ProgramMotionModifierDescriptor(
                descriptorIndex,
                ProgramMotionModifierKind.MotionWarp,
                ProgramMotionModifierChannel.Action,
                operation.Handle,
                sourceOperation,
                new OperationHandle(timelineOwner),
                actionContext,
                catalog.Index,
                stateSlotStart,
                ProgramMotionModifierDescriptor.MotionWarpStateSlotCount,
                translationMode,
                targetOffsetSpace,
                rotationMode,
                rotationMethod,
                OptionalLiteral(semanticIr, catalog, "TargetPlanarOffset", SemanticLiteralKind.Vector2, hasTranslation),
                OptionalLiteral(semanticIr, catalog, "TargetYawOffsetDegrees", SemanticLiteralKind.Number, hasRotation),
                OptionalLiteral(semanticIr, catalog, "MaximumPlanarCorrection", SemanticLiteralKind.Number, hasTranslation),
                OptionalLiteral(semanticIr, catalog, "MaximumYawCorrectionDegrees", SemanticLiteralKind.Number, hasRotation),
                OptionalLiteral(semanticIr, catalog, "MaximumYawRateDegreesPerSecond", SemanticLiteralKind.Number, usesYawRate),
                limitPolicy,
                OptionalLiteral(semanticIr, catalog, "PositionProgressCurve", SemanticLiteralKind.Document, usesPositionProgress),
                OptionalLiteral(semanticIr, catalog, "YawProgressCurve", SemanticLiteralKind.Document, usesYawProgress));
        }

        static void ValidateScaleSource(
            CharacterGameplaySemanticIr semanticIr,
            SemanticOperation operation,
            ProgramCatalogEntry warpCatalog,
            ProgramCatalogEntry sourceCatalog,
            ProgramMotionWarpTranslationMode translationMode,
            ProgramMotionWarpRotationMode rotationMode,
            ProgramMotionWarpRotationMethod rotationMethod)
        {
            if (translationMode != ProgramMotionWarpTranslationMode.ScaleToTarget &&
                (rotationMode == ProgramMotionWarpRotationMode.Disabled || rotationMethod != ProgramMotionWarpRotationMethod.ScaleSourceYaw))
            {
                return;
            }

            int sourceStartFrame = RequireInt32Literal(semanticIr, sourceCatalog, "StartFrame");
            int sourceCurveEndFrame = RequireInt32Literal(semanticIr, sourceCatalog, "CurveEndFrame");
            int warpStartFrame = RequireInt32Literal(semanticIr, warpCatalog, "StartFrame");
            int warpEndFrame = RequireInt32Literal(semanticIr, warpCatalog, "EndFrame");
            int sourceDuration = sourceCurveEndFrame - sourceStartFrame;
            if (sourceDuration <= 0)
                throw new InvalidDataException($"MotionWarp operation '{operation.Handle}' source MotionCurve has a non-positive duration.");
            double start = Clamp01((warpStartFrame - sourceStartFrame) / (double)sourceDuration);
            double end = Clamp01((warpEndFrame - sourceStartFrame) / (double)sourceDuration);

            if (translationMode == ProgramMotionWarpTranslationMode.ScaleToTarget)
            {
                double startX = EvaluateCurve(RequireDocumentLiteral(semanticIr, sourceCatalog, "PositionX"), start);
                double startZ = EvaluateCurve(RequireDocumentLiteral(semanticIr, sourceCatalog, "PositionZ"), start);
                double endX = EvaluateCurve(RequireDocumentLiteral(semanticIr, sourceCatalog, "PositionX"), end);
                double endZ = EvaluateCurve(RequireDocumentLiteral(semanticIr, sourceCatalog, "PositionZ"), end);
                double x = endX - startX;
                double z = endZ - startZ;
                if (x * x + z * z <= 0.000000000001d)
                    throw new InvalidDataException($"MotionWarp operation '{operation.Handle}' ScaleToTarget requires a non-zero source window planar endpoint.");
            }

            if (rotationMode != ProgramMotionWarpRotationMode.Disabled && rotationMethod == ProgramMotionWarpRotationMethod.ScaleSourceYaw)
            {
                SemanticDataDocument yaw = RequireDocumentLiteral(semanticIr, sourceCatalog, "Yaw");
                if (Math.Abs(EvaluateCurve(yaw, end) - EvaluateCurve(yaw, start)) <= 0.000001d)
                    throw new InvalidDataException($"MotionWarp operation '{operation.Handle}' ScaleSourceYaw requires non-zero source window yaw.");
            }
        }

        static SemanticDataDocument RequireDocumentLiteral(
            CharacterGameplaySemanticIr semanticIr,
            ProgramCatalogEntry catalog,
            string name)
        {
            return semanticIr.Literals[RequireLiteral(semanticIr, catalog, name, SemanticLiteralKind.Document)].Document;
        }

        static double EvaluateCurve(SemanticDataDocument document, double time)
        {
            IReadOnlyList<SemanticDataToken> tokens = document?.Tokens ?? throw new InvalidDataException("MotionCurve document is missing.");
            if (tokens.Count < 5 ||
                tokens[0].Kind != SemanticDataTokenKind.UInt32 || tokens[0].UInt32 != 0x56525543 ||
                tokens[1].Kind != SemanticDataTokenKind.Int32 || tokens[1].Int32 != 1 ||
                tokens[2].Kind != SemanticDataTokenKind.Int32 ||
                tokens[3].Kind != SemanticDataTokenKind.Int32 ||
                tokens[4].Kind != SemanticDataTokenKind.Int32)
            {
                throw new InvalidDataException("MotionCurve document header is invalid.");
            }
            int count = tokens[4].Int32;
            if (count <= 0 || tokens.Count != 5 + count * 7)
                throw new InvalidDataException($"MotionCurve document key count '{count}' is invalid.");

            var keys = new SemanticCurveKey[count];
            int tokenIndex = 5;
            for (int i = 0; i < count; i++)
            {
                for (int field = 0; field < 6; field++)
                {
                    if (tokens[tokenIndex + field].Kind != SemanticDataTokenKind.Number)
                        throw new InvalidDataException($"MotionCurve key '{i}' field '{field}' is not numeric.");
                }
                if (tokens[tokenIndex + 6].Kind != SemanticDataTokenKind.Int32 || tokens[tokenIndex + 6].Int32 != 0)
                    throw new InvalidDataException($"MotionCurve key '{i}' uses unsupported weighted tangents.");
                keys[i] = new SemanticCurveKey(
                    tokens[tokenIndex].Number,
                    tokens[tokenIndex + 1].Number,
                    tokens[tokenIndex + 2].Number,
                    tokens[tokenIndex + 3].Number);
                if (i > 0 && keys[i].Time <= keys[i - 1].Time)
                    throw new InvalidDataException($"MotionCurve key '{i}' time must be greater than the previous key time.");
                tokenIndex += 7;
            }

            if (count == 1 || time <= keys[0].Time)
                return keys[0].Value;
            if (time >= keys[count - 1].Time)
                return keys[count - 1].Value;
            int low = 0;
            int high = count - 1;
            while (high - low > 1)
            {
                int middle = low + (high - low) / 2;
                if (keys[middle].Time <= time)
                    low = middle;
                else
                    high = middle;
            }
            SemanticCurveKey from = keys[low];
            SemanticCurveKey to = keys[high];
            double duration = to.Time - from.Time;
            double t = (time - from.Time) / duration;
            double t2 = t * t;
            double t3 = t2 * t;
            return (2d * t3 - 3d * t2 + 1d) * from.Value +
                   (t3 - 2d * t2 + t) * duration * from.OutTangent +
                   (-2d * t3 + 3d * t2) * to.Value +
                   (t3 - t2) * duration * to.InTangent;
        }

        static double Clamp01(double value) => value < 0d ? 0d : value > 1d ? 1d : value;

        readonly struct SemanticCurveKey
        {
            public SemanticCurveKey(double time, double value, double inTangent, double outTangent)
            {
                Time = time;
                Value = value;
                InTangent = inTangent;
                OutTangent = outTangent;
            }

            public double Time { get; }
            public double Value { get; }
            public double InTangent { get; }
            public double OutTangent { get; }
        }

        static void ValidateSourceAndOwner(
            CharacterGameplaySemanticIr semanticIr,
            SemanticOperation operation,
            ProgramCatalogEntry warpCatalog,
            ProgramCatalogEntry sourceCatalog,
            OperationHandle timelineOwnerOperation)
        {
            string declaredTimelineOwner = RequireIdentity(warpCatalog, "TimelineOwner");
            ProgramCatalogEntry warpTrack = RequireCatalog(
                semanticIr,
                ProgramCatalogEntryKind.TimelineTrack,
                RequireIdentity(warpCatalog, "Track"));
            ProgramCatalogEntry sourceTrack = RequireCatalog(
                semanticIr,
                ProgramCatalogEntryKind.TimelineTrack,
                RequireIdentity(sourceCatalog, "Track"));
            string warpTimelineOwner = RequireIdentity(warpTrack, "Timeline");
            string sourceTimelineOwner = RequireIdentity(sourceTrack, "Timeline");
            ProgramCatalogEntry timelineCatalog = RequireCatalog(
                semanticIr,
                ProgramCatalogEntryKind.Timeline,
                declaredTimelineOwner);

            if (!ReferencesCatalog(semanticIr, timelineOwnerOperation, timelineCatalog.Index) ||
                !string.Equals(declaredTimelineOwner, warpTimelineOwner, StringComparison.Ordinal) ||
                !string.Equals(declaredTimelineOwner, sourceTimelineOwner, StringComparison.Ordinal))
            {
                throw new InvalidDataException($"MotionWarp operation '{operation.Handle}' and its source must share the declared Timeline owner.");
            }

            int channel = RequireInt32Literal(semanticIr, sourceCatalog, "Channel");
            int blendMode = RequireInt32Literal(semanticIr, sourceCatalog, "BlendMode");
            int space = RequireInt32Literal(semanticIr, sourceCatalog, "Space");
            if (channel != (int)ProgramMotionModifierChannel.Action ||
                blendMode != (int)ProgramMotionSourceBlendMode.Override ||
                space != 0)
            {
                throw new InvalidDataException($"MotionWarp operation '{operation.Handle}' source must use Action channel, Override blend mode, and ActorLocal space.");
            }
            if (RequireInt32Literal(semanticIr, sourceCatalog, "EaseInFrame") != 0 ||
                RequireInt32Literal(semanticIr, sourceCatalog, "EaseOutFrame") != 0 ||
                !IsUnitCurve(RequireDocumentLiteral(semanticIr, sourceCatalog, "WeightCurve")))
            {
                throw new InvalidDataException($"MotionWarp operation '{operation.Handle}' source must use unit Gameplay weight with no ease.");
            }

            int sourceStartFrame = RequireInt32Literal(semanticIr, sourceCatalog, "StartFrame");
            int sourceCurveEndFrame = RequireInt32Literal(semanticIr, sourceCatalog, "CurveEndFrame");
            int warpStartFrame = RequireInt32Literal(semanticIr, warpCatalog, "StartFrame");
            int warpEndFrame = RequireInt32Literal(semanticIr, warpCatalog, "EndFrame");
            if (warpStartFrame < sourceStartFrame || warpEndFrame > sourceCurveEndFrame || warpEndFrame <= warpStartFrame)
            {
                throw new InvalidDataException($"MotionWarp operation '{operation.Handle}' window must be non-empty and remain within its source MotionCurve range.");
            }
        }

        static bool IsUnitCurve(SemanticDataDocument document)
        {
            IReadOnlyList<SemanticDataToken> tokens = document?.Tokens ?? throw new InvalidDataException("MotionCurve weight document is missing.");
            if (tokens.Count < 12 ||
                tokens[0].Kind != SemanticDataTokenKind.UInt32 || tokens[0].UInt32 != 0x56525543 ||
                tokens[1].Kind != SemanticDataTokenKind.Int32 || tokens[1].Int32 != 1 ||
                tokens[4].Kind != SemanticDataTokenKind.Int32 || tokens[4].Int32 <= 0 ||
                tokens.Count != 5 + tokens[4].Int32 * 7)
            {
                throw new InvalidDataException("MotionCurve weight document header is invalid.");
            }
            for (int tokenIndex = 5; tokenIndex < tokens.Count; tokenIndex += 7)
            {
                if (tokens[tokenIndex + 1].Kind != SemanticDataTokenKind.Number || Math.Abs(tokens[tokenIndex + 1].Number - 1d) > 0.000001d ||
                    tokens[tokenIndex + 2].Kind != SemanticDataTokenKind.Number || Math.Abs(tokens[tokenIndex + 2].Number) > 0.000001d ||
                    tokens[tokenIndex + 3].Kind != SemanticDataTokenKind.Number || Math.Abs(tokens[tokenIndex + 3].Number) > 0.000001d ||
                    tokens[tokenIndex + 6].Kind != SemanticDataTokenKind.Int32 || tokens[tokenIndex + 6].Int32 != 0)
                    return false;
            }
            return true;
        }

        static ProgramCatalogEntry RequireOperationCatalog(
            CharacterGameplaySemanticIr semanticIr,
            OperationHandle operation,
            ProgramCatalogEntryKind kind,
            string label)
        {
            ProgramCatalogEntry found = null;
            for (int i = 0; i < semanticIr.References.Count; i++)
            {
                ProgramReference reference = semanticIr.References[i];
                if (!reference.HasSourceOperation || !reference.SourceOperation.Equals(operation) ||
                    reference.Kind != ProgramReferenceKind.CatalogEntry ||
                    reference.TargetIndex < 0 || reference.TargetIndex >= semanticIr.CatalogEntries.Count)
                {
                    continue;
                }

                ProgramCatalogEntry candidate = semanticIr.CatalogEntries[reference.TargetIndex];
                if (candidate.Kind != kind)
                    continue;
                if (found != null)
                    throw new InvalidDataException($"Operation '{operation}' has multiple {label} catalogs.");
                found = candidate;
            }
            return found ?? throw new InvalidDataException($"Operation '{operation}' has no {label} catalog.");
        }

        static ProgramCatalogEntry RequireCatalog(
            CharacterGameplaySemanticIr semanticIr,
            ProgramCatalogEntryKind kind,
            string identity)
        {
            ProgramCatalogEntry found = null;
            for (int i = 0; i < semanticIr.CatalogEntries.Count; i++)
            {
                ProgramCatalogEntry candidate = semanticIr.CatalogEntries[i];
                if (candidate.Kind != kind || !string.Equals(candidate.Identity, identity, StringComparison.Ordinal))
                    continue;
                if (found != null)
                    throw new InvalidDataException($"Catalog identity '{identity}' is duplicated for kind '{kind}'.");
                found = candidate;
            }
            return found ?? throw new InvalidDataException($"Catalog identity '{identity}' with kind '{kind}' is missing.");
        }

        static bool ReferencesCatalog(
            CharacterGameplaySemanticIr semanticIr,
            OperationHandle operation,
            int catalogIndex)
        {
            for (int i = 0; i < semanticIr.References.Count; i++)
            {
                ProgramReference reference = semanticIr.References[i];
                if (reference.HasSourceOperation && reference.SourceOperation.Equals(operation) &&
                    reference.Kind == ProgramReferenceKind.CatalogEntry && reference.TargetIndex == catalogIndex)
                {
                    return true;
                }
            }
            return false;
        }

        static ProgramReference RequireSingleReference(
            CharacterGameplaySemanticIr semanticIr,
            OperationHandle operation,
            ProgramReferenceKind kind,
            string label)
        {
            ProgramReference found = null;
            for (int i = 0; i < semanticIr.References.Count; i++)
            {
                ProgramReference reference = semanticIr.References[i];
                if (!reference.HasSourceOperation || !reference.SourceOperation.Equals(operation) || reference.Kind != kind)
                    continue;
                if (found != null)
                    throw new InvalidDataException($"Operation '{operation}' has multiple {label} references.");
                found = reference;
            }
            return found ?? throw new InvalidDataException($"Operation '{operation}' has no {label} reference.");
        }

        static ProgramMotionWarpTranslationMode RequireTranslationMode(
            SemanticOperation operation,
            CharacterGameplaySemanticIr semanticIr,
            ProgramCatalogEntry catalog)
        {
            int value = RequireInt32Literal(semanticIr, catalog, "TranslationMode");
            ProgramMotionWarpTranslationMode mode = (ProgramMotionWarpTranslationMode)value;
            if (value != operation.Integer0 || !Enum.IsDefined(typeof(ProgramMotionWarpTranslationMode), mode))
                throw new InvalidDataException($"MotionWarp operation '{operation.Handle}' translation mode is inconsistent.");
            return mode;
        }

        static ProgramMotionWarpRotationMode RequireRotationMode(
            SemanticOperation operation,
            CharacterGameplaySemanticIr semanticIr,
            ProgramCatalogEntry catalog)
        {
            int value = RequireInt32Literal(semanticIr, catalog, "RotationMode");
            ProgramMotionWarpRotationMode mode = (ProgramMotionWarpRotationMode)value;
            if (value != operation.Integer1 || !Enum.IsDefined(typeof(ProgramMotionWarpRotationMode), mode))
                throw new InvalidDataException($"MotionWarp operation '{operation.Handle}' rotation mode is inconsistent.");
            return mode;
        }

        static int RequireMotionWarpStateLayout(CharacterGameplaySemanticIr semanticIr, SemanticOperation operation)
        {
            if (operation.StateSlots.Count != s_MotionWarpStateSemantics.Length)
                throw new InvalidDataException($"MotionWarp operation '{operation.Handle}' requires exactly {s_MotionWarpStateSemantics.Length} state slots.");
            int start = operation.StateSlots[0];
            for (int i = 0; i < s_MotionWarpStateSemantics.Length; i++)
            {
                int slotIndex = operation.StateSlots[i];
                if (slotIndex != start + i || slotIndex < 0 || slotIndex >= semanticIr.StateDeclarations.Count)
                    throw new InvalidDataException($"MotionWarp operation '{operation.Handle}' state layout is not contiguous.");
                ProgramStateSlot slot = semanticIr.StateDeclarations[slotIndex];
                if (slot.OwnerKind != ProgramStateOwnerKind.MotionModifier ||
                    slot.Semantic != s_MotionWarpStateSemantics[i] ||
                    !string.Equals(slot.OwnerIdentity, $"operation:{operation.Handle.Value}", StringComparison.Ordinal))
                {
                    throw new InvalidDataException($"MotionWarp operation '{operation.Handle}' state slot '{slotIndex}' has the wrong semantic owner.");
                }
            }
            return start;
        }

        static int RequireInt32Literal(
            CharacterGameplaySemanticIr semanticIr,
            ProgramCatalogEntry catalog,
            string name)
        {
            int index = RequireLiteral(semanticIr, catalog, name, SemanticLiteralKind.Int32);
            return semanticIr.Literals[index].Int32;
        }

        static TEnum RequireEnumLiteral<TEnum>(
            CharacterGameplaySemanticIr semanticIr,
            ProgramCatalogEntry catalog,
            string name)
            where TEnum : struct, Enum
        {
            int value = RequireInt32Literal(semanticIr, catalog, name);
            object candidate = Enum.ToObject(typeof(TEnum), value);
            if (!Enum.IsDefined(typeof(TEnum), candidate))
                throw new InvalidDataException($"MotionWarp catalog '{catalog.Identity}' field '{name}' has unknown value '{value}'.");
            return (TEnum)candidate;
        }

        static int OptionalLiteral(
            CharacterGameplaySemanticIr semanticIr,
            ProgramCatalogEntry catalog,
            string name,
            SemanticLiteralKind kind,
            bool required)
        {
            ProgramCatalogField field = FindField(catalog, name);
            if (!required)
            {
                if (field != null)
                    throw new InvalidDataException($"MotionWarp catalog '{catalog.Identity}' contains unconsumed field '{name}'.");
                return -1;
            }
            if (field == null || field.Kind != ProgramCatalogFieldKind.Constant ||
                field.ConstantIndex < 0 || field.ConstantIndex >= semanticIr.Literals.Count ||
                semanticIr.Literals[field.ConstantIndex].Kind != kind)
            {
                throw new InvalidDataException($"MotionWarp catalog '{catalog.Identity}' field '{name}' requires literal kind '{kind}'.");
            }
            return field.ConstantIndex;
        }

        static int RequireLiteral(
            CharacterGameplaySemanticIr semanticIr,
            ProgramCatalogEntry catalog,
            string name,
            SemanticLiteralKind kind)
        {
            ProgramCatalogField field = RequireField(catalog, name, ProgramCatalogFieldKind.Constant);
            if (field.ConstantIndex < 0 || field.ConstantIndex >= semanticIr.Literals.Count ||
                semanticIr.Literals[field.ConstantIndex].Kind != kind)
            {
                throw new InvalidDataException($"MotionWarp catalog '{catalog.Identity}' field '{name}' requires literal kind '{kind}'.");
            }
            return field.ConstantIndex;
        }

        static string RequireIdentity(ProgramCatalogEntry catalog, string name)
        {
            return RequireField(catalog, name, ProgramCatalogFieldKind.Identity).Identity;
        }

        static ProgramCatalogField RequireField(
            ProgramCatalogEntry catalog,
            string name,
            ProgramCatalogFieldKind kind)
        {
            ProgramCatalogField found = FindField(catalog, name);
            if (found == null || found.Kind != kind)
                throw new InvalidDataException($"MotionWarp catalog '{catalog.Identity}' is missing field '{name}' with kind '{kind}'.");
            return found;
        }

        static ProgramCatalogField FindField(ProgramCatalogEntry catalog, string name)
        {
            ProgramCatalogField found = null;
            for (int i = 0; i < catalog.Fields.Count; i++)
            {
                ProgramCatalogField field = catalog.Fields[i];
                if (!string.Equals(field.Name, name, StringComparison.Ordinal))
                    continue;
                if (found != null)
                    throw new InvalidDataException($"MotionWarp catalog '{catalog.Identity}' field '{name}' is ambiguous.");
                found = field;
            }
            return found;
        }
    }
}
