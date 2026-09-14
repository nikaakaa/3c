using System;
using System.Collections.Generic;
using ThirdPersonPerformance.Instrumentation;

namespace ThirdPersonSimulation.Fixed
{
    internal sealed class FixedAbilityControlRuntime : IFixedAbilityControlRuntime
    {
        readonly FixedAbilityExecutionFrame m_Frame;
        readonly FixedActionStateStore m_ActionStore;
        readonly FixedInputRuntime m_Input;
        readonly FixedGameplayEffectOperationRuntime m_GameplayEffects;
        readonly FixedEquipmentRuntime m_Equipment;
        readonly FixedActionRuntime m_Actions;
        readonly FixedValueRuntime m_Values;
        readonly FixedBlackboardRuntime m_Blackboard;
        readonly FixedMotionAccumulator m_Motion;
        readonly FixedLocomotionRuntime m_Locomotion;
        readonly FixedAbilityExecutionTarget m_Target;
        OperationControlRuntime<FixedAbilityExecutionTarget> m_Runtime;

        public FixedAbilityControlRuntime(
            FixedGameplayAbilityExecutionInstallation installation,
            FixedGameplayAbilityExecutionInstallationSet installations,
            FixedAbilityExecutionFrame frame,
            FixedAbilityExecutionWorkspace workspace,
            CharacterControlRuntimeBinding controlRuntimeBinding)
        {
            if (installation == null)
                throw new ArgumentNullException(nameof(installation));
            if (installations == null)
                throw new ArgumentNullException(nameof(installations));
            m_Frame = frame ?? throw new ArgumentNullException(nameof(frame));
            if (workspace == null)
                throw new ArgumentNullException(nameof(workspace));

            FixedGameplayAbilityExecutionAccess access = installation.Access;
            FixedStatePort controlState = frame.CreateStatePort("AbilityControl", installation.Services.ControlPolicy);
            m_ActionStore = new FixedActionStateStore(access, frame);
            m_Input = new FixedInputRuntime(access, frame);
            var handles = new FixedHandleAllocator(access, frame);
            m_Blackboard = new FixedBlackboardRuntime(
                access,
                frame.CreateStatePort("AbilityBlackboard", installation.Services.BlackboardPolicy),
                frame,
                m_ActionStore,
                frame.Facts,
                frame.Trace,
                workspace);
            m_GameplayEffects = new FixedGameplayEffectOperationRuntime(
                access,
                frame,
                m_ActionStore,
                handles,
                frame.Facts,
                frame.Presentation,
                frame.Trace,
                workspace.GameplayEffects);
            m_Equipment = new FixedEquipmentRuntime(
                access,
                frame,
                m_ActionStore,
                handles,
                m_GameplayEffects,
                frame.Facts,
                frame.Trace);
            m_Actions = new FixedActionRuntime(
                access,
                installations,
                frame,
                m_Input,
                m_ActionStore,
                m_Blackboard,
                m_GameplayEffects,
                m_GameplayEffects,
                handles,
                frame.Facts,
                frame.Trace,
                m_Equipment,
                operation => m_Runtime == null || !m_Runtime.IsActive(operation) && !m_Runtime.IsStopping(operation));
            m_Values = new FixedValueRuntime(
                access,
                m_Input,
                m_ActionStore,
                m_Actions,
                m_GameplayEffects,
                m_Equipment,
                m_Blackboard,
                frame,
                workspace);
            m_Motion = new FixedMotionAccumulator(
                access,
                frame,
                workspace.MotionContributions,
                workspace.MotionWarpSamples,
                m_ActionStore);
            m_Locomotion = new FixedLocomotionRuntime(
                access,
                m_Values,
                m_Motion,
                frame,
                controlRuntimeBinding);
            m_Target = new FixedAbilityExecutionTarget(
                access,
                controlState,
                frame.CreateOperationStateReset(),
                m_Values,
                m_Blackboard,
                m_Actions,
                m_GameplayEffects,
                m_Equipment,
                m_Locomotion,
                frame.Facts,
                frame.Presentation,
                frame.Trace);
            m_Runtime = new OperationControlRuntime<FixedAbilityExecutionTarget>(
                installation.Topology,
                m_Target,
                checked(Math.Max(1024, installation.Data.Operations.Count * 128)));
        }

        internal FixedActionRuntime Actions => m_Actions;
        internal FixedActionStateStore ActionStore => m_ActionStore;
        internal FixedInputRuntime Input => m_Input;
        internal FixedGameplayEffectOperationRuntime GameplayEffects => m_GameplayEffects;
        internal FixedEquipmentRuntime Equipment => m_Equipment;
        internal FixedValueRuntime Values => m_Values;
        internal FixedBlackboardRuntime Blackboard => m_Blackboard;
        internal FixedMotionAccumulator Motion => m_Motion;
        internal OperationControlCursor<FixedAbilityExecutionTarget> Cursor => m_Runtime.Cursor;

        internal void BeginEvaluation(bool diagnosticsEnabled, bool captureValues, bool captureControlFlow)
        {
            m_Frame.Trace.Begin(diagnosticsEnabled, captureValues, captureControlFlow);
            m_ActionStore.BeginEvaluation();
            m_Values.BeginEvaluation();
            m_GameplayEffects.BeginEvaluation();
            m_Equipment.BeginEvaluation();
            m_Blackboard.BeginFrame();
            m_Runtime.BeginEvaluation();
        }

        internal void EndEvaluation()
        {
            m_Equipment.EndEvaluation();
            m_Blackboard.EndFrame();
            m_GameplayEffects.EndEvaluation();
            m_ActionStore.EndEvaluation();
        }

        internal void ApplyIngress()
        {
            for (int i = 0; i < m_Frame.Ingress.Count; i++)
            {
                SimulationIngress ingress = m_Frame.Ingress[i];
                if (ingress.Header.Kind == SimulationIngressKind.ActionLifecycle)
                    m_Actions.ApplyIngress(ingress);
                else
                    m_GameplayEffects.ApplyIngress(ingress);
            }
        }

        internal void ApplyInputRequests()
        {
            m_Input.ApplyRequests();
            m_Input.ApplyBlackboardInputBindings(m_Blackboard);
        }

        public OperationExecutionResult Tick(OperationHandle operation) => m_Runtime.Tick(operation);

        public bool IsActive(OperationHandle operation) => m_Runtime.IsActive(operation);
        public bool IsStopping(OperationHandle operation) => m_Runtime.IsStopping(operation);
        public OperationStopStatus RequestStop(OperationHandle operation, OperationStopContext context) =>
            m_Runtime.RequestStop(operation, context);
        public OperationStopStatus ContinueStop(OperationHandle operation) => m_Runtime.ContinueStop(operation);
        public void ForceStop(OperationHandle operation, OperationStopContext context) => m_Runtime.ForceStop(operation, context);
        public OperationRunnableStatus ReadStatus(OperationHandle operation) => m_Runtime.ReadStatus(operation);
        public ulong ReadGeneration(OperationHandle operation) => m_Runtime.ReadGeneration(operation);
    }

    internal readonly struct FixedAbilityExecutionTarget :
        IOperationControlTarget<FixedAbilityExecutionTarget>,
        IOperationControlEdgeTraceTarget
    {
        readonly FixedGameplayAbilityExecutionAccess m_Access;
        readonly FixedStatePort m_ControlState;
        readonly FixedOperationStateReset m_ResetState;
        readonly FixedValueRuntime m_Values;
        readonly FixedBlackboardRuntime m_Blackboard;
        readonly FixedActionRuntime m_Actions;
        readonly FixedGameplayEffectOperationRuntime m_GameplayEffects;
        readonly FixedEquipmentRuntime m_Equipment;
        readonly FixedLocomotionRuntime m_Locomotion;
        readonly FixedFactSink m_Facts;
        readonly FixedPresentationSink m_Presentation;
        readonly FixedTraceSink m_Trace;

        public FixedAbilityExecutionTarget(
            FixedGameplayAbilityExecutionAccess access,
            FixedStatePort controlState,
            FixedOperationStateReset resetState,
            FixedValueRuntime values,
            FixedBlackboardRuntime blackboard,
            FixedActionRuntime actions,
            FixedGameplayEffectOperationRuntime gameplayEffects,
            FixedEquipmentRuntime equipment,
            FixedLocomotionRuntime locomotion,
            FixedFactSink facts,
            FixedPresentationSink presentation,
            FixedTraceSink trace)
        {
            m_Access = access;
            m_ControlState = controlState;
            m_ResetState = resetState;
            m_Values = values;
            m_Blackboard = blackboard;
            m_Actions = actions;
            m_GameplayEffects = gameplayEffects;
            m_Equipment = equipment;
            m_Locomotion = locomotion;
            m_Facts = facts;
            m_Presentation = presentation;
            m_Trace = trace;
        }

        public bool DiagnosticsEnabled => m_Trace.Enabled;
        public bool ControlTraceEnabled => m_Trace.CaptureControlFlow;
        public int ReadInt32(int slotIndex) => m_ControlState.Get(slotIndex).Int32;
        public void WriteInt32(int slotIndex, int value) => m_ControlState.Set(slotIndex, CharacterStateValue.FromInt32(value));
        public ulong ReadUInt64(int slotIndex) => m_ControlState.Get(slotIndex).UInt64;
        public void WriteUInt64(int slotIndex, ulong value) => m_ControlState.Set(slotIndex, CharacterStateValue.FromUInt64(value));
        public string ReadIdentity(int slotIndex) => m_ControlState.Get(slotIndex).Identity;
        public void WriteIdentity(int slotIndex, string value) => m_ControlState.Set(slotIndex, CharacterStateValue.FromIdentity(value));
        public void TraceEdge(ProgramControlFlowEdge edge, bool selected, bool passed) =>
            m_Trace.AddControlFlow(edge, selected, passed);

        public bool EvaluateCondition(
            OperationControlCursor<FixedAbilityExecutionTarget> cursor,
            ProgramControlFlowEdge edge) => m_Values.EvaluateCondition(cursor, edge);

        public OperationExecutionResult ExecuteLeaf(
            OperationControlCursor<FixedAbilityExecutionTarget> cursor,
            OperationExecutionDescriptor descriptor)
        {
            SimulationOperation operation = m_Access.Operation(descriptor.Handle);
            switch (descriptor.Code)
            {
                case SimulationOperationCode.BlackboardSet:
                    return m_Values.SetBlackboard(cursor, operation)
                        ? OperationExecutionResult.Success
                        : OperationExecutionResult.Failure;
                case SimulationOperationCode.ActivateActionInstance:
                    return m_Actions.Activate(cursor, operation)
                        ? OperationExecutionResult.Success
                        : OperationExecutionResult.Failure;
                case SimulationOperationCode.SubmitActionLifecycle:
                    return m_Actions.SubmitLifecycle(operation)
                        ? OperationExecutionResult.Success
                        : OperationExecutionResult.Failure;
                case SimulationOperationCode.GameplayEffectApply:
                    return m_GameplayEffects.Apply(operation)
                        ? OperationExecutionResult.Success
                        : OperationExecutionResult.Failure;
                case SimulationOperationCode.GameplayEffectRemove:
                    return m_GameplayEffects.Remove(operation)
                        ? OperationExecutionResult.Success
                        : OperationExecutionResult.Failure;
                case SimulationOperationCode.RequestEquipmentChange:
                case SimulationOperationCode.BeginEquipmentChange:
                case SimulationOperationCode.CommitEquipmentChange:
                case SimulationOperationCode.CancelEquipmentChange:
                    using (FixedValueInputLease inputs = m_Values.ReadInputs(cursor, operation))
                        return m_Equipment.Execute(cursor, operation, inputs)
                            ? OperationExecutionResult.Success
                            : OperationExecutionResult.Failure;
                case SimulationOperationCode.LocomotionInputMotion:
                    return TickLocomotion(cursor, operation);
                case SimulationOperationCode.StateRootCompleted:
                case SimulationOperationCode.StateExitCause:
                case SimulationOperationCode.BlackboardGet:
                case SimulationOperationCode.InputBoolean:
                case SimulationOperationCode.InputScalar:
                case SimulationOperationCode.InputVector2:
                case SimulationOperationCode.InputVector2Magnitude:
                case SimulationOperationCode.InputRequest:
                case SimulationOperationCode.MoveFacingAngle:
                case SimulationOperationCode.CharacterStateRead:
                case SimulationOperationCode.ActionContextActive:
                case SimulationOperationCode.ActionWindowActive:
                case SimulationOperationCode.CanActivateAction:
                case SimulationOperationCode.GameplayEffectHasTag:
                case SimulationOperationCode.GameplayEffectMatchTags:
                case SimulationOperationCode.GameplayAttributeRead:
                case SimulationOperationCode.CameraBasisRead:
                case SimulationOperationCode.ConditionResult:
                case SimulationOperationCode.Compare:
                case SimulationOperationCode.And:
                case SimulationOperationCode.Or:
                case SimulationOperationCode.Not:
                case SimulationOperationCode.Constant:
                case SimulationOperationCode.ReadEquipmentIdentity:
                case SimulationOperationCode.ReadEquipmentParameter:
                    return FixedValueRuntime.ToBoolean(m_Values.Evaluate(cursor, operation.Handle))
                        ? OperationExecutionResult.Success
                        : OperationExecutionResult.Failure;
                case SimulationOperationCode.Timeline:
                    throw new InvalidOperationException(
                        $"Ability Timeline operation '{m_Access.SourcePath(operation)}' has no direct Timeline runtime binding.");
                case SimulationOperationCode.CameraStateRequest:
                case SimulationOperationCode.CameraCue:
                case SimulationOperationCode.CameraResponse:
                case SimulationOperationCode.CameraTarget:
                    throw new InvalidOperationException(
                        $"Ability operation '{m_Access.SourcePath(operation)}' cannot own a Camera presentation request.");
                case SimulationOperationCode.Root:
                case SimulationOperationCode.Loop:
                case SimulationOperationCode.Parallel:
                case SimulationOperationCode.Sequence:
                case SimulationOperationCode.Selector:
                case SimulationOperationCode.Succeed:
                case SimulationOperationCode.SubGraph:
                case SimulationOperationCode.StateMachine:
                case SimulationOperationCode.State:
                case SimulationOperationCode.StateOnEnter:
                case SimulationOperationCode.StateOnExit:
                case SimulationOperationCode.TimelineEnter:
                    throw new InvalidOperationException(
                        $"Portable control operation '{descriptor.Code}' reached the Ability leaf dispatcher.");
                case SimulationOperationCode.StateEnter:
                case SimulationOperationCode.StateAny:
                case SimulationOperationCode.StateExit:
                case SimulationOperationCode.TimelineAnimation:
                case SimulationOperationCode.TimelineMotionCurve:
                case SimulationOperationCode.TimelineTreeClip:
                case SimulationOperationCode.TimelineCue:
                case SimulationOperationCode.TimelineCameraState:
                case SimulationOperationCode.TimelineCameraCue:
                case SimulationOperationCode.TimelineCameraResponse:
                    throw new InvalidOperationException(
                        $"Timeline content operation '{descriptor.Code}' cannot execute as an Ability leaf.");
                default:
                    throw new InvalidOperationException(
                        $"Ability operation '{descriptor.Handle}' code '{descriptor.Code}' has no Fixed owner.");
            }
        }

        OperationExecutionResult TickLocomotion(
            OperationControlCursor<FixedAbilityExecutionTarget> cursor,
            SimulationOperation operation)
        {
            int slot = m_Access.RequireOperationSlot(operation.Handle, ProgramStateSemantic.LocomotionMotionElapsedTicks);
            int committedTicks = checked(m_ControlState.Get(slot).Int32 + 1);
            int generationSlot = m_Access.RequireOperationSlot(
                operation.Handle,
                ProgramStateSemantic.RunnableActivationGeneration);
            m_Locomotion.Submit(
                cursor,
                operation,
                committedTicks,
                m_ControlState.Get(generationSlot).UInt64);
            var mode = (LocomotionInputMotionExecutionMode)operation.Integer0;
            if (mode == LocomotionInputMotionExecutionMode.Once)
                return OperationExecutionResult.Success;
            m_ControlState.Set(slot, CharacterStateValue.FromInt32(committedTicks));
            if (mode == LocomotionInputMotionExecutionMode.Continuous)
                return OperationExecutionResult.Running;
            if (mode != LocomotionInputMotionExecutionMode.Timed)
                throw new InvalidOperationException(
                    $"Locomotion operation '{m_Access.SourcePath(operation)}' has invalid execution mode '{operation.Integer0}'.");
            ProgramConstant duration = m_Access.FindConstant(operation, OperationNamedConstant.DurationSeconds);
            if (duration == null || duration.Kind != ProgramConstantKind.Scalar)
                throw new InvalidOperationException(
                    $"Locomotion operation '{m_Access.SourcePath(operation)}' has no duration.");
            int requiredTicks = checked((int)Math.Ceiling(duration.Scalar.ToDouble() * m_Access.Data.TickRate));
            if (requiredTicks <= 0)
                throw new InvalidOperationException(
                    $"Locomotion operation '{m_Access.SourcePath(operation)}' has a non-positive timed duration.");
            return committedTicks >= requiredTicks
                ? OperationExecutionResult.Success
                : OperationExecutionResult.Running;
        }

        public void PrepareActivation(OperationExecutionDescriptor operation)
        {
        }

        public void PrepareSubGraph(
            OperationControlCursor<FixedAbilityExecutionTarget> cursor,
            OperationExecutionDescriptor operation) =>
            m_Values.PrepareSubGraph(cursor, m_Access.Operation(operation.Handle));

        public void ActivateScopes(
            OperationControlCursor<FixedAbilityExecutionTarget> cursor,
            OperationExecutionDescriptor operation,
            ulong generation)
        {
            m_Actions.BindCurrentSkillExecution(operation.Handle, generation);
            m_Blackboard.ActivateOperationScopes(cursor, m_Access.Operation(operation.Handle), generation);
        }

        public void CompleteScopes(OperationExecutionDescriptor operation) =>
            m_Blackboard.CompleteOperationScopes(m_Access.Operation(operation.Handle));

        public void ClearStateScope(OperationExecutionDescriptor state) =>
            m_Blackboard.ClearStateScopes(m_Access.Operation(state.Handle));

        public void ResetOperationState(OperationExecutionDescriptor operation) =>
            m_ResetState.Reset(m_Access.Operation(operation.Handle));

        public OperationStopStatus ContinueLeafStop(
            OperationControlCursor<FixedAbilityExecutionTarget> cursor,
            OperationExecutionDescriptor operation,
            OperationStopContext context) => throw new InvalidOperationException(
                $"Ability operation '{m_Access.SourcePath(m_Access.Operation(operation.Handle))}' has no direct Timeline stop owner.");

        public void ForceStopLeaf(
            OperationControlCursor<FixedAbilityExecutionTarget> cursor,
            OperationExecutionDescriptor operation,
            OperationStopContext context) => throw new InvalidOperationException(
                $"Ability operation '{m_Access.SourcePath(m_Access.Operation(operation.Handle))}' has no direct Timeline stop owner.");

        public void EmitTrace(
            OperationExecutionDescriptor operation,
            string code,
            OperationControlTraceSeverity severity,
            string detail) =>
            m_Trace.Add(
                m_Access.Operation(operation.Handle),
                code,
                severity == OperationControlTraceSeverity.Error
                    ? SimulationTraceSeverity.Error
                    : SimulationTraceSeverity.Detail,
                detail);

        public void NotifyStateLifecycle(
            OperationExecutionDescriptor machine,
            OperationHandle state,
            OperationStateLifecyclePhase phase)
        {
            SimulationOperation operation = m_Access.Operation(machine.Handle);
            SimulationEventHeader header = m_Facts.Next(operation);
            m_Facts.Add(new GameplayFact(
                header,
                GameplayFactKind.State,
                $"state:{state.Value}",
                phase.ToString(),
                FixedScalar.Zero));
        }

        public void NotifyStateTransition(
            OperationExecutionDescriptor machine,
            OperationHandle exitingState,
            OperationHandle targetState)
        {
            m_Actions.AdvanceSegmentGeneration();
            foreach (ActionAdmissionActiveAction action in ((IActionAdmissionReadPort)m_Actions).ActiveActions)
            {
                SimulationOperation operation = m_Access.Operation(machine.Handle);
                SimulationEventHeader header = m_Presentation.Next(operation);
                m_Presentation.Add(new PresentationCommand(
                    header,
                    PresentationCommandKind.DomainEvent,
                    "domain/action-segment-changed",
                    FixedScalar.Zero,
                    FixedScalar.Zero,
                    sourceActionInstanceId: action.InstanceId,
                    domainPayload: $"prev:{exitingState.Value};next:{targetState.Value}"));
            }
        }
    }
}
