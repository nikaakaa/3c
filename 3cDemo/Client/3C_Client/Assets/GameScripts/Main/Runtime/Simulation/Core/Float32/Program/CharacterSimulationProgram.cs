using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ThirdPersonSimulation
{
    public enum ProgramConstantKind : byte
    {
        Boolean = 1,
        Int32 = 2,
        UInt64 = 3,
        Scalar = 4,
        Vector2 = 5,
        Vector3 = 6,
        Yaw = 7,
        String = 8,
        Bytes = 9
    }

    public sealed class ProgramConstant
    {
        readonly byte[] m_Bytes;

        ProgramConstant(int index, string identity, ProgramConstantKind kind, bool boolean, int int32, ulong uint64, Float32Scalar scalar, Float32Vector2 vector2, Float32Vector3 vector3, Float32Yaw yaw, string text, byte[] bytes)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            Index = index;
            Identity = SimulationIdentity.Require(identity, nameof(identity));
            Kind = kind;
            Boolean = boolean;
            Int32 = int32;
            UInt64 = uint64;
            Scalar = scalar;
            Vector2 = vector2;
            Vector3 = vector3;
            Yaw = yaw;
            Text = text ?? string.Empty;
            m_Bytes = bytes == null ? Array.Empty<byte>() : (byte[])bytes.Clone();
        }

        public int Index { get; }
        public string Identity { get; }
        public ProgramConstantKind Kind { get; }
        public bool Boolean { get; }
        public int Int32 { get; }
        public ulong UInt64 { get; }
        public Float32Scalar Scalar { get; }
        public Float32Vector2 Vector2 { get; }
        public Float32Vector3 Vector3 { get; }
        public Float32Yaw Yaw { get; }
        public string Text { get; }
        public ReadOnlyMemory<byte> Bytes => m_Bytes;
        public static ProgramConstant FromBoolean(int index, string identity, bool value) => new ProgramConstant(index, identity, ProgramConstantKind.Boolean, value, default, default, default, default, default, default, null, null);
        public static ProgramConstant FromInt32(int index, string identity, int value) => new ProgramConstant(index, identity, ProgramConstantKind.Int32, default, value, default, default, default, default, default, null, null);
        public static ProgramConstant FromUInt64(int index, string identity, ulong value) => new ProgramConstant(index, identity, ProgramConstantKind.UInt64, default, default, value, default, default, default, default, null, null);
        public static ProgramConstant FromScalar(int index, string identity, Float32Scalar value) => new ProgramConstant(index, identity, ProgramConstantKind.Scalar, default, default, default, value, default, default, default, null, null);
        public static ProgramConstant FromVector2(int index, string identity, Float32Vector2 value) => new ProgramConstant(index, identity, ProgramConstantKind.Vector2, default, default, default, default, value, default, default, null, null);
        public static ProgramConstant FromVector3(int index, string identity, Float32Vector3 value) => new ProgramConstant(index, identity, ProgramConstantKind.Vector3, default, default, default, default, default, value, default, null, null);
        public static ProgramConstant FromYaw(int index, string identity, Float32Yaw value) => new ProgramConstant(index, identity, ProgramConstantKind.Yaw, default, default, default, default, default, default, value, null, null);
        public static ProgramConstant FromString(int index, string identity, string value) => new ProgramConstant(index, identity, ProgramConstantKind.String, default, default, default, default, default, default, default, value, null);
        public static ProgramConstant FromBytes(int index, string identity, byte[] value) => new ProgramConstant(index, identity, ProgramConstantKind.Bytes, default, default, default, default, default, default, default, null, value);
    }

    public sealed class SimulationOperationDefinition
    {
        readonly ReadOnlyCollection<int> m_ConstantReferences;

        public SimulationOperationDefinition(
            int index,
            string identity,
            SimulationOperationCode code,
            IEnumerable<int> constantReferences,
            int integer0,
            int integer1,
            ulong unsigned0,
            Float32Scalar scalar0,
            string text0,
            uint flags)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            Index = index;
            Identity = SimulationIdentity.Require(identity, nameof(identity));
            Code = code;
            m_ConstantReferences = ReadOnly(constantReferences);
            Integer0 = integer0;
            Integer1 = integer1;
            Unsigned0 = unsigned0;
            Scalar0 = scalar0;
            Text0 = text0 ?? string.Empty;
            Flags = flags;
        }

        public int Index { get; }
        public string Identity { get; }
        public SimulationOperationCode Code { get; }
        public IReadOnlyList<int> ConstantReferences => m_ConstantReferences;
        public int Integer0 { get; }
        public int Integer1 { get; }
        public ulong Unsigned0 { get; }
        public Float32Scalar Scalar0 { get; }
        public string Text0 { get; }
        public uint Flags { get; }

        static ReadOnlyCollection<int> ReadOnly(IEnumerable<int> source)
        {
            var values = source == null ? new List<int>() : new List<int>(source);
            return values.AsReadOnly();
        }
    }

    public sealed class SimulationOperation
    {
        readonly ReadOnlyCollection<int> m_Operands;
        readonly ReadOnlyCollection<int> m_StateSlots;

        public SimulationOperation(
            OperationHandle handle,
            SimulationOperationDefinition definition,
            IEnumerable<int> operands,
            IEnumerable<int> stateSlots)
        {
            if (!handle.IsValid)
                throw new ArgumentException("Operation handle is invalid.", nameof(handle));
            Handle = handle;
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            m_Operands = ReadOnly(operands);
            m_StateSlots = ReadOnly(stateSlots);
        }

        public OperationHandle Handle { get; }
        public SimulationOperationDefinition Definition { get; }
        public int DefinitionIndex => Definition.Index;
        public SimulationOperationCode Code => Definition.Code;
        public IReadOnlyList<int> Operands => m_Operands;
        public IReadOnlyList<int> ConstantReferences => Definition.ConstantReferences;
        public IReadOnlyList<int> StateSlots => m_StateSlots;
        public int Integer0 => Definition.Integer0;
        public int Integer1 => Definition.Integer1;
        public ulong Unsigned0 => Definition.Unsigned0;
        public Float32Scalar Scalar0 => Definition.Scalar0;
        public string Text0 => Definition.Text0;
        public uint Flags => Definition.Flags;

        static ReadOnlyCollection<int> ReadOnly(IEnumerable<int> source)
        {
            var values = source == null ? new List<int>() : new List<int>(source);
            return values.AsReadOnly();
        }
    }

    public sealed class ProgramBodyMotionDescriptor
    {
        public ProgramBodyMotionDescriptor(
            string sourceIdentity,
            StableHash contentRevision,
            int semanticVersion,
            Float32Scalar gravityAcceleration,
            Float32Scalar maximumFallSpeed)
        {
            SourceIdentity = SimulationIdentity.Require(sourceIdentity, nameof(sourceIdentity));
            if (!contentRevision.IsValid)
                throw new ArgumentException("Body Motion content revision is required.", nameof(contentRevision));
            if (semanticVersion != 1)
                throw new ArgumentOutOfRangeException(nameof(semanticVersion), "Body Motion semantic version is unsupported.");
            if (gravityAcceleration >= Float32Scalar.Zero)
                throw new ArgumentOutOfRangeException(nameof(gravityAcceleration));
            if (maximumFallSpeed <= Float32Scalar.Zero)
                throw new ArgumentOutOfRangeException(nameof(maximumFallSpeed));
            ContentRevision = contentRevision;
            SemanticVersion = semanticVersion;
            GravityAcceleration = gravityAcceleration;
            MaximumFallSpeed = maximumFallSpeed;
        }

        public string SourceIdentity { get; }
        public StableHash ContentRevision { get; }
        public int SemanticVersion { get; }
        public Float32Scalar GravityAcceleration { get; }
        public Float32Scalar MaximumFallSpeed { get; }
    }

    public sealed class CharacterSimulationProgramManifest
    {
        public CharacterSimulationProgramManifest(
            ProgramId programId,
            string compilerVersion,
            OperationSetVersion operationSetVersion,
            int tickRate,
            ProgramRevision sourceRevision,
            SemanticHash semanticHash,
            SimulationNumericProfile numericProfile,
            ProgramCapabilityManifest capabilities)
        {
            if (!programId.IsValid || !operationSetVersion.IsValid || tickRate <= 0 || string.IsNullOrEmpty(sourceRevision.Value) || !semanticHash.IsValid)
                throw new ArgumentException("Program manifest is incomplete.");
            CharacterGameplayOperationSet.RequireVersion(operationSetVersion);
            if (numericProfile != Float32SimulationNumericProfile.Value)
                throw new ArgumentException($"Numeric Profile '{numericProfile.Id}' is not installed by this runtime.", nameof(numericProfile));
            ProgramId = programId;
            CompilerVersion = SimulationIdentity.Require(compilerVersion, nameof(compilerVersion));
            OperationSetVersion = operationSetVersion;
            TickRate = tickRate;
            SourceRevision = sourceRevision;
            SemanticHash = semanticHash;
            NumericProfile = numericProfile;
            Capabilities = capabilities ?? throw new ArgumentNullException(nameof(capabilities));
        }
        public ProgramId ProgramId { get; }
        public string CompilerVersion { get; }
        public OperationSetVersion OperationSetVersion { get; }
        public int TickRate { get; }
        public ProgramRevision SourceRevision { get; }
        public SemanticHash SemanticHash { get; }
        public SimulationNumericProfile NumericProfile { get; }
        public ProgramCapabilityManifest Capabilities { get; }
    }

    public sealed class CharacterSimulationProgram
    {
        readonly ReadOnlyCollection<SimulationOperationDefinition> m_OperationDefinitions;
        readonly ReadOnlyCollection<SimulationOperation> m_Operations;
        readonly ReadOnlyCollection<ProgramConstant> m_Constants;
        readonly ReadOnlyCollection<ProgramConstantInputBinding> m_ConstantInputBindings;
        readonly ReadOnlyCollection<ProgramControlFlowEdge> m_ControlFlow;
        readonly ReadOnlyCollection<ProgramReference> m_References;
        readonly ReadOnlyCollection<ProgramStateSlot> m_StateSlots;
        readonly ReadOnlyCollection<ProgramScopeLayout> m_Scopes;
        readonly ReadOnlyCollection<ProgramWorldRequestLayout> m_WorldRequests;
        readonly ReadOnlyCollection<ProgramOutputChannelLayout> m_OutputChannels;
        readonly ReadOnlyCollection<ProgramSourceMapEntry> m_SourceMap;
        readonly ReadOnlyCollection<ProgramProducer> m_Producers;
        readonly ReadOnlyCollection<ProgramCatalogEntry> m_CatalogEntries;
        readonly ReadOnlyCollection<ProgramMotionModifierDescriptor> m_MotionModifiers;

        public CharacterSimulationProgram(
            CharacterSimulationProgramManifest manifest,
            ProgramBodyMotionDescriptor bodyMotion,
            IEnumerable<SimulationOperationDefinition> operationDefinitions,
            IEnumerable<SimulationOperation> operations,
            IEnumerable<ProgramConstant> constants,
            IEnumerable<ProgramConstantInputBinding> constantInputBindings,
            IEnumerable<ProgramControlFlowEdge> controlFlow,
            IEnumerable<ProgramReference> references,
            IEnumerable<ProgramStateSlot> stateSlots,
            IEnumerable<ProgramScopeLayout> scopes,
            IEnumerable<ProgramWorldRequestLayout> worldRequests,
            IEnumerable<ProgramOutputChannelLayout> outputChannels,
            IEnumerable<ProgramCatalogEntry> catalogEntries,
            IEnumerable<ProgramMotionModifierDescriptor> motionModifiers,
            IEnumerable<ProgramSourceMapEntry> sourceMap,
            IEnumerable<ProgramProducer> producers)
        {
            Manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
            BodyMotion = bodyMotion ?? throw new ArgumentNullException(nameof(bodyMotion));
            if ((Manifest.Capabilities.RequiredWorldCapabilities & WorldCapability.AirborneVerticalMotion) == 0)
                throw new ArgumentException("Program Body Motion requires AirborneVerticalMotion capability.", nameof(manifest));
            m_OperationDefinitions = SortIndexed(operationDefinitions, value => value.Index, "operation definition");
            m_Operations = SortIndexed(operations, value => value.Handle.Value, "operation");
            m_Constants = SortIndexed(constants, value => value.Index, "constant");
            m_ConstantInputBindings = SortConstantInputs(constantInputBindings, m_Operations);
            m_StateSlots = SortIndexed(stateSlots, value => value.Index, "state slot");
            m_WorldRequests = SortIndexed(worldRequests, value => value.Index, "world request");
            m_OutputChannels = SortIndexed(outputChannels, value => value.Index, "output channel");
            m_CatalogEntries = SortIndexed(catalogEntries, value => value.Index, "catalog entry");
            m_MotionModifiers = SortIndexed(motionModifiers, value => value.Index, "motion modifier");
            m_Producers = SortIndexed(producers, value => value.Index, "producer");
            m_ControlFlow = SortByIdentity(controlFlow, value => value.Identity, "control-flow edge");
            m_References = SortByIdentity(references, value => value.Identity, "reference");
            m_Scopes = SortByIdentity(scopes, value => value.Identity, "scope");
            m_SourceMap = SortSourceMap(sourceMap);
            ValidateReferences();
            LayoutHash = CharacterSimulationProgramCodec.ComputeLayoutHash(this);
            ProgramHash = CharacterSimulationProgramCodec.ComputeProgramHash(this);
        }

        public CharacterSimulationProgramManifest Manifest { get; }
        public ProgramBodyMotionDescriptor BodyMotion { get; }
        public IReadOnlyList<SimulationOperationDefinition> OperationDefinitions => m_OperationDefinitions;
        public IReadOnlyList<SimulationOperation> Operations => m_Operations;
        public IReadOnlyList<ProgramConstant> Constants => m_Constants;
        public IReadOnlyList<ProgramConstantInputBinding> ConstantInputBindings => m_ConstantInputBindings;
        public IReadOnlyList<ProgramControlFlowEdge> ControlFlow => m_ControlFlow;
        public IReadOnlyList<ProgramReference> References => m_References;
        public IReadOnlyList<ProgramStateSlot> StateSlots => m_StateSlots;
        public IReadOnlyList<ProgramScopeLayout> Scopes => m_Scopes;
        public IReadOnlyList<ProgramWorldRequestLayout> WorldRequests => m_WorldRequests;
        public IReadOnlyList<ProgramOutputChannelLayout> OutputChannels => m_OutputChannels;
        public IReadOnlyList<ProgramCatalogEntry> CatalogEntries => m_CatalogEntries;
        public IReadOnlyList<ProgramMotionModifierDescriptor> MotionModifiers => m_MotionModifiers;
        public IReadOnlyList<ProgramSourceMapEntry> SourceMap => m_SourceMap;
        public IReadOnlyList<ProgramProducer> Producers => m_Producers;
        public LayoutHash LayoutHash { get; }
        public ProgramHash ProgramHash { get; }

        void ValidateReferences()
        {
            var definitionIdentities = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < m_OperationDefinitions.Count; i++)
            {
                SimulationOperationDefinition definition = m_OperationDefinitions[i];
                if (!definitionIdentities.Add(definition.Identity))
                    throw new ArgumentException($"Operation definition identity '{definition.Identity}' is duplicated.");
                CharacterGameplayOperationSet.RequireOperation(definition.Code);
                ValidateIndexes(definition.ConstantReferences, m_Constants.Count, $"operation definition {i} constant");
            }
            for (int i = 0; i < m_Operations.Count; i++)
            {
                SimulationOperation operation = m_Operations[i];
                RequireIndex(operation.DefinitionIndex, m_OperationDefinitions.Count, $"operation {i} definition");
                if (!ReferenceEquals(operation.Definition, m_OperationDefinitions[operation.DefinitionIndex]))
                    throw new ArgumentException($"operation {i} does not reference the Program-owned definition instance.");
                ValidateIndexes(operation.Operands, m_Operations.Count, $"operation {i} operand");
                ValidateIndexes(operation.StateSlots, m_StateSlots.Count, $"operation {i} state slot");
            }
            for (int i = 0; i < m_ControlFlow.Count; i++)
            {
                ProgramControlFlowEdge edge = m_ControlFlow[i];
                RequireIndex(edge.Source.Value, m_Operations.Count, $"edge '{edge.Identity}' source");
                RequireIndex(edge.Target.Value, m_Operations.Count, $"edge '{edge.Identity}' target");
                if (edge.HasCondition)
                    RequireIndex(edge.Condition.Value, m_Operations.Count, $"edge '{edge.Identity}' condition");
            }
            var valueSources = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < m_ControlFlow.Count; i++)
            {
                ProgramControlFlowEdge edge = m_ControlFlow[i];
                if (edge.Kind != ProgramControlFlowKind.Value)
                    continue;
                string key = edge.Target.Value.ToString() + ":" + edge.TargetPort;
                if (!valueSources.Add(key))
                    throw new ArgumentException($"Operation '{edge.Target}' input port '{edge.TargetPort}' has multiple linked Value sources.");
                CharacterGameplayValuePortContracts.Require(m_Operations[edge.Source.Value].Code).RequireOutput(edge.SourcePort);
                CharacterGameplayValuePortContracts.Require(m_Operations[edge.Target.Value].Code).RequireInput(edge.TargetPort);
            }
            for (int i = 0; i < m_ConstantInputBindings.Count; i++)
            {
                ProgramConstantInputBinding binding = m_ConstantInputBindings[i];
                RequireIndex(binding.TargetOperation.Value, m_Operations.Count, "constant input operation");
                RequireIndex(binding.ConstantIndex, m_Constants.Count, "constant input constant");
                OperationValuePortDefinition port = CharacterGameplayValuePortContracts.Require(m_Operations[binding.TargetOperation.Value].Code).RequireInput(binding.TargetPort);
                if (!port.Accepts(binding.ResolvedValueKind) || ConstantKind(m_Constants[binding.ConstantIndex].Kind) != binding.ResolvedValueKind)
                    throw new ArgumentException($"Constant input '{binding.TargetOperation}/{binding.TargetPort}' has incompatible kind '{binding.ResolvedValueKind}'.");
                string key = binding.TargetOperation.Value.ToString() + ":" + binding.TargetPort;
                if (!valueSources.Add(key))
                    throw new ArgumentException($"Operation '{binding.TargetOperation}' input port '{binding.TargetPort}' has multiple Value sources.");
            }
            for (int i = 0; i < m_References.Count; i++)
            {
                ProgramReference reference = m_References[i];
                if (reference.HasSourceOperation)
                    RequireIndex(reference.SourceOperation.Value, m_Operations.Count, $"reference '{reference.Identity}' source operation");
                int count = reference.Kind switch
                {
                    ProgramReferenceKind.Operation => m_Operations.Count,
                    ProgramReferenceKind.Constant => m_Constants.Count,
                    ProgramReferenceKind.StateSlot => m_StateSlots.Count,
                    ProgramReferenceKind.Scope => m_Scopes.Count,
                    ProgramReferenceKind.WorldRequest => m_WorldRequests.Count,
                    ProgramReferenceKind.OutputChannel => m_OutputChannels.Count,
                    ProgramReferenceKind.Producer => m_Producers.Count,
                    ProgramReferenceKind.CatalogEntry => m_CatalogEntries.Count,
                    ProgramReferenceKind.MotionSourceOperation => m_Operations.Count,
                    _ => throw new ArgumentOutOfRangeException(nameof(reference.Kind))
                };
                RequireIndex(reference.TargetIndex, count, $"reference '{reference.Identity}' target");
            }
            for (int i = 0; i < m_StateSlots.Count; i++)
            {
                int defaultIndex = m_StateSlots[i].DefaultConstantIndex;
                if (defaultIndex >= 0)
                    RequireIndex(defaultIndex, m_Constants.Count, $"state slot {i} default");
            }
            for (int i = 0; i < m_Scopes.Count; i++)
                ValidateIndexes(m_Scopes[i].StateSlots, m_StateSlots.Count, $"scope '{m_Scopes[i].Identity}' state slot");
            for (int i = 0; i < m_CatalogEntries.Count; i++)
            {
                ProgramCatalogEntry entry = m_CatalogEntries[i];
                for (int fieldIndex = 0; fieldIndex < entry.Fields.Count; fieldIndex++)
                {
                    ProgramCatalogField field = entry.Fields[fieldIndex];
                    if (field.Kind == ProgramCatalogFieldKind.Constant)
                        RequireIndex(field.ConstantIndex, m_Constants.Count, $"catalog entry '{entry.Identity}' field '{field.Name}'");
                }
            }
            ValidateMotionModifiers();
        }

        void ValidateMotionModifiers()
        {
            var operations = new HashSet<int>();
            for (int i = 0; i < m_MotionModifiers.Count; i++)
            {
                ProgramMotionModifierDescriptor descriptor = m_MotionModifiers[i];
                RequireIndex(descriptor.Operation.Value, m_Operations.Count, $"motion modifier {i} operation");
                RequireIndex(descriptor.SourceMotionOperation.Value, m_Operations.Count, $"motion modifier {i} source");
                RequireIndex(descriptor.TimelineOwnerOperation.Value, m_Operations.Count, $"motion modifier {i} timeline owner");
                RequireIndex(descriptor.CatalogEntryIndex, m_CatalogEntries.Count, $"motion modifier {i} catalog");
                if (!operations.Add(descriptor.Operation.Value) ||
                    m_Operations[descriptor.Operation.Value].Definition.Code != SimulationOperationCode.TimelineMotionWarp ||
                    m_Operations[descriptor.SourceMotionOperation.Value].Definition.Code != SimulationOperationCode.TimelineMotionCurve ||
                    m_Operations[descriptor.TimelineOwnerOperation.Value].Definition.Code != SimulationOperationCode.Timeline ||
                    m_CatalogEntries[descriptor.CatalogEntryIndex].Kind != ProgramCatalogEntryKind.TimelineClip)
                {
                    throw new ArgumentException($"Motion modifier '{i}' topology is invalid.");
                }
                if (descriptor.StateSlotStart < 0 ||
                    descriptor.StateSlotStart + descriptor.StateSlotCount > m_StateSlots.Count ||
                    descriptor.StateSlotCount != ProgramMotionModifierDescriptor.MotionWarpStateSlotCount)
                {
                    throw new ArgumentException($"Motion modifier '{i}' state range is invalid.");
                }
                ValidateModifierConstant(descriptor.TargetLocalPlanarOffsetConstantIndex, ProgramConstantKind.Vector2, i, "target offset");
                ValidateModifierConstant(descriptor.TargetYawOffsetConstantIndex, ProgramConstantKind.Scalar, i, "target yaw offset");
                ValidateModifierConstant(descriptor.PositionWeightConstantIndex, ProgramConstantKind.Scalar, i, "position weight");
                ValidateModifierConstant(descriptor.YawWeightConstantIndex, ProgramConstantKind.Scalar, i, "yaw weight");
                ValidateModifierConstant(descriptor.MaximumPositionCorrectionConstantIndex, ProgramConstantKind.Scalar, i, "position clamp");
                ValidateModifierConstant(descriptor.MaximumYawCorrectionConstantIndex, ProgramConstantKind.Scalar, i, "yaw clamp");
                ValidateModifierConstant(descriptor.PositionProgressCurveConstantIndex, ProgramConstantKind.Bytes, i, "position progress curve");
                ValidateModifierConstant(descriptor.YawProgressCurveConstantIndex, ProgramConstantKind.Bytes, i, "yaw progress curve");
            }
        }

        void ValidateModifierConstant(int index, ProgramConstantKind kind, int descriptorIndex, string label)
        {
            RequireIndex(index, m_Constants.Count, $"motion modifier {descriptorIndex} {label}");
            if (m_Constants[index].Kind != kind)
                throw new ArgumentException($"Motion modifier '{descriptorIndex}' {label} constant has kind '{m_Constants[index].Kind}', expected '{kind}'.");
        }

        static void ValidateIndexes(IReadOnlyList<int> indexes, int count, string label)
        {
            for (int i = 0; i < indexes.Count; i++)
                RequireIndex(indexes[i], count, label);
        }

        static void RequireIndex(int index, int count, string label)
        {
            if (index < 0 || index >= count)
                throw new ArgumentException($"{label} index '{index}' is outside 0..{count - 1}.");
        }

        static ReadOnlyCollection<T> SortIndexed<T>(IEnumerable<T> source, Func<T, int> index, string label) where T : class
        {
            var values = source == null ? new List<T>() : new List<T>(source);
            RequireNoNull(values, nameof(source));
            values.Sort((left, right) => index(left).CompareTo(index(right)));
            for (int i = 0; i < values.Count; i++)
            {
                if (index(values[i]) != i)
                    throw new ArgumentException($"Program {label} indexes must be contiguous from zero.", nameof(source));
            }
            return values.AsReadOnly();
        }

        static ReadOnlyCollection<T> SortByIdentity<T>(IEnumerable<T> source, Func<T, string> identity, string label) where T : class
        {
            var values = source == null ? new List<T>() : new List<T>(source);
            RequireNoNull(values, nameof(source));
            values.Sort((left, right) => string.CompareOrdinal(identity(left), identity(right)));
            for (int i = 0; i < values.Count; i++)
            {
                if (i > 0 && string.Equals(identity(values[i - 1]), identity(values[i]), StringComparison.Ordinal))
                    throw new ArgumentException($"Program {label} identities must be unique.", nameof(source));
            }
            return values.AsReadOnly();
        }

        static ReadOnlyCollection<ProgramConstantInputBinding> SortConstantInputs(
            IEnumerable<ProgramConstantInputBinding> source,
            IReadOnlyList<SimulationOperation> operations)
        {
            var values = new List<ProgramConstantInputBinding>(source ?? Array.Empty<ProgramConstantInputBinding>());
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
                    throw new ArgumentException("Constant input binding table contains null.", nameof(source));
                if (i > 0 && values[i - 1].TargetOperation.Equals(values[i].TargetOperation) && string.Equals(values[i - 1].TargetPort, values[i].TargetPort, StringComparison.Ordinal))
                    throw new ArgumentException($"Constant input binding '{values[i].TargetOperation}/{values[i].TargetPort}' is duplicated.", nameof(source));
            }
            return values.AsReadOnly();
        }

        static SemanticValueKind ConstantKind(ProgramConstantKind kind)
        {
            return kind switch
            {
                ProgramConstantKind.Boolean => SemanticValueKind.Boolean,
                ProgramConstantKind.Int32 => SemanticValueKind.Int32,
                ProgramConstantKind.UInt64 => SemanticValueKind.UInt64,
                ProgramConstantKind.Scalar => SemanticValueKind.Number,
                ProgramConstantKind.Vector2 => SemanticValueKind.Vector2,
                ProgramConstantKind.Vector3 => SemanticValueKind.Vector3,
                ProgramConstantKind.Yaw => SemanticValueKind.Yaw,
                ProgramConstantKind.String => SemanticValueKind.Identity,
                _ => throw new ArgumentException($"Program constant kind '{kind}' cannot enter the Value graph.")
            };
        }

        static ReadOnlyCollection<ProgramSourceMapEntry> SortSourceMap(IEnumerable<ProgramSourceMapEntry> source)
        {
            var values = source == null ? new List<ProgramSourceMapEntry>() : new List<ProgramSourceMapEntry>(source);
            RequireNoNull(values, nameof(source));
            values.Sort((left, right) =>
            {
                int byKind = left.TargetKind.CompareTo(right.TargetKind);
                if (byKind != 0)
                    return byKind;
                int byIndex = left.TargetIndex.CompareTo(right.TargetIndex);
                if (byIndex != 0)
                    return byIndex;
                int byGraph = string.CompareOrdinal(left.GraphId, right.GraphId);
                return byGraph != 0 ? byGraph : string.CompareOrdinal(left.NodeId, right.NodeId);
            });
            return values.AsReadOnly();
        }

        static void RequireNoNull<T>(IReadOnlyList<T> values, string parameterName) where T : class
        {
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i] == null)
                    throw new ArgumentException("Program collections cannot contain null values.", parameterName);
            }
        }
    }
}
