using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Network;
using UnityEngine;
using UnityEngine.InputSystem;
using ThirdPersonGameplay.Tick;

namespace ThirdPersonCharacter.Pipeline.Input
{
    public sealed class CharacterInputStage : IDisposable
    {
        readonly CharacterInputProfile m_Profile;
        readonly CharacterInputSource m_InputSource;
        readonly CharacterInputRequestBuffer m_RequestBuffer = new CharacterInputRequestBuffer();
        readonly CharacterInputHistory m_History = new CharacterInputHistory();
        readonly List<string> m_ProfileErrors = new List<string>();
        readonly Dictionary<string, CharacterInputValue> m_LatchedInputValues = new Dictionary<string, CharacterInputValue>();
        readonly Dictionary<string, LatchedActionValue> m_LatchedActionValues = new Dictionary<string, LatchedActionValue>();
        readonly List<PendingRequestEvent> m_PendingRequestEvents = new List<PendingRequestEvent>();
        ulong m_LatchedRenderFrame = ulong.MaxValue;
        bool m_Active;
        bool m_ReportedProfileErrors;

        public CharacterInputStage(CharacterInputProfile profile, CharacterInputSource inputSource)
        {
            m_Profile = profile;
            m_InputSource = inputSource;
            m_History.SetCapacity(profile != null ? profile.InputHistoryCapacity : 1);
        }

        public CharacterInputProfile Profile => m_Profile;
        public InputActionAsset Actions => m_Profile != null ? m_Profile.SourceAsset : null;
        public CharacterInputRequestBuffer RequestBuffer => m_RequestBuffer;
        public CharacterInputHistory History => m_History;
        public CharacterInputSource InputSource => m_InputSource;

        public void Activate()
        {
            if (m_Active)
                return;

            if (m_InputSource == CharacterInputSource.LocalDevice)
                Actions?.Enable();
            m_Active = true;
        }

        public void Deactivate()
        {
            if (!m_Active)
                return;

            Actions?.Disable();
            m_RequestBuffer.Clear();
            m_History.Clear();
            ClearLatchedInput();
            m_Active = false;
        }

        public void BeginRenderFrame(ulong renderFrame)
        {
            if (m_LatchedRenderFrame == renderFrame)
                return;

            m_LatchedRenderFrame = renderFrame;
            m_LatchedInputValues.Clear();
            m_LatchedActionValues.Clear();

            if (m_InputSource != CharacterInputSource.LocalDevice || !m_Active)
            {
                m_PendingRequestEvents.Clear();
                return;
            }

            if (!ValidateProfile())
            {
                m_PendingRequestEvents.Clear();
                return;
            }

            LatchActionValues();
            LatchInputValues();
            LatchRequests();
        }

        public void Update(GameplayLogicTickContext context, CharacterPipelineFrame frame)
        {
            ExternalCharacterInputFact externalFact = GetLatestExternalFact(frame);
            ulong inputSequence = m_InputSource == CharacterInputSource.ExternalFacts && externalFact.IsValid
                ? externalFact.InputSequence
                : context.InputSequence;
            CharacterInputFrame inputFrame = new CharacterInputFrame();
            inputFrame.Begin(
                context,
                m_InputSource,
                inputSequence,
                m_InputSource == CharacterInputSource.LocalDevice ? Actions : null,
                m_InputSource == CharacterInputSource.LocalDevice && m_Active && Actions != null);

            m_RequestBuffer.CleanupExpired(context.LocalLogicTick);

            if (m_InputSource == CharacterInputSource.LocalDevice)
            {
                ApplyLatchedInputValues(inputFrame);
                ConsumePendingRequests(context, inputFrame);
            }
            else if (m_InputSource == CharacterInputSource.ExternalFacts && externalFact.IsValid)
            {
                ApplyExternalFact(externalFact, inputFrame);
            }

            frame.SetInput(inputFrame);
            m_History.SetCapacity(m_Profile != null ? m_Profile.InputHistoryCapacity : m_History.Capacity);
            m_History.Record(inputFrame);
            if (m_InputSource != CharacterInputSource.None)
                frame.Output.SyncFacts.CollectInputFrame(inputFrame);
        }

        static ExternalCharacterInputFact GetLatestExternalFact(CharacterPipelineFrame frame)
        {
            if (frame == null || frame.NetworkInput.Input.Facts.Count == 0)
                return default;

            return frame.NetworkInput.Input.Facts[frame.NetworkInput.Input.Facts.Count - 1];
        }

        void ApplyExternalFact(ExternalCharacterInputFact fact, CharacterInputFrame frame)
        {
            for (int i = 0; i < fact.InputValues.Length; i++)
            {
                CharacterInputValue value = fact.InputValues[i];
                switch (value.ValueType)
                {
                    case CharacterInputValueType.Bool:
                        frame.SetBool(value.InputValueId, value.BoolValue);
                        break;
                    case CharacterInputValueType.Float:
                        frame.SetFloat(value.InputValueId, value.FloatValue);
                        break;
                    case CharacterInputValueType.Vector2:
                        frame.SetVector2(value.InputValueId, value.Vector2Value);
                        break;
                }
            }

            for (int i = 0; i < fact.ActionRequests.Length; i++)
            {
                CharacterInputRequest request = fact.ActionRequests[i];
                m_RequestBuffer.Add(request);
                frame.AddRequest(request);
            }
        }

        public bool TryReadButton(InputActionAsset sourceAsset, string actionId, out bool value)
        {
            value = false;
            if (!TryFindAction(sourceAsset, actionId, out InputAction action))
                return false;

            return TryGetLatchedActionValue(action, out LatchedActionValue latchedValue) &&
                   latchedValue.TryGetBool(out value);
        }

        public bool TryReadFloat(InputActionAsset sourceAsset, string actionId, out float value)
        {
            value = 0f;
            if (!TryFindAction(sourceAsset, actionId, out InputAction action))
                return false;

            return TryGetLatchedActionValue(action, out LatchedActionValue latchedValue) &&
                   latchedValue.TryGetFloat(out value);
        }

        public bool TryReadVector2(InputActionAsset sourceAsset, string actionId, out Vector2 value)
        {
            value = Vector2.zero;
            if (!TryFindAction(sourceAsset, actionId, out InputAction action))
                return false;

            return TryGetLatchedActionValue(action, out LatchedActionValue latchedValue) &&
                   latchedValue.TryGetVector2(out value);
        }

        public bool TryGetLatchedVector2(string inputValueId, out Vector2 value)
        {
            value = Vector2.zero;
            if (string.IsNullOrEmpty(inputValueId) ||
                !m_LatchedInputValues.TryGetValue(inputValueId, out CharacterInputValue inputValue) ||
                inputValue.ValueType != CharacterInputValueType.Vector2)
                return false;

            value = inputValue.Vector2Value;
            return true;
        }

        public void Dispose()
        {
            Deactivate();
        }

        bool TryFindAction(InputActionAsset sourceAsset, string actionId, out InputAction action)
        {
            action = null;
            InputActionAsset actions = Actions;
            if (actions == null || sourceAsset == null || sourceAsset != actions || string.IsNullOrEmpty(actionId))
                return false;

            if (!Guid.TryParse(actionId, out Guid actionGuid))
                return false;

            action = sourceAsset.FindAction(actionGuid);
            return action != null;
        }

        bool TryGetLatchedActionValue(InputAction action, out LatchedActionValue value)
        {
            value = default;
            return action != null && m_LatchedActionValues.TryGetValue(action.id.ToString(), out value);
        }

        bool ValidateProfile()
        {
            if (m_Profile == null)
            {
                ReportProfileError("CharacterInputProfile is missing.");
                return false;
            }

            m_ProfileErrors.Clear();
            bool valid = m_Profile.CollectConfigurationErrors(m_ProfileErrors);
            if (valid)
                return true;

            if (!m_ReportedProfileErrors)
            {
                for (int i = 0; i < m_ProfileErrors.Count; i++)
                    Debug.LogError(m_ProfileErrors[i], m_Profile);
                m_ReportedProfileErrors = true;
            }

            return false;
        }

        void LatchActionValues()
        {
            InputActionAsset actions = Actions;
            if (actions == null)
                return;

            for (int mapIndex = 0; mapIndex < actions.actionMaps.Count; mapIndex++)
            {
                InputActionMap map = actions.actionMaps[mapIndex];
                if (map == null)
                    continue;

                for (int actionIndex = 0; actionIndex < map.actions.Count; actionIndex++)
                {
                    InputAction action = map.actions[actionIndex];
                    if (action != null)
                        m_LatchedActionValues[action.id.ToString()] = LatchedActionValue.FromAction(action);
                }
            }
        }

        void LatchInputValues()
        {
            IReadOnlyList<CharacterInputValueDefinition> inputValues = m_Profile.InputValues;
            for (int i = 0; i < inputValues.Count; i++)
            {
                CharacterInputValueDefinition inputValue = inputValues[i];
                if (inputValue == null || !inputValue.TryResolveAction(Actions, out InputAction action, out _))
                    continue;

                if (!TryGetLatchedActionValue(action, out LatchedActionValue value))
                    continue;

                switch (inputValue.ValueType)
                {
                    case CharacterInputValueType.Bool:
                        if (value.TryGetBool(out bool boolValue))
                            m_LatchedInputValues[inputValue.InputValueId] = CharacterInputValue.Bool(inputValue.InputValueId, boolValue);
                        else
                            ReportProfileError($"{m_Profile.name}: input value '{inputValue.InputValueId}' could not be read as {inputValue.ValueType}.");
                        break;
                    case CharacterInputValueType.Float:
                        if (value.TryGetFloat(out float floatValue))
                            m_LatchedInputValues[inputValue.InputValueId] = CharacterInputValue.Float(inputValue.InputValueId, floatValue);
                        else
                            ReportProfileError($"{m_Profile.name}: input value '{inputValue.InputValueId}' could not be read as {inputValue.ValueType}.");
                        break;
                    case CharacterInputValueType.Vector2:
                        if (value.TryGetVector2(out Vector2 vector2Value))
                            m_LatchedInputValues[inputValue.InputValueId] = CharacterInputValue.Vector2(inputValue.InputValueId, vector2Value);
                        else
                            ReportProfileError($"{m_Profile.name}: input value '{inputValue.InputValueId}' could not be read as {inputValue.ValueType}.");
                        break;
                }
            }
        }

        void LatchRequests()
        {
            IReadOnlyList<CharacterActionRequestDefinition> requests = m_Profile.ActionRequests;
            for (int i = 0; i < requests.Count; i++)
            {
                CharacterActionRequestDefinition definition = requests[i];
                if (definition == null || !definition.TryResolveAction(Actions, out InputAction action, out _))
                    continue;

                if (!IsRequestTriggered(action))
                    continue;

                m_PendingRequestEvents.Add(new PendingRequestEvent(
                    definition.RequestId,
                    definition.BufferSeconds,
                    definition.Priority));
            }
        }

        static bool IsRequestTriggered(InputAction action)
        {
            return action.WasPressedThisFrame() || action.triggered;
        }

        void ApplyLatchedInputValues(CharacterInputFrame frame)
        {
            foreach (KeyValuePair<string, CharacterInputValue> pair in m_LatchedInputValues)
            {
                CharacterInputValue inputValue = pair.Value;
                switch (inputValue.ValueType)
                {
                    case CharacterInputValueType.Bool:
                        frame.SetBool(inputValue.InputValueId, inputValue.BoolValue);
                        break;
                    case CharacterInputValueType.Float:
                        frame.SetFloat(inputValue.InputValueId, inputValue.FloatValue);
                        break;
                    case CharacterInputValueType.Vector2:
                        frame.SetVector2(inputValue.InputValueId, inputValue.Vector2Value);
                        break;
                }
            }
        }

        void ConsumePendingRequests(GameplayLogicTickContext context, CharacterInputFrame frame)
        {
            if (m_PendingRequestEvents.Count == 0)
                return;

            for (int i = 0; i < m_PendingRequestEvents.Count; i++)
            {
                PendingRequestEvent pending = m_PendingRequestEvents[i];
                ulong expireLocalLogicTick = CalculateExpireLocalLogicTick(context.LocalLogicTick, context.FixedDeltaSeconds, pending.BufferSeconds);
                CharacterInputRequest request = new CharacterInputRequest(
                    pending.RequestId,
                    context.LocalLogicTick,
                    context.InputSequence,
                    expireLocalLogicTick,
                    pending.BufferSeconds,
                    pending.Priority);
                m_RequestBuffer.Add(request);
                frame.AddRequest(request);
            }

            m_PendingRequestEvents.Clear();
        }

        void ClearLatchedInput()
        {
            m_LatchedInputValues.Clear();
            m_LatchedActionValues.Clear();
            m_PendingRequestEvents.Clear();
        }

        static ulong CalculateExpireLocalLogicTick(ulong localLogicTick, float fixedDeltaSeconds, float bufferSeconds)
        {
            if (bufferSeconds <= 0f)
                return localLogicTick;

            int tickDuration = fixedDeltaSeconds > 0f ? Mathf.CeilToInt(bufferSeconds / fixedDeltaSeconds) : 1;
            return localLogicTick + (ulong)Mathf.Max(1, tickDuration);
        }

        void ReportProfileError(string message)
        {
            if (m_ReportedProfileErrors)
                return;

            m_ReportedProfileErrors = true;
            Debug.LogError(message, m_Profile);
        }

        readonly struct PendingRequestEvent
        {
            public PendingRequestEvent(string requestId, float bufferSeconds, int priority)
            {
                RequestId = requestId;
                BufferSeconds = bufferSeconds;
                Priority = priority;
            }

            public string RequestId { get; }
            public float BufferSeconds { get; }
            public int Priority { get; }
        }

        readonly struct LatchedActionValue
        {
            readonly bool m_HasBool;
            readonly bool m_BoolValue;
            readonly bool m_HasFloat;
            readonly float m_FloatValue;
            readonly bool m_HasVector2;
            readonly Vector2 m_Vector2Value;

            LatchedActionValue(
                bool hasBool,
                bool boolValue,
                bool hasFloat,
                float floatValue,
                bool hasVector2,
                Vector2 vector2Value)
            {
                m_HasBool = hasBool;
                m_BoolValue = boolValue;
                m_HasFloat = hasFloat;
                m_FloatValue = floatValue;
                m_HasVector2 = hasVector2;
                m_Vector2Value = vector2Value;
            }

            public static LatchedActionValue FromAction(InputAction action)
            {
                bool hasBool = false;
                bool boolValue = false;
                bool hasFloat = false;
                float floatValue = 0f;
                bool hasVector2 = false;
                Vector2 vector2Value = Vector2.zero;

                try
                {
                    boolValue = action.IsPressed();
                    hasBool = true;
                }
                catch (InvalidOperationException)
                {
                }

                try
                {
                    floatValue = action.ReadValue<float>();
                    hasFloat = true;
                }
                catch (InvalidOperationException)
                {
                }

                try
                {
                    vector2Value = action.ReadValue<Vector2>();
                    hasVector2 = true;
                }
                catch (InvalidOperationException)
                {
                }

                return new LatchedActionValue(hasBool, boolValue, hasFloat, floatValue, hasVector2, vector2Value);
            }

            public bool TryGetBool(out bool value)
            {
                value = m_BoolValue;
                return m_HasBool;
            }

            public bool TryGetFloat(out float value)
            {
                value = m_FloatValue;
                return m_HasFloat;
            }

            public bool TryGetVector2(out Vector2 value)
            {
                value = m_Vector2Value;
                return m_HasVector2;
            }
        }
    }
}
