namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal static class CharacterFootPlacementDiagnosticFormatter
    {
        public static string Format(CharacterFootPlacementFrameSnapshot snapshot)
        {
            return
                $"body={snapshot.PreviousBodyTick}->{snapshot.CurrentBodyTick};reset={snapshot.ResetSequence};" +
                $"pose={snapshot.PosePlanHash}/{snapshot.CompletionIdentity}/{snapshot.PoseContinuityIdentity};" +
                $"placement={snapshot.FootPlacementWeightParameterId}[{snapshot.FootPlacementWeightParameterIndex}]={snapshot.FootPlacementWeight:0.###};" +
                $"calibration={snapshot.CalibrationId}/{snapshot.CalibrationRevision};" +
                $"analysis={snapshot.AnalysisSourceId}/v{snapshot.AnalysisVersion}/{snapshot.AnalysisAlgorithmVersion};" +
                $"contributions={snapshot.ContributionCount}/{ResolveContributionWeight(snapshot):0.###};" +
                $"leftHeelToe={snapshot.Left.HeelSupportIdentity}/{snapshot.Left.ToeSupportIdentity};" +
                $"rightHeelToe={snapshot.Right.HeelSupportIdentity}/{snapshot.Right.ToeSupportIdentity};" +
                $"leftSurface={snapshot.Left.CurrentSupportIdentity}->{snapshot.Left.FutureSupportIdentity};" +
                $"rightSurface={snapshot.Right.CurrentSupportIdentity}->{snapshot.Right.FutureSupportIdentity};" +
                $"leftReason={snapshot.Left.TransitionReason};rightReason={snapshot.Right.TransitionReason};" +
                $"leftGenerated={snapshot.Left.GeneratedPlantConfidence:0.###}/{snapshot.Left.GeneratedLandingDelay:0.###}/{snapshot.Left.GeneratedLandingOffset}/{snapshot.Left.GeneratedSoleWorldVelocity};" +
                $"rightGenerated={snapshot.Right.GeneratedPlantConfidence:0.###}/{snapshot.Right.GeneratedLandingDelay:0.###}/{snapshot.Right.GeneratedLandingOffset}/{snapshot.Right.GeneratedSoleWorldVelocity};" +
                $"leftEnvelope={snapshot.Left.GroundEnvelopeSegmentCount}/{snapshot.Left.GroundEnvelopeRejectReason};" +
                $"rightEnvelope={snapshot.Right.GroundEnvelopeSegmentCount}/{snapshot.Right.GroundEnvelopeRejectReason};" +
                $"leftConstraint={snapshot.Left.AnkleTwistDegrees:0.###}/{snapshot.Left.HeelLiftDistance:0.###}/{snapshot.Left.SeparationCorrection:0.###};" +
                $"rightConstraint={snapshot.Right.AnkleTwistDegrees:0.###}/{snapshot.Right.HeelLiftDistance:0.###}/{snapshot.Right.SeparationCorrection:0.###};" +
                $"leftPrediction={snapshot.Left.PredictionHorizon:0.####}/clamped:{snapshot.Left.PredictionHorizonClamped}/reject:{snapshot.Left.PredictionRejectReason};" +
                $"rightPrediction={snapshot.Right.PredictionHorizon:0.####}/clamped:{snapshot.Right.PredictionHorizonClamped}/reject:{snapshot.Right.PredictionRejectReason};" +
                $"bodyGrounded={snapshot.BodyGroundedBefore}->{snapshot.BodyGroundedAfter};" +
                $"bodyDelta={snapshot.BodySourceTranslationDelta}/{snapshot.BodyVisibleTranslationDelta};" +
                $"actorComp={snapshot.ActorMovementCompensationMode}/{snapshot.ActorMovementCompensationTargetOffset:0.####}/{snapshot.ActorMovementCompensationCurrentOffset:0.####}/{snapshot.ActorMovementCompensationVelocity:0.####};" +
                $"pelvisReach={snapshot.PelvisReachTargetOffset:0.####}/{snapshot.PelvisReachCurrentOffset:0.####};" +
                $"pelvisHeight={snapshot.PelvisHeightMode}/{snapshot.PelvisHeightDecision}/{snapshot.PelvisHeightReason};" +
                $"pelvisEvidence={snapshot.PelvisDirectionalSpeed:0.####}/{snapshot.PelvisFootLeadDistance:0.####}/{snapshot.PelvisSlopeHeightDifference:0.####};" +
                $"pelvis={snapshot.PelvisTargetOffset:0.####}/{snapshot.PelvisCurrentOffset:0.####};support={snapshot.SupportFoot};" +
                $"queries={snapshot.Left.QueryCount + snapshot.Right.QueryCount}";
        }

        static float ResolveContributionWeight(CharacterFootPlacementFrameSnapshot snapshot)
        {
            float weight = 0f;
            for (int i = 0; i < snapshot.ContributionCount; i++)
                weight += snapshot.GetContribution(i).Weight;
            return weight;
        }
    }
}
