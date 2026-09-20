using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Timeline;
using FlowCanvas;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCamera;
using ThirdPersonCharacter.Pipeline.Motion;
using ThirdPersonCharacter.Pipeline.Motion.RootMotion;
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
        readonly GameplayAbilitySemanticBuilder m_Builder;
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
            m_Builder = builder;
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
            SimulationNodeEmission emission = Describe(node);
            BtsmtlSkillNativeNodeCatalog.TryGet(node.GetType(), out BtsmtlSkillNativeNodeContract native);
            OperationValuePortContract contract = GameplayAbilityValuePortContracts.Require(emission.Code);
            if (node.GetInputValuePorts().Count() != contract.Inputs.Count || node.GetOutputValuePorts().Count() != contract.Outputs.Count)
                throw new InvalidOperationException($"Skill node '{node.UID}' does not match its compiled value port shape.");
            SimulationSourceLocation sourceLocation = Source(node, graph, route, contentHash, string.Empty);
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
            OperationHandle operation = m_Emitter.Emit(sourceLocation, emission, inputs);
            if (CameraProgramOperationSchema.IsCameraPresentationOperation(emission.Code))
                DeclareCameraProducer(operation, emission.Code, sourceLocation);
            if (emission.Code == SimulationOperationCode.Timeline)
            {
                int producer = m_Builder.DeclareProducer(sourceLocation.DisplayPath, new AnimationChannelId("timeline-progress"),
                    sourceLocation.TemplateIdentity, ProgramOutputChannelKind.Presentation, sourceLocation);
                m_Builder.DeclareReference($"{sourceLocation.Identity}/timeline-progress-producer", operation,
                    ProgramReferenceKind.Producer, producer, sourceLocation.DisplayPath, sourceLocation);
            }
            return operation;
        }

        void DeclareCameraProducer(
            OperationHandle operation,
            SimulationOperationCode code,
            SimulationSourceLocation source)
        {
            string producerIdentity = $"camera:{source.TemplateIdentity}";
            int producer = m_Builder.DeclareProducer(
                producerIdentity,
                CameraProgramOperationSchema.ChannelId,
                source.TemplateIdentity,
                ProgramOutputChannelKind.Presentation,
                source);
            if (producer < 0)
                return;
            m_Builder.DeclareReference(
                $"{source.TemplateIdentity}/camera-producer",
                operation,
                ProgramReferenceKind.Producer,
                producer,
                producerIdentity,
                source);
        }

        static SimulationNodeEmission Describe(FlowNode node)
        {
            if (BtsmtlSkillNativeNodeCatalog.TryGet(node.GetType(), out BtsmtlSkillNativeNodeContract native))
                return new SimulationNodeEmission(native.Code, integer0: native.Variant);
            return node switch
            {
            BtsmtlSkillRootFlowNode => new SimulationNodeEmission(SimulationOperationCode.Root),
            BtsmtlSkillSucceedFlowNode => new SimulationNodeEmission(SimulationOperationCode.Succeed),
            BtsmtlSkillConditionResultFlowNode => new SimulationNodeEmission(SimulationOperationCode.ConditionResult),
            BtsmtlSkillStateEnterFlowNode => new SimulationNodeEmission(SimulationOperationCode.StateEnter),
            BtsmtlSkillStateAnyFlowNode => new SimulationNodeEmission(SimulationOperationCode.StateAny),
            BtsmtlSkillStateExitFlowNode => new SimulationNodeEmission(SimulationOperationCode.StateExit),
            BtsmtlSkillStateOnEnterFlowNode => new SimulationNodeEmission(SimulationOperationCode.StateOnEnter),
            BtsmtlSkillStateOnExitFlowNode => new SimulationNodeEmission(SimulationOperationCode.StateOnExit),
            BtsmtlSkillStateRootCompletedFlowNode => new SimulationNodeEmission(SimulationOperationCode.StateRootCompleted),
            BtsmtlSkillStateExitCauseFlowNode cause => new SimulationNodeEmission(SimulationOperationCode.StateExitCause, integer0: (int)Field<BtsmtlSkillStateExitCauseFlowNode, BtsmtlSkillStateExitCause>(cause, "cause")),
            BtsmtlSkillLoopFlowNode loop => new SimulationNodeEmission(SimulationOperationCode.Loop, integer0: (int)Field<BtsmtlSkillLoopFlowNode, BtsmtlSkillLoopStopType>(loop, "stopType")),
            BtsmtlSkillTimelineExitRequestFlowNode => new SimulationNodeEmission(SimulationOperationCode.TimelineClipExitRequest),
            BtsmtlSkillTimelineHookFlowNode hook => new SimulationNodeEmission(SimulationOperationCode.TimelineEnter, integer0: (int)hook.Hook),
            BtsmtlSkillTimelineFlowNode timeline => new SimulationNodeEmission(SimulationOperationCode.Timeline,
                integer0: (int)Field<BtsmtlSkillTimelineFlowNode, TimelinePlaybackMode>(timeline, "playbackMode"), text0: Identity(timeline, "timelineId"),
                constants: SimulationNodeEmissionFields.Fields(("ActionContext", SimulationAssetIdentity.Of(Field<BtsmtlSkillTimelineFlowNode, ActionContextSlot>(timeline, "actionContext"))))),
            BtsmtlSkillStateMachineFlowNode machine => new SimulationNodeEmission(SimulationOperationCode.StateMachine, text0: Identity(machine, "graphId")),
            BtsmtlSkillStateFlowNode state => new SimulationNodeEmission(SimulationOperationCode.State, text0: Identity(state, "bodyGraphId")),
            BtsmtlSkillSequenceFlowNode => new SimulationNodeEmission(SimulationOperationCode.Sequence),
            BtsmtlSkillSelectorFlowNode => new SimulationNodeEmission(SimulationOperationCode.Selector),
            BtsmtlSkillParallelFlowNode parallel => new SimulationNodeEmission(SimulationOperationCode.Parallel, integer0: (int)Field<BtsmtlSkillParallelFlowNode, BtsmtlSkillParallelMode>(parallel, "mode")),
            BtsmtlSkillBooleanInputFlowNode input => Input(SimulationOperationCode.InputBoolean, Field<BtsmtlSkillBooleanInputFlowNode, string>(input, "inputId"), Field<BtsmtlSkillBooleanInputFlowNode, string>(input, "providerOwnerId")),
            BtsmtlSkillScalarInputFlowNode input => Input(SimulationOperationCode.InputScalar, Field<BtsmtlSkillScalarInputFlowNode, string>(input, "inputId"), Field<BtsmtlSkillScalarInputFlowNode, string>(input, "providerOwnerId")),
            BtsmtlSkillVector2InputFlowNode input => Input(SimulationOperationCode.InputVector2, Field<BtsmtlSkillVector2InputFlowNode, string>(input, "inputId"), Field<BtsmtlSkillVector2InputFlowNode, string>(input, "providerOwnerId")),
            BtsmtlSkillInputMagnitudeFlowNode input => Input(SimulationOperationCode.InputVector2Magnitude, Field<BtsmtlSkillInputMagnitudeFlowNode, string>(input, "inputId"), Field<BtsmtlSkillInputMagnitudeFlowNode, string>(input, "providerOwnerId")),
            BtsmtlSkillActionRequestFlowNode input => Input(SimulationOperationCode.InputRequest, Field<BtsmtlSkillActionRequestFlowNode, string>(input, "inputId"), Field<BtsmtlSkillActionRequestFlowNode, string>(input, "providerOwnerId")),
            IBtsmtlSkillBlackboardReadNode blackboard => new SimulationNodeEmission(
                SimulationOperationCode.BlackboardGet, text0: Field<IBtsmtlSkillBlackboardReadNode, string>(blackboard, "declarationId"),
                constants: SimulationNodeEmissionFields.Fields(("DeclarationOwner", Field<IBtsmtlSkillBlackboardReadNode, string>(blackboard, "ownerId")))),
            BtsmtlSkillBlackboardAccessFlowNode blackboard => new SimulationNodeEmission(
                Field<BtsmtlSkillBlackboardAccessFlowNode, string>(blackboard, "accessMode") == "set" ? SimulationOperationCode.BlackboardSet : SimulationOperationCode.BlackboardGet,
                integer0: Field<BtsmtlSkillBlackboardAccessFlowNode, string>(blackboard, "accessMode") == "set" ? 1 : 0,
                text0: Field<BtsmtlSkillBlackboardAccessFlowNode, string>(blackboard, "declarationId"),
                constants: SimulationNodeEmissionFields.Fields(("DeclarationOwner", Field<BtsmtlSkillBlackboardAccessFlowNode, string>(blackboard, "ownerId")),
                    ("FactContext", SimulationAssetIdentity.Of(Field<BtsmtlSkillBlackboardAccessFlowNode, UnityEngine.Object>(blackboard, "factContext"))))),
            BtsmtlSkillMoveFacingAngleFlowNode move => new SimulationNodeEmission(
                SimulationOperationCode.MoveFacingAngle,
                constants: SimulationNodeEmissionFields.Fields(("ProviderOwner", Field<BtsmtlSkillMoveFacingAngleFlowNode, string>(move, "providerOwnerId")))),
            BtsmtlSkillCharacterStateVector3FlowNode state => CharacterState(Field<BtsmtlSkillCharacterStateVector3FlowNode, string>(state, "fieldId"), Field<BtsmtlSkillCharacterStateVector3FlowNode, string>(state, "providerOwnerId")),
            BtsmtlSkillCharacterStateScalarFlowNode state => CharacterState(Field<BtsmtlSkillCharacterStateScalarFlowNode, string>(state, "fieldId"), Field<BtsmtlSkillCharacterStateScalarFlowNode, string>(state, "providerOwnerId")),
            BtsmtlSkillCharacterStateYawFlowNode state => CharacterState(Field<BtsmtlSkillCharacterStateYawFlowNode, string>(state, "fieldId"), Field<BtsmtlSkillCharacterStateYawFlowNode, string>(state, "providerOwnerId")),
            BtsmtlSkillCharacterStateBooleanFlowNode state => CharacterState(Field<BtsmtlSkillCharacterStateBooleanFlowNode, string>(state, "fieldId"), Field<BtsmtlSkillCharacterStateBooleanFlowNode, string>(state, "providerOwnerId")),
            BtsmtlSkillLocomotionFlowNode motion => Locomotion(motion),
            BtsmtlSkillActionContextActiveFlowNode context => new SimulationNodeEmission(
                SimulationOperationCode.ActionContextActive, text0: SimulationAssetIdentity.Of(Field<BtsmtlSkillActionContextActiveFlowNode, ActionContextSlot>(context, "actionContext"))),
            BtsmtlSkillActionWindowActiveFlowNode window => new SimulationNodeEmission(
                SimulationOperationCode.ActionWindowActive, text0: Field<BtsmtlSkillActionWindowActiveFlowNode, string>(window, "windowType")),
            BtsmtlSkillCanActivateActionFlowNode action => CanActivate(action),
            BtsmtlSkillGameplayTagFlowNode tag => new SimulationNodeEmission(
                SimulationOperationCode.GameplayEffectHasTag,
                text0: TagIdentity(Field<BtsmtlSkillGameplayTagFlowNode, string>(tag, "tagId")),
                constants: SimulationNodeEmissionFields.Fields(("ProviderOwner", Field<BtsmtlSkillGameplayTagFlowNode, string>(tag, "providerOwnerId")))),
            BtsmtlSkillGameplayTagQueryFlowNode query => new SimulationNodeEmission(
                SimulationOperationCode.GameplayEffectMatchTags,
                constants: QueryFields(Field<BtsmtlSkillGameplayTagQueryFlowNode, GameplayTagQuery>(query, "query"), "Query", Field<BtsmtlSkillGameplayTagQueryFlowNode, string>(query, "providerOwnerId"))),
            BtsmtlSkillGameplayAttributeFlowNode attribute => new SimulationNodeEmission(
                SimulationOperationCode.GameplayAttributeRead,
                text0: AttributeIdentity(Field<BtsmtlSkillGameplayAttributeFlowNode, string>(attribute, "attributeId")),
                constants: SimulationNodeEmissionFields.Fields(("ProviderOwner", Field<BtsmtlSkillGameplayAttributeFlowNode, string>(attribute, "providerOwnerId")))),
            BtsmtlSkillApplyGameplayEffectFlowNode apply => new SimulationNodeEmission(
                SimulationOperationCode.GameplayEffectApply,
                text0: EffectIdentity(EffectId(Field<BtsmtlSkillApplyGameplayEffectFlowNode, GameplayEffectDefinition>(apply, "effect"))),
                constants: SimulationNodeEmissionFields.Fields(
                    ("DefinitionRevision", DefinitionRevision(Field<BtsmtlSkillApplyGameplayEffectFlowNode, GameplayEffectDefinition>(apply, "effect"))),
                    ("ActionContext", SimulationAssetIdentity.Of(Field<BtsmtlSkillApplyGameplayEffectFlowNode, ActionContextSlot>(apply, "actionContext"))),
                    ("Predicted", Field<BtsmtlSkillApplyGameplayEffectFlowNode, bool>(apply, "predicted")),
                    ("ProviderOwner", Field<BtsmtlSkillApplyGameplayEffectFlowNode, string>(apply, "providerOwnerId")))),
            BtsmtlSkillRemoveGameplayEffectFlowNode remove => new SimulationNodeEmission(
                SimulationOperationCode.GameplayEffectRemove,
                integer0: (int)Field<BtsmtlSkillRemoveGameplayEffectFlowNode, GameplayEffectRemoveSelector>(remove, "selector"),
                constants: RemoveEffectFields(remove)),
            RequestCameraStateNode request => CameraStateRequest(request),
            RequestCameraEffectNode request => CameraEffectRequest(request),
            SetCameraResponseNode response => CameraResponse(response),
            SetCameraTargetNode target => CameraTarget(target),
            ReadCameraBasisNode => new SimulationNodeEmission(
                SimulationOperationCode.CameraBasisRead,
                integer0: CameraProgramOperationSchema.PayloadVersion),
            _ => throw new InvalidOperationException($"Skill node '{node.GetType().Name}' has no leaf emission contract.")
            };
        }

        static SimulationNodeEmission CameraStateRequest(RequestCameraStateNode node)
        {
            string sequenceId = Field<RequestCameraStateNode, string>(node, "sequenceId");
            if (string.IsNullOrWhiteSpace(sequenceId))
                throw new InvalidOperationException("A camera state request has no sequence identity.");
            return new SimulationNodeEmission(
                SimulationOperationCode.CameraStateRequest,
                integer0: CameraProgramOperationSchema.PayloadVersion,
                integer1: (int)Field<RequestCameraStateNode, CameraMode>(node, "mode"),
                flags: (uint)Field<RequestCameraStateNode, CameraInterruptPolicy>(node, "interruptPolicy"),
                constants: SimulationNodeEmissionFields.Fields(
                    ("Priority", Field<RequestCameraStateNode, int>(node, "priority")),
                    ("Weight", Field<RequestCameraStateNode, float>(node, "weight")),
                    ("SequenceId", sequenceId),
                    ("BlendInSeconds", Field<RequestCameraStateNode, float>(node, "blendInSeconds")),
                    ("BlendOutSeconds", Field<RequestCameraStateNode, float>(node, "blendOutSeconds")),
                    ("TargetKey", Field<RequestCameraStateNode, string>(node, "targetKey")),
                    ("ActionContext", SimulationAssetIdentity.Of(Field<RequestCameraStateNode, ActionContextSlot>(node, "actionContext")))));
        }

        static SimulationNodeEmission CameraEffectRequest(RequestCameraEffectNode node)
        {
            string requestId = Field<RequestCameraEffectNode, string>(node, "requestId");
            string resourceId = Field<RequestCameraEffectNode, string>(node, "resourceId");
            if (string.IsNullOrWhiteSpace(requestId) || string.IsNullOrWhiteSpace(resourceId))
                throw new InvalidOperationException("A camera effect request has no request or resource identity.");
            return new SimulationNodeEmission(
                SimulationOperationCode.CameraEffectRequest,
                integer0: CameraProgramOperationSchema.EffectPayloadVersion,
                integer1: (int)Field<RequestCameraEffectNode, CameraEffectKind>(node, "effectKind"),
                constants: SimulationNodeEmissionFields.Fields(
                    ("RequestId", requestId),
                    ("ResourceId", resourceId),
                    ("Weight", Field<RequestCameraEffectNode, float>(node, "weight")),
                    ("Priority", Field<RequestCameraEffectNode, int>(node, "priority")),
                    ("ActionContext", SimulationAssetIdentity.Of(Field<RequestCameraEffectNode, ActionContextSlot>(node, "actionContext")))));
        }

        static SimulationNodeEmission CameraResponse(SetCameraResponseNode node) =>
            new(
                SimulationOperationCode.CameraResponse,
                integer0: CameraProgramOperationSchema.PayloadVersion,
                integer1: (int)Field<SetCameraResponseNode, CameraLookResponseMode>(node, "lookResponse"),
                constants: SimulationNodeEmissionFields.Fields(
                    ("ManualOrbitWeight", Field<SetCameraResponseNode, float>(node, "manualOrbitWeight")),
                    ("PitchResponseWeight", Field<SetCameraResponseNode, float>(node, "pitchResponseWeight")),
                    ("YawResponseWeight", Field<SetCameraResponseNode, float>(node, "yawResponseWeight")),
                    ("Priority", Field<SetCameraResponseNode, int>(node, "priority")),
                    ("Weight", Field<SetCameraResponseNode, float>(node, "weight")),
                    ("ActionContext", SimulationAssetIdentity.Of(Field<SetCameraResponseNode, ActionContextSlot>(node, "actionContext")))));

        static SimulationNodeEmission CameraTarget(SetCameraTargetNode node)
        {
            string targetKey = Field<SetCameraTargetNode, string>(node, "targetKey");
            string anchorKey = Field<SetCameraTargetNode, string>(node, "anchorKey");
            string aimPointKey = Field<SetCameraTargetNode, string>(node, "aimPointKey");
            string preferredBoneKey = Field<SetCameraTargetNode, string>(node, "preferredBoneKey");
            int targetMask = (string.IsNullOrEmpty(targetKey) ? 0 : CameraProgramOperationSchema.TargetKeyMask) |
                             (string.IsNullOrEmpty(anchorKey) ? 0 : CameraProgramOperationSchema.AnchorKeyMask) |
                             (string.IsNullOrEmpty(aimPointKey) ? 0 : CameraProgramOperationSchema.AimPointKeyMask) |
                             (string.IsNullOrEmpty(preferredBoneKey) ? 0 : CameraProgramOperationSchema.PreferredBoneKeyMask);
            if (targetMask == 0)
                throw new InvalidOperationException("A camera target request has no target identity.");
            return new SimulationNodeEmission(
                SimulationOperationCode.CameraTarget,
                integer0: CameraProgramOperationSchema.PayloadVersion,
                integer1: targetMask,
                constants: SimulationNodeEmissionFields.Fields(
                    ("TargetKey", targetKey),
                    ("AnchorKey", anchorKey),
                    ("AimPointKey", aimPointKey),
                    ("PreferredBoneKey", preferredBoneKey),
                    ("Priority", Field<SetCameraTargetNode, int>(node, "priority")),
                    ("Weight", Field<SetCameraTargetNode, float>(node, "weight")),
                    ("ActionContext", SimulationAssetIdentity.Of(Field<SetCameraTargetNode, ActionContextSlot>(node, "actionContext")))));
        }

        static SimulationNodeEmission Locomotion(BtsmtlSkillLocomotionFlowNode node)
        {
            LocomotionInputMotionAuthoringRules.Validate(node);
            LocomotionInputMotionDisplacementMode displacement = node.DisplacementMode;
            var constants = new List<KeyValuePair<string, object>>
            {
                new("TurnSpeedDegrees", node.TurnSpeedDegrees),
                new("DurationSeconds", node.DurationSeconds)
            };
            if (displacement == LocomotionInputMotionDisplacementMode.ConstantSpeed)
            {
                constants.Add(new KeyValuePair<string, object>("MoveSpeed", node.MoveSpeed));
            }
            else
            {
                RootMotionCurveAsset curve = node.ActionMotionCurve;
                constants.Add(new KeyValuePair<string, object>("ActionMotionPositionX", BakeCurve(curve.LocalPositionX, $"{node.UID}/ActionMotionPositionX")));
                constants.Add(new KeyValuePair<string, object>("ActionMotionPositionZ", BakeCurve(curve.LocalPositionZ, $"{node.UID}/ActionMotionPositionZ")));
                constants.Add(new KeyValuePair<string, object>("ActionMotionDuration", curve.Duration));
            }

            return new SimulationNodeEmission(
                SimulationOperationCode.LocomotionInputMotion,
                integer0: (int)node.ExecutionMode,
                integer1: (int)displacement,
                flags: node.CameraRelative ? 1U : 0U,
                constants: constants);
        }

        static SemanticDataDocument BakeCurve(AnimationCurve curve, string identity)
        {
            if (curve == null || curve.length == 0)
                throw new InvalidOperationException($"Curve '{identity}' is empty.");
            var writer = new SemanticDataWriter();
            writer.WriteUInt32(0x56525543);
            writer.WriteInt32(1);
            writer.WriteInt32((int)curve.preWrapMode);
            writer.WriteInt32((int)curve.postWrapMode);
            writer.WriteInt32(curve.length);
            for (int i = 0; i < curve.length; i++)
            {
                Keyframe key = curve.keys[i];
                if (key.weightedMode != WeightedMode.None)
                    throw new InvalidOperationException($"Curve '{identity}' key #{i} uses unsupported weighted tangents.");
                writer.WriteNumber(key.time, $"{identity}[{i}].time");
                writer.WriteNumber(key.value, $"{identity}[{i}].value");
                writer.WriteNumber(key.inTangent, $"{identity}[{i}].inTangent");
                writer.WriteNumber(key.outTangent, $"{identity}[{i}].outTangent");
                writer.WriteInt32((int)key.weightedMode);
            }

            return writer.Build();
        }

        static TValue Field<TNode, TValue>(TNode node, string fieldId) =>
            (TValue)BtsmtlSkillGraphAuthoringMetadata.ReadField(
                (FlowNode)(object)node,
                fieldId);

        static string Identity(FlowNode node, string fieldId) =>
            BtsmtlSkillGraphAuthoringMetadata.ReadIdentity(node, fieldId);

        static SimulationNodeEmission CanActivate(
            BtsmtlSkillCanActivateActionFlowNode node)
        {
            GameplayAbilityAdmissionProfile profile = Field<BtsmtlSkillCanActivateActionFlowNode, GameplayAbilityAdmissionProfile>(node, "admissionProfile");
            BtsmtlSkillTargetSnapshotReference snapshot =
                Field<BtsmtlSkillCanActivateActionFlowNode, BtsmtlSkillTargetSnapshotReference>(node, "targetSnapshot");
            return new SimulationNodeEmission(
                SimulationOperationCode.CanActivateAction,
                text0: profile ? profile.ActionId : string.Empty,
                constants: SimulationNodeEmissionFields.Fields(
                    ("AdmissionProfile", SimulationAssetIdentity.Of(profile)),
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

        static SimulationNodeEmission Input(SimulationOperationCode code, string identity, string providerOwnerId)
        {
            if (string.IsNullOrWhiteSpace(identity) || string.IsNullOrWhiteSpace(providerOwnerId))
                throw new InvalidOperationException("A skill input node has no declared input identity or provider owner.");
            return new SimulationNodeEmission(
                code,
                text0: identity,
                constants: SimulationNodeEmissionFields.Fields(("ProviderOwner", providerOwnerId)));
        }

        static SimulationNodeEmission CharacterState(string fieldId, string providerOwnerId)
        {
            if (!CharacterStateProviderFields.IsValid(fieldId) || !CharacterStateProviderFields.IsOwner(providerOwnerId))
                throw new InvalidOperationException("Character State provider reference is incomplete.");
            return new SimulationNodeEmission(
                SimulationOperationCode.CharacterStateRead,
                text0: fieldId,
                constants: SimulationNodeEmissionFields.Fields(("ProviderOwner", providerOwnerId)));
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

        static SimulationSourceLocation Source(FlowNode node, IBtsmtlSkillFlowGraph graph,
            string route, string contentHash, string portId) => new SimulationSourceLocation(
                node.GetType().FullName, graph.AuthoringId, node.UID, string.Empty, string.Empty, string.Empty,
                string.IsNullOrEmpty(portId) ? $"{route}/node:{node.UID}" : $"{route}/node:{node.UID}/port:{portId}",
                portId: portId, contentHash: contentHash);
    }
}
