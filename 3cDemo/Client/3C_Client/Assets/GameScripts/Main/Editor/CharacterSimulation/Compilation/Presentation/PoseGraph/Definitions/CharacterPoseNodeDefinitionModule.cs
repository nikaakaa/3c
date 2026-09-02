using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Editor;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;
using TreeDesigner.Editor;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    internal enum CharacterPoseOperationFamily : byte
    {
        None = 0,
        ParameterInput = 1,
        ParameterResolve = 2,
        Player = 3,
        StateMachine = 4,
        ActionInput = 5,
        AnimationSlot = 6,
        Blend = 7,
        Inertialization = 8,
        Composition = 9,
        SpaceConversion = 10,
        ComponentControl = 11,
        MotionMatching = 12,
        PoseHistory = 13,
        GoalContribution = 14,
        GoalAssembler = 15,
        FullBodyIk = 16,
        LinkedPose = 17,
        Output = 18
    }

    internal enum CharacterPoseGraphDependencyKind : byte
    {
        StatePose = 1,
        Subgraph = 2,
        MotionMatchingEntry = 3,
        LinkedPoseEntry = 4
    }

    internal readonly struct CharacterPoseGraphDependency
    {
        public CharacterPoseGraphDependency(
            CharacterPoseGraphDependencyKind kind,
            PoseGraphId graphId,
            string ownerIdentity)
        {
            if (!Enum.IsDefined(typeof(CharacterPoseGraphDependencyKind), kind) ||
                string.IsNullOrWhiteSpace(ownerIdentity))
            {
                throw new ArgumentException("Pose Graph dependency is invalid.");
            }
            Kind = kind;
            GraphId = graphId;
            OwnerIdentity = ownerIdentity.Trim();
        }

        public CharacterPoseGraphDependencyKind Kind { get; }
        public PoseGraphId GraphId { get; }
        public string OwnerIdentity { get; }
    }

    internal enum CharacterPoseCanvasCreationKind : byte
    {
        Direct = 1,
        DedicatedSurface = 2
    }

    internal sealed class CharacterPoseNodeDefinition
    {
        readonly ICharacterPoseCompilerHandler m_Adapter;
        GraphAuthoringCapabilityDescriptor m_Capability;

        public CharacterPoseNodeDefinition(
            ICharacterPoseCompilerHandler adapter)
        {
            m_Adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
            Kind = adapter.Kind;
            PayloadType = adapter.PayloadType;
            NativeRole = adapter.NativeRole;
            OperationCode = adapter.Code;
            OperationFamily = ResolveFamily(Kind);
            CanvasCreation = Kind == CharacterPoseNodeKind.LinkedPoseCall
                ? CharacterPoseCanvasCreationKind.DedicatedSurface
                : CharacterPoseCanvasCreationKind.Direct;
            Copyable = Kind != CharacterPoseNodeKind.GraphInput &&
                       Kind != CharacterPoseNodeKind.GraphOutput &&
                       Kind != CharacterPoseNodeKind.OutputPose &&
                       Kind != CharacterPoseNodeKind.PoseStateMachine;
        }

        public CharacterPoseNodeKind Kind { get; }
        public Type PayloadType { get; }
        public CharacterPoseNativeNodeRole NativeRole { get; }
        public CharacterPoseOperationCode OperationCode { get; }
        public CharacterPoseOperationFamily OperationFamily { get; }
        public CharacterPoseCanvasCreationKind CanvasCreation { get; }
        public bool Copyable { get; }
        public bool UsesPoseSourceSlot =>
            Kind == CharacterPoseNodeKind.SelectedPosePlayer ||
            Kind == CharacterPoseNodeKind.BlendStack ||
            Kind == CharacterPoseNodeKind.BlendSpacePlayer ||
            Kind == CharacterPoseNodeKind.ClipPlayer;
        public bool UsesAnimationChannel =>
            Kind == CharacterPoseNodeKind.ActionPlaybackInput ||
            Kind == CharacterPoseNodeKind.AnimationSlot;
        public bool Player => m_Adapter.Player;
        public bool ActionPlaybackControl => m_Adapter.ActionPlaybackControl;
        public bool BlendPolicy => m_Adapter.BlendPolicy;
        public bool StateMachine => m_Adapter.StateMachine;
        public bool AnimationSlot => m_Adapter.AnimationSlot;
        public bool Inertialization => m_Adapter.Inertialization;
        public bool Additive => m_Adapter.Additive;
        public bool ModifyBone => m_Adapter.ModifyBone;
        public bool RootOrientationWarp => m_Adapter.RootOrientationWarp;
        public bool ClipPlayer => m_Adapter.ClipPlayer;
        public GraphAuthoringCapabilityDescriptor Capability =>
            m_Capability ?? throw new InvalidOperationException(
                $"Pose Node Definition '{Kind}' has no capability projection.");
        public string CapabilityIdentity => Capability.CapabilityId.Value;
        public CharacterPoseExecutionDomain ExecutionDomain
        {
            get
            {
                if (!Enum.TryParse(
                        Capability.ExecutionDomainId,
                        false,
                        out CharacterPoseExecutionDomain value) ||
                    !Enum.IsDefined(typeof(CharacterPoseExecutionDomain), value))
                {
                    throw new InvalidOperationException(
                        $"Pose Node Definition '{Kind}' has invalid execution domain '{Capability.ExecutionDomainId}'.");
                }
                return value;
            }
        }

        internal void BindCapability(
            GraphAuthoringCapabilityDescriptor capability)
        {
            if (m_Capability != null)
                throw new InvalidOperationException(
                    $"Pose Node Definition '{Kind}' capability is already bound.");
            if (capability == null ||
                capability.AuthoringType != PayloadType ||
                !string.Equals(
                    capability.ExternalKind,
                    CharacterPoseGraphAuthoringCapabilities.Get(Kind).Value,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Pose Node Definition '{Kind}' capability projection does not match payload '{PayloadType.FullName}'.");
            }
            m_Capability = capability;
        }

        public CharacterPoseNodePayload CreatePayload(
            CharacterPoseAuthoringPayloadInput input) =>
            m_Adapter.CreatePayload(input);

        public CharacterPoseNodePayload CreateDefaultPayload() =>
            (CharacterPoseNodePayload)Activator.CreateInstance(PayloadType);

        public CharacterPoseNodePayload MutatePayload(
            CharacterPoseNodePayload payload,
            IReadOnlyDictionary<string, object> fields)
        {
            RequirePayload(payload);
            if (fields == null || fields.Count == 0)
                throw new ArgumentException(
                    "Pose payload field mutation set is empty.",
                    nameof(fields));
            foreach (string fieldId in fields.Keys)
            {
                if (!Capability.TryGetField(
                        new GraphAuthoringFieldId(fieldId),
                        out GraphAuthoringFieldDescriptor field) ||
                    !field.AuthoringWritable)
                {
                    throw new InvalidOperationException(
                        $"Pose capability '{Capability.CapabilityId}' does not declare writable field '{fieldId}'.");
                }
            }
            return CreatePayload(new CharacterPoseAuthoringPayloadInput(
                (fieldId, expectedType) =>
                {
                    object value = fields.TryGetValue(fieldId, out object replacement)
                        ? replacement
                        : ReadField(payload, fieldId);
                    return ConvertFieldValue(fieldId, value, expectedType);
                },
                payload is CharacterPoseStateMachineNodePayload stateMachine
                    ? new Func<CharacterPoseStateMachineDefinition>(() =>
                        stateMachine.StateMachine)
                    : null));
        }

        public object ReadField(
            CharacterPoseNodePayload payload,
            string field) =>
            m_Adapter.ReadField(payload, field);

        public void RequirePayload(CharacterPoseNodePayload payload) =>
            m_Adapter.RequirePayload(payload);

        public void ValidatePayload(
            CharacterPoseNodePayload payload,
            string sourcePath) =>
            m_Adapter.ValidatePayload(payload, sourcePath);

        public void ValidateRig(
            CharacterPoseNodePayload payload,
            CharacterAnimationRigDefinition rig,
            string sourcePath) =>
            m_Adapter.ValidateRig(payload, rig, sourcePath);

        public CharacterPoseIrNode Lower(
            CharacterTypedPoseNode node,
            IReadOnlyList<CharacterPoseIrInput> inputs,
            string sourcePath) =>
            m_Adapter.Lower(node, inputs, sourcePath);

        public CharacterPresentationPoseSourceSlot Source(
            CharacterPoseNodePayload payload) =>
            m_Adapter.Source(payload);

        public AnimationChannelId Channel(
            CharacterPoseNodePayload payload) =>
            m_Adapter.Channel(payload);

        public PoseParameterId Parameter(
            CharacterPoseNodePayload payload) =>
            m_Adapter.Parameter(payload);

        public AnimationSelectionAvailabilityPolicy Availability(
            CharacterPoseNodePayload payload,
            bool stateLocal) =>
            m_Adapter.Availability(payload, stateLocal);

        public CharacterAnimationBlendSpaceInputRangePolicy InputRange(
            CharacterPoseNodePayload payload) =>
            m_Adapter.InputRange(payload);

        public float Weight(CharacterPoseNodePayload payload) =>
            m_Adapter.Weight(payload);

        public CharacterAnimationBoneMaskAsset BoneMask(
            CharacterPoseNodePayload payload) =>
            m_Adapter.BoneMask(payload);

        public IReadOnlyList<CharacterPoseParameterPolicy>
            ParameterPolicies(CharacterPoseNodePayload payload) =>
            m_Adapter.ParameterPolicies(payload);

        public IReadOnlyList<CharacterPoseGraphDependency>
            ProjectGraphDependencies(CharacterPoseNodePayload payload) =>
            m_Adapter.ProjectGraphDependencies(payload);

        public string ProjectChildDocumentId(
            CharacterPoseNodePayload payload) =>
            m_Adapter.ProjectChildDocumentId(payload);

        public string SourceMapName(CharacterPoseNodePayload payload) =>
            m_Adapter.SourceMapName(payload);

        public IReadOnlyList<GraphAuthoringTypedPropertyValue>
            ProjectTypedProperties(CharacterPoseNodePayload payload)
        {
            RequirePayload(payload);
            return Capability.PortVariants
                .Select(value => value.When.FieldId)
                .Distinct()
                .Select(fieldId =>
                {
                    GraphAuthoringFieldDescriptor field = Capability.Fields
                        .Single(value => value.FieldId.Equals(fieldId));
                    return new GraphAuthoringTypedPropertyValue(
                        fieldId,
                        field.ValueKind,
                        Canonical(ReadField(payload, fieldId.Value)));
                })
                .ToArray();
        }

        public IReadOnlyList<GraphAuthoringDynamicPortProjection>
            ProjectPortShape(CharacterTypedPoseNode node)
        {
            if (node == null || node.Kind != Kind)
                throw new ArgumentException(
                    "Pose node does not match Node Definition.",
                    nameof(node));
            return GraphAuthoringNodePortShapeProjector.ProjectComplete(
                Capability,
                ProjectTypedProperties(node.Payload),
                node.DynamicPorts.Select(ProjectDynamicPort).ToArray());
        }

        public IReadOnlyList<GraphAuthoringDynamicPortProjection>
            ProjectAdditionalPorts(CharacterTypedPoseNode node)
        {
            if (node == null || node.Kind != Kind)
                throw new ArgumentException(
                    "Pose node does not match Node Definition.",
                    nameof(node));
            return GraphAuthoringNodePortShapeProjector.Project(
                Capability,
                ProjectTypedProperties(node.Payload),
                node.DynamicPorts.Select(ProjectDynamicPort).ToArray());
        }

        public IReadOnlyList<GraphAuthoringDynamicPortProjection>
            ProjectDeclaredPortShape(CharacterPoseNodePayload payload) =>
            GraphAuthoringNodePortShapeProjector.ProjectComplete(
                Capability,
                ProjectTypedProperties(payload));

        static GraphAuthoringDynamicPortProjection ProjectDynamicPort(
            CharacterPoseDynamicPort port)
        {
            if (port == null)
                throw new InvalidOperationException("Pose dynamic port is missing.");
            return new GraphAuthoringDynamicPortProjection(
                new GraphAuthoringPortId(port.PortId.Value),
                port.DisplayName,
                CharacterPoseAuthoringPortProjection.ValueType(port.Kind),
                port.Direction == CharacterPosePortDirection.Input
                    ? GraphAuthoringPortDirection.Input
                    : GraphAuthoringPortDirection.Output,
                port.Direction == CharacterPosePortDirection.Input
                    ? GraphAuthoringPortCapacity.Single
                    : GraphAuthoringPortCapacity.Multiple,
                port.Required,
                port.Order,
                port.InterfacePortId.Value);
        }

        static string Canonical(object value)
        {
            if (value == null)
                return string.Empty;
            return value is IFormattable formattable
                ? formattable.ToString(null, CultureInfo.InvariantCulture)
                : value.ToString();
        }

        static object ConvertFieldValue(
            string fieldId,
            object value,
            Type expectedType)
        {
            if (value == null || expectedType.IsInstanceOfType(value))
                return value;
            try
            {
                if (expectedType == typeof(string))
                    return Convert.ToString(value, CultureInfo.InvariantCulture);
                if (expectedType.IsEnum)
                    return Enum.Parse(expectedType, value.ToString(), false);
                if (typeof(IConvertible).IsAssignableFrom(expectedType) &&
                    value is IConvertible)
                {
                    return Convert.ChangeType(
                        value,
                        expectedType,
                        CultureInfo.InvariantCulture);
                }
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    $"Pose field '{fieldId}' cannot convert to '{expectedType.Name}'.",
                    exception);
            }
            throw new InvalidOperationException(
                $"Pose field '{fieldId}' requires '{expectedType.Name}'.");
        }

        static CharacterPoseOperationFamily ResolveFamily(
            CharacterPoseNodeKind kind) =>
            kind switch
            {
                CharacterPoseNodeKind.ProgramParameterInput =>
                    CharacterPoseOperationFamily.ParameterInput,
                CharacterPoseNodeKind.PoseParameterResolve =>
                    CharacterPoseOperationFamily.ParameterResolve,
                CharacterPoseNodeKind.SelectedPosePlayer or
                CharacterPoseNodeKind.ClipPlayer or
                CharacterPoseNodeKind.BlendSpacePlayer =>
                    CharacterPoseOperationFamily.Player,
                CharacterPoseNodeKind.PoseStateMachine =>
                    CharacterPoseOperationFamily.StateMachine,
                CharacterPoseNodeKind.ActionPlaybackInput =>
                    CharacterPoseOperationFamily.ActionInput,
                CharacterPoseNodeKind.AnimationSlot =>
                    CharacterPoseOperationFamily.AnimationSlot,
                CharacterPoseNodeKind.BlendStack or
                CharacterPoseNodeKind.BlendPose =>
                    CharacterPoseOperationFamily.Blend,
                CharacterPoseNodeKind.Inertialization =>
                    CharacterPoseOperationFamily.Inertialization,
                CharacterPoseNodeKind.LayeredBoneBlend or
                CharacterPoseNodeKind.AdditivePose =>
                    CharacterPoseOperationFamily.Composition,
                CharacterPoseNodeKind.LocalToComponentPose or
                CharacterPoseNodeKind.ComponentToLocalPose =>
                    CharacterPoseOperationFamily.SpaceConversion,
                CharacterPoseNodeKind.ModifyBone or
                CharacterPoseNodeKind.RootOrientationWarp =>
                    CharacterPoseOperationFamily.ComponentControl,
                CharacterPoseNodeKind.MotionMatchingPose =>
                    CharacterPoseOperationFamily.MotionMatching,
                CharacterPoseNodeKind.PoseHistoryCollector =>
                    CharacterPoseOperationFamily.PoseHistory,
                CharacterPoseNodeKind.FootPlacement or
                CharacterPoseNodeKind.PoseBoneIKGoals =>
                    CharacterPoseOperationFamily.GoalContribution,
                CharacterPoseNodeKind.FullBodyIkGoalAssembler =>
                    CharacterPoseOperationFamily.GoalAssembler,
                CharacterPoseNodeKind.FullBodyIK =>
                    CharacterPoseOperationFamily.FullBodyIk,
                CharacterPoseNodeKind.LinkedPoseCall =>
                    CharacterPoseOperationFamily.LinkedPose,
                CharacterPoseNodeKind.OutputPose =>
                    CharacterPoseOperationFamily.Output,
                CharacterPoseNodeKind.PoseSubgraph or
                CharacterPoseNodeKind.GraphInput or
                CharacterPoseNodeKind.GraphOutput or
                CharacterPoseNodeKind.EntryPoseInput =>
                    CharacterPoseOperationFamily.None,
                _ => throw new InvalidOperationException(
                    $"Pose node kind '{kind}' has no Operation Family.")
            };
    }

    internal sealed class CharacterPoseNodeDefinitionModule
    {
        readonly Dictionary<CharacterPoseNodeKind, CharacterPoseNodeDefinition>
            m_Definitions;
        readonly Dictionary<Type, CharacterPoseNodeDefinition>
            m_ByPayloadType;
        readonly Dictionary<string, CharacterPoseNodeDefinition>
            m_ByCapability = new Dictionary<string, CharacterPoseNodeDefinition>(
                StringComparer.Ordinal);
        bool m_CapabilitiesBound;

        public static CharacterPoseNodeDefinitionModule Shared { get; } =
            new CharacterPoseNodeDefinitionModule();

        CharacterPoseNodeDefinitionModule()
        {
            ICharacterPoseCompilerHandler[] adapters =
            {
                new CharacterProgramParameterInputPoseCompilerHandler(),
                new CharacterActionPlaybackInputPoseCompilerHandler(),
                new CharacterSelectedPosePlayerCompilerHandler(),
                new CharacterBlendSpacePlayerPoseCompilerHandler(),
                new CharacterClipPlayerPoseCompilerHandler(),
                new CharacterPoseStateMachineNodeCompilerHandler(),
                new CharacterAnimationSlotPoseCompilerHandler(),
                new CharacterBlendStackPoseCompilerHandler(),
                new CharacterInertializationPoseCompilerHandler(),
                new CharacterBlendPoseCompilerHandler(),
                new CharacterLayeredBoneBlendPoseCompilerHandler(),
                new CharacterAdditivePoseCompilerHandler(),
                new CharacterPoseParameterResolveCompilerHandler(),
                new CharacterModifyBonePoseCompilerHandler(),
                new CharacterRootOrientationWarpPoseCompilerHandler(),
                new CharacterPoseSubgraphCompilerHandler(),
                new CharacterLocalToComponentPoseCompilerHandler(),
                new CharacterComponentToLocalPoseCompilerHandler(),
                new CharacterGraphInputPoseCompilerHandler(),
                new CharacterGraphOutputPoseCompilerHandler(),
                new CharacterOutputPoseCompilerHandler(),
                new CharacterFootPlacementPoseCompilerHandler(),
                new CharacterPoseBoneIkGoalsCompilerHandler(),
                new CharacterFullBodyIkPoseCompilerHandler(),
                new CharacterFullBodyIkGoalAssemblerCompilerHandler(),
                new CharacterLinkedPoseCallCompilerHandler(),
                new CharacterMotionMatchingPoseCompilerHandler(),
                new CharacterPoseHistoryCollectorCompilerHandler(),
                new CharacterEntryPoseInputCompilerHandler()
            };
            m_Definitions = new Dictionary<CharacterPoseNodeKind,
                CharacterPoseNodeDefinition>();
            m_ByPayloadType = new Dictionary<Type,
                CharacterPoseNodeDefinition>();
            foreach (ICharacterPoseCompilerHandler adapter in adapters)
            {
                var definition = new CharacterPoseNodeDefinition(adapter);
                if (m_Definitions.TryGetValue(
                        definition.Kind,
                        out CharacterPoseNodeDefinition duplicateKind))
                {
                    throw new InvalidOperationException(
                        $"Pose Node Kind '{definition.Kind}' is declared by both '{duplicateKind.PayloadType.FullName}' and '{definition.PayloadType.FullName}'.");
                }
                if (m_ByPayloadType.TryGetValue(
                        definition.PayloadType,
                        out CharacterPoseNodeDefinition duplicatePayload))
                {
                    throw new InvalidOperationException(
                        $"Pose payload '{definition.PayloadType.FullName}' is declared by both '{duplicatePayload.Kind}' and '{definition.Kind}'.");
                }
                m_Definitions.Add(definition.Kind, definition);
                m_ByPayloadType.Add(
                    definition.PayloadType,
                    definition);
            }
            CharacterPoseNodeKind[] formalKinds = Enum
                .GetValues(typeof(CharacterPoseNodeKind))
                .Cast<CharacterPoseNodeKind>()
                .ToArray();
            if (m_Definitions.Count != formalKinds.Length ||
                formalKinds.Any(value => !m_Definitions.ContainsKey(value)))
            {
                throw new InvalidOperationException(
                    "Every formal Pose Node Kind must have exactly one Node Definition adapter.");
            }
        }

        public IReadOnlyCollection<CharacterPoseNodeDefinition> All
        {
            get
            {
                EnsureCapabilities();
                return m_Definitions.Values;
            }
        }

        public GraphAuthoringCapabilityDescriptor ProjectCapability(
            GraphAuthoringCapabilityDescriptor capability)
        {
            if (m_CapabilitiesBound)
                throw new InvalidOperationException(
                    "Pose Node Definition capabilities are already sealed.");
            if (capability?.AuthoringType == null ||
                !m_ByPayloadType.TryGetValue(
                    capability.AuthoringType,
                    out CharacterPoseNodeDefinition definition))
            {
                throw new InvalidOperationException(
                    $"Pose capability '{capability?.CapabilityId}' has no matching Node Definition adapter.");
            }
            definition.BindCapability(capability);
            if (!m_ByCapability.TryAdd(
                    capability.CapabilityId.Value,
                    definition))
            {
                throw new InvalidOperationException(
                    $"Pose capability '{capability.CapabilityId}' is registered more than once.");
            }
            return capability;
        }

        public void SealCapabilities()
        {
            if (m_CapabilitiesBound)
                return;
            CharacterPoseNodeDefinition[] missing = m_Definitions.Values
                .Where(value => !m_ByCapability.ContainsKey(
                    CharacterPoseGraphAuthoringCapabilities.Get(value.Kind).Value))
                .ToArray();
            if (missing.Length != 0 ||
                m_ByCapability.Count != m_Definitions.Count)
            {
                throw new InvalidOperationException(
                    $"Pose Node Definition capability projection is incomplete: [{string.Join(",", missing.Select(value => value.Kind))}].");
            }
            m_CapabilitiesBound = true;
        }

        public CharacterPoseNodeDefinition Require(
            CharacterPoseNodeKind kind)
        {
            EnsureCapabilities();
            return m_Definitions.TryGetValue(kind, out CharacterPoseNodeDefinition value)
                ? value
                : throw new InvalidOperationException(
                    $"Pose Node Kind '{kind}' has no Node Definition.");
        }

        public CharacterPoseNodeDefinition RequireCapability(
            string capabilityIdentity)
        {
            EnsureCapabilities();
            return !string.IsNullOrWhiteSpace(capabilityIdentity) &&
                   m_ByCapability.TryGetValue(
                       capabilityIdentity,
                       out CharacterPoseNodeDefinition value)
                ? value
                : throw new InvalidOperationException(
                    $"Pose capability '{capabilityIdentity ?? "<null>"}' has no Node Definition.");
        }

        public CharacterPoseNodeDefinition RequirePayload(
            CharacterPoseNodePayload payload)
        {
            EnsureCapabilities();
            return payload != null &&
                   m_ByPayloadType.TryGetValue(
                       payload.GetType(),
                       out CharacterPoseNodeDefinition value) &&
                   value.Kind == payload.Kind
                ? value
                : throw new InvalidOperationException(
                    $"Pose payload '{payload?.GetType().FullName ?? "null"}' has no Node Definition.");
        }

        void EnsureCapabilities()
        {
            CharacterPoseGraphAuthoringCapabilities.EnsureRegistered();
            if (!m_CapabilitiesBound)
                throw new InvalidOperationException(
                    "Pose Node Definition capabilities are not sealed.");
        }
    }
}
