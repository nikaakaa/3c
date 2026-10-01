using System;
using UnityEngine;

namespace ThirdPersonCamera
{
    public sealed class CharacterCameraSequenceEvaluator
    {
        readonly CharacterCameraProjectionPayload m_Projection;
        readonly CharacterCameraFramePlanner m_FramePlanner;
        readonly CharacterCameraSequenceTransition m_Transition;
        readonly CameraWorldBasicHistory m_WorldBasicHistory;
        readonly CameraDelayModeRuntime m_Delay;
        bool m_Initialized;

        public CharacterCameraSequenceEvaluator(CharacterCameraProjectionPayload projection)
        {
            if (projection == null)
                throw new ArgumentNullException(nameof(projection));
            projection.RequireValid();
            m_Projection = projection;
            m_FramePlanner = new CharacterCameraFramePlanner(projection);
            m_Transition = new CharacterCameraSequenceTransition(projection, m_FramePlanner);
            m_Delay = new CameraDelayModeRuntime(projection.Delay);
            m_WorldBasicHistory = new CameraWorldBasicHistory();
        }

        public void Reset()
        {
            m_FramePlanner.Reset();
            m_Transition.Reset();
            m_WorldBasicHistory.Reset();
            m_Delay.Reset();
            m_Initialized = false;
        }

        public void SetInitialState(in CameraInitialState state)
        {
            m_FramePlanner.SetInitialState(in state);
            m_Transition.Reset();
            m_WorldBasicHistory.Reset();
            m_Delay.Reset();
            m_Initialized = false;
        }

        public bool Retire(
            string sourceId,
            ulong generation,
            ulong sourceActionInstanceId,
            int cycle,
            float blendOutSeconds,
            CameraPresentationStopReason reason)
        {
            if (!m_Initialized)
                return false;
            return m_Transition.Retire(
                sourceId,
                generation,
                sourceActionInstanceId,
                cycle,
                blendOutSeconds,
                reason);
        }

        public bool IsRetiring => m_Transition.IsRetiring;
        public CameraPresentationStopReason RetireReason => m_Transition.RetireReason;

        public void ForceTeardown(
            string sourceId,
            ulong generation,
            ulong sourceActionInstanceId)
        {
            if (!m_Initialized)
                return;
            m_Transition.ForceTeardown(sourceId, generation, sourceActionInstanceId);
        }

        public CameraFramePlan Evaluate(
            in CameraFrameInput input,
            in CameraSequenceRequest request,
            in CameraResponseRequest response,
            CameraEffectEvaluator effects)
        {
            if (input.ResetHistory || !m_Initialized)
            {
                m_FramePlanner.Reset();
                m_Transition.Reset();
                m_Delay.Reset();
                m_Initialized = true;
            }
            Vector2 look = m_FramePlanner.ResolveLook(
                in input,
                in response);
            m_FramePlanner.ApplyElevation(effects.ResolveElevation(
                m_FramePlanner.ElevationWithOverrun, m_FramePlanner.HasRotationControl, in input));
            CameraFramePlan target = m_Transition.Evaluate(in input, in request, look);
            target = effects.ApplyFraming(target, in input);
            target = target.WithAimResolved();
            CameraDelayOrbitSettings delay = m_Delay.Evaluate(in input, m_FramePlanner.HasRotationControl, m_FramePlanner.ElevationRatio);
            return m_WorldBasicHistory.Apply(target, in input, delay,
                m_Delay.MinimumDistanceRatio, m_Projection.Delay.Muted);
        }

    }

    sealed class CameraWorldBasicHistory
    {
        CameraWorldBasicData m_Current;
        Vector3 m_PreviousOffset;
        readonly CameraDelayDirectionRuntime m_Direction = new CameraDelayDirectionRuntime();
        bool m_Initialized;

        public void Reset()
        {
            m_Current = default;
            m_PreviousOffset = Vector3.zero;
            m_Direction.Reset();
            m_Initialized = false;
        }

        public CameraFramePlan Apply(CameraFramePlan target, in CameraFrameInput input,
            CameraDelayOrbitSettings delaySettings, float minimumDistanceRatio, bool muted)
        {
            CameraWorldBasicData targetData = target.WorldBasicData;
            if (!target.Valid)
                return target;
            Vector3 offset = -targetData.CameraToPivot;
            if (input.ResetHistory)
                m_Direction.Reset();
            m_Direction.AddModelForward(input.BodyRotation * Vector3.forward);
            if (!m_Initialized || input.ResetHistory || muted)
            {
                m_Current = target.WorldBasicData;
                m_PreviousOffset = offset;
                m_Initialized = true;
                return target;
            }
            float delta = input.PresentationDeltaSeconds;
            if (delta <= 0f || input.Paused)
                return target.WithWorldBasicData(m_Current);
            Vector3 pivot = m_Current.PivotLocation;
            if ((offset - m_PreviousOffset).sqrMagnitude > 0.01f)
            {
                Quaternion orbitDelta = Quaternion.FromToRotation(
                    Vector3.ProjectOnPlane(m_PreviousOffset, Vector3.up),
                    Vector3.ProjectOnPlane(offset, Vector3.up));
                pivot = target.PivotLocation + orbitDelta * (pivot - target.PivotLocation);
            }
            float directionRatio = m_Direction.Evaluate(delaySettings.FollowDirection, m_Current.Rotation);
            Vector3 damping = delaySettings.FollowPositionDamping;
            damping.x *= directionRatio;
            damping.z *= directionRatio;
            float verticalDelta = target.PivotLocation.y - pivot.y;
            float previousY = pivot.y;
            Quaternion dampingSpace = Quaternion.LookRotation(offset, Vector3.up);
            Vector3 localDelta = Quaternion.Inverse(dampingSpace) * (target.PivotLocation - pivot);
            pivot += dampingSpace * Cinemachine.Utility.Damper.Damp(localDelta, damping, delta);
            pivot.y = previousY + Cinemachine.Utility.Damper.Damp(verticalDelta, damping.y * directionRatio, delta);
            Vector3 previousNominal = Quaternion.Inverse(m_Current.Rotation) * -m_PreviousOffset;
            Vector2 previousComposition = m_Current.Offset - new Vector2(previousNominal.x, previousNominal.y);
            Vector2 composition = targetData.Offset + m_Current.WithOffset(previousComposition)
                .WithFraming(targetData.Radius, targetData.FieldOfView).Offset;
            Vector3 cameraLocation = pivot - targetData.Rotation *
                new Vector3(composition.x, composition.y, targetData.Radius);
            Vector3 localTarget = Quaternion.Inverse(targetData.Rotation) * (targetData.PivotLocation - cameraLocation);
            float screenSize = Mathf.Tan(targetData.FieldOfView * 0.5f * Mathf.Deg2Rad) * localTarget.z;
            Vector2 compositionOffset = ResolveCompositionOffset(
                new Vector2(localTarget.x, localTarget.y),
                delaySettings,
                input.PixelWidth / (float)input.PixelHeight,
                screenSize,
                delta);
            Vector3 horizontalOffset = Vector3.ProjectOnPlane(offset, Vector3.up);
            float minimumDistance = horizontalOffset.magnitude * Mathf.Max(0.2f, minimumDistanceRatio);
            Vector3 actualTarget = Vector3.ProjectOnPlane(target.PivotLocation, Vector3.up);
            Vector3 dampedTarget = Vector3.ProjectOnPlane(pivot, Vector3.up);
            Vector3 cameraPosition = dampedTarget + horizontalOffset;
            float distance = Vector3.Dot(actualTarget - cameraPosition, -horizontalOffset.normalized);
            if (distance < minimumDistance)
            {
                Vector3 direction = actualTarget - dampedTarget;
                float length = direction.magnitude;
                direction = length < 0.01f
                    ? -Vector3.ProjectOnPlane(target.Rotation * Vector3.forward, Vector3.up)
                    : direction / length;
                pivot += direction * (minimumDistance - distance);
            }
            m_PreviousOffset = offset;
            m_Current = targetData.WithPivotLocation(pivot).WithOffset(composition - compositionOffset);
            return target.WithWorldBasicData(m_Current);
        }

        internal static Vector2 ResolveCompositionOffset(
            Vector2 targetPosition,
            CameraDelayOrbitSettings settings,
            float aspectRatio,
            float screenSize,
            float delta)
        {
            Rect deadRect = ScreenRectToOrtho(new Rect(
                settings.ScreenPosition.x - settings.DeadZone.x * 0.5f,
                settings.ScreenPosition.y - settings.DeadZone.y * 0.5f,
                settings.DeadZone.x,
                settings.DeadZone.y), aspectRatio, screenSize);
            Vector2 offset = OrthoOffsetToScreenRect(targetPosition, deadRect);
            offset = Cinemachine.Utility.Damper.Damp(offset,
                new Vector3(settings.CompositionDamping.x, settings.CompositionDamping.y, 0f), delta);

            Vector2 softCenter = settings.ScreenPosition + Vector2.Scale(
                settings.Bias, settings.SoftZone - settings.DeadZone);
            Rect softRect = ScreenRectToOrtho(new Rect(
                softCenter.x - settings.SoftZone.x * 0.5f,
                softCenter.y - settings.SoftZone.y * 0.5f,
                settings.SoftZone.x,
                settings.SoftZone.y), aspectRatio, screenSize);
            offset += OrthoOffsetToScreenRect(targetPosition - offset, softRect);
            return offset;
        }

        static Rect ScreenRectToOrtho(Rect screenRect, float aspectRatio, float screenSize)
        {
            Rect result = default;
            result.yMax = 2f * screenSize * ((1f - screenRect.yMin) - 0.5f);
            result.yMin = 2f * screenSize * ((1f - screenRect.yMax) - 0.5f);
            result.xMin = 2f * screenSize * aspectRatio * (screenRect.xMin - 0.5f);
            result.xMax = 2f * screenSize * aspectRatio * (screenRect.xMax - 0.5f);
            return result;
        }

        static Vector2 OrthoOffsetToScreenRect(Vector2 position, Rect rect)
        {
            Vector2 offset = Vector2.zero;
            if (position.x < rect.xMin)
                offset.x += position.x - rect.xMin;
            if (position.x > rect.xMax)
                offset.x += position.x - rect.xMax;
            if (position.y < rect.yMin)
                offset.y += position.y - rect.yMin;
            if (position.y > rect.yMax)
                offset.y += position.y - rect.yMax;
            return offset;
        }
    }
}
