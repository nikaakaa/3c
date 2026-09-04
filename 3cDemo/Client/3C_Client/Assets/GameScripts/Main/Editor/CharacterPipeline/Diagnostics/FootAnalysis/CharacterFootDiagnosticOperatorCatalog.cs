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
            registry.Register(new CharacterFootLandingPathContinuityOperator());
            registry.Register(new CharacterFootLandingPathXzDistanceOperator());
            registry.Register(new CharacterFootLateApproachLandingRevisionOperator());
            registry.Register(new CharacterFootLandingStateConsistencyOperator());
            registry.Register(new CharacterFootContactSupportGapOperator());
            registry.Register(new CharacterFootContactStateOutputJumpOperator());
            registry.Register(new CharacterFootStableSwingOutputJumpOperator());
            registry.Register(new CharacterFootPathRevisionOutputJumpOperator());
            registry.Register(new CharacterFootPathRevisionAmplificationEvidenceOperator());
            registry.Register(new CharacterFootSwingActualEnvelopeCounterfactualOperator());
            registry.Register(new CharacterFootSwingCorrectionCadenceOperator());
            registry.Register(new CharacterFootStepTimeSelectionOperator());
            registry.Register(new CharacterFootLandingLegExtensionOperator());
            registry.Register(new CharacterFootSameLevelFeetPelvisDescentOperator());
            registry.Register(new CharacterFootPelvisResponseChainOperator());
            registry.Register(new CharacterFootContactLifecycleChatterOperator());
            registry.Register(new CharacterFootReleaseFlybackOperator());
            registry.Register(new CharacterFootSwingToLandingFloorHandoffOperator());
            registry.Register(new CharacterFootPlantInterpolationOutputJumpOperator());
            registry.Register(new CharacterFootContactAcquisitionContinuityOperator());
            registry.Register(new CharacterFootLockWeightCompletionByContactEventOperator());
            registry.Register(new CharacterFootApproachProgressOwnershipOperator());
            registry.Register(new CharacterFootActionHardOwnershipOperator());
            registry.Register(new CharacterFootContactTransitionContextOperator());
            registry.Register(new CharacterFootFormalGoalWeightPolicyOperator());
            registry.Register(new CharacterFootContactReentryOutputGeometryOperator());
            return registry;
        }
    }
}
