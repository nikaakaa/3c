using System;
using UnityEngine;

namespace ThirdPersonGameplay.Effects
{
    [Serializable]
    public struct GameplayEffectId : IEquatable<GameplayEffectId>, IComparable<GameplayEffectId>
    {
        [SerializeField] string m_Value;

        public GameplayEffectId(string value)
        {
            m_Value = Normalize(value);
        }

        public string Value => Normalize(m_Value);
        public bool IsValid => !string.IsNullOrEmpty(Value);
        public bool Equals(GameplayEffectId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is GameplayEffectId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
        public int CompareTo(GameplayEffectId other) => string.Compare(Value, other.Value, StringComparison.Ordinal);
        public override string ToString() => Value;
        public static bool operator ==(GameplayEffectId left, GameplayEffectId right) => left.Equals(right);
        public static bool operator !=(GameplayEffectId left, GameplayEffectId right) => !left.Equals(right);

        static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    [Serializable]
    public struct GameplaySetByCallerValue
    {
        [SerializeField] string m_ParameterId;
        [SerializeField] float m_Value;

        public GameplaySetByCallerValue(string parameterId, float value)
        {
            m_ParameterId = parameterId ?? string.Empty;
            m_Value = value;
        }

        public string ParameterId => string.IsNullOrWhiteSpace(m_ParameterId) ? string.Empty : m_ParameterId.Trim();
        public float Value => m_Value;
    }

    public enum GameplayEffectRemoveSelector : byte
    {
        EffectId = 1,
        EffectTagQuery = 3
    }

    public enum GameplayCueTrigger : byte
    {
        OnActive,
        Executed,
        WhileActive,
        Removed,
        Expired
    }
}
