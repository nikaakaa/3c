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
        public static readonly CharacterSkillId DodgeBack = new CharacterSkillId("DodgeBack");
        public static readonly CharacterSkillId DodgeForward = new CharacterSkillId("DodgeForward");

        static readonly CharacterControlStateFieldId s_ActiveState = new CharacterControlStateFieldId("control:character.corin.control:active-state");
        static readonly CharacterControlStateFieldId s_EnteredTick = new CharacterControlStateFieldId("control:character.corin.control:entered-tick");
        static readonly CharacterControlStateFieldId s_LastTransition = new CharacterControlStateFieldId("control:character.corin.control:last-transition");
        static readonly CharacterControlStateFieldId s_TransitionProgress = new CharacterControlStateFieldId("control:character.corin.control:transition-progress");
        static readonly CharacterControlStateFieldId s_MotionElapsed = new CharacterControlStateFieldId("control:character.corin.control:motion-elapsed-ticks");
        static readonly CharacterControlStateFieldId s_DirectionalDodgeRunIntent = new CharacterControlStateFieldId("control:character.corin.control:directional-dodge-run-intent");
        static readonly CharacterControlStateFieldId s_DodgeForwardCompletionInstance = new CharacterControlStateFieldId("control:character.corin.control:dodge-forward-completion-instance");
        static readonly CharacterControlParameterId s_StopThreshold = new CharacterControlParameterId("StopThreshold");
        static readonly CharacterControlParameterId s_MovingTurnAngleThreshold = new CharacterControlParameterId("MovingTurnAngleThreshold");
        static readonly SimulationInputValueId s_MoveAxis = new SimulationInputValueId("MoveAxis");
        static readonly SimulationInputValueId s_LookAxis = new SimulationInputValueId("LookAxis");
        static readonly string s_AttackRequest = "Attack";
        static readonly string s_DodgeRequest = "Dodge";
        static readonly string s_ActionTarget = "ActionTarget";
        static readonly string s_WalkStartMotion = "locomotion:corin:walk-start";
        static readonly string s_WalkLoopMotion = "locomotion:corin:walk-loop";
        static readonly string s_RunLoopMotion = "locomotion:corin:run-loop";
        static readonly string s_MovingTurnMotion = "locomotion:corin:moving-turn";
        static readonly string s_MovingTurnSourceMotion = "timeline:8a6491b4-93fe-4002-a814-2ac6eb75e567/clip:e04f4e26-be58-4698-8905-36dcef1d5405";
        static readonly CharacterControlModuleContract s_Contract = BuildContract();

        // UnityHFSM 骨架:状态/迁移/条件的唯一登记处。状态真值仍存仿真状态槽
        // (CharacterControlStateMachineRuntime 按 Priority/Order 推进),本机器只承载
        // 声明与条件求值,不持有活跃状态,因此与 rollback 天然一致。
        readonly StateMachine<string, string> m_Machine;
        readonly Dictionary<string, Transition<string>> m_TransitionsById =
            new Dictionary<string, Transition<string>>(StringComparer.Ordinal);

        CharacterControlTickContext m_EvaluatedContext;
        ICharacterControlReadPort m_EvaluatedRead;
        ICharacterControlStateReadPort m_EvaluatedState;

        public CharacterControlModuleContract Contract => s_Contract;

        public CorinCharacterControlModule()
        {
            m_Machine = new StateMachine<string, string>();
            m_Machine.SetStartState(Idle.Value);
            for (int i = 0; i < s_Contract.States.Count; i++)
                m_Machine.AddState(s_Contract.States[i].Id.Value, new State<string, string>());
            for (int i = 0; i < s_Contract.Transitions.Count; i++)
            {
                CharacterControlTransitionDescriptor descriptor = s_Contract.Transitions[i];
                string id = descriptor.Id.Value;
                var transition = new Transition<string>(
                    descriptor.Source.Value,
                    descriptor.Target.Value,
                    condition: _ => EvaluateRegistered(id));
                m_TransitionsById.Add(id, transition);
                m_Machine.AddTransition(transition);
            }
        }

        bool EvaluateRegistered(string transitionId)
        {
            switch (transitionId)
            {
                case "IdleToWalkStart": return MoveAbove(m_EvaluatedRead);
                case "WalkStartToWalkLoop": return MotionElapsed(m_EvaluatedState) >= Ticks(m_EvaluatedContext, 1.1d) && MoveAbove(m_EvaluatedRead);
                case "WalkLoopToWalkStopping": return MoveBelow(m_EvaluatedRead);
                case "WalkStoppingToIdle": return MotionElapsed(m_EvaluatedState) > 0 && MoveBelow(m_EvaluatedRead);
                case "RunLoopToRunStopping": return MoveBelow(m_EvaluatedRead);
                case "RunStoppingToIdle": return MotionElapsed(m_EvaluatedState) > 0 && MoveBelow(m_EvaluatedRead);
                case "MovingTurnToRunLoop":
                    return MotionElapsed(m_EvaluatedState) >= Ticks(m_EvaluatedContext, 28d / 30d) && m_EvaluatedState.ReadBoolean(s_DirectionalDodgeRunIntent) && MoveAbove(m_EvaluatedRead);
                case "WalkStartToWalkStopping": return MoveBelow(m_EvaluatedRead);
                case "WalkStoppingToWalkStart": return MoveAbove(m_EvaluatedRead);
                case "RunStoppingToRunLoop": return MoveAbove(m_EvaluatedRead);
                case "MovingTurnToWalkStopping": return MotionElapsed(m_EvaluatedState) >= Ticks(m_EvaluatedContext, 28d / 30d) && MoveBelow(m_EvaluatedRead);
                case "WalkLoopToRunLoop": return m_EvaluatedState.ReadBoolean(s_DirectionalDodgeRunIntent);
                case "WalkStartToRunLoop": return m_EvaluatedState.ReadBoolean(s_DirectionalDodgeRunIntent);
                case "RunLoopToMovingTurn":
                    return MoveAbove(m_EvaluatedRead) &&
                           m_EvaluatedRead.CompareInputDirectionToBodyYaw(s_MoveAxis, s_MovingTurnAngleThreshold, CharacterControlNumericComparison.GreaterOrEqual) &&
                           !IsAttackActive(m_EvaluatedRead) && !m_EvaluatedRead.IsSkillActive(DodgeBack) && !m_EvaluatedRead.IsSkillActive(DodgeForward);
                case "MovingTurnToWalkLoop":
                    return MotionElapsed(m_EvaluatedState) >= Ticks(m_EvaluatedContext, 28d / 30d) && !m_EvaluatedState.ReadBoolean(s_DirectionalDodgeRunIntent) && MoveAbove(m_EvaluatedRead);
                default: throw new InvalidOperationException($"Corin control transition '{transitionId}' is not implemented.");
            }
        }

        static int MotionElapsed(ICharacterControlStateReadPort state) => state.ReadInt32(s_MotionElapsed);

        public void Enter(
            in CharacterControlTickContext context,
            CharacterControlStateId stateId,
            ICharacterControlReadPort read,
            ICharacterControlStatePort state,
            ICharacterControlOutputPort output)
        {
            state.WriteInt32(s_MotionElapsed, 0);
            if (stateId == Idle)
                state.WriteBoolean(s_DirectionalDodgeRunIntent, false);
        }

        public void Tick(
            in CharacterControlTickContext context,
            CharacterControlStateId stateId,
            ICharacterControlReadPort read,
            ICharacterControlStatePort state,
            ICharacterControlOutputPort output)
        {
            int elapsed = state.ReadInt32(s_MotionElapsed);
            if (stateId == WalkStart)
            {
                output.SubmitMotion(new CharacterControlMotionRequest(Source(stateId), s_WalkStartMotion, s_MoveAxis, elapsed, 0));
            }
            else if (stateId == WalkLoop)
            {
                output.SubmitMotion(new CharacterControlMotionRequest(Source(stateId), s_WalkLoopMotion, s_MoveAxis, elapsed, 0));
            }
            else if (stateId == RunLoop)
            {
                output.SubmitMotion(new CharacterControlMotionRequest(Source(stateId), s_RunLoopMotion, s_MoveAxis, elapsed, 0));
            }
            else if (stateId == MovingTurn)
            {
                output.SubmitMotion(new CharacterControlMotionRequest(Source(stateId), s_MovingTurnMotion, s_MoveAxis, elapsed, 0));
            }

            if (stateId != Idle)
                state.WriteInt32(s_MotionElapsed, checked(elapsed + 1));
            ulong completedDodgeForward = read.CompletedSkillInstanceId(DodgeForward);
            if (completedDodgeForward != 0 &&
                state.ReadUInt64(s_DodgeForwardCompletionInstance) != completedDodgeForward)
            {
                state.WriteUInt64(s_DodgeForwardCompletionInstance, completedDodgeForward);
                state.WriteBoolean(s_DirectionalDodgeRunIntent, true);
            }
            SubmitSkillRequests(stateId, read, state, output);
        }

        public bool EvaluateTransition(
            in CharacterControlTickContext context,
            CharacterControlTransitionId transitionId,
            ICharacterControlReadPort read,
            ICharacterControlStateReadPort state)
        {
            m_EvaluatedContext = context;
            m_EvaluatedRead = read;
            m_EvaluatedState = state;
            return m_TransitionsById[transitionId.Value].ShouldTransition();
        }

        public void Exit(
            in CharacterControlTickContext context,
            CharacterControlStateId stateId,
            ICharacterControlReadPort read,
            ICharacterControlStatePort state,
            ICharacterControlOutputPort output)
        {
        }

        void SubmitSkillRequests(
            CharacterControlStateId stateId,
            ICharacterControlReadPort read,
            ICharacterControlStatePort state,
            ICharacterControlOutputPort output)
        {
            SimulationExecutionSource source = Source(stateId);
            if (read.HasInputRequest(s_DodgeRequest))
            {
                CharacterSkillId skill = read.IsInputDirectionBehindBodyYaw(s_MoveAxis) ? DodgeBack : DodgeForward;
                ulong replacementActionInstanceId = 0;
                TryGetActiveAttackInstance(read, out replacementActionInstanceId);
                bool accepted = output.SubmitSkill(new CharacterControlSkillRequest(
                    source,
                    skill,
                    s_DodgeRequest,
                    true,
                    replacementActionInstanceId: replacementActionInstanceId));
                return;
            }
            if (read.HasInputRequest(s_AttackRequest))
            {
                output.SubmitSkill(new CharacterControlSkillRequest(
                    source,
                    Attack,
                    s_AttackRequest,
                    true,
                    s_ActionTarget));
            }
        }

        bool TryGetActiveAttackInstance(ICharacterControlReadPort read, out ulong instanceId) =>
            read.TryGetActiveSkillInstanceId(Attack, out instanceId);

        bool IsAttackActive(ICharacterControlReadPort read) => read.IsSkillActive(Attack);

        SimulationExecutionSource Source(CharacterControlStateId stateId) =>
            SimulationExecutionSource.FromCharacterControl(ModuleId, stateId, default);

        bool MoveAbove(ICharacterControlReadPort read) => read.CompareInputVector2Magnitude(s_MoveAxis, s_StopThreshold, CharacterControlNumericComparison.Greater);
        bool MoveBelow(ICharacterControlReadPort read) => read.CompareInputVector2Magnitude(s_MoveAxis, s_StopThreshold, CharacterControlNumericComparison.Less);
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
                    Transition("MovingTurnToWalkStopping", MovingTurn, WalkStopping, 0, 10),
                    Transition("WalkLoopToRunLoop", WalkLoop, RunLoop, 0, 11),
                    Transition("WalkStartToRunLoop", WalkStart, RunLoop, 100, 12),
                    Transition("RunLoopToMovingTurn", RunLoop, MovingTurn, 2, 13),
                    Transition("MovingTurnToWalkLoop", MovingTurn, WalkLoop, 100, 14)
                },
                new[]
                {
                    new CharacterControlStateFieldDescriptor(s_ActiveState, ProgramStateValueKind.Identity, ProgramStateSemantic.ControlActiveState),
                    new CharacterControlStateFieldDescriptor(s_EnteredTick, ProgramStateValueKind.UInt64, ProgramStateSemantic.ControlEnteredTick),
                    new CharacterControlStateFieldDescriptor(s_LastTransition, ProgramStateValueKind.Identity, ProgramStateSemantic.ControlTransition),
                    new CharacterControlStateFieldDescriptor(s_TransitionProgress, ProgramStateValueKind.Int32, ProgramStateSemantic.ControlTransitionProgress),
                    new CharacterControlStateFieldDescriptor(s_MotionElapsed, ProgramStateValueKind.Int32, ProgramStateSemantic.ControlState),
                    new CharacterControlStateFieldDescriptor(s_DirectionalDodgeRunIntent, ProgramStateValueKind.Boolean, ProgramStateSemantic.ControlState),
                    new CharacterControlStateFieldDescriptor(s_DodgeForwardCompletionInstance, ProgramStateValueKind.UInt64, ProgramStateSemantic.ControlState)
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
                        28d / 30d,
                        s_MovingTurnSourceMotion,
                        CharacterControlMotionDisplacementMode.SourceCurve,
                        CharacterControlMotionSpace.ActorLocal,
                        100,
                        true)
                },
                new[] { Attack, DodgeBack, DodgeForward });
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
        public static CharacterControlModuleCatalog Create() => new CharacterControlModuleCatalog(new[] { new CorinCharacterControlModule() });
    }
}
