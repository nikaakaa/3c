using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation
{
    internal enum Float32CharacterStateTransactionStatus : byte
    {
        Created = 0,
        Active = 1,
        Committed = 2,
        Aborted = 3,
        Disposed = 4
    }

    internal readonly struct Float32CharacterStateTransactionDiagnostics
    {
        public Float32CharacterStateTransactionDiagnostics(
            Float32CharacterStateTransactionStatus status,
            ulong epoch,
            int dirtyPartitions,
            int dirtyPages,
            int workspaceOwnedPages,
            int publishedPages,
            int discardedPages,
            bool gameplayEffectDirty,
            int savepointDepth)
        {
            Status = status;
            Epoch = epoch;
            DirtyPartitions = dirtyPartitions;
            DirtyPages = dirtyPages;
            WorkspaceOwnedPages = workspaceOwnedPages;
            PublishedPages = publishedPages;
            DiscardedPages = discardedPages;
            GameplayEffectDirty = gameplayEffectDirty;
            SavepointDepth = savepointDepth;
        }

        public Float32CharacterStateTransactionStatus Status { get; }
        public ulong Epoch { get; }
        public int DirtyPartitions { get; }
        public int DirtyPages { get; }
        public int WorkspaceOwnedPages { get; }
        public int PublishedPages { get; }
        public int DiscardedPages { get; }
        public bool GameplayEffectDirty { get; }
        public int SavepointDepth { get; }
    }

    internal sealed class Float32CharacterStateTransactionWorkspace
    {
        readonly ProgramExecutionLayout m_Layout;
        readonly int[] m_PartitionOffsets;
        readonly int[] m_PagePartitions;
        readonly int[] m_PageIndexes;
        readonly ulong[] m_PageEpochs;
        readonly ulong[] m_PartitionEpochs;
        readonly DirtyPageOwnership[] m_Ownership;
        readonly CharacterStateValue[][] m_PageValues;
        readonly int[] m_DirtySlots;
        readonly CharacterStatePageReplacement[] m_Replacements;
        ulong m_Epoch;
        int m_DirtyCount;
        int m_DirtyPartitionCount;
        bool m_Active;

        public Float32CharacterStateTransactionWorkspace(ProgramExecutionLayout layout)
        {
            m_Layout = layout ?? throw new ArgumentNullException(nameof(layout));
            m_PartitionOffsets = new int[layout.StatePartitions.Count + 1];
            int pageCount = 0;
            for (int i = 0; i < layout.StatePartitions.Count; i++)
            {
                m_PartitionOffsets[i] = pageCount;
                pageCount = checked(pageCount + layout.StatePartitions[i].PageCount);
            }
            m_PartitionOffsets[layout.StatePartitions.Count] = pageCount;
            m_PagePartitions = new int[pageCount];
            m_PageIndexes = new int[pageCount];
            for (int partition = 0; partition < layout.StatePartitions.Count; partition++)
            {
                for (int page = 0; page < layout.StatePartitions[partition].PageCount; page++)
                {
                    int slot = m_PartitionOffsets[partition] + page;
                    m_PagePartitions[slot] = partition;
                    m_PageIndexes[slot] = page;
                }
            }
            m_PageEpochs = new ulong[pageCount];
            m_PartitionEpochs = new ulong[layout.StatePartitions.Count];
            m_Ownership = new DirtyPageOwnership[pageCount];
            m_PageValues = new CharacterStateValue[pageCount][];
            m_DirtySlots = new int[pageCount];
            m_Replacements = new CharacterStatePageReplacement[pageCount];
        }

        public ulong Epoch => m_Epoch;
        public int DirtyCount => m_DirtyCount;
        public int DirtyPartitionCount => m_DirtyPartitionCount;
        public CharacterStatePageReplacement[] Replacements => m_Replacements;

        public ulong Begin(ProgramExecutionLayout layout)
        {
            if (!ReferenceEquals(m_Layout, layout))
                throw new InvalidOperationException("State transaction workspace belongs to another Program layout.");
            if (m_Active)
                throw new InvalidOperationException("State transaction workspace is already active.");
            m_Epoch = checked(m_Epoch + 1);
            if (m_Epoch == 0)
                throw new OverflowException("State transaction workspace epoch overflowed.");
            m_DirtyCount = 0;
            m_DirtyPartitionCount = 0;
            m_Active = true;
            return m_Epoch;
        }

        public bool TryGet(TypedStateAddress address, ulong epoch, out CharacterStateValue value)
        {
            Require(epoch);
            int slot = Slot(address);
            if (m_PageEpochs[slot] == epoch && m_Ownership[slot] == DirtyPageOwnership.WorkspaceOwned)
            {
                value = m_PageValues[slot][address.Offset];
                return true;
            }
            value = default;
            return false;
        }

        public CharacterStateValue[] GetWritable(
            TypedStateAddress address,
            ulong epoch,
            CharacterSimulationState baseState)
        {
            Require(epoch);
            int slot = Slot(address);
            if (m_PageEpochs[slot] == epoch)
            {
                if (m_Ownership[slot] != DirtyPageOwnership.WorkspaceOwned)
                    throw new InvalidOperationException("Dirty page is not writable in the current transaction epoch.");
                return m_PageValues[slot];
            }

            CharacterStatePage basePage = baseState.GetPage(address);
            CharacterStateValue[] values = m_PageValues[slot];
            if (values == null || values.Length != basePage.Count)
                values = new CharacterStateValue[basePage.Count];
            basePage.CopyTo(values);
            m_PageValues[slot] = values;
            m_PageEpochs[slot] = epoch;
            m_Ownership[slot] = DirtyPageOwnership.WorkspaceOwned;
            m_DirtySlots[m_DirtyCount++] = slot;
            if (m_PartitionEpochs[address.PartitionIndex] != epoch)
            {
                m_PartitionEpochs[address.PartitionIndex] = epoch;
                m_DirtyPartitionCount++;
            }
            return values;
        }

        public void Publish(ulong epoch)
        {
            Require(epoch);
            for (int i = 0; i < m_DirtyCount; i++)
            {
                int slot = m_DirtySlots[i];
                if (m_PageEpochs[slot] != epoch || m_Ownership[slot] != DirtyPageOwnership.WorkspaceOwned)
                    throw new InvalidOperationException("State transaction dirty page ownership is invalid during Commit.");
                CharacterStateValue[] values = m_PageValues[slot];
                m_PageValues[slot] = null;
                m_Ownership[slot] = DirtyPageOwnership.Published;
                m_Replacements[i] = new CharacterStatePageReplacement(
                    m_PagePartitions[slot],
                    m_PageIndexes[slot],
                    new CharacterStatePage(values, true));
            }
        }

        public void CompleteCommit(ulong epoch)
        {
            Require(epoch);
            for (int i = 0; i < m_DirtyCount; i++)
            {
                int slot = m_DirtySlots[i];
                if (m_Ownership[slot] != DirtyPageOwnership.Published)
                    throw new InvalidOperationException("State transaction page was not published by Commit.");
                m_Replacements[i] = default;
                m_Ownership[slot] = DirtyPageOwnership.Empty;
            }
            m_Active = false;
        }

        public void Abort(ulong epoch)
        {
            Require(epoch);
            for (int i = 0; i < m_DirtyCount; i++)
            {
                int slot = m_DirtySlots[i];
                if (m_Ownership[slot] == DirtyPageOwnership.WorkspaceOwned)
                    m_Ownership[slot] = DirtyPageOwnership.Discarded;
                else if (m_Ownership[slot] == DirtyPageOwnership.Published)
                    m_PageValues[slot] = null;
                m_Replacements[i] = default;
            }
            m_Active = false;
        }

        public void CountOwnership(out int workspaceOwned, out int published, out int discarded)
        {
            workspaceOwned = 0;
            published = 0;
            discarded = 0;
            for (int i = 0; i < m_Ownership.Length; i++)
            {
                switch (m_Ownership[i])
                {
                    case DirtyPageOwnership.WorkspaceOwned: workspaceOwned++; break;
                    case DirtyPageOwnership.Published: published++; break;
                    case DirtyPageOwnership.Discarded: discarded++; break;
                }
            }
        }

        int Slot(TypedStateAddress address)
        {
            return m_PartitionOffsets[address.PartitionIndex] + address.PageIndex;
        }

        void Require(ulong epoch)
        {
            if (!m_Active || epoch == 0 || epoch != m_Epoch)
                throw new InvalidOperationException("State transaction workspace epoch is stale or inactive.");
        }
    }

    internal sealed class Float32CharacterStateSavepoint
    {
        internal Float32CharacterStateSavepoint(
            int depth,
            bool hadGameplayEffectWorking,
            bool gameplayEffectDirty,
            GameplayEffectStateAggregate gameplayEffectSnapshot)
        {
            Depth = depth;
            HadGameplayEffectWorking = hadGameplayEffectWorking;
            GameplayEffectDirty = gameplayEffectDirty;
            GameplayEffectSnapshot = gameplayEffectSnapshot;
        }

        internal int Depth { get; }
        internal bool HadGameplayEffectWorking { get; }
        internal bool GameplayEffectDirty { get; }
        internal GameplayEffectStateAggregate GameplayEffectSnapshot { get; }
    }

    internal sealed class Float32CharacterStateTransaction : IDisposable
    {
        readonly CharacterSimulationProgram m_Program;
        readonly ProgramExecutionLayout m_Layout;
        readonly CharacterSimulationState m_BaseState;
        readonly ActorId m_ActorId;
        readonly SimulationTick m_Tick;
        readonly Float32CharacterStateTransactionWorkspace m_Workspace;
        readonly ulong m_Epoch;
        readonly Stack<Float32CharacterStateSavepoint> m_Savepoints =
            new Stack<Float32CharacterStateSavepoint>();
        SimulationGameplayEffectState m_GameplayEffectWorking;
        Float32GameplayEffectExecutionScratch m_GameplayEffectScratch;
        Float32CharacterStateTransactionStatus m_Status;

        Float32CharacterStateTransaction(
            CharacterSimulationProgram program,
            ProgramExecutionLayout layout,
            CharacterSimulationState baseState,
            ActorId actorId,
            SimulationTick tick,
            Float32CharacterStateTransactionWorkspace workspace)
        {
            m_Program = program ?? throw new ArgumentNullException(nameof(program));
            m_Layout = layout ?? throw new ArgumentNullException(nameof(layout));
            m_BaseState = baseState ?? throw new ArgumentNullException(nameof(baseState));
            if (!actorId.IsValid || !tick.IsValid)
                throw new ArgumentException("Character state transaction identity is incomplete.");
            m_Layout.RequireProgram(m_Program);
            if (!ReferenceEquals(baseState.ExecutionLayout, layout) ||
                baseState.ProgramId != program.Manifest.ProgramId ||
                !baseState.ProgramHash.Equals(program.ProgramHash) ||
                !baseState.LayoutHash.Equals(program.LayoutHash) ||
                tick.Value != baseState.LastCompletedTick + 1)
            {
                throw new InvalidOperationException("Character state transaction base binding is stale or mismatched.");
            }
            m_ActorId = actorId;
            m_Tick = tick;
            m_Workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
            m_Epoch = workspace.Begin(layout);
            m_Status = Float32CharacterStateTransactionStatus.Created;
            m_Status = Float32CharacterStateTransactionStatus.Active;
        }

        public CharacterSimulationProgram Program => m_Program;
        public ProgramExecutionLayout Layout => m_Layout;
        public CharacterSimulationState BaseState => m_BaseState;
        public ActorId ActorId => m_ActorId;
        public SimulationTick Tick => m_Tick;
        public Float32CharacterStateTransactionStatus Status => m_Status;

        public static Float32CharacterStateTransaction Begin(
            CharacterSimulationProgram program,
            ProgramExecutionLayout layout,
            CharacterSimulationState baseState,
            ActorId actorId,
            SimulationTick tick,
            Float32CharacterStateTransactionWorkspace workspace)
        {
            return new Float32CharacterStateTransaction(program, layout, baseState, actorId, tick, workspace);
        }

        public CharacterStateValue Get(int slotIndex)
        {
            return Get(m_Layout.Address(slotIndex));
        }

        public CharacterStateValue Get(TypedStateAddress address)
        {
            RequireActive();
            RequireAddress(address);
            if (m_Workspace.TryGet(address, m_Epoch, out CharacterStateValue value))
                return value;
            return m_BaseState.Get(address);
        }

        public void Set(int slotIndex, CharacterStateValue value)
        {
            Set(m_Layout.Address(slotIndex), value);
        }

        public void Set(TypedStateAddress address, CharacterStateValue value)
        {
            RequireActive();
            RequireAddress(address);
            if (value.Kind != address.ValueKind)
                throw new InvalidOperationException($"State slot '{address.SlotIndex}' expects '{address.ValueKind}', received '{value.Kind}'.");
            CharacterStateValue[] page = GetWritablePage(address);
            page[address.Offset] = value;
        }

        public void Reset(int slotIndex)
        {
            RequireActive();
            ProgramStateSlot slot = m_Program.StateSlots[slotIndex];
            CharacterStateValue value;
            if (slot.ValueKind == ProgramStateValueKind.GameplayEffectAggregate)
            {
                value = CharacterStateValue.FromGameplayEffectAggregate(
                    GameplayEffectStateAggregate.CreateInitial(m_Layout.GameplayEffectProgram));
            }
            else
            {
                value = slot.DefaultConstantIndex >= 0
                    ? CharacterStateValue.FromConstant(m_Program.Constants[slot.DefaultConstantIndex], slot.ValueKind)
                    : CharacterStateValue.Default(slot.ValueKind);
            }
            Set(slotIndex, value);
        }

        public SimulationGameplayEffectState GetGameplayEffectState(
            Float32GameplayEffectExecutionScratch scratch)
        {
            RequireActive();
            if (scratch == null)
                throw new ArgumentNullException(nameof(scratch));
            if (m_GameplayEffectWorking != null)
            {
                if (!ReferenceEquals(m_GameplayEffectScratch, scratch))
                    throw new InvalidOperationException("Gameplay Effect state is bound to another Actor workspace.");
                return m_GameplayEffectWorking;
            }
            GameplayEffectStateAggregate aggregate = Get(m_Layout.GameplayEffectAggregateAddress).GameplayEffectAggregate;
            m_GameplayEffectScratch = scratch;
            m_GameplayEffectWorking = new SimulationGameplayEffectState(
                m_Layout.GameplayEffectProgram,
                aggregate,
                scratch);
            return m_GameplayEffectWorking;
        }

        public GameplayEffectStateAggregate GetGameplayEffectAggregate()
        {
            RequireActive();
            return Get(m_Layout.GameplayEffectAggregateAddress).GameplayEffectAggregate;
        }

        public Float32CharacterStateSavepoint CreateSavepoint()
        {
            RequireActive();
            var savepoint = new Float32CharacterStateSavepoint(
                m_Savepoints.Count + 1,
                m_GameplayEffectWorking != null,
                m_GameplayEffectWorking != null && m_GameplayEffectWorking.HasChanges,
                m_GameplayEffectWorking?.Freeze());
            m_Savepoints.Push(savepoint);
            return savepoint;
        }

        public void Restore(Float32CharacterStateSavepoint savepoint)
        {
            RequireTopSavepoint(savepoint);
            if (!savepoint.HadGameplayEffectWorking)
            {
                m_GameplayEffectWorking = null;
            }
            else if (m_GameplayEffectWorking == null)
            {
                m_GameplayEffectWorking = new SimulationGameplayEffectState(
                    m_Layout.GameplayEffectProgram,
                    savepoint.GameplayEffectSnapshot,
                    m_GameplayEffectScratch);
            }
            else
            {
                m_GameplayEffectWorking.Restore(
                    savepoint.GameplayEffectSnapshot,
                    savepoint.GameplayEffectDirty);
            }
            m_Savepoints.Pop();
        }

        public void Release(Float32CharacterStateSavepoint savepoint)
        {
            RequireTopSavepoint(savepoint);
            m_Savepoints.Pop();
        }

        public CharacterSimulationState Commit()
        {
            RequireActive();
            if (m_Savepoints.Count != 0)
                throw new InvalidOperationException("Character state transaction cannot Commit with active savepoints.");
            if (m_GameplayEffectWorking != null && m_GameplayEffectWorking.HasChanges)
            {
                Set(
                    m_Layout.GameplayEffectAggregateAddress,
                    CharacterStateValue.FromGameplayEffectAggregate(m_GameplayEffectWorking.Freeze()));
            }

            try
            {
                m_Workspace.Publish(m_Epoch);
                CharacterSimulationState committed = m_BaseState.WithDirtyPages(
                    m_Program,
                    m_Tick,
                    m_Workspace.Replacements,
                    m_Workspace.DirtyCount);
                m_Workspace.CompleteCommit(m_Epoch);
                m_Status = Float32CharacterStateTransactionStatus.Committed;
                ClearOwnedState();
                return committed;
            }
            catch
            {
                m_Workspace.Abort(m_Epoch);
                m_Status = Float32CharacterStateTransactionStatus.Aborted;
                ClearOwnedState();
                throw;
            }
        }

        public void Abort()
        {
            if (m_Status != Float32CharacterStateTransactionStatus.Active)
                throw new InvalidOperationException($"Character state transaction cannot Abort from '{m_Status}'.");
            m_Workspace.Abort(m_Epoch);
            m_Status = Float32CharacterStateTransactionStatus.Aborted;
            ClearOwnedState();
        }

        public Float32CharacterStateTransactionDiagnostics Diagnostics()
        {
            m_Workspace.CountOwnership(out int workspaceOwned, out int published, out int discarded);
            return new Float32CharacterStateTransactionDiagnostics(
                m_Status,
                m_Epoch,
                m_Workspace.DirtyPartitionCount,
                m_Workspace.DirtyCount,
                workspaceOwned,
                published,
                discarded,
                m_GameplayEffectWorking != null && m_GameplayEffectWorking.HasChanges,
                m_Savepoints.Count);
        }

        public void Dispose()
        {
            if (m_Status == Float32CharacterStateTransactionStatus.Active)
                Abort();
            m_Status = Float32CharacterStateTransactionStatus.Disposed;
        }

        CharacterStateValue[] GetWritablePage(TypedStateAddress address)
        {
            return m_Workspace.GetWritable(address, m_Epoch, m_BaseState);
        }

        void RequireAddress(TypedStateAddress address)
        {
            if (!address.IsValid || !m_Layout.Address(address.SlotIndex).Equals(address))
                throw new InvalidOperationException("Typed state address does not belong to this transaction Program layout.");
        }

        void RequireActive()
        {
            if (m_Status != Float32CharacterStateTransactionStatus.Active)
                throw new InvalidOperationException($"Character state transaction is '{m_Status}', expected Active.");
        }

        void RequireTopSavepoint(Float32CharacterStateSavepoint savepoint)
        {
            RequireActive();
            if (savepoint == null || m_Savepoints.Count == 0 || !ReferenceEquals(m_Savepoints.Peek(), savepoint) || savepoint.Depth != m_Savepoints.Count)
                throw new InvalidOperationException("Character state savepoints must be restored or released in stack order.");
        }

        void ClearOwnedState()
        {
            m_Savepoints.Clear();
            m_GameplayEffectWorking = null;
            m_GameplayEffectScratch = null;
        }

    }
}
