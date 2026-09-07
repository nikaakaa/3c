using System;

namespace ThirdPersonCharacter.Pipeline.Animation.Resources
{
    internal sealed class CharacterAclResourceLeaseTable : IDisposable
    {
        struct Slot
        {
            internal bool Active;
            internal ulong SlotGeneration;
            internal int StoreResourceIndex;
            internal ulong ResourceGeneration;
        }

        readonly Slot[] m_Slots;
        ulong m_NextSlotGeneration = 1;
        int m_ActiveCount;
        bool m_Closed;

        internal CharacterAclResourceLeaseTable(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            m_Slots = new Slot[capacity];
        }

        internal bool TryAcquire(
            int storeResourceIndex,
            ulong resourceGeneration,
            out int slotIndex,
            out ulong slotGeneration)
        {
            RequireOpen();
            RequireResourceIdentity(storeResourceIndex, resourceGeneration);
            for (int i = 0; i < m_Slots.Length; i++)
            {
                if (m_Slots[i].Active)
                    continue;
                ulong generation = NextSlotGeneration();
                m_Slots[i] = new Slot
                {
                    Active = true,
                    SlotGeneration = generation,
                    StoreResourceIndex = storeResourceIndex,
                    ResourceGeneration = resourceGeneration
                };
                m_ActiveCount++;
                slotIndex = i;
                slotGeneration = generation;
                return true;
            }
            slotIndex = -1;
            slotGeneration = 0;
            return false;
        }

        internal void Require(
            int slotIndex,
            ulong slotGeneration,
            int storeResourceIndex,
            ulong resourceGeneration)
        {
            RequireOpen();
            if ((uint)slotIndex >= (uint)m_Slots.Length)
                throw new InvalidOperationException("ACL resource lease slot is stale.");
            Slot slot = m_Slots[slotIndex];
            if (!slot.Active ||
                slot.SlotGeneration != slotGeneration ||
                slot.StoreResourceIndex != storeResourceIndex ||
                slot.ResourceGeneration != resourceGeneration)
                throw new InvalidOperationException("ACL resource lease slot is stale.");
        }

        internal void Release(
            int slotIndex,
            ulong slotGeneration,
            int storeResourceIndex,
            ulong resourceGeneration)
        {
            Require(
                slotIndex,
                slotGeneration,
                storeResourceIndex,
                resourceGeneration);
            m_Slots[slotIndex] = default;
            m_ActiveCount--;
        }

        public void Dispose()
        {
            if (m_Closed)
                return;
            if (m_ActiveCount != 0)
                throw new InvalidOperationException(
                    "ACL resource lease table still has active leases.");
            Array.Clear(m_Slots, 0, m_Slots.Length);
            m_Closed = true;
        }

        void RequireOpen()
        {
            if (m_Closed)
                throw new ObjectDisposedException(nameof(CharacterAclResourceLeaseTable));
        }

        ulong NextSlotGeneration()
        {
            if (m_NextSlotGeneration == 0)
                throw new InvalidOperationException(
                    "ACL resource lease slot generation was exhausted.");
            return m_NextSlotGeneration++;
        }

        static void RequireResourceIdentity(
            int resourceIndex,
            ulong resourceGeneration)
        {
            if (resourceIndex < 0 || resourceGeneration == 0)
                throw new ArgumentException("ACL resource lease identity is invalid.");
        }
    }
}
