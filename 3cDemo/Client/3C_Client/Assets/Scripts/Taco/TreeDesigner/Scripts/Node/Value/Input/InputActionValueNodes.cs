using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TreeDesigner
{
    [Serializable]
    public abstract class InputActionValueNode : ValueNode
    {
        [NonSerialized]
        bool m_ReportedBindingError;

        [NonSerialized]
        bool m_ReportedSourceError;

        [NonSerialized]
        bool m_ReportedReadError;

        public InputActionBindingModule Binding => GetModule<InputActionBindingModule>();

        protected override IEnumerable<NodeModule> CreateDefaultModules()
        {
            yield return new InputActionBindingModule();
        }

        public void BindAction(InputAction action)
        {
            Binding?.Bind(action);
            ResetReports();
#if UNITY_EDITOR
            OnNodeChangedCallback();
#endif
        }

        protected bool TryGetBinding(out InputActionBindingModule binding)
        {
            binding = Binding;
            if (binding == null)
            {
                ReportBindingError("InputAction binding module is missing.");
                return false;
            }

            if (!binding.TryResolveAction(out _, out string error))
            {
                ReportBindingError(error);
                return false;
            }

            return true;
        }

        protected bool TryGetValueSource(out IInputActionValueSource valueSource)
        {
            valueSource = null;
            if (Owner != null && Owner.TryGetUser(out valueSource) && valueSource != null)
                return true;

            ReportSourceError("InputAction value source is missing from graph user.");
            return false;
        }

        protected void ReportReadError(string message)
        {
            if (m_ReportedReadError)
                return;

            m_ReportedReadError = true;
            Debug.LogError($"{GetType().Name}: {message}", Owner);
        }

        void ReportBindingError(string message)
        {
            if (m_ReportedBindingError)
                return;

            m_ReportedBindingError = true;
            Debug.LogError($"{GetType().Name}: {message}", Owner);
        }

        void ReportSourceError(string message)
        {
            if (m_ReportedSourceError)
                return;

            m_ReportedSourceError = true;
            Debug.LogError($"{GetType().Name}: {message}", Owner);
        }

        void ResetReports()
        {
            m_ReportedBindingError = false;
            m_ReportedSourceError = false;
            m_ReportedReadError = false;
        }
    }

    [Serializable]
    [NodeName("Input Button")]
    [NodePath("Base/Value/Input/Button")]
    public sealed class InputActionButtonNode : InputActionValueNode
    {
        [SerializeField, PropertyPort(PortDirection.Output, "Pressed"), ReadOnly]
        BoolPropertyPort m_Output = new BoolPropertyPort();

        protected override void OutputValue()
        {
            base.OutputValue();
            m_Output.Value = false;

            if (!TryGetBinding(out InputActionBindingModule binding) || !TryGetValueSource(out IInputActionValueSource source))
                return;

            if (source.TryReadButton(binding.Asset, binding.ActionId, out bool value))
                m_Output.Value = value;
            else
                ReportReadError($"Could not read button value for '{binding.DisplayName}'.");
        }
    }

    [Serializable]
    [NodeName("Input Float")]
    [NodePath("Base/Value/Input/Float")]
    public sealed class InputActionFloatNode : InputActionValueNode
    {
        [SerializeField, PropertyPort(PortDirection.Output, "Value"), ReadOnly]
        FloatPropertyPort m_Output = new FloatPropertyPort();

        protected override void OutputValue()
        {
            base.OutputValue();
            m_Output.Value = 0f;

            if (!TryGetBinding(out InputActionBindingModule binding) || !TryGetValueSource(out IInputActionValueSource source))
                return;

            if (source.TryReadFloat(binding.Asset, binding.ActionId, out float value))
                m_Output.Value = value;
            else
                ReportReadError($"Could not read float value for '{binding.DisplayName}'.");
        }
    }

    [Serializable]
    [NodeName("Input Vector2")]
    [NodePath("Base/Value/Input/Vector2")]
    public sealed class InputActionVector2Node : InputActionValueNode
    {
        [SerializeField, PropertyPort(PortDirection.Output, "Value"), ReadOnly]
        Vector2PropertyPort m_Output = new Vector2PropertyPort();

        protected override void OutputValue()
        {
            base.OutputValue();
            m_Output.Value = Vector2.zero;

            if (!TryGetBinding(out InputActionBindingModule binding) || !TryGetValueSource(out IInputActionValueSource source))
                return;

            if (source.TryReadVector2(binding.Asset, binding.ActionId, out Vector2 value))
                m_Output.Value = value;
            else
                ReportReadError($"Could not read Vector2 value for '{binding.DisplayName}'.");
        }
    }
}
