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

        public void ForceTeardown(
            string sourceId,
            ulong generation,
            ulong sourceActionInstanceId)
        {
            if (!m_Initialized)
                return;
            m_Transition.ForceTeardown(sourceId, generation, sourceActionInstanceId);
        }

        public bool IsCurrentScope(CameraPresentationScopeKey scope) =>
            m_Initialized && m_Transition.IsCurrentScope(scope);

        public CameraFramePlan Evaluate(
            in CameraFrameInput input,
            in CameraSequenceRequest request,
            in CameraResponseRequest response)
        {
            if (input.ResetHistory || !m_Initialized)
            {
                m_FramePlanner.Reset(input.BodyRotation);
                m_Transition.Reset();
                m_Initialized = true;
            }
            Vector2 look = m_FramePlanner.ResolveLook(input.LookInput, in response);
            CameraFramePlan target = m_Transition.Evaluate(in input, in request, look);
            return m_WorldBasicHistory.Apply(target, in input);
        }

        public CameraFramePlan EvaluateSuppressed(
            in CameraFrameInput input,
            in CameraResponseRequest response)
        {
            State state = CaptureState();
            try
            {
                CameraSequenceRequest request = new CameraSequenceRequest(
                    m_Projection.DefaultSequence.SequenceId,
                    int.MinValue,
                    1f,
                    0f,
                    0f,
                    string.Empty,
                    "camera.default.sequence",
                    0,
                    0,
                    CameraSequenceInterruptPolicy.BlendOut,
                    true);
                return Evaluate(in input, in request, in response);
            }
            finally
            {
                RestoreState(state);
            }
        }

        public bool TryCaptureScopeState(
            CameraPresentationScopeKey scope,
            out State state)
        {
            if (!m_Initialized || !m_Transition.IsCurrentScope(scope))
            {
                state = default;
                return false;
            }
            state = CaptureState();
            return true;
        }

        State CaptureState() => new State(
            m_FramePlanner.CaptureState(),
            m_Transition.CaptureState(),
            m_WorldBasicHistory.CaptureState(),
            m_Initialized);

        public void RestoreState(State state)
        {
            m_FramePlanner.RestoreState(state.FramePlanner);
            m_Transition.RestoreState(state.Transition);
            m_WorldBasicHistory.RestoreState(state.WorldBasicHistory);
            m_Initialized = state.Initialized;
        }

        public readonly struct State
        {
            public State(
                CharacterCameraFramePlanner.State framePlanner,
                CharacterCameraSequenceTransition.State transition,
                CameraWorldBasicHistory.State worldBasicHistory,
                bool initialized)
            {
                FramePlanner = framePlanner;
                Transition = transition;
                WorldBasicHistory = worldBasicHistory;
                Initialized = initialized;
            }

            public CharacterCameraFramePlanner.State FramePlanner { get; }
            public CharacterCameraSequenceTransition.State Transition { get; }
            public CameraWorldBasicHistory.State WorldBasicHistory { get; }
            public bool Initialized { get; }
        }
    }

    sealed class CameraWorldBasicHistory
    {
        readonly float m_SmoothTime;
        CameraWorldBasicData m_Current;
        Vector3 m_PivotVelocity;
        float m_RadiusVelocity;
        Vector2 m_OffsetVelocity;
        float m_FieldOfViewVelocity;
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
            m_FieldOfViewVelocity = 0f;
            m_Initialized = false;
        }

        public State CaptureState() => new State(
            m_Current,
            m_PivotVelocity,
            m_RadiusVelocity,
            m_OffsetVelocity,
            m_FieldOfViewVelocity,
            m_Initialized);

        public void RestoreState(State state)
        {
            m_Current = state.Current;
            m_PivotVelocity = state.PivotVelocity;
            m_RadiusVelocity = state.RadiusVelocity;
            m_OffsetVelocity = state.OffsetVelocity;
            m_FieldOfViewVelocity = state.FieldOfViewVelocity;
            m_Initialized = state.Initialized;
        }

        public CameraFramePlan Apply(CameraFramePlan target, in CameraFrameInput input)
        {
            if (!target.Valid)
                return target;
            if (!m_Initialized || input.ResetHistory || m_SmoothTime <= 0f)
            {
                SetCurrent(target.WorldBasicData);
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
            float fieldOfView = Mathf.SmoothDamp(
                m_Current.FieldOfView,
                target.FieldOfView,
                ref m_FieldOfViewVelocity,
                m_SmoothTime,
                Mathf.Infinity,
                deltaTime);
            float rotationAlpha = 1f - Mathf.Exp(-deltaTime / m_SmoothTime);
            Quaternion rotation = Quaternion.SlerpUnclamped(
                m_Current.Rotation,
                target.Rotation,
                rotationAlpha);
            SetCurrent(new CameraWorldBasicData(pivot, rotation, radius, offset, fieldOfView));
            return target.WithWorldBasicData(m_Current);
        }

        void SetCurrent(CameraWorldBasicData data)
        {
            m_Current = data;
            m_PivotVelocity = Vector3.zero;
            m_RadiusVelocity = 0f;
            m_OffsetVelocity = Vector2.zero;
            m_FieldOfViewVelocity = 0f;
            m_Initialized = true;
        }

        public readonly struct State
        {
            public State(
                CameraWorldBasicData current,
                Vector3 pivotVelocity,
                float radiusVelocity,
                Vector2 offsetVelocity,
                float fieldOfViewVelocity,
                bool initialized)
            {
                Current = current;
                PivotVelocity = pivotVelocity;
                RadiusVelocity = radiusVelocity;
                OffsetVelocity = offsetVelocity;
                FieldOfViewVelocity = fieldOfViewVelocity;
                Initialized = initialized;
            }

            public CameraWorldBasicData Current { get; }
            public Vector3 PivotVelocity { get; }
            public float RadiusVelocity { get; }
            public Vector2 OffsetVelocity { get; }
            public float FieldOfViewVelocity { get; }
            public bool Initialized { get; }
        }
    }
}
