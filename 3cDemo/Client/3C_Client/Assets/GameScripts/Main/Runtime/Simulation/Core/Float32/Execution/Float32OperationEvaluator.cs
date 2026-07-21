using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation
{
    internal sealed class Float32CameraOperationRuntime : Float32OperationModule
    {
        readonly Float32PresentationSink m_Presentation;

        public Float32CameraOperationRuntime(Float32ProgramAccess access, Float32PresentationSink presentation)
            : base(access)
        {
            m_Presentation = presentation ?? throw new ArgumentNullException(nameof(presentation));
        }

        public void Submit(SimulationOperation operation)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));
            if (!CameraProgramOperationSchema.IsCameraPresentationOperation(operation.Code))
                throw new InvalidOperationException($"Operation '{operation.Handle}' is not a Camera operation.");
            if (operation.Integer0 != CameraProgramOperationSchema.PayloadVersion)
                throw new InvalidOperationException(
                    $"Camera operation '{SourcePath(operation)}' payload version '{operation.Integer0}' is unsupported.");

            ProgramProducer producer = RequireProducer(operation);
            Float32Scalar weight = operation.Code switch
            {
                SimulationOperationCode.CameraStateRequest => RequireScalar(operation, OperationNamedConstant.Weight),
                SimulationOperationCode.CameraCue => RequireScalar(operation, OperationNamedConstant.Intensity),
                SimulationOperationCode.CameraResponse => RequireScalar(operation, OperationNamedConstant.Weight),
                SimulationOperationCode.CameraTarget => RequireScalar(operation, OperationNamedConstant.Weight),
                _ => throw new InvalidOperationException($"Camera operation '{operation.Code}' is unsupported.")
            };
            m_Presentation.Add(new PresentationCommand(
                m_Presentation.Next(operation),
                PresentationCommandKind.Camera,
                producer.Identity,
                Float32Scalar.Zero,
                weight));
        }

        Float32Scalar RequireScalar(SimulationOperation operation, OperationNamedConstant field)
        {
            ProgramConstant constant = FindConstant(operation, field);
            if (constant == null || constant.Kind != ProgramConstantKind.Scalar)
                throw new InvalidOperationException(
                    $"Camera operation '{SourcePath(operation)}' requires Scalar field '{field}'.");
            return constant.Scalar;
        }

        ProgramProducer RequireProducer(SimulationOperation operation)
        {
            ProgramProducer producer = null;
            IReadOnlyList<ProgramReference> references = References(operation.Handle, ProgramReferenceKind.Producer);
            for (int i = 0; i < references.Count; i++)
            {
                if (producer != null)
                    throw new InvalidOperationException(
                        $"Camera operation '{SourcePath(operation)}' has multiple producer references.");
                producer = m_Program.Producers[references[i].TargetIndex];
            }
            if (producer == null ||
                producer.AnimationChannelId != CameraProgramOperationSchema.ChannelId ||
                producer.ChannelKind != ProgramOutputChannelKind.Presentation)
            {
                throw new InvalidOperationException(
                    $"Camera operation '{SourcePath(operation)}' has no valid Camera presentation producer.");
            }
            return producer;
        }
    }

    internal readonly struct Float32OperationTarget : IOperationControlTarget<Float32OperationTarget>
    {
        readonly Float32ProgramAccess m_Access;
        readonly Float32ControlStateAccess m_ControlState;
        readonly Float32OperationStateReset m_ResetState;
        readonly Float32ValueRuntime m_Values;
        readonly Float32BlackboardRuntime m_Blackboard;
        readonly Float32ActionRuntime m_Actions;
        readonly Float32GameplayEffectOperationRuntime m_GameplayEffects;
        readonly Float32EquipmentRuntime m_Equipment;
        readonly Float32CameraOperationRuntime m_Camera;
        readonly TimelineControlRuntime<Float32OperationTarget, Float32Scalar> m_Timeline;
        readonly Float32LocomotionRuntime m_Locomotion;
        readonly Float32FactSink m_Facts;
        readonly Float32TraceSink m_Trace;

        public Float32OperationTarget(
            Float32ProgramAccess access,
            Float32ControlStateAccess controlState,
            Float32OperationStateReset resetState,
            Float32ValueRuntime values,
            Float32BlackboardRuntime blackboard,
            Float32ActionRuntime actions,
            Float32GameplayEffectOperationRuntime gameplayEffects,
            Float32EquipmentRuntime equipment,
            Float32CameraOperationRuntime camera,
            TimelineControlRuntime<Float32OperationTarget, Float32Scalar> timeline,
            Float32LocomotionRuntime locomotion,
            Float32FactSink facts,
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
            m_Camera = camera;
            m_Timeline = timeline;
            m_Locomotion = locomotion;
            m_Facts = facts;
            m_Trace = trace;
        }

        public int ReadInt32(int slotIndex) => m_ControlState.ReadInt32(slotIndex);
        public bool DiagnosticsEnabled => m_Trace.Enabled;
        public void WriteInt32(int slotIndex, int value) => m_ControlState.WriteInt32(slotIndex, value);
        public ulong ReadUInt64(int slotIndex) => m_ControlState.ReadUInt64(slotIndex);
        public void WriteUInt64(int slotIndex, ulong value) => m_ControlState.WriteUInt64(slotIndex, value);
        public string ReadIdentity(int slotIndex) => m_ControlState.ReadIdentity(slotIndex);
        public void WriteIdentity(int slotIndex, string value) => m_ControlState.WriteIdentity(slotIndex, value);

        public bool EvaluateCondition(
            OperationControlCursor<Float32OperationTarget> cursor,
            ProgramControlFlowEdge edge)
        {
            return m_Values.EvaluateCondition(cursor, edge);
        }

		public OperationExecutionResult ExecuteLeaf(
			OperationControlCursor<Float32OperationTarget> cursor,
			OperationExecutionDescriptor descriptor)
		{
			SimulationOperation operation = m_Access.Operation(descriptor.Handle);
			switch (descriptor.Code)
			{
				case SimulationOperationCode.Timeline:
					return m_Timeline.TickTimeline(cursor, operation.Handle);
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
				case SimulationOperationCode.EnterEquipmentFeatureHost:
				case SimulationOperationCode.ExitEquipmentFeatureHost:
				case SimulationOperationCode.ResolveEquipmentActionRoute:
					using (Float32ValueInputLease equipmentInputs = m_Values.ReadInputs(cursor, operation))
						return m_Equipment.TickHost(cursor, operation, equipmentInputs);
				case SimulationOperationCode.LocomotionInputMotion:
					m_Locomotion.Submit(cursor, operation);
					return (operation.Flags & 2U) != 0
						? OperationExecutionResult.Running
						: OperationExecutionResult.Success;
				case SimulationOperationCode.CameraStateRequest:
				case SimulationOperationCode.CameraCue:
				case SimulationOperationCode.CameraResponse:
				case SimulationOperationCode.CameraTarget:
					m_Camera.Submit(operation);
					return OperationExecutionResult.Success;
				case SimulationOperationCode.StateRootCompleted:
				case SimulationOperationCode.StateExitCause:
				case SimulationOperationCode.BlackboardGet:
				case SimulationOperationCode.InputBoolean:
				case SimulationOperationCode.InputScalar:
				case SimulationOperationCode.InputVector2:
				case SimulationOperationCode.InputVector2Magnitude:
				case SimulationOperationCode.InputRequest:
				case SimulationOperationCode.MoveFacingAngle:
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
				case SimulationOperationCode.Root:
				case SimulationOperationCode.Loop:
				case SimulationOperationCode.Parallel:
				case SimulationOperationCode.Sequence:
				case SimulationOperationCode.Selector:
				case SimulationOperationCode.Succeed:
				case SimulationOperationCode.StateMachine:
				case SimulationOperationCode.State:
				case SimulationOperationCode.StateOnEnter:
				case SimulationOperationCode.StateOnExit:
				case SimulationOperationCode.TimelineEnter:
					throw new InvalidOperationException(
						$"Portable control operation '{descriptor.Code}' reached the Float32 leaf dispatcher.");
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
						$"Descriptor operation '{descriptor.Code}' cannot execute as a Runnable leaf.");
				default:
					throw new InvalidOperationException(
						$"Operation '{descriptor.Handle}' code '{descriptor.Code}' has no Float32 owner.");
			}
		}

        public void PrepareActivation(OperationExecutionDescriptor operation)
        {
        }

        public void ActivateScopes(
            OperationControlCursor<Float32OperationTarget> cursor,
            OperationExecutionDescriptor descriptor,
            ulong generation)
        {
            m_Blackboard.ActivateOperationScopes(cursor, m_Access.Operation(descriptor.Handle), generation);
        }

        public void CompleteScopes(OperationExecutionDescriptor descriptor)
        {
            m_Blackboard.CompleteOperationScopes(m_Access.Operation(descriptor.Handle));
        }

        public void ClearStateScope(OperationExecutionDescriptor descriptor)
        {
            m_Blackboard.ClearStateScopes(m_Access.Operation(descriptor.Handle));
        }

        public void ResetOperationState(OperationExecutionDescriptor descriptor)
        {
            m_ResetState.Reset(m_Access.Operation(descriptor.Handle));
        }

        public OperationStopStatus ContinueLeafStop(
            OperationControlCursor<Float32OperationTarget> cursor,
            OperationExecutionDescriptor descriptor,
            OperationStopContext context)
        {
            if (descriptor.Code == SimulationOperationCode.EnterEquipmentFeatureHost ||
                descriptor.Code == SimulationOperationCode.ResolveEquipmentActionRoute)
            {
                m_Equipment.ForceStopHost(cursor, m_Access.Operation(descriptor.Handle), context);
                return OperationStopStatus.Completed;
            }
            if (descriptor.Code != SimulationOperationCode.Timeline)
                throw new InvalidOperationException($"Leaf '{descriptor.Code}' does not own a graceful stop lifecycle.");
            return m_Timeline.ContinueTimelineStop(cursor, descriptor.Handle, context);
        }

        public void ForceStopLeaf(
            OperationControlCursor<Float32OperationTarget> cursor,
            OperationExecutionDescriptor descriptor,
            OperationStopContext context)
        {
            if (descriptor.Code == SimulationOperationCode.EnterEquipmentFeatureHost ||
                descriptor.Code == SimulationOperationCode.ResolveEquipmentActionRoute)
            {
                m_Equipment.ForceStopHost(cursor, m_Access.Operation(descriptor.Handle), context);
                return;
            }
            if (descriptor.Code != SimulationOperationCode.Timeline)
                throw new InvalidOperationException($"Leaf '{descriptor.Code}' does not own a force-stop lifecycle.");
            m_Timeline.ForceStopTimeline(cursor, descriptor.Handle, context);
        }

        public void EmitTrace(
            OperationExecutionDescriptor descriptor,
            string code,
            OperationControlTraceSeverity severity,
            string detail)
        {
            m_Trace.Add(
                m_Access.Operation(descriptor.Handle),
                code,
                severity == OperationControlTraceSeverity.Error
                    ? SimulationTraceSeverity.Error
                    : SimulationTraceSeverity.Detail,
                detail);
        }

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
    }

    internal sealed class Float32OperationEvaluator
    {
        readonly Float32EvaluationFrame m_Frame;
        readonly Float32BlackboardRuntime m_Blackboard;
        readonly Float32ActionRuntime m_Actions;
        readonly Float32GameplayEffectOperationRuntime m_GameplayEffects;
        readonly Float32EquipmentRuntime m_Equipment;
        readonly Float32InputRuntime m_Input;
        readonly Float32ValueRuntime m_Values;
        readonly TimelineControlRuntime<Float32OperationTarget, Float32Scalar> m_Timeline;
        readonly Float32MotionAccumulator m_Motion;
        readonly OperationControlRuntime<Float32OperationTarget> m_Control;

        public Float32OperationEvaluator(
            CharacterSimulationProgram program,
            ProgramExecutionLayout layout,
            ActorId actorId,
            Float32EvaluationWorkspace workspace)
        {
            m_Frame = new Float32EvaluationFrame(program, layout, actorId, workspace);
            Float32ProgramExecutionServices services = m_Frame.Services;
            Float32ProgramAccess access = services.Access;
            var controlState = new Float32ControlStateAccess(
                access,
                m_Frame.CreateStatePort("Control", services.ControlPolicy));
            var actionStore = new Float32ActionStateStore(
                access,
                m_Frame.CreateStatePort("Action", services.ActionPolicy));
            m_Input = new Float32InputRuntime(
                access,
                m_Frame.CreateStatePort("Input", services.InputPolicy),
                m_Frame);
            var handles = new Float32HandleAllocator(
                access,
                m_Frame.CreateStatePort("HandleAllocator", services.HandleAllocatorPolicy));
            m_Blackboard = new Float32BlackboardRuntime(
                access,
                m_Frame.CreateStatePort("Blackboard", services.BlackboardPolicy),
                m_Frame,
                actionStore,
                m_Frame.Facts,
                m_Frame.Trace,
                workspace);
            m_GameplayEffects = new Float32GameplayEffectOperationRuntime(
                access,
                m_Frame,
                actionStore,
                handles,
                m_Frame.Facts,
                m_Frame.Presentation,
                m_Frame.Trace,
                workspace.GameplayEffects);
            m_Equipment = new Float32EquipmentRuntime(
                access,
                m_Frame,
                m_Frame.CreateStatePort("Equipment", services.EquipmentPolicy),
                actionStore,
                m_Input,
                handles,
                m_GameplayEffects,
                m_Frame.Facts,
                m_Frame.Trace);
            m_Actions = new Float32ActionRuntime(
                access,
                m_Frame,
                m_Input,
                actionStore,
                m_Blackboard,
                m_GameplayEffects,
                m_GameplayEffects,
                handles,
                m_Frame.Facts,
                m_Frame.Trace,
                m_Equipment);
            m_Values = new Float32ValueRuntime(
                access,
                m_Input,
                actionStore,
                m_Actions,
                m_GameplayEffects,
                m_Equipment,
                m_Blackboard,
                m_Frame,
                workspace);
            m_Motion = new Float32MotionAccumulator(
                access,
                m_Frame,
                m_Frame.CreateStatePort("MotionModifier", services.MotionModifierPolicy),
                workspace.MotionContributions,
                workspace.MotionWarpSamples);
            var locomotion = new Float32LocomotionRuntime(access, m_Values, m_Motion, m_Frame);
            var camera = new Float32CameraOperationRuntime(access, m_Frame.Presentation);
            Float32StatePort timelineState = m_Frame.CreateStatePort("Timeline", services.TimelinePolicy);
            var timelineControlState = new Float32TimelineControlStatePort(access, timelineState);
            var timelineTarget = new Float32TimelineTargetLeaf(
                access,
                timelineState,
                m_Frame,
                actionStore,
                controlState,
                m_Blackboard,
                m_Motion,
                m_Motion,
                m_Frame.Facts,
                m_Frame.Presentation,
                m_Frame.Trace);
            m_Timeline = new TimelineControlRuntime<Float32OperationTarget, Float32Scalar>(
                timelineControlState,
                timelineTarget,
                workspace.TimelineSegments);
            var target = new Float32OperationTarget(
                access,
                controlState,
                m_Frame.CreateOperationStateReset(),
                m_Values,
                m_Blackboard,
                m_Actions,
                m_GameplayEffects,
                m_Equipment,
                camera,
                m_Timeline,
                locomotion,
                m_Frame.Facts,
                m_Frame.Trace);
            m_Control = new OperationControlRuntime<Float32OperationTarget>(
                access.Topology,
                target,
                checked(Math.Max(1024, m_Frame.Program.Operations.Count * 128)));
        }

        public bool Matches(SimulationEvaluateRequest request)
        {
            return request != null &&
                ReferenceEquals(request.Program, m_Frame.Program) &&
                ReferenceEquals(request.ExecutionLayout, m_Frame.Layout) &&
                request.ActorId == m_Frame.ActorId;
        }

        public bool Matches(PendingCharacterEvaluation pending)
        {
            return pending != null &&
                ReferenceEquals(pending.Program, m_Frame.Program) &&
                ReferenceEquals(pending.ExecutionLayout, m_Frame.Layout) &&
                pending.ActorId == m_Frame.ActorId;
        }

        public CharacterOperationEvaluation Evaluate(SimulationEvaluateRequest request)
        {
            using (request.Performance.Measure(SimulationPerformancePhase.OperationFrameBegin))
                m_Frame.Begin(request);
            try
            {
                using (request.Performance.Measure(SimulationPerformancePhase.OperationSetup))
                {
                    m_Control.BeginEvaluation();
                    m_Values.BeginEvaluation();
                    m_GameplayEffects.BeginEvaluation();
                    m_Equipment.BeginEvaluation();
                    m_Blackboard.BeginFrame();
                }
                using (request.Performance.Measure(SimulationPerformancePhase.OperationIngress))
                    ApplyIngress();
                using (request.Performance.Measure(SimulationPerformancePhase.GameplayEffectAdvance))
                    m_GameplayEffects.Advance();
                using (request.Performance.Measure(SimulationPerformancePhase.InputRequestApply))
                {
                    m_Input.ApplyRequests();
                    m_Input.ApplyInputDerived(m_Blackboard);
                }
                using (request.Performance.Measure(SimulationPerformancePhase.TimelineDecision))
                    m_Timeline.PrepareDecisionTimelines(m_Control.Cursor);
                using (request.Performance.Measure(SimulationPerformancePhase.ControlTick))
                    m_Control.Tick(m_Frame.Layout.RootOperation);
                m_Equipment.EndEvaluation();
                ResolvedGameplayMotion motion;
                using (request.Performance.Measure(SimulationPerformancePhase.MotionResolve))
                    motion = m_Motion.Resolve();
                using (request.Performance.Measure(SimulationPerformancePhase.BlackboardFinalize))
                    m_Blackboard.EndFrame();
                using (request.Performance.Measure(SimulationPerformancePhase.EvaluationFreeze))
                    return m_Frame.Complete(motion);
            }
            finally
            {
                m_GameplayEffects.EndEvaluation();
                m_Frame.End();
            }
        }

        void ApplyIngress()
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

    }
}
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                               
