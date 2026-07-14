using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Input
{
    public readonly struct CharacterInputValue
    {
        CharacterInputValue(string inputValueId, CharacterInputValueType valueType, bool boolValue, float floatValue, Vector2 vector2Value)
        {
            InputValueId = inputValueId;
            ValueType = valueType;
            BoolValue = boolValue;
            FloatValue = floatValue;
            Vector2Value = vector2Value;
        }

        public string InputValueId { get; }
        public CharacterInputValueType ValueType { get; }
        public bool BoolValue { get; }
        public float FloatValue { get; }
        public Vector2 Vector2Value { get; }

        public static CharacterInputValue Bool(string inputValueId, bool value)
        {
            return new CharacterInputValue(inputValueId, CharacterInputValueType.Bool, value, 0f, UnityEngine.Vector2.zero);
        }

        public static CharacterInputValue Float(string inputValueId, float value)
        {
            return new CharacterInputValue(inputValueId, CharacterInputValueType.Float, false, value, UnityEngine.Vector2.zero);
        }

        public static CharacterInputValue Vector2(string inputValueId, UnityEngine.Vector2 value)
        {
            return new CharacterInputValue(inputValueId, CharacterInputValueType.Vector2, false, 0f, value);
        }
    }
}
