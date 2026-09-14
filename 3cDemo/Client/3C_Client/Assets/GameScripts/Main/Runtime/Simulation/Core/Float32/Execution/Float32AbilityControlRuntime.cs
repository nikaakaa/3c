using System;
using System.Collections.Generic;
using ThirdPersonPerformance.Instrumentation;

namespace ThirdPersonSimulation
{
    internal sealed class Float32AbilityControlRuntime : IFloat32AbilityControlRuntime
    {
        readonly Float32AbilityExecutionFrame m_Frame;
        readonly Float32ActionStateStore m_ActionStore;
        readonly Float32InputRuntime m_Input;
        readonly Float32GameplayEffectOperationRuntime m_GameplayEffects;
        readonly Float32EquipmentRuntime m_Equipment;
        readonly Float32ActionRuntime m_Actions;
        readonly Float32ValueRuntime m_Values;
        readonly Float32BlackboardRuntime m_Blackboard;
        readonly Float32MotionAccumulator m_Motion;
        readonly Float32LocomotionRuntime m_Locomotion;
        readonly Float32AbilityExecutionTarget m_Target;
        OperationControlRuntime<Float32AbilityExecutionTarget> m_Runtime;

        public Float32AbilityControlRuntime(
            Float32GameplayAbilityExecutionInstallation installation,
            Float32GameplayAbilityExecutionInstallationSet installations,
            Float32AbilityExecutionFrame frame,
            Float32AbilityExecutionWorkspace workspace,
            CharacterControlRuntimeBinding controlRuntimeBinding)
        {
            if (installation == null)
                throw new ArgumentNullException(nameof(installation));
            if (installations == null)
                throw new ArgumentNullException(nameof(installations));
            m_Frame = frame ?? throw new ArgumentNullException(nameof(frame));
            if (workspace == null)
                throw new ArgumentNullException(nameof(workspace));

            Float32GameplayAbilityExecutionAccess access = installation.Access;
            Float32StatePort controlState = frame.CreateStatePort("AbilityControl", installation.Services.ControlPolicy);
            m_ActionStore = new Float32ActionStateStore(access, frame);
            m_Input = new Float32InputRuntime(access, frame);
            var handles = new Float32HandleAllocator(access, frame);
            m_Blackboard = new Float32BlackboardRuntime(
                access,
                frame.CreateStatePort("AbilityBlackboard", installation.Services.BlackboardPolicy),
                frame,
                m_ActionStore,
                frame.Facts,
                frame.Trace,
                workspace);
            m_GameplayEffects = new Float32GameplayEffectOperationRuntime(
                access,
                frame,
                m_ActionStore,
                handles,
                frame.Facts,
                frame.Presentation,
                frame.Trace,
                workspace.GameplayEffects);
            m_Equipment = new Float32EquipmentRuntime(
                access,
                frame,
                m_ActionStore,
                handles,
                m_GameplayEffects,
                frame.Facts,
                frame.Trace);
            m_Actions = new Float32ActionRuntime(
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
            m_Values = new Float32ValueRuntime(
                access,
                m_Input,
                m_ActionStore,
                m_Actions,
                m_GameplayEffects,
                m_Equipment,
                m_Blackboard,
                frame,
                workspace);
            m_Motion = new Float32MotionAccumulator(
                access,
                frame,
                workspace.MotionContributions,
                workspace.MotionWarpSamples,
                m_ActionStore);
            m_Locomotion = new Float32LocomotionRuntime(
                access,
                m_Values,
                m_Motion,
                frame,
                controlRuntimeBinding);
            m_Target = new Float32AbilityExecutionTarget(
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
            m_Runtime = new OperationControlRuntime<Float32AbilityExecutionTarget>(
                installation.Topology,
                m_Target,
                checked(Math.Max(1024, installation.Data.Operations.Count * 128)));
        }

        internal Float32ActionRuntime Actions => m_Actions;
        internal Float32ActionStateStore ActionStore => m_ActionStore;
        internal Float32InputRuntime Input => m_Input;
        internal Float32GameplayEffectOperationRuntime GameplayEffects => m_GameplayEffects;
        internal Float32EquipmentRuntime Equipment => m_Equipment;
        internal Float32ValueRuntime Values => m_Values;
        internal Float32BlackboardRuntime Blackboard => m_Blackboard;
        internal Float32MotionAccumulator Motion => m_Motion;
        internal OperationControlCursor<Float32AbilityExecutionTarget> Cursor => m_Runtime.Cursor;

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

    internal readonly struct Float32AbilityExecutionTarget :
        IOperationControlTarget<Float32AbilityExecutionTarget>,
        IOperationControlEdgeTraceTarget
    {
        readonly Float32GameplayAbilityExecutionAccess m_Access;
        readonly Float32StatePort m_ControlState;
        readonly Float32OperationStateReset m_ResetState;
        readonly Float32ValueRuntime m_Values;
        readonly Float32BlackboardRuntime m_Blackboard;
        readonly Float32ActionRuntime m_Actions;
        readonly Float32GameplayEffectOperationRuntime m_GameplayEffects;
        readonly Float32EquipmentRuntime m_Equipment;
        readonly Float32LocomotionRuntime m_Locomotion;
        readonly Float32FactSink m_Facts;
        readonly Float32PresentationSink m_Presentation;
        readonly Float32TraceSink m_Trace;

        public Float32AbilityExecutionTarget(
            Float32GameplayAbilityExecutionAccess access,
            Float32StatePort controlState,
            Float32OperationStateReset resetState,
            Float32ValueRuntime values,
            Float32BlackboardRuntime blackboard,
            Float32ActionRuntime actions,
            Float32GameplayEffectOperationRuntime gameplayEffects,
            Float32EquipmentRuntime equipment,
            Float32LocomotionRuntime locomotion,
            Float32FactSink facts,
            Float32PresentationSink presentation,
            Float32TraceSink trace)
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
            OperationControlCursor<Float32AbilityExecutionTarget> cursor,
            ProgramControlFlowEdge edge) => m_Values.EvaluateCondition(cursor, edge);

        public OperationExecutionResult ExecuteLeaf(
            OperationControlCursor<Float32AbilityExecutionTarget> cursor,
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
                    using (Float32ValueInputLease inputs = m_Values.ReadInputs(cursor, operation))
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
                    return Float32ValueRuntime.ToBoolean(m_Values.Evaluate(cursor, operation.Handle))
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
                        $"Ability operation '{descriptor.Handle}' code '{descriptor.Code}' has no Float32 owner.");
            }
        }

        OperationExecutionResult TickLocomotion(
            OperationControlCursor<Float32AbilityExecutionTarget> cursor,
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
            OperationControlCursor<Float32AbilityExecutionTarget> cursor,
            OperationExecutionDescriptor operation) =>
            m_Values.PrepareSubGraph(cursor, m_Access.Operation(operation.Handle));

        public void ActivateScopes(
            OperationControlCursor<Float32AbilityExecutionTarget> cursor,
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
            OperationControlCursor<Float32AbilityExecutionTarget> cursor,
            OperationExecutionDescriptor operation,
            OperationStopContext context) => throw new InvalidOperationException(
                $"Ability operation '{m_Access.SourcePath(m_Access.Operation(operation.Handle))}' has no direct Timeline stop owner.");

        public void ForceStopLeaf(
            OperationControlCursor<Float32AbilityExecutionTarget> cursor,
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
                Float32Scalar.Zero));
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
                    Float32Scalar.Zero,
                    Float32Scalar.Zero,
                    sourceActionInstanceId: action.InstanceId,
                    domainPayload: $"prev:{exitingState.Value};next:{targetState.Value}"));
            }
        }
    }
}
