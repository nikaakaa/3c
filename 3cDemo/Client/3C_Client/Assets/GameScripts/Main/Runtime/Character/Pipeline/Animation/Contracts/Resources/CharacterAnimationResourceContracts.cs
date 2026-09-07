using System;
using ThirdPersonCharacter.Pipeline.Animation.Resources;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal readonly struct CharacterAnimationResourceAddress : IEquatable<CharacterAnimationResourceAddress>
    {
        internal CharacterAnimationResourceAddress(string value)
        {
            Value = string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException("Animation resource address is required.", nameof(value))
                : value.Trim();
        }

        internal string Value { get; }
        internal bool IsValid => !string.IsNullOrEmpty(Value);
        public bool Equals(CharacterAnimationResourceAddress other) =>
            string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) =>
            obj is CharacterAnimationResourceAddress other && Equals(other);
        public override int GetHashCode() =>
            StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        public override string ToString() => Value ?? string.Empty;
    }

    internal readonly struct CharacterAnimationAssetLoadTicket : IEquatable<CharacterAnimationAssetLoadTicket>
    {
        internal CharacterAnimationAssetLoadTicket(ulong value)
        {
            Value = value == 0
                ? throw new ArgumentOutOfRangeException(nameof(value))
                : value;
        }

        internal ulong Value { get; }
        internal bool IsValid => Value != 0;
        public bool Equals(CharacterAnimationAssetLoadTicket other) => Value == other.Value;
        public override bool Equals(object obj) =>
            obj is CharacterAnimationAssetLoadTicket other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
    }

    internal enum CharacterAnimationAssetLoadState : byte
    {
        Pending = 1,
        Ready = 2,
        Invalid = 3
    }

    internal readonly struct CharacterAnimationAssetLoadResult
    {
        internal CharacterAnimationAssetLoadResult(
            CharacterAnimationAssetLoadState state,
            CharacterAclAnimationResource resource,
            string message)
        {
            if (!Enum.IsDefined(typeof(CharacterAnimationAssetLoadState), state) ||
                state == CharacterAnimationAssetLoadState.Ready && !resource ||
                state != CharacterAnimationAssetLoadState.Ready && resource)
                throw new ArgumentException("Animation asset load result is invalid.");
            State = state;
            Resource = resource;
            Message = message ?? string.Empty;
        }

        internal CharacterAnimationAssetLoadState State { get; }
        internal CharacterAclAnimationResource Resource { get; }
        internal string Message { get; }
        internal bool IsPending => State == CharacterAnimationAssetLoadState.Pending;
        internal bool IsReady => State == CharacterAnimationAssetLoadState.Ready;
        internal bool IsInvalid => State == CharacterAnimationAssetLoadState.Invalid;
    }

    internal readonly struct CharacterAnimationResourceSettings
    {
        internal CharacterAnimationResourceSettings(long residentBudgetBytes)
        {
            ResidentBudgetBytes = residentBudgetBytes > 0
                ? residentBudgetBytes
                : throw new ArgumentOutOfRangeException(nameof(residentBudgetBytes));
        }

        internal long ResidentBudgetBytes { get; }
        internal static CharacterAnimationResourceSettings CorinInitial =>
            new CharacterAnimationResourceSettings(128L * 1024L * 1024L);
    }

    internal readonly struct CharacterAclResourceLease : IDisposable
    {
        internal CharacterAclResourceLease(
            CharacterAclResourceStore owner,
            CharacterAclResourceLeaseTable leaseTable,
            int slotIndex,
            ulong slotGeneration,
            int resourceIndex,
            ulong resourceGeneration)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            LeaseTable = leaseTable ?? throw new ArgumentNullException(nameof(leaseTable));
            SlotIndex = slotIndex >= 0
                ? slotIndex
                : throw new ArgumentOutOfRangeException(nameof(slotIndex));
            SlotGeneration = slotGeneration != 0
                ? slotGeneration
                : throw new ArgumentOutOfRangeException(nameof(slotGeneration));
            ResourceIndex = resourceIndex >= 0
                ? resourceIndex
                : throw new ArgumentOutOfRangeException(nameof(resourceIndex));
            ResourceGeneration = resourceGeneration != 0
                ? resourceGeneration
                : throw new ArgumentOutOfRangeException(nameof(resourceGeneration));
        }

        internal CharacterAclResourceStore Owner { get; }
        internal CharacterAclResourceLeaseTable LeaseTable { get; }
        internal int SlotIndex { get; }
        internal ulong SlotGeneration { get; }
        internal int ResourceIndex { get; }
        internal ulong ResourceGeneration { get; }
        internal bool IsValid =>
            Owner != null &&
            LeaseTable != null &&
            SlotIndex >= 0 &&
            SlotGeneration != 0 &&
            ResourceIndex >= 0 &&
            ResourceGeneration != 0;
        internal IntPtr Group => Owner.RequireGroup(in this);

        public void Dispose()
        {
            if (IsValid)
                Owner.Release(in this);
        }
    }
}
