using System;
using System.Collections.Generic;
using System.Linq;
using FlowCanvas;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonGameplay.Tags;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public sealed class BtsmtlSkillFlowLeafEmitter
    {
        readonly CharacterSimulationOperationEmitter m_Emitter;
        readonly string m_ControlModuleId;
        readonly string m_InputProviderOwnerId;
        readonly string m_GameplayProviderOwnerId;

        public BtsmtlSkillFlowLeafEmitter(
            CharacterSimulationProgramBuilder builder,
            string controlModuleId,
            string inputProviderOwnerId,
            string gameplayProviderOwnerId)
        {
            m_Emitter = new CharacterSimulationOperationEmitter(builder);
            m_ControlModuleId = controlModuleId ?? string.Empty;
            m_InputProviderOwnerId = inputProviderOwnerId ?? string.Empty;
            m_GameplayProviderOwnerId = gameplayProviderOwnerId ?? string.Empty;
        }

        public OperationHandle Emit(FlowNode node, string route, string contentHash)
        {
            if (node.graph is not IBtsmtlSkillFlowGraph graph)
                throw new ArgumentException("A skill node must belong to its formal authoring graph.", nameof(node));
            ValidateCharacterStateNode(node, m_ControlModuleId);
            ValidateProviderOwner(node, m_ControlModuleId, m_InputProviderOwnerId, m_GameplayProviderOwnerId);
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
            BtsmtlSkillBooleanInputFlowNode input => Input(SimulationOperationCode.InputBoolean, input.InputId, input.ProviderOwnerId),
            BtsmtlSkillScalarInputFlowNode input => Input(SimulationOperationCode.InputScalar, input.InputId, input.ProviderOwnerId),
            BtsmtlSkillVector2InputFlowNode input => Input(SimulationOperationCode.InputVector2, input.InputId, input.ProviderOwnerId),
            BtsmtlSkillInputMagnitudeFlowNode input => Input(SimulationOperationCode.InputVector2Magnitude, input.InputId, input.ProviderOwnerId),
            BtsmtlSkillActionRequestFlowNode input => Input(SimulationOperationCode.InputRequest, input.InputId, input.ProviderOwnerId),
            IBtsmtlSkillBlackboardReadNode blackboard => new CharacterSimulationNodeEmission(
                SimulationOperationCode.BlackboardGet, text0: blackboard.Variable.DeclarationId,
                constants: CharacterSimulationNodeEmitterRegistry.Fields(("DeclarationOwner", blackboard.Variable.OwnerId))),
            BtsmtlSkillBlackboardAccessFlowNode blackboard => new CharacterSimulationNodeEmission(
                blackboard.Writes ? SimulationOperationCode.BlackboardSet : SimulationOperationCode.BlackboardGet,
                integer0: blackboard.Writes ? 1 : 0, text0: blackboard.Variable.DeclarationId,
                constants: CharacterSimulationNodeEmitterRegistry.Fields(("DeclarationOwner", blackboard.Variable.OwnerId),
                    ("FactContext", CharacterSimulationNodeEmitterContext.AssetIdentity(blackboard.FactContext)))),
            BtsmtlSkillMoveFacingAngleFlowNode move => new CharacterSimulationNodeEmission(
                SimulationOperationCode.MoveFacingAngle,
                constants: CharacterSimulationNodeEmitterRegistry.Fields(("ProviderOwner", move.ProviderOwnerId))),
            BtsmtlSkillCharacterStateVector3FlowNode state => CharacterState(state.FieldId, state.ProviderOwnerId),
            BtsmtlSkillCharacterStateScalarFlowNode state => CharacterState(state.FieldId, state.ProviderOwnerId),
            BtsmtlSkillCharacterStateYawFlowNode state => CharacterState(state.FieldId, state.ProviderOwnerId),
            BtsmtlSkillCharacterStateBooleanFlowNode state => CharacterState(state.FieldId, state.ProviderOwnerId),
            BtsmtlSkillLocomotionFlowNode motion => CharacterSimulationMotionNodeEmitterRegistration.Locomotion(motion, motion.UID),
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
            BtsmtlSkillGameplayTagFlowNode tag => new CharacterSimulationNodeEmission(
                SimulationOperationCode.GameplayEffectHasTag,
                text0: TagIdentity(tag.Tag.Value),
                constants: CharacterSimulationNodeEmitterRegistry.Fields(("ProviderOwner", tag.ProviderOwnerId))),
            BtsmtlSkillGameplayTagQueryFlowNode query => new CharacterSimulationNodeEmission(
                SimulationOperationCode.GameplayEffectMatchTags,
                constants: QueryFields(query.Query, "Query", query.ProviderOwnerId)),
            BtsmtlSkillGameplayAttributeFlowNode attribute => new CharacterSimulationNodeEmission(
                SimulationOperationCode.GameplayAttributeRead,
                text0: AttributeIdentity(attribute.Attribute.Value),
                constants: CharacterSimulationNodeEmitterRegistry.Fields(("ProviderOwner", attribute.ProviderOwnerId))),
            BtsmtlSkillApplyGameplayEffectFlowNode apply => new CharacterSimulationNodeEmission(
                SimulationOperationCode.GameplayEffectApply,
                text0: EffectIdentity(apply.Effect ? apply.Effect.EffectId.Value : string.Empty),
                constants: CharacterSimulationNodeEmitterRegistry.Fields(
                    ("DefinitionRevision", apply.Effect ? apply.Effect.DefinitionRevision : 0U),
                    ("ActionContext", CharacterSimulationNodeEmitterContext.AssetIdentity(apply.ActionContext)),
                    ("Predicted", apply.Predicted),
                    ("ProviderOwner", apply.ProviderOwnerId))),
            BtsmtlSkillRemoveGameplayEffectFlowNode remove => new CharacterSimulationNodeEmission(
                SimulationOperationCode.GameplayEffectRemove,
                integer0: (int)remove.Selector,
                constants: RemoveEffectFields(remove)),
            _ => throw new InvalidOperationException($"Skill node '{node.GetType().Name}' has no leaf emission contract.")
            };
        }

        static IReadOnlyList<KeyValuePair<string, object>> RemoveEffectFields(BtsmtlSkillRemoveGameplayEffectFlowNode node)
        {
            var fields = new List<KeyValuePair<string, object>>
            {
                new KeyValuePair<string, object>("Handle", node.Handle),
                new KeyValuePair<string, object>("Effect", EffectIdentity(node.Effect ? node.Effect.EffectId.Value : string.Empty)),
                new KeyValuePair<string, object>("ProviderOwner", node.ProviderOwnerId)
            };
            fields.AddRange(QueryFields(node.EffectTagQuery, "Query", node.ProviderOwnerId));
            return fields;
        }

        static IReadOnlyList<KeyValuePair<string, object>> QueryFields(
            GameplayTagQuery query,
            string prefix,
            string providerOwnerId)
        {
            var fields = new List<KeyValuePair<string, object>>
            {
                new KeyValuePair<string, object>("ProviderOwner", providerOwnerId ?? string.Empty)
            };
            Add(query?.All, "All");
            Add(query?.Any, "Any");
            Add(query?.None, "None");
            return fields;

            void Add(IReadOnlyList<GameplayTagId> values, string kind)
            {
                if (values == null)
                    return;
                for (int i = 0; i < values.Count; i++)
                    fields.Add(new KeyValuePair<string, object>($"{prefix}:{kind}:{i:D4}", TagIdentity(values[i].Value)));
            }
        }

        static string TagIdentity(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : $"tag:{value.Trim()}";
        static string AttributeIdentity(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : $"attribute:{value.Trim()}";
        static string EffectIdentity(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : $"effect:{value.Trim()}";

        static CharacterSimulationNodeEmission Input(SimulationOperationCode code, string identity, string providerOwnerId)
        {
            if (string.IsNullOrWhiteSpace(identity) || string.IsNullOrWhiteSpace(providerOwnerId))
                throw new InvalidOperationException("A skill input node has no declared input identity or provider owner.");
            return new CharacterSimulationNodeEmission(
                code,
                text0: identity,
                constants: CharacterSimulationNodeEmitterRegistry.Fields(("ProviderOwner", providerOwnerId)));
        }

        static CharacterSimulationNodeEmission CharacterState(string fieldId, string providerOwnerId)
        {
            if (!CharacterStateProviderFields.IsValid(fieldId) || !CharacterStateProviderFields.IsOwner(providerOwnerId))
                throw new InvalidOperationException("Character State provider reference is incomplete.");
            return new CharacterSimulationNodeEmission(
                SimulationOperationCode.CharacterStateRead,
                text0: fieldId,
                constants: CharacterSimulationNodeEmitterRegistry.Fields(("ProviderOwner", providerOwnerId)));
        }

        static void ValidateCharacterStateNode(FlowNode node, string controlModuleId)
        {
            string field = node switch
            {
                BtsmtlSkillCharacterStateVector3FlowNode value => value.FieldId,
                BtsmtlSkillCharacterStateScalarFlowNode value => value.FieldId,
                BtsmtlSkillCharacterStateYawFlowNode value => value.FieldId,
                BtsmtlSkillCharacterStateBooleanFlowNode value => value.FieldId,
                _ => string.Empty
            };
            if (string.IsNullOrEmpty(field))
                return;
            string owner = node switch
            {
                BtsmtlSkillCharacterStateVector3FlowNode value => value.ProviderOwnerId,
                BtsmtlSkillCharacterStateScalarFlowNode value => value.ProviderOwnerId,
                BtsmtlSkillCharacterStateYawFlowNode value => value.ProviderOwnerId,
                BtsmtlSkillCharacterStateBooleanFlowNode value => value.ProviderOwnerId,
                _ => string.Empty
            };
            bool valid = node switch
            {
                BtsmtlSkillCharacterStateVector3FlowNode => CharacterStateProviderFields.IsVector3(field),
                BtsmtlSkillCharacterStateScalarFlowNode => CharacterStateProviderFields.IsScalar(field),
                BtsmtlSkillCharacterStateYawFlowNode => CharacterStateProviderFields.IsYaw(field),
                BtsmtlSkillCharacterStateBooleanFlowNode => CharacterStateProviderFields.IsBoolean(field),
                _ => false
            };
            if (!valid)
                throw new InvalidOperationException($"Character State field '{field}' does not match node '{node.GetType().Name}'.");
        }

        static void ValidateProviderOwner(
            FlowNode node,
            string controlModuleId,
            string inputProviderOwnerId,
            string gameplayProviderOwnerId)
        {
            string owner = node switch
            {
                BtsmtlSkillMoveFacingAngleFlowNode value => value.ProviderOwnerId,
                BtsmtlSkillCharacterStateVector3FlowNode value => value.ProviderOwnerId,
                BtsmtlSkillCharacterStateScalarFlowNode value => value.ProviderOwnerId,
                BtsmtlSkillCharacterStateYawFlowNode value => value.ProviderOwnerId,
                BtsmtlSkillCharacterStateBooleanFlowNode value => value.ProviderOwnerId,
                IBtsmtlSkillInputNode value => value.ProviderOwnerId,
                BtsmtlSkillGameplayTagFlowNode value => value.ProviderOwnerId,
                BtsmtlSkillGameplayTagQueryFlowNode value => value.ProviderOwnerId,
                BtsmtlSkillGameplayAttributeFlowNode value => value.ProviderOwnerId,
                BtsmtlSkillApplyGameplayEffectFlowNode value => value.ProviderOwnerId,
                BtsmtlSkillRemoveGameplayEffectFlowNode value => value.ProviderOwnerId,
                _ => string.Empty
            };
            if (string.IsNullOrEmpty(owner))
                return;
            BtsmtlSkillProviderKind providerKind = BtsmtlSkillProviderContract.Resolve(node);
            if (!BtsmtlSkillProviderContract.Matches(
                    providerKind,
                    owner,
                    controlModuleId,
                    inputProviderOwnerId,
                    gameplayProviderOwnerId))
                throw new InvalidOperationException($"Skill provider owner '{owner}' does not match {providerKind}.");
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
