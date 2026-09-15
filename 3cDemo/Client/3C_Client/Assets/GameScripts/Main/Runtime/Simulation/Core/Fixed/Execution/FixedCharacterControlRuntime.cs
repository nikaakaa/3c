using System;
using System.Collections.Generic;
using ThirdPersonSimulation;

namespace ThirdPersonSimulation.Fixed
{
    internal sealed class FixedCharacterControlReadPort : ICharacterControlReadPort
    {
        readonly IReadOnlyList<SimulationInputValue> m_InputValues;
        readonly FixedCharacterBodyFacts m_Body;
        readonly Func<string, bool> m_HasInputRequest;
        readonly Func<CharacterControlParameterId, FixedScalar> m_ReadParameter;
        readonly Func<CharacterSkillId, bool> m_IsAbilityActive;
        readonly Func<CharacterSkillId, (bool Found, ulong InstanceId)> m_TryGetActiveAbilityInstanceId;
        readonly Func<CharacterSkillId, bool> m_IsAbilityCompleted;
        readonly Func<CharacterSkillId, ulong> m_CompletedAbilityInstanceId;
        readonly Func<CharacterSkillId, string, bool> m_IsActionWindowActive;
        readonly Func<EquipmentActionRouteId, (bool Found, EquipmentActionContext Context)> m_TryReadEquipmentActionContext;

        public FixedCharacterControlReadPort(
            IReadOnlyList<SimulationInputValue> inputValues,
            FixedCharacterBodyFacts body,
            Func<string, bool> hasInputRequest,
            Func<CharacterControlParameterId, FixedScalar> readParameter,
            Func<CharacterSkillId, bool> isAbilityActive,
            Func<CharacterSkillId, (bool Found, ulong InstanceId)> tryGetActiveAbilityInstanceId,
            Func<CharacterSkillId, bool> isAbilityCompleted,
            Func<CharacterSkillId, ulong> completedAbilityInstanceId,
            Func<CharacterSkillId, string, bool> isActionWindowActive,
            Func<EquipmentActionRouteId, (bool Found, EquipmentActionContext Context)> tryReadEquipmentActionContext)
        {
            m_InputValues = inputValues ?? throw new ArgumentNullException(nameof(inputValues));
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
            for (int i = 0; i < m_InputValues.Count; i++)
            {
                SimulationInputValue value = m_InputValues[i];
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

    internal sealed class FixedCharacterControlMotionRuntime
    {
        readonly CharacterControlModuleContract m_Contract;
        readonly CharacterControlRuntimeBinding m_Binding;
        readonly FixedCharacterBodyFacts m_Body;
        readonly SimulationTick m_Tick;
        readonly int m_TickRate;
        readonly IReadOnlyList<SimulationInputValue> m_InputValues;
        readonly List<SimulationMotionContribution> m_Contributions =
            new List<SimulationMotionContribution>();

        public FixedCharacterControlMotionRuntime(
            CharacterControlModuleContract contract,
            CharacterControlRuntimeBinding binding,
            FixedCharacterBodyFacts body,
            SimulationTick tick,
            int tickRate,
            IReadOnlyList<SimulationInputValue> inputValues)
        {
            m_Contract = contract ?? throw new ArgumentNullException(nameof(contract));
            m_Binding = binding ?? throw new ArgumentNullException(nameof(binding));
            if (!body.IsValid || !tick.IsValid || tickRate <= 0)
                throw new ArgumentException("Fixed Character Control motion identity is incomplete.");
            m_Body = body;
            m_Tick = tick;
            m_TickRate = tickRate;
            m_InputValues = inputValues ?? throw new ArgumentNullException(nameof(inputValues));
        }

        public void Submit(CharacterControlMotionRequest request)
        {
            CharacterControlMotionDescriptor descriptor = RequireMotion(request.Binding);
            FixedVector2 move = FixedVector2.Zero;
            FixedVector3 displacement;
            FixedScalar yaw;
            if (descriptor.DisplacementMode == CharacterControlMotionDisplacementMode.SourceCurve)
            {
                CharacterControlMotionBinding source = m_Binding.MotionBindings.Require(descriptor.SourceMotionIdentity);
                double tickRate = m_TickRate;
                CharacterControlMotionDelta delta = source.EvaluateDelta(
                    request.ContinuousTicks / tickRate,
                    (request.ContinuousTicks + 1) / tickRate);
                displacement = new FixedVector3(
                    FixedScalar.FromDouble(delta.X),
                    FixedScalar.FromDouble(delta.Y),
                    FixedScalar.FromDouble(delta.Z));
                yaw = FixedScalar.FromDouble(delta.Yaw);
            }
            else
            {
                move = ReadValue(request.Input.Value, SimulationInputValueKind.Vector2).Vector2;
                if (move.SqrMagnitude > FixedScalar.One)
                    move = move.Normalized;
                FixedScalar delta = FixedScalar.One / FixedScalar.FromInt64(m_TickRate);
                FixedScalar moveSpeed = FixedScalar.FromDouble(descriptor.MoveSpeed);
                FixedScalar turnSpeed = FixedScalar.FromDouble(descriptor.TurnSpeedDegrees);
                FixedScalar maxYaw = turnSpeed * delta;
                displacement = new FixedVector3(
                    move.X * moveSpeed * delta,
                    FixedScalar.Zero,
                    move.Y * moveSpeed * delta);
                yaw = FixedScalar.Zero;
                if (move != FixedVector2.Zero && maxYaw > FixedScalar.Zero)
                {
                    FixedYaw desired = FixedAngle.FromPlanarDirection(move);
                    yaw = FixedScalar.Clamp(FixedAngle.Delta(m_Body.Yaw, desired), -maxYaw, maxYaw);
                }
            }

            FixedScalar motionDelta = FixedScalar.One / FixedScalar.FromInt64(m_TickRate);
            int continuousTicks = checked(request.ContinuousTicks + 1);
            int durationTicks = descriptor.DisplacementMode == CharacterControlMotionDisplacementMode.SourceCurve
                ? 0
                : descriptor.ExecutionMode == CharacterControlMotionExecutionMode.Timed
                ? checked((int)Math.Ceiling(descriptor.DurationSeconds * m_TickRate))
                : 0;
            var movementPlaybackClock = new CommittedMovementPlaybackClock(
                request.Source.Identity,
                request.PlaybackGeneration,
                m_Tick,
                continuousTicks,
                m_TickRate);
            var locomotionTimeline = new CommittedLocomotionPlanarMotionTimeline(
                request.Source.Identity,
                request.PlaybackGeneration,
                m_Tick,
                m_TickRate,
                (displacement.X / motionDelta).ToSingle(),
                (displacement.Z / motionDelta).ToSingle(),
                (yaw / motionDelta).ToSingle(),
                (FixedScalar.FromDouble(descriptor.TurnSpeedDegrees)).ToSingle(),
                durationTicks,
                string.Empty,
                0f,
                0f);
            m_Contributions.Add(new SimulationMotionContribution(
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

        public ResolvedGameplayMotion Resolve()
        {
            if (m_Contributions.Count == 0)
                return new ResolvedGameplayMotion(
                    FixedVector3.Zero,
                    FixedScalar.Zero,
                    FixedVector2.Zero,
                    false,
                    default,
                    default,
                    string.Empty,
                    string.Empty);

            FixedVector3 displacement = FixedVector3.Zero;
            FixedScalar yaw = FixedScalar.Zero;
            FixedVector2 planarBasis = FixedVector2.Zero;
            FixedScalar totalWeight = FixedScalar.Zero;
            FixedVector3 weightedDisplacement = FixedVector3.Zero;
            FixedScalar weightedYaw = FixedScalar.Zero;
            SimulationMotionContribution winner = default;
            bool hasAdditive = false;
            bool hasWeighted = false;
            bool hasOverride = false;
            for (int i = 0; i < m_Contributions.Count; i++)
            {
                SimulationMotionContribution contribution = m_Contributions[i];
                if (!contribution.CanResolve)
                    continue;
                FixedVector3 resolved = contribution.Space == SimulationMotionContributionSpace.ActorLocal
                    ? FixedAngle.RotatePlanar(contribution.Displacement, m_Body.Yaw)
                    : contribution.Displacement;
                switch (contribution.BlendMode)
                {
                    case SimulationMotionBlendMode.Additive:
                        displacement += resolved * contribution.Weight;
                        yaw += contribution.YawDegrees * contribution.Weight;
                        hasAdditive = true;
                        break;
                    case SimulationMotionBlendMode.WeightedBlend:
                        weightedDisplacement += resolved * contribution.Weight;
                        weightedYaw += contribution.YawDegrees * contribution.Weight;
                        totalWeight += contribution.Weight;
                        hasWeighted = true;
                        break;
                    case SimulationMotionBlendMode.Override:
                        if (!hasOverride || contribution.Priority > winner.Priority)
                        {
                            winner = contribution;
                            hasOverride = true;
                        }
                        break;
                    default:
                        throw new InvalidOperationException(
                            $"Character Control motion '{contribution.SourceIdentity}' has invalid blend mode '{contribution.BlendMode}'.");
                }
            }
            if (hasOverride)
            {
                FixedVector3 resolved = winner.Space == SimulationMotionContributionSpace.ActorLocal
                    ? FixedAngle.RotatePlanar(winner.Displacement, m_Body.Yaw)
                    : winner.Displacement;
                displacement += resolved * winner.Weight;
                yaw += winner.YawDegrees * winner.Weight;
                planarBasis = winner.PlanarBasis;
            }
            else if (hasWeighted && totalWeight > FixedScalar.Zero)
            {
                displacement += new FixedVector3(
                    weightedDisplacement.X / totalWeight,
                    weightedDisplacement.Y / totalWeight,
                    weightedDisplacement.Z / totalWeight);
                yaw += weightedYaw / totalWeight;
            }
            if (!hasAdditive && !hasWeighted && !hasOverride)
                return new ResolvedGameplayMotion(
                    FixedVector3.Zero,
                    FixedScalar.Zero,
                    FixedVector2.Zero,
                    false,
                    default,
                    default,
                    string.Empty,
                    string.Empty);
            if (!hasOverride)
                throw new InvalidOperationException("Character Control locomotion has no single committed playback clock owner.");
            return new ResolvedGameplayMotion(
                displacement,
                yaw,
                planarBasis,
                displacement != FixedVector3.Zero || yaw != FixedScalar.Zero,
                winner.MovementPlaybackClock,
                winner.LocomotionTimeline,
                string.Empty,
                string.Empty);
        }

        SimulationInputValue ReadValue(string inputId, SimulationInputValueKind kind)
        {
            for (int i = 0; i < m_InputValues.Count; i++)
            {
                SimulationInputValue value = m_InputValues[i];
                if (!string.Equals(value.InputId, inputId, StringComparison.Ordinal))
                    continue;
                if (value.Kind != kind)
                    throw new InvalidOperationException($"Input '{inputId}' is '{value.Kind}', expected '{kind}'.");
                return value;
            }
            throw new InvalidOperationException($"Tick input does not contain required value '{inputId}'.");
        }

        CharacterControlMotionDescriptor RequireMotion(string binding)
        {
            for (int i = 0; i < m_Contract.Motions.Count; i++)
            {
                CharacterControlMotionDescriptor motion = m_Contract.Motions[i];
                if (string.Equals(motion.Binding, binding, StringComparison.Ordinal))
                    return motion;
            }
            throw new InvalidOperationException($"Control module '{m_Contract.ModuleId}' has no motion '{binding}'.");
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
        readonly FixedInputRuntime m_Input;
        readonly FixedLocomotionRuntime m_Locomotion;
        readonly FixedCharacterControlMotionRuntime m_CharacterMotion;
        readonly IReadOnlyDictionary<CharacterSkillId, FixedActionRuntime> m_Actions;
        readonly FixedTraceSink m_Trace;
        readonly Action<SimulationExecutionSource, string, string, ulong> m_TraceAction;

        public FixedCharacterControlOutputPort(
            CharacterControlModuleContract controlModule,
            FixedInputRuntime input,
            FixedLocomotionRuntime locomotion,
            IReadOnlyDictionary<CharacterSkillId, FixedActionRuntime> actions,
            FixedTraceSink trace)
        {
            m_ControlModule = controlModule ?? throw new ArgumentNullException(nameof(controlModule));
            m_Input = input ?? throw new ArgumentNullException(nameof(input));
            m_Locomotion = locomotion ?? throw new ArgumentNullException(nameof(locomotion));
            m_CharacterMotion = null;
            m_Actions = actions ?? throw new ArgumentNullException(nameof(actions));
            m_Trace = trace ?? throw new ArgumentNullException(nameof(trace));
            m_TraceAction = (source, code, detail, generation) => m_Trace.Add(source, code, detail, generation);
        }

        public FixedCharacterControlOutputPort(
            CharacterControlModuleContract controlModule,
            FixedCharacterControlMotionRuntime characterMotion,
            IReadOnlyDictionary<CharacterSkillId, FixedActionRuntime> actions,
            Action<SimulationExecutionSource, string, string, ulong> trace)
        {
            m_ControlModule = controlModule ?? throw new ArgumentNullException(nameof(controlModule));
            m_Input = null;
            m_Locomotion = null;
            m_CharacterMotion = characterMotion ?? throw new ArgumentNullException(nameof(characterMotion));
            m_Actions = actions ?? throw new ArgumentNullException(nameof(actions));
            m_Trace = null;
            m_TraceAction = trace ?? throw new ArgumentNullException(nameof(trace));
        }

        public void SubmitMotion(CharacterControlMotionRequest request)
        {
            if (m_CharacterMotion != null)
            {
                m_CharacterMotion.Submit(request);
                return;
            }
            CharacterControlMotionDescriptor descriptor = RequireMotion(request.Binding);
            m_Locomotion.SubmitControl(m_Input, request, descriptor);
        }

        public bool SubmitAbility(CharacterControlAbilityRequest request) =>
            RequireAction(request.AbilityId).ActivateFromControl(request);

        public void SubmitAbilityStop(CharacterControlAbilityStopRequest request) =>
            RequireAction(request.AbilityId).StopFromControl(request);

        public void Trace(SimulationExecutionSource source, string code, string detail, ulong generation) =>
            m_TraceAction(source, code, detail, generation);

        public ResolvedGameplayMotion Motion => m_CharacterMotion == null
            ? new ResolvedGameplayMotion(
                FixedVector3.Zero,
                FixedScalar.Zero,
                FixedVector2.Zero,
                false,
                default,
                default,
                string.Empty,
                string.Empty)
            : m_CharacterMotion.Resolve();

        FixedActionRuntime RequireAction(CharacterSkillId abilityId)
        {
            if (!m_Actions.TryGetValue(abilityId, out FixedActionRuntime action))
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
            FixedCharacterRuntimeStateTransaction roleState,
            ActorId actorId,
            SimulationTick tick,
            int tickRate,
            FixedAbilityExecutionInput input,
            FixedCharacterBodyFacts body,
            FixedInputRuntime inputRuntime,
            FixedLocomotionRuntime locomotion,
            FixedTraceSink trace,
            IReadOnlyDictionary<CharacterSkillId, FixedActionRuntime> actions,
            Func<CharacterSkillId, string, bool> isActionWindowActive,
            Func<EquipmentActionRouteId, (bool Found, EquipmentActionContext Context)> tryReadEquipmentActionContext)
            : this(
                RequireControl(controlModules, binding),
                binding,
                roleState,
                actorId,
                tick,
                tickRate,
                CreateReadPort(
                    input,
                    body,
                    roleState,
                    binding,
                    isActionWindowActive,
                    tryReadEquipmentActionContext),
                new FixedCharacterControlOutputPort(
                    RequireControl(controlModules, binding).Contract,
                    inputRuntime,
                    locomotion,
                    actions,
                    trace))
        {
        }

        public FixedCharacterControlRuntime(
            CharacterControlModuleCatalog controlModules,
            CharacterControlRuntimeBinding binding,
            FixedCharacterRuntimeStateTransaction roleState,
            ActorId actorId,
            SimulationTick tick,
            int tickRate,
            CharacterSimulationInput input,
            FixedCharacterBodyFacts body,
            FixedCharacterControlMotionRuntime characterMotion,
            Action<SimulationExecutionSource, string, string, ulong> trace)
            : this(
                RequireControl(controlModules, binding),
                binding,
                roleState,
                actorId,
                tick,
                tickRate,
                CreateReadPort(input, body, roleState, binding),
                new FixedCharacterControlOutputPort(
                    RequireControl(controlModules, binding).Contract,
                    characterMotion,
                    new Dictionary<CharacterSkillId, FixedActionRuntime>(),
                    trace))
        {
        }

        FixedCharacterControlRuntime(
            ICharacterControlModule control,
            CharacterControlRuntimeBinding binding,
            FixedCharacterRuntimeStateTransaction roleState,
            ActorId actorId,
            SimulationTick tick,
            int tickRate,
            FixedCharacterControlReadPort read,
            FixedCharacterControlOutputPort output)
        {
            m_Control = control ?? throw new ArgumentNullException(nameof(control));
            m_Binding = binding ?? throw new ArgumentNullException(nameof(binding));
            roleState = roleState ?? throw new ArgumentNullException(nameof(roleState));
            m_Read = read ?? throw new ArgumentNullException(nameof(read));
            m_Output = output ?? throw new ArgumentNullException(nameof(output));
            if (!actorId.IsValid || !tick.IsValid || tickRate <= 0)
                throw new ArgumentException("Fixed Character Control runtime identity is incomplete.");
            binding.RequireContract(m_Control.Contract);
            m_Schema = new CharacterControlStateSchema(m_Control.Contract);
            m_State = roleState.BindControl(m_Schema);
            m_ActorId = actorId;
            m_Tick = tick;
            m_TickRate = tickRate;
        }

        public ResolvedGameplayMotion Motion => m_Output.Motion;

        public CharacterControlRuntimeStateTransaction State => m_State;

        public void Tick()
        {
            var context = new CharacterControlTickContext(m_ActorId, m_Tick, m_TickRate);
            m_State.BaseState.RequireBinding(m_Binding);
            m_Control.Tick(in context, m_Read, new FixedCharacterControlStatePort(m_State, m_Schema), m_Output);
        }

        static ICharacterControlModule RequireControl(
            CharacterControlModuleCatalog controlModules,
            CharacterControlRuntimeBinding binding)
        {
            if (controlModules == null)
                throw new ArgumentNullException(nameof(controlModules));
            if (binding == null)
                throw new ArgumentNullException(nameof(binding));
            return controlModules.Require(binding.ModuleId);
        }

        static FixedCharacterControlReadPort CreateReadPort(
            FixedAbilityExecutionInput input,
            FixedCharacterBodyFacts body,
            FixedCharacterRuntimeStateTransaction roleState,
            CharacterControlRuntimeBinding binding,
            Func<CharacterSkillId, string, bool> isActionWindowActive,
            Func<EquipmentActionRouteId, (bool Found, EquipmentActionContext Context)> tryReadEquipmentActionContext)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));
            if (roleState == null)
                throw new ArgumentNullException(nameof(roleState));
            if (binding == null)
                throw new ArgumentNullException(nameof(binding));
            return new FixedCharacterControlReadPort(
                input.Values,
                body,
                requestId => HasInputRequest(roleState, requestId),
                parameter => FixedScalar.FromDouble(binding.Parameters.ReadNumeric(parameter)),
                skill => IsAbilityActive(roleState, skill),
                skill => TryGetActiveAbilityInstanceId(roleState, skill),
                skill => IsAbilityCompleted(roleState, skill),
                skill => CompletedAbilityInstanceId(roleState, skill),
                isActionWindowActive,
                tryReadEquipmentActionContext);
        }

        static FixedCharacterControlReadPort CreateReadPort(
            CharacterSimulationInput input,
            FixedCharacterBodyFacts body,
            FixedCharacterRuntimeStateTransaction roleState,
            CharacterControlRuntimeBinding binding)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));
            if (roleState == null)
                throw new ArgumentNullException(nameof(roleState));
            if (binding == null)
                throw new ArgumentNullException(nameof(binding));
            return new FixedCharacterControlReadPort(
                input.Values,
                body,
                requestId => HasInputRequest(roleState, requestId),
                parameter => FixedScalar.FromDouble(binding.Parameters.ReadNumeric(parameter)),
                skill => IsAbilityActive(roleState, skill),
                skill => TryGetActiveAbilityInstanceId(roleState, skill),
                skill => IsAbilityCompleted(roleState, skill),
                skill => CompletedAbilityInstanceId(roleState, skill),
                (skill, window) => false,
                route => (false, default));
        }

        static bool IsAbilityActive(FixedCharacterRuntimeStateTransaction state, CharacterSkillId abilityId)
        {
            IReadOnlyList<FixedActionInstanceState> actions = state.GetActionInstances();
            for (int i = 0; i < actions.Count; i++)
                if (actions[i].IsActive && actions[i].SkillId == abilityId)
                    return true;
            return false;
        }

        static bool HasInputRequest(FixedCharacterRuntimeStateTransaction state, string requestId)
        {
            SimulationInputRequestState request = state.GetInputRequest(requestId);
            return request.IsValid && !request.Consumed && request.ExpireTick >= state.Tick.Value;
        }

        static (bool Found, ulong InstanceId) TryGetActiveAbilityInstanceId(
            FixedCharacterRuntimeStateTransaction state,
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

        static bool IsAbilityCompleted(FixedCharacterRuntimeStateTransaction state, CharacterSkillId abilityId) =>
            CompletedAbilityInstanceId(state, abilityId) != 0;

        static ulong CompletedAbilityInstanceId(FixedCharacterRuntimeStateTransaction state, CharacterSkillId abilityId)
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
