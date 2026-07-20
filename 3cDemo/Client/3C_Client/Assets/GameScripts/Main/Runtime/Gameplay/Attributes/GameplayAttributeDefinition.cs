using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonGameplay.Attributes
{
    [Serializable]
    public struct GameplayAttributeId : IEquatable<GameplayAttributeId>, IComparable<GameplayAttributeId>
    {
        [SerializeField] string m_Value;

        public GameplayAttributeId(string value)
        {
            m_Value = Normalize(value);
        }

        public string Value => Normalize(m_Value);
        public bool IsValid => !string.IsNullOrEmpty(Value);
        public bool Equals(GameplayAttributeId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is GameplayAttributeId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
        public int CompareTo(GameplayAttributeId other) => string.Compare(Value, other.Value, StringComparison.Ordinal);
        public override string ToString() => Value;
        public static bool operator ==(GameplayAttributeId left, GameplayAttributeId right) => left.Equals(right);
        public static bool operator !=(GameplayAttributeId left, GameplayAttributeId right) => !left.Equals(right);

        static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public enum GameplayAttributeBoundSource : byte
    {
        Constant,
        Attribute
    }

    [Serializable]
    public sealed class GameplayAttributeBoundDefinition
    {
        [SerializeField] bool m_Enabled;
        [SerializeField] GameplayAttributeBoundSource m_Source;
        [SerializeField] float m_Constant;
        [SerializeField] GameplayAttributeId m_AttributeId;

        public bool Enabled => m_Enabled;
        public GameplayAttributeBoundSource Source => m_Source;
        public float Constant => m_Constant;
        public GameplayAttributeId AttributeId => m_AttributeId;
    }

    [CreateAssetMenu(fileName = "GameplayAttributeDefinition", menuName = "3C/Gameplay/Attribute Definition")]
    public sealed class GameplayAttributeDefinition : ScriptableObject
    {
        [SerializeField] GameplayAttributeId m_AttributeId;
        [SerializeField] string m_DisplayName;
        [SerializeField] string m_DebugCategory;
        [SerializeField] GameplayAttributeBoundDefinition m_Minimum = new GameplayAttributeBoundDefinition();
        [SerializeField] GameplayAttributeBoundDefinition m_Maximum = new GameplayAttributeBoundDefinition();

        public GameplayAttributeId AttributeId => m_AttributeId;
        public string DisplayName => m_DisplayName ?? string.Empty;
        public string DebugCategory => m_DebugCategory ?? string.Empty;
        public GameplayAttributeBoundDefinition Minimum => m_Minimum;
        public GameplayAttributeBoundDefinition Maximum => m_Maximum;

        public bool CollectConfigurationErrors(ISet<GameplayAttributeId> registeredAttributes, List<string> errors)
        {
            bool valid = true;
            if (!m_AttributeId.IsValid)
            {
                errors?.Add($"{name}: attribute id is missing.");
                valid = false;
            }
            if (string.IsNullOrWhiteSpace(m_DisplayName))
            {
                errors?.Add($"{name}: display name is missing.");
                valid = false;
            }
            valid &= ValidateBound(m_Minimum, "minimum", registeredAttributes, errors);
            valid &= ValidateBound(m_Maximum, "maximum", registeredAttributes, errors);
            if (m_Minimum != null && m_Maximum != null &&
                m_Minimum.Enabled && m_Maximum.Enabled &&
                m_Minimum.Source == GameplayAttributeBoundSource.Constant &&
                m_Maximum.Source == GameplayAttributeBoundSource.Constant &&
                m_Minimum.Constant > m_Maximum.Constant)
            {
                errors?.Add($"{name}: minimum bound exceeds maximum bound.");
                valid = false;
            }
            return valid;
        }

        bool ValidateBound(
            GameplayAttributeBoundDefinition bound,
            string label,
            ISet<GameplayAttributeId> registeredAttributes,
            List<string> errors)
        {
            if (bound == null || !bound.Enabled)
                return true;
            if (bound.Source == GameplayAttributeBoundSource.Constant)
            {
                if (GameplayNumber.IsFinite(bound.Constant))
                    return true;
                errors?.Add($"{name}: {label} constant must be finite.");
                return false;
            }
            if (!bound.AttributeId.IsValid || registeredAttributes == null || !registeredAttributes.Contains(bound.AttributeId))
            {
                errors?.Add($"{name}: {label} bound references missing attribute '{bound.AttributeId}'.");
                return false;
            }
            return true;
        }
    }

    internal static class GameplayNumber
    {
        public static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }

    [Serializable]
    public sealed class InitialGameplayAttributeValue
    {
        [SerializeField] GameplayAttributeDefinition m_Definition;
        [SerializeField] float m_BaseValue;

        public GameplayAttributeDefinition Definition => m_Definition;
        public float BaseValue => m_BaseValue;
    }

    public enum GameplayModifierOperation : byte
    {
        Additive,
        Multiplicative,
        Override,
        Clamp
    }

    public enum GameplayClampBound : byte
    {
        Minimum,
        Maximum
    }
}
