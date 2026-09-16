using System;
using System.Collections.Generic;
using ThirdPersonSimulation;

namespace ThirdPersonSimulation.Fixed
{
    internal sealed class FixedCharacterControlMotionRuntime
    {
        readonly FixedAbilityExecutionInput m_Input;
        readonly FixedAbilityBodyFacts m_Body;
        readonly SimulationTick m_Tick;
        readonly int m_TickRate;
        readonly CharacterControlMotionBindingCatalog m_ControlMotionBindings;
        readonly List<SimulationMotionContribution> m_Contributions =
            new List<SimulationMotionContribution>();

        public FixedCharacterControlMotionRuntime(
            FixedAbilityExecutionInput input,
            FixedAbilityBodyFacts body,
            SimulationTick tick,
            int tickRate,
            CharacterControlMotionBindingCatalog controlMotionBindings)
        {
            m_Input = input ?? throw new ArgumentNullException(nameof(input));
            if (!body.IsValid)
                throw new ArgumentException("Fixed Character Control motion requires Body Facts.", nameof(body));
            if (!tick.IsValid || tickRate <= 0)
                throw new ArgumentException("Fixed Character Control motion identity is incomplete.");
            m_Body = body;
            m_Tick = tick;
            m_TickRate = tickRate;
            m_ControlMotionBindings = controlMotionBindings;
        }

        public IReadOnlyList<SimulationMotionContribution> Contributions => m_Contributions;

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
                m_Contributions.Add);
        }

        public ResolvedGameplayMotion Resolve()
        {
            FixedVector3 additiveDisplacement = FixedVector3.Zero;
            FixedScalar additiveYaw = FixedScalar.Zero;
            FixedVector3 weightedDisplacement = FixedVector3.Zero;
            FixedScalar weightedYaw = FixedScalar.Zero;
            FixedScalar totalWeight = FixedScalar.Zero;
            SimulationMotionContribution overrideWinner = default;
            FixedVector3 overrideDisplacement = FixedVector3.Zero;
            FixedScalar overrideYaw = FixedScalar.Zero;
            bool hasWeighted = false;
            bool hasOverride = false;
            for (int i = 0; i < m_Contributions.Count; i++)
            {
                SimulationMotionContribution contribution = m_Contributions[i];
                FixedVector3 resolved = contribution.Space == SimulationMotionContributionSpace.ActorLocal
                    ? FixedAngle.RotatePlanar(contribution.Displacement, m_Body.Yaw)
                    : contribution.Displacement;
                switch (contribution.BlendMode)
                {
                    case SimulationMotionBlendMode.Additive:
                        additiveDisplacement += resolved * contribution.Weight;
                        additiveYaw += contribution.YawDegrees * contribution.Weight;
                        break;
                    case SimulationMotionBlendMode.WeightedBlend:
                        weightedDisplacement += resolved * contribution.Weight;
                        weightedYaw += contribution.YawDegrees * contribution.Weight;
                        totalWeight += contribution.Weight;
                        hasWeighted = true;
                        break;
                    case SimulationMotionBlendMode.Override:
                        if (!hasOverride || contribution.Priority > overrideWinner.Priority)
                        {
                            overrideWinner = contribution;
                            overrideDisplacement = resolved * contribution.Weight;
                            overrideYaw = contribution.YawDegrees * contribution.Weight;
                            hasOverride = true;
                        }
                        break;
                    default:
                        throw new InvalidOperationException(
                            $"Motion contribution '{contribution.SourceIdentity}' has invalid blend mode '{contribution.BlendMode}'.");
                }
            }
            if (!hasWeighted && !hasOverride && additiveDisplacement == FixedVector3.Zero && additiveYaw == FixedScalar.Zero)
                return new ResolvedGameplayMotion(
                    FixedVector3.Zero,
                    FixedScalar.Zero,
                    FixedVector2.Zero,
                    false,
                    default,
                    default,
                    string.Empty,
                    string.Empty);

            FixedVector3 displacement = additiveDisplacement;
            FixedScalar yaw = additiveYaw;
            if (hasOverride)
            {
                displacement += overrideDisplacement;
                yaw += overrideYaw;
            }
            else if (hasWeighted && totalWeight > FixedScalar.Zero)
            {
                displacement += new FixedVector3(
                    weightedDisplacement.X / totalWeight,
                    weightedDisplacement.Y / totalWeight,
                    weightedDisplacement.Z / totalWeight);
                yaw += weightedYaw / totalWeight;
            }
            if (!hasOverride || !overrideWinner.MovementPlaybackClock.IsValid)
                throw new InvalidOperationException("Resolved Character Control motion has no committed Movement playback clock owner.");
            return new ResolvedGameplayMotion(
                displacement,
                yaw,
                overrideWinner.PlanarBasis,
                displacement != FixedVector3.Zero || yaw != FixedScalar.Zero,
                overrideWinner.MovementPlaybackClock,
                overrideWinner.LocomotionTimeline,
                string.Empty,
                string.Empty);
        }

        internal static void SubmitControl(
            FixedVector2 move,
            FixedAbilityBodyFacts body,
            SimulationTick tick,
            int tickRate,
            CharacterControlMotionBindingCatalog controlMotionBindings,
            CharacterControlMotionRequest request,
            CharacterControlMotionDescriptor descriptor,
            Action<SimulationMotionContribution> submit)
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
                CharacterControlMotionBinding source = controlMotionBindings == null
                    ? throw new InvalidOperationException("Fixed Control motion bindings are not installed.")
                    : controlMotionBindings.Require(descriptor.SourceMotionIdentity);
                CharacterControlMotionDelta curveDelta = source.EvaluateDelta(
                    request.ContinuousTicks / (double)tickRate,
                    (request.ContinuousTicks + 1) / (double)tickRate);
                displacement = new FixedVector3(
                    FixedScalar.FromDouble(curveDelta.X),
                    FixedScalar.FromDouble(curveDelta.Y),
                    FixedScalar.FromDouble(curveDelta.Z));
                yaw = FixedScalar.FromDouble(curveDelta.Yaw);
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
            submit(new SimulationMotionContribution(
                request.Source,
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
                locomotionTimeline));
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

    internal sealed class FixedCharacterControlTraceSink
    {
        readonly List<SimulationTraceRecord> m_Records;
        readonly SimulationNumericProfile m_NumericProfile;
        readonly GameplayContentHash m_ContentHash;
        readonly ActorId m_ActorId;
        readonly SimulationTick m_Tick;
        ulong m_Sequence;
        readonly bool m_Enabled;

        public FixedCharacterControlTraceSink(
            List<SimulationTraceRecord> records,
            SimulationNumericProfile numericProfile,
            StableHash contentHash,
            ActorId actorId,
            SimulationTick tick,
            bool enabled)
        {
            m_Records = records ?? throw new ArgumentNullException(nameof(records));
            if (!numericProfile.IsValid || !contentHash.IsValid || !actorId.IsValid || !tick.IsValid)
                throw new ArgumentException("Fixed Character Control trace identity is incomplete.");
            m_NumericProfile = numericProfile;
            m_ContentHash = new GameplayContentHash(contentHash);
            m_ActorId = actorId;
            m_Tick = tick;
            m_Enabled = enabled;
        }

        public void Add(
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
                "Character.Control",
                code,
                detail));
        }
    }

    internal sealed class FixedCharacterControlReadPort : ICharacterControlReadPort
    {
        readonly FixedAbilityExecutionInput m_Input;
        readonly FixedAbilityBodyFacts m_Body;
        readonly Func<string, bool> m_HasInputRequest;
        readonly Func<CharacterControlParameterId, FixedScalar> m_ReadParameter;
        readonly Func<CharacterSkillId, bool> m_IsAbilityActive;
        readonly Func<CharacterSkillId, (bool Found, ulong InstanceId)> m_TryGetActiveAbilityInstanceId;
        readonly Func<CharacterSkillId, bool> m_IsAbilityCompleted;
        readonly Func<CharacterSkillId, ulong> m_CompletedAbilityInstanceId;
        readonly Func<CharacterSkillId, string, bool> m_IsActionWindowActive;
        readonly Func<EquipmentActionRouteId, (bool Found, EquipmentActionContext Context)> m_TryReadEquipmentActionContext;

        public FixedCharacterControlReadPort(
            FixedAbilityExecutionInput input,
            FixedAbilityBodyFacts body,
            Func<string, bool> hasInputRequest,
            Func<CharacterControlParameterId, FixedScalar> readParameter,
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
            FixedVector2 value = ReadValue(input, SimulationInputValueKind.Vector2).Vector2;
            return Compare(value.Magnitude, m_ReadParameter(threshold), comparison);
        }

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
    }

    internal sealed class FixedCharacterControlStatePort : ICharacterControlStatePort
    {
        readonly CharacterControlRuntimeStateTransaction m_State;
        readonly CharacterControlStateSchema m_Schema;

        public FixedCharacterControlStatePort(
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

    internal sealed class FixedCharacterControlOutputPort : ICharacterControlOutputPort
    {
        readonly CharacterControlModuleContract m_ControlModule;
        readonly FixedCharacterControlMotionRuntime m_Motion;
        readonly IReadOnlyDictionary<CharacterSkillId, IFixedAbilityActionControlPort> m_Actions;
        readonly FixedCharacterControlTraceSink m_Trace;

        public FixedCharacterControlOutputPort(
            CharacterControlModuleContract controlModule,
            FixedCharacterControlMotionRuntime motion,
            IReadOnlyDictionary<CharacterSkillId, IFixedAbilityActionControlPort> actions,
            FixedCharacterControlTraceSink trace)
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
            m_Trace.Add(source, code, SimulationTraceSeverity.Information, detail, generation);

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
        readonly CharacterControlRuntimeStateTransaction m_State;
        readonly CharacterControlStateSchema m_Schema;
        readonly FixedCharacterControlReadPort m_Read;
        readonly FixedCharacterControlOutputPort m_Output;
        readonly ActorId m_ActorId;
        readonly SimulationTick m_Tick;
        readonly int m_TickRate;

        public FixedCharacterControlRuntime(
            CharacterControlModuleCatalog controlModules,
            CharacterControlRuntimeBinding binding,
            IFixedControlRuntimeStatePort controlState,
            IFixedInputRequestStatePort inputRequests,
            IFixedActionRuntimeStatePort actionState,
            ActorId actorId,
            SimulationTick tick,
            int tickRate,
            FixedAbilityExecutionInput input,
            FixedAbilityBodyFacts body,
            FixedCharacterControlMotionRuntime motion,
            FixedCharacterControlTraceSink trace,
            IReadOnlyDictionary<CharacterSkillId, IFixedAbilityActionControlPort> actions,
            Func<CharacterSkillId, string, bool> isActionWindowActive,
            Func<EquipmentActionRouteId, (bool Found, EquipmentActionContext Context)> tryReadEquipmentActionContext)
        {
            controlModules = controlModules ?? throw new ArgumentNullException(nameof(controlModules));
            m_Binding = binding ?? throw new ArgumentNullException(nameof(binding));
            controlState = controlState ?? throw new ArgumentNullException(nameof(controlState));
            inputRequests = inputRequests ?? throw new ArgumentNullException(nameof(inputRequests));
            actionState = actionState ?? throw new ArgumentNullException(nameof(actionState));
            if (!actorId.IsValid || !tick.IsValid || tickRate <= 0)
                throw new ArgumentException("Fixed Character Control runtime identity is incomplete.");
            m_Control = controlModules.Require(binding.ModuleId);
            binding.RequireContract(m_Control.Contract);
            m_Schema = new CharacterControlStateSchema(m_Control.Contract);
            m_State = controlState.BindControl(m_Schema);
            m_ActorId = actorId;
            m_Tick = tick;
            m_TickRate = tickRate;
            m_Read = new FixedCharacterControlReadPort(
                input,
                body,
                requestId => HasInputRequest(inputRequests, requestId),
                parameter => FixedScalar.FromDouble(binding.Parameters.ReadNumeric(parameter)),
                skill => IsAbilityActive(actionState, skill),
                skill => TryGetActiveAbilityInstanceId(actionState, skill),
                skill => IsAbilityCompleted(actionState, skill),
                skill => CompletedAbilityInstanceId(actionState, skill),
                isActionWindowActive,
                tryReadEquipmentActionContext);
            m_Output = new FixedCharacterControlOutputPort(
                m_Control.Contract,
                motion,
                actions,
                trace);
        }

        public CharacterControlRuntimeStateTransaction State => m_State;

        public void Tick()
        {
            var context = new CharacterControlTickContext(m_ActorId, m_Tick, m_TickRate);
            m_State.BaseState.RequireBinding(m_Binding);
            m_Control.Tick(in context, m_Read, new FixedCharacterControlStatePort(m_State, m_Schema), m_Output);
        }

        static bool IsAbilityActive(IFixedActionRuntimeStatePort state, CharacterSkillId abilityId)
        {
            IReadOnlyList<FixedActionInstanceState> actions = state.GetActionInstances();
            for (int i = 0; i < actions.Count; i++)
                if (actions[i].IsActive && actions[i].SkillId == abilityId)
                    return true;
            return false;
        }

        static bool HasInputRequest(IFixedInputRequestStatePort state, string requestId)
        {
            SimulationInputRequestState request = state.GetInputRequest(requestId);
            return request.IsValid && !request.Consumed && request.ExpireTick >= state.Tick.Value;
        }

        static (bool Found, ulong InstanceId) TryGetActiveAbilityInstanceId(
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

        static bool IsAbilityCompleted(IFixedActionRuntimeStatePort state, CharacterSkillId abilityId) =>
            CompletedAbilityInstanceId(state, abilityId) != 0;

        static ulong CompletedAbilityInstanceId(IFixedActionRuntimeStatePort state, CharacterSkillId abilityId)
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
}
