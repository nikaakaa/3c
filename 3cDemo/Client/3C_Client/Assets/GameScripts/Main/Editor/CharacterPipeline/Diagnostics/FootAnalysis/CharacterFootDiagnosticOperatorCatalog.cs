using KK.GeneratedDiagnosticSampling.Host;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.FootAnalysis.Editor
{
    public static class CharacterFootDiagnosticOperatorCatalog
    {
        public static DiagnosticOperatorRegistry Create()
        {
            var registry = new DiagnosticOperatorRegistry();
            registry.Register(new CharacterFootFinalContactPlanePenetrationOperator());
            registry.Register(new CharacterFootContactPlanePenetrationContributionOperator());
            registry.Register(new CharacterFootLockedHorizontalDriftOperator());
            registry.Register(new CharacterFootLockedVerticalAnchorEvidenceOperator());
            return registry;
        }
    }
}
