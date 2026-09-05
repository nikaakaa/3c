using ThirdPersonSimulation;
using System;
using System.Collections.Generic;
using ThirdPersonPerformance.Instrumentation;

namespace ThirdPersonSimulation.Fixed
{
    internal sealed class FixedCameraOperationRuntime : FixedOperationModule
    {
        readonly FixedPresentationSink m_Presentation;

        public FixedCameraOperationRuntime(FixedProgramAccess access, FixedPresentationSink presentation)
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
            FixedScalar weight = operation.Code switch
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
                FixedScalar.Zero,
                weight));
        }

        FixedScalar RequireScalar(SimulationOperation operation, OperationNamedConstant field)
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

    internal readonly struct FixedOperationTarget : IOperationControlTarget<FixedOperationTarget>
    {
        readonly FixedProgramAccess m_Access;
        readonly FixedControlStateAccess m_ControlState;
        readonly FixedOperationStateReset m_ResetState;
        readonly FixedValueRuntime m_Values;
        readonly FixedBlackboardRuntime m_Blackboard;
        readonly FixedActionRuntime m_Actions;
        readonly FixedGameplayEffectOperationRuntime m_GameplayEffects;
        readonly FixedEquipmentRuntime m_Equipment;
        readonly FixedCameraOperationRuntime m_Camera;
        readonly TimelineControlRuntime<FixedOperationTarget, FixedScalar> m_Timeline;
        readonly FixedLocomotionRuntime m_Locomotion;
        readonly FixedFactSink m_Facts;
        readonly FixedTraceSink m_Trace;

        public FixedOperationTarget(
            FixedProgramAccess access,
            FixedControlStateAccess controlState,
            FixedOperationStateReset resetState,
            FixedValueRuntime values,
            FixedBlackboardRuntime blackboard,
            FixedActionRuntime actions,
            FixedGameplayEffectOperationRuntime gameplayEffects,
            FixedEquipmentRuntime equipment,
            FixedCameraOperationRuntime camera,
            TimelineControlRuntime<FixedOperationTarget, FixedScalar> timeline,
            FixedLocomotionRuntime locomotion,
            FixedFactSink facts,
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
            OperationControlCursor<FixedOperationTarget> cursor,
            ProgramControlFlowEdge edge)
        {
            return m_Values.EvaluateCondition(cursor, edge);
        }

        OperationExecutionResult TickLocomotion(
            OperationControlCursor<FixedOperationTarget> cursor,
            SimulationOperation operation)
        {
            int slot = m_Access.RequireOperationSlot(operation.Handle, ProgramStateSemantic.LocomotionMotionElapsedTicks);
            int elapsedTicks = m_ControlState.ReadInt32(slot);
            int committedTicks = checked(elapsedTicks + 1);
            int generationSlot = m_Access.RequireOperationSlot(
                operation.Handle,
                ProgramStateSemantic.RunnableActivationGeneration);
            m_Locomotion.Submit(
                cursor,
                operation,
                committedTicks,
                m_ControlState.ReadUInt64(generationSlot));
            var mode = (LocomotionInputMotionExecutionMode)operation.Integer0;
            if (mode == LocomotionInputMotionExecutionMode.Once)
                return OperationExecutionResult.Success;
            m_ControlState.WriteInt32(slot, committedTicks);
            if (mode == LocomotionInputMotionExecutionMode.Continuous)
                return OperationExecutionResult.Running;
            if (mode != LocomotionInputMotionExecutionMode.Timed)
                throw new InvalidOperationException($"Locomotion operation '{operation.Handle}' has invalid execution mode '{operation.Integer0}'.");
            ProgramConstant duration = m_Access.FindConstant(operation, OperationNamedConstant.DurationSeconds);
            if (duration == null || duration.Kind != ProgramConstantKind.Scalar)
                throw new InvalidOperationException($"Locomotion operation '{operation.Handle}' has no duration.");
            int requiredTicks = checked((int)Math.Ceiling(duration.Scalar.ToDouble() * m_Access.Program.Manifest.TickRate));
            if (requiredTicks <= 0)
                throw new InvalidOperationException($"Locomotion operation '{operation.Handle}' has a non-positive timed duration.");
            return committedTicks >= requiredTicks
                ? OperationExecutionResult.Success
                : OperationExecutionResult.Running;
        }

		public OperationExecutionResult ExecuteLeaf(
			OperationControlCursor<FixedOperationTarget> cursor,
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
					using (FixedValueInputLease equipmentInputs = m_Values.ReadInputs(cursor, operation))
						return m_Equipment.TickHost(cursor, operation, equipmentInputs);
				case SimulationOperationCode.LocomotionInputMotion:
					return TickLocomotion(cursor, operation);
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
					return FixedValueRuntime.ToBoolean(m_Values.Evaluate(cursor, operation.Handle))
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
						$"Portable control operation '{descriptor.Code}' reached the Fixed leaf dispatcher.");
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
						$"Operation '{descriptor.Handle}' code '{descriptor.Code}' has no Fixed owner.");
			}
		}

        public void PrepareActivation(OperationExecutionDescriptor operation)
        {
        }

        public void ActivateScopes(
            OperationControlCursor<FixedOperationTarget> cursor,
            OperationExecutionDescriptor descriptor,
            ulong generation)
        {
            m_Actions.BindCurrentSkillExecution(descriptor.Handle, generation);
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
            OperationControlCursor<FixedOperationTarget> cursor,
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
            OperationControlCursor<FixedOperationTarget> cursor,
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
                FixedScalar.Zero));
        }
    }

    internal sealed class FixedOperationEvaluator
    {
        readonly FixedEvaluationFrame m_Frame;
        readonly FixedBlackboardRuntime m_Blackboard;
        readonly FixedActionRuntime m_Actions;
        readonly FixedActionStateStore m_ActionStore;
        readonly FixedGameplayEffectOperationRuntime m_GameplayEffects;
        readonly FixedEquipmentRuntime m_Equipment;
        readonly FixedInputRuntime m_Input;
        readonly FixedValueRuntime m_Values;
        readonly TimelineControlRuntime<FixedOperationTarget, FixedScalar> m_Timeline;
        readonly FixedMotionAccumulator m_Motion;
        readonly OperationControlRuntime<FixedOperationTarget> m_Control;
        readonly CharacterControlStateMachineRuntime m_CharacterControl;
        readonly FixedCharacterControlReadPort m_CharacterControlRead;
        readonly ICharacterControlStatePort m_CharacterControlState;
        readonly FixedCharacterControlOutputPort m_CharacterControlOutput;

        public FixedOperationEvaluator(
            CharacterSimulationProgram program,
            ProgramExecutionLayout layout,
            ActorId actorId,
            FixedEvaluationWorkspace workspace,
            CharacterControlModuleCatalog controlModules)
        {
            if (controlModules == null)
                throw new ArgumentNullException(nameof(controlModules));
            m_Frame = new FixedEvaluationFrame(program, layout, actorId, workspace);
            FixedProgramExecutionServices services = m_Frame.Services;
            FixedProgramAccess access = services.Access;
            var controlState = new FixedControlStateAccess(
                access,
                m_Frame.CreateStatePort("Control", services.ControlPolicy));
            var actionStore = new FixedActionStateStore(
                access,
                m_Frame.CreateStatePort("Action", services.ActionPolicy));
            m_ActionStore = actionStore;
            m_Input = new FixedInputRuntime(
                access,
                m_Frame.CreateStatePort("Input", services.InputPolicy),
                m_Frame);
            var handles = new FixedHandleAllocator(
                access,
                m_Frame.CreateStatePort("HandleAllocator", services.HandleAllocatorPolicy));
            m_Blackboard = new FixedBlackboardRuntime(
                access,
                m_Frame.CreateStatePort("Blackboard", services.BlackboardPolicy),
                m_Frame,
                actionStore,
                m_Frame.Facts,
                m_Frame.Trace,
                workspace);
            m_GameplayEffects = new FixedGameplayEffectOperationRuntime(
                access,
                m_Frame,
                actionStore,
                handles,
                m_Frame.Facts,
                m_Frame.Presentation,
                m_Frame.Trace,
                workspace.GameplayEffects);
            m_Equipment = new FixedEquipmentRuntime(
                access,
                m_Frame,
                m_Frame.CreateStatePort("Equipment", services.EquipmentPolicy),
                actionStore,
                m_Input,
                handles,
                m_GameplayEffects,
                m_Frame.Facts,
                m_Frame.Trace);
            m_Actions = new FixedActionRuntime(
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
            m_Values = new FixedValueRuntime(
                access,
                m_Input,
                actionStore,
                m_Actions,
                m_GameplayEffects,
                m_Equipment,
                m_Blackboard,
                m_Frame,
                workspace);
            m_Motion = new FixedMotionAccumulator(
                access,
                m_Frame,
                m_Frame.CreateStatePort("MotionModifier", services.MotionModifierPolicy),
                workspace.MotionContributions,
                workspace.MotionWarpSamples);
            var locomotion = new FixedLocomotionRuntime(access, m_Values, m_Motion, m_Frame);
            if (program.ControlModuleBinding.IsValid)
            {
                ICharacterControlModule module = controlModules.Require(program.ControlModuleBinding);
                ProgramCatalogEntry controlCatalog = program.CatalogEntries[program.ControlModuleBinding.CatalogEntryIndex];
                FixedStatePort characterControlState = m_Frame.CreateStatePort("CharacterControl", services.ControlPolicy);
                CharacterControlStateLayout characterControlLayout = layout.CreateControlStateLayout(module.Contract);
                m_CharacterControl = new CharacterControlStateMachineRuntime(module);
                m_CharacterControlRead = new FixedCharacterControlReadPort(
                    m_Input,
                    m_Frame,
                    parameter => ReadControlParameter(controlCatalog, parameter),
                    skill => m_Actions.IsSkillActive(skill),
                    skill => m_Actions.IsSkillCompleted(skill),
                    skill => m_Actions.CompletedSkillInstanceId(skill),
                    (skill, window) => m_Blackboard.IsActionWindowActive(skill, window));
                m_CharacterControlState = new FixedCharacterControlStatePort(characterControlState, characterControlLayout);
                m_CharacterControlOutput = new FixedCharacterControlOutputPort(
                    access,
                    controlCatalog,
                    m_Input,
                    locomotion,
                    m_Actions);
            }
            var camera = new FixedCameraOperationRuntime(access, m_Frame.Presentation);
            FixedStatePort timelineState = m_Frame.CreateStatePort("Timeline", services.TimelinePolicy);
            var timelineControlState = new FixedTimelineControlStatePort(access, timelineState);
            var timelineTarget = new FixedTimelineTargetLeaf(
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
            m_Timeline = new TimelineControlRuntime<FixedOperationTarget, FixedScalar>(
                timelineControlState,
                timelineTarget,
                workspace.TimelineSegments);
            var target = new FixedOperationTarget(
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
            m_Control = new OperationControlRuntime<FixedOperationTarget>(
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
            BeginOperationFrame(request);
            try
            {
                SetupOperation();
                ApplyIngress();
                AdvanceGameplayEffects();
                ApplyInputRequests();
                PrepareTimelineDecision();
                TickCharacterControl();
                TickSkillPrograms();
                TickOperationControl();
                m_Equipment.EndEvaluation();
                ResolvedGameplayMotion motion = ResolveMotion();
                FinalizeBlackboard();
                return CompleteOperationFrame(motion);
            }
            finally
            {
                m_GameplayEffects.EndEvaluation();
                m_Frame.End();
            }
        }

        [PerformanceProbe("simulation.operation.frame-begin")]
        void BeginOperationFrame(SimulationEvaluateRequest request)
        {
            m_Frame.Begin(request);
        }

        [PerformanceProbe("simulation.operation.setup")]
        void SetupOperation()
        {
            m_Control.BeginEvaluation();
            m_Values.BeginEvaluation();
            m_GameplayEffects.BeginEvaluation();
            m_Equipment.BeginEvaluation();
            m_Blackboard.BeginFrame();
        }

        [PerformanceProbe("simulation.operation.ingress")]
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

        [PerformanceProbe("simulation.operation.gameplay-effect-advance")]
        void AdvanceGameplayEffects()
        {
            m_GameplayEffects.Advance();
        }

        [PerformanceProbe("simulation.operation.input-request-apply")]
        void ApplyInputRequests()
        {
            m_Input.ApplyRequests();
            m_Input.ApplyBlackboardInputBindings(m_Blackboard);
        }

        [PerformanceProbe("simulation.operation.timeline-decision")]
        void PrepareTimelineDecision()
        {
            m_Timeline.PrepareDecisionTimelines(m_Control.Cursor);
        }

        [PerformanceProbe("simulation.operation.control-tick")]
        void TickOperationControl()
        {
        }

        [PerformanceProbe("simulation.operation.character-control-tick")]
        void TickCharacterControl()
        {
            if (m_CharacterControl == null)
                return;
            var context = new CharacterControlTickContext(
                m_Frame.ActorId,
                m_Frame.Tick,
                m_Frame.Program.Manifest.TickRate);
            m_CharacterControl.Tick(
                in context,
                m_CharacterControlRead,
                m_CharacterControlState,
                m_CharacterControlOutput);
        }

        [PerformanceProbe("simulation.operation.skill-program-tick")]
        void TickSkillPrograms()
        {
            if (m_CharacterControl == null)
                return;
            IReadOnlyList<CharacterSkillProgramBinding> skills = m_Frame.Program.SkillPrograms.Bindings;
            var stoppingContexts = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < skills.Count; i++)
            {
                CharacterSkillProgramBinding skill = skills[i];
                if (m_Control.IsStopping(skill.EntryOperation))
                {
                    OperationStopStatus stop = m_Control.ContinueStop(skill.EntryOperation);
                    if (stop == OperationStopStatus.Failed)
                        throw new InvalidOperationException($"Skill '{skill.SkillId}' EntryOperation stop failed.");
                    if (stop == OperationStopStatus.Running)
                        stoppingContexts.Add(skill.ActionContextId);
                    continue;
                }
                if (!m_ActionStore.IsSkillActive(skill.SkillId) && m_Control.IsActive(skill.EntryOperation))
                {
                    OperationStopStatus stop = m_Control.RequestStop(
                        skill.EntryOperation,
                        OperationStopContext.ActionContextEnded(skill.EntryOperation));
                    if (stop == OperationStopStatus.Failed)
                        throw new InvalidOperationException($"Skill '{skill.SkillId}' EntryOperation stop failed.");
                    if (stop == OperationStopStatus.Running)
                        stoppingContexts.Add(skill.ActionContextId);
                }
            }
            for (int i = 0; i < skills.Count; i++)
            {
                CharacterSkillProgramBinding skill = skills[i];
                if (stoppingContexts.Contains(skill.ActionContextId))
                    continue;
                bool actionActive = m_ActionStore.FindActive(skill.SkillId, out FixedActionInstanceState action) >= 0;
                if (!actionActive)
                {
                    m_Actions.TryCommitPendingControl(skill.SkillId);
                    actionActive = m_ActionStore.FindActive(skill.SkillId, out action) >= 0;
                }
                if (!actionActive)
                    continue;
                if (!action.SkillEntryOperation.Equals(skill.EntryOperation))
                    throw new InvalidOperationException($"Skill '{skill.SkillId}' Action instance is bound to a different EntryOperation.");
                OperationRunnableStatus status = m_Control.ReadStatus(skill.EntryOperation);
                if (status == OperationRunnableStatus.Success || status == OperationRunnableStatus.Failure)
                {
                    if (action.SkillExecutionGeneration == 0)
                    {
                        OperationStopStatus reset = m_Control.RequestStop(
                            skill.EntryOperation,
                            OperationStopContext.ActionContextEnded(skill.EntryOperation));
                        if (reset != OperationStopStatus.Completed)
                            throw new InvalidOperationException($"Skill '{skill.SkillId}' previous EntryOperation state could not reset.");
                        status = m_Control.ReadStatus(skill.EntryOperation);
                    }
                    else
                    {
                        m_Actions.FinishFromControl(
                            skill.SkillId,
                            SimulationExecutionSource.FromSkillOperation(
                                skill.EntryOperation,
                                m_Frame.Services.SourcePath(skill.EntryOperation)),
                            status == OperationRunnableStatus.Success,
                            status == OperationRunnableStatus.Success ? "SkillCompleted" : "SkillExecutionFailed");
                        continue;
                    }
                }
                if (action.SkillExecutionGeneration != 0 && status == OperationRunnableStatus.Dormant)
                    throw new InvalidOperationException($"Skill '{skill.SkillId}' Action instance lost its EntryOperation state.");
                if (action.SkillExecutionGeneration != 0 &&
                    m_Control.ReadGeneration(skill.EntryOperation) != action.SkillExecutionGeneration)
                    throw new InvalidOperationException($"Skill '{skill.SkillId}' Action instance generation does not match its EntryOperation.");
                using (m_ActionStore.PushSkillExecution(action))
                {
                    OperationExecutionResult result = m_Control.Tick(skill.EntryOperation);
                    FixedActionInstanceState current = m_ActionStore.RequireActiveTransient(
                        FixedActionInstanceReference.FromInstance(action));
                    if (current.IsActive)
                    {
                        current = m_ActionStore.BindSkillExecution(
                            current,
                            skill.EntryOperation,
                            m_Control.ReadGeneration(skill.EntryOperation));
                        if (result == OperationExecutionResult.Success || result == OperationExecutionResult.Failure)
                            m_Actions.FinishFromControl(
                                skill.SkillId,
                                SimulationExecutionSource.FromSkillOperation(
                                    skill.EntryOperation,
                                    m_Frame.Services.SourcePath(skill.EntryOperation)),
                                result == OperationExecutionResult.Success,
                                result == OperationExecutionResult.Success ? "SkillCompleted" : "SkillExecutionFailed");
                    }
                }
            }
        }

        FixedScalar ReadControlParameter(
            ProgramCatalogEntry controlCatalog,
            CharacterControlParameterId parameter)
        {
            string name = $"Parameter:{parameter.Value}:NumericValue";
            for (int i = 0; i < controlCatalog.Fields.Count; i++)
            {
                ProgramCatalogField field = controlCatalog.Fields[i];
                if (!string.Equals(field.Name, name, StringComparison.Ordinal))
                    continue;
                if (field.Kind != ProgramCatalogFieldKind.Constant || field.ConstantIndex < 0 ||
                    field.ConstantIndex >= m_Frame.Program.Constants.Count)
                    throw new InvalidOperationException($"Control module field '{name}' is not a valid constant.");
                ProgramConstant value = m_Frame.Program.Constants[field.ConstantIndex];
                if (value.Kind != ProgramConstantKind.Scalar)
                    throw new InvalidOperationException($"Control module field '{name}' is not Scalar.");
                return value.Scalar;
            }
            throw new InvalidOperationException($"Control module '{controlCatalog.Identity}' has no '{name}' field.");
        }

        [PerformanceProbe("simulation.operation.motion-resolve")]
        ResolvedGameplayMotion ResolveMotion()
        {
            return m_Motion.Resolve();
        }

        [PerformanceProbe("simulation.operation.blackboard-finalize")]
        void FinalizeBlackboard()
        {
            m_Blackboard.EndFrame();
        }

        [PerformanceProbe("simulation.operation.frame-complete")]
        CharacterOperationEvaluation CompleteOperationFrame(ResolvedGameplayMotion motion)
        {
            return m_Frame.Complete(motion);
        }

    }
}
