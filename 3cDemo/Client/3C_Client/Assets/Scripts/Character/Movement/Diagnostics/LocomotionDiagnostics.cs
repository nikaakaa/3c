using ThirdPersonAnimation;
using ThirdPersonCharacterStateMachine;
using ThirdPersonDiagnostics;
using UnityEngine;

namespace ThirdPersonMovement
{
    public static class LocomotionDiagnostics
    {
        const string TurnBackRootMotionLogKeyword = "TURNBACK_RM_CHAIN";
        const string TurnBackDirectionDebugChannel = "Locomotion.turnback-direction-debug";
        static readonly LocomotionDiagnosticAdapter defaultAdapter =
            new LocomotionDiagnosticAdapter(RuntimeDiagnosticLogCharacterSink.Instance);

        public static void SubmitLegacyPlayerEnabled()
        {
            SubmitDiagnostic(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.Locomotion,
                RuntimeDiagnosticLogLevel.Error,
                "legacy-player-enabled",
                string.Empty,
                string.Empty,
                0,
                Time.frameCount,
                "Legacy Player path is enabled. Player locomotion is disabled to avoid double movement input."));
        }

        public static void SubmitInputSourceMissing()
        {
            SubmitDiagnostic(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.Locomotion,
                RuntimeDiagnosticLogLevel.Error,
                "input-source-missing",
                string.Empty,
                string.Empty,
                0,
                Time.frameCount,
                "Locomotion input source is missing. Player locomotion cannot read movement input."));
        }

        public static void SubmitMotionExecutorMissing()
        {
            SubmitDiagnostic(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.Locomotion,
                RuntimeDiagnosticLogLevel.Error,
                "motion-executor-missing",
                string.Empty,
                string.Empty,
                0,
                Time.frameCount,
                "Locomotion motion executor is missing. Player locomotion cannot enter the main movement path."));
        }

        public static void SubmitFormalConfigMissing(string activeStatePath, string eventId, string message)
        {
            SubmitDiagnostic(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.Locomotion,
                RuntimeDiagnosticLogLevel.Error,
                eventId,
                activeStatePath,
                string.Empty,
                0,
                Time.frameCount,
                message));
        }

        public static void SubmitRetiredDirectTick(string activeStatePath, int step)
        {
            SubmitDiagnostic(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.Locomotion,
                RuntimeDiagnosticLogLevel.Error,
                "locomotion-direct-driver-retired",
                activeStatePath,
                string.Empty,
                step,
                Time.frameCount,
                "PlayerLocomotionController direct gameplay tick is retired. Drive locomotion through PlayerFullBodyActionController and CharacterFramePipeline."));
        }

        public static void SubmitDriverConflict(string targetName, string conflictName)
        {
            SubmitDiagnostic(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.Locomotion,
                RuntimeDiagnosticLogLevel.Error,
                "locomotion-driver-conflict",
                string.Empty,
                string.Empty,
                0,
                Time.frameCount,
                $"LocomotionTickAdapter and FullBodyActionTickAdapter both target {targetName}. Disable one gameplay driver. conflict={conflictName}"));
        }

        public static void SubmitRetiredTickAdapter(string targetName)
        {
            SubmitDiagnostic(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.Locomotion,
                RuntimeDiagnosticLogLevel.Error,
                "locomotion-tick-adapter-retired",
                string.Empty,
                string.Empty,
                0,
                Time.frameCount,
                $"LocomotionTickAdapter is retired and cannot drive gameplay ticks. Drive {targetName} through FullBodyActionTickAdapter."));
        }

        public static void LogTickSnapshot(string activeStatePath, int step, string context)
        {
            SubmitDiagnostic(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.Locomotion,
                RuntimeDiagnosticLogLevel.Trace,
                "locomotion-tick-snapshot",
                activeStatePath,
                string.Empty,
                step,
                Time.frameCount,
                context));
        }

        public static void LogRunLatchResetAfterIdle(
            string activeStatePath,
            BasicMovementPhase phase,
            bool intentHasMove,
            BasicMovementGait lastMovingGait,
            bool runLatchBefore,
            string animationName)
        {
            SubmitDiagnostic(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.Locomotion,
                RuntimeDiagnosticLogLevel.Info,
                "locomotion-run-latch-reset-after-idle",
                activeStatePath,
                string.Empty,
                0,
                Time.frameCount,
                $"phase={phase} intentHasMove={intentHasMove} lastMovingGait={lastMovingGait} runLatchBefore={runLatchBefore} animation={animationName}"));
        }

        public static void LogCameraInput(
            string objectName,
            Vector2 moveInput,
            Vector2 lookInput,
            string cameraName,
            string cameraAutoTick,
            Vector3 followPosition)
        {
            SubmitDiagnostic(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.Locomotion,
                RuntimeDiagnosticLogLevel.Info,
                "movement-camera-input",
                string.Empty,
                string.Empty,
                0,
                Time.frameCount,
                $"[DEBUG-CAM-CHAIN] movement.camera frame={Time.frameCount} object={objectName} " +
                $"move={moveInput.ToString("F3")} look={lookInput.ToString("F3")} camera={cameraName} " +
                $"cameraAutoTick={cameraAutoTick} followPosition={followPosition.ToString("F3")}"));
        }

        public static void LogRunLatchOutputApplied(
            string activePath,
            bool setRunLatch,
            bool resetRunLatch,
            bool previousRunLatch,
            bool runLatchActive,
            BasicMovementPhase statePhase,
            CharacterStateVariant stateVariant,
            bool actionCompleted)
        {
            SubmitDiagnostic(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.Locomotion,
                RuntimeDiagnosticLogLevel.Info,
                "locomotion-run-latch-output-applied",
                activePath,
                string.Empty,
                0,
                Time.frameCount,
                $"setOutput={setRunLatch} resetOutput={resetRunLatch} before={previousRunLatch} after={runLatchActive} statePhase={statePhase} stateGait={stateVariant} actionCompleted={actionCompleted}"));
        }

        public static void LogStateMachineOutputProbe(
            int currentStep,
            BasicMovementPhase phaseBeforeTick,
            BasicMovementGait frameGait,
            in MovementInputIntent pendingIntent,
            in BasicMovementPhaseFacts phaseFacts,
            bool runLatchBeforeTick,
            bool runLatchActive,
            BasicMovementGait lastMovingGait,
            bool hasActiveMoveStopGait,
            BasicMovementGait activeMoveStopGait,
            in CharacterStateMachineFrame stateFrame)
        {
            if (!runLatchBeforeTick &&
                !runLatchActive &&
                !stateFrame.SetRunLatch &&
                !stateFrame.ResetRunLatch &&
                stateFrame.LocomotionPhase != BasicMovementPhase.MoveStop &&
                frameGait != BasicMovementGait.Run)
            {
                return;
            }

            SubmitDiagnostic(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.Locomotion,
                RuntimeDiagnosticLogLevel.Trace,
                "locomotion-state-machine-output-probe",
                stateFrame.Snapshot.ActivePath,
                string.Empty,
                currentStep,
                Time.frameCount,
                $"phaseBefore={phaseBeforeTick} statePhase={stateFrame.LocomotionPhase} frameGait={frameGait} hasMove={pendingIntent.HasMoveIntent} pendingGait={pendingIntent.Gait} phaseCanExit={phaseFacts.PhaseCanExit} setRunLatch={stateFrame.SetRunLatch} resetRunLatch={stateFrame.ResetRunLatch} runLatchBeforeTick={runLatchBeforeTick} runLatchAfterOutput={runLatchActive} lastMovingGait={lastMovingGait} hasMoveStopGait={hasActiveMoveStopGait} moveStopGait={activeMoveStopGait} executeBasic={stateFrame.ExecuteBasicMovement} presentLocomotion={stateFrame.PresentLocomotionAnimation}"));
        }

        public static void LogLocomotionFacts(
            string activeStatePath,
            int currentStep,
            BasicMovementPhase phaseBeforeTick,
            in LocomotionDecisionFacts facts)
        {
            SubmitDiagnostic(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.Locomotion,
                RuntimeDiagnosticLogLevel.Trace,
                "locomotion-decision-pipeline",
                activeStatePath,
                string.Empty,
                currentStep,
                Time.frameCount,
                $"phaseBefore={phaseBeforeTick} hasMove={facts.HasMoveIntent} gaitCandidate={facts.GaitCandidate} " +
                $"rawMove={facts.MoveIntent.RawInput.ToString("F3")} normalizedMove={facts.MoveIntent.NormalizedInput.ToString("F3")} strength={facts.MoveIntent.Strength:F3} " +
                $"worldMove={facts.SpatialFacts.WorldMoveDirection.ToString("F3")} facing={facts.SpatialFacts.FacingForward.ToString("F3")} " +
                $"cameraForward={facts.SpatialFacts.CameraPlanarForward.ToString("F3")} cameraRight={facts.SpatialFacts.CameraPlanarRight.ToString("F3")} " +
                $"phaseCanExit={facts.PhaseFacts.PhaseCanExit} turnBackValid={facts.TurnBackIntent.IsValidAt(currentStep)} " +
                $"turnBackAngle={facts.TurnBackIntent.Angle:F3} turnBackThreshold={facts.TurnBackIntent.Threshold:F3} " +
                $"turnBackOrigin={facts.TurnBackIntent.OriginStep} turnBackExpire={facts.TurnBackIntent.ExpireStep}"));
        }

        public static void LogTurnBackIntent(
            string activeStatePath,
            string reason,
            int currentStep,
            in LocomotionTurnBackIntent intent,
            float observedAngle = -1f)
        {
            SubmitDiagnostic(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.Locomotion,
                RuntimeDiagnosticLogLevel.Trace,
                "locomotion-turnback-intent",
                activeStatePath,
                string.Empty,
                currentStep,
                Time.frameCount,
                $"reason={reason} valid={intent.IsValidAt(currentStep)} rawValid={intent.IsValid} origin={intent.OriginStep} expire={intent.ExpireStep} " +
                $"angle={intent.Angle:F3} observedAngle={observedAngle:F3} threshold={intent.Threshold:F3} " +
                $"worldMove={intent.WorldMoveDirection.ToString("F3")} facing={intent.FacingForward.ToString("F3")}"));
        }

        public static void LogTurnBackRootMotionConsumed(
            BasicMovementPhase phase,
            BasicMovementGait gait,
            string aliasKey,
            in AnimationMotionProfileSample bakedSample,
            in TurnBackMotionPolicy policy,
            in AnimationMotionPlaybackWindow playbackWindow,
            in Vector3 appliedPlanarDelta,
            float appliedYawDelta,
            BasicMovementPlanarDeltaSpace deltaSpace,
            Vector3 entryPlanarBasisForward,
            in StateTimelineWindowFacts timelineFacts)
        {
            Vector3 entryPlanarBasisRight = ResolvePlanarRightOrZero(entryPlanarBasisForward);
            SubmitDiagnostic(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.Locomotion,
                RuntimeDiagnosticLogLevel.Info,
                "turnback-root-motion-consumed",
                aliasKey,
                string.Empty,
                0,
                Time.frameCount,
                $"[{TurnBackRootMotionLogKeyword}] stage=controller phase={phase} gait={gait} alias={aliasKey} " +
                $"bakedMotion={bakedSample.HasMotionContribution} bakedAlias={bakedSample.SourceAliasKey} bakedLocalDelta={bakedSample.LocalPlanarDelta.ToString("F3")} bakedYawDelta={bakedSample.YawDelta:F3} " +
                $"playbackWindow={playbackWindow.HasValidPlayback}/{playbackWindow.PreviousNormalizedTime:F3}->{playbackWindow.CurrentNormalizedTime:F3} " +
                $"appliedTranslationSource={policy.TranslationSource} appliedPlanarDelta={appliedPlanarDelta.ToString("F3")} appliedYawSource={policy.YawSource} yawDelta={appliedYawDelta:F3} deltaSpace={deltaSpace} entryBasisForward={entryPlanarBasisForward.ToString("F3")} entryBasisRight={entryPlanarBasisRight.ToString("F3")} " +
                $"turnComplete={policy.TurnCompleteNormalizedTime:F3} suppressInputRotation={policy.SuppressInputRotation} suppressInputPlanarMovement={policy.SuppressInputPlanarMovement} bakedProfile={policy.BakedMotionProfileId} " +
                $"timelineMotion={timelineFacts.MotionWindowActive} timelineInputLock={timelineFacts.InputLockWindowActive} timelineInterrupt={timelineFacts.InterruptWindowActive} timelineExit={timelineFacts.ExitWindowActive} timelineWindows={timelineFacts.ActiveWindowIds}",
                TurnBackDirectionDebugChannel));
        }

        public static void LogTurnBackStatePolicy(
            string activeStatePath,
            int currentStep,
            BasicMovementPhase phase,
            BasicMovementGait gait,
            string aliasKey,
            in TurnBackMotionPolicy policy,
            Vector3 lockedWorldDirection,
            Vector3 entryPlanarBasisForward,
            in Vector3 planarDelta,
            float yawDelta,
            in StateTimelineWindowFacts timelineFacts,
            in AnimationPhasePlaybackProgress progress,
            float currentYaw)
        {
            float lockedYaw = TryNormalizePlanar(lockedWorldDirection, out Vector3 lockedDirection)
                ? Quaternion.LookRotation(lockedDirection, Vector3.up).eulerAngles.y
                : 0f;
            float entryYaw = TryNormalizePlanar(entryPlanarBasisForward, out Vector3 entryDirection)
                ? Quaternion.LookRotation(entryDirection, Vector3.up).eulerAngles.y
                : 0f;
            bool canExit = progress.HasValidPlayback && progress.NormalizedTime >= policy.TurnCompleteNormalizedTime;
            SubmitDiagnostic(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.Locomotion,
                RuntimeDiagnosticLogLevel.Info,
                "locomotion-turnback-state-policy",
                activeStatePath,
                aliasKey,
                currentStep,
                Time.frameCount,
                $"[{TurnBackRootMotionLogKeyword}] stage=policy phase={phase} gait={gait} alias={aliasKey} entryPhase={policy.EntryPhase} entryGait={policy.EntryGait} " +
                $"lockedDirection={lockedWorldDirection.ToString("F3")} lockedYaw={lockedYaw:F3} entryBasisForward={entryPlanarBasisForward.ToString("F3")} entryYaw={entryYaw:F3} currentYaw={currentYaw:F3} " +
                $"yawSource={policy.YawSource} translationSource={policy.TranslationSource} planarDelta={planarDelta.ToString("F3")} yawDelta={yawDelta:F3} " +
                $"suppressInputRotation={policy.SuppressInputRotation} suppressInputPlanarMovement={policy.SuppressInputPlanarMovement} " +
                $"startNormalized={policy.StartNormalizedTime:F3} lockInputNormalized={policy.LockInputNormalizedTime:F3} exitNormalized={policy.ExitNormalizedTime:F3} turnComplete={policy.TurnCompleteNormalizedTime:F3} " +
                $"progressAlias={progress.AliasKey} progressNormalized={progress.NormalizedTime:F3} progressValid={progress.HasValidPlayback} progressEnded={progress.IsEnded} canExit={canExit} bakedProfile={policy.BakedMotionProfileId} " +
                $"timelineNormalized={timelineFacts.NormalizedTime:F3} timelineNormalizedValid={timelineFacts.HasValidNormalizedTime} timelineElapsed={timelineFacts.ElapsedSeconds:F3} timelineMotion={timelineFacts.MotionWindowActive} timelineInputLock={timelineFacts.InputLockWindowActive} timelineInterrupt={timelineFacts.InterruptWindowActive} timelineExit={timelineFacts.ExitWindowActive} timelineWindows={timelineFacts.ActiveWindowIds}",
                TurnBackDirectionDebugChannel));
        }

        public static void LogTurnBackEntryBasisMissing(
            string activeStatePath,
            int currentStep,
            BasicMovementPhase phase,
            BasicMovementGait gait,
            string aliasKey,
            in Vector3 rejectedPlanarDelta)
        {
            SubmitDiagnostic(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.Locomotion,
                RuntimeDiagnosticLogLevel.Warning,
                "turnback-entry-basis-missing",
                activeStatePath,
                aliasKey,
                currentStep,
                Time.frameCount,
                $"[{TurnBackRootMotionLogKeyword}] stage=controller-entry-basis-missing phase={phase} gait={gait} alias={aliasKey} deltaSpace={BasicMovementPlanarDeltaSpace.EntryLocal} rejectedPlanarDelta={rejectedPlanarDelta.ToString("F3")}"));
        }

        public static void LogTurnBackFrameSummary(
            int currentStep,
            BasicMovementPhase phaseBeforeTick,
            in LocomotionDecisionFacts facts,
            in CharacterStateMachineFrame stateFrame,
            in BasicMovementMotionFacts motionFacts,
            in BasicLocomotionFrame frame,
            in AnimationPhasePlaybackProgress progress)
        {
            bool relevant =
                phaseBeforeTick == BasicMovementPhase.TurnBack ||
                stateFrame.LocomotionPhase == BasicMovementPhase.TurnBack ||
                facts.TurnBackIntent.IsValidAt(currentStep) ||
                motionFacts.SourcePhase == BasicMovementPhase.TurnBack;

            if (!relevant)
                return;

            MovementCommand command = frame.Command;
            SubmitDiagnostic(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.Locomotion,
                RuntimeDiagnosticLogLevel.Info,
                "turnback-frame-summary",
                stateFrame.Snapshot.ActivePath,
                progress.AliasKey,
                currentStep,
                Time.frameCount,
                $"phaseBefore={phaseBeforeTick} statePhase={stateFrame.LocomotionPhase} stateTime={stateFrame.Snapshot.StateTime:F3} " +
                $"hasMove={facts.HasMoveIntent} gaitCandidate={facts.GaitCandidate} commandGait={command.Gait} " +
                $"worldMove={facts.SpatialFacts.WorldMoveDirection.ToString("F3")} facing={facts.SpatialFacts.FacingForward.ToString("F3")} desiredFacing={command.DesiredFacing.ToString("F3")} " +
                $"turnBackValid={facts.TurnBackIntent.IsValidAt(currentStep)} turnBackRawValid={facts.TurnBackIntent.IsValid} turnBackAngle={facts.TurnBackIntent.Angle:F3} turnBackThreshold={facts.TurnBackIntent.Threshold:F3} " +
                $"turnBackOrigin={facts.TurnBackIntent.OriginStep} turnBackExpire={facts.TurnBackIntent.ExpireStep} executeBasic={stateFrame.ExecuteBasicMovement} presentLocomotion={stateFrame.PresentLocomotionAnimation} " +
                $"hasAnimationMotion={command.HasAnimationMotion} animationAlias={command.AnimationMotionSourceAliasKey} animationDeltaSpace={command.AnimationPlanarDeltaSpace} animationDelta={command.AnimationLocalPlanarDelta.ToString("F3")} animationBasisForward={command.AnimationPlanarBasisForward.ToString("F3")} animationYawDelta={command.AnimationYawDelta:F3} " +
                $"suppressInputRotation={command.SuppressInputRotation} suppressInputPlanarMovement={command.SuppressInputPlanarMovement} planarSpeed={command.PlanarSpeed:F3} rotationSpeed={command.RotationSpeed:F3} deltaTime={command.DeltaTime:F3} " +
                $"animationProgressAlias={progress.AliasKey} animationProgressPhase={progress.Phase} animationNormalized={progress.NormalizedTime:F3} animationValid={progress.HasValidPlayback} animationEnded={progress.IsEnded}",
                TurnBackDirectionDebugChannel));
        }

        static Vector3 ResolvePlanarRightOrZero(Vector3 forward)
        {
            return TryNormalizePlanar(forward, out Vector3 normalizedForward)
                ? Vector3.Cross(Vector3.up, normalizedForward).normalized
                : Vector3.zero;
        }

        static void SubmitDiagnostic(RuntimeDiagnosticLogEvent diagnosticEvent)
        {
            defaultAdapter.Submit(in diagnosticEvent);
        }

        static bool TryNormalizePlanar(Vector3 value, out Vector3 normalized)
        {
            value.y = 0f;
            float sqrMagnitude = value.sqrMagnitude;
            if (sqrMagnitude <= 0.000001f)
            {
                normalized = Vector3.zero;
                return false;
            }

            normalized = value / Mathf.Sqrt(sqrMagnitude);
            return true;
        }
    }
}
