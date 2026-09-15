using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Timeline;
using FlowCanvas;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonGameplay.Tags;
using ThirdPersonSimulation;
using UnityEngine;
using GameplayEffectDefinition = ThirdPersonGameplay.Effects.GameplayEffectDefinition;
using GameplayEffectRemoveSelector = ThirdPersonGameplay.Effects.GameplayEffectRemoveSelector;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public sealed class BtsmtlSkillFlowLeafEmitter
    {
        readonly SimulationOperationEmitter m_Emitter;
        readonly string m_ControlModuleId;
        readonly string m_InputProviderOwnerId;
        readonly string m_GameplayProviderOwnerId;

        public BtsmtlSkillFlowLeafEmitter(
            GameplayAbilitySemanticBuilder builder,
            string controlModuleId,
            string inputProviderOwnerId,
            string gameplayProviderOwnerId)
        {
            m_Emitter = new SimulationOperationEmitter(builder);
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
            foreach (var field in
                     BtsmtlSkillGraphAuthoringMetadata.ReadFields(node))
                if (!field.IsValid)
                    throw new InvalidOperationException(
                        $"Skill node '{node.UID}' field '{field.Field.FieldId.Value}' is invalid.");
            CharacterSimulationNodeEmission emission = Describe(node);
            BtsmtlSkillNativeNodeCatalog.TryGet(node.GetType(), out BtsmtlSkillNativeNodeContract native);
            OperationValuePortContract contract = GameplayAbilityValuePortContracts.Require(emission.Code);
            if (node.GetInputValuePorts().Count() != contract.Inputs.Count || node.GetOutputValuePorts().Count() != contract.Outputs.Count)
                throw new InvalidOperationException($"Skill node '{node.UID}' does not match its compiled value port shape.");
            var inputs = new List<SimulationConstantInput>();
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
                inputs.Add(new SimulationConstantInput(compiledPort, kind, value, Source(node, graph, route, contentHash, port.ID)));
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
            BtsmtlSkillStateExitCauseFlowNode cause => new CharacterSimulationNodeEmission(SimulationOperationCode.StateExitCause, integer0: (int)Field<BtsmtlSkillStateExitCauseFlowNode, BtsmtlSkillStateExitCause>(cause, "cause")),
            BtsmtlSkillLoopFlowNode loop => new CharacterSimulationNodeEmission(SimulationOperationCode.Loop, integer0: (int)Field<BtsmtlSkillLoopFlowNode, BtsmtlSkillLoopStopType>(loop, "stopType")),
            BtsmtlSkillTimelineHookFlowNode hook => new CharacterSimulationNodeEmission(SimulationOperationCode.TimelineEnter, integer0: (int)hook.Hook),
            BtsmtlSkillTimelineFlowNode timeline => new CharacterSimulationNodeEmission(SimulationOperationCode.Timeline,
                integer0: (int)Field<BtsmtlSkillTimelineFlowNode, TimelinePlaybackMode>(timeline, "playbackMode"), text0: Identity(timeline, "timelineId"),
                constants: CharacterSimulationNodeEmitterRegistry.Fields(("ActionContext", CharacterSimulationNodeEmitterContext.AssetIdentity(Field<BtsmtlSkillTimelineFlowNode, ActionContextSlot>(timeline, "actionContext"))))),
            BtsmtlSkillStateMachineFlowNode machine => new CharacterSimulationNodeEmission(SimulationOperationCode.StateMachine, text0: Identity(machine, "graphId")),
            BtsmtlSkillStateFlowNode state => new CharacterSimulationNodeEmission(SimulationOperationCode.State, text0: Identity(state, "bodyGraphId")),
            BtsmtlSkillSequenceFlowNode => new CharacterSimulationNodeEmission(SimulationOperationCode.Sequence),
            BtsmtlSkillSelectorFlowNode => new CharacterSimulationNodeEmission(SimulationOperationCode.Selector),
            BtsmtlSkillParallelFlowNode parallel => new CharacterSimulationNodeEmission(SimulationOperationCode.Parallel, integer0: (int)Field<BtsmtlSkillParallelFlowNode, BtsmtlSkillParallelMode>(parallel, "mode")),
            BtsmtlSkillBooleanInputFlowNode input => Input(SimulationOperationCode.InputBoolean, Field<BtsmtlSkillBooleanInputFlowNode, string>(input, "inputId"), Field<BtsmtlSkillBooleanInputFlowNode, string>(input, "providerOwnerId")),
            BtsmtlSkillScalarInputFlowNode input => Input(SimulationOperationCode.InputScalar, Field<BtsmtlSkillScalarInputFlowNode, string>(input, "inputId"), Field<BtsmtlSkillScalarInputFlowNode, string>(input, "providerOwnerId")),
            BtsmtlSkillVector2InputFlowNode input => Input(SimulationOperationCode.InputVector2, Field<BtsmtlSkillVector2InputFlowNode, string>(input, "inputId"), Field<BtsmtlSkillVector2InputFlowNode, string>(input, "providerOwnerId")),
            BtsmtlSkillInputMagnitudeFlowNode input => Input(SimulationOperationCode.InputVector2Magnitude, Field<BtsmtlSkillInputMagnitudeFlowNode, string>(input, "inputId"), Field<BtsmtlSkillInputMagnitudeFlowNode, string>(input, "providerOwnerId")),
            BtsmtlSkillActionRequestFlowNode input => Input(SimulationOperationCode.InputRequest, Field<BtsmtlSkillActionRequestFlowNode, string>(input, "inputId"), Field<BtsmtlSkillActionRequestFlowNode, string>(input, "providerOwnerId")),
            IBtsmtlSkillBlackboardReadNode blackboard => new CharacterSimulationNodeEmission(
                SimulationOperationCode.BlackboardGet, text0: Field<IBtsmtlSkillBlackboardReadNode, string>(blackboard, "declarationId"),
                constants: CharacterSimulationNodeEmitterRegistry.Fields(("DeclarationOwner", Field<IBtsmtlSkillBlackboardReadNode, string>(blackboard, "ownerId")))),
            BtsmtlSkillBlackboardAccessFlowNode blackboard => new CharacterSimulationNodeEmission(
                Field<BtsmtlSkillBlackboardAccessFlowNode, string>(blackboard, "accessMode") == "set" ? SimulationOperationCode.BlackboardSet : SimulationOperationCode.BlackboardGet,
                integer0: Field<BtsmtlSkillBlackboardAccessFlowNode, string>(blackboard, "accessMode") == "set" ? 1 : 0,
                text0: Field<BtsmtlSkillBlackboardAccessFlowNode, string>(blackboard, "declarationId"),
                constants: CharacterSimulationNodeEmitterRegistry.Fields(("DeclarationOwner", Field<BtsmtlSkillBlackboardAccessFlowNode, string>(blackboard, "ownerId")),
                    ("FactContext", CharacterSimulationNodeEmitterContext.AssetIdentity(Field<BtsmtlSkillBlackboardAccessFlowNode, UnityEngine.Object>(blackboard, "factContext"))))),
            BtsmtlSkillMoveFacingAngleFlowNode move => new CharacterSimulationNodeEmission(
                SimulationOperationCode.MoveFacingAngle,
                constants: CharacterSimulationNodeEmitterRegistry.Fields(("ProviderOwner", Field<BtsmtlSkillMoveFacingAngleFlowNode, string>(move, "providerOwnerId")))),
            BtsmtlSkillCharacterStateVector3FlowNode state => CharacterState(Field<BtsmtlSkillCharacterStateVector3FlowNode, string>(state, "fieldId"), Field<BtsmtlSkillCharacterStateVector3FlowNode, string>(state, "providerOwnerId")),
            BtsmtlSkillCharacterStateScalarFlowNode state => CharacterState(Field<BtsmtlSkillCharacterStateScalarFlowNode, string>(state, "fieldId"), Field<BtsmtlSkillCharacterStateScalarFlowNode, string>(state, "providerOwnerId")),
            BtsmtlSkillCharacterStateYawFlowNode state => CharacterState(Field<BtsmtlSkillCharacterStateYawFlowNode, string>(state, "fieldId"), Field<BtsmtlSkillCharacterStateYawFlowNode, string>(state, "providerOwnerId")),
            BtsmtlSkillCharacterStateBooleanFlowNode state => CharacterState(Field<BtsmtlSkillCharacterStateBooleanFlowNode, string>(state, "fieldId"), Field<BtsmtlSkillCharacterStateBooleanFlowNode, string>(state, "providerOwnerId")),
            BtsmtlSkillLocomotionFlowNode motion => CharacterSimulationMotionNodeEmitterRegistration.Locomotion(motion, motion.UID),
            BtsmtlSkillActionContextActiveFlowNode context => new CharacterSimulationNodeEmission(
                SimulationOperationCode.ActionContextActive, text0: CharacterSimulationNodeEmitterContext.AssetIdentity(Field<BtsmtlSkillActionContextActiveFlowNode, ActionContextSlot>(context, "actionContext"))),
            BtsmtlSkillActionWindowActiveFlowNode window => new CharacterSimulationNodeEmission(
                SimulationOperationCode.ActionWindowActive, text0: Field<BtsmtlSkillActionWindowActiveFlowNode, string>(window, "windowType")),
            BtsmtlSkillCanActivateActionFlowNode action => CanActivate(action),
            BtsmtlSkillGameplayTagFlowNode tag => new CharacterSimulationNodeEmission(
                SimulationOperationCode.GameplayEffectHasTag,
                text0: TagIdentity(Field<BtsmtlSkillGameplayTagFlowNode, string>(tag, "tagId")),
                constants: CharacterSimulationNodeEmitterRegistry.Fields(("ProviderOwner", Field<BtsmtlSkillGameplayTagFlowNode, string>(tag, "providerOwnerId")))),
            BtsmtlSkillGameplayTagQueryFlowNode query => new CharacterSimulationNodeEmission(
                SimulationOperationCode.GameplayEffectMatchTags,
                constants: QueryFields(Field<BtsmtlSkillGameplayTagQueryFlowNode, GameplayTagQuery>(query, "query"), "Query", Field<BtsmtlSkillGameplayTagQueryFlowNode, string>(query, "providerOwnerId"))),
            BtsmtlSkillGameplayAttributeFlowNode attribute => new CharacterSimulationNodeEmission(
                SimulationOperationCode.GameplayAttributeRead,
                text0: AttributeIdentity(Field<BtsmtlSkillGameplayAttributeFlowNode, string>(attribute, "attributeId")),
                constants: CharacterSimulationNodeEmitterRegistry.Fields(("ProviderOwner", Field<BtsmtlSkillGameplayAttributeFlowNode, string>(attribute, "providerOwnerId")))),
            BtsmtlSkillApplyGameplayEffectFlowNode apply => new CharacterSimulationNodeEmission(
                SimulationOperationCode.GameplayEffectApply,
                text0: EffectIdentity(EffectId(Field<BtsmtlSkillApplyGameplayEffectFlowNode, GameplayEffectDefinition>(apply, "effect"))),
                constants: CharacterSimulationNodeEmitterRegistry.Fields(
                    ("DefinitionRevision", DefinitionRevision(Field<BtsmtlSkillApplyGameplayEffectFlowNode, GameplayEffectDefinition>(apply, "effect"))),
                    ("ActionContext", CharacterSimulationNodeEmitterContext.AssetIdentity(Field<BtsmtlSkillApplyGameplayEffectFlowNode, ActionContextSlot>(apply, "actionContext"))),
                    ("Predicted", Field<BtsmtlSkillApplyGameplayEffectFlowNode, bool>(apply, "predicted")),
                    ("ProviderOwner", Field<BtsmtlSkillApplyGameplayEffectFlowNode, string>(apply, "providerOwnerId")))),
            BtsmtlSkillRemoveGameplayEffectFlowNode remove => new CharacterSimulationNodeEmission(
                SimulationOperationCode.GameplayEffectRemove,
                integer0: (int)Field<BtsmtlSkillRemoveGameplayEffectFlowNode, GameplayEffectRemoveSelector>(remove, "selector"),
                constants: RemoveEffectFields(remove)),
            _ => throw new InvalidOperationException($"Skill node '{node.GetType().Name}' has no leaf emission contract.")
            };
        }

        static TValue Field<TNode, TValue>(TNode node, string fieldId) =>
            (TValue)BtsmtlSkillGraphAuthoringMetadata.ReadField(
                (FlowNode)(object)node,
                fieldId);

        static string Identity(FlowNode node, string fieldId) =>
            BtsmtlSkillGraphAuthoringMetadata.ReadIdentity(node, fieldId);

        static CharacterSimulationNodeEmission CanActivate(
            BtsmtlSkillCanActivateActionFlowNode node)
        {
            GameplayAbilityAdmissionProfile profile = Field<BtsmtlSkillCanActivateActionFlowNode, GameplayAbilityAdmissionProfile>(node, "admissionProfile");
            BtsmtlSkillTargetSnapshotReference snapshot =
                Field<BtsmtlSkillCanActivateActionFlowNode, BtsmtlSkillTargetSnapshotReference>(node, "targetSnapshot");
            return new CharacterSimulationNodeEmission(
                SimulationOperationCode.CanActivateAction,
                text0: profile ? profile.ActionId : string.Empty,
                constants: CharacterSimulationNodeEmitterRegistry.Fields(
                    ("AdmissionProfile", CharacterSimulationNodeEmitterContext.AssetIdentity(profile)),
                    ("TargetSnapshotDeclaration", snapshot.DeclarationId),
                    ("TargetSnapshotOwner", snapshot.OwnerId)));
        }

        static IReadOnlyList<KeyValuePair<string, object>> RemoveEffectFields(BtsmtlSkillRemoveGameplayEffectFlowNode node)
        {
            GameplayEffectDefinition effect =
                Field<BtsmtlSkillRemoveGameplayEffectFlowNode, GameplayEffectDefinition>(node, "effect");
            var fields = new List<KeyValuePair<string, object>>
            {
                new KeyValuePair<string, object>("Handle", Field<BtsmtlSkillRemoveGameplayEffectFlowNode, ulong>(node, "handle")),
                new KeyValuePair<string, object>("Effect", EffectIdentity(EffectId(effect))),
                new KeyValuePair<string, object>("ProviderOwner", Field<BtsmtlSkillRemoveGameplayEffectFlowNode, string>(node, "providerOwnerId"))
            };
            fields.AddRange(QueryFields(
                Field<BtsmtlSkillRemoveGameplayEffectFlowNode, GameplayTagQuery>(node, "query"),
                "Query",
                Field<BtsmtlSkillRemoveGameplayEffectFlowNode, string>(node, "providerOwnerId")));
            return fields;
        }

        static string EffectId(GameplayEffectDefinition effect) =>
            effect ? effect.EffectId.Value : string.Empty;

        static uint DefinitionRevision(GameplayEffectDefinition effect) =>
            effect ? effect.DefinitionRevision : 0U;

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
                BtsmtlSkillCharacterStateVector3FlowNode value => Field<BtsmtlSkillCharacterStateVector3FlowNode, string>(value, "fieldId"),
                BtsmtlSkillCharacterStateScalarFlowNode value => Field<BtsmtlSkillCharacterStateScalarFlowNode, string>(value, "fieldId"),
                BtsmtlSkillCharacterStateYawFlowNode value => Field<BtsmtlSkillCharacterStateYawFlowNode, string>(value, "fieldId"),
                BtsmtlSkillCharacterStateBooleanFlowNode value => Field<BtsmtlSkillCharacterStateBooleanFlowNode, string>(value, "fieldId"),
                _ => string.Empty
            };
            if (string.IsNullOrEmpty(field))
                return;
            string owner = node switch
            {
                BtsmtlSkillCharacterStateVector3FlowNode value => Field<BtsmtlSkillCharacterStateVector3FlowNode, string>(value, "providerOwnerId"),
                BtsmtlSkillCharacterStateScalarFlowNode value => Field<BtsmtlSkillCharacterStateScalarFlowNode, string>(value, "providerOwnerId"),
                BtsmtlSkillCharacterStateYawFlowNode value => Field<BtsmtlSkillCharacterStateYawFlowNode, string>(value, "providerOwnerId"),
                BtsmtlSkillCharacterStateBooleanFlowNode value => Field<BtsmtlSkillCharacterStateBooleanFlowNode, string>(value, "providerOwnerId"),
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
                BtsmtlSkillMoveFacingAngleFlowNode value => Field<BtsmtlSkillMoveFacingAngleFlowNode, string>(value, "providerOwnerId"),
                BtsmtlSkillCharacterStateVector3FlowNode value => Field<BtsmtlSkillCharacterStateVector3FlowNode, string>(value, "providerOwnerId"),
                BtsmtlSkillCharacterStateScalarFlowNode value => Field<BtsmtlSkillCharacterStateScalarFlowNode, string>(value, "providerOwnerId"),
                BtsmtlSkillCharacterStateYawFlowNode value => Field<BtsmtlSkillCharacterStateYawFlowNode, string>(value, "providerOwnerId"),
                BtsmtlSkillCharacterStateBooleanFlowNode value => Field<BtsmtlSkillCharacterStateBooleanFlowNode, string>(value, "providerOwnerId"),
                IBtsmtlSkillInputNode value => Field<IBtsmtlSkillInputNode, string>(value, "providerOwnerId"),
                BtsmtlSkillGameplayTagFlowNode value => Field<BtsmtlSkillGameplayTagFlowNode, string>(value, "providerOwnerId"),
                BtsmtlSkillGameplayTagQueryFlowNode value => Field<BtsmtlSkillGameplayTagQueryFlowNode, string>(value, "providerOwnerId"),
                BtsmtlSkillGameplayAttributeFlowNode value => Field<BtsmtlSkillGameplayAttributeFlowNode, string>(value, "providerOwnerId"),
                BtsmtlSkillApplyGameplayEffectFlowNode value => Field<BtsmtlSkillApplyGameplayEffectFlowNode, string>(value, "providerOwnerId"),
                BtsmtlSkillRemoveGameplayEffectFlowNode value => Field<BtsmtlSkillRemoveGameplayEffectFlowNode, string>(value, "providerOwnerId"),
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
