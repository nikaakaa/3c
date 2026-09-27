using System;
using System.Linq;
using BTSMTL.Authoring.Graph;
using ThirdPersonCharacter.Pipeline.Animation;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    internal static class CharacterPoseGraphCapabilityProjector
    {
        static bool s_Registered;

        public static GraphAuthoringCapabilityCatalog Catalog
        {
            get
            {
                EnsureRegistered();
                return GraphAuthoringCapabilityRegistrationRoot.Catalog;
            }
        }

        public static void EnsureRegistered()
        {
            if (s_Registered)
                return;
            GraphAuthoringCapabilityRegistrationRoot.RegisterDomain(
                "character-presentation.pose",
                ProjectCatalog);
            s_Registered = true;
        }

        static void ProjectCatalog(GraphAuthoringCapabilityCatalog catalog)
        {
            foreach (CharacterPoseNodeDefinition definition in
                     CharacterPoseNodeDefinitionModule.Shared.Declarations)
            {
                catalog.Register(definition.Declare());
            }
            catalog.Register(CharacterPoseCapabilityDeclarations.Surface(
                "pose.state-machine.entry",
                "Entry",
                GraphAuthoringNodePresentationKind.StateMachineEntry));
            catalog.Register(CharacterPoseCapabilityDeclarations.Surface(
                CharacterPoseGraphAuthoringCapabilities.StateMachineState.Value,
                "State",
                GraphAuthoringNodePresentationKind.State,
                fields: CharacterPoseCapabilityDeclarations.Fields(
                    CharacterPoseCapabilityDeclarations.StringField("display-name", "名称", "State"),
                    CharacterPoseCapabilityDeclarations.BoolField("always-reset-on-entry", "Always Reset on Entry", true))));
            catalog.Register(CharacterPoseCapabilityDeclarations.Surface(
                "pose.state-machine.alias",
                "State Alias",
                GraphAuthoringNodePresentationKind.StateAlias));
            catalog.Register(CharacterPoseCapabilityDeclarations.Surface(
                CharacterPoseGraphAuthoringCapabilities.StateMachineTransition.Value,
                "Transition",
                GraphAuthoringNodePresentationKind.Standard,
                fields: CharacterPoseCapabilityDeclarations.Fields(
                    CharacterPoseCapabilityDeclarations.IntegerField("priority", "Priority", 0, 0),
                    CharacterPoseCapabilityDeclarations.EnumField("blend-logic", "Blend Logic", typeof(ThirdPersonCharacter.Animation.TransitionRouting.AnimationTransitionBlendLogic)),
                    CharacterPoseCapabilityDeclarations.FloatField("duration-seconds", "Duration", 0.1f, 0f),
                    CharacterPoseCapabilityDeclarations.EnumField("blend-mode", "Blend Mode", typeof(CharacterAnimationBlendMode)),
                    CharacterPoseCapabilityDeclarations.ConditionalAssetField(
                        "custom-blend-curve",
                        "Custom Blend Curve",
                        "pose-resource-slot",
                        typeof(CharacterPoseResourceSlot),
                        "blend-mode",
                        CharacterAnimationBlendMode.Custom.ToString()),
                    CharacterPoseCapabilityDeclarations.ResourceField("blend-profile", "Blend Profile"),
                    CharacterPoseCapabilityDeclarations.ReadOnlyField("source-readiness", "Source Readiness", GraphAuthoringFieldValueKind.Enum),
                    CharacterPoseCapabilityDeclarations.ReadOnlyField("pose-rule-id", "Pose Rule", GraphAuthoringFieldValueKind.IdentityReference))));
            foreach (PoseTransitionRuleOperationKind kind in
                     Enum.GetValues(typeof(PoseTransitionRuleOperationKind)))
            {
                catalog.Register(CharacterPoseCapabilityDeclarations.RuleOperation(kind));
            }
        }

        public static GraphAuthoringCapabilityDescriptor Require(
            CharacterPoseNodeKind kind) =>
            Catalog.Require(CharacterPoseGraphAuthoringCapabilities.Get(kind));

        public static Type RequirePayloadType(
            CharacterPoseNodeKind kind) =>
            Require(kind).AuthoringType;

        public static CharacterPoseNodeKind RequireKind(
            CharacterPoseNodePayload payload) =>
            payload != null && Require(payload.Kind).AuthoringType == payload.GetType()
                ? payload.Kind
                : throw new InvalidOperationException("Pose payload does not match its registered schema.");

        public static bool IsDocumentNodeRetired(string capability)
        {
            GraphAuthoringCapabilityDescriptor descriptor = Catalog.Require(
                new GraphAuthoringCapabilityId(capability));
            return descriptor.AuthoringType != null &&
                   descriptor.AuthoringType
                       .GetCustomAttributes(
                           typeof(CharacterPoseDocumentPolicyAttribute),
                           false)
                       .OfType<CharacterPoseDocumentPolicyAttribute>()
                       .Any(value => value.Retired);
        }
    }
}
