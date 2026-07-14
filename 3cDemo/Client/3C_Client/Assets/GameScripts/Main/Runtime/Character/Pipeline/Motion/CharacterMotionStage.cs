using System;
using System.Collections.Generic;
using BTSMTL.Diagnostics;
using ThirdPersonCharacter.Pipeline.Network;
using ThirdPersonGameplay.Tick;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Motion
{
    public sealed class CharacterMotionStage
    {
        const float SmoothCorrectionFraction = 0.5f;
        const float MaxSmoothCorrectionDistance = 0.5f;
        const float MaxSmoothCorrectionYawDegrees = 20f;
        const float ForceCorrectionDistance = 2f;
        const float ForceCorrectionYawDegrees = 90f;
        const float CorrectionPositionTolerance = 0.01f;
        const float CorrectionYawTolerance = 0.1f;
        const float ExecutionPositionTolerance = 0.0001f;
        const float ExecutionRotationTolerance = 0.01f;

        readonly ICharacterLogicPosePort m_LogicPosePort;
        readonly ICharacterMotionExecutor m_MotionExecutor;
        readonly ICharacterMotionContext m_MotionContext;
        readonly CharacterMotionAuthority m_MotionAuthority;
        readonly MotionResolver m_MotionResolver = new MotionResolver();
        readonly MotionWarpModifier m_MotionWarpModifier = new MotionWarpModifier();

        public CharacterMotionStage(
            ICharacterLogicPosePort logicPosePort,
            ICharacterMotionExecutor motionExecutor,
            ICharacterMotionContext motionContext,
            CharacterMotionAuthority motionAuthority)
        {
            m_LogicPosePort = logicPosePort ?? throw new ArgumentNullException(nameof(logicPosePort));
            m_MotionExecutor = motionExecutor;
            m_MotionContext = motionContext ?? throw new ArgumentNullException(nameof(motionContext));
            m_MotionAuthority = motionAuthority;
            if (motionAuthority == CharacterMotionAuthority.LocalSolver && motionExecutor == null)
            {
                throw new ArgumentNullException(
                    nameof(motionExecutor),
                    "LocalSolver requires an explicit motion executor.");
            }
        }

        public void Update(GameplayLogicTickContext context, CharacterPipelineFrame frame)
        {
            frame.Output.StrictGameplay.MotionCorrectionApplicationResult = default;
            frame.Output.StrictGameplay.MotionDebug.Clear();

            switch (m_MotionAuthority)
            {
                case CharacterMotionAuthority.LocalSolver:
                    UpdateLocalSolver(context, frame);
                    break;
                case CharacterMotionAuthority.ExternalPose:
                    UpdateExternalPose(frame);
                    break;
                case CharacterMotionAuthority.None:
                    HoldPose(frame);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            PublishResolvedMotionFact(context, frame);
        }

        void UpdateLocalSolver(GameplayLogicTickContext context, CharacterPipelineFrame frame)
        {
            CharacterLogicBodyState before = ReadState();
            Quaternion actorRotation = before.Rotation.ToUnityRotation();
            MotionIntent rawIntent = m_MotionResolver.Resolve(
                frame.Output.StrictGameplay.MotionContributions,
                actorRotation,
                context.FixedDeltaSeconds,
                frame.Output.StrictGameplay.MotionDebug);

            frame.Output.StrictGameplay.MotionDebug.SetMotionWarpWindows(frame.Output.StrictGameplay.MotionWarpWindows);
            MotionIntent intent = ApplyModifiers(rawIntent, before, context, frame);
            frame.Output.StrictGameplay.MotionDebug.SetModifiedIntent(rawIntent, intent);
            MotionCorrectionPlan correctionPlan = BuildCorrectionPlan(intent, before, context, frame);
            if (correctionPlan.Valid)
                intent = correctionPlan.Intent;
            frame.Output.StrictGameplay.MotionIntent = intent;

            MotionResult result;
            if (correctionPlan.Valid && correctionPlan.Extent == MotionCorrectionApplicationExtent.Full)
            {
                result = ApplyFullCorrection(correctionPlan);
            }
            else
            {
                result = Execute(context, before, intent, frame.Output.StrictGameplay.MotionDebug);
            }

            frame.Output.StrictGameplay.MotionResult = result;
            MotionCorrectionApplicationResult correctionApplication =
                BuildCorrectionApplication(correctionPlan, result);
            frame.Output.StrictGameplay.MotionCorrectionApplicationResult = correctionApplication;
            frame.Output.SyncFacts.Motion.CorrectionApplicationResult = correctionApplication;
            frame.Output.StrictGameplay.MotionDebug.SetCorrection(correctionApplication);
            PublishDiagnostics(frame, intent, result);
        }

        void UpdateExternalPose(CharacterPipelineFrame frame)
        {
            if (frame.NetworkInput.Motion.ExternalPoseSamples.Count == 0)
            {
                HoldPose(frame);
                return;
            }

            CharacterLogicBodyState before = ReadState();
            ExternalPoseSample sample = frame.NetworkInput.Motion.ExternalPoseSamples[
                frame.NetworkInput.Motion.ExternalPoseSamples.Count - 1];
            CharacterLogicPose requestedPose = sample.Position.ToLogicPose(sample.Rotation);
            if (!m_LogicPosePort.TryApplyPose(requestedPose, out CharacterLogicBodyState after, out string error))
                throw new InvalidOperationException($"Logic pose port failed to apply ExternalPose: {error}");
            EnsureValidState(after, "ExternalPose");

            Vector3 beforePosition = before.Position.ToUnityVector();
            Quaternion beforeRotation = before.Rotation.ToUnityRotation();
            Vector3 afterPosition = after.Position.ToUnityVector();
            Quaternion afterRotation = after.Rotation.ToUnityRotation();
            Vector3 appliedDisplacement = afterPosition - beforePosition;
            float appliedYaw = SignedYawDelta(beforeRotation, afterRotation);
            MotionResult result = new MotionResult(
                appliedDisplacement,
                appliedDisplacement,
                afterPosition,
                afterRotation,
                after.Grounded,
                appliedDisplacement.sqrMagnitude > 0.0000001f || Mathf.Abs(appliedYaw) > 0.0001f,
                appliedYaw,
                appliedYaw);
            frame.Output.StrictGameplay.MotionIntent = default;
            frame.Output.StrictGameplay.MotionResult = result;
            PublishDiagnostics(frame, default, result);
        }

        void HoldPose(CharacterPipelineFrame frame)
        {
            CharacterLogicBodyState state = ReadState();
            MotionResult result = new MotionResult(
                Vector3.zero,
                Vector3.zero,
                state.Position.ToUnityVector(),
                state.Rotation.ToUnityRotation(),
                state.Grounded,
                false);
            frame.Output.StrictGameplay.MotionIntent = default;
            frame.Output.StrictGameplay.MotionResult = result;
            PublishDiagnostics(frame, default, result);
        }

        void PublishResolvedMotionFact(GameplayLogicTickContext context, CharacterPipelineFrame frame)
        {
            MotionResult result = frame.Output.StrictGameplay.MotionResult;
            Vector3 horizontalDisplacement = Vector3.ProjectOnPlane(result.AppliedDisplacement, Vector3.up);
            float horizontalSpeed = context.FixedDeltaSeconds > 0f
                ? horizontalDisplacement.magnitude / context.FixedDeltaSeconds
                : 0f;
            frame.Output.SyncFacts.Motion.ResolvedMotion = new ResolvedCharacterMotionFact(
                frame.Input != null ? frame.Input.InputSequence : context.InputSequence,
                context.LocalLogicTick,
                result.AppliedDisplacement,
                result.AppliedYawDegrees,
                result.Position,
                result.Rotation,
                result.Grounded,
                result.HasMotion,
                horizontalSpeed);
        }

        MotionCorrectionPlan BuildCorrectionPlan(
            MotionIntent intent,
            CharacterLogicBodyState before,
            GameplayLogicTickContext context,
            CharacterPipelineFrame frame)
        {
            if (frame == null || frame.NetworkInput.Motion.ExternalPoseCorrections.Count == 0)
                return default;

            ExternalPoseCorrection correction = frame.NetworkInput.Motion.ExternalPoseCorrections[
                frame.NetworkInput.Motion.ExternalPoseCorrections.Count - 1];
            Vector3 actorPosition = before.Position.ToUnityVector();
            Quaternion actorRotation = before.Rotation.ToUnityRotation();
            Vector3 predictedPosition = actorPosition + (intent.HasMotion ? intent.Displacement : Vector3.zero);
            Quaternion predictedRotation = intent.HasMotion
                ? Quaternion.AngleAxis(intent.YawDegrees, Vector3.up) * actorRotation
                : actorRotation;

            Vector3 error = correction.Position - predictedPosition;
            float predictedYawError = SignedYawDelta(predictedRotation, correction.Rotation);
            bool force = error.magnitude > ForceCorrectionDistance ||
                         Mathf.Abs(predictedYawError) > ForceCorrectionYawDegrees;

            MotionCorrectionApplicationExtent extent;
            MotionIntent correctedIntent;
            if (force)
            {
                extent = MotionCorrectionApplicationExtent.Full;
                correctedIntent = CreateIntent(
                    correction.Position - actorPosition,
                    SignedYawDelta(actorRotation, correction.Rotation),
                    context.FixedDeltaSeconds);
            }
            else
            {
                Vector3 correctionDelta = ClampMagnitude(
                    error * SmoothCorrectionFraction,
                    MaxSmoothCorrectionDistance);
                float correctionYaw = Mathf.Clamp(
                    predictedYawError * SmoothCorrectionFraction,
                    -MaxSmoothCorrectionYawDegrees,
                    MaxSmoothCorrectionYawDegrees);
                extent = MotionCorrectionApplicationExtent.Partial;
                correctedIntent = CombineIntent(intent, correctionDelta, correctionYaw, context.FixedDeltaSeconds);
            }

            Vector3 requestedPosition = force
                ? correction.Position
                : actorPosition + (correctedIntent.HasMotion ? correctedIntent.Displacement : Vector3.zero);
            Quaternion requestedRotation = force
                ? correction.Rotation
                : correctedIntent.HasMotion
                    ? Quaternion.AngleAxis(correctedIntent.YawDegrees, Vector3.up) * actorRotation
                    : actorRotation;
            return new MotionCorrectionPlan(
                correctedIntent,
                extent,
                correction,
                actorPosition,
                actorRotation,
                predictedPosition,
                predictedRotation,
                requestedPosition,
                requestedRotation);
        }

        MotionResult ApplyFullCorrection(MotionCorrectionPlan plan)
        {
            CharacterLogicPose targetPose = plan.RequestedPosition.ToLogicPose(plan.RequestedRotation);
            if (!m_LogicPosePort.TryApplyPose(targetPose, out CharacterLogicBodyState after, out string error))
                throw new InvalidOperationException($"Logic pose port failed to apply full correction: {error}");
            EnsureValidState(after, "full correction");

            Vector3 position = after.Position.ToUnityVector();
            Quaternion rotation = after.Rotation.ToUnityRotation();
            Vector3 appliedDisplacement = position - plan.BeforePosition;
            float appliedYaw = SignedYawDelta(plan.BeforeRotation, rotation);
            return new MotionResult(
                plan.Intent.Displacement,
                appliedDisplacement,
                position,
                rotation,
                after.Grounded,
                plan.Intent.HasMotion || appliedDisplacement.sqrMagnitude > 0.0000001f || Mathf.Abs(appliedYaw) > 0.0001f,
                plan.Intent.YawDegrees,
                appliedYaw);
        }

        MotionCorrectionApplicationResult BuildCorrectionApplication(
            MotionCorrectionPlan plan,
            MotionResult result)
        {
            if (!plan.Valid)
                return default;

            Vector3 appliedDelta = result.Position - plan.PredictedPosition;
            float appliedYaw = SignedYawDelta(plan.PredictedRotation, result.Rotation);
            bool applied = Vector3.Distance(result.Position, plan.RequestedPosition) <= CorrectionPositionTolerance &&
                           Mathf.Abs(SignedYawDelta(result.Rotation, plan.RequestedRotation)) <= CorrectionYawTolerance;
            return new MotionCorrectionApplicationResult(
                plan.Extent,
                plan.Correction.InputSequence,
                plan.Correction.SourceTick,
                plan.BeforePosition,
                plan.BeforeRotation,
                plan.Correction.Position,
                plan.Correction.Rotation,
                appliedDelta,
                appliedYaw,
                applied);
        }

        MotionIntent ApplyModifiers(
            MotionIntent intent,
            CharacterLogicBodyState state,
            GameplayLogicTickContext context,
            CharacterPipelineFrame frame)
        {
            MotionModifierContext modifierContext = new MotionModifierContext(
                state.Position.ToUnityVector(),
                state.Rotation.ToUnityRotation(),
                context.FixedDeltaSeconds,
                m_MotionContext,
                frame.Output.StrictGameplay.MotionWarpWindows);
            return m_MotionWarpModifier.Modify(intent, modifierContext);
        }

        MotionResult Execute(
            GameplayLogicTickContext context,
            CharacterLogicBodyState before,
            MotionIntent intent,
            MotionResolveDebugFrame debug)
        {
            CharacterMotionExecutionInput input = new CharacterMotionExecutionInput(
                context.LocalLogicTick,
                context.FixedDeltaSeconds,
                before,
                intent.Displacement.ToMotionVector(),
                intent.Velocity.ToMotionVector(),
                intent.YawDegrees,
                intent.HasMotion);
            if (!m_MotionExecutor.TryExecute(input, out CharacterMotionExecutionResult execution, out string error))
                throw new InvalidOperationException($"Motion executor '{m_MotionExecutor.ImplementationId}' failed: {error}");
            if (!execution.IsValid)
                throw new InvalidOperationException($"Motion executor '{m_MotionExecutor.ImplementationId}' returned an invalid result.");

            CharacterLogicBodyState current = ReadState();
            EnsureExecutionStateMatches(execution.FinalState, current);
            debug.SetExecution(m_MotionExecutor.ImplementationId, input, execution);
            return new MotionResult(
                input.RequestedDisplacement.ToUnityVector(),
                execution.AppliedDisplacement.ToUnityVector(),
                execution.FinalState.Position.ToUnityVector(),
                execution.FinalState.Rotation.ToUnityRotation(),
                execution.FinalState.Grounded,
                input.HasMotion,
                input.RequestedYawDegrees,
                execution.AppliedYawDegrees);
        }

        CharacterLogicBodyState ReadState()
        {
            if (!m_LogicPosePort.TryReadState(out CharacterLogicBodyState state, out string error))
                throw new InvalidOperationException($"Logic pose port '{m_LogicPosePort.ImplementationId}' failed: {error}");
            EnsureValidState(state, "logic pose read");
            return state;
        }

        static void EnsureValidState(CharacterLogicBodyState state, string operation)
        {
            if (!state.IsValid)
                throw new InvalidOperationException($"Character logic body state is invalid after {operation}.");
        }

        static void EnsureExecutionStateMatches(
            CharacterLogicBodyState executionState,
            CharacterLogicBodyState portState)
        {
            float positionError = Vector3.Distance(
                executionState.Position.ToUnityVector(),
                portState.Position.ToUnityVector());
            float rotationError = Quaternion.Angle(
                executionState.Rotation.ToUnityRotation(),
                portState.Rotation.ToUnityRotation());
            if (positionError > ExecutionPositionTolerance || rotationError > ExecutionRotationTolerance)
            {
                throw new InvalidOperationException(
                    $"Motion executor result does not match Logic Pose Port state: positionError={positionError}, rotationError={rotationError}.");
            }
        }

        static MotionIntent CombineIntent(
            MotionIntent intent,
            Vector3 displacementDelta,
            float yawDelta,
            float deltaTime)
        {
            Vector3 displacement = (intent.HasMotion ? intent.Displacement : Vector3.zero) + displacementDelta;
            float yaw = (intent.HasMotion ? intent.YawDegrees : 0f) + yawDelta;
            return CreateIntent(displacement, yaw, deltaTime);
        }

        static MotionIntent CreateIntent(Vector3 displacement, float yaw, float deltaTime)
        {
            if (displacement.sqrMagnitude <= 0.0000001f && Mathf.Abs(yaw) <= 0.0001f)
                return default;

            Vector3 velocity = deltaTime > 0f ? displacement / deltaTime : Vector3.zero;
            return new MotionIntent(displacement, velocity, yaw);
        }

        static Vector3 ClampMagnitude(Vector3 value, float maxMagnitude)
        {
            if (maxMagnitude <= 0f || value.sqrMagnitude <= maxMagnitude * maxMagnitude)
                return value;

            return value.normalized * maxMagnitude;
        }

        static float SignedYawDelta(Quaternion from, Quaternion to)
        {
            Vector3 forward = Quaternion.Inverse(from) * (to * Vector3.forward);
            forward.y = 0f;
            if (forward.sqrMagnitude <= 0.0000001f)
                return 0f;

            return Vector3.SignedAngle(Vector3.forward, forward.normalized, Vector3.up);
        }

        readonly struct MotionCorrectionPlan
        {
            public MotionCorrectionPlan(
                MotionIntent intent,
                MotionCorrectionApplicationExtent extent,
                ExternalPoseCorrection correction,
                Vector3 beforePosition,
                Quaternion beforeRotation,
                Vector3 predictedPosition,
                Quaternion predictedRotation,
                Vector3 requestedPosition,
                Quaternion requestedRotation)
            {
                Intent = intent;
                Extent = extent;
                Correction = correction;
                BeforePosition = beforePosition;
                BeforeRotation = beforeRotation;
                PredictedPosition = predictedPosition;
                PredictedRotation = predictedRotation;
                RequestedPosition = requestedPosition;
                RequestedRotation = requestedRotation;
                Valid = true;
            }

            public MotionIntent Intent { get; }
            public MotionCorrectionApplicationExtent Extent { get; }
            public ExternalPoseCorrection Correction { get; }
            public Vector3 BeforePosition { get; }
            public Quaternion BeforeRotation { get; }
            public Vector3 PredictedPosition { get; }
            public Quaternion PredictedRotation { get; }
            public Vector3 RequestedPosition { get; }
            public Quaternion RequestedRotation { get; }
            public bool Valid { get; }
        }

        void PublishDiagnostics(CharacterPipelineFrame frame, MotionIntent intent, MotionResult result)
        {
            RuntimeDiagnosticsContext diagnostics = m_MotionContext.RuntimeDiagnostics;
            if (frame == null || diagnostics == null)
                return;

            RuntimeInstanceKey instance = RuntimeInstanceKey.Character(diagnostics.CharacterRuntimeId);
            if (diagnostics.ShouldPublish(RuntimeTraceChannel.Motion, RuntimeTraceEventKind.MotionContribution))
            {
                IReadOnlyList<MotionContribution> contributions = frame.Output.StrictGameplay.MotionContributions;
                for (int i = 0; i < contributions.Count; i++)
                {
                    MotionContribution contribution = contributions[i];
                    diagnostics.Publish(
                        RuntimeTraceChannel.Motion,
                        RuntimeTraceDomain.Logic,
                        RuntimeTraceEventKind.MotionContribution,
                        RuntimeSourceElementHandle.Invalid,
                        instance,
                        new RuntimeTracePayload
                        {
                            Name = contribution.SourceName,
                            Status = $"{contribution.Channel}/{contribution.BlendMode}",
                            Detail = contribution.DebugSourceIdentity,
                            RelatedElementId = contribution.SourceId,
                            Weight = contribution.Weight,
                            Priority = contribution.Priority,
                            SecondaryTime = contribution.YawDegrees,
                            Value = DebugValueSnapshot.Capture(contribution.Displacement),
                            Flag = contribution.ConsumeLowerChannels
                        });
                }
            }

            if (diagnostics.ShouldPublish(RuntimeTraceChannel.Motion, RuntimeTraceEventKind.MotionResolved))
            {
                diagnostics.Publish(
                    RuntimeTraceChannel.Motion,
                    RuntimeTraceDomain.Logic,
                    RuntimeTraceEventKind.MotionResolved,
                    RuntimeSourceElementHandle.Invalid,
                    instance,
                    new RuntimeTracePayload
                    {
                        Name = m_MotionAuthority == CharacterMotionAuthority.LocalSolver
                            ? m_MotionExecutor.ImplementationId
                            : m_LogicPosePort.ImplementationId,
                        Status = intent.HasMotion ? "Applied" : "Idle",
                        Detail = frame.Output.StrictGameplay.MotionDebug.Execution.Valid
                            ? frame.Output.StrictGameplay.MotionDebug.Execution.CollisionSummary.ToString()
                            : string.Empty,
                        Time = result.AppliedYawDegrees,
                        SecondaryTime = intent.YawDegrees,
                        Value = DebugValueSnapshot.Capture(result.AppliedDisplacement),
                        Flag = result.HasMotion
                    });
            }
        }
    }
}
