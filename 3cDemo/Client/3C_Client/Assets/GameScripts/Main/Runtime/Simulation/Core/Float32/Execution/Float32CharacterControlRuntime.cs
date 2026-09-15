using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation
{
    internal sealed class Float32CharacterControlReadPort : ICharacterControlReadPort
    {
        readonly Float32AbilityExecutionInput m_Input;
        readonly Float32AbilityBodyFacts m_Body;
        readonly Func<string, bool> m_HasInputRequest;
        readonly Func<CharacterControlParameterId, Float32Scalar> m_ReadParameter;
        readonly Func<CharacterSkillId, bool> m_IsAbilityActive;
        readonly Func<CharacterSkillId, (bool Found, ulong InstanceId)> m_TryGetActiveAbilityInstanceId;
        readonly Func<CharacterSkillId, bool> m_IsAbilityCompleted;
        readonly Func<CharacterSkillId, ulong> m_CompletedAbilityInstanceId;
        readonly Func<CharacterSkillId, string, bool> m_IsActionWindowActive;
        readonly Func<EquipmentActionRouteId, (bool Found, EquipmentActionContext Context)> m_TryReadEquipmentActionContext;

        public Float32CharacterControlReadPort(
            Float32AbilityExecutionInput input,
            Float32AbilityBodyFacts body,
            Func<string, bool> hasInputRequest,
            Func<CharacterControlParameterId, Float32Scalar> readParameter,
            Func<CharacterSkillId, bool> isAbilityActive,
            Func<CharacterSkillId, (bool Found, ulong InstanceId)> tryGetActiveAbilityInstanceId,
            Func<CharacterSkillId, bool> isAbilityCompleted,
            Func<CharacterSkillId, ulong> completedAbilityInstanceId,
            Func<CharacterSkillId, string, bool> isActionWindowActive,
            Func<EquipmentActionRouteId, (bool Found, EquipmentActionContext Context)> tryReadEquipmentActionContext)
        {
            m_Input = input ?? throw new ArgumentNullException(nameof(input));
            m_Body = body;
            m_HasInputRequest = hasInputRequest ?? throw new ArgumentNullException(nameof(hasInputRequest));
            m_ReadParameter = readParameter ?? throw new ArgumentNullException(nameof(readParameter));
            m_IsAbilityActive = isAbilityActive ?? throw new ArgumentNullException(nameof(isAbilityActive));
            m_TryGetActiveAbilityInstanceId = tryGetActiveAbilityInstanceId ?? throw new ArgumentNullException(nameof(tryGetActiveAbilityInstanceId));
            m_IsAbilityCompleted = isAbilityCompleted ?? throw new ArgumentNullException(nameof(isAbilityCompleted));
            m_CompletedAbilityInstanceId = completedAbilityInstanceId ?? throw new ArgumentNullException(nameof(completedAbilityInstanceId));
            m_IsActionWindowActive = isActionWindowActive ?? throw new ArgumentNullException(nameof(isActionWindowActive));
            m_TryReadEquipmentActionContext = tryReadEquipmentActionContext ?? throw new ArgumentNullException(nameof(tryReadEquipmentActionContext));
        }

        public bool HasInputRequest(string requestId) => m_HasInputRequest(requestId);

        public bool IsAbilityActive(CharacterSkillId abilityId) => m_IsAbilityActive(abilityId);

        public bool TryGetActiveAbilityInstanceId(CharacterSkillId abilityId, out ulong instanceId)
        {
            (bool found, ulong value) = m_TryGetActiveAbilityInstanceId(abilityId);
            instanceId = value;
            return found;
        }

        public bool IsAbilityCompleted(CharacterSkillId abilityId) => m_IsAbilityCompleted(abilityId);
        public ulong CompletedAbilityInstanceId(CharacterSkillId abilityId) => m_CompletedAbilityInstanceId(abilityId);
        public bool IsAbilityWindowActive(CharacterSkillId abilityId, string windowType) => m_IsActionWindowActive(abilityId, windowType);

        public bool TryReadEquipmentActionContext(EquipmentActionRouteId routeId, out EquipmentActionContext context)
        {
            (bool found, EquipmentActionContext value) = m_TryReadEquipmentActionContext(routeId);
            context = value;
            return found;
        }

        public bool CompareInputVector2Magnitude(
            SimulationInputValueId input,
            CharacterControlParameterId threshold,
            CharacterControlNumericComparison comparison)
        {
            Float32Vector2 value = ReadValue(input, SimulationInputValueKind.Vector2).Vector2;
            return Compare(value.Magnitude, m_ReadParameter(threshold), comparison);
        }

        public bool CompareInputDirectionToBodyYaw(
            SimulationInputValueId input,
            CharacterControlParameterId threshold,
            CharacterControlNumericComparison comparison)
        {
            Float32Vector2 value = ReadValue(input, SimulationInputValueKind.Vector2).Vector2;
            Float32Scalar angle = value == Float32Vector2.Zero
                ? Float32Scalar.Zero
                : Float32Scalar.Abs(Float32Angle.Delta(m_Body.Yaw, Float32Angle.FromPlanarDirection(value)));
            return Compare(angle, m_ReadParameter(threshold), comparison);
        }

        public bool IsInputDirectionBehindBodyYaw(SimulationInputValueId input)
        {
            Float32Vector2 value = ReadValue(input, SimulationInputValueKind.Vector2).Vector2;
            if (value == Float32Vector2.Zero)
                return false;
            Float32Scalar angle = Float32Scalar.Abs(Float32Angle.Delta(m_Body.Yaw, Float32Angle.FromPlanarDirection(value)));
            return angle >= Float32Scalar.FromInt64(90);
        }

        SimulationInputValue ReadValue(SimulationInputValueId input, SimulationInputValueKind kind)
        {
            for (int i = 0; i < m_Input.Values.Count; i++)
            {
                SimulationInputValue value = m_Input.Values[i];
                if (!string.Equals(value.InputId, input.Value, StringComparison.Ordinal))
                    continue;
                if (value.Kind != kind)
                    throw new InvalidOperationException($"Input '{input}' is '{value.Kind}', expected '{kind}'.");
                return value;
            }
            throw new InvalidOperationException($"Tick input does not contain required value '{input}'.");
        }

        static bool Compare(
            Float32Scalar value,
            Float32Scalar threshold,
            CharacterControlNumericComparison comparison)
        {
            return comparison switch
            {
                CharacterControlNumericComparison.Less => value < threshold,
                CharacterControlNumericComparison.LessOrEqual => value <= threshold,
                CharacterControlNumericComparison.Equal => value == threshold,
                CharacterControlNumericComparison.Greater => value > threshold,
                CharacterControlNumericComparison.GreaterOrEqual => value >= threshold,
                _ => throw new ArgumentOutOfRangeException(nameof(comparison))
            };
        }
    }

    internal sealed class Float32CharacterControlStatePort : ICharacterControlStatePort
    {
        readonly CharacterControlRuntimeStateTransaction m_State;
        readonly CharacterControlStateSchema m_Schema;

        public Float32CharacterControlStatePort(
            CharacterControlRuntimeStateTransaction state,
            CharacterControlStateSchema schema)
        {
            m_State = state ?? throw new ArgumentNullException(nameof(state));
            m_Schema = schema ?? throw new ArgumentNullException(nameof(schema));
        }

        public CharacterControlStateId ReadState(CharacterControlStateFieldId field)
        {
            RequireKind(field, CharacterControlStateValueKind.Identity);
            string value = m_State.Get(field).Identity;
            return string.IsNullOrEmpty(value) ? default : new CharacterControlStateId(value);
        }

        public void WriteState(CharacterControlStateFieldId field, CharacterControlStateId value)
        {
            RequireKind(field, CharacterControlStateValueKind.Identity);
            m_State.Set(field, CharacterControlStateValue.FromIdentity(value.Value));
        }

        public CharacterControlTransitionId ReadTransition(CharacterControlStateFieldId field)
        {
            RequireKind(field, CharacterControlStateValueKind.Identity);
            string value = m_State.Get(field).Identity;
            return string.IsNullOrEmpty(value) ? default : new CharacterControlTransitionId(value);
        }

        public void WriteTransition(CharacterControlStateFieldId field, CharacterControlTransitionId value)
        {
            RequireKind(field, CharacterControlStateValueKind.Identity);
            m_State.Set(field, CharacterControlStateValue.FromIdentity(value.Value));
        }

        public bool ReadBoolean(CharacterControlStateFieldId field)
        {
            RequireKind(field, CharacterControlStateValueKind.Boolean);
            return m_State.Get(field).Boolean;
        }

        public void WriteBoolean(CharacterControlStateFieldId field, bool value)
        {
            RequireKind(field, CharacterControlStateValueKind.Boolean);
            m_State.Set(field, CharacterControlStateValue.FromBoolean(value));
        }

        public int ReadInt32(CharacterControlStateFieldId field)
        {
            RequireKind(field, CharacterControlStateValueKind.Int32);
            return m_State.Get(field).Int32;
        }

        public void WriteInt32(CharacterControlStateFieldId field, int value)
        {
            RequireKind(field, CharacterControlStateValueKind.Int32);
            m_State.Set(field, CharacterControlStateValue.FromInt32(value));
        }

        public ulong ReadUInt64(CharacterControlStateFieldId field)
        {
            RequireKind(field, CharacterControlStateValueKind.UInt64);
            return m_State.Get(field).UInt64;
        }

        public void WriteUInt64(CharacterControlStateFieldId field, ulong value)
        {
            RequireKind(field, CharacterControlStateValueKind.UInt64);
            m_State.Set(field, CharacterControlStateValue.FromUInt64(value));
        }

        void RequireKind(CharacterControlStateFieldId field, CharacterControlStateValueKind expected)
        {
            if (m_Schema.RequireKind(field) != expected)
                throw new InvalidOperationException($"Control state field '{field}' is not '{expected}'.");
        }
    }

    internal sealed class Float32CharacterControlOutputPort : ICharacterControlOutputPort
    {
        readonly CharacterControlModuleContract m_ControlModule;
        readonly Float32InputRuntime m_Input;
        readonly Float32LocomotionRuntime m_Locomotion;
        readonly IReadOnlyDictionary<CharacterSkillId, Float32ActionRuntime> m_Actions;
        readonly Float32TraceSink m_Trace;

        public Float32CharacterControlOutputPort(
            CharacterControlModuleContract controlModule,
            Float32InputRuntime input,
            Float32LocomotionRuntime locomotion,
            IReadOnlyDictionary<CharacterSkillId, Float32ActionRuntime> actions,
            Float32TraceSink trace)
        {
            m_ControlModule = controlModule ?? throw new ArgumentNullException(nameof(controlModule));
            m_Input = input ?? throw new ArgumentNullException(nameof(input));
            m_Locomotion = locomotion ?? throw new ArgumentNullException(nameof(locomotion));
            m_Actions = actions ?? throw new ArgumentNullException(nameof(actions));
            m_Trace = trace ?? throw new ArgumentNullException(nameof(trace));
        }

        public void SubmitMotion(CharacterControlMotionRequest request)
        {
            CharacterControlMotionDescriptor descriptor = RequireMotion(request.Binding);
            m_Locomotion.SubmitControl(m_Input, request, descriptor);
        }

        public bool SubmitAbility(CharacterControlAbilityRequest request) =>
            RequireAction(request.AbilityId).ActivateFromControl(request);

        public void SubmitAbilityStop(CharacterControlAbilityStopRequest request) =>
            RequireAction(request.AbilityId).StopFromControl(request);

        public void Trace(SimulationExecutionSource source, string code, string detail, ulong generation) =>
            m_Trace.Add(source, code, SimulationTraceSeverity.Information, detail, generation);

        Float32ActionRuntime RequireAction(CharacterSkillId abilityId)
        {
            if (!m_Actions.TryGetValue(abilityId, out Float32ActionRuntime action))
                throw new InvalidOperationException($"Character Control requested uninstalled Ability '{abilityId}'.");
            return action;
        }

        CharacterControlMotionDescriptor RequireMotion(string binding)
        {
            for (int i = 0; i < m_ControlModule.Motions.Count; i++)
            {
                CharacterControlMotionDescriptor motion = m_ControlModule.Motions[i];
                if (string.Equals(motion.Binding, binding, StringComparison.Ordinal))
                    return motion;
            }
            throw new InvalidOperationException($"Control module '{m_ControlModule.ModuleId}' has no motion '{binding}'.");
        }
    }

    internal sealed class Float32CharacterControlRuntime
    {
        readonly ICharacterControlModule m_Control;
        readonly CharacterControlRuntimeBinding m_Binding;
        readonly CharacterControlRuntimeStateTransaction m_State;
        readonly CharacterControlStateSchema m_Schema;
        readonly Float32CharacterControlReadPort m_Read;
        readonly Float32CharacterControlOutputPort m_Output;
        readonly ActorId m_ActorId;
        readonly SimulationTick m_Tick;
        readonly int m_TickRate;

        public Float32CharacterControlRuntime(
            CharacterControlModuleCatalog controlModules,
            CharacterControlRuntimeBinding binding,
            Float32CharacterRuntimeStateTransaction roleState,
            ActorId actorId,
            SimulationTick tick,
            int tickRate,
            Float32AbilityExecutionInput input,
            Float32AbilityBodyFacts body,
            Float32InputRuntime inputRuntime,
            Float32LocomotionRuntime locomotion,
            Float32TraceSink trace,
            IReadOnlyDictionary<CharacterSkillId, Float32ActionRuntime> actions,
            Func<CharacterSkillId, string, bool> isActionWindowActive,
            Func<EquipmentActionRouteId, (bool Found, EquipmentActionContext Context)> tryReadEquipmentActionContext)
        {
            controlModules = controlModules ?? throw new ArgumentNullException(nameof(controlModules));
            m_Binding = binding ?? throw new ArgumentNullException(nameof(binding));
            roleState = roleState ?? throw new ArgumentNullException(nameof(roleState));
            if (!actorId.IsValid || !tick.IsValid || tickRate <= 0)
                throw new ArgumentException("Float32 Character Control runtime identity is incomplete.");
            m_Control = controlModules.Require(binding.ModuleId);
            binding.RequireContract(m_Control.Contract);
            m_Schema = new CharacterControlStateSchema(m_Control.Contract);
            m_State = roleState.BindControl(m_Schema);
            m_ActorId = actorId;
            m_Tick = tick;
            m_TickRate = tickRate;
            m_Read = new Float32CharacterControlReadPort(
                input,
                body,
                requestId => HasInputRequest(roleState, requestId),
                parameter => Float32Scalar.FromDouble(binding.Parameters.ReadNumeric(parameter)),
                skill => IsAbilityActive(roleState, skill),
                skill => TryGetActiveAbilityInstanceId(roleState, skill),
                skill => IsAbilityCompleted(roleState, skill),
                skill => CompletedAbilityInstanceId(roleState, skill),
                isActionWindowActive,
                tryReadEquipmentActionContext);
            m_Output = new Float32CharacterControlOutputPort(
                m_Control.Contract,
                inputRuntime,
                locomotion,
                actions,
                trace);
        }

        public CharacterControlRuntimeStateTransaction State => m_State;

        public void Tick()
        {
            var context = new CharacterControlTickContext(m_ActorId, m_Tick, m_TickRate);
            m_State.BaseState.RequireBinding(m_Binding);
            m_Control.Tick(in context, m_Read, new Float32CharacterControlStatePort(m_State, m_Schema), m_Output);
        }

        static bool IsAbilityActive(Float32CharacterRuntimeStateTransaction state, CharacterSkillId abilityId)
        {
            IReadOnlyList<Float32ActionInstanceState> actions = state.GetActionInstances();
            for (int i = 0; i < actions.Count; i++)
                if (actions[i].IsActive && actions[i].SkillId == abilityId)
                    return true;
            return false;
        }

        static bool HasInputRequest(Float32CharacterRuntimeStateTransaction state, string requestId)
        {
            SimulationInputRequestState request = state.GetInputRequest(requestId);
            return request.IsValid && !request.Consumed && request.ExpireTick >= state.Tick.Value;
        }

        static bool IsAbilityCompleted(Float32CharacterRuntimeStateTransaction state, CharacterSkillId abilityId) =>
            CompletedAbilityInstanceId(state, abilityId) != 0;

        static (bool Found, ulong InstanceId) TryGetActiveAbilityInstanceId(
            Float32CharacterRuntimeStateTransaction state,
            CharacterSkillId abilityId)
        {
            ulong found = 0;
            IReadOnlyList<Float32ActionInstanceState> actions = state.GetActionInstances();
            for (int i = 0; i < actions.Count; i++)
            {
                Float32ActionInstanceState action = actions[i];
                if (!action.IsActive || action.SkillId != abilityId)
                    continue;
                if (found != 0)
                    return (false, 0);
                found = action.InstanceId;
            }
            return (found != 0, found);
        }

        static ulong CompletedAbilityInstanceId(Float32CharacterRuntimeStateTransaction state, CharacterSkillId abilityId)
        {
            ulong result = 0;
            ulong tick = 0;
            IReadOnlyList<Float32ActionInstanceState> actions = state.GetActionInstances();
            for (int i = 0; i < actions.Count; i++)
            {
                Float32ActionInstanceState action = actions[i];
                if (action.SkillId != abilityId || action.State != SimulationActionState.Ended)
                    continue;
                if (result == 0 || action.LastTransitionTick > tick ||
                    action.LastTransitionTick == tick && action.InstanceId > result)
                {
                    result = action.InstanceId;
                    tick = action.LastTransitionTick;
                }
            }
            return result;
        }
    }
}
