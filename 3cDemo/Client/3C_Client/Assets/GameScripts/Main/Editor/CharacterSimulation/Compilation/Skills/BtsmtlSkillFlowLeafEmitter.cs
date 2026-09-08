using System;
using System.Collections.Generic;
using System.Linq;
using FlowCanvas;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public sealed class BtsmtlSkillFlowLeafEmitter
    {
        readonly CharacterSimulationOperationEmitter m_Emitter;

        public BtsmtlSkillFlowLeafEmitter(CharacterSimulationProgramBuilder builder)
        {
            m_Emitter = new CharacterSimulationOperationEmitter(builder);
        }

        public OperationHandle Emit(FlowNode node, string route, string contentHash)
        {
            if (node.graph is not IBtsmtlSkillFlowGraph graph)
                throw new ArgumentException("A skill node must belong to its formal authoring graph.", nameof(node));
            CharacterSimulationNodeEmission emission = Describe(node);
            BtsmtlSkillNativeNodeCatalog.TryGet(node.GetType(), out BtsmtlSkillNativeNodeContract native);
            OperationValuePortContract contract = CharacterGameplayValuePortContracts.Require(emission.Code);
            if (node.GetInputValuePorts().Count() != contract.Inputs.Count || node.GetOutputValuePorts().Count() != contract.Outputs.Count)
                throw new InvalidOperationException($"Skill node '{node.UID}' does not match its compiled value port shape.");
            var inputs = new List<CharacterSimulationConstantInput>();
            foreach (ValueInput port in node.GetInputValuePorts().OrderBy(value => value.ID, StringComparer.Ordinal))
            {
                string compiledPort = native != null ? native.Input(port.ID) : port.ID;
                OperationValuePortDefinition definition = contract.RequireInput(compiledPort);
                if (!definition.Accepts(Kind(port.type)))
                    throw new InvalidOperationException($"Skill port '{port.ID}' has an incompatible declared type.");
                if (node.inConnections.OfType<BinderConnection>().Any(edge => edge.targetPortID == port.ID))
                    continue;
                object value = port.serializedValue;
                SemanticValueKind kind = Kind(port.type);
                if (value == null && port.type == typeof(string))
                    value = string.Empty;
                if (!definition.Accepts(kind))
                    throw new InvalidOperationException($"Skill port '{port.ID}' does not accept '{kind}'.");
                inputs.Add(new CharacterSimulationConstantInput(compiledPort, kind, value, Source(node, graph, route, contentHash, port.ID)));
            }
            foreach (ValueOutput port in node.GetOutputValuePorts())
                if (!contract.RequireSelection(native != null ? native.Output(port.ID) : port.ID).Accepts(Kind(port.type)))
                    throw new InvalidOperationException($"Skill output '{port.ID}' has an incompatible declared type.");
            return m_Emitter.Emit(Source(node, graph, route, contentHash, string.Empty), emission, inputs);
        }

        static CharacterSimulationNodeEmission Describe(FlowNode node)
        {
            if (BtsmtlSkillNativeNodeCatalog.TryGet(node.GetType(), out BtsmtlSkillNativeNodeContract native))
                return new CharacterSimulationNodeEmission(native.Code, integer0: native.Variant);
            return node switch
            {
            BtsmtlSkillRootFlowNode => new CharacterSimulationNodeEmission(SimulationOperationCode.Root),
            BtsmtlSkillSucceedFlowNode => new CharacterSimulationNodeEmission(SimulationOperationCode.Succeed),
            BtsmtlSkillConditionResultFlowNode => new CharacterSimulationNodeEmission(SimulationOperationCode.ConditionResult),
            BtsmtlSkillStateEnterFlowNode => new CharacterSimulationNodeEmission(SimulationOperationCode.StateEnter),
            BtsmtlSkillStateAnyFlowNode => new CharacterSimulationNodeEmission(SimulationOperationCode.StateAny),
            BtsmtlSkillStateExitFlowNode => new CharacterSimulationNodeEmission(SimulationOperationCode.StateExit),
            BtsmtlSkillStateOnEnterFlowNode => new CharacterSimulationNodeEmission(SimulationOperationCode.StateOnEnter),
            BtsmtlSkillStateOnExitFlowNode => new CharacterSimulationNodeEmission(SimulationOperationCode.StateOnExit),
            BtsmtlSkillStateRootCompletedFlowNode => new CharacterSimulationNodeEmission(SimulationOperationCode.StateRootCompleted),
            BtsmtlSkillStateExitCauseFlowNode cause => new CharacterSimulationNodeEmission(SimulationOperationCode.StateExitCause, integer0: (int)cause.Cause),
            BtsmtlSkillLoopFlowNode loop => new CharacterSimulationNodeEmission(SimulationOperationCode.Loop, integer0: (int)loop.StopType),
            BtsmtlSkillTimelineHookFlowNode hook => new CharacterSimulationNodeEmission(SimulationOperationCode.TimelineEnter, integer0: (int)hook.Hook),
            BtsmtlSkillTimelineFlowNode timeline => new CharacterSimulationNodeEmission(SimulationOperationCode.Timeline,
                integer0: (int)timeline.PlaybackMode, text0: timeline.Timeline?.AuthoringId,
                constants: CharacterSimulationNodeEmitterRegistry.Fields(("ActionContext", CharacterSimulationNodeEmitterContext.AssetIdentity(timeline.ActionContext)))),
            BtsmtlSkillStateMachineFlowNode machine => new CharacterSimulationNodeEmission(SimulationOperationCode.StateMachine, text0: machine.StateMachine?.AuthoringId),
            BtsmtlSkillStateFlowNode state => new CharacterSimulationNodeEmission(SimulationOperationCode.State, text0: state.Body?.AuthoringId),
            BtsmtlSkillSequenceFlowNode => new CharacterSimulationNodeEmission(SimulationOperationCode.Sequence),
            BtsmtlSkillSelectorFlowNode => new CharacterSimulationNodeEmission(SimulationOperationCode.Selector),
            BtsmtlSkillParallelFlowNode parallel => new CharacterSimulationNodeEmission(SimulationOperationCode.Parallel, integer0: (int)parallel.Mode),
            BtsmtlSkillBooleanInputFlowNode input => Input(SimulationOperationCode.InputBoolean, input.InputId),
            BtsmtlSkillScalarInputFlowNode input => Input(SimulationOperationCode.InputScalar, input.InputId),
            BtsmtlSkillVector2InputFlowNode input => Input(SimulationOperationCode.InputVector2, input.InputId),
            BtsmtlSkillInputMagnitudeFlowNode input => Input(SimulationOperationCode.InputVector2Magnitude, input.InputId),
            BtsmtlSkillActionRequestFlowNode input => Input(SimulationOperationCode.InputRequest, input.InputId),
            IBtsmtlSkillBlackboardReadNode blackboard => new CharacterSimulationNodeEmission(
                SimulationOperationCode.BlackboardGet, text0: blackboard.Variable.DeclarationId,
                constants: CharacterSimulationNodeEmitterRegistry.Fields(("DeclarationOwner", blackboard.Variable.OwnerId))),
            BtsmtlSkillBlackboardAccessFlowNode blackboard => new CharacterSimulationNodeEmission(
                blackboard.Writes ? SimulationOperationCode.BlackboardSet : SimulationOperationCode.BlackboardGet,
                integer0: blackboard.Writes ? 1 : 0, text0: blackboard.Variable.DeclarationId,
                constants: CharacterSimulationNodeEmitterRegistry.Fields(("DeclarationOwner", blackboard.Variable.OwnerId),
                    ("FactContext", CharacterSimulationNodeEmitterContext.AssetIdentity(blackboard.FactContext)))),
            BtsmtlSkillMoveFacingAngleFlowNode => new CharacterSimulationNodeEmission(SimulationOperationCode.MoveFacingAngle),
            BtsmtlSkillActionContextActiveFlowNode context => new CharacterSimulationNodeEmission(
                SimulationOperationCode.ActionContextActive, text0: CharacterSimulationNodeEmitterContext.AssetIdentity(context.ActionContext)),
            BtsmtlSkillActionWindowActiveFlowNode window => new CharacterSimulationNodeEmission(
                SimulationOperationCode.ActionWindowActive, text0: window.WindowType),
            BtsmtlSkillCanActivateActionFlowNode action => new CharacterSimulationNodeEmission(
                SimulationOperationCode.CanActivateAction,
                text0: action.ActionProfile ? action.ActionProfile.ActionId : string.Empty,
                constants: CharacterSimulationNodeEmitterRegistry.Fields(
                    ("ActionProfile", CharacterSimulationNodeEmitterContext.AssetIdentity(action.ActionProfile)),
                    ("TargetSnapshotDeclaration", action.TargetSnapshotDeclarationId),
                    ("TargetSnapshotOwner", action.TargetSnapshotOwnerId))),
            BtsmtlSkillSubmitActionLifecycleFlowNode lifecycle => new CharacterSimulationNodeEmission(
                SimulationOperationCode.SubmitActionLifecycle,
                integer0: (int)lifecycle.TransitionType,
                text0: lifecycle.Reason,
                constants: CharacterSimulationNodeEmitterRegistry.Fields(
                    ("ActionContext", CharacterSimulationNodeEmitterContext.AssetIdentity(lifecycle.ActionContext)))),
                _ => throw new InvalidOperationException($"Skill node '{node.GetType().Name}' has no leaf emission contract.")
            };
        }

        static CharacterSimulationNodeEmission Input(SimulationOperationCode code, string identity)
        {
            if (string.IsNullOrWhiteSpace(identity))
                throw new InvalidOperationException("A skill input node has no declared input identity.");
            return new CharacterSimulationNodeEmission(code, text0: identity);
        }

        static SemanticValueKind Kind(Type type)
        {
            if (type == typeof(bool)) return SemanticValueKind.Boolean;
            if (type == typeof(int) || type.IsEnum) return SemanticValueKind.Int32;
            if (type == typeof(uint) || type == typeof(ulong)) return SemanticValueKind.UInt64;
            if (type == typeof(float) || type == typeof(double)) return SemanticValueKind.Number;
            if (type == typeof(Vector2)) return SemanticValueKind.Vector2;
            if (type == typeof(Vector3)) return SemanticValueKind.Vector3;
            if (type == typeof(string)) return SemanticValueKind.Identity;
            throw new InvalidOperationException($"Unsupported skill value type '{type.FullName}'.");
        }

        static CharacterSimulationSourceLocation Source(FlowNode node, IBtsmtlSkillFlowGraph graph,
            string route, string contentHash, string portId) => new CharacterSimulationSourceLocation(
                node.GetType().FullName, graph.AuthoringId, node.UID, string.Empty, string.Empty, string.Empty,
                string.IsNullOrEmpty(portId) ? $"{route}/node:{node.UID}" : $"{route}/node:{node.UID}/port:{portId}",
                portId: portId, contentHash: contentHash);
    }
}
