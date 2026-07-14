using System;
using System.Collections.Generic;
using ThirdPersonGameplay.Effects;
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

        bool ValidateBound(GameplayAttributeBoundDefinition bound, string label, ISet<GameplayAttributeId> registeredAttributes, List<string> errors)
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

    public readonly struct GameplayAttributeBoundData
    {
        public GameplayAttributeBoundData(bool enabled, GameplayAttributeBoundSource source, float constant, GameplayAttributeId attributeId)
        {
            Enabled = enabled;
            Source = source;
            Constant = constant;
            AttributeId = attributeId;
        }

        public bool Enabled { get; }
        public GameplayAttributeBoundSource Source { get; }
        public float Constant { get; }
        public GameplayAttributeId AttributeId { get; }
    }

    public sealed class GameplayAttributeDefinitionData
    {
        public GameplayAttributeDefinitionData(
            GameplayAttributeId attributeId,
            string displayName,
            string debugCategory,
            GameplayAttributeBoundData minimum,
            GameplayAttributeBoundData maximum)
        {
            AttributeId = attributeId;
            DisplayName = displayName ?? string.Empty;
            DebugCategory = debugCategory ?? string.Empty;
            Minimum = minimum;
            Maximum = maximum;
        }

        public GameplayAttributeId AttributeId { get; }
        public string DisplayName { get; }
        public string DebugCategory { get; }
        public GameplayAttributeBoundData Minimum { get; }
        public GameplayAttributeBoundData Maximum { get; }
    }

    public readonly struct GameplayAttributeInitialValueData
    {
        public GameplayAttributeInitialValueData(GameplayAttributeId attributeId, float baseValue)
        {
            AttributeId = attributeId;
            BaseValue = baseValue;
        }

        public GameplayAttributeId AttributeId { get; }
        public float BaseValue { get; }
    }

    public readonly struct GameplayAttributeValue
    {
        public GameplayAttributeValue(GameplayAttributeId attributeId, float baseValue, float currentValue, ulong revision)
        {
            AttributeId = attributeId;
            BaseValue = baseValue;
            CurrentValue = currentValue;
            Revision = revision;
        }

        public GameplayAttributeId AttributeId { get; }
        public float BaseValue { get; }
        public float CurrentValue { get; }
        public ulong Revision { get; }
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

    public readonly struct GameplayModifierHandle : IEquatable<GameplayModifierHandle>
    {
        public GameplayModifierHandle(
            ulong value,
            GameplayEffectHandle sourceEffect,
            int priority,
            ulong insertionSequence)
        {
            Value = value;
            SourceEffect = sourceEffect;
            Priority = priority;
            InsertionSequence = insertionSequence;
        }

        public ulong Value { get; }
        public GameplayEffectHandle SourceEffect { get; }
        public int Priority { get; }
        public ulong InsertionSequence { get; }
        public bool IsValid => Value != 0 && SourceEffect.IsValid;
        public bool Equals(GameplayModifierHandle other) => Value == other.Value;
        public override bool Equals(object obj) => obj is GameplayModifierHandle other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value.ToString();
    }

    public readonly struct GameplayAttributeModifier
    {
        public GameplayAttributeModifier(
            GameplayModifierHandle handle,
            GameplayAttributeId attributeId,
            GameplayModifierOperation operation,
            float magnitude,
            GameplayClampBound clampBound,
            GameplayAttributeId liveMagnitudeAttribute,
            float liveCoefficient,
            float livePostAdd)
        {
            Handle = handle;
            AttributeId = attributeId;
            Operation = operation;
            Magnitude = magnitude;
            ClampBound = clampBound;
            LiveMagnitudeAttribute = liveMagnitudeAttribute;
            LiveCoefficient = liveCoefficient;
            LivePostAdd = livePostAdd;
        }

        public GameplayModifierHandle Handle { get; }
        public GameplayAttributeId AttributeId { get; }
        public GameplayModifierOperation Operation { get; }
        public float Magnitude { get; }
        public GameplayClampBound ClampBound { get; }
        public GameplayAttributeId LiveMagnitudeAttribute { get; }
        public float LiveCoefficient { get; }
        public float LivePostAdd { get; }
        public bool HasLiveMagnitude => LiveMagnitudeAttribute.IsValid;
    }

    public readonly struct GameplayAttributeMutation
    {
        public GameplayAttributeMutation(
            GameplayAttributeId attributeId,
            GameplayModifierOperation operation,
            float magnitude,
            GameplayClampBound clampBound = GameplayClampBound.Maximum)
        {
            AttributeId = attributeId;
            Operation = operation;
            Magnitude = magnitude;
            ClampBound = clampBound;
        }

        public GameplayAttributeId AttributeId { get; }
        public GameplayModifierOperation Operation { get; }
        public float Magnitude { get; }
        public GameplayClampBound ClampBound { get; }
    }

    public readonly struct GameplayAttributeChange
    {
        public GameplayAttributeChange(
            GameplayAttributeId attributeId,
            float beforeBase,
            float afterBase,
            float beforeCurrent,
            float afterCurrent,
            ulong revision,
            GameplayEffectHandle causeEffect)
        {
            AttributeId = attributeId;
            BeforeBase = beforeBase;
            AfterBase = afterBase;
            BeforeCurrent = beforeCurrent;
            AfterCurrent = afterCurrent;
            Revision = revision;
            CauseEffect = causeEffect;
        }

        public GameplayAttributeId AttributeId { get; }
        public float BeforeBase { get; }
        public float AfterBase { get; }
        public float BeforeCurrent { get; }
        public float AfterCurrent { get; }
        public ulong Revision { get; }
        public GameplayEffectHandle CauseEffect { get; }
    }

    public readonly struct GameplayAttributeStateSnapshot
    {
        public GameplayAttributeStateSnapshot(GameplayAttributeValue value)
        {
            Value = value;
        }

        public GameplayAttributeValue Value { get; }
    }
}
