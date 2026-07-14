using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Network;
using UnityEngine;
using UnityEngine.InputSystem;
using ThirdPersonGameplay.Tick;

namespace ThirdPersonCharacter.Pipeline.Input
{
    public sealed class CharacterInputFrame
    {
        readonly Dictionary<string, CharacterInputValue> m_InputValues = new Dictionary<string, CharacterInputValue>();
        readonly List<CharacterInputRequest> m_NewRequests = new List<CharacterInputRequest>();

        public CharacterInputFrame()
        {
        }

        CharacterInputFrame(CharacterInputFrame source)
        {
            LocalLogicTick = source.LocalLogicTick;
            InputSequence = source.InputSequence;
            InputSource = source.InputSource;
            SourceAsset = source.SourceAsset;
            ActionsEnabled = source.ActionsEnabled;

            foreach (KeyValuePair<string, CharacterInputValue> pair in source.m_InputValues)
                m_InputValues.Add(pair.Key, pair.Value);

            m_NewRequests.AddRange(source.m_NewRequests);
        }

        public ulong LocalLogicTick { get; private set; }
        public ulong InputSequence { get; private set; }
        public CharacterInputSource InputSource { get; private set; }
        public InputActionAsset SourceAsset { get; private set; }
        public bool ActionsEnabled { get; private set; }
        public IEnumerable<CharacterInputValue> InputValues => m_InputValues.Values;
        public IReadOnlyList<CharacterInputRequest> NewRequests => m_NewRequests;

        public void Begin(
            GameplayLogicTickContext context,
            CharacterInputSource inputSource,
            ulong inputSequence,
            InputActionAsset sourceAsset,
            bool actionsEnabled)
        {
            LocalLogicTick = context.LocalLogicTick;
            InputSequence = inputSequence;
            InputSource = inputSource;
            SourceAsset = sourceAsset;
            ActionsEnabled = actionsEnabled;
            m_InputValues.Clear();
            m_NewRequests.Clear();
        }

        public CharacterInputFrame Clone()
        {
            return new CharacterInputFrame(this);
        }

        public void SetBool(string inputValueId, bool value)
        {
            if (!string.IsNullOrEmpty(inputValueId))
                m_InputValues[inputValueId] = CharacterInputValue.Bool(inputValueId, value);
        }

        public void SetFloat(string inputValueId, float value)
        {
            if (!string.IsNullOrEmpty(inputValueId))
                m_InputValues[inputValueId] = CharacterInputValue.Float(inputValueId, value);
        }

        public void SetVector2(string inputValueId, Vector2 value)
        {
            if (!string.IsNullOrEmpty(inputValueId))
                m_InputValues[inputValueId] = CharacterInputValue.Vector2(inputValueId, value);
        }

        public void AddRequest(CharacterInputRequest request)
        {
            if (!string.IsNullOrEmpty(request.RequestId))
                m_NewRequests.Add(request);
        }

        public bool TryGetBool(string inputValueId, out bool value)
        {
            value = false;
            if (!TryGetInputValue(inputValueId, CharacterInputValueType.Bool, out CharacterInputValue inputValue))
                return false;

            value = inputValue.BoolValue;
            return true;
        }

        public bool TryGetFloat(string inputValueId, out float value)
        {
            value = 0f;
            if (!TryGetInputValue(inputValueId, CharacterInputValueType.Float, out CharacterInputValue inputValue))
                return false;

            value = inputValue.FloatValue;
            return true;
        }

        public bool TryGetVector2(string inputValueId, out Vector2 value)
        {
            value = Vector2.zero;
            if (!TryGetInputValue(inputValueId, CharacterInputValueType.Vector2, out CharacterInputValue inputValue))
                return false;

            value = inputValue.Vector2Value;
            return true;
        }

        bool TryGetInputValue(string inputValueId, CharacterInputValueType expectedType, out CharacterInputValue inputValue)
        {
            inputValue = default;
            if (string.IsNullOrEmpty(inputValueId) || !m_InputValues.TryGetValue(inputValueId, out CharacterInputValue foundInputValue))
                return false;

            inputValue = foundInputValue;
            return inputValue.ValueType == expectedType;
        }
    }
}
