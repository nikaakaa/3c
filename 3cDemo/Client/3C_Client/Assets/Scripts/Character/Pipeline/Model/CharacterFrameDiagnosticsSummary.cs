namespace ThirdPersonAction
{
    internal static class CharacterFrameDiagnosticsSummary
    {
        public static string Build(in CharacterFrameContext context)
        {
            return
                $"step={context.Step} phase={context.CurrentStep} success={context.Success} " +
                $"request={context.InputRequest.RequestKind} hasRequest={context.InputRequest.HasRequest} consumed={context.InputRequestConsumed} " +
                $"owner={context.StateFrame.Owner.Kind} action={context.StateFrame.ActionState.Value} " +
                $"actionMotion={context.ActionMotionResult.HasSpec}/{context.ActionMotionResult.HasActionMovement}/{context.ActionMotionResult.ActionCompleted} " +
                $"motionAction={context.ActionMovementExecuted} motionBasic={context.BasicMovementExecuted} " +
                $"presentAction={context.ActionAnimationPresented} presentLocomotion={context.LocomotionAnimationPresented} animationFacts={context.AnimationFactsWritten}";
        }
    }
}
