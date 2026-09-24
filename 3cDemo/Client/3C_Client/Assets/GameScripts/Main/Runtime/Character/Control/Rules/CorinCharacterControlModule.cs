using System;
using System.Collections.Generic;
using ThirdPersonSimulation;
using UnityHFSM;

namespace ThirdPersonCharacter.Control.Rules
{
    public sealed class CorinCharacterControlModule : ICharacterControlModule
    {
        public static readonly CharacterControlModuleId ModuleId = new CharacterControlModuleId("character.corin.control");
        public static readonly CharacterControlStateId Idle = new CharacterControlStateId("Idle");
        public static readonly CharacterControlStateId WalkStart = new CharacterControlStateId("WalkStart");
        public static readonly CharacterControlStateId WalkLoop = new CharacterControlStateId("WalkLoop");
        public static readonly CharacterControlStateId WalkStopping = new CharacterControlStateId("WalkStopping");
        public static readonly CharacterControlStateId RunLoop = new CharacterControlStateId("RunLoop");
        public static readonly CharacterControlStateId RunStopping = new CharacterControlStateId("RunStopping");
        public static readonly CharacterControlStateId MovingTurn = new CharacterControlStateId("MovingTurn");
        public static readonly CharacterSkillId Attack = new CharacterSkillId("Attack");
        public static readonly CharacterSkillId BranchAttack = new CharacterSkillId("BranchAttack");
        public static readonly CharacterSkillId RushAttack = new CharacterSkillId("RushAttack");
        public static readonly CharacterSkillId DodgeBack = new CharacterSkillId("DodgeBack");
        public static readonly CharacterSkillId DodgeForward = new CharacterSkillId("DodgeForward");

        static readonly CharacterControlStateFieldId s_ActiveState = new CharacterControlStateFieldId("control:character.corin.control:active-state");
        static readonly CharacterControlStateFieldId s_EnteredTick = new CharacterControlStateFieldId("control:character.corin.control:entered-tick");
        static readonly CharacterControlStateFieldId s_LastTransition = new CharacterControlStateFieldId("control:character.corin.control:last-transition");
        static readonly CharacterControlStateFieldId s_MotionElapsed = new CharacterControlStateFieldId("control:character.corin.control:motion-elapsed-ticks");
        static readonly CharacterControlStateFieldId s_DirectionalDodgeRunIntent = new CharacterControlStateFieldId("control:character.corin.control:directional-dodge-run-intent");
        static readonly CharacterControlParameterId s_StopThreshold = new CharacterControlParameterId("StopThreshold");
        static readonly CharacterControlParameterId s_MovingTurnAngleThreshold = new CharacterControlParameterId("MovingTurnAngleThreshold");
        static readonly SimulationInputValueId s_MoveAxis = new SimulationInputValueId("MoveAxis");
        static readonly SimulationInputValueId s_LookAxis = new SimulationInputValueId("LookAxis");
        static readonly string s_AttackRequest = "Attack";
        static readonly string s_BranchRequest = "Branch";
        static readonly string s_DodgeRequest = "Dodge";
        static readonly string s_ActionTarget = "ActionTarget";
        static readonly string s_DodgeRushFollowupWindow = "RushFollowup";
        static readonly string s_RushHandoffWindow = "RushAttackHandoff";
        static readonly string s_RushHandoffEntry = "Attack4";
        static readonly string s_RushMoveExitWindow = "RushMoveExit";
        static readonly string s_WalkStartMotion = "locomotion:corin:walk-start";
        static readonly string s_WalkLoopMotion = "locomotion:corin:walk-loop";
        static readonly string s_RunLoopMotion = "locomotion:corin:run-loop";
        static readonly string s_MovingTurnMotion = "locomotion:corin:moving-turn";
        static readonly string s_MovingTurnSourceMotion = "timeline:8a6491b4-93fe-4002-a814-2ac6eb75e567/track:9b2e235b-266b-47ef-8ecd-c2fa8a4207fc/clip:e04f4e26-be58-4698-8905-36dcef1d5405";
        const double MovingTurnMotionSeconds = 28d / 60d;
        const double MovingTurnPoseExitSeconds = 53d / 60d;
        static readonly CharacterControlModuleContract s_Contract = BuildContract();

        readonly StateMachine<string, string> m_Machine;
        bool m_Initialized;
        bool m_RestoringState;

        CharacterControlTickContext m_Context;
        ICharacterControlReadPort m_Read;
        ICharacterControlStatePort m_State;
        ICharacterControlOutputPort m_Output;

        public CharacterControlModuleContract Contract => s_Contract;

        public CorinCharacterControlModule()
        {
            m_Machine = new StateMachine<string, string>();
            for (int i = 0; i < s_Contract.States.Count; i++)
            {
                CharacterControlStateId stateId = s_Contract.States[i].Id;
                m_Machine.AddState(stateId.Value, new State<string, string>(
                    onEnter: _ => OnStateEnter(stateId),
                    onLogic: _ => OnStateLogic(stateId),
                    onExit: _ => OnStateExit(stateId)));
            }
            for (int i = 0; i < s_Contract.Transitions.Count; i++)
            {
                CharacterControlTransitionDescriptor descriptor = s_Contract.Transitions[i];
                string transitionId = descriptor.Id.Value;
                m_Machine.AddTransition(new Transition<string>(
                    descriptor.Source.Value,
                    descriptor.Target.Value,
                    condition: _ => EvaluateTransition(transitionId, descriptor)));
            }
        }

        public void Tick(
            in CharacterControlTickContext context,
            ICharacterControlReadPort read,
            ICharacterControlStatePort state,
            ICharacterControlOutputPort output)
        {
            m_Context = context;
            m_Read = read;
            m_State = state;
            m_Output = output;
            if (!m_Initialized)
            {
                CharacterControlStateId activeState = m_State.ReadState(s_ActiveState);
                m_RestoringState = activeState.IsValid;
                m_Machine.SetStartState(m_RestoringState ? activeState.Value : s_Contract.InitialState.Value);
                m_Initialized = true;
                m_Machine.Init();
                m_RestoringState = false;
            }
            m_Machine.OnLogic();
        }

        void OnStateEnter(CharacterControlStateId stateId)
        {
            if (m_RestoringState)
                return;
            m_State.WriteState(s_ActiveState, stateId);
            m_State.WriteUInt64(s_EnteredTick, m_Context.Tick.Value);
            m_State.WriteInt32(s_MotionElapsed, 0);
            if (stateId == Idle)
                m_State.WriteBoolean(s_DirectionalDodgeRunIntent, false);
            Trace(stateId, default, "control_state_entered", $"state={stateId.Value}:tick={m_Context.Tick.Value}", m_Context.Tick.Value);
        }

        void OnStateLogic(CharacterControlStateId stateId)
        {
            int elapsed = m_State.ReadInt32(s_MotionElapsed);
            ulong playbackGeneration = m_State.ReadUInt64(s_EnteredTick);
            if (stateId == WalkStart)
            {
                m_Output.SubmitMotion(new CharacterControlMotionRequest(Source(stateId), s_WalkStartMotion, s_MoveAxis, elapsed, 0, playbackGeneration));
            }
            else if (stateId == WalkLoop)
            {
                m_Output.SubmitMotion(new CharacterControlMotionRequest(Source(stateId), s_WalkLoopMotion, s_MoveAxis, elapsed, 0, playbackGeneration));
            }
            else if (stateId == RunLoop)
            {
                m_Output.SubmitMotion(new CharacterControlMotionRequest(Source(stateId), s_RunLoopMotion, s_MoveAxis, elapsed, 0, playbackGeneration));
            }
            else if (stateId == MovingTurn)
            {
                string motion = elapsed < Ticks(m_Context, MovingTurnMotionSeconds)
                    ? s_MovingTurnMotion
                    : s_RunLoopMotion;
                m_Output.SubmitMotion(new CharacterControlMotionRequest(Source(stateId), motion, s_MoveAxis, elapsed, 0, playbackGeneration));
            }

            if (stateId != Idle)
                m_State.WriteInt32(s_MotionElapsed, checked(elapsed + 1));

            SubmitAbilityRequests(stateId);
        }

        void OnStateExit(CharacterControlStateId stateId)
        {
            Trace(
                stateId,
                default,
                "control_state_exited",
                $"state={stateId.Value}:tick={m_Context.Tick.Value}",
                m_State.ReadUInt64(s_EnteredTick));
        }

        bool EvaluateTransition(string transitionId, CharacterControlTransitionDescriptor descriptor)
        {
            ulong enteredTick = m_State.ReadUInt64(s_EnteredTick);
            bool result = transitionId switch
            {
                "IdleToWalkStart" => MoveAbove(m_Read),
                "WalkStartToWalkLoop" => MotionElapsed(m_State) >= Ticks(m_Context, 1.1d) &&
                    !m_State.ReadBoolean(s_DirectionalDodgeRunIntent) && MoveAbove(m_Read),
                "WalkLoopToWalkStopping" => MoveBelow(m_Read),
                "WalkStoppingToIdle" => MotionElapsed(m_State) > 0 && MoveBelow(m_Read),
                "RunLoopToRunStopping" => MoveBelow(m_Read),
                "RunStoppingToIdle" => MotionElapsed(m_State) > 0 && MoveBelow(m_Read),
                "MovingTurnToRunLoop" => MotionElapsed(m_State) >= Ticks(m_Context, MovingTurnPoseExitSeconds) && MoveAbove(m_Read) &&
                    m_Read.CompareInputDirectionToBodyYaw(s_MoveAxis, s_MovingTurnAngleThreshold, CharacterControlNumericComparison.Less),
                "WalkStartToWalkStopping" => MoveBelow(m_Read),
                "WalkStoppingToWalkStart" => MoveAbove(m_Read),
                "RunStoppingToRunLoop" => MoveAbove(m_Read),
                "MovingTurnToRunStopping" => MotionElapsed(m_State) >= Ticks(m_Context, MovingTurnPoseExitSeconds) && MoveBelow(m_Read),
                "WalkLoopToRunLoop" => m_State.ReadBoolean(s_DirectionalDodgeRunIntent) && MoveAbove(m_Read),
                "WalkStartToRunLoop" => m_State.ReadBoolean(s_DirectionalDodgeRunIntent) && MoveAbove(m_Read),
                "RunLoopToMovingTurn" => MoveAbove(m_Read) &&
                    m_Read.CompareInputDirectionToBodyYaw(s_MoveAxis, s_MovingTurnAngleThreshold, CharacterControlNumericComparison.GreaterOrEqual) &&
                    !IsAttackActive(m_Read) && !m_Read.IsAbilityActive(DodgeBack) && !m_Read.IsAbilityActive(DodgeForward),
                "MovingTurnToMovingTurn" => MotionElapsed(m_State) >= Ticks(m_Context, MovingTurnPoseExitSeconds) && MoveAbove(m_Read) &&
                    m_Read.CompareInputDirectionToBodyYaw(s_MoveAxis, s_MovingTurnAngleThreshold, CharacterControlNumericComparison.GreaterOrEqual) &&
                    !IsAttackActive(m_Read) && !m_Read.IsAbilityActive(DodgeBack) && !m_Read.IsAbilityActive(DodgeForward),
                _ => throw new InvalidOperationException($"Corin control transition '{transitionId}' is not implemented.")
            };
            Trace(
                descriptor.Source,
                descriptor.Id,
                "control_transition_evaluated",
                $"source={descriptor.Source.Value}:target={descriptor.Target.Value}:result={result}",
                enteredTick);
            if (result)
            {
                m_State.WriteTransition(s_LastTransition, descriptor.Id);
                Trace(
                    descriptor.Source,
                    descriptor.Id,
                    "control_transition_selected",
                    $"source={descriptor.Source.Value}:target={descriptor.Target.Value}:tick={m_Context.Tick.Value}",
                    m_Context.Tick.Value);
            }
            return result;
        }

        void SubmitAbilityRequests(CharacterControlStateId stateId)
        {
            SimulationExecutionSource source = Source(stateId);
            if (m_Read.HasInputRequest(s_DodgeRequest))
            {
                CharacterSkillId skill = m_Read.IsInputDirectionBehindBodyYaw(s_MoveAxis) ? DodgeBack : DodgeForward;
                ulong replacementActionInstanceId = 0;
                TryGetActiveAttackInstance(m_Read, out replacementActionInstanceId);
                m_Output.SubmitAbility(new CharacterControlAbilityRequest(
                    source,
                    skill,
                    s_DodgeRequest,
                    true,
                    replacementActionInstanceId: replacementActionInstanceId));
                return;
            }
            if (m_Read.HasInputRequest(s_BranchRequest))
            {
                if (m_Read.IsAbilityActive(BranchAttack))
                    return;
                ulong replacementActionInstanceId = 0;
                TryGetActiveAttackInstance(m_Read, out replacementActionInstanceId);
                m_Output.SubmitAbility(new CharacterControlAbilityRequest(
                    source,
                    BranchAttack,
                    s_BranchRequest,
                    true,
                    replacementActionInstanceId: replacementActionInstanceId));
                return;
            }
            if (m_Read.HasInputRequest(s_AttackRequest))
            {
                if (m_Read.IsAbilityActive(BranchAttack) ||
                    m_Read.IsAbilityActive(RushAttack) ||
                    m_Read.IsAbilityActive(DodgeForward) ||
                    m_Read.IsAbilityActive(DodgeBack))
                    return;
                if (!m_Read.IsAbilityActive(Attack) &&
                    MoveAbove(m_Read) &&
                    (stateId == RunLoop || stateId == MovingTurn))
                {
                    SubmitRushAttack(stateId, 0);
                    return;
                }
                SubmitAttack(stateId, 0, string.Empty);
                return;
            }
        }

        public void ResolveAbilityOutputs()
        {
            if (m_Read.HasInputRequest(s_AttackRequest))
            {
                CharacterControlStateId stateId = m_State.ReadState(s_ActiveState);
                if (m_Read.TryGetActiveAbilityInstanceId(DodgeForward, out ulong forwardInstanceId) &&
                    m_Read.IsAbilityWindowActive(DodgeForward, s_DodgeRushFollowupWindow))
                    SubmitRushAttack(stateId, forwardInstanceId);
                else if (m_Read.TryGetActiveAbilityInstanceId(DodgeBack, out ulong backInstanceId) &&
                         m_Read.IsAbilityWindowActive(DodgeBack, s_DodgeRushFollowupWindow))
                    SubmitRushAttack(stateId, backInstanceId);
            }
            if (m_Read.HasInputRequest(s_AttackRequest) &&
                m_Read.TryGetActiveAbilityInstanceId(RushAttack, out ulong rushInstanceId) &&
                m_Read.IsAbilityWindowActive(RushAttack, s_RushHandoffWindow))
            {
                SubmitAttack(m_State.ReadState(s_ActiveState), rushInstanceId, s_RushHandoffEntry);
                return;
            }
            if (MoveAbove(m_Read))
            {
                CharacterControlStateId stateId = m_State.ReadState(s_ActiveState);
                ResumeRunningAfterDodge(stateId, DodgeForward);
                ResumeRunningAfterDodge(stateId, DodgeBack);
                ResumeRunningAfterRush(stateId);
            }
        }

        void SubmitAttack(
            CharacterControlStateId stateId,
            ulong replacementActionInstanceId,
            string activationEntryId) =>
            m_Output.SubmitAbility(new CharacterControlAbilityRequest(
                Source(stateId),
                Attack,
                s_AttackRequest,
                true,
                s_ActionTarget,
                replacementActionInstanceId: replacementActionInstanceId,
                activationEntryId: activationEntryId));

        void SubmitRushAttack(CharacterControlStateId stateId, ulong replacementActionInstanceId) =>
            m_Output.SubmitAbility(new CharacterControlAbilityRequest(
                Source(stateId),
                RushAttack,
                s_AttackRequest,
                true,
                replacementActionInstanceId: replacementActionInstanceId));

        void ResumeRunningAfterDodge(CharacterControlStateId stateId, CharacterSkillId ability)
        {
            if (!m_Read.TryGetActiveAbilityInstanceId(ability, out ulong instanceId) ||
                !m_Read.IsAbilityWindowActive(ability, "RecoveryOpen"))
                return;
            m_Output.SubmitAbilityStop(new CharacterControlAbilityStopRequest(
                Source(stateId), ability, CharacterControlAbilityStopMode.Graceful,
                "DodgeRecoveryMovement", instanceId, "RecoveryOpen"));
            m_State.WriteBoolean(s_DirectionalDodgeRunIntent, true);
        }

        void ResumeRunningAfterRush(CharacterControlStateId stateId)
        {
            if (!m_Read.TryGetActiveAbilityInstanceId(RushAttack, out ulong instanceId) ||
                !m_Read.IsAbilityWindowActive(RushAttack, s_RushMoveExitWindow))
                return;
            m_Output.SubmitAbilityStop(new CharacterControlAbilityStopRequest(
                Source(stateId), RushAttack, CharacterControlAbilityStopMode.Graceful,
                "RushMovement", instanceId, s_RushMoveExitWindow));
            m_State.WriteBoolean(s_DirectionalDodgeRunIntent, true);
        }

        bool TryGetActiveAttackInstance(ICharacterControlReadPort read, out ulong instanceId) =>
            read.TryGetActiveAbilityInstanceId(BranchAttack, out instanceId) ||
            read.TryGetActiveAbilityInstanceId(Attack, out instanceId);

        bool IsAttackActive(ICharacterControlReadPort read) => read.IsAbilityActive(Attack);

        SimulationExecutionSource Source(CharacterControlStateId stateId) =>
            SimulationExecutionSource.FromCharacterControl(ModuleId, stateId, default);

        void Trace(
            CharacterControlStateId stateId,
            CharacterControlTransitionId transitionId,
            string code,
            string detail,
            ulong generation)
        {
            m_Output.Trace(
                SimulationExecutionSource.FromCharacterControl(ModuleId, stateId, transitionId),
                code,
                detail,
                generation == 0 ? 1 : generation);
        }

        bool MoveAbove(ICharacterControlReadPort read) => read.CompareInputVector2Magnitude(s_MoveAxis, s_StopThreshold, CharacterControlNumericComparison.Greater);
        bool MoveBelow(ICharacterControlReadPort read) => read.CompareInputVector2Magnitude(s_MoveAxis, s_StopThreshold, CharacterControlNumericComparison.Less);
        static int MotionElapsed(ICharacterControlStateReadPort state) => state.ReadInt32(s_MotionElapsed);
        static int Ticks(in CharacterControlTickContext context, double seconds) => checked((int)Math.Ceiling(seconds * context.TickRate));

        static CharacterControlModuleContract BuildContract()
        {
            return new CharacterControlModuleContract(
                ModuleId,
                1,
                Idle,
                new[]
                {
                    new CharacterControlStateDescriptor(Idle, 0),
                    new CharacterControlStateDescriptor(WalkStart, 1),
                    new CharacterControlStateDescriptor(WalkLoop, 2),
                    new CharacterControlStateDescriptor(WalkStopping, 3),
                    new CharacterControlStateDescriptor(RunLoop, 4),
                    new CharacterControlStateDescriptor(RunStopping, 5),
                    new CharacterControlStateDescriptor(MovingTurn, 6)
                },
                new[]
                {
                    Transition("IdleToWalkStart", Idle, WalkStart, 1, 0),
                    Transition("WalkStartToWalkLoop", WalkStart, WalkLoop, 100, 1),
                    Transition("WalkLoopToWalkStopping", WalkLoop, WalkStopping, 1, 2),
                    Transition("WalkStoppingToIdle", WalkStopping, Idle, 100, 3),
                    Transition("RunLoopToRunStopping", RunLoop, RunStopping, 0, 4),
                    Transition("RunStoppingToIdle", RunStopping, Idle, 100, 5),
                    Transition("MovingTurnToRunLoop", MovingTurn, RunLoop, 100, 6),
                    Transition("WalkStartToWalkStopping", WalkStart, WalkStopping, 1, 7),
                    Transition("WalkStoppingToWalkStart", WalkStopping, WalkStart, 1, 8),
                    Transition("RunStoppingToRunLoop", RunStopping, RunLoop, 1, 9),
                    Transition("MovingTurnToRunStopping", MovingTurn, RunStopping, 0, 10),
                    Transition("WalkLoopToRunLoop", WalkLoop, RunLoop, 0, 11),
                    Transition("WalkStartToRunLoop", WalkStart, RunLoop, 100, 12),
                    Transition("RunLoopToMovingTurn", RunLoop, MovingTurn, 2, 13),
                    Transition("MovingTurnToMovingTurn", MovingTurn, MovingTurn, 1, 14)
                },
                new[]
                {
                    new CharacterControlStateFieldDescriptor(s_ActiveState, CharacterControlStateValueKind.Identity, CharacterControlStateSemantic.ActiveState),
                    new CharacterControlStateFieldDescriptor(s_EnteredTick, CharacterControlStateValueKind.UInt64, CharacterControlStateSemantic.EnteredTick),
                    new CharacterControlStateFieldDescriptor(s_LastTransition, CharacterControlStateValueKind.Identity, CharacterControlStateSemantic.Transition),
                    new CharacterControlStateFieldDescriptor(s_MotionElapsed, CharacterControlStateValueKind.Int32, CharacterControlStateSemantic.StateValue),
                    new CharacterControlStateFieldDescriptor(s_DirectionalDodgeRunIntent, CharacterControlStateValueKind.Boolean, CharacterControlStateSemantic.StateValue)
                },
                new[]
                {
                    new CharacterControlParameterDescriptor(s_StopThreshold, SemanticValueKind.Number, 0.05d),
                    new CharacterControlParameterDescriptor(s_MovingTurnAngleThreshold, SemanticValueKind.Number, 135d)
                },
                new[] { s_MoveAxis, s_LookAxis },
                new[]
                {
                    new CharacterControlMotionDescriptor(s_WalkStartMotion, s_MoveAxis, 4.592d, 720d, CharacterControlMotionExecutionMode.Timed, 1.1d, string.Empty, CharacterControlMotionDisplacementMode.ConstantSpeed, CharacterControlMotionSpace.CameraRelative),
                    new CharacterControlMotionDescriptor(s_WalkLoopMotion, s_MoveAxis, 6d, 720d, CharacterControlMotionExecutionMode.Continuous, 0d, string.Empty, CharacterControlMotionDisplacementMode.ConstantSpeed, CharacterControlMotionSpace.CameraRelative),
                    new CharacterControlMotionDescriptor(s_RunLoopMotion, s_MoveAxis, 7.36d, 720d, CharacterControlMotionExecutionMode.Continuous, 0d, string.Empty, CharacterControlMotionDisplacementMode.ConstantSpeed, CharacterControlMotionSpace.CameraRelative),
                    new CharacterControlMotionDescriptor(
                        s_MovingTurnMotion,
                        s_MoveAxis,
                        0d,
                        0d,
                        CharacterControlMotionExecutionMode.Timed,
                        MovingTurnMotionSeconds,
                        s_MovingTurnSourceMotion,
                        CharacterControlMotionDisplacementMode.SourceCurve,
                        CharacterControlMotionSpace.ActorLocal,
                        100,
                        true)
                },
                new[] { Attack, BranchAttack, DodgeBack, DodgeForward, RushAttack });
        }

        static CharacterControlTransitionDescriptor Transition(
            string id,
            CharacterControlStateId source,
            CharacterControlStateId target,
            int priority,
            int order) => new CharacterControlTransitionDescriptor(new CharacterControlTransitionId(id), source, target, priority, order);
    }

    public static class CorinCharacterControlModuleCatalog
    {
        public static CharacterControlModuleCatalog Create() =>
            new CharacterControlModuleCatalog(new Func<ICharacterControlModule>[] { () => new CorinCharacterControlModule() });
    }
}
