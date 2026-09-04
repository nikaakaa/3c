using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCamera
{
    public sealed class CameraEffectEvaluator
    {
        readonly CharacterCameraProjectionPayload m_Projection;
        readonly List<ActiveEffect> m_Active = new List<ActiveEffect>();
        readonly List<CameraEffectContribution> m_Contributions = new List<CameraEffectContribution>();

        public CameraEffectEvaluator(CharacterCameraProjectionPayload projection)
        {
            m_Projection = projection ?? throw new ArgumentNullException(nameof(projection));
        }

        public IReadOnlyList<CameraEffectContribution> Contributions => m_Contributions;

        public void Reset()
        {
            m_Active.Clear();
            m_Contributions.Clear();
        }

        public void Retire(string eventId, ulong generation, string sourceId = null)
        {
            for (int i = m_Active.Count - 1; i >= 0; i--)
            {
                ActiveEffect effect = m_Active[i];
                if (effect.Request.Generation == generation &&
                    (string.Equals(effect.Request.EventId, eventId, StringComparison.Ordinal) ||
                     !string.IsNullOrEmpty(sourceId) &&
                     string.Equals(effect.Request.SourceId, sourceId, StringComparison.Ordinal)))
                {
                    effect.Retired = true;
                    effect.RetireElapsed = 0f;
                }
            }
        }

        public CameraFramePlan Resolve(
            CameraFramePlan basePlan,
            IReadOnlyList<CameraEffectRequest> newRequests,
            in CameraFrameInput input)
        {
            AddRequests(newRequests);
            CameraFramePlan plan = ApplyOverride(basePlan, input);
            plan = ApplyZoom(plan, input);
            plan = ApplyStretch(plan, input);
            plan = ApplyShake(plan, input);
            plan = ApplyShot(plan, input);
            m_Contributions.Clear();
            for (int i = 0; i < m_Active.Count; i++)
            {
                ActiveEffect active = m_Active[i];
                m_Contributions.Add(new CameraEffectContribution(
                    ToStage(active.Request.Kind),
                    active.Request.ResourceId,
                    active.Request.Weight,
                    RemainingSeconds(active),
                    active.Request.Priority,
                    true));
            }
            Advance(input);
            return plan;
        }

        void AddRequests(IReadOnlyList<CameraEffectRequest> requests)
        {
            if (requests == null)
                return;
            for (int i = 0; i < requests.Count; i++)
            {
                CameraEffectRequest request = requests[i];
                if (!request.Active || ContainsRequest(request))
                    continue;
                RequireResource(request);
                if (request.Kind != CameraEffectKind.Shake)
                {
                    ActiveEffect existing = FindSource(request.SourceId, request.Generation);
                    if (existing != null)
                    {
                        existing.Request = request;
                        existing.Retired = false;
                        existing.RetireElapsed = 0f;
                        continue;
                    }
                }
                m_Active.Add(new ActiveEffect(request));
            }
        }

        CameraFramePlan ApplyOverride(CameraFramePlan plan, in CameraFrameInput input)
        {
            ActiveEffect active = Select(CameraEffectKind.Override);
            if (active == null || !m_Projection.TryGetOverride(active.Request.ResourceId, out CameraOverrideTrackPayload payload))
                return plan;
            float envelopeTime = active.Retired ? active.RetireElapsed : active.Elapsed;
            float weight = Mathf.Clamp01(active.Request.Weight) * EvaluateEnvelope(
                envelopeTime,
                payload.BlendInSeconds,
                payload.BlendOutSeconds,
                payload.BlendInCurve,
                payload.BlendOutCurve,
                active.Retired);
            weight *= ReleaseWeight(active, payload.BlendOutSeconds);
            Vector3 follow = plan.FollowPoint + payload.Settings.FollowOffset * weight;
            Vector3 aim = plan.AimPoint + payload.Settings.AimOffset * weight;
            return plan
                .WithTargets(follow, aim)
                .WithFieldOfView(Mathf.LerpUnclamped(plan.FieldOfView, payload.Settings.FieldOfView, weight));
        }

        CameraFramePlan ApplyZoom(CameraFramePlan plan, in CameraFrameInput input)
        {
            ActiveEffect active = Select(CameraEffectKind.Zoom);
            if (active == null || !m_Projection.TryGetZoom(active.Request.ResourceId, out CameraZoomPayload payload))
                return plan;
            float progress = EffectProgress(active.Elapsed, payload.DelayTime, payload.StartTime, payload.EndTime);
            if (progress <= 0f && active.Elapsed < payload.DelayTime + payload.StartTime)
                return plan;
            float start = payload.StartCurve.Evaluate(progress);
            float end = payload.EndCurve.Evaluate(progress);
            float envelope = Mathf.Clamp01(Mathf.LerpUnclamped(start, end, progress));
            float weight = active.Request.Weight * envelope * ReleaseWeight(active, Mathf.Max(0.016f, payload.EndTime - payload.StartTime));
            float fov = payload.FovVariationType switch
            {
                CameraFovVariationType.Additive => plan.FieldOfView + payload.FieldOfView * weight,
                CameraFovVariationType.Multiplicative => plan.FieldOfView * Mathf.LerpUnclamped(1f, payload.FieldOfView, weight),
                _ => Mathf.LerpUnclamped(plan.FieldOfView, payload.FieldOfView, weight)
            };
            return plan.WithFieldOfView(fov);
        }

        CameraFramePlan ApplyStretch(CameraFramePlan plan, in CameraFrameInput input)
        {
            ActiveEffect active = Select(CameraEffectKind.Stretch);
            if (active == null || !m_Projection.TryGetStretch(active.Request.ResourceId, out CameraStretchPayload payload))
                return plan;
            float progress = EffectProgress(active.Elapsed, payload.DelayTime, payload.StretchTime, payload.HoldTime);
            float start = payload.StartCurve.Evaluate(progress);
            float end = payload.EndCurve.Evaluate(progress);
            float envelope = Mathf.LerpUnclamped(start, end, progress) * active.Request.Weight;
            envelope *= ReleaseWeight(active, Mathf.Max(0.016f, payload.RecoilTime));
            float radiusScale = plan.RadiusScale + payload.RadiusRatio * envelope;
            Vector3 offset = payload.CamOffset * envelope;
            float pitch = plan.OrbitPitch;
            if (payload.IsElevationAngleAbsolute)
                pitch = Mathf.LerpUnclamped(pitch, payload.ElevationAngleMax, envelope);
            else
                pitch += Mathf.LerpUnclamped(payload.ElevationAngleMin, payload.ElevationAngleMax, envelope);
            return plan
                .WithRadiusScale(radiusScale)
                .WithCameraOffset(offset)
                .WithOrbit(plan.OrbitYaw, pitch, plan.OrbitRadius);
        }

        CameraFramePlan ApplyShake(CameraFramePlan plan, in CameraFrameInput input)
        {
            throw new InvalidOperationException(
                "Camera Shake evaluation is unavailable before its ZZZ consumer semantics are closed.");
        }

        CameraFramePlan ApplyShot(CameraFramePlan plan, in CameraFrameInput input)
        {
            ActiveEffect active = Select(CameraEffectKind.Shot);
            if (active == null || !m_Projection.TryGetShot(active.Request.ResourceId, out CameraShotPayload payload))
                return plan;
            float progress = payload.Duration < 0f
                ? 1f
                : Mathf.Clamp01(active.Elapsed / Mathf.Max(0.0001f, payload.Duration));
            float envelope = Mathf.Clamp01(active.Request.Weight) * progress;
            envelope *= ReleaseWeight(active, payload.BlendOut.Duration);
            return plan
                .WithTargets(
                    plan.FollowPoint + payload.FollowOffset * envelope,
                    plan.AimPoint + payload.LookAtOffset * envelope)
                .WithFieldOfView(Mathf.LerpUnclamped(plan.FieldOfView, payload.FieldOfView, envelope));
        }

        void Advance(in CameraFrameInput input)
        {
            for (int i = m_Active.Count - 1; i >= 0; i--)
            {
                ActiveEffect active = m_Active[i];
                float delta = ResolveDelta(active.Request.Kind, active, input);
                active.Elapsed += delta;
                if (active.Retired)
                    active.RetireElapsed += delta;
                if (IsExpired(active))
                    m_Active.RemoveAt(i);
            }
        }

        float ResolveDelta(CameraEffectKind kind, ActiveEffect active, in CameraFrameInput input)
        {
            switch (kind)
            {
                case CameraEffectKind.Shake:
                    if (m_Projection.TryGetShake(active.Request.ResourceId, out CameraShakePayload shake))
                        return shake.IgnoreTimeScale ? input.UnscaledDeltaSeconds : input.PresentationDeltaSeconds;
                    break;
                case CameraEffectKind.Zoom:
                    if (m_Projection.TryGetZoom(active.Request.ResourceId, out CameraZoomPayload zoom))
                        return ResolveEffectDelta(zoom.IgnoreWorldTimeScale, zoom.IgnoreOwnerTimeScale, zoom.IgnoreLocalAvatar, input);
                    break;
                case CameraEffectKind.Stretch:
                    if (m_Projection.TryGetStretch(active.Request.ResourceId, out CameraStretchPayload stretch))
                        return ResolveEffectDelta(stretch.IgnoreWorldTimeScale, stretch.IgnoreOwnerTimeScale, stretch.IgnoreLocalAvatar, input);
                    break;
                case CameraEffectKind.Override:
                    if (m_Projection.TryGetOverride(active.Request.ResourceId, out CameraOverrideTrackPayload track))
                        return ResolveEffectDelta(track.IgnoreWorldTimeScale, track.IgnoreOwnerTimeScale, track.IgnoreLocalAvatar, input);
                    break;
                case CameraEffectKind.Shot:
                    if (m_Projection.TryGetShot(active.Request.ResourceId, out CameraShotPayload shot))
                        return input.Delta(shot.TimeDomain);
                    break;
            }
            return input.PresentationDeltaSeconds;
        }

        static float ResolveEffectDelta(bool ignoreWorld, bool ignoreOwner, bool ignoreLocal, in CameraFrameInput input)
        {
            if (ignoreWorld || ignoreOwner || ignoreLocal)
                return input.UnscaledDeltaSeconds;
            return input.PresentationDeltaSeconds;
        }

        bool IsExpired(ActiveEffect active)
        {
            if (active.Retired)
                return active.RetireElapsed >= RetireDuration(active);
            switch (active.Request.Kind)
            {
                case CameraEffectKind.Shake:
                    return m_Projection.TryGetShake(active.Request.ResourceId, out CameraShakePayload shake) &&
                           active.Elapsed >= shake.ShakeTotalTime;
                case CameraEffectKind.Zoom:
                    return m_Projection.TryGetZoom(active.Request.ResourceId, out CameraZoomPayload zoom) &&
                           zoom.LastTime >= 0f && active.Elapsed >= zoom.LastTime;
                case CameraEffectKind.Stretch:
                    return m_Projection.TryGetStretch(active.Request.ResourceId, out CameraStretchPayload stretch) &&
                           stretch.HoldTime >= 0f && active.Elapsed >= stretch.DelayTime + stretch.StretchTime + stretch.HoldTime + stretch.RecoilTime;
                case CameraEffectKind.Shot:
                    return m_Projection.TryGetShot(active.Request.ResourceId, out CameraShotPayload shot) &&
                           shot.Duration >= 0f && active.Elapsed >= shot.Duration;
                case CameraEffectKind.Override:
                    return m_Projection.TryGetOverride(active.Request.ResourceId, out CameraOverrideTrackPayload track) &&
                           track.Duration >= 0f && active.Elapsed >= track.Duration;
                default:
                    return false;
            }
        }

        ActiveEffect Select(CameraEffectKind kind)
        {
            ActiveEffect selected = null;
            for (int i = 0; i < m_Active.Count; i++)
            {
                ActiveEffect candidate = m_Active[i];
                if (candidate.Request.Kind != kind ||
                    selected != null && candidate.Request.Priority <= selected.Request.Priority)
                    continue;
                selected = candidate;
            }
            return selected;
        }

        bool ContainsRequest(CameraEffectRequest request)
        {
            for (int i = 0; i < m_Active.Count; i++)
            {
                CameraEffectRequest active = m_Active[i].Request;
                if (request.Kind == CameraEffectKind.Shake &&
                    active.Generation == request.Generation &&
                    string.Equals(active.EventId, request.EventId, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        ActiveEffect FindSource(string sourceId, ulong generation)
        {
            for (int i = 0; i < m_Active.Count; i++)
            {
                ActiveEffect active = m_Active[i];
                if (active.Request.Generation == generation &&
                    string.Equals(active.Request.SourceId, sourceId, StringComparison.Ordinal))
                    return active;
            }
            return null;
        }

        void RequireResource(CameraEffectRequest request)
        {
            bool exists = request.Kind switch
            {
                CameraEffectKind.Override => m_Projection.TryGetOverride(request.ResourceId, out _),
                CameraEffectKind.Zoom => m_Projection.TryGetZoom(request.ResourceId, out _),
                CameraEffectKind.Stretch => m_Projection.TryGetStretch(request.ResourceId, out _),
                CameraEffectKind.Shake => m_Projection.TryGetShake(request.ResourceId, out _),
                CameraEffectKind.Shot => m_Projection.TryGetShot(request.ResourceId, out _),
                _ => false
            };
            if (!exists)
                throw new InvalidOperationException($"Camera effect resource '{request.ResourceId}' is not present in the Projection.");
        }

        static float EvaluateEnvelope(
            float elapsed,
            float blendInSeconds,
            float blendOutSeconds,
            CameraCurvePayload blendInCurve,
            CameraCurvePayload blendOutCurve,
            bool retired)
        {
            if (retired)
            {
                if (blendOutSeconds <= 0f)
                    return 0f;
                return 1f - Mathf.Clamp01(blendOutCurve.Evaluate(Mathf.Clamp01(elapsed / blendOutSeconds)));
            }
            if (blendInSeconds <= 0f)
                return 1f;
            return Mathf.Clamp01(blendInCurve.Evaluate(Mathf.Clamp01(elapsed / blendInSeconds)));
        }

        static float EffectProgress(float elapsed, float delay, float start, float end)
        {
            float duration = Mathf.Max(0.0001f, end - start);
            return Mathf.Clamp01((elapsed - delay - start) / duration);
        }

        static CameraEffectStage ToStage(CameraEffectKind kind)
        {
            switch (kind)
            {
                case CameraEffectKind.Override: return CameraEffectStage.Override;
                case CameraEffectKind.Zoom: return CameraEffectStage.Zoom;
                case CameraEffectKind.Stretch: return CameraEffectStage.Stretch;
                case CameraEffectKind.Shake: return CameraEffectStage.Shake;
                default: return CameraEffectStage.Shot;
            }
        }

        float RetireDuration(ActiveEffect active)
        {
            switch (active.Request.Kind)
            {
                case CameraEffectKind.Override:
                    return m_Projection.TryGetOverride(active.Request.ResourceId, out CameraOverrideTrackPayload track)
                        ? track.BlendOutSeconds
                        : 0.016f;
                case CameraEffectKind.Shake:
                    return m_Projection.TryGetShake(active.Request.ResourceId, out CameraShakePayload shake)
                        ? Mathf.Max(0.016f, shake.FadeOutDuration)
                        : 0.016f;
                case CameraEffectKind.Shot:
                    return m_Projection.TryGetShot(active.Request.ResourceId, out CameraShotPayload shot)
                        ? Mathf.Max(0.016f, shot.BlendOut.Duration)
                        : 0.016f;
                case CameraEffectKind.Stretch:
                    return m_Projection.TryGetStretch(active.Request.ResourceId, out CameraStretchPayload stretch)
                        ? Mathf.Max(0.016f, stretch.RecoilTime)
                        : 0.016f;
                default:
                    return m_Projection.TryGetZoom(active.Request.ResourceId, out CameraZoomPayload zoom)
                        ? Mathf.Max(0.016f, zoom.EndTime - zoom.StartTime)
                        : 0.016f;
            }
        }

        float ReleaseWeight(ActiveEffect active, float duration) =>
            !active.Retired ? 1f : Mathf.Clamp01(1f - active.RetireElapsed / Mathf.Max(0.016f, duration));

        float RemainingSeconds(ActiveEffect active) =>
            active.Retired ? Mathf.Max(0f, RetireDuration(active) - active.RetireElapsed) : float.PositiveInfinity;

        sealed class ActiveEffect
        {
            public ActiveEffect(CameraEffectRequest request)
            {
                Request = request;
            }

            public CameraEffectRequest Request { get; set; }
            public float Elapsed;
            public bool Retired;
            public float RetireElapsed;
        }
    }
}
