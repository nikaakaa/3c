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

        // UnityHFSM 是控制层状态机的执行引擎:Contract 构造时已按 Priority/Order/Id 排序,
        // 机器按该序登记转移,OnLogic 内"体先行、按登记序首个真条件即转移"实现优先级抢占语义。
        // 状态真值仍在仿真槽位,模块由目录工厂按 evaluator(即按 actor)新建,机器可变状态不跨 actor 共享。
        readonly StateMachine<string, string> m_Machine;
        bool m_Initialized;

        CharacterControlTickContext m_Context;
        ICharacterControlReadPort m_Read;
        ICharacterControlStatePort m_State;
        ICharacterControlOutputPort m_Output;

        public CharacterControlModuleContract Contract => s_Contract;

        public CorinCharacterControlModule()
        {
            m_Machine = new StateMachine<string, string>();
            m_Machine.SetStartState(Idle.Value);
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
                m_Initialized = true;
                m_Machine.Init();
            }
            m_Machine.OnLogic();
        }

        void OnStateEnter(CharacterControlStateId stateId)
        {
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
                m_Output.SubmitMotion(new CharacterControlMotionRequest(Source(stateId), s_MovingTurnMotion, s_MoveAxis, elapsed, 0, playbackGeneration));
            }

            if (stateId != Idle)
                m_State.WriteInt32(s_MotionElapsed, checked(elapsed + 1));

            ulong completedDodgeForward = m_Read.CompletedSkillInstanceId(DodgeForward);
            if (completedDodgeForward != 0 &&
                m_State.ReadUInt64(s_DodgeForwardCompletionInstance) != completedDodgeForward)
            {
                m_State.WriteUInt64(s_DodgeForwardCompletionInstance, completedDodgeForward);
                m_State.WriteBoolean(s_DirectionalDodgeRunIntent, true);
            }
            SubmitSkillRequests(stateId);
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
                "WalkStartToWalkLoop" => MotionElapsed(m_State) >= Ticks(m_Context, 1.1d) && MoveAbove(m_Read),
                "WalkLoopToWalkStopping" => MoveBelow(m_Read),
                "WalkStoppingToIdle" => MotionElapsed(m_State) > 0 && MoveBelow(m_Read),
                "RunLoopToRunStopping" => MoveBelow(m_Read),
                "RunStoppingToIdle" => MotionElapsed(m_State) > 0 && MoveBelow(m_Read),
                "MovingTurnToRunLoop" => MotionElapsed(m_State) >= Ticks(m_Context, 28d / 30d) && m_State.ReadBoolean(s_DirectionalDodgeRunIntent) && MoveAbove(m_Read),
                "WalkStartToWalkStopping" => MoveBelow(m_Read),
                "WalkStoppingToWalkStart" => MoveAbove(m_Read),
                "RunStoppingToRunLoop" => MoveAbove(m_Read),
                "MovingTurnToWalkStopping" => MotionElapsed(m_State) >= Ticks(m_Context, 28d / 30d) && MoveBelow(m_Read),
                "WalkLoopToRunLoop" => m_State.ReadBoolean(s_DirectionalDodgeRunIntent),
                "WalkStartToRunLoop" => m_State.ReadBoolean(s_DirectionalDodgeRunIntent),
                "RunLoopToMovingTurn" => MoveAbove(m_Read) &&
                    m_Read.CompareInputDirectionToBodyYaw(s_MoveAxis, s_MovingTurnAngleThreshold, CharacterControlNumericComparison.GreaterOrEqual) &&
                    !IsAttackActive(m_Read) && !m_Read.IsSkillActive(DodgeBack) && !m_Read.IsSkillActive(DodgeForward),
                "MovingTurnToWalkLoop" => MotionElapsed(m_State) >= Ticks(m_Context, 28d / 30d) && !m_State.ReadBoolean(s_DirectionalDodgeRunIntent) && MoveAbove(m_Read),
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

        void SubmitSkillRequests(CharacterControlStateId stateId)
        {
            SimulationExecutionSource source = Source(stateId);
            if (m_Read.HasInputRequest(s_DodgeRequest))
            {
                CharacterSkillId skill = m_Read.IsInputDirectionBehindBodyYaw(s_MoveAxis) ? DodgeBack : DodgeForward;
                ulong replacementActionInstanceId = 0;
                TryGetActiveAttackInstance(m_Read, out replacementActionInstanceId);
                m_Output.SubmitSkill(new CharacterControlSkillRequest(
                    source,
                    skill,
                    s_DodgeRequest,
                    true,
                    replacementActionInstanceId: replacementActionInstanceId));
                return;
            }
            if (m_Read.HasInputRequest(s_AttackRequest))
            {
                m_Output.SubmitSkill(new CharacterControlSkillRequest(
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
                    Transition("MovingTurnToWalkStopping", MovingTurn, WalkStopping, 0, 10),
                    Transition("WalkLoopToRunLoop", WalkLoop, RunLoop, 0, 11),
                    Transition("WalkStartToRunLoop", WalkStart, RunLoop, 100, 12),
                    Transition("RunLoopToMovingTurn", RunLoop, MovingTurn, 2, 13),
                    Transition("MovingTurnToWalkLoop", MovingTurn, WalkLoop, 100, 14)
                },
                new[]
                {
                    new CharacterControlStateFieldDescriptor(s_ActiveState, CharacterControlStateValueKind.Identity, CharacterControlStateSemantic.ActiveState),
                    new CharacterControlStateFieldDescriptor(s_EnteredTick, CharacterControlStateValueKind.UInt64, CharacterControlStateSemantic.EnteredTick),
                    new CharacterControlStateFieldDescriptor(s_LastTransition, CharacterControlStateValueKind.Identity, CharacterControlStateSemantic.Transition),
                    new CharacterControlStateFieldDescriptor(s_MotionElapsed, CharacterControlStateValueKind.Int32, CharacterControlStateSemantic.StateValue),
                    new CharacterControlStateFieldDescriptor(s_DirectionalDodgeRunIntent, CharacterControlStateValueKind.Boolean, CharacterControlStateSemantic.StateValue),
                    new CharacterControlStateFieldDescriptor(s_DodgeForwardCompletionInstance, CharacterControlStateValueKind.UInt64, CharacterControlStateSemantic.StateValue)
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
        public static CharacterControlModuleCatalog Create() =>
            new CharacterControlModuleCatalog(new Func<ICharacterControlModule>[] { () => new CorinCharacterControlModule() });
    }
}
