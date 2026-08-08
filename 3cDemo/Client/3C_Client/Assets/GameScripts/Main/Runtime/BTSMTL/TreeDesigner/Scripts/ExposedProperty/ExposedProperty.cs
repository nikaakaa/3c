using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TreeDesigner
{
    public enum PipelineBlackboardVariableScope
    {
        Graph,
        State,
        ActionInstance,
        Character,
        Frame,
        AIController,
        AITick
    }

    public enum PipelineBlackboardVariableLifetime
    {
        Config,
        Spawn,
        StateEnterToExit,
        ActionInstance,
        Frame,
        ManualClear,
        GraphInstance,
        AIController,
        AITick
    }

    public enum PipelineBlackboardFactProjectionKind
    {
        ActionWindow
    }

    [Serializable]
    public sealed class PipelineBlackboardInputBinding
    {
        [SerializeField]
        string m_InputValueId;

        public string InputValueId => m_InputValueId ?? string.Empty;
        public bool IsDefined => !string.IsNullOrWhiteSpace(m_InputValueId);

        public PipelineBlackboardInputBinding(string inputValueId)
        {
            m_InputValueId = string.IsNullOrWhiteSpace(inputValueId)
                ? throw new ArgumentException("Blackboard Input Binding requires a stable InputValueId.", nameof(inputValueId))
                : inputValueId.Trim();
        }
    }

    [Serializable]
    public sealed class PipelineBlackboardFactProjection
    {
        [SerializeField]
        PipelineBlackboardFactProjectionKind m_Kind;

        [SerializeField]
        string m_ActionWindowType;

        [SerializeField]
        string m_ActionWindowId;

        [SerializeField]
        ulong m_ActionWindowDigest;

        public PipelineBlackboardFactProjectionKind Kind => m_Kind;
        public string ActionWindowType => m_ActionWindowType ?? string.Empty;
        public string ActionWindowId => m_ActionWindowId ?? string.Empty;
        public ulong ActionWindowDigest => m_ActionWindowDigest;
        public bool IsDefined =>
            !string.IsNullOrWhiteSpace(m_ActionWindowType) ||
            !string.IsNullOrWhiteSpace(m_ActionWindowId) ||
            m_ActionWindowDigest != 0;

        public PipelineBlackboardFactProjection(
            PipelineBlackboardFactProjectionKind kind,
            string actionWindowType,
            string actionWindowId,
            ulong actionWindowDigest)
        {
            m_Kind = kind;
            m_ActionWindowType = actionWindowType ?? string.Empty;
            m_ActionWindowId = actionWindowId ?? string.Empty;
            m_ActionWindowDigest = actionWindowDigest;
        }
    }

    public interface IPipelineBlackboardRuntimeAccess
    {
        void RegisterPipelineBlackboardVariables(BaseGraph graph, IReadOnlyList<BaseExposedProperty> variables);
        void UnregisterPipelineBlackboardGraph(BaseGraph graph);
        bool TryResolvePipelineBlackboardDeclaration(PipelineBlackboardVariableReference reference, out BaseExposedProperty declaration);
        bool TryGetPipelineBlackboardValue(BaseGraph accessGraph, PipelineBlackboardVariableReference reference, Type expectedType, out object value);
        bool SetPipelineBlackboardValue(
            BaseGraph accessGraph,
            PipelineBlackboardVariableReference reference,
            object value,
            UnityEngine.Object factContext);
        void NotifyPipelineBlackboardStateEntered(StateMachineExecutionScope scope);
        void NotifyPipelineBlackboardStateExited(StateMachineExecutionScope scope);
    }

    [Serializable]
    public struct PipelineBlackboardVariableReference
    {
        [SerializeField]
        string m_DeclarationId;

        [SerializeField]
        string m_DeclarationOwnerId;

        [SerializeField]
        string m_DisplayKey;

        [SerializeField]
        string m_ValueTypeName;

        public string DeclarationId => m_DeclarationId ?? string.Empty;
        public string DeclarationOwnerId => m_DeclarationOwnerId ?? string.Empty;
        public string DisplayKey => m_DisplayKey ?? string.Empty;
        public string ValueTypeName => m_ValueTypeName ?? string.Empty;
        public bool IsValid => !string.IsNullOrEmpty(DeclarationId) && !string.IsNullOrEmpty(DeclarationOwnerId);

        public PipelineBlackboardVariableReference(BaseExposedProperty declaration)
        {
            m_DeclarationId = declaration?.DeclarationId ?? string.Empty;
            m_DeclarationOwnerId = declaration?.DeclarationOwnerId ?? string.Empty;
            m_DisplayKey = declaration?.BlackboardKey ?? string.Empty;
            m_ValueTypeName = declaration?.ValueType?.AssemblyQualifiedName ?? string.Empty;
        }

        public bool MatchesValueType(Type type)
        {
            if (type == null || string.IsNullOrEmpty(ValueTypeName))
                return false;

            return string.Equals(ValueTypeName, type.AssemblyQualifiedName, StringComparison.Ordinal) ||
                   string.Equals(ValueTypeName, type.FullName, StringComparison.Ordinal);
        }

        public static PipelineBlackboardVariableReference None => default;
    }

    public static class PipelineBlackboardVariablePolicy
    {
        public static bool IsValid(PipelineBlackboardVariableScope scope, PipelineBlackboardVariableLifetime lifetime)
        {
            switch (scope)
            {
                case PipelineBlackboardVariableScope.Character:
                    return lifetime == PipelineBlackboardVariableLifetime.Config ||
                           lifetime == PipelineBlackboardVariableLifetime.Spawn ||
                           lifetime == PipelineBlackboardVariableLifetime.ManualClear;
                case PipelineBlackboardVariableScope.Graph:
                    return lifetime == PipelineBlackboardVariableLifetime.Config ||
                           lifetime == PipelineBlackboardVariableLifetime.GraphInstance;
                case PipelineBlackboardVariableScope.State:
                    return lifetime == PipelineBlackboardVariableLifetime.StateEnterToExit;
                case PipelineBlackboardVariableScope.ActionInstance:
                    return lifetime == PipelineBlackboardVariableLifetime.ActionInstance;
                case PipelineBlackboardVariableScope.Frame:
                    return lifetime == PipelineBlackboardVariableLifetime.Frame;
                case PipelineBlackboardVariableScope.AIController:
                    return lifetime == PipelineBlackboardVariableLifetime.AIController;
                case PipelineBlackboardVariableScope.AITick:
                    return lifetime == PipelineBlackboardVariableLifetime.AITick;
                default:
                    return false;
            }
        }

        public static PipelineBlackboardVariableLifetime DefaultLifetime(PipelineBlackboardVariableScope scope)
        {
            switch (scope)
            {
                case PipelineBlackboardVariableScope.Character:
                    return PipelineBlackboardVariableLifetime.Config;
                case PipelineBlackboardVariableScope.Graph:
                    return PipelineBlackboardVariableLifetime.Config;
                case PipelineBlackboardVariableScope.State:
                    return PipelineBlackboardVariableLifetime.StateEnterToExit;
                case PipelineBlackboardVariableScope.ActionInstance:
                    return PipelineBlackboardVariableLifetime.ActionInstance;
                case PipelineBlackboardVariableScope.Frame:
                    return PipelineBlackboardVariableLifetime.Frame;
                case PipelineBlackboardVariableScope.AIController:
                    return PipelineBlackboardVariableLifetime.AIController;
                case PipelineBlackboardVariableScope.AITick:
                    return PipelineBlackboardVariableLifetime.AITick;
                default:
                    throw new ArgumentOutOfRangeException(nameof(scope), scope, null);
            }
        }

        public static bool TryValidateInputBinding(BaseExposedProperty declaration, out string error)
        {
            error = string.Empty;
            if (declaration == null)
            {
                error = "Blackboard declaration is missing.";
                return false;
            }
            if (declaration.InputBinding == null)
                return true;
            if (string.IsNullOrWhiteSpace(declaration.InputBinding.InputValueId))
                error = "Blackboard Input Binding requires a stable InputValueId.";
            else if (declaration.BlackboardScope != PipelineBlackboardVariableScope.Character)
                error = "Blackboard Input Binding requires Character scope.";
            else if (declaration.BlackboardLifetime != PipelineBlackboardVariableLifetime.Spawn)
                error = "Blackboard Input Binding requires Spawn lifetime.";
            return string.IsNullOrEmpty(error);
        }
    }

    [Serializable]
    public partial class BaseExposedProperty
    {
        [SerializeField]
        protected string m_GUID;
        public string GUID { get => m_GUID; set => m_GUID = value; }
        public string DeclarationId => m_GUID ?? string.Empty;

        [SerializeField]
        protected string m_Name;
        public string Name { get => m_Name; set => m_Name = value; }

        [SerializeField]
        protected string m_BlackboardKey;
        public string BlackboardKey => string.IsNullOrEmpty(m_BlackboardKey) ? m_Name : m_BlackboardKey;

        [SerializeField]
        protected PipelineBlackboardVariableScope m_BlackboardScope = PipelineBlackboardVariableScope.Graph;
        public PipelineBlackboardVariableScope BlackboardScope => m_BlackboardScope;

        [SerializeField]
        protected PipelineBlackboardVariableLifetime m_BlackboardLifetime = PipelineBlackboardVariableLifetime.Config;
        public PipelineBlackboardVariableLifetime BlackboardLifetime => m_BlackboardLifetime;

        [SerializeField]
        protected PipelineBlackboardInputBinding m_InputBinding;
        public PipelineBlackboardInputBinding InputBinding =>
            m_InputBinding?.IsDefined == true ? m_InputBinding : null;
        public string InputValueId => InputBinding?.InputValueId ?? string.Empty;

        [SerializeField]
        protected PipelineBlackboardFactProjection m_FactProjection;
        public PipelineBlackboardFactProjection FactProjection =>
            m_FactProjection?.IsDefined == true ? m_FactProjection : null;

        [SerializeField]
        protected string m_BlackboardCategoryPath;
        public string BlackboardCategoryPath => m_BlackboardCategoryPath ?? string.Empty;

        [NonSerialized]
        protected BaseGraph m_Owner;
        public BaseGraph Owner => m_Owner;
        public string DeclarationOwnerId => m_Owner?.GraphAuthoringId ?? string.Empty;

        public virtual Type ValueType => null;

        public BaseExposedProperty() { }

        public virtual void Init(BaseGraph tree)
        {
            m_Owner = tree;
        }
        public virtual void Dispose()
        {
            m_Owner = null;
        }
        public virtual object GetValue()
        {
            return null;
        }
        public virtual void SetValue(object value) { }

#if UNITY_EDITOR
        public void ConfigureDeclaration(
            string key,
            PipelineBlackboardVariableScope scope,
            PipelineBlackboardVariableLifetime lifetime,
            string categoryPath)
        {
            m_BlackboardKey = key ?? string.Empty;
            m_BlackboardScope = scope;
            m_BlackboardLifetime = lifetime;
            m_BlackboardCategoryPath = categoryPath ?? string.Empty;
        }

        public void ConfigureInputBinding(string inputValueId)
        {
            m_InputBinding = new PipelineBlackboardInputBinding(inputValueId);
        }

        public void ClearInputBinding()
        {
            m_InputBinding = null;
        }

        public void ConfigureFactProjection(
            PipelineBlackboardFactProjectionKind projection,
            string windowType,
            string windowId,
            ulong digest)
        {
            m_FactProjection = new PipelineBlackboardFactProjection(projection, windowType, windowId, digest);
        }

        public void ClearFactProjection()
        {
            m_FactProjection = null;
        }
#endif

        public PipelineBlackboardVariableReference CreateBlackboardReference()
        {
            return new PipelineBlackboardVariableReference(this);
        }

        public static implicit operator bool(BaseExposedProperty exists) => exists != null;
    }

    public static class PipelineBlackboardFactProjectionPolicy
    {
        public static bool TryValidate(BaseExposedProperty declaration, out string error)
        {
            error = string.Empty;
            if (declaration == null || declaration.FactProjection == null)
                return true;

            if (declaration.FactProjection.Kind != PipelineBlackboardFactProjectionKind.ActionWindow)
            {
                error = $"Unsupported fact projection '{declaration.FactProjection.Kind}'.";
                return false;
            }

            if (declaration.ValueType != typeof(bool))
                error = "ActionWindow projection requires a Bool declaration.";
            else if (declaration.BlackboardScope != PipelineBlackboardVariableScope.Frame ||
                     declaration.BlackboardLifetime != PipelineBlackboardVariableLifetime.Frame)
                error = "ActionWindow projection requires Frame scope and Frame lifetime.";
            else if (string.IsNullOrWhiteSpace(declaration.FactProjection.ActionWindowType))
                error = "ActionWindow projection requires WindowType.";
            else if (string.IsNullOrWhiteSpace(declaration.FactProjection.ActionWindowId))
                error = "ActionWindow projection requires WindowId.";

            return string.IsNullOrEmpty(error);
        }
    }

    [Serializable]
    public abstract class BaseExposedProperty<T> : BaseExposedProperty
    {
        [SerializeField]
        protected T m_Value;
        public T Value { get => m_Value; set => m_Value = value; }

        public override Type ValueType => typeof(T);

        public override object GetValue()
        {
            return m_Value;
        }
        public override void SetValue(object value)
        {
            m_Value = (T)value;
        }
    }

    [Serializable]
    [PropertyColor(210, 210, 210)]
    public class BoolExposedProperty : BaseExposedProperty<bool>
    {
        public BoolExposedProperty() { }
    }

    [Serializable]
    [PropertyColor(148, 129, 230)]
    public class IntExposedProperty : BaseExposedProperty<int>
    {
        public IntExposedProperty() { }
    }

    [Serializable]
    [PropertyColor(132, 228, 231)]
    public class FloatExposedProperty : BaseExposedProperty<float>
    {
        public FloatExposedProperty() { }
    }

    [Serializable]
    [PropertyColor(252, 218, 110)]
    public class StringExposedProperty : BaseExposedProperty<string>
    {
        public StringExposedProperty() { }
    }

    [Serializable]
    [PropertyColor(246, 255, 154)]
    public class Vector3ExposedProperty : BaseExposedProperty<Vector3>
    {
        public Vector3ExposedProperty() { }
    }

    [Serializable]
    [PropertyColor(154, 239, 146)]
    public class Vector2ExposedProperty : BaseExposedProperty<Vector2>
    {
        public Vector2ExposedProperty() { }
    }

    [Serializable]
    [PropertyColor(132, 228, 231)]
    public class FloatListExposedProperty : BaseExposedProperty<List<float>>
    {
        public FloatListExposedProperty() { }
    }

    [Serializable]
    [PropertyColor(252, 218, 110)]
    public class StringListExposedProperty : BaseExposedProperty<List<string>>
    {
        public StringListExposedProperty() { }
    }

    [Serializable]
    [PropertyColor(252, 218, 110)]
    public class AnimationCurveExposedProperty : BaseExposedProperty<AnimationCurve>
    {
        public AnimationCurveExposedProperty() { }
    }
}
