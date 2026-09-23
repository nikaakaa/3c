using System;
using System.Collections.Generic;
using ThirdPersonSimulation;

namespace ThirdPersonSimulation.Fixed
{
    internal sealed class FixedCharacterControlMotionRuntime
    {
        FixedAbilityExecutionInput m_Input;
        FixedAbilityBodyFacts m_Body;
        SimulationTick m_Tick;
        int m_TickRate;
        readonly FixedCharacterControlMotionBindingCatalog m_ControlMotionBindings;
        SimulationMotionContribution[] m_Contributions = Array.Empty<SimulationMotionContribution>();
        int m_ContributionCount;

        public FixedCharacterControlMotionRuntime(
            FixedCharacterControlMotionBindingCatalog controlMotionBindings)
        {
            m_ControlMotionBindings = controlMotionBindings;
        }

        internal void Begin(
            FixedAbilityExecutionInput input,
            FixedAbilityBodyFacts body,
            SimulationTick tick,
            int tickRate)
        {
            m_Input = input ?? throw new ArgumentNullException(nameof(input));
            if (!body.IsValid)
                throw new ArgumentException("Fixed Character Control motion requires Body Facts.", nameof(body));
            if (!tick.IsValid || tickRate <= 0)
                throw new ArgumentException("Fixed Character Control motion identity is incomplete.");
            Array.Clear(m_Contributions, 0, m_ContributionCount);
            m_ContributionCount = 0;
            m_Body = body;
            m_Tick = tick;
            m_TickRate = tickRate;
        }

        internal void CopyContributionsTo(FixedMotionContributionScratch contributions)
        {
            for (int i = 0; i < m_ContributionCount; i++)
                contributions.Append(m_Contributions[i]);
        }

        internal void ClearContributions()
        {
            Array.Clear(m_Contributions, 0, m_ContributionCount);
            m_ContributionCount = 0;
        }

        public void SubmitControl(
            CharacterControlMotionRequest request,
            CharacterControlMotionDescriptor descriptor)
        {
            SimulationInputValue input = ReadValue(request.Input.Value, SimulationInputValueKind.Vector2);
            SubmitControl(
                input.Vector2,
                m_Body,
                m_Tick,
                m_TickRate,
                m_ControlMotionBindings,
                request,
                descriptor,
                ref m_Contributions,
                ref m_ContributionCount);
        }

        internal static void SubmitControl(
            FixedVector2 move,
            FixedAbilityBodyFacts body,
            SimulationTick tick,
            int tickRate,
            FixedCharacterControlMotionBindingCatalog controlMotionBindings,
            CharacterControlMotionRequest request,
            CharacterControlMotionDescriptor descriptor,
            ref SimulationMotionContribution[] contributions,
            ref int contributionCount)
        {
            if (!body.IsValid || !tick.IsValid || tickRate <= 0)
                throw new ArgumentException("Fixed Character Control motion identity is incomplete.");
            if (request.Input != descriptor.Input || !string.Equals(request.Binding, descriptor.Binding, StringComparison.Ordinal))
                throw new InvalidOperationException($"Control motion request '{request.Binding}' does not match its declared motion.");
            if (move.SqrMagnitude > FixedScalar.One)
                move = move.Normalized;
            FixedScalar delta = FixedScalar.One / FixedScalar.FromInt64(tickRate);
            FixedScalar moveSpeed = FixedScalar.FromDouble(descriptor.MoveSpeed);
            FixedScalar turnSpeed = FixedScalar.FromDouble(descriptor.TurnSpeedDegrees);
            FixedScalar maxYaw = turnSpeed * delta;
            FixedVector3 displacement;
            FixedScalar yaw;
            if (descriptor.DisplacementMode == CharacterControlMotionDisplacementMode.SourceCurve)
            {
                FixedCharacterControlMotionBinding source = controlMotionBindings == null
                    ? throw new InvalidOperationException("Fixed Control motion bindings are not installed.")
                    : controlMotionBindings.Require(descriptor.SourceMotionIdentity);
                source.EvaluateDelta(
                    FixedScalar.FromRatio(request.ContinuousTicks, tickRate),
                    FixedScalar.FromRatio(request.ContinuousTicks + 1, tickRate),
                    out displacement,
                    out yaw);
            }
            else
            {
                displacement = new FixedVector3(
                    move.X * moveSpeed * delta,
                    FixedScalar.Zero,
                    move.Y * moveSpeed * delta);
                yaw = FixedScalar.Zero;
                if (move != FixedVector2.Zero && maxYaw > FixedScalar.Zero)
                {
                    FixedYaw desired = FixedAngle.FromPlanarDirection(move);
                    yaw = FixedScalar.Clamp(FixedAngle.Delta(body.Yaw, desired), -maxYaw, maxYaw);
                }
            }
            int continuousTicks = checked(request.ContinuousTicks + 1);
            int durationTicks = descriptor.DisplacementMode == CharacterControlMotionDisplacementMode.SourceCurve
                ? 0
                : descriptor.ExecutionMode == CharacterControlMotionExecutionMode.Timed
                ? checked((int)Math.Ceiling(descriptor.DurationSeconds * tickRate))
                : 0;
            var movementPlaybackClock = new CommittedMovementPlaybackClock(
                request.Source.Identity,
                request.PlaybackGeneration,
                tick,
                continuousTicks,
                tickRate);
            var locomotionTimeline = new CommittedLocomotionPlanarMotionTimeline(
                request.Source.Identity,
                request.PlaybackGeneration,
                tick,
                tickRate,
                (displacement.X / delta).ToSingle(),
                (displacement.Z / delta).ToSingle(),
                (yaw / delta).ToSingle(),
                turnSpeed.ToSingle(),
                durationTicks,
                string.Empty,
                0f,
                0f);
            if (contributionCount == contributions.Length)
            {
                int capacity = Math.Max(4, contributions.Length * 2);
                var values = new SimulationMotionContribution[capacity];
                Array.Copy(contributions, values, contributionCount);
                contributions = values;
            }

            contributions[contributionCount++] = new SimulationMotionContribution(
                request.Source,
                default,
                request.PlaybackGeneration,
                displacement,
                yaw,
                descriptor.DisplacementMode == CharacterControlMotionDisplacementMode.SourceCurve
                    ? FixedVector2.Zero
                    : move,
                descriptor.Space == CharacterControlMotionSpace.ActorLocal
                    ? SimulationMotionContributionSpace.ActorLocal
                    : SimulationMotionContributionSpace.World,
                FixedScalar.One,
                descriptor.Priority,
                SimulationMotionChannel.Locomotion,
                SimulationMotionBlendMode.Override,
                descriptor.ConsumeLowerChannels,
                movementPlaybackClock,
                locomotionTimeline);
        }

        SimulationInputValue ReadValue(string inputId, SimulationInputValueKind kind)
        {
            for (int i = 0; i < m_Input.Values.Count; i++)
            {
                SimulationInputValue value = m_Input.Values[i];
                if (!string.Equals(value.InputId, inputId, StringComparison.Ordinal))
                    continue;
                if (value.Kind != kind)
                    throw new InvalidOperationException($"Input '{inputId}' is '{value.Kind}', expected '{kind}'.");
                return value;
            }
            throw new InvalidOperationException($"Tick input does not contain required value '{inputId}'.");
        }
    }

    internal sealed class FixedCharacterTraceSink
    {
        readonly List<SimulationTraceRecord> m_Records;
        SimulationNumericProfile m_NumericProfile;
        GameplayContentHash m_ContentHash;
        ActorId m_ActorId;
        SimulationTick m_Tick;
        ulong m_Sequence;
        bool m_Enabled;

        public FixedCharacterTraceSink(
            List<SimulationTraceRecord> records)
        {
            m_Records = records ?? throw new ArgumentNullException(nameof(records));
        }

        internal void Begin(
            SimulationNumericProfile numericProfile,
            StableHash contentHash,
            ActorId actorId,
            SimulationTick tick,
            bool enabled)
        {
            if (!numericProfile.IsValid || !contentHash.IsValid || !actorId.IsValid || !tick.IsValid)
                throw new ArgumentException("Fixed Character Control trace identity is incomplete.");
            m_NumericProfile = numericProfile;
            m_ContentHash = new GameplayContentHash(contentHash);
            m_ActorId = actorId;
            m_Tick = tick;
            m_Enabled = enabled;
            m_Sequence = 0;
        }

        public void Add(
            string category,
            SimulationExecutionSource source,
            string code,
            SimulationTraceSeverity severity,
            string detail,
            ulong generation = 0)
        {
            if (!m_Enabled)
                return;
            ulong sequence = checked(++m_Sequence);
            if (generation == 0)
                generation = 1;
            var activation = new ActivationId(source, generation);
            var header = new SimulationEventHeader(
                m_NumericProfile,
                EventId.Create(m_ContentHash, m_ActorId, activation, m_Tick, sequence, "Trace"),
                m_ActorId,
                m_Tick,
                activation,
                sequence,
                "Trace");
            m_Records.Add(new SimulationTraceRecord(
                header,
                severity,
                category,
                code,
                detail));
        }
    }

    internal sealed class FixedCharacterControlReadPort : ICharacterControlReadPort
    {
        readonly FixedAbilityExecutionInput m_Input;
        FixedAbilityBodyFacts m_Body;
        IFixedInputRequestStatePort m_InputRequests;
        IFixedActionRuntimeStatePort m_ActionState;
        readonly Func<string, bool> m_HasInputRequest;
        readonly Func<CharacterControlParameterId, FixedScalar> m_ReadParameter;
        readonly Func<CharacterSkillId, string, bool> m_IsActionWindowActive;
        readonly Func<EquipmentActionRouteId, (bool Found, EquipmentActionContext Context)> m_TryReadEquipmentActionContext;

        public FixedCharacterControlReadPort(
            FixedAbilityExecutionInput input,
            Func<string, bool> hasInputRequest,
            Func<CharacterControlParameterId, FixedScalar> readParameter,
            Func<CharacterSkillId, string, bool> isActionWindowActive,
            Func<EquipmentActionRouteId, (bool Found, EquipmentActionContext Context)> tryReadEquipmentActionContext)
        {
            m_Input = input ?? throw new ArgumentNullException(nameof(input));
            m_HasInputRequest = hasInputRequest ?? throw new ArgumentNullException(nameof(hasInputRequest));
            m_ReadParameter = readParameter ?? throw new ArgumentNullException(nameof(readParameter));
            m_IsActionWindowActive = isActionWindowActive ?? throw new ArgumentNullException(nameof(isActionWindowActive));
            m_TryReadEquipmentActionContext = tryReadEquipmentActionContext ?? throw new ArgumentNullException(nameof(tryReadEquipmentActionContext));
        }

        internal void Begin(
            FixedAbilityBodyFacts body,
            IFixedInputRequestStatePort inputRequests,
            IFixedActionRuntimeStatePort actionState)
        {
            if (!body.IsValid)
                throw new ArgumentException("Fixed Character Control read port requires Body Facts.", nameof(body));
            m_Body = body;
            m_InputRequests = inputRequests ?? throw new ArgumentNullException(nameof(inputRequests));
            m_ActionState = actionState ?? throw new ArgumentNullException(nameof(actionState));
        }

        public bool HasInputRequest(string requestId) => m_HasInputRequest(requestId);

        public bool IsAbilityActive(CharacterSkillId abilityId) =>
            IsStateAbilityActive(m_ActionState, abilityId);

        public bool TryGetActiveAbilityInstanceId(CharacterSkillId abilityId, out ulong instanceId)
        {
            (bool found, ulong value) = TryGetStateActiveAbilityInstanceId(m_ActionState, abilityId);
            instanceId = value;
            return found;
        }

        public bool IsAbilityCompleted(CharacterSkillId abilityId) =>
            IsStateAbilityCompleted(m_ActionState, abilityId);

        public ulong CompletedAbilityInstanceId(CharacterSkillId abilityId) =>
            GetStateCompletedAbilityInstanceId(m_ActionState, abilityId);
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
            FixedVector2 value = ReadValue(input, SimulationInputValueKind.Vector2).Vector2;
            return Compare(value.Magnitude, m_ReadParameter(threshold), comparison);
        }

        public bool ReadInputBoolean(SimulationInputValueId input) =>
            ReadValue(input, SimulationInputValueKind.Boolean).Boolean;

        public bool CompareInputDirectionToBodyYaw(
            SimulationInputValueId input,
            CharacterControlParameterId threshold,
            CharacterControlNumericComparison comparison)
        {
            FixedVector2 value = ReadValue(input, SimulationInputValueKind.Vector2).Vector2;
            FixedScalar angle = value == FixedVector2.Zero
                ? FixedScalar.Zero
                : FixedScalar.Abs(FixedAngle.Delta(m_Body.Yaw, FixedAngle.FromPlanarDirection(value)));
            return Compare(angle, m_ReadParameter(threshold), comparison);
        }

        public bool IsInputDirectionBehindBodyYaw(SimulationInputValueId input)
        {
            FixedVector2 value = ReadValue(input, SimulationInputValueKind.Vector2).Vector2;
            if (value == FixedVector2.Zero)
                return false;
            FixedScalar angle = FixedScalar.Abs(FixedAngle.Delta(m_Body.Yaw, FixedAngle.FromPlanarDirection(value)));
            return angle >= FixedScalar.FromInt64(90);
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
            FixedScalar value,
            FixedScalar threshold,
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

        static bool IsStateAbilityActive(IFixedActionRuntimeStatePort state, CharacterSkillId abilityId)
        {
            IReadOnlyList<FixedActionInstanceState> actions = state.GetActionInstances();
            for (int i = 0; i < actions.Count; i++)
                if (actions[i].IsActive && actions[i].SkillId == abilityId)
                    return true;
            return false;
        }

        static (bool Found, ulong InstanceId) TryGetStateActiveAbilityInstanceId(
            IFixedActionRuntimeStatePort state,
            CharacterSkillId abilityId)
        {
            ulong found = 0;
            IReadOnlyList<FixedActionInstanceState> actions = state.GetActionInstances();
            for (int i = 0; i < actions.Count; i++)
            {
                FixedActionInstanceState action = actions[i];
                if (!action.IsActive || action.SkillId != abilityId)
                    continue;
                if (found != 0)
                    return (false, 0);
                found = action.InstanceId;
            }
            return (found != 0, found);
        }

        static bool IsStateAbilityCompleted(IFixedActionRuntimeStatePort state, CharacterSkillId abilityId) =>
            GetStateCompletedAbilityInstanceId(state, abilityId) != 0;

        static ulong GetStateCompletedAbilityInstanceId(IFixedActionRuntimeStatePort state, CharacterSkillId abilityId)
        {
            ulong result = 0;
            ulong tick = 0;
            IReadOnlyList<FixedActionInstanceState> actions = state.GetActionInstances();
            for (int i = 0; i < actions.Count; i++)
            {
                FixedActionInstanceState action = actions[i];
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

    internal sealed class FixedCharacterControlStatePort : ICharacterControlStatePort
    {
        readonly CharacterControlStateSchema m_Schema;
        CharacterControlRuntimeStateTransaction m_State;

        public FixedCharacterControlStatePort(CharacterControlStateSchema schema)
        {
            m_Schema = schema ?? throw new ArgumentNullException(nameof(schema));
        }

        internal void Begin(CharacterControlRuntimeStateTransaction state)
        {
            m_State = state ?? throw new ArgumentNullException(nameof(state));
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

    internal sealed class FixedCharacterControlOutputPort : ICharacterControlOutputPort
    {
        readonly CharacterControlModuleContract m_ControlModule;
        readonly FixedCharacterControlMotionRuntime m_Motion;
        readonly IReadOnlyDictionary<CharacterSkillId, IFixedAbilityActionControlPort> m_Actions;
        readonly FixedCharacterTraceSink m_Trace;

        public FixedCharacterControlOutputPort(
            CharacterControlModuleContract controlModule,
            FixedCharacterControlMotionRuntime motion,
            IReadOnlyDictionary<CharacterSkillId, IFixedAbilityActionControlPort> actions,
            FixedCharacterTraceSink trace)
        {
            m_ControlModule = controlModule ?? throw new ArgumentNullException(nameof(controlModule));
            m_Motion = motion ?? throw new ArgumentNullException(nameof(motion));
            m_Actions = actions ?? throw new ArgumentNullException(nameof(actions));
            m_Trace = trace ?? throw new ArgumentNullException(nameof(trace));
        }

        public void SubmitMotion(CharacterControlMotionRequest request)
        {
            CharacterControlMotionDescriptor descriptor = RequireMotion(request.Binding);
            m_Motion.SubmitControl(request, descriptor);
        }

        public bool SubmitAbility(CharacterControlAbilityRequest request) =>
            RequireAction(request.AbilityId).ActivateFromControl(request);

        public void SubmitAbilityStop(CharacterControlAbilityStopRequest request) =>
            RequireAction(request.AbilityId).StopFromControl(request);

        public void Trace(SimulationExecutionSource source, string code, string detail, ulong generation) =>
            m_Trace.Add("Character.Control", source, code, SimulationTraceSeverity.Information, detail, generation);

        IFixedAbilityActionControlPort RequireAction(CharacterSkillId abilityId)
        {
            if (!m_Actions.TryGetValue(abilityId, out IFixedAbilityActionControlPort action))
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

    internal sealed class FixedCharacterControlRuntime
    {
        readonly ICharacterControlModule m_Control;
        readonly CharacterControlRuntimeBinding m_Binding;
        readonly CharacterControlStateSchema m_Schema;
        FixedCharacterControlReadPort m_Read;
        FixedCharacterControlOutputPort m_Output;
        FixedCharacterControlStatePort m_StatePort;
        readonly ActorId m_ActorId;
        readonly int m_TickRate;
        SimulationTick m_Tick;
        IFixedInputRequestStatePort m_InputRequests;
        IFixedActionRuntimeStatePort m_ActionState;
        CharacterControlRuntimeStateTransaction m_State;

        public FixedCharacterControlRuntime(
            CharacterControlModuleCatalog controlModules,
            CharacterControlRuntimeBinding binding,
            FixedAbilityExecutionInput input,
            ActorId actorId,
            int tickRate,
            FixedCharacterControlMotionRuntime motion,
            FixedCharacterTraceSink trace,
            IReadOnlyDictionary<CharacterSkillId, IFixedAbilityActionControlPort> actions,
            Func<CharacterSkillId, string, bool> isActionWindowActive,
            Func<EquipmentActionRouteId, (bool Found, EquipmentActionContext Context)> tryReadEquipmentActionContext)
        {
            controlModules = controlModules ?? throw new ArgumentNullException(nameof(controlModules));
            m_Binding = binding ?? throw new ArgumentNullException(nameof(binding));
            if (!actorId.IsValid || tickRate <= 0)
                throw new ArgumentException("Fixed Character Control runtime identity is incomplete.");
            m_Control = controlModules.Require(binding.ModuleId);
            binding.RequireContract(m_Control.Contract);
            m_Schema = m_Control.Contract.StateSchema;
            m_ActorId = actorId;
            m_TickRate = tickRate;

            m_Read = new FixedCharacterControlReadPort(
                input,
                requestId => HasInputRequest(m_InputRequests, requestId),
                parameter => FixedScalar.FromDouble(binding.Parameters.ReadNumeric(parameter)),
                isActionWindowActive,
                tryReadEquipmentActionContext);
            m_Output = new FixedCharacterControlOutputPort(
                m_Control.Contract,
                motion,
                actions,
                trace);
            m_StatePort = new FixedCharacterControlStatePort(m_Schema);
        }

        internal void Begin(
            IFixedControlRuntimeStatePort controlState,
            IFixedInputRequestStatePort inputRequests,
            IFixedActionRuntimeStatePort actionState,
            SimulationTick tick,
            FixedAbilityBodyFacts body)
        {
            if (controlState == null)
                throw new ArgumentNullException(nameof(controlState));
            if (!tick.IsValid)
                throw new ArgumentException("Fixed Character Control runtime identity is incomplete.", nameof(tick));
            m_State = controlState.BindControl(m_Schema);
            m_StatePort.Begin(m_State);
            m_InputRequests = inputRequests ?? throw new ArgumentNullException(nameof(inputRequests));
            m_ActionState = actionState ?? throw new ArgumentNullException(nameof(actionState));
            m_Read.Begin(body, inputRequests, actionState);
            m_Tick = tick;
        }

        public CharacterControlRuntimeStateTransaction State => m_State;

        public void Tick()
        {
            var context = new CharacterControlTickContext(m_ActorId, m_Tick, m_TickRate);
            m_State.BaseState.RequireBinding(m_Binding);
            m_Control.Tick(in context, m_Read, m_StatePort, m_Output);
        }

        public void ResolveAbilityOutputs() => m_Control.ResolveAbilityOutputs();

        static bool HasInputRequest(IFixedInputRequestStatePort state, string requestId)
        {
            SimulationInputRequestState request = state.GetInputRequest(requestId);
            return request.IsValid && !request.Consumed && request.ExpireTick >= state.Tick.Value;
        }

    }
}
