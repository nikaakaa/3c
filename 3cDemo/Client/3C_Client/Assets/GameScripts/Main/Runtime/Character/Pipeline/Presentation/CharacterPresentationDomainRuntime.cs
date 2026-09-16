using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonGameplay.Tick;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal sealed class CharacterPresentationDomainRuntime :
        IDisposable,
        ICharacterPresentationDomainRuntime,
        ICharacterPoseNativeEventFrameSource
    {
        readonly ActorId m_ActorId;
        readonly CharacterBodyPresentationRuntime m_Body;
        readonly CharacterAnimationEventGraphHost m_EventGraph;
        readonly CharacterEquipmentDomainRuntime m_Equipment;
        readonly CharacterCameraDomainRuntime m_Camera;
        readonly double m_PresentationTimePerTick;
        CharacterAnimationVariableFrame m_EventFrame;
        CharacterPresentationTrajectoryIntent m_Trajectory;
        bool m_HasTrajectory;
        bool m_Disposed;

        internal CharacterPresentationDomainRuntime(
            ActorId actorId,
            CharacterBodyPresentationRuntime body,
            int tickRate,
            CharacterAnimationPresentationProfile presentationProfile,
            CharacterEquipmentDomainRuntime equipment,
            CharacterCameraDomainRuntime camera)
        {
            m_ActorId = actorId;
            m_Body = body ?? throw new ArgumentNullException(nameof(body));
            if (tickRate <= 0)
                throw new ArgumentOutOfRangeException(nameof(tickRate));
            m_PresentationTimePerTick = 1d / tickRate;
            if (!presentationProfile)
                throw new ArgumentNullException(nameof(presentationProfile));
            if (!presentationProfile.EventGraph)
                throw new InvalidOperationException("Presentation domain requires an Animation EventGraph.");
            m_Equipment = equipment;
            m_Camera = camera;
            m_EventGraph = new CharacterAnimationEventGraphHost(
                presentationProfile.EventGraph,
                actorId);
        }

        public bool AcceptsTrajectoryIntent => true;
        public ulong BodyResetSequence => m_Body.ResetSequence;
        public bool SupportsCheckpointCapture => false;
        public bool SupportsCheckpointRestore => false;

        public bool TryGetLatestBody(out CharacterPresentationBodyState body) =>
            m_Body.TryGetLatestBody(out body);

        public void CaptureBodyTransaction(IReadOnlyList<CharacterPresentationBodyInterval> intervals) =>
            m_Body.CaptureTransaction(intervals);

        public void CaptureTrajectoryIntent(CharacterPresentationTrajectoryIntent intent)
        {
            if (intent.ActorId != m_ActorId)
                throw new InvalidOperationException("Presentation trajectory targets another Actor.");
            if (intent.ResetSequence != m_Body.ResetSequence)
                throw new InvalidOperationException("Presentation trajectory reset generation is stale.");
            m_Trajectory = intent;
            m_HasTrajectory = true;
        }

        public void CaptureEquipmentSelections(IReadOnlyList<EquipmentVisualSelection> selections)
        {
            if (m_Equipment == null)
                throw new InvalidOperationException(
                    "Equipment presentation requires a composed Equipment runtime.");
            m_Equipment.Capture(selections);
        }

        public void Publish(CharacterPresentationCommand command)
        {
            RequireActor(command.Header.ActorId);
            throw new InvalidOperationException(
                "Presentation commands require composed Action and Timeline runtimes.");
        }

        public void Replace(CharacterPresentationCommand current, CharacterPresentationCommand replacement)
        {
            RequireActor(current.Header.ActorId);
            RequireActor(replacement.Header.ActorId);
            throw new InvalidOperationException(
                "Presentation replacement requires composed Action and Timeline runtimes.");
        }

        public void Retire(CharacterPresentationCommand command)
        {
            RequireActor(command.Header.ActorId);
            throw new InvalidOperationException(
                "Presentation retirement requires composed Action and Timeline runtimes.");
        }

        public void Reset()
        {
            m_Camera?.Reset();
            m_Equipment?.Reset();
            m_Body.Reset();
            m_EventGraph.Reset();
            m_EventFrame = null;
            m_Trajectory = default;
            m_HasTrajectory = false;
        }

        public CharacterPresentationDomainDiagnosticsSnapshot CaptureDiagnostics() =>
            new(
                0,
                0,
                m_Body.FollowerPositionCorrectionMeters,
                m_Body.FollowerYawCorrectionDegrees);

        public bool TryCaptureCheckpoint(SimulationSessionCheckpoint checkpoint, out string error)
        {
            if (checkpoint == null)
                throw new ArgumentNullException(nameof(checkpoint));
            error = "Character Presentation checkpoint capture is not composed.";
            return false;
        }

        public bool TryRestoreCheckpoint(SimulationSessionCheckpoint checkpoint, out string error)
        {
            if (checkpoint == null)
                throw new ArgumentNullException(nameof(checkpoint));
            error = "Character Presentation checkpoint restore is not composed.";
            return false;
        }

        public void PresentationFrame(GameplayPresentationFrameContext context)
        {
            m_Equipment?.Present();
            CharacterBodyPresentationFrame bodyFrame = m_Body.Present(context);
            if (!bodyFrame.IsValid)
                return;
            m_Camera?.Present(bodyFrame, context);
            CharacterPresentationFactFrame factFrame = CreateFactFrame(in bodyFrame);
            CharacterAnimationVariableUpdateResult update = m_EventGraph.Update(
                in factFrame,
                Mathf.Max(0f, context.PresentationDeltaSeconds),
                context.RenderFrame);
            if (!update.Succeeded)
                throw new InvalidOperationException(
                    $"Animation EventGraph update failed: {update.Failure.Message}");
            m_EventFrame = update.Frame;
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            m_Camera?.Dispose();
            m_Equipment?.Dispose();
            m_EventGraph.Dispose();
            m_Body.Dispose();
        }

        bool ICharacterPoseNativeEventFrameSource.TryGetFrame(
            ActorId actorId,
            ulong frameIdentity,
            out CharacterAnimationVariableFrame frame)
        {
            frame = m_EventFrame;
            return actorId == m_ActorId && frame != null && frame.RenderFrame == frameIdentity;
        }

        CharacterPresentationFactFrame CreateFactFrame(
            in CharacterBodyPresentationFrame bodyFrame)
        {
            var identity = new CharacterPresentationFactFrameIdentity(
                m_ActorId,
                bodyFrame.CurrentTick);
            Vector2 desiredVelocity = m_HasTrajectory ? m_Trajectory.DesiredPlanarVelocity : Vector2.zero;
            Vector2 desiredFacing = m_HasTrajectory
                ? m_Trajectory.DesiredFacing
                : NormalizeFacing(bodyFrame.TargetRotation * Vector3.forward);
            return new CharacterPresentationFactFrame(
                identity,
                new SimulationTick(bodyFrame.CurrentTick),
                bodyFrame.CurrentTick * m_PresentationTimePerTick,
                bodyFrame.TargetGrounded,
                bodyFrame.TargetVelocity,
                bodyFrame.TargetRotation,
                desiredFacing,
                m_HasTrajectory && m_Trajectory.HasMotion,
                m_Trajectory.LocomotionPlanarBasis,
                desiredVelocity,
                m_HasTrajectory
                    ? m_Trajectory.MovementModeId
                    : CharacterPresentationTrajectoryIntent.StationaryMovementModeId,
                default,
                default,
                0d,
                m_Body.ResetSequence);
        }

        static Vector2 NormalizeFacing(Vector3 forward)
        {
            var value = new Vector2(forward.x, forward.z);
            return value.sqrMagnitude > 0.000001f ? value.normalized : Vector2.up;
        }

        void RequireActor(ActorId actorId)
        {
            if (actorId != m_ActorId)
                throw new InvalidOperationException("Presentation input targets another Actor.");
        }
    }
}



