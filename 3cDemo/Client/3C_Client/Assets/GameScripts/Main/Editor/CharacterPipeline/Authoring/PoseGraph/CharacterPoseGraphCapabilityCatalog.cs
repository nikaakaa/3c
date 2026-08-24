using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Motion.RootMotion;
using ThirdPersonCharacter.Pipeline.Presentation;
using TreeDesigner.Editor;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    [InitializeOnLoad]
    static class CharacterGraphAuthoringCapabilityBootstrap
    {
        static CharacterGraphAuthoringCapabilityBootstrap()
        {
            _ = new BtsmtlGraphAuthoringCapabilities();
            CharacterPoseGraphAuthoringCapabilities.EnsureRegistered();
        }
    }

    public static class CharacterPoseGraphAuthoringCapabilities
    {
        public static readonly GraphAuthoringCommandId PingPoseSource = new GraphAuthoringCommandId("ping-pose-source");
        public static readonly GraphAuthoringCommandId OpenPoseSource = new GraphAuthoringCommandId("open-pose-source");
        public static readonly GraphAuthoringCommandId OpenPoseSourceProfile = new GraphAuthoringCommandId("open-pose-source-profile");
        public static readonly GraphAuthoringCommandId OpenFullBodyIkProfile = new GraphAuthoringCommandId("open-full-body-ik-profile");
        public static readonly GraphAuthoringDomainId Domain = new GraphAuthoringDomainId("character-presentation");
        public static readonly GraphAuthoringDocumentRoleId RootGraph = new GraphAuthoringDocumentRoleId("pose-graph");
        public static readonly GraphAuthoringDocumentRoleId StatePoseGraph = new GraphAuthoringDocumentRoleId("pose-state-graph");
        public static readonly GraphAuthoringDocumentRoleId Subgraph = new GraphAuthoringDocumentRoleId("pose-subgraph");
        public static readonly GraphAuthoringDocumentRoleId LinkedPoseEntry = new GraphAuthoringDocumentRoleId("linked-pose-entry");
        public static readonly GraphAuthoringDocumentRoleId StateMachine = new GraphAuthoringDocumentRoleId("pose-state-machine");
        public static readonly GraphAuthoringDocumentRoleId TransitionRule = new GraphAuthoringDocumentRoleId("pose-transition-rule");
        public static readonly GraphAuthoringCapabilityId StateMachineState = new GraphAuthoringCapabilityId("pose.state-machine.state");
        public static readonly GraphAuthoringCapabilityId StateMachineTransition = new GraphAuthoringCapabilityId("pose.state-machine.transition");

        static readonly IReadOnlyDictionary<PoseTransitionRuleOperationKind, GraphAuthoringCapabilityId> s_RuleCapabilities =
            Enum.GetValues(typeof(PoseTransitionRuleOperationKind))
                .Cast<PoseTransitionRuleOperationKind>()
                .ToDictionary(
                    value => value,
                    value => new GraphAuthoringCapabilityId(
                        "pose.transition-rule." +
                        ToKebabCase(value.ToString())));

        static bool s_Registered;

        public static GraphAuthoringCapabilityCatalog Catalog
        {
            get
            {
                EnsureRegistered();
                return GraphAuthoringCapabilityRegistrationRoot.Catalog;
            }
        }

        public static GraphAuthoringCapabilityId Get(CharacterPoseNodeKind kind)
        {
            if (!Enum.IsDefined(typeof(CharacterPoseNodeKind), kind))
            {
                throw new InvalidOperationException(
                    $"Pose node kind '{kind}' has no authoring capability identity.");
            }
            return new GraphAuthoringCapabilityId(
                "pose." + ToKebabCase(kind.ToString()));
        }

        public static GraphAuthoringCapabilityDescriptor Require(
            CharacterPoseNodeKind kind) =>
            Catalog.Require(Get(kind));

        public static Type RequirePayloadType(
            CharacterPoseNodeKind kind)
        {
            Type type = Require(kind).AuthoringType;
            return type != null &&
                   typeof(CharacterPoseNodePayload).IsAssignableFrom(type)
                ? type
                : throw new InvalidOperationException(
                    $"Pose capability '{Get(kind)}' has no typed payload.");
        }

        public static CharacterPoseNodeKind RequireKind(
            CharacterPoseNodePayload payload)
        {
            if (payload == null ||
                !Catalog.TryGetByAuthoringType(
                    Domain,
                    payload.GetType(),
                    out GraphAuthoringCapabilityDescriptor descriptor) ||
                !descriptor.CapabilityId.Equals(Get(payload.Kind)))
            {
                throw new InvalidOperationException(
                    $"Pose payload '{payload?.GetType().FullName ?? "null"}' is not registered.");
            }
            return payload.Kind;
        }

        public static GraphAuthoringCapabilityId Get(
            PoseTransitionRuleOperationKind kind)
        {
            if (!s_RuleCapabilities.TryGetValue(
                    kind,
                    out GraphAuthoringCapabilityId capabilityId))
            {
                throw new InvalidOperationException(
                    $"Pose Transition Rule operation kind '{kind}' has no authoring capability identity.");
            }
            return capabilityId;
        }

        public static bool TryGetRuleOperationKind(
            GraphAuthoringCapabilityId capabilityId,
            out PoseTransitionRuleOperationKind kind)
        {
            foreach (KeyValuePair<
                         PoseTransitionRuleOperationKind,
                         GraphAuthoringCapabilityId> entry in
                     s_RuleCapabilities)
            {
                if (!entry.Value.Equals(capabilityId))
                    continue;
                kind = entry.Key;
                return true;
            }
            kind = default;
            return false;
        }

        public static void EnsureRegistered()
        {
            if (s_Registered)
                return;
            GraphAuthoringCapabilityRegistrationRoot.RegisterDomain(
                "character-presentation.pose",
                Register);
            s_Registered = true;
            CharacterMotionMatchingPoseAuthoringCapabilities.EnsureRegistered();
        }

        static void Register(GraphAuthoringCapabilityCatalog catalog)
        {
            GraphAuthoringDocumentRoleId[] allPoseGraphs = { RootGraph, StatePoseGraph, Subgraph };
            GraphAuthoringDocumentRoleId[] allPoseGraphsWithLinkedEntry = { RootGraph, StatePoseGraph, Subgraph, LinkedPoseEntry };
            GraphAuthoringDocumentRoleId[] rootAndState = { RootGraph, StatePoseGraph };
            GraphAuthoringDocumentRoleId[] rootAndStateWithLinkedEntry = { RootGraph, StatePoseGraph, LinkedPoseEntry };
            GraphAuthoringDocumentRoleId[] rootAndLinkedEntry = { RootGraph, LinkedPoseEntry };
            GraphAuthoringDocumentRoleId[] rootOnly = { RootGraph };
            GraphAuthoringDocumentRoleId[] stateSubgraphAndLinkedEntry = { StatePoseGraph, Subgraph, LinkedPoseEntry };
            GraphAuthoringDocumentRoleId[] linkedEntryOnly = { LinkedPoseEntry };
            Color inputColor = new Color32(58, 103, 138, 255);
            Color sourceColor = new Color32(55, 115, 92, 255);
            Color blendColor = new Color32(98, 76, 142, 255);
            Color constraintColor = new Color32(133, 83, 55, 255);
            Color outputColor = new Color32(132, 55, 67, 255);

            catalog.Register(Node<CharacterProgramParameterInputPosePayload>(CharacterPoseNodeKind.ProgramParameterInput, allPoseGraphsWithLinkedEntry, "Animation Parameter", "Inputs", inputColor,
                Fields(Field("parameter-id", "Parameter", GraphAuthoringFieldValueKind.IdentityReference, "pose-parameter")),
                Ports(Out("parameter", "Parameter", "pose.parameter")),
                executionDomain: CharacterPoseExecutionDomain.FactAndDemand));
            catalog.Register(Node<CharacterActionPlaybackInputPosePayload>(CharacterPoseNodeKind.ActionPlaybackInput, rootOnly, "Action Playback Input", "Inputs", inputColor,
                Fields(Field("animation-channel-id", "Animation Channel", GraphAuthoringFieldValueKind.IdentityReference, "animation-channel")),
                Ports(Out("action-playback", "Action Playback", "pose.action-playback")),
                executionDomain: CharacterPoseExecutionDomain.FactAndDemand));
            catalog.Register(Node<CharacterSelectedPosePlayerPayload>(CharacterPoseNodeKind.SelectedPosePlayer, rootAndStateWithLinkedEntry, "Selected Pose Player", "Sources", sourceColor,
                Fields(SourceField(typeof(CharacterMotionMatchingPoseSourceSlot))),
                Ports(Out("pose", "Local Pose", "pose.local")),
                commands: SourceCommands(),
                executionDomain: CharacterPoseExecutionDomain.SourceCapture));
            catalog.Register(Node<CharacterBlendSpacePlayerPosePayload>(CharacterPoseNodeKind.BlendSpacePlayer, rootAndStateWithLinkedEntry, "Blend Space Player", "Sources", sourceColor,
                Fields(SourceField(typeof(CharacterBlendSpacePoseSourceSlot)), EnumField("input-range-policy", "Input Range", typeof(CharacterAnimationBlendSpaceInputRangePolicy))),
                Ports(In("x", "X", "pose.parameter"), OptionalIn("y", "Y", "pose.parameter"), Out("pose", "Local Pose", "pose.local"), Out("discontinuity", "Discontinuity", "pose.discontinuity")),
                commands: SourceCommands(),
                executionDomain: CharacterPoseExecutionDomain.SourceCapture));
            catalog.Register(Node<CharacterClipPlayerPosePayload>(CharacterPoseNodeKind.ClipPlayer, rootAndStateWithLinkedEntry, "Clip Player", "Sources", sourceColor,
                Fields(SourceField(typeof(CharacterClipPoseSourceSlot)), FloatField("play-rate", "Play Rate", 1f), FloatField("initial-time", "Initial Time", 0f), EnumField("clock-source", "Clock Source", typeof(CharacterClipPlayerClockSource))),
                Ports(Out("pose", "Local Pose", "pose.local"), Out("discontinuity", "Discontinuity", "pose.discontinuity")),
                commands: SourceCommands(),
                executionDomain: CharacterPoseExecutionDomain.SourceCapture));
            catalog.Register(Node<CharacterPoseStateMachineNodePayload>(CharacterPoseNodeKind.PoseStateMachine, rootAndLinkedEntry, "Animation State Machine", "State Machine", blendColor,
                Array.Empty<GraphAuthoringFieldDescriptor>(),
                Ports(Out("pose", "Local Pose", "pose.local")),
                GraphAuthoringDynamicPortPolicy.None,
                new[] { Child("open-state-machine", "Open State Machine", StateMachine) }));
            catalog.Register(Node<CharacterAnimationSlotPosePayload>(CharacterPoseNodeKind.AnimationSlot, rootOnly, "Slot", "Action", blendColor,
                Fields(Field("animation-channel-id", "Animation Channel", GraphAuthoringFieldValueKind.IdentityReference, "animation-channel"), Field("slot-id", "Slot", GraphAuthoringFieldValueKind.IdentityReference, "animation-slot"), SelectionAvailabilityField(), AssetField("blend-policy", "Blend Policy", "animation-blend-policy", typeof(CharacterAnimationBlendPolicy))),
                Ports(In("source-pose", "Source Local Pose", "pose.local"), In("action-playback", "Action Playback", "pose.action-playback"), Out("pose", "Local Pose", "pose.local"))));
            catalog.Register(Node<CharacterBlendStackPosePayload>(CharacterPoseNodeKind.BlendStack, rootAndStateWithLinkedEntry, "Blend Stack", "Blend", blendColor,
                Fields(SourceField(typeof(CharacterMotionMatchingPoseSourceSlot)), AssetField("blend-policy", "Blend Policy", "animation-blend-policy", typeof(CharacterAnimationBlendPolicy))),
                Ports(Out("pose", "Local Pose", "pose.local")),
                commands: SourceCommands(),
                executionDomain: CharacterPoseExecutionDomain.SourceCapture));
            catalog.Register(Node<CharacterInertializationPosePayload>(CharacterPoseNodeKind.Inertialization, allPoseGraphsWithLinkedEntry, "Inertialization", "Blend", blendColor,
                Fields(AssetField("inertialization-policy", "Policy", "pose-inertialization-policy", typeof(CharacterPoseInertializationPolicy))),
                UnaryLocalPosePorts()));
            catalog.Register(Node<CharacterBlendPosePayload>(CharacterPoseNodeKind.BlendPose, allPoseGraphsWithLinkedEntry, "Blend Pose", "Blend", blendColor,
                Fields(FloatField("weight", "Weight", 1f, 0f, 1f)),
                BinaryLocalPoseWithWeight("Base", "Overlay"),
                GraphAuthoringDynamicPortPolicy.OrderedInputs));
            catalog.Register(Node<CharacterLayeredBoneBlendPosePayload>(CharacterPoseNodeKind.LayeredBoneBlend, allPoseGraphsWithLinkedEntry, "Layered Blend Per Bone", "Blend", blendColor,
                Fields(AssetField("bone-mask", "Bone Mask", "animation-bone-mask", typeof(CharacterAnimationBoneMaskAsset)), FloatField("weight", "Weight", 1f, 0f, 1f)),
                BinaryLocalPoseWithWeight("Base", "Overlay")));
            catalog.Register(Node<CharacterAdditivePosePayload>(CharacterPoseNodeKind.AdditivePose, allPoseGraphsWithLinkedEntry, "Additive Pose", "Blend", blendColor,
                Fields(StringField("reference-pose-id", "Reference Pose", "RigReference"), EnumField("reference-space", "Reference Space", typeof(AdditiveReferenceSpace)), EnumField("scale-policy", "Scale Policy", typeof(AdditiveScalePolicy)), FloatField("weight", "Weight", 1f, 0f, 1f)),
                BinaryLocalPoseWithWeight("Base", "Additive")));
            catalog.Register(Node<CharacterPoseParameterResolvePayload>(CharacterPoseNodeKind.PoseParameterResolve, allPoseGraphsWithLinkedEntry, "Pose Parameter Resolve", "Parameters", blendColor,
                Fields(Field("parameter-policies", "Parameter Policies", GraphAuthoringFieldValueKind.Object, "pose-parameter-policy")),
                Ports(In("base-pose", "Base Local Pose", "pose.local"), In("parameter-source-pose", "Parameter Source Local Pose", "pose.local"), Out("pose", "Local Pose", "pose.local"))));
            catalog.Register(Node<CharacterModifyBonePosePayload>(CharacterPoseNodeKind.ModifyBone, allPoseGraphs, "Modify Bone", "Constraints", constraintColor,
                Fields(Field("bone-id", "Bone", GraphAuthoringFieldValueKind.IdentityReference, "rig-bone"), EnumField("reference-space", "Reference Space", typeof(ModifyBoneReferenceSpace)), EnumField("operations", "Operations", typeof(ModifyBoneOperationMask)), Vector3Field("position", "Position"), Field("rotation", "Rotation", GraphAuthoringFieldValueKind.Quaternion, ""), Vector3Field("scale", "Scale", Vector3.one)),
                UnaryComponentPoseWithWeight()));
            catalog.Register(Node<CharacterRootOrientationWarpPosePayload>(CharacterPoseNodeKind.RootOrientationWarp, rootAndStateWithLinkedEntry, "Root Orientation Warp", "Constraints", constraintColor,
                Fields(AssetField("yaw-curve", "Yaw Profile", "root-motion-curve", typeof(RootMotionCurveAsset))),
                UnaryLocalPosePorts()));
            catalog.Register(Node<CharacterFootPlacementPosePayload>(CharacterPoseNodeKind.FootPlacement, allPoseGraphs, "Foot Placement", "Goal Sources", constraintColor,
                Fields(AssetField("profile", "Profile", "foot-placement-profile", typeof(CharacterFootPlacementProfile)), AssetField("calibration", "Calibration", "foot-placement-calibration", typeof(CharacterFootPlacementRigCalibration))),
                Ports(In("pose", "Component Pose", "pose.component"), OptionalIn("weight", "Weight", "pose.parameter"), Out("contribution", "Goal Contribution", "component.full-body-ik-goal-contribution")),
                executionDomain: CharacterPoseExecutionDomain.WorldAwareValue));
            catalog.Register(Node<CharacterPoseBoneIkGoalsPayload>(CharacterPoseNodeKind.PoseBoneIKGoals, allPoseGraphsWithLinkedEntry, "Pose Bone IK Goals", "Goal Sources", constraintColor,
                Fields(Field("bindings", "Effector Bindings", GraphAuthoringFieldValueKind.Object, "full-body-ik-goal-binding")),
                Ports(In("pose", "Component Pose", "pose.component"), Out("contribution", "Goal Contribution", "component.full-body-ik-goal-contribution")),
                executionDomain: CharacterPoseExecutionDomain.PureValue));
            catalog.Register(Node<CharacterFullBodyIkGoalAssemblerPayload>(CharacterPoseNodeKind.FullBodyIkGoalAssembler, allPoseGraphs, "Goal Assembler", "Goal Sources", constraintColor,
                Array.Empty<GraphAuthoringFieldDescriptor>(),
                Ports(Out("goals", "Full Body IK Goals", "component.full-body-ik-goals")),
                GraphAuthoringDynamicPortPolicy.OrderedInputs,
                executionDomain: CharacterPoseExecutionDomain.PureValue));
            catalog.Register(Node<CharacterFullBodyIkPosePayload>(CharacterPoseNodeKind.FullBodyIK, allPoseGraphs, "Full Body IK", "Constraints", constraintColor,
                Fields(ReadOnlyField("backend", "Solver Backend", GraphAuthoringFieldValueKind.String)),
                Ports(In("pose", "Component Pose", "pose.component"), In("goals", "Full Body IK Goals", "component.full-body-ik-goals"), Out("result", "Solved Component Pose", "pose.component")),
                commands: new[]
                {
                    new GraphAuthoringCommandDescriptor(OpenFullBodyIkProfile, "Edit FinalIK FBBIK Profile", false)
                },
                executionDomain: CharacterPoseExecutionDomain.PurePose));
            catalog.Register(Node<CharacterLocalToComponentPosePayload>(CharacterPoseNodeKind.LocalToComponentPose, allPoseGraphs, "Local To Component", "Pose Space", constraintColor,
                Array.Empty<GraphAuthoringFieldDescriptor>(),
                Ports(In("local-pose", "Local Pose", "pose.local"), Out("component-pose", "Component Pose", "pose.component"))));
            catalog.Register(Node<CharacterComponentToLocalPosePayload>(CharacterPoseNodeKind.ComponentToLocalPose, allPoseGraphs, "Component To Local", "Pose Space", blendColor,
                Array.Empty<GraphAuthoringFieldDescriptor>(),
                Ports(In("component-pose", "Component Pose", "pose.component"), Out("local-pose", "Local Pose", "pose.local"))));
            catalog.Register(Node<CharacterLinkedPoseCallPayload>(CharacterPoseNodeKind.LinkedPoseCall, rootOnly, "Linked Pose Call", "Graph", blendColor,
                Fields(
                    Field("group-id", "Group", GraphAuthoringFieldValueKind.IdentityReference, "linked-pose-group"),
                    Field("interface-id", "Interface", GraphAuthoringFieldValueKind.IdentityReference, "linked-pose-interface"),
                    Field("entry-id", "Entry", GraphAuthoringFieldValueKind.IdentityReference, "linked-pose-entry")),
                Array.Empty<GraphAuthoringPortDescriptor>(),
                GraphAuthoringDynamicPortPolicy.OrderedBidirectional));
            catalog.Register(Node<CharacterPoseSubgraphPayload>(CharacterPoseNodeKind.PoseSubgraph, allPoseGraphsWithLinkedEntry, "Pose Subgraph", "Graph", blendColor,
                Fields(Field("graph-id", "Graph", GraphAuthoringFieldValueKind.IdentityReference, "pose-graph")),
                Array.Empty<GraphAuthoringPortDescriptor>(),
                GraphAuthoringDynamicPortPolicy.OrderedBidirectional,
                new[] { Child("open-subgraph", "Open Subgraph", Subgraph) }));
            catalog.Register(Node<CharacterGraphInputPosePayload>(CharacterPoseNodeKind.GraphInput, stateSubgraphAndLinkedEntry, "Graph Input", "Graph", inputColor,
                Array.Empty<GraphAuthoringFieldDescriptor>(), Array.Empty<GraphAuthoringPortDescriptor>(), GraphAuthoringDynamicPortPolicy.OrderedOutputs));
            catalog.Register(Node<CharacterGraphOutputPosePayload>(CharacterPoseNodeKind.GraphOutput, stateSubgraphAndLinkedEntry, "Graph Output", "Graph", outputColor,
                Array.Empty<GraphAuthoringFieldDescriptor>(), Array.Empty<GraphAuthoringPortDescriptor>(), GraphAuthoringDynamicPortPolicy.OrderedInputs));
            catalog.Register(Node<CharacterOutputPosePayload>(CharacterPoseNodeKind.OutputPose, rootAndState, "Output Pose", "Output", outputColor,
                Array.Empty<GraphAuthoringFieldDescriptor>(), Ports(In("pose", "Local Pose", "pose.local")),
                executionDomain: CharacterPoseExecutionDomain.FinalPublication));

            catalog.Register(Surface("pose.state-machine.entry", "Entry", GraphAuthoringNodePresentationKind.StateMachineEntry));
            catalog.Register(Surface(
                StateMachineState.Value,
                "State",
                GraphAuthoringNodePresentationKind.State,
                fields: Fields(BoolField("always-reset-on-entry", "Always Reset on Entry", true))));
            catalog.Register(Surface("pose.state-machine.alias", "State Alias", GraphAuthoringNodePresentationKind.StateAlias));
            catalog.Register(Surface(
                StateMachineTransition.Value,
                "Transition",
                GraphAuthoringNodePresentationKind.Standard,
                fields: Fields(
                    IntegerField("priority", "Priority", 0, 0),
                    EnumField("blend-logic", "Blend Logic", typeof(ThirdPersonCharacter.Animation.TransitionRouting.AnimationTransitionBlendLogic)),
                    FloatField("duration-seconds", "Duration", 0.1f, 0f),
                    EnumField("blend-mode", "Blend Mode", typeof(CharacterAnimationBlendMode)),
                    ConditionalAssetField(
                        "custom-blend-curve",
                        "Custom Blend Curve",
                        "animation-blend-curve",
                        typeof(CharacterAnimationBlendCurveAsset),
                        "blend-mode",
                    CharacterAnimationBlendMode.Custom.ToString()),
                    AssetField("blend-profile", "Blend Profile", "animation-blend-profile", typeof(CharacterAnimationBlendProfile)),
                    ReadOnlyField("source-readiness", "Source Readiness", GraphAuthoringFieldValueKind.Enum),
                    ReadOnlyField("pose-rule-id", "Pose Rule", GraphAuthoringFieldValueKind.IdentityReference))));
            foreach (PoseTransitionRuleOperationKind kind in
                     Enum.GetValues(typeof(PoseTransitionRuleOperationKind)))
            {
                catalog.Register(RuleOperation(kind));
            }
        }

        static IReadOnlyList<GraphAuthoringCommandDescriptor> SourceCommands() =>
            new[]
            {
                new GraphAuthoringCommandDescriptor(PingPoseSource, "Ping Source", false),
                new GraphAuthoringCommandDescriptor(OpenPoseSource, "Open Source", false),
                new GraphAuthoringCommandDescriptor(OpenPoseSourceProfile, "Open Profile Owner", false)
            };

        static GraphAuthoringCapabilityDescriptor Node<TPayload>(
            CharacterPoseNodeKind kind,
            IReadOnlyList<GraphAuthoringDocumentRoleId> roles,
            string displayName,
            string category,
            Color color,
            IReadOnlyList<GraphAuthoringFieldDescriptor> fields,
            IReadOnlyList<GraphAuthoringPortDescriptor> ports,
            GraphAuthoringDynamicPortPolicy dynamicPortPolicy = GraphAuthoringDynamicPortPolicy.None,
            IReadOnlyList<GraphAuthoringChildSurfaceDescriptor> childSurfaces = null,
            IReadOnlyList<GraphAuthoringCommandDescriptor> commands = null,
            CharacterPoseExecutionDomain executionDomain = CharacterPoseExecutionDomain.PurePose)
            where TPayload : CharacterPoseNodePayload, new()
        {
            if (new TPayload().Kind != kind)
            {
                throw new InvalidOperationException(
                    $"Pose capability '{Get(kind)}' payload type '{typeof(TPayload).FullName}' declares a different kind.");
            }
            return new GraphAuthoringCapabilityDescriptor(
                Get(kind),
                Domain,
                roles,
                displayName,
                category,
                color,
                fields,
                ports,
                dynamicPortPolicy,
                childSurfaces,
                commands: commands,
                mutationBindingId: "presentation.pose-node",
                validationBindingId: "presentation.pose-node",
                compilerBindingId: "presentation.pose-node." + ToKebabCase(kind.ToString()),
                documentCodecId: "presentation.pose-node",
                authoringType: typeof(TPayload),
                externalKind: Get(kind).Value,
                executionDomainId: executionDomain.ToString());
        }

        static GraphAuthoringCapabilityDescriptor Surface(
            string identity,
            string displayName,
            GraphAuthoringNodePresentationKind presentationKind,
            GraphAuthoringDocumentRoleId? role = null,
            IReadOnlyList<GraphAuthoringFieldDescriptor> fields = null)
        {
            return new GraphAuthoringCapabilityDescriptor(
                new GraphAuthoringCapabilityId(identity),
                Domain,
                new[] { role ?? StateMachine },
                displayName,
                "State Machine",
                new Color32(74, 91, 126, 255),
                fields,
                presentationKind: presentationKind,
                mutationBindingId: "presentation.pose-state-machine",
                validationBindingId: "presentation.pose-state-machine",
                compilerBindingId: "presentation.pose-state-machine",
                documentCodecId: "presentation.pose-state-machine");
        }

        static GraphAuthoringCapabilityDescriptor RuleOperation(
            PoseTransitionRuleOperationKind kind)
        {
            IReadOnlyList<GraphAuthoringFieldDescriptor> fields =
                kind switch
                {
                    PoseTransitionRuleOperationKind.FactInput =>
                        Fields(Field(
                            "fact-id",
                            "Presentation Fact",
                            GraphAuthoringFieldValueKind.IdentityReference,
                            "presentation-fact")),
                    PoseTransitionRuleOperationKind.BoolLiteral =>
                        Fields(BoolField(
                            "bool-literal",
                            "Value",
                            false)),
                    PoseTransitionRuleOperationKind.FloatLiteral =>
                        Fields(FloatField(
                            "float-literal",
                            "Value",
                            0f)),
                    PoseTransitionRuleOperationKind.EnumLiteral =>
                        Fields(
                            ReadOnlyField(
                                "enum-type-id",
                                "Enum Type",
                                GraphAuthoringFieldValueKind
                                    .IdentityReference),
                            EnumField(
                                "enum-literal",
                                "Value",
                                typeof(
                                    CharacterPresentationMotionPhase))),
                    PoseTransitionRuleOperationKind.IdentityLiteral =>
                        Fields(Field(
                            "identity-literal",
                            "Movement Mode",
                            GraphAuthoringFieldValueKind.IdentityReference,
                            "gameplay-state")),
                    _ => Array.Empty<
                        GraphAuthoringFieldDescriptor>()
                };
            IReadOnlyList<GraphAuthoringPortDescriptor> ports =
                kind switch
                {
                    PoseTransitionRuleOperationKind.Not =>
                        Ports(
                            In(
                                "input-a",
                                "Value",
                                "pose.rule.value"),
                            Out(
                                "result",
                                "Result",
                                "pose.rule.value")),
                    PoseTransitionRuleOperationKind.And or
                    PoseTransitionRuleOperationKind.Or or
                    PoseTransitionRuleOperationKind.Equal or
                    PoseTransitionRuleOperationKind.NotEqual or
                    PoseTransitionRuleOperationKind.Greater or
                    PoseTransitionRuleOperationKind.GreaterOrEqual or
                    PoseTransitionRuleOperationKind.Less or
                    PoseTransitionRuleOperationKind.LessOrEqual =>
                        Ports(
                            In(
                                "input-a",
                                "A",
                                "pose.rule.value"),
                            In(
                                "input-b",
                                "B",
                                "pose.rule.value"),
                            Out(
                                "result",
                                "Result",
                                "pose.rule.value")),
                    _ => Ports(
                        Out(
                            "result",
                            "Value",
                            "pose.rule.value"))
                };
            bool canBeOutput =
                kind == PoseTransitionRuleOperationKind.FactInput ||
                kind == PoseTransitionRuleOperationKind.BoolLiteral ||
                kind == PoseTransitionRuleOperationKind.Not ||
                kind == PoseTransitionRuleOperationKind.And ||
                kind == PoseTransitionRuleOperationKind.Or ||
                kind == PoseTransitionRuleOperationKind.Equal ||
                kind == PoseTransitionRuleOperationKind.NotEqual ||
                kind == PoseTransitionRuleOperationKind.Greater ||
                kind == PoseTransitionRuleOperationKind.GreaterOrEqual ||
                kind == PoseTransitionRuleOperationKind.Less ||
                kind == PoseTransitionRuleOperationKind.LessOrEqual;
            return new GraphAuthoringCapabilityDescriptor(
                Get(kind),
                Domain,
                new[] { TransitionRule },
                RuleOperationDisplayName(kind),
                "Transition Rule",
                new Color32(89, 74, 126, 255),
                fields,
                ports,
                commands: canBeOutput
                    ? new[]
                    {
                        new GraphAuthoringCommandDescriptor(
                            new GraphAuthoringCommandId(
                                "set-rule-output"),
                            "Set as Rule Output",
                            false)
                    }
                    : Array.Empty<
                        GraphAuthoringCommandDescriptor>(),
                presentationKind:
                    GraphAuthoringNodePresentationKind
                        .TransitionRule,
                mutationBindingId:
                    "presentation.pose-transition-rule",
                validationBindingId:
                    "presentation.pose-transition-rule",
                compilerBindingId:
                    "presentation.pose-transition-rule." +
                    ToKebabCase(kind.ToString()),
                documentCodecId:
                    "presentation.pose-transition-rule");
        }

        static string RuleOperationDisplayName(
            PoseTransitionRuleOperationKind kind) =>
            kind switch
            {
                PoseTransitionRuleOperationKind.FactInput =>
                    "Presentation Fact",
                PoseTransitionRuleOperationKind.BoolLiteral =>
                    "Bool Literal",
                PoseTransitionRuleOperationKind.FloatLiteral =>
                    "Float Literal",
                PoseTransitionRuleOperationKind.EnumLiteral =>
                    "Enum Literal",
                PoseTransitionRuleOperationKind.IdentityLiteral =>
                    "Identity Literal",
                PoseTransitionRuleOperationKind.TimeInState =>
                    "Time in State",
                PoseTransitionRuleOperationKind
                    .StatePoseRemainingTime =>
                    "State Pose Remaining Time",
                PoseTransitionRuleOperationKind.GreaterOrEqual =>
                    "Greater or Equal",
                PoseTransitionRuleOperationKind.LessOrEqual =>
                    "Less or Equal",
                PoseTransitionRuleOperationKind.NotEqual =>
                    "Not Equal",
                _ => kind.ToString()
            };

        static GraphAuthoringFieldDescriptor Field(string id, string name, GraphAuthoringFieldValueKind kind, string pickerKind, Type objectType = null) =>
            new GraphAuthoringFieldDescriptor(
                new GraphAuthoringFieldId(id),
                name,
                kind,
                GraphAuthoringFieldAccess.AuthoringRead | GraphAuthoringFieldAccess.AuthoringWrite,
                constraint: new GraphAuthoringFieldConstraint(
                    nonEmpty:
                    kind == GraphAuthoringFieldValueKind.IdentityReference ||
                    kind == GraphAuthoringFieldValueKind.AssetReference),
                pickerKind: pickerKind,
                objectType: objectType,
                tuning: Tuning(id, kind, GraphAuthoringFieldAccess.AuthoringRead | GraphAuthoringFieldAccess.AuthoringWrite));

        static GraphAuthoringFieldDescriptor SourceField(Type slotType) =>
            Field("pose-source-slot", "Pose Source", GraphAuthoringFieldValueKind.AssetReference, "pose-source-slot", slotType);
        static GraphAuthoringFieldDescriptor OptionalIdentityField(string id, string name, string pickerKind) =>
            new GraphAuthoringFieldDescriptor(
                new GraphAuthoringFieldId(id),
                name,
                GraphAuthoringFieldValueKind.IdentityReference,
                GraphAuthoringFieldAccess.AuthoringRead | GraphAuthoringFieldAccess.AuthoringWrite,
                defaultValue: string.Empty,
                pickerKind: pickerKind,
                optional: true,
                tuning: Tuning(id, GraphAuthoringFieldValueKind.IdentityReference, GraphAuthoringFieldAccess.AuthoringRead | GraphAuthoringFieldAccess.AuthoringWrite));
        static GraphAuthoringFieldDescriptor SelectionAvailabilityField() => EnumField("selection-availability", "Availability", typeof(AnimationSelectionAvailabilityPolicy));
        static GraphAuthoringFieldDescriptor AssetField(string id, string name, string pickerKind, Type objectType) =>
            Field(id, name, GraphAuthoringFieldValueKind.AssetReference, pickerKind, objectType);
        static GraphAuthoringFieldDescriptor ConditionalAssetField(
            string id,
            string name,
            string pickerKind,
            Type objectType,
            string controllerFieldId,
            string expectedValue) =>
            new GraphAuthoringFieldDescriptor(
                new GraphAuthoringFieldId(id),
                name,
                GraphAuthoringFieldValueKind.AssetReference,
                GraphAuthoringFieldAccess.AuthoringRead | GraphAuthoringFieldAccess.AuthoringWrite,
                constraint: new GraphAuthoringFieldConstraint(nonEmpty: true),
                pickerKind: pickerKind,
                objectType: objectType,
                visibility: new GraphAuthoringFieldVisibilityCondition(
                    new GraphAuthoringFieldId(controllerFieldId),
                    expectedValue),
                tuning: Tuning(id, GraphAuthoringFieldValueKind.AssetReference, GraphAuthoringFieldAccess.AuthoringRead | GraphAuthoringFieldAccess.AuthoringWrite));
        static GraphAuthoringFieldDescriptor StringField(string id, string name, string defaultValue) =>
            new GraphAuthoringFieldDescriptor(new GraphAuthoringFieldId(id), name, GraphAuthoringFieldValueKind.String, GraphAuthoringFieldAccess.AuthoringRead | GraphAuthoringFieldAccess.AuthoringWrite, defaultValue: defaultValue, constraint: new GraphAuthoringFieldConstraint(nonEmpty: true), tuning: Tuning(id, GraphAuthoringFieldValueKind.String, GraphAuthoringFieldAccess.AuthoringRead | GraphAuthoringFieldAccess.AuthoringWrite));
        static GraphAuthoringFieldDescriptor BoolField(string id, string name, bool defaultValue) =>
            new GraphAuthoringFieldDescriptor(new GraphAuthoringFieldId(id), name, GraphAuthoringFieldValueKind.Boolean, GraphAuthoringFieldAccess.AuthoringRead | GraphAuthoringFieldAccess.AuthoringWrite, defaultValue: defaultValue, tuning: Tuning(id, GraphAuthoringFieldValueKind.Boolean, GraphAuthoringFieldAccess.AuthoringRead | GraphAuthoringFieldAccess.AuthoringWrite));
        static GraphAuthoringFieldDescriptor FloatField(string id, string name, float defaultValue, float? minimum = null, float? maximum = null) =>
            new GraphAuthoringFieldDescriptor(new GraphAuthoringFieldId(id), name, GraphAuthoringFieldValueKind.Float, GraphAuthoringFieldAccess.AuthoringRead | GraphAuthoringFieldAccess.AuthoringWrite, defaultValue: defaultValue, constraint: new GraphAuthoringFieldConstraint(minimum, maximum, true), tuning: Tuning(id, GraphAuthoringFieldValueKind.Float, GraphAuthoringFieldAccess.AuthoringRead | GraphAuthoringFieldAccess.AuthoringWrite, minimum ?? double.MinValue, maximum ?? double.MaxValue));
        static GraphAuthoringFieldDescriptor IntegerField(string id, string name, int defaultValue, int? minimum = null, int? maximum = null) =>
            new GraphAuthoringFieldDescriptor(new GraphAuthoringFieldId(id), name, GraphAuthoringFieldValueKind.Integer, GraphAuthoringFieldAccess.AuthoringRead | GraphAuthoringFieldAccess.AuthoringWrite, defaultValue: defaultValue, constraint: new GraphAuthoringFieldConstraint(minimum, maximum), tuning: Tuning(id, GraphAuthoringFieldValueKind.Integer, GraphAuthoringFieldAccess.AuthoringRead | GraphAuthoringFieldAccess.AuthoringWrite, minimum ?? int.MinValue, maximum ?? int.MaxValue));
        static GraphAuthoringFieldDescriptor ReadOnlyField(string id, string name, GraphAuthoringFieldValueKind kind) =>
            new GraphAuthoringFieldDescriptor(new GraphAuthoringFieldId(id), name, kind, GraphAuthoringFieldAccess.AuthoringRead, tuning: Tuning(id, kind, GraphAuthoringFieldAccess.AuthoringRead));
        static GraphAuthoringFieldDescriptor Vector3Field(string id, string name, Vector3 defaultValue = default) =>
            new GraphAuthoringFieldDescriptor(new GraphAuthoringFieldId(id), name, GraphAuthoringFieldValueKind.Vector3, GraphAuthoringFieldAccess.AuthoringRead | GraphAuthoringFieldAccess.AuthoringWrite, defaultValue: defaultValue, tuning: Tuning(id, GraphAuthoringFieldValueKind.Vector3, GraphAuthoringFieldAccess.AuthoringRead | GraphAuthoringFieldAccess.AuthoringWrite));
        static GraphAuthoringFieldDescriptor EnumField(string id, string name, Type enumType) =>
            new GraphAuthoringFieldDescriptor(new GraphAuthoringFieldId(id), name, GraphAuthoringFieldValueKind.Enum, GraphAuthoringFieldAccess.AuthoringRead | GraphAuthoringFieldAccess.AuthoringWrite, constraint: new GraphAuthoringFieldConstraint(allowedValues: Enum.GetNames(enumType)), tuning: Tuning(id, GraphAuthoringFieldValueKind.Enum, GraphAuthoringFieldAccess.AuthoringRead | GraphAuthoringFieldAccess.AuthoringWrite));

        static GraphAuthoringFieldTuningMetadata Tuning(
            string id,
            GraphAuthoringFieldValueKind kind,
            GraphAuthoringFieldAccess access,
            double minimum = double.MinValue,
            double maximum = double.MaxValue)
        {
            bool writable = (access & GraphAuthoringFieldAccess.AuthoringWrite) != 0;
            bool tunable = writable &&
                (string.Equals(id, "weight", StringComparison.Ordinal) ||
                 string.Equals(id, "play-rate", StringComparison.Ordinal) ||
                 string.Equals(id, "duration-seconds", StringComparison.Ordinal));
            GraphAuthoringFieldInteractionPolicy interaction = !writable
                ? GraphAuthoringFieldInteractionPolicy.DerivedReadOnly
                : tunable
                    ? GraphAuthoringFieldInteractionPolicy.TunableDefault
                    : GraphAuthoringFieldInteractionPolicy.Structural;
            string unit = string.Equals(id, "weight", StringComparison.Ordinal)
                ? "normalized"
                : string.Equals(id, "play-rate", StringComparison.Ordinal)
                    ? "multiplier"
                    : string.Equals(id, "duration-seconds", StringComparison.Ordinal)
                        ? "seconds"
                        : string.Empty;
            GraphAuthoringFieldApplyTiming timing =
                string.Equals(id, "duration-seconds", StringComparison.Ordinal)
                    ? GraphAuthoringFieldApplyTiming.NextActivation
                    : GraphAuthoringFieldApplyTiming.NextFrame;
            if (kind != GraphAuthoringFieldValueKind.Float &&
                kind != GraphAuthoringFieldValueKind.Integer)
            {
                minimum = 0d;
                maximum = 1d;
            }
            return new GraphAuthoringFieldTuningMetadata(
                interaction,
                kind,
                unit,
                minimum,
                maximum,
                true,
                timing,
                GraphAuthoringFieldStatePolicy.PreserveState,
                "pose-graph",
                0,
                0);
        }


        static GraphAuthoringPortDescriptor In(string id, string name, string valueType) => Port(id, name, valueType, GraphAuthoringPortDirection.Input, true);
        static GraphAuthoringPortDescriptor OptionalIn(string id, string name, string valueType) => Port(id, name, valueType, GraphAuthoringPortDirection.Input, false);
        static GraphAuthoringPortDescriptor Out(string id, string name, string valueType) => Port(id, name, valueType, GraphAuthoringPortDirection.Output, false);
        static GraphAuthoringPortDescriptor Port(string id, string name, string valueType, GraphAuthoringPortDirection direction, bool required) =>
            new GraphAuthoringPortDescriptor(new GraphAuthoringPortId(id), name, valueType, direction, direction == GraphAuthoringPortDirection.Input ? GraphAuthoringPortCapacity.Single : GraphAuthoringPortCapacity.Multiple, required, 0);
        static GraphAuthoringPortDescriptor[] Ports(
            params GraphAuthoringPortDescriptor[] ports)
        {
            var ordered =
                new GraphAuthoringPortDescriptor[ports.Length];
            for (int i = 0; i < ports.Length; i++)
            {
                GraphAuthoringPortDescriptor port = ports[i];
                ordered[i] = new GraphAuthoringPortDescriptor(
                    port.PortId,
                    port.DisplayName,
                    port.ValueTypeId,
                    port.Direction,
                    port.Capacity,
                    port.Required,
                    i);
            }
            return ordered;
        }
        static GraphAuthoringFieldDescriptor[] Fields(params GraphAuthoringFieldDescriptor[] fields) => fields;
        static GraphAuthoringChildSurfaceDescriptor Child(string id, string name, GraphAuthoringDocumentRoleId role) =>
            new GraphAuthoringChildSurfaceDescriptor(new GraphAuthoringCommandId(id), role, name);
        static GraphAuthoringPortDescriptor[] UnaryLocalPosePorts() => Ports(In("pose", "Local Pose", "pose.local"), Out("result", "Local Pose", "pose.local"));
        static GraphAuthoringPortDescriptor[] UnaryComponentPoseWithWeight() => Ports(In("pose", "Component Pose", "pose.component"), OptionalIn("weight", "Weight", "pose.parameter"), Out("result", "Component Pose", "pose.component"));
        static GraphAuthoringPortDescriptor[] BinaryLocalPoseWithWeight(string first, string second) => Ports(In("base", first + " Local Pose", "pose.local"), In("overlay", second + " Local Pose", "pose.local"), OptionalIn("weight", "Weight", "pose.parameter"), Out("result", "Local Pose", "pose.local"));

        static string ToKebabCase(string value)
        {
            var characters = new List<char>(value.Length + 8);
            for (int i = 0; i < value.Length; i++)
            {
                char current = value[i];
                if (i > 0 && char.IsUpper(current))
                    characters.Add('-');
                characters.Add(char.ToLowerInvariant(current));
            }
            return new string(characters.ToArray());
        }

    }

    internal static class CharacterPoseAuthoringPortProjection
    {
        public static IReadOnlyList<CharacterPosePortDefinition> Get(
            CharacterTypedPoseNode node)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));
            IEnumerable<CharacterPosePortDefinition> fixedPorts =
                CharacterPoseGraphAuthoringCapabilities
                    .Require(node.Kind)
                    .FixedPorts
                    .OrderBy(value => value.Order)
                    .Select(ToPosePort);
            IEnumerable<CharacterPosePortDefinition> dynamicPorts =
                node.DynamicPorts
                    .OrderBy(value => value.Order)
                    .Select(value =>
                        new CharacterPosePortDefinition(
                            value.PortId,
                            value.DisplayName,
                            value.Kind,
                            value.Direction,
                            value.Required,
                            value.InterfacePortId));
            return fixedPorts.Concat(dynamicPorts).ToArray();
        }

        public static IReadOnlyList<CharacterPosePortDefinition>
            GetFixed(CharacterPoseNodeKind kind) =>
            CharacterPoseGraphAuthoringCapabilities
                .Require(kind)
                .FixedPorts
                .OrderBy(value => value.Order)
                .Select(ToPosePort)
                .ToArray();

        public static CharacterPosePortDefinition Require(
            CharacterTypedPoseNode node,
            string portId,
            CharacterPosePortDirection? direction = null)
        {
            CharacterPosePortDefinition port = Get(node)
                .SingleOrDefault(value =>
                    string.Equals(
                        value.PortId.Value,
                        portId,
                        StringComparison.Ordinal) &&
                    (!direction.HasValue ||
                     value.Direction == direction.Value));
            return port ??
                   throw new InvalidOperationException(
                       $"Pose node '{node.NodeId}' does not declare port '{portId}'.");
        }

        public static CharacterPosePortKind Kind(string valueTypeId) =>
            valueTypeId switch
            {
                "pose.local" => CharacterPosePortKind.LocalPose,
                "pose.component" => CharacterPosePortKind.ComponentPose,
                "pose.parameter" =>
                    CharacterPosePortKind.Parameter,
                "pose.discontinuity" =>
                    CharacterPosePortKind.PoseDiscontinuity,
                "pose.action-playback" =>
                    CharacterPosePortKind.ActionPlayback,
                "component.full-body-ik-goals" =>
                    CharacterPosePortKind.FullBodyIkGoals,
                "component.full-body-ik-goal-contribution" =>
                    CharacterPosePortKind.FullBodyIkGoalContribution,
                "pose.history" => CharacterPosePortKind.PoseHistory,
                "motion-matching.trajectory" => CharacterPosePortKind.Trajectory,
                "presentation.facts" => CharacterPosePortKind.PresentationFacts,
                "motion-matching.binding" => CharacterPosePortKind.MotionMatchingBinding,
                _ => throw new InvalidOperationException(
                    $"Pose value type '{valueTypeId}' is not registered.")
            };

        public static string ValueType(CharacterPosePortKind kind) =>
            kind switch
            {
                CharacterPosePortKind.LocalPose => "pose.local",
                CharacterPosePortKind.ComponentPose => "pose.component",
                CharacterPosePortKind.Parameter =>
                    "pose.parameter",
                CharacterPosePortKind.PoseDiscontinuity =>
                    "pose.discontinuity",
                CharacterPosePortKind.ActionPlayback =>
                    "pose.action-playback",
                CharacterPosePortKind.FullBodyIkGoals =>
                    "component.full-body-ik-goals",
                CharacterPosePortKind.FullBodyIkGoalContribution =>
                    "component.full-body-ik-goal-contribution",
                CharacterPosePortKind.PoseHistory => "pose.history",
                CharacterPosePortKind.Trajectory => "motion-matching.trajectory",
                CharacterPosePortKind.PresentationFacts => "presentation.facts",
                CharacterPosePortKind.MotionMatchingBinding => "motion-matching.binding",
                _ => throw new InvalidOperationException(
                    $"Pose port kind '{kind}' is not registered.")
            };

        static CharacterPosePortDefinition ToPosePort(
            GraphAuthoringPortDescriptor port) =>
            new CharacterPosePortDefinition(
                new PosePortId(port.PortId.Value),
                port.DisplayName,
                Kind(port.ValueTypeId),
                port.Direction ==
                GraphAuthoringPortDirection.Input
                    ? CharacterPosePortDirection.Input
                    : CharacterPosePortDirection.Output,
                port.Required,
                string.IsNullOrWhiteSpace(port.InterfacePortId)
                    ? default
                    : new PoseInterfacePortId(port.InterfacePortId));
    }
}
