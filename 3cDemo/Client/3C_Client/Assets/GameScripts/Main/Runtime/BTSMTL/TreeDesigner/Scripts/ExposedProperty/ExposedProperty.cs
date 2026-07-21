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

    public enum PipelineBlackboardVariableAuthority
    {
        LocalOnly,
        ClientPredicted,
        ServerAuthoritative,
        PresentationOnly
    }

    public enum PipelineBlackboardVariableSyncPolicy
    {
        None,
        ConfigVersion,
        InputDerived,
        SyncFact,
        ReplicatedCue,
        CorrectionOnly
    }

    public enum PipelineBlackboardFactProjectionKind
    {
        None,
        ActionWindow
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
            bool inputDerived = declaration.BlackboardSyncPolicy == PipelineBlackboardVariableSyncPolicy.InputDerived;
            if (!inputDerived)
            {
                if (!string.IsNullOrWhiteSpace(declaration.InputValueId))
                    error = "Only InputDerived Blackboard declarations may retain an InputValueId.";
                return string.IsNullOrEmpty(error);
            }
            if (string.IsNullOrWhiteSpace(declaration.InputValueId))
                error = "InputDerived Blackboard declaration requires a stable InputValueId.";
            else if (declaration.BlackboardScope != PipelineBlackboardVariableScope.Character)
                error = "InputDerived Blackboard declaration requires Character scope.";
            else if (declaration.BlackboardLifetime != PipelineBlackboardVariableLifetime.Spawn)
                error = "InputDerived Blackboard declaration requires Spawn lifetime.";
            else if (declaration.BlackboardAuthority == PipelineBlackboardVariableAuthority.PresentationOnly)
                error = "InputDerived Blackboard declaration cannot use PresentationOnly authority.";
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
        protected PipelineBlackboardVariableAuthority m_BlackboardAuthority = PipelineBlackboardVariableAuthority.LocalOnly;
        public PipelineBlackboardVariableAuthority BlackboardAuthority => m_BlackboardAuthority;

        [SerializeField]
        protected PipelineBlackboardVariableSyncPolicy m_BlackboardSyncPolicy = PipelineBlackboardVariableSyncPolicy.None;
        public PipelineBlackboardVariableSyncPolicy BlackboardSyncPolicy => m_BlackboardSyncPolicy;

        [SerializeField]
        protected string m_InputValueId;
        public string InputValueId => m_InputValueId ?? string.Empty;

        [SerializeField]
        protected PipelineBlackboardFactProjectionKind m_BlackboardFactProjection;
        public PipelineBlackboardFactProjectionKind BlackboardFactProjection => m_BlackboardFactProjection;

        [SerializeField]
        protected string m_ActionWindowType;
        public string ActionWindowType => m_ActionWindowType ?? string.Empty;

        [SerializeField]
        protected string m_ActionWindowId;
        public string ActionWindowId => m_ActionWindowId ?? string.Empty;

        [SerializeField]
        protected ulong m_ActionWindowDigest;
        public ulong ActionWindowDigest => m_ActionWindowDigest;

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
        public void ConfigurePipelineBlackboard(
            string key,
            PipelineBlackboardVariableScope scope,
            PipelineBlackboardVariableLifetime lifetime,
            PipelineBlackboardVariableAuthority authority,
            PipelineBlackboardVariableSyncPolicy syncPolicy,
            string inputValueId,
            string categoryPath)
        {
            m_BlackboardKey = key ?? string.Empty;
            m_BlackboardScope = scope;
            m_BlackboardLifetime = lifetime;
            m_BlackboardAuthority = authority;
            m_BlackboardSyncPolicy = syncPolicy;
            m_InputValueId = inputValueId ?? string.Empty;
            m_BlackboardCategoryPath = categoryPath ?? string.Empty;
        }

        public void ConfigureFactProjection(
            PipelineBlackboardFactProjectionKind projection,
            string windowType,
            string windowId,
            ulong digest)
        {
            m_BlackboardFactProjection = projection;
            m_ActionWindowType = windowType ?? string.Empty;
            m_ActionWindowId = windowId ?? string.Empty;
            m_ActionWindowDigest = digest;
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
            if (declaration == null || declaration.BlackboardFactProjection == PipelineBlackboardFactProjectionKind.None)
                return true;

            if (declaration.BlackboardFactProjection != PipelineBlackboardFactProjectionKind.ActionWindow)
            {
                error = $"Unsupported fact projection '{declaration.BlackboardFactProjection}'.";
                return false;
            }

            if (declaration.ValueType != typeof(bool))
                error = "ActionWindow projection requires a Bool declaration.";
            else if (declaration.BlackboardScope != PipelineBlackboardVariableScope.Frame ||
                     declaration.BlackboardLifetime != PipelineBlackboardVariableLifetime.Frame)
                error = "ActionWindow projection requires Frame scope and Frame lifetime.";
            else if (declaration.BlackboardSyncPolicy != PipelineBlackboardVariableSyncPolicy.SyncFact)
                error = "ActionWindow projection requires SyncFact policy.";
            else if (string.IsNullOrWhiteSpace(declaration.ActionWindowType))
                error = "ActionWindow projection requires WindowType.";
            else if (string.IsNullOrWhiteSpace(declaration.ActionWindowId))
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
