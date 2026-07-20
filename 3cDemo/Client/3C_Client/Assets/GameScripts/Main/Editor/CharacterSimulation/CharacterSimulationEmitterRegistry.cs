using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonCharacter.Pipeline.Motion;
using ThirdPersonGameplay.Tags;
using ThirdPersonSimulation;
using TreeDesigner;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public readonly struct CharacterSimulationNodeEmission
    {
        public CharacterSimulationNodeEmission(SimulationOperationCode code, int integer0 = 0, int integer1 = 0, ulong unsigned0 = 0, string text0 = null, uint flags = 0, IEnumerable<KeyValuePair<string, object>> constants = null)
        {
            Code = code;
            Integer0 = integer0;
            Integer1 = integer1;
            Unsigned0 = unsigned0;
            Text0 = text0 ?? string.Empty;
            Flags = flags;
            Constants = constants == null ? Array.Empty<KeyValuePair<string, object>>() : constants.ToArray();
        }
        public SimulationOperationCode Code { get; }
        public int Integer0 { get; }
        public int Integer1 { get; }
        public ulong Unsigned0 { get; }
        public string Text0 { get; }
        public uint Flags { get; }
        public IReadOnlyList<KeyValuePair<string, object>> Constants { get; }
    }

    public interface ICharacterSimulationNodeEmitter
    {
        Type SourceType { get; }
        OperationHandle Emit(BaseNode node, CharacterSimulationNodeEmitterContext context);
    }

    public sealed class CharacterSimulationNodeEmitterContext
    {
        readonly BaseGraph m_Graph;
        readonly string m_Route;
        readonly CharacterSimulationProgramBuilder m_Builder;

        public CharacterSimulationNodeEmitterContext(BaseGraph graph, string route, CharacterSimulationProgramBuilder builder)
        {
            m_Graph = graph ?? throw new ArgumentNullException(nameof(graph));
            m_Route = route ?? string.Empty;
            m_Builder = builder ?? throw new ArgumentNullException(nameof(builder));
        }

        public CharacterSimulationProgramBuilder Builder => m_Builder;

        public OperationHandle Emit(BaseNode node, CharacterSimulationNodeEmission emission)
        {
            return Emit(node, emission, string.Empty);
        }

        public OperationHandle Emit(BaseNode node, CharacterSimulationNodeEmission emission, string portId)
        {
            CharacterSimulationSourceLocation source = Source(node, portId);
            var constants = new List<int>();
            List<CapturedValuePort> valuePorts = CaptureValuePorts(node, emission.Code);
            var constantInputs = new List<CapturedConstantInput>();
            CaptureUnconnectedInputConstants(node, valuePorts, constants, constantInputs);
            for (int i = 0; i < emission.Constants.Count; i++)
            {
                KeyValuePair<string, object> pair = emission.Constants[i];
                int constant = m_Builder.DeclareConstant(source, pair.Key, pair.Value);
                if (constant >= 0)
                    constants.Add(constant);
            }
            OperationHandle operation = m_Builder.DeclareOperation(
                source,
                emission.Code,
                constants,
                emission.Integer0,
                emission.Integer1,
                emission.Unsigned0,
                default,
                emission.Text0,
                emission.Flags);
            for (int i = 0; i < constantInputs.Count; i++)
            {
                CapturedConstantInput input = constantInputs[i];
                m_Builder.DeclareConstantInputBinding(
                    operation,
                    input.PortId,
                    input.ConstantIndex,
                    input.Kind,
                    input.Source);
            }
            return operation;
        }

        public CharacterSimulationSourceLocation Source(BaseNode node, string portId = "")
        {
            string sourcePort = portId ?? string.Empty;
            return new CharacterSimulationSourceLocation(
                node.GetType().FullName,
                m_Graph.GraphAuthoringId,
                node.GUID,
                string.Empty,
                string.Empty,
                string.Empty,
                sourcePort.Length == 0
                    ? $"{m_Route}/node:{node.GUID}"
                    : $"{m_Route}/node:{node.GUID}/port:{sourcePort}",
                portId: sourcePort);
        }

        public void RecordOutputPorts(BaseNode node, OperationHandle operation, params string[] portIds)
        {
            if (portIds == null)
                throw new ArgumentNullException(nameof(portIds));
            for (int i = 0; i < portIds.Length; i++)
                m_Builder.DeclareOperationPortSource(operation, Source(node, portIds[i]));
        }

        public static string AssetIdentity(UnityEngine.Object asset)
        {
            if (!asset)
                return string.Empty;
            string path = AssetDatabase.GetAssetPath(asset);
            string guid = string.IsNullOrEmpty(path) ? string.Empty : AssetDatabase.AssetPathToGUID(path);
            return string.IsNullOrEmpty(guid) ? string.Empty : $"asset:{guid}";
        }

        List<CapturedValuePort> CaptureValuePorts(BaseNode node, SimulationOperationCode code)
        {
            OperationValuePortContract contract = CharacterGameplayValuePortContracts.Require(code);
            var result = new List<CapturedValuePort>();
            List<NodeFieldAccessor> accessors = node.GetFieldAccessors().OrderBy(value => value.FieldKey, StringComparer.Ordinal).ToList();
            for (int i = 0; i < accessors.Count; i++)
            {
                NodeFieldAccessor accessor = accessors[i];
                if (!accessor.TryGetPropertyPort(out PropertyPort port) || port == null)
                    continue;
                string portId = string.IsNullOrEmpty(port.PortId) ? accessor.FieldKey : port.PortId;
                if (port.Direction == PortDirection.Output)
                {
                    if (IsPropertyOutputLinked(node.GUID, portId, accessor.FieldKey))
                        contract.RequireOutput(portId);
                    continue;
                }
                OperationValuePortDefinition definition = contract.RequireInput(portId);
                if (IsPropertyInputLinked(node.GUID, portId, accessor.FieldKey))
                    continue;
                SemanticValueKind kind = ResolvePortKind(port, node, portId);
                if (!definition.Accepts(kind))
                    throw new InvalidOperationException($"Node '{node.GetType().Name}' port '{portId}' kind '{kind}' violates operation '{code}' contract '{definition.Constraint}'.");
                result.Add(new CapturedValuePort(accessor.FieldKey, portId, port.Direction, kind, port));
            }
            return result;
        }

        void CaptureUnconnectedInputConstants(
            BaseNode node,
            IReadOnlyList<CapturedValuePort> ports,
            List<int> constants,
            List<CapturedConstantInput> constantInputs)
        {
            for (int i = 0; i < ports.Count; i++)
            {
                CapturedValuePort captured = ports[i];
                if (captured.Direction != PortDirection.Input)
                    continue;
                string portId = captured.PortId;
                if (IsPropertyInputLinked(node.GUID, portId, captured.FieldKey))
                    continue;
                CharacterSimulationSourceLocation source = Source(node, portId);
                int constant = m_Builder.DeclareConstant(source, "default-value", captured.Port.GetValue());
                if (constant >= 0)
                {
                    constants.Add(constant);
                    constantInputs.Add(new CapturedConstantInput(portId, constant, captured.Kind, source));
                }
            }
        }

        static SemanticValueKind ResolvePortKind(PropertyPort port, BaseNode node, string portId)
        {
            object value = port.GetValue();
            return value switch
            {
                bool => SemanticValueKind.Boolean,
                int => SemanticValueKind.Int32,
                uint => SemanticValueKind.UInt64,
                ulong => SemanticValueKind.UInt64,
                float => SemanticValueKind.Number,
                double => SemanticValueKind.Number,
                Vector2 => SemanticValueKind.Vector2,
                Vector3 => SemanticValueKind.Vector3,
                string => SemanticValueKind.Identity,
                Enum => SemanticValueKind.Int32,
                _ => throw new InvalidOperationException($"Node '{node.GetType().Name}' Value port '{portId}' uses unsupported property type '{value?.GetType().FullName ?? port.GetType().FullName}'.")
            };
        }

        bool IsPropertyInputLinked(string nodeId, string portId, string fieldKey)
        {
            for (int i = 0; i < m_Graph.PropertyEdges.Count; i++)
            {
                PropertyEdge edge = m_Graph.PropertyEdges[i];
                if (edge != null &&
                    string.Equals(edge.EndNodeGUID, nodeId, StringComparison.Ordinal) &&
                    (string.Equals(edge.EndPortName, portId, StringComparison.Ordinal) || string.Equals(edge.EndPortName, fieldKey, StringComparison.Ordinal)))
                    return true;
            }
            return false;
        }

        bool IsPropertyOutputLinked(string nodeId, string portId, string fieldKey)
        {
            for (int i = 0; i < m_Graph.PropertyEdges.Count; i++)
            {
                PropertyEdge edge = m_Graph.PropertyEdges[i];
                if (edge != null &&
                    string.Equals(edge.StartNodeGUID, nodeId, StringComparison.Ordinal) &&
                    (string.Equals(edge.StartPortName, portId, StringComparison.Ordinal) || string.Equals(edge.StartPortName, fieldKey, StringComparison.Ordinal)))
                    return true;
            }
            return false;
        }

        readonly struct CapturedValuePort
        {
            public CapturedValuePort(string fieldKey, string portId, PortDirection direction, SemanticValueKind kind, PropertyPort port)
            {
                FieldKey = fieldKey;
                PortId = portId;
                Direction = direction;
                Kind = kind;
                Port = port;
            }

            public string FieldKey { get; }
            public string PortId { get; }
            public PortDirection Direction { get; }
            public SemanticValueKind Kind { get; }
            public PropertyPort Port { get; }
        }

        readonly struct CapturedConstantInput
        {
            public CapturedConstantInput(string portId, int constantIndex, SemanticValueKind kind, CharacterSimulationSourceLocation source)
            {
                PortId = portId;
                ConstantIndex = constantIndex;
                Kind = kind;
                Source = source;
            }

            public string PortId { get; }
            public int ConstantIndex { get; }
            public SemanticValueKind Kind { get; }
            public CharacterSimulationSourceLocation Source { get; }
        }
    }

    public sealed class CharacterSimulationNodeEmitterRegistry
    {
        readonly Dictionary<Type, ICharacterSimulationNodeEmitter> m_Emitters = new Dictionary<Type, ICharacterSimulationNodeEmitter>();

        public void Register(ICharacterSimulationNodeEmitter emitter)
        {
            if (emitter == null)
                throw new ArgumentNullException(nameof(emitter));
            if (!m_Emitters.TryAdd(emitter.SourceType, emitter))
                throw new InvalidOperationException($"Emitter for '{emitter.SourceType.FullName}' is already registered.");
        }

        public bool TryGet(Type sourceType, out ICharacterSimulationNodeEmitter emitter)
        {
            return m_Emitters.TryGetValue(sourceType, out emitter);
        }

        public static CharacterSimulationNodeEmitterRegistry CreateDefault()
        {
            var registry = new CharacterSimulationNodeEmitterRegistry();
            registry.Register(Simple<RootNode>(node => new CharacterSimulationNodeEmission(SimulationOperationCode.Root)));
            registry.Register(Simple<LoopNode>(node => new CharacterSimulationNodeEmission(SimulationOperationCode.Loop, integer0: (int)node.LoopStopType)));
            registry.Register(Simple<ParallelNode>(node => new CharacterSimulationNodeEmission(SimulationOperationCode.Parallel, integer0: (int)node.Mode)));
            registry.Register(Simple<SequenceNode>(node => new CharacterSimulationNodeEmission(SimulationOperationCode.Sequence)));
            registry.Register(Simple<SelectorNode>(node => new CharacterSimulationNodeEmission(SimulationOperationCode.Selector)));
            registry.Register(Simple<SucceedNode>(node => new CharacterSimulationNodeEmission(SimulationOperationCode.Succeed)));
            registry.Register(Simple<StateMachineNode>(node => new CharacterSimulationNodeEmission(SimulationOperationCode.StateMachine, text0: node.Graph?.GraphAuthoringId)));
            registry.Register(Simple<StateNode>(node => new CharacterSimulationNodeEmission(SimulationOperationCode.State, text0: node.SubTree?.GraphAuthoringId)));
            registry.Register(Simple<StateMachineEnterNode>(node => new CharacterSimulationNodeEmission(SimulationOperationCode.StateEnter)));
            registry.Register(Simple<StateMachineAnyStateNode>(node => new CharacterSimulationNodeEmission(SimulationOperationCode.StateAny)));
            registry.Register(Simple<StateMachineExitNode>(node => new CharacterSimulationNodeEmission(SimulationOperationCode.StateExit)));
            registry.Register(Simple<StateOnEnterNode>(node => new CharacterSimulationNodeEmission(SimulationOperationCode.StateOnEnter)));
            registry.Register(Simple<StateOnExitNode>(node => new CharacterSimulationNodeEmission(SimulationOperationCode.StateOnExit)));
            registry.Register(Simple<StateRootCompletedNode>(node => new CharacterSimulationNodeEmission(SimulationOperationCode.StateRootCompleted)));
            registry.Register(Simple<StateExitCauseInfoNode>(node => new CharacterSimulationNodeEmission(SimulationOperationCode.StateExitCause, integer0: (int)node.Cause)));
            registry.Register(Simple<TimelineEnterNode>(node => new CharacterSimulationNodeEmission(SimulationOperationCode.TimelineEnter, integer0: (int)node.EnterType)));
            registry.Register(Simple<TimelineNode>(node => new CharacterSimulationNodeEmission(
                SimulationOperationCode.Timeline,
                integer0: (int)node.PlaybackMode,
                text0: node.Timeline?.AuthoringId,
                constants: Fields(("ActionContext", AssetIdentity(node.ActionContext))))));
            registry.Register(Simple<ExposedPropertyNode>(node => new CharacterSimulationNodeEmission(
                node.NodeType == ExposedPropertyNodeType.Get ? SimulationOperationCode.BlackboardGet : SimulationOperationCode.BlackboardSet,
                integer0: (int)node.NodeType,
                text0: node.BlackboardVariable.DeclarationId,
                constants: Fields(
                    ("DeclarationOwner", node.BlackboardVariable.DeclarationOwnerId),
                    ("FactContext", AssetIdentity(node.FactContext))))));
            registry.Register(Simple<CharacterInputBoolInfoNode>(node => new CharacterSimulationNodeEmission(SimulationOperationCode.InputBoolean, text0: node.InputValueId)));
            registry.Register(Simple<CharacterInputFloatInfoNode>(node => new CharacterSimulationNodeEmission(SimulationOperationCode.InputScalar, text0: node.InputValueId)));
            registry.Register(Simple<CharacterInputVector2InfoNode>(node => new CharacterSimulationNodeEmission(SimulationOperationCode.InputVector2, text0: node.InputValueId)));
            registry.Register(Simple<CharacterInputVector2MagnitudeInfoNode>(node => new CharacterSimulationNodeEmission(SimulationOperationCode.InputVector2Magnitude, text0: node.InputValueId)));
            registry.Register(Simple<CharacterActionRequestInfoNode>(node => new CharacterSimulationNodeEmission(SimulationOperationCode.InputRequest, text0: node.RequestId)));
            registry.Register(Simple<PipelineBlackboardBoolInfoNode>(node => BlackboardRead(node)));
            registry.Register(Simple<PipelineBlackboardIntInfoNode>(node => BlackboardRead(node)));
            registry.Register(Simple<PipelineBlackboardFloatInfoNode>(node => BlackboardRead(node)));
            registry.Register(Simple<PipelineBlackboardStringInfoNode>(node => BlackboardRead(node)));
            registry.Register(Simple<PipelineBlackboardVector2InfoNode>(node => BlackboardRead(node)));
            registry.Register(Simple<PipelineBlackboardVector3InfoNode>(node => BlackboardRead(node)));
            registry.Register(Simple<CharacterMoveFacingAngleInfoNode>(node => new CharacterSimulationNodeEmission(SimulationOperationCode.MoveFacingAngle)));
            registry.Register(Simple<ActionContextActiveInfoNode>(node => new CharacterSimulationNodeEmission(
                SimulationOperationCode.ActionContextActive,
                text0: AssetIdentity(node.ActionContext))));
            registry.Register(Simple<ActionWindowActiveInfoNode>(node => new CharacterSimulationNodeEmission(
                SimulationOperationCode.ActionWindowActive,
                text0: node.WindowType)));
            registry.Register(Simple<CanActivateActionInfoNode>(node => new CharacterSimulationNodeEmission(
                SimulationOperationCode.CanActivateAction,
                text0: node.ActionProfile ? node.ActionProfile.ActionId : string.Empty,
                constants: Fields(
                    ("ActionProfile", AssetIdentity(node.ActionProfile)),
                    ("TargetSnapshotDeclaration", node.TargetSnapshotVariable.DeclarationId),
                    ("TargetSnapshotOwner", node.TargetSnapshotVariable.DeclarationOwnerId)))));
            registry.Register(Simple<ActivateActionInstanceNode>(node => new CharacterSimulationNodeEmission(
                SimulationOperationCode.ActivateActionInstance,
                text0: node.ActionProfile ? node.ActionProfile.ActionId : string.Empty,
                constants: Fields(
                    ("ActionProfile", AssetIdentity(node.ActionProfile)),
                    ("SourceInputRequest", node.SourceInputRequestId),
                    ("ConsumeSourceInputRequest", node.ConsumeSourceInputRequest),
                    ("TargetKey", node.TargetKey),
                    ("TargetSnapshotDeclaration", node.TargetSnapshotVariable.DeclarationId),
                    ("TargetSnapshotOwner", node.TargetSnapshotVariable.DeclarationOwnerId),
                    ("ActionContext", AssetIdentity(node.ActionContext))))));
            registry.Register(Simple<SubmitActionLifecycleTransitionNode>(node => new CharacterSimulationNodeEmission(
                SimulationOperationCode.SubmitActionLifecycle,
                integer0: (int)node.TransitionType,
                text0: node.Reason,
                constants: Fields(("ActionContext", AssetIdentity(node.ActionContext))))));
            registry.Register(Camera<RequestCameraStateNode>(RequestCameraState));
            registry.Register(Camera<EmitCameraCueNode>(EmitCameraCue));
            registry.Register(Camera<SetCameraResponseNode>(SetCameraResponse));
            registry.Register(Camera<SetCameraTargetNode>(SetCameraTarget));
            registry.Register(new CameraBasisCharacterSimulationNodeEmitter());
            registry.Register(Simple<HasGameplayTagNode>(node => new CharacterSimulationNodeEmission(
                SimulationOperationCode.GameplayEffectHasTag,
                text0: TagIdentity(node.Tag.Value))));
            registry.Register(Simple<MatchGameplayTagQueryNode>(node => new CharacterSimulationNodeEmission(
                SimulationOperationCode.GameplayEffectMatchTags,
                constants: QueryFields(node.Query, "Query"))));
            registry.Register(Simple<ReadGameplayAttributeNode>(node => new CharacterSimulationNodeEmission(
                SimulationOperationCode.GameplayAttributeRead,
                text0: AttributeIdentity(node.Attribute.Value))));
            registry.Register(Simple<ApplyGameplayEffectNode>(ApplyGameplayEffect));
            registry.Register(Simple<RemoveGameplayEffectNode>(RemoveGameplayEffect));
            registry.Register(Simple<LocomotionInputMotionNode>(node => new CharacterSimulationNodeEmission(
                SimulationOperationCode.LocomotionInputMotion,
                flags: (node.CameraRelative ? 1U : 0U) | (node.Continuous ? 2U : 0U),
                constants: Fields(("MoveSpeed", node.MoveSpeed), ("TurnSpeedDegrees", node.TurnSpeedDegrees)))));
            registry.Register(Simple<ConditionRuleResultNode>(node => new CharacterSimulationNodeEmission(SimulationOperationCode.ConditionResult)));
            registry.Register(Simple<CompareNode>(node => new CharacterSimulationNodeEmission(SimulationOperationCode.Compare, integer0: (int)node.Comparison)));
            registry.Register(Simple<AndNode>(node => new CharacterSimulationNodeEmission(SimulationOperationCode.And)));
            registry.Register(Simple<OrNode>(node => new CharacterSimulationNodeEmission(SimulationOperationCode.Or)));
            registry.Register(Simple<NotNode>(node => new CharacterSimulationNodeEmission(SimulationOperationCode.Not)));
            return registry;
        }

        static CharacterSimulationNodeEmission BlackboardRead(PipelineBlackboardValueInfoNode node)
        {
            return new CharacterSimulationNodeEmission(
                SimulationOperationCode.BlackboardGet,
                text0: node.BlackboardVariable.DeclarationId,
                constants: Fields(("DeclarationOwner", node.BlackboardVariable.DeclarationOwnerId)));
        }

        static CharacterSimulationNodeEmission ApplyGameplayEffect(ApplyGameplayEffectNode node)
        {
            var constants = new List<KeyValuePair<string, object>>
            {
                new KeyValuePair<string, object>("DefinitionRevision", node.Effect ? node.Effect.DefinitionRevision : 0U),
                new KeyValuePair<string, object>("ActionContext", AssetIdentity(node.ActionContext)),
                new KeyValuePair<string, object>("Predicted", node.Predicted)
            };
            for (int i = 0; i < node.SetByCallerValues.Count; i++)
            {
                string parameterId = node.SetByCallerValues[i].ParameterId;
                constants.Add(new KeyValuePair<string, object>($"SetByCaller:{parameterId}", node.SetByCallerValues[i].Value));
            }
            return new CharacterSimulationNodeEmission(
                SimulationOperationCode.GameplayEffectApply,
                text0: EffectIdentity(node.Effect ? node.Effect.EffectId.Value : string.Empty),
                constants: constants);
        }

        static CharacterSimulationNodeEmission RemoveGameplayEffect(RemoveGameplayEffectNode node)
        {
            var constants = new List<KeyValuePair<string, object>>
            {
                new KeyValuePair<string, object>("Handle", node.Handle),
                new KeyValuePair<string, object>("Effect", EffectIdentity(node.Effect ? node.Effect.EffectId.Value : string.Empty))
            };
            constants.AddRange(QueryFields(node.EffectTagQuery, "Query"));
            return new CharacterSimulationNodeEmission(
                SimulationOperationCode.GameplayEffectRemove,
                integer0: (int)node.Selector,
                constants: constants);
        }

        static CharacterSimulationNodeEmission RequestCameraState(RequestCameraStateNode node)
        {
            RequireDefined(node.Mode, nameof(node.Mode));
            RequireDefined(node.InterruptPolicy, nameof(node.InterruptPolicy));
            RequireUnit(node.Weight, nameof(node.Weight));
            RequireNonNegative(node.BlendInSeconds, nameof(node.BlendInSeconds));
            RequireNonNegative(node.BlendOutSeconds, nameof(node.BlendOutSeconds));
            RequireOptionalIdentity(node.TargetKey, nameof(node.TargetKey));
            return new CharacterSimulationNodeEmission(
                SimulationOperationCode.CameraStateRequest,
                integer0: CameraProgramOperationSchema.PayloadVersion,
                integer1: (int)node.Mode,
                flags: (uint)node.InterruptPolicy,
                constants: Fields(
                    ("Priority", node.Priority),
                    ("Weight", node.Weight),
                    ("BlendInSeconds", node.BlendInSeconds),
                    ("BlendOutSeconds", node.BlendOutSeconds),
                    ("TargetKey", node.TargetKey),
                    ("ActionContext", AssetIdentity(node.ActionContext))));
        }

        static CharacterSimulationNodeEmission EmitCameraCue(EmitCameraCueNode node)
        {
            RequireIdentity(node.CueId, nameof(node.CueId));
            RequireDefined(node.CueKind, nameof(node.CueKind));
            RequireIdentity(node.CueType, nameof(node.CueType));
            RequireNonNegative(node.Intensity, nameof(node.Intensity));
            RequireNonNegative(node.DurationSeconds, nameof(node.DurationSeconds));
            return new CharacterSimulationNodeEmission(
                SimulationOperationCode.CameraCue,
                integer0: CameraProgramOperationSchema.PayloadVersion,
                integer1: (int)node.CueKind,
                constants: Fields(
                    ("CueId", node.CueId),
                    ("CueType", node.CueType),
                    ("Intensity", node.Intensity),
                    ("DurationSeconds", node.DurationSeconds),
                    ("Priority", node.Priority),
                    ("ActionContext", AssetIdentity(node.ActionContext))));
        }

        static CharacterSimulationNodeEmission SetCameraResponse(SetCameraResponseNode node)
        {
            RequireDefined(node.LookResponse, nameof(node.LookResponse));
            RequireUnit(node.ManualOrbitWeight, nameof(node.ManualOrbitWeight));
            RequireUnit(node.PitchResponseWeight, nameof(node.PitchResponseWeight));
            RequireUnit(node.YawResponseWeight, nameof(node.YawResponseWeight));
            RequireUnit(node.Weight, nameof(node.Weight));
            return new CharacterSimulationNodeEmission(
                SimulationOperationCode.CameraResponse,
                integer0: CameraProgramOperationSchema.PayloadVersion,
                integer1: (int)node.LookResponse,
                constants: Fields(
                    ("ManualOrbitWeight", node.ManualOrbitWeight),
                    ("PitchResponseWeight", node.PitchResponseWeight),
                    ("YawResponseWeight", node.YawResponseWeight),
                    ("Priority", node.Priority),
                    ("Weight", node.Weight),
                    ("ActionContext", AssetIdentity(node.ActionContext))));
        }

        static CharacterSimulationNodeEmission SetCameraTarget(SetCameraTargetNode node)
        {
            RequireOptionalIdentity(node.TargetKey, nameof(node.TargetKey));
            RequireOptionalIdentity(node.AnchorKey, nameof(node.AnchorKey));
            RequireOptionalIdentity(node.AimPointKey, nameof(node.AimPointKey));
            RequireOptionalIdentity(node.PreferredBoneKey, nameof(node.PreferredBoneKey));
            RequireUnit(node.Weight, nameof(node.Weight));
            int targetMask = (string.IsNullOrEmpty(node.TargetKey) ? 0 : CameraProgramOperationSchema.TargetKeyMask) |
                             (string.IsNullOrEmpty(node.AnchorKey) ? 0 : CameraProgramOperationSchema.AnchorKeyMask) |
                             (string.IsNullOrEmpty(node.AimPointKey) ? 0 : CameraProgramOperationSchema.AimPointKeyMask) |
                             (string.IsNullOrEmpty(node.PreferredBoneKey) ? 0 : CameraProgramOperationSchema.PreferredBoneKeyMask);
            if (targetMask == 0)
                throw new InvalidOperationException("SetCameraTarget requires at least one formal target identity.");
            return new CharacterSimulationNodeEmission(
                SimulationOperationCode.CameraTarget,
                integer0: CameraProgramOperationSchema.PayloadVersion,
                integer1: targetMask,
                constants: Fields(
                    ("TargetKey", node.TargetKey),
                    ("AnchorKey", node.AnchorKey),
                    ("AimPointKey", node.AimPointKey),
                    ("PreferredBoneKey", node.PreferredBoneKey),
                    ("Priority", node.Priority),
                    ("Weight", node.Weight),
                    ("ActionContext", AssetIdentity(node.ActionContext))));
        }

        internal static CharacterSimulationNodeEmission ReadCameraBasis(ReadCameraBasisNode node)
        {
            return new CharacterSimulationNodeEmission(
                SimulationOperationCode.CameraBasisRead,
                integer0: CameraProgramOperationSchema.PayloadVersion);
        }

        static KeyValuePair<string, object>[] QueryFields(GameplayTagQuery query, string prefix)
        {
            var fields = new List<KeyValuePair<string, object>>();
            Add(query?.All, "All");
            Add(query?.Any, "Any");
            Add(query?.None, "None");
            return fields.ToArray();

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

        static ICharacterSimulationNodeEmitter Simple<T>(Func<T, CharacterSimulationNodeEmission> emit) where T : BaseNode
        {
            return new SimpleCharacterSimulationNodeEmitter<T>(emit);
        }

        static ICharacterSimulationNodeEmitter Camera<T>(Func<T, CharacterSimulationNodeEmission> emit) where T : BaseNode
        {
            return new CameraCharacterSimulationNodeEmitter<T>(emit);
        }

        static void RequireDefined<T>(T value, string field) where T : struct, Enum
        {
            if (!Enum.IsDefined(typeof(T), value))
                throw new InvalidOperationException($"Camera field '{field}' contains unknown enum value '{Convert.ToInt32(value)}'.");
        }

        static void RequireUnit(float value, string field)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f || value > 1f)
                throw new InvalidOperationException($"Camera field '{field}' must be finite and in [0, 1].");
        }

        static void RequireNonNegative(float value, string field)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
                throw new InvalidOperationException($"Camera field '{field}' must be finite and non-negative.");
        }

        static void RequireIdentity(string value, string field)
        {
            if (string.IsNullOrWhiteSpace(value) || !string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new InvalidOperationException($"Camera field '{field}' requires a trimmed identity.");
        }

        static void RequireOptionalIdentity(string value, string field)
        {
            if (value != null && !string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new InvalidOperationException($"Camera field '{field}' must not contain leading or trailing whitespace.");
        }

        static KeyValuePair<string, object>[] Fields(params (string Name, object Value)[] values)
        {
            var result = new KeyValuePair<string, object>[values.Length];
            for (int i = 0; i < values.Length; i++)
                result[i] = new KeyValuePair<string, object>(values[i].Name, values[i].Value);
            return result;
        }

        static string AssetIdentity(UnityEngine.Object asset) => CharacterSimulationNodeEmitterContext.AssetIdentity(asset);
    }

    sealed class SimpleCharacterSimulationNodeEmitter<T> : ICharacterSimulationNodeEmitter where T : BaseNode
    {
        readonly Func<T, CharacterSimulationNodeEmission> m_Emit;

        public SimpleCharacterSimulationNodeEmitter(Func<T, CharacterSimulationNodeEmission> emit)
        {
            m_Emit = emit ?? throw new ArgumentNullException(nameof(emit));
        }
        public Type SourceType => typeof(T);
        public OperationHandle Emit(BaseNode node, CharacterSimulationNodeEmitterContext context)
        {
            return context.Emit((T)node, m_Emit((T)node));
        }
    }

    sealed class CameraCharacterSimulationNodeEmitter<T> : ICharacterSimulationNodeEmitter where T : BaseNode
    {
        readonly Func<T, CharacterSimulationNodeEmission> m_Emit;

        public CameraCharacterSimulationNodeEmitter(Func<T, CharacterSimulationNodeEmission> emit)
        {
            m_Emit = emit ?? throw new ArgumentNullException(nameof(emit));
        }

        public Type SourceType => typeof(T);

        public OperationHandle Emit(BaseNode node, CharacterSimulationNodeEmitterContext context)
        {
            CharacterSimulationNodeEmission emission = m_Emit((T)node);
            CharacterSimulationSourceLocation source = context.Source(node, CameraProgramOperationSchema.OutputPortId);
            OperationHandle operation = context.Emit(node, emission, CameraProgramOperationSchema.OutputPortId);
            string producerIdentity = $"camera:{source.TemplateIdentity}";
            int producer = context.Builder.DeclareProducer(
                producerIdentity,
                CameraProgramOperationSchema.LayerId,
                source.TemplateIdentity,
                ProgramOutputChannelKind.Presentation,
                source);
            if (producer < 0)
                return operation;
            context.Builder.DeclareReference(
                $"{source.TemplateIdentity}/camera-producer",
                operation,
                ProgramReferenceKind.Producer,
                producer,
                producerIdentity,
                source);
            return operation;
        }
    }

    sealed class CameraBasisCharacterSimulationNodeEmitter : ICharacterSimulationNodeEmitter
    {
        public Type SourceType => typeof(ReadCameraBasisNode);

        public OperationHandle Emit(BaseNode node, CharacterSimulationNodeEmitterContext context)
        {
            var cameraBasis = (ReadCameraBasisNode)node;
            OperationHandle operation = context.Emit(
                cameraBasis,
                CharacterSimulationNodeEmitterRegistry.ReadCameraBasis(cameraBasis));
            context.RecordOutputPorts(
                cameraBasis,
                operation,
                CameraProgramOperationSchema.BasisValidPortId,
                CameraProgramOperationSchema.BasisPlanarForwardPortId,
                CameraProgramOperationSchema.BasisPlanarRightPortId,
                CameraProgramOperationSchema.BasisLookDirectionPortId,
                CameraProgramOperationSchema.BasisAimPointPortId,
                CameraProgramOperationSchema.BasisYawPortId,
                CameraProgramOperationSchema.BasisPitchPortId);
            return operation;
        }
    }
}
