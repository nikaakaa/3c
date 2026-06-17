using ThirdPersonAnimation;
using ThirdPersonCharacterConfig;
using ThirdPersonCharacterStateMachine;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonMovement
{
    internal interface ILocomotionMotionOutputDependencies
    {
        IBasicLocomotionMotionExecutor MotionExecutor { get; }
        bool SuppressBasicMotionExecution { get; }
        void ResolveMotionExecutor();
    }

    internal interface ILocomotionAnimationOutputDependencies
    {
        IBasicLocomotionMotionExecutor MotionExecutor { get; }
        bool SuppressLocomotionAnimationPresentation { get; }
        RunLocomotionAnimationConfigSO AnimationConfig { get; }
        CharacterRuntimeBlackboardSnapshot RuntimeBlackboardSnapshot { get; }
        BasicMovementGait CurrentGait { get; }
        void PresentAnimation(in MovementAnimationContext context);
    }

    internal interface ILocomotionRuntimeBlackboardDependencies
    {
        RunLocomotionAnimationConfigSO AnimationConfig { get; }
        CharacterRuntimeBlackboardSnapshot RuntimeBlackboardSnapshot { get; }
        BasicMovementGait CurrentGait { get; }
        void WriteActionFactsToBlackboard(in CharacterRuntimeActionFacts facts);
        void WriteAnimationFactsToBlackboard(in CharacterRuntimeAnimationFacts facts);
        void WriteLocomotionPreemptionFactToBlackboard(in LocomotionPreemptionFact fact);
    }

    internal interface ILocomotionOutputCompletionDependencies
    {
        LocomotionRuntimeStateStore StateStore { get; }
        bool HasCameraController { get; }
        bool IsRollbackCameraBasisOverrideActive { get; }
        string CurrentAnimationName { get; }
        void ResolveCamera();
        void SyncRollbackCameraBasis();
    }

    internal sealed class LocomotionOutputRuntime
    {
        readonly LocomotionMotionOutputApplier motionOutputApplier;
        readonly LocomotionAnimationOutputPresenter animationOutputPresenter;
        readonly LocomotionRuntimeBlackboardWriter blackboardWriter;
        readonly LocomotionOutputCompletion outputCompletion;

        public LocomotionOutputRuntime(
            LocomotionMotionOutputApplier motionOutputApplier,
            LocomotionAnimationOutputPresenter animationOutputPresenter,
            LocomotionRuntimeBlackboardWriter blackboardWriter,
            LocomotionOutputCompletion outputCompletion)
        {
            this.motionOutputApplier = motionOutputApplier;
            this.animationOutputPresenter = animationOutputPresenter;
            this.blackboardWriter = blackboardWriter;
            this.outputCompletion = outputCompletion;
        }

        public void ExecuteLocomotionMotion(in BasicLocomotionFrame frame)
        {
            motionOutputApplier.Apply(in frame);
        }

        public void PresentLocomotionAnimation(in BasicLocomotionFrame frame)
        {
            animationOutputPresenter.Present(in frame);
        }

        public void SetRunLatchActive(bool active)
        {
            outputCompletion.SetRunLatchActive(active);
        }

        public void WriteActionFacts(in CharacterRuntimeActionFacts facts)
        {
            blackboardWriter.WriteActionFacts(in facts);
        }

        public void WriteAnimationFacts(in CharacterRuntimeAnimationFacts facts)
        {
            blackboardWriter.WriteAnimationFacts(in facts);
        }

        public void WriteLocomotionPreemptionFact(in LocomotionPreemptionFact fact)
        {
            blackboardWriter.WriteLocomotionPreemptionFact(in fact);
        }

        public void CompleteLocomotionTick()
        {
            outputCompletion.Complete();
        }
    }

    internal sealed class LocomotionOutputRuntimeAdapter : ILocomotionOutputRuntimePort
    {
        readonly LocomotionOutputRuntime runtime;

        public LocomotionOutputRuntimeAdapter(LocomotionOutputRuntime runtime)
        {
            this.runtime = runtime;
        }

        public void ExecuteLocomotionMotion(in BasicLocomotionFrame frame)
        {
            runtime.ExecuteLocomotionMotion(in frame);
        }

        public void PresentLocomotionAnimation(in BasicLocomotionFrame frame)
        {
            runtime.PresentLocomotionAnimation(in frame);
        }

        public void SetRunLatchActive(bool active)
        {
            runtime.SetRunLatchActive(active);
        }

        public void WriteActionFacts(in CharacterRuntimeActionFacts facts)
        {
            runtime.WriteActionFacts(in facts);
        }

        public void WriteAnimationFacts(in CharacterRuntimeAnimationFacts facts)
        {
            runtime.WriteAnimationFacts(in facts);
        }

        public void WriteLocomotionPreemptionFact(in LocomotionPreemptionFact fact)
        {
            runtime.WriteLocomotionPreemptionFact(in fact);
        }

        public void CompleteLocomotionTick()
        {
            runtime.CompleteLocomotionTick();
        }
    }

    internal sealed class LocomotionMotionOutputApplier
    {
        readonly ILocomotionMotionOutputDependencies dependencies;

        public LocomotionMotionOutputApplier(ILocomotionMotionOutputDependencies dependencies)
        {
            this.dependencies = dependencies;
        }

        public void Apply(in BasicLocomotionFrame frame)
        {
            if (dependencies.MotionExecutor == null)
                dependencies.ResolveMotionExecutor();

            IBasicLocomotionMotionExecutor executor = dependencies.MotionExecutor;
            if (executor == null || dependencies.SuppressBasicMotionExecution)
                return;

            MovementCommand command = frame.Command;
            executor.ExecuteBasicMovement(in command);
        }
    }

    internal sealed class LocomotionAnimationOutputPresenter
    {
        readonly ILocomotionAnimationOutputDependencies dependencies;

        public LocomotionAnimationOutputPresenter(ILocomotionAnimationOutputDependencies dependencies)
        {
            this.dependencies = dependencies;
        }

        public void Present(in BasicLocomotionFrame frame)
        {
            if (dependencies.SuppressLocomotionAnimationPresentation)
                return;

            float currentSpeed = dependencies.MotionExecutor != null
                ? dependencies.MotionExecutor.CurrentSpeed
                : frame.Command.PlanarSpeed;
            MovementAnimationContext context = BuildAnimationContext(in frame, currentSpeed);
            dependencies.PresentAnimation(in context);
        }

        MovementAnimationContext BuildAnimationContext(in BasicLocomotionFrame frame, float planarSpeed)
        {
            bool hasEntryFootPhaseMatchRequest = TryResolveRunLoopEntryFootPhaseMatch(
                in frame,
                out LocomotionFootPhaseMatchResult entryFootPhaseMatchResult);

            return new MovementAnimationContext(
                frame.Phase,
                frame.Command.Gait,
                frame.Intent.HasMoveIntent,
                frame.Intent.Strength,
                frame.WorldDirection,
                planarSpeed,
                frame.Command.TurnBackMotionPolicy,
                frame.Command.HasTurnBackMotionPolicy,
                entryFootPhaseMatchResult,
                hasEntryFootPhaseMatchRequest,
                dependencies.AnimationConfig);
        }

        bool TryResolveRunLoopEntryFootPhaseMatch(
            in BasicLocomotionFrame frame,
            out LocomotionFootPhaseMatchResult result)
        {
            result = LocomotionFootPhaseMatchResult.NotRequested;
            if (frame.Phase != BasicMovementPhase.MoveLoop || frame.Command.Gait != BasicMovementGait.Run)
                return false;

            RunLocomotionAnimationConfigSO animationConfig = dependencies.AnimationConfig;
            string runLoopAlias = LocomotionAnimationAliasResolver.ResolveAliasKey(
                animationConfig,
                BasicMovementPhase.MoveLoop,
                BasicMovementGait.Run);
            CharacterRuntimeAnimationFacts previousAnimation = dependencies.RuntimeBlackboardSnapshot.Animation;
            bool previousWasTurnBack =
                previousAnimation.LocomotionProgress.Phase == BasicMovementPhase.TurnBack ||
                previousAnimation.CurrentLocomotionFootPhase.Phase == BasicMovementPhase.TurnBack;
            if (!previousWasTurnBack)
                return false;

            LocomotionFootPhaseSample exitSample = previousAnimation.CurrentLocomotionFootPhase;
            if (!exitSample.IsValid)
            {
                result = LocomotionFootPhaseMatchResult.Invalid("exit-foot-phase-invalid");
                return true;
            }

            LocomotionFootPhaseMatchRequest request = new LocomotionFootPhaseMatchRequest(
                exitSample,
                BasicMovementPhase.MoveLoop,
                BasicMovementGait.Run,
                runLoopAlias);
            LocomotionFootPhaseProfileSO targetProfile = animationConfig != null
                ? animationConfig.ResolveFootPhaseProfile(BasicMovementPhase.MoveLoop, BasicMovementGait.Run, runLoopAlias)
                : null;
            result = LocomotionFootPhaseMatcher.Match(in request, targetProfile);
            return true;
        }
    }

    internal sealed class LocomotionRuntimeBlackboardWriter
    {
        readonly ILocomotionRuntimeBlackboardDependencies dependencies;

        public LocomotionRuntimeBlackboardWriter(ILocomotionRuntimeBlackboardDependencies dependencies)
        {
            this.dependencies = dependencies;
        }

        public void WriteActionFacts(in CharacterRuntimeActionFacts facts)
        {
            dependencies.WriteActionFactsToBlackboard(in facts);
        }

        public void WriteAnimationFacts(in CharacterRuntimeAnimationFacts facts)
        {
            CharacterRuntimeAnimationFacts resolvedFacts = ResolveLocomotionFootPhaseAnimationFacts(in facts);
            dependencies.WriteAnimationFactsToBlackboard(in resolvedFacts);
        }

        public void WriteLocomotionPreemptionFact(in LocomotionPreemptionFact fact)
        {
            dependencies.WriteLocomotionPreemptionFactToBlackboard(in fact);
        }

        CharacterRuntimeAnimationFacts ResolveLocomotionFootPhaseAnimationFacts(in CharacterRuntimeAnimationFacts facts)
        {
            CharacterRuntimeAnimationFacts previous = dependencies.RuntimeBlackboardSnapshot.Animation;
            AnimationPhasePlaybackProgress locomotionProgress = facts.LocomotionProgress;
            LocomotionFootPhaseSample currentSample = ResolveCurrentLocomotionFootPhaseSample(
                in locomotionProgress,
                facts.SourceStep);
            LocomotionFootPhaseSample exitSample = ResolveLastLocomotionExitFootPhase(
                in previous,
                in facts,
                facts.SourceStep);

            return new CharacterRuntimeAnimationFacts(
                facts.LocomotionProgress,
                facts.LocomotionAnimationName,
                facts.ActionProgress,
                facts.ActionAnimationName,
                currentSample,
                exitSample,
                facts.SourceStep);
        }

        LocomotionFootPhaseSample ResolveCurrentLocomotionFootPhaseSample(
            in AnimationPhasePlaybackProgress progress,
            int sourceStep)
        {
            BasicMovementGait gait = ResolvePlaybackGait(in progress, dependencies.CurrentGait);
            if (!progress.HasValidPlayback || string.IsNullOrWhiteSpace(progress.AliasKey))
            {
                return LocomotionFootPhaseSample.Invalid(
                    progress.Phase,
                    gait,
                    progress.AliasKey,
                    progress.NormalizedTime,
                    sourceStep);
            }

            RunLocomotionAnimationConfigSO animationConfig = dependencies.AnimationConfig;
            LocomotionFootPhaseProfileSO profile = animationConfig != null
                ? animationConfig.ResolveFootPhaseProfile(progress.Phase, gait, progress.AliasKey)
                : null;
            return LocomotionFootPhaseSampler.Sample(
                profile,
                progress.Phase,
                gait,
                progress.AliasKey,
                progress.NormalizedTime,
                sourceStep);
        }

        LocomotionFootPhaseSample ResolveLastLocomotionExitFootPhase(
            in CharacterRuntimeAnimationFacts previous,
            in CharacterRuntimeAnimationFacts current,
            int sourceStep)
        {
            if (!IsTurnBackToRunLoopAnimationTransition(in previous, in current))
                return previous.LastLocomotionExitFootPhase;

            LocomotionFootPhaseSample previousSample = previous.CurrentLocomotionFootPhase;
            if (previousSample.IsValid && previousSample.Phase == BasicMovementPhase.TurnBack)
                return previousSample.WithSourceStep(sourceStep);

            AnimationPhasePlaybackProgress progress = previous.LocomotionProgress;
            BasicMovementGait gait = ResolvePlaybackGait(in progress, BasicMovementGait.Run);
            return LocomotionFootPhaseSample.Invalid(
                BasicMovementPhase.TurnBack,
                gait,
                progress.AliasKey,
                progress.NormalizedTime,
                sourceStep);
        }

        bool IsTurnBackToRunLoopAnimationTransition(
            in CharacterRuntimeAnimationFacts previous,
            in CharacterRuntimeAnimationFacts current)
        {
            bool previousWasTurnBack =
                previous.LocomotionProgress.Phase == BasicMovementPhase.TurnBack ||
                previous.CurrentLocomotionFootPhase.Phase == BasicMovementPhase.TurnBack;
            if (!previousWasTurnBack)
                return false;

            AnimationPhasePlaybackProgress currentProgress = current.LocomotionProgress;
            if (!currentProgress.HasValidPlayback || currentProgress.Phase != BasicMovementPhase.MoveLoop)
                return false;

            RunLocomotionAnimationConfigSO animationConfig = dependencies.AnimationConfig;
            string runLoopAlias = LocomotionAnimationAliasResolver.ResolveAliasKey(
                animationConfig,
                BasicMovementPhase.MoveLoop,
                BasicMovementGait.Run);
            return string.Equals(currentProgress.AliasKey, runLoopAlias, System.StringComparison.Ordinal);
        }

        BasicMovementGait ResolvePlaybackGait(
            in AnimationPhasePlaybackProgress progress,
            BasicMovementGait fallback)
        {
            return LocomotionAnimationAliasResolver.ResolveGaitForAlias(
                dependencies.AnimationConfig,
                progress.Phase,
                progress.AliasKey,
                fallback);
        }
    }

    internal sealed class LocomotionOutputCompletion
    {
        readonly ILocomotionOutputCompletionDependencies dependencies;

        public LocomotionOutputCompletion(ILocomotionOutputCompletionDependencies dependencies)
        {
            this.dependencies = dependencies;
        }

        public void Complete()
        {
            if (dependencies.HasCameraController && !dependencies.IsRollbackCameraBasisOverrideActive)
                dependencies.ResolveCamera();

            dependencies.SyncRollbackCameraBasis();
            ResetRunLatchAfterIdle();
        }

        public void SetRunLatchActive(bool active)
        {
            dependencies.StateStore.SetRunLatchActive(active);
        }

        void ResetRunLatchAfterIdle()
        {
            LocomotionRuntimeStateStore stateStore = dependencies.StateStore;
            if (stateStore.CurrentPhase != BasicMovementPhase.Idle || stateStore.CurrentIntent.HasMoveIntent)
                return;

            if (stateStore.RunLatchActive || stateStore.LastMovingGait != BasicMovementGait.Walk)
            {
                LocomotionDiagnostics.LogRunLatchResetAfterIdle(
                    stateStore.ActiveStatePath,
                    stateStore.CurrentPhase,
                    stateStore.CurrentIntent.HasMoveIntent,
                    stateStore.LastMovingGait,
                    stateStore.RunLatchActive,
                    dependencies.CurrentAnimationName);
            }

            stateStore.ClearRunLatchAfterIdle();
        }
    }
}
