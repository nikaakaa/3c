using System;
using ThirdPersonSimulation;

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
        public static readonly CharacterSkillId Attack1 = new CharacterSkillId("Attack1");
        public static readonly CharacterSkillId Attack2 = new CharacterSkillId("Attack2");
        public static readonly CharacterSkillId Attack3 = new CharacterSkillId("Attack3");
        public static readonly CharacterSkillId Attack4 = new CharacterSkillId("Attack4");
        public static readonly CharacterSkillId Attack5 = new CharacterSkillId("Attack5");
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

        public CharacterControlModuleContract Contract => s_Contract;

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
            int elapsed = state.ReadInt32(s_MotionElapsed);
            switch (transitionId.Value)
            {
                case "IdleToWalkStart": return MoveAbove(read);
                case "WalkStartToWalkLoop": return elapsed >= Ticks(context, 1.1d) && MoveAbove(read);
                case "WalkLoopToWalkStopping": return MoveBelow(read);
                case "WalkStoppingToIdle": return elapsed > 0 && MoveBelow(read);
                case "RunLoopToRunStopping": return MoveBelow(read);
                case "RunStoppingToIdle": return elapsed > 0 && MoveBelow(read);
                case "MovingTurnToRunLoop":
                    return elapsed >= Ticks(context, 28d / 30d) && state.ReadBoolean(s_DirectionalDodgeRunIntent) && MoveAbove(read);
                case "WalkStartToWalkStopping": return MoveBelow(read);
                case "WalkStoppingToWalkStart": return MoveAbove(read);
                case "RunStoppingToRunLoop": return MoveAbove(read);
                case "MovingTurnToWalkStopping": return elapsed >= Ticks(context, 28d / 30d) && MoveBelow(read);
                case "WalkLoopToRunLoop": return state.ReadBoolean(s_DirectionalDodgeRunIntent);
                case "WalkStartToRunLoop": return state.ReadBoolean(s_DirectionalDodgeRunIntent);
                case "RunLoopToMovingTurn":
                    return MoveAbove(read) &&
                           read.CompareInputDirectionToBodyYaw(s_MoveAxis, s_MovingTurnAngleThreshold, CharacterControlNumericComparison.GreaterOrEqual) &&
                           !IsAttackActive(read) && !read.IsSkillActive(DodgeBack) && !read.IsSkillActive(DodgeForward);
                case "MovingTurnToWalkLoop":
                    return elapsed >= Ticks(context, 28d / 30d) && !state.ReadBoolean(s_DirectionalDodgeRunIntent) && MoveAbove(read);
                default: throw new InvalidOperationException($"Corin control transition '{transitionId}' is not implemented.");
            }
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
                bool accepted = output.SubmitSkill(new CharacterControlSkillRequest(source, skill, s_DodgeRequest, true));
                return;
            }
            if (read.HasInputRequest(s_AttackRequest))
            {
                CharacterSkillId skill = SelectAttackSkill(read);
                if (skill.IsValid)
                    output.SubmitSkill(new CharacterControlSkillRequest(source, skill, s_AttackRequest, true, s_ActionTarget));
            }
        }

        CharacterSkillId SelectAttackSkill(ICharacterControlReadPort read)
        {
            if (read.IsSkillActive(Attack1))
                return read.IsActionWindowActive(Attack1, "ComboAccept") ? Attack2 : default;
            if (read.IsSkillActive(Attack2))
                return read.IsActionWindowActive(Attack2, "ComboAccept") ? Attack3 : default;
            if (read.IsSkillActive(Attack3))
                return read.IsActionWindowActive(Attack3, "ComboAccept") ? Attack4 : default;
            if (read.IsSkillActive(Attack4))
                return read.IsActionWindowActive(Attack4, "ComboAccept") ? Attack5 : default;
            if (read.IsSkillActive(Attack5))
                return default;
            return Attack1;
        }

        bool IsAttackActive(ICharacterControlReadPort read) =>
            read.IsSkillActive(Attack1) ||
            read.IsSkillActive(Attack2) ||
            read.IsSkillActive(Attack3) ||
            read.IsSkillActive(Attack4) ||
            read.IsSkillActive(Attack5);

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
                    new CharacterControlMotionDescriptor(s_WalkStartMotion, s_MoveAxis, 4.592d, 720d, CharacterControlMotionExecutionMode.Timed, 1.1d),
                    new CharacterControlMotionDescriptor(s_WalkLoopMotion, s_MoveAxis, 6d, 720d, CharacterControlMotionExecutionMode.Continuous, 0d),
                    new CharacterControlMotionDescriptor(s_RunLoopMotion, s_MoveAxis, 7.36d, 720d, CharacterControlMotionExecutionMode.Continuous, 0d),
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
                new[] { Attack1, Attack2, Attack3, Attack4, Attack5, DodgeBack, DodgeForward });
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
