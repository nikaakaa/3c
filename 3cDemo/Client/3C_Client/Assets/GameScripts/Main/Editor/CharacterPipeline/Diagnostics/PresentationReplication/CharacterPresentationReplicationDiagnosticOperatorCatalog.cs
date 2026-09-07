using KK.GeneratedDiagnosticSampling.Host;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.PresentationReplication.Editor
{
    public static class CharacterPresentationReplicationDiagnosticOperatorCatalog
    {
        public static DiagnosticOperatorRegistry Create()
        {
            var registry = new DiagnosticOperatorRegistry();
            registry.Register(
                new CharacterPresentationAnimationOutputValidityOperator());
            registry.Register(
                new CharacterPresentationAnimationParameterAvailabilityOperator());
            registry.Register(
                new CharacterPresentationAnimationStateContinuityOperator());
            registry.Register(
                new CharacterPresentationCameraPlanContinuityOperator());
            registry.Register(
                new CharacterPresentationCameraCueLifecycleOperator());
            registry.Register(
                new CharacterPresentationCameraCueRoutingOperator());
            return registry;
        }
    }
}
