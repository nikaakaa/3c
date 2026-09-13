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
        bool m_Initialized;

        public CharacterCameraSequenceEvaluator(CharacterCameraProjectionPayload projection)
        {
            if (projection == null)
                throw new ArgumentNullException(nameof(projection));
            projection.RequireValid();
            m_Projection = projection;
            m_FramePlanner = new CharacterCameraFramePlanner(projection);
            m_Transition = new CharacterCameraSequenceTransition(projection, m_FramePlanner);
            m_WorldBasicHistory = new CameraWorldBasicHistory(projection.DefaultSmoothTime);
        }

        public void Reset()
        {
            m_FramePlanner.Reset();
            m_Transition.Reset();
            m_WorldBasicHistory.Reset();
            m_Initialized = false;
        }

        public void SetInitialState(in CameraInitialState state)
        {
            m_FramePlanner.SetInitialState(in state);
            m_Transition.Reset();
            m_WorldBasicHistory.Reset();
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
            in CameraResponseRequest response)
        {
            if (input.ResetHistory || !m_Initialized)
            {
                m_FramePlanner.Reset();
                m_Transition.Reset();
                m_Initialized = true;
            }
            Vector2 look = m_FramePlanner.ResolveLook(
                input.Paused ? Vector2.zero : input.LookInput,
                in response);
            CameraFramePlan target = m_Transition.Evaluate(in input, in request, look);
            return m_WorldBasicHistory.Apply(target, in input);
        }

    }

    sealed class CameraWorldBasicHistory
    {
        readonly float m_SmoothTime;
        CameraWorldBasicData m_Current;
        Vector3 m_PivotVelocity;
        float m_RadiusVelocity;
        Vector2 m_OffsetVelocity;
        bool m_Initialized;

        public CameraWorldBasicHistory(float smoothTime)
        {
            m_SmoothTime = smoothTime;
        }

        public void Reset()
        {
            m_Current = default;
            m_PivotVelocity = Vector3.zero;
            m_RadiusVelocity = 0f;
            m_OffsetVelocity = Vector2.zero;
            m_Initialized = false;
        }

        public CameraFramePlan Apply(CameraFramePlan target, in CameraFrameInput input)
        {
            if (!target.Valid)
                return target;
            if (!m_Initialized || input.ResetHistory || m_SmoothTime <= 0f)
            {
                SetCurrent(target.WorldBasicData, true);
                return target;
            }

            float deltaTime = input.PresentationDeltaSeconds;
            if (deltaTime <= 0f)
                return target.WithWorldBasicData(m_Current);

            Vector3 pivot = Vector3.SmoothDamp(
                m_Current.PivotLocation,
                target.PivotLocation,
                ref m_PivotVelocity,
                m_SmoothTime,
                Mathf.Infinity,
                deltaTime);
            float radius = Mathf.SmoothDamp(
                m_Current.Radius,
                target.Radius,
                ref m_RadiusVelocity,
                m_SmoothTime,
                Mathf.Infinity,
                deltaTime);
            Vector2 offset = Vector2.SmoothDamp(
                m_Current.Offset,
                target.Offset,
                ref m_OffsetVelocity,
                m_SmoothTime,
                Mathf.Infinity,
                deltaTime);
            SetCurrent(new CameraWorldBasicData(pivot, target.Rotation, radius, offset, target.FieldOfView), false);
            return target.WithWorldBasicData(m_Current);
        }

        void SetCurrent(CameraWorldBasicData data, bool resetVelocity)
        {
            m_Current = data;
            if (resetVelocity)
            {
                m_PivotVelocity = Vector3.zero;
                m_RadiusVelocity = 0f;
                m_OffsetVelocity = Vector2.zero;
            }
            m_Initialized = true;
        }

    }
}
