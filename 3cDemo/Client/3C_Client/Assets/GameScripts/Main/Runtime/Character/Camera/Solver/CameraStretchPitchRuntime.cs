using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCamera
{
    internal sealed class CameraStretchPitchRuntime
    {
        readonly CharacterCameraProjectionPayload m_Projection;

        public CameraStretchPitchRuntime(CharacterCameraProjectionPayload projection)
        {
            m_Projection = projection;
        }

        public float Evaluate(IReadOnlyList<CameraEffectRuntimeState> states, float elevation,
            bool rotationControl, in CameraFrameInput input)
        {
            CameraEffectRuntimeState selected = null;
            bool selectEnd = false;
            int selectedIndex = -1;
            for (int i = 0; i < states.Count; i++)
            {
                CameraEffectRuntimeState state = states[i];
                if (state.Request.Kind != CameraEffectKind.Stretch)
                    continue;
                m_Projection.TryGetStretch(state.Request.ResourceId, out CameraStretchPayload payload);
                if (!state.StretchPitchInitialized)
                {
                    state.StretchPitchInitialized = true;
                    if (!rotationControl && payload.PlayStackingType != CameraEffectStackingType.Add)
                        Configure(state, payload, elevation);
                }
                if (state.Retired && !state.StretchPitchRetired)
                {
                    state.StretchPitchRetired = true;
                    state.StartPitch.Stop();
                    state.EndPitch.ReleaseDelay();
                }
                float delta = CameraEffectEvaluationMath.ResolveDelta(
                    payload.IgnoreWorldTimeScale, payload.IgnoreOwnerTimeScale, in input);
                state.StartPitch.AdvanceDelay(delta, rotationControl, false);
                state.EndPitch.AdvanceDelay(delta, rotationControl, true);
                if (selected == null && state.StartPitch.Ready)
                {
                    selected = state;
                    selectedIndex = i;
                }
                else if (selected == null && state.EndPitch.Ready)
                {
                    selected = state;
                    selectedIndex = i;
                    selectEnd = true;
                }
            }
            if (selected != null)
            {
                for (int i = 0; i < states.Count; i++)
                {
                    CameraEffectRuntimeState state = states[i];
                    if (state.Request.Kind != CameraEffectKind.Stretch)
                        continue;
                    if (state.StartPitch.Active || i < selectedIndex)
                        state.StartPitch.Stop();
                    if (state.EndPitch.Active || i < selectedIndex)
                        state.EndPitch.Stop();
                }
                if (selectEnd)
                    selected.EndPitch.Begin(elevation, m_Projection.Input.ElevationRange, true);
                else
                    selected.StartPitch.Begin(elevation, m_Projection.Input.ElevationRange, false);
            }
            for (int i = 0; i < states.Count; i++)
            {
                CameraEffectRuntimeState state = states[i];
                if (state.Request.Kind != CameraEffectKind.Stretch)
                    continue;
                m_Projection.TryGetStretch(state.Request.ResourceId, out CameraStretchPayload payload);
                float delta = CameraEffectEvaluationMath.ResolveDelta(
                    payload.IgnoreWorldTimeScale, payload.IgnoreOwnerTimeScale, in input);
                if (state.StartPitch.Active)
                    elevation = state.StartPitch.Sample(delta, elevation, rotationControl,
                        m_Projection.Input.ElevationRange);
                if (state.EndPitch.Active)
                    elevation = state.EndPitch.Sample(delta, elevation, rotationControl,
                        m_Projection.Input.ElevationRange);
            }
            return elevation;
        }

        void Configure(CameraEffectRuntimeState state, CameraStretchPayload payload, float elevation)
        {
            float configuredStart = elevation;
            if (payload.IsAppliedElevationRatio)
            {
                configuredStart = ConfigureAngle(elevation, payload.ElevationAngleMin,
                    payload.ElevationAngleMax, payload.IsElevationAngleAbsolute, false);
                state.StartPitch = new CameraStretchPitchInstance(configuredStart,
                    payload.IsElevationAngleAbsolute, payload.DelayTime, payload.StretchTime, payload.StartCurve);
            }
            float endDelay = payload.HoldTime < 0f ? -1f
                : payload.DelayTime + payload.StretchTime + payload.HoldTime;
            if (payload.IsAppliedEndElevationAngle)
            {
                float configuredEnd = ConfigureAngle(configuredStart, payload.EndElevationAngleMin,
                    payload.EndElevationAngleMax, payload.IsEndElevationAngleAbsolute, true);
                state.EndPitch = new CameraStretchPitchInstance(configuredEnd,
                    payload.IsEndElevationAngleAbsolute, endDelay, payload.RecoilTime, payload.EndCurve);
            }
            else if (payload.IsAppliedElevationRatio &&
                     (payload.IsElevationAngleAbsolute ? configuredStart : configuredStart + elevation)
                     > m_Projection.Input.ElevationRange.y)
                state.EndPitch = new CameraStretchPitchInstance(m_Projection.Input.ElevationRange.y,
                    true, endDelay, payload.RecoilTime, payload.EndCurve);
        }

        static float ConfigureAngle(float current, float minimum, float maximum, bool absolute, bool end)
        {
            if (end ? current > maximum : current >= maximum)
                return absolute ? maximum : maximum - current;
            if (end ? current < minimum : current <= minimum)
                return absolute ? minimum : current - (end ? minimum : maximum);
            return current;
        }
    }

    internal struct CameraStretchPitchInstance
    {
        enum Phase : byte { None, Pending, Ready, Active, Complete }
        Phase m_Phase;
        readonly float m_ConfiguredTarget;
        readonly bool m_Absolute;
        float m_Delay;
        readonly float m_Duration;
        readonly CameraCurvePayload m_Curve;
        float m_Elapsed;
        float m_From;
        float m_Target;

        public CameraStretchPitchInstance(float target, bool absolute, float delay,
            float duration, CameraCurvePayload curve)
        {
            m_Phase = Phase.Pending;
            m_ConfiguredTarget = target;
            m_Absolute = absolute;
            m_Delay = delay;
            m_Duration = duration;
            m_Curve = curve;
            m_Elapsed = 0f;
            m_From = 0f;
            m_Target = 0f;
        }

        public bool Ready => m_Phase == Phase.Ready;
        public bool Active => m_Phase == Phase.Active;

        public void Stop() => m_Phase = Phase.Complete;

        public void ReleaseDelay()
        {
            if (m_Phase != Phase.Pending)
                return;
            m_Delay = 0f;
            m_Elapsed = 0f;
        }

        public void AdvanceDelay(float delta, bool rotationControl, bool end)
        {
            if (m_Phase != Phase.Pending)
                return;
            if (m_Delay >= 0f && m_Elapsed >= m_Delay)
                m_Phase = Phase.Ready;
            else
                m_Elapsed += delta;
            if (rotationControl)
                m_Phase = end ? Phase.Ready : Phase.Complete;
        }

        public void Begin(float elevation, Vector2 range, bool end)
        {
            m_Phase = Phase.Active;
            m_Elapsed = 0f;
            m_From = elevation;
            m_Target = Mathf.Clamp((m_Absolute ? 0f : Mathf.Clamp(elevation, range.x, range.y))
                + m_ConfiguredTarget, range.x, end ? range.y : 1.5f);
        }

        public float Sample(float delta, float elevation, bool rotationControl, Vector2 range)
        {
            if (rotationControl)
            {
                m_From = elevation;
                m_Target = Mathf.Clamp(elevation, range.x, range.y);
            }
            float t = m_Duration == 0f ? 1f : m_Elapsed / m_Duration;
            float result = Mathf.Lerp(m_From, m_Target, m_Curve.Evaluate(t));
            if (m_Elapsed >= m_Duration)
                m_Phase = Phase.Complete;
            m_Elapsed += delta;
            return result;
        }
    }
}
