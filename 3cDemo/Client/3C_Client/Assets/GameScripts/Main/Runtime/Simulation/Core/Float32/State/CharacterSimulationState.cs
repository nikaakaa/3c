using System;
using System.Collections.Generic;
using System.IO;

namespace ThirdPersonSimulation
{
    public readonly struct CharacterStateValue
    {
        readonly Float32InputRequestState m_InputRequest;
        readonly Float32ActionActivationRequestState m_ActionActivationRequest;
        readonly Float32ActionInstanceState m_ActionInstance;
        readonly Float32ActionInstanceReference m_ActionInstanceReference;
        readonly GameplayEffectStateAggregate m_GameplayEffectAggregate;
        readonly BlackboardOwnerToken m_BlackboardOwnerToken;
        readonly BlackboardWriteStamp m_BlackboardWriteStamp;

        CharacterStateValue(
            ProgramStateValueKind kind,
            bool boolean,
            int int32,
            ulong uint64,
            Float32Scalar scalar,
            Float32Vector2 vector2,
            Float32Vector3 vector3,
            Float32Yaw yaw,
            string identity,
            BlackboardOwnerToken blackboardOwnerToken,
            BlackboardWriteStamp blackboardWriteStamp,
            Float32InputRequestState inputRequest,
            Float32ActionActivationRequestState actionActivationRequest,
            Float32ActionInstanceState actionInstance,
            Float32ActionInstanceReference actionInstanceReference,
            SimulationActionTargetSnapshot actionTargetSnapshot,
            GameplayEffectStateAggregate gameplayEffectAggregate)
        {
            Kind = kind;
            Boolean = boolean;
            Int32 = int32;
            UInt64 = uint64;
            Scalar = scalar;
            Vector2 = vector2;
            Vector3 = vector3;
            Yaw = yaw;
            Identity = identity ?? string.Empty;
            m_BlackboardOwnerToken = blackboardOwnerToken;
            m_BlackboardWriteStamp = blackboardWriteStamp;
            m_InputRequest = inputRequest;
            m_ActionActivationRequest = actionActivationRequest;
            m_ActionInstance = actionInstance;
            m_ActionInstanceReference = actionInstanceReference;
            ActionTargetSnapshot = actionTargetSnapshot;
            m_GameplayEffectAggregate = gameplayEffectAggregate;
        }

        public ProgramStateValueKind Kind { get; }
        public bool Boolean { get; }
        public int Int32 { get; }
        public ulong UInt64 { get; }
        public Float32Scalar Scalar { get; }
        public Float32Vector2 Vector2 { get; }
        public Float32Vector3 Vector3 { get; }
        public Float32Yaw Yaw { get; }
        public string Identity { get; }
        public BlackboardOwnerToken BlackboardOwnerToken => Require(ProgramStateValueKind.BlackboardOwnerToken, m_BlackboardOwnerToken);
        public BlackboardWriteStamp BlackboardWriteStamp => Require(ProgramStateValueKind.BlackboardWriteStamp, m_BlackboardWriteStamp);
        public SimulationActionTargetSnapshot ActionTargetSnapshot { get; }
        internal Float32InputRequestState InputRequest => Require(ProgramStateValueKind.InputRequest, m_InputRequest);
        internal Float32ActionActivationRequestState ActionActivationRequest => Require(ProgramStateValueKind.ActionActivationRequest, m_ActionActivationRequest);
        internal Float32ActionInstanceState ActionInstance => Require(ProgramStateValueKind.ActionInstance, m_ActionInstance);
        internal Float32ActionInstanceReference ActionInstanceReference => Require(ProgramStateValueKind.ActionInstanceReference, m_ActionInstanceReference);
        internal GameplayEffectStateAggregate GameplayEffectAggregate =>
            Kind == ProgramStateValueKind.GameplayEffectAggregate
                ? m_GameplayEffectAggregate ?? throw new InvalidOperationException("Gameplay Effect state aggregate is missing.")
                : throw new InvalidOperationException($"State value is '{Kind}', expected GameplayEffectAggregate.");

        public static CharacterStateValue FromBoolean(bool value) => Create(ProgramStateValueKind.Boolean, boolean: value);
        public static CharacterStateValue FromInt32(int value) => Create(ProgramStateValueKind.Int32, int32: value);
        public static CharacterStateValue FromUInt64(ulong value) => Create(ProgramStateValueKind.UInt64, uint64: value);
        public static CharacterStateValue FromScalar(Float32Scalar value) => Create(ProgramStateValueKind.Scalar, scalar: value);
        public static CharacterStateValue FromVector2(Float32Vector2 value) => Create(ProgramStateValueKind.Vector2, vector2: value);
        public static CharacterStateValue FromVector3(Float32Vector3 value) => Create(ProgramStateValueKind.Vector3, vector3: value);
        public static CharacterStateValue FromYaw(Float32Yaw value) => Create(ProgramStateValueKind.Yaw, yaw: value);
        public static CharacterStateValue FromIdentity(string value) => Create(ProgramStateValueKind.Identity, identity: value);
        public static CharacterStateValue FromBlackboardOwnerToken(BlackboardOwnerToken value) => Create(ProgramStateValueKind.BlackboardOwnerToken, blackboardOwnerToken: value);
        public static CharacterStateValue FromBlackboardWriteStamp(BlackboardWriteStamp value) => Create(ProgramStateValueKind.BlackboardWriteStamp, blackboardWriteStamp: value);
        internal static CharacterStateValue FromInputRequest(Float32InputRequestState value) => Create(ProgramStateValueKind.InputRequest, inputRequest: value);
        internal static CharacterStateValue FromActionActivationRequest(Float32ActionActivationRequestState value) => Create(ProgramStateValueKind.ActionActivationRequest, actionActivationRequest: value);
        internal static CharacterStateValue FromActionInstance(Float32ActionInstanceState value) => Create(ProgramStateValueKind.ActionInstance, actionInstance: value);
        internal static CharacterStateValue FromActionInstanceReference(Float32ActionInstanceReference value) => Create(ProgramStateValueKind.ActionInstanceReference, actionInstanceReference: value);
        public static CharacterStateValue FromActionTargetSnapshot(SimulationActionTargetSnapshot value) => Create(ProgramStateValueKind.ActionTargetSnapshot, actionTargetSnapshot: value);
        internal static CharacterStateValue FromGameplayEffectAggregate(GameplayEffectStateAggregate value)
        {
            return Create(
                ProgramStateValueKind.GameplayEffectAggregate,
                gameplayEffectAggregate: value ?? throw new ArgumentNullException(nameof(value)));
        }

        public static CharacterStateValue Default(ProgramStateValueKind kind)
        {
            return kind switch
            {
                ProgramStateValueKind.Boolean => FromBoolean(false),
                ProgramStateValueKind.Int32 => FromInt32(0),
                ProgramStateValueKind.UInt64 => FromUInt64(0),
                ProgramStateValueKind.Scalar => FromScalar(Float32Scalar.Zero),
                ProgramStateValueKind.Vector2 => FromVector2(Float32Vector2.Zero),
                ProgramStateValueKind.Vector3 => FromVector3(Float32Vector3.Zero),
                ProgramStateValueKind.Yaw => FromYaw(Float32Yaw.Zero),
                ProgramStateValueKind.Identity => FromIdentity(string.Empty),
                ProgramStateValueKind.BlackboardOwnerToken => FromBlackboardOwnerToken(default),
                ProgramStateValueKind.BlackboardWriteStamp => FromBlackboardWriteStamp(default),
                ProgramStateValueKind.InputRequest => FromInputRequest(default),
                ProgramStateValueKind.ActionActivationRequest => FromActionActivationRequest(default),
                ProgramStateValueKind.ActionInstance => FromActionInstance(default),
                ProgramStateValueKind.ActionInstanceReference => FromActionInstanceReference(default),
                ProgramStateValueKind.ActionTargetSnapshot => FromActionTargetSnapshot(SimulationActionTargetSnapshot.None),
                ProgramStateValueKind.GameplayEffectAggregate => throw new InvalidOperationException("Gameplay Effect aggregate requires the Program catalog."),
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
            };
        }

        public static CharacterStateValue FromConstant(ProgramConstant constant, ProgramStateValueKind expectedKind)
        {
            if (constant == null)
                throw new ArgumentNullException(nameof(constant));
            CharacterStateValue value = expectedKind switch
            {
                ProgramStateValueKind.Boolean when constant.Kind == ProgramConstantKind.Boolean => FromBoolean(constant.Boolean),
                ProgramStateValueKind.Int32 when constant.Kind == ProgramConstantKind.Int32 => FromInt32(constant.Int32),
                ProgramStateValueKind.UInt64 when constant.Kind == ProgramConstantKind.UInt64 => FromUInt64(constant.UInt64),
                ProgramStateValueKind.Scalar when constant.Kind == ProgramConstantKind.Scalar => FromScalar(constant.Scalar),
                ProgramStateValueKind.Vector2 when constant.Kind == ProgramConstantKind.Vector2 => FromVector2(constant.Vector2),
                ProgramStateValueKind.Vector3 when constant.Kind == ProgramConstantKind.Vector3 => FromVector3(constant.Vector3),
                ProgramStateValueKind.Yaw when constant.Kind == ProgramConstantKind.Yaw => FromYaw(constant.Yaw),
                ProgramStateValueKind.Identity when constant.Kind == ProgramConstantKind.String => FromIdentity(constant.Text),
                ProgramStateValueKind.ActionTargetSnapshot when constant.Kind == ProgramConstantKind.Bytes =>
                    FromActionTargetSnapshot(SimulationActionTargetSnapshotCodec.Read(constant.Bytes.ToArray())),
                _ => throw new InvalidDataException(
                    $"Constant '{constant.Identity}' kind '{constant.Kind}' does not match state kind '{expectedKind}'.")
            };
            return value;
        }

        static CharacterStateValue Create(
            ProgramStateValueKind kind,
            bool boolean = default,
            int int32 = default,
            ulong uint64 = default,
            Float32Scalar scalar = default,
            Float32Vector2 vector2 = default,
            Float32Vector3 vector3 = default,
            Float32Yaw yaw = default,
            string identity = null,
            BlackboardOwnerToken blackboardOwnerToken = default,
            BlackboardWriteStamp blackboardWriteStamp = default,
            Float32InputRequestState inputRequest = default,
            Float32ActionActivationRequestState actionActivationRequest = default,
            Float32ActionInstanceState actionInstance = default,
            Float32ActionInstanceReference actionInstanceReference = default,
            SimulationActionTargetSnapshot actionTargetSnapshot = default,
            GameplayEffectStateAggregate gameplayEffectAggregate = null)
        {
            return new CharacterStateValue(
                kind,
                boolean,
                int32,
                uint64,
                scalar,
                vector2,
                vector3,
                yaw,
                identity,
                blackboardOwnerToken,
                blackboardWriteStamp,
                inputRequest,
                actionActivationRequest,
                actionInstance,
                actionInstanceReference,
                actionTargetSnapshot,
                gameplayEffectAggregate);
        }

        T Require<T>(ProgramStateValueKind expected, T value)
        {
            if (Kind != expected)
                throw new InvalidOperationException($"State value is '{Kind}', expected '{expected}'.");
            return value;
        }
    }

    internal sealed class CharacterStatePage
    {
        readonly CharacterStateValue[] m_Values;

        public CharacterStatePage(CharacterStateValue[] values, bool takeOwnership)
        {
            if (values == null || values.Length == 0 || values.Length > CharacterSimulationState.PageSize)
                throw new ArgumentException("Character state page size is invalid.", nameof(values));
            m_Values = takeOwnership ? values : (CharacterStateValue[])values.Clone();
        }

        public CharacterStateValue Get(int offset)
        {
            if (offset < 0 || offset >= m_Values.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
            return m_Values[offset];
        }

        public CharacterStateValue[] CopyValues() => (CharacterStateValue[])m_Values.Clone();

        public int Count => m_Values.Length;

        public void CopyTo(CharacterStateValue[] destination)
        {
            if (destination == null || destination.Length != m_Values.Length)
                throw new ArgumentException("Character state page destination size is invalid.", nameof(destination));
            Array.Copy(m_Values, destination, m_Values.Length);
        }
    }

    internal readonly struct CharacterStatePageReplacement
    {
        public CharacterStatePageReplacement(int partitionIndex, int pageIndex, CharacterStatePage page)
        {
            PartitionIndex = partitionIndex;
            PageIndex = pageIndex;
            Page = page ?? throw new ArgumentNullException(nameof(page));
        }

        public int PartitionIndex { get; }
        public int PageIndex { get; }
        public CharacterStatePage Page { get; }
    }

    internal sealed class CharacterStatePartition
    {
        readonly CharacterStatePage[] m_Pages;

        public CharacterStatePartition(ProgramStateValueKind valueKind, CharacterStatePage[] pages, bool takeOwnership)
        {
            ValueKind = valueKind;
            if (pages == null || pages.Length == 0)
                throw new ArgumentException("Character state partition pages are missing.", nameof(pages));
            m_Pages = takeOwnership ? pages : (CharacterStatePage[])pages.Clone();
        }

        public ProgramStateValueKind ValueKind { get; }
        public int PageCount => m_Pages.Length;
        public CharacterStatePage GetPage(int pageIndex)
        {
            if (pageIndex < 0 || pageIndex >= m_Pages.Length)
                throw new ArgumentOutOfRangeException(nameof(pageIndex));
            return m_Pages[pageIndex];
        }

        public CharacterStatePartition WithPages(
            CharacterStatePageReplacement[] replacements,
            int replacementCount,
            int partitionIndex)
        {
            CharacterStatePage[] pages = null;
            for (int i = 0; i < replacementCount; i++)
            {
                CharacterStatePageReplacement replacement = replacements[i];
                if (replacement.PartitionIndex != partitionIndex)
                    continue;
                pages ??= (CharacterStatePage[])m_Pages.Clone();
                if (replacement.PageIndex < 0 || replacement.PageIndex >= pages.Length)
                    throw new InvalidOperationException("Character state dirty page replacement is invalid.");
                pages[replacement.PageIndex] = replacement.Page;
            }
            return pages == null ? this : new CharacterStatePartition(ValueKind, pages, true);
        }
    }

    public sealed class CharacterSimulationState
    {
        public const int PageSize = 32;

        readonly CharacterStatePartition[] m_Partitions;
        readonly ProgramExecutionLayout m_Layout;
        CharacterStateHash m_StateHash;

        CharacterSimulationState(
            CharacterSimulationProgram program,
            ProgramExecutionLayout layout,
            ulong lastCompletedTick,
            CharacterStatePartition[] partitions,
            bool takeOwnership)
        {
            if (program == null)
                throw new ArgumentNullException(nameof(program));
            m_Layout = layout ?? throw new ArgumentNullException(nameof(layout));
            m_Layout.RequireProgram(program);
            NumericProfile = program.Manifest.NumericProfile;
            ProgramId = program.Manifest.ProgramId;
            ProgramHash = program.ProgramHash;
            LayoutHash = program.LayoutHash;
            LastCompletedTick = lastCompletedTick;
            m_Partitions = takeOwnership ? partitions : (CharacterStatePartition[])partitions.Clone();
            if (m_Partitions.Length != layout.StatePartitions.Count)
                throw new ArgumentException("Character state partition count does not match Program layout.", nameof(partitions));
            for (int i = 0; i < m_Partitions.Length; i++)
            {
                if (m_Partitions[i] == null || m_Partitions[i].ValueKind != layout.StatePartitions[i].ValueKind)
                    throw new ArgumentException($"Character state partition '{i}' does not match Program layout.", nameof(partitions));
            }
        }

        public SimulationNumericProfile NumericProfile { get; }
        public ProgramId ProgramId { get; }
        public ProgramHash ProgramHash { get; }
        public LayoutHash LayoutHash { get; }
        public ulong LastCompletedTick { get; }
        public int SlotCount => m_Layout == null ? 0 : m_Layout.StatePartitions.Count == 0 ? 0 : CountSlots(m_Layout.StatePartitions);
        internal ProgramExecutionLayout ExecutionLayout => m_Layout;

        internal bool TryGetStateHash(out CharacterStateHash stateHash)
        {
            stateHash = m_StateHash;
            return stateHash.IsValid;
        }

        internal CharacterStateHash CacheStateHash(CharacterStateHash stateHash)
        {
            if (!stateHash.IsValid)
                throw new ArgumentException("Character state hash is invalid.", nameof(stateHash));
            if (m_StateHash.IsValid && !m_StateHash.Equals(stateHash))
                throw new InvalidOperationException("Character state hash does not match its cached identity.");
            m_StateHash = stateHash;
            return m_StateHash;
        }

        public static CharacterSimulationState CreateInitial(CharacterSimulationProgram program)
        {
            if (program == null)
                throw new ArgumentNullException(nameof(program));
            ProgramExecutionLayout layout = ProgramExecutionLayout.GetOrCreate(program);
            var values = new CharacterStateValue[program.StateSlots.Count];
            for (int i = 0; i < values.Length; i++)
            {
                ProgramStateSlot slot = program.StateSlots[i];
                if (slot.ValueKind == ProgramStateValueKind.GameplayEffectAggregate)
                {
                    values[i] = CharacterStateValue.FromGameplayEffectAggregate(
                        GameplayEffectStateAggregate.CreateInitial(layout.GameplayEffectProgram));
                }
                else if (slot.ValueKind == ProgramStateValueKind.BlackboardOwnerToken &&
                    layout.Services.TryGetInitialBlackboardOwnerToken(i, out BlackboardOwnerToken token))
                {
                    values[i] = CharacterStateValue.FromBlackboardOwnerToken(token);
                }
                else
                {
                    values[i] = slot.DefaultConstantIndex >= 0
                        ? CharacterStateValue.FromConstant(program.Constants[slot.DefaultConstantIndex], slot.ValueKind)
                        : CharacterStateValue.Default(slot.ValueKind);
                }
            }
            return Create(program, layout, 0, values);
        }

        internal static CharacterSimulationState Create(
            CharacterSimulationProgram program,
            ProgramExecutionLayout layout,
            ulong lastCompletedTick,
            IReadOnlyList<CharacterStateValue> values)
        {
            if (values == null || values.Count != program.StateSlots.Count)
                throw new ArgumentException("Character state values do not match Program layout.", nameof(values));
            var partitions = new CharacterStatePartition[layout.StatePartitions.Count];
            for (int partitionIndex = 0; partitionIndex < partitions.Length; partitionIndex++)
            {
                TypedStatePartitionDescriptor descriptor = layout.StatePartitions[partitionIndex];
                var pages = new CharacterStatePage[descriptor.PageCount];
                for (int pageIndex = 0; pageIndex < pages.Length; pageIndex++)
                {
                    int pageLength = Math.Min(PageSize, descriptor.SlotCount - pageIndex * PageSize);
                    var pageValues = new CharacterStateValue[pageLength];
                    for (int offset = 0; offset < pageLength; offset++)
                    {
                        int slotIndex = descriptor.SlotIndexes[pageIndex * PageSize + offset];
                        CharacterStateValue value = values[slotIndex];
                        if (value.Kind != descriptor.ValueKind)
                            throw new InvalidDataException($"Character state slot '{slotIndex}' kind does not match Program layout.");
                        pageValues[offset] = value;
                    }
                    pages[pageIndex] = new CharacterStatePage(pageValues, true);
                }
                partitions[partitionIndex] = new CharacterStatePartition(descriptor.ValueKind, pages, true);
            }
            return new CharacterSimulationState(program, layout, lastCompletedTick, partitions, true);
        }

        public CharacterStateValue Get(int slotIndex, ProgramStateValueKind expectedKind)
        {
            TypedStateAddress address = m_Layout.Address(slotIndex);
            if (address.ValueKind != expectedKind)
                throw new InvalidOperationException($"State slot '{slotIndex}' is '{address.ValueKind}', expected '{expectedKind}'.");
            return Get(address);
        }

        internal CharacterStateValue Get(TypedStateAddress address)
        {
            RequireAddress(address);
            return m_Partitions[address.PartitionIndex].GetPage(address.PageIndex).Get(address.Offset);
        }

        internal CharacterStatePage GetPage(TypedStateAddress address)
        {
            RequireAddress(address);
            return m_Partitions[address.PartitionIndex].GetPage(address.PageIndex);
        }

        internal CharacterSimulationState WithDirtyPages(
            CharacterSimulationProgram program,
            SimulationTick completedTick,
            CharacterStatePageReplacement[] replacements,
            int replacementCount)
        {
            if (!completedTick.IsValid || completedTick.Value != LastCompletedTick + 1)
                throw new InvalidOperationException("Character state commit Tick is not the next Tick.");
            if (replacements == null || replacementCount < 0 || replacementCount > replacements.Length)
                throw new ArgumentException("Character state page replacements are invalid.", nameof(replacements));
            var partitions = (CharacterStatePartition[])m_Partitions.Clone();
            for (int partitionIndex = 0; partitionIndex < partitions.Length; partitionIndex++)
            {
                partitions[partitionIndex] = partitions[partitionIndex].WithPages(
                    replacements,
                    replacementCount,
                    partitionIndex);
            }
            return new CharacterSimulationState(program, m_Layout, completedTick.Value, partitions, true);
        }

        void RequireAddress(TypedStateAddress address)
        {
            if (!address.IsValid || !m_Layout.Address(address.SlotIndex).Equals(address))
                throw new InvalidOperationException("Typed state address does not belong to this Character state layout.");
        }

        static int CountSlots(IReadOnlyList<TypedStatePartitionDescriptor> partitions)
        {
            int count = 0;
            for (int i = 0; i < partitions.Count; i++)
                count = checked(count + partitions[i].SlotCount);
            return count;
        }
    }
}
