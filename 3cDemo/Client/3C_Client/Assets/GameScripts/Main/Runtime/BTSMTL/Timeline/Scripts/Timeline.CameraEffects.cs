using System;
using ThirdPersonSimulation.Fixed;
using System.Collections.Generic;
using BTSMTL.Diagnostics;
using ThirdPersonCamera;
using UnityEngine;

namespace BTSMTL.Timeline
{
    public enum TimelineCameraResourceKind : byte
    {
        Override = 1,
        Zoom = 2,
        Stretch = 3,
        Shot = 4
    }

    public readonly struct TimelineCameraResourceSample
    {
        public TimelineCameraResourceSample(
            string sourceId,
            string sourceName,
            string trackAuthoringId,
            string clipAuthoringId,
            TimelineCameraResourceKind kind,
            string resourceId,
            int priority,
            float weight,
            float normalizedTime)
        {
            SourceId = sourceId ?? string.Empty;
            SourceName = sourceName ?? string.Empty;
            TrackAuthoringId = trackAuthoringId ?? string.Empty;
            ClipAuthoringId = clipAuthoringId ?? string.Empty;
            Kind = kind;
            ResourceId = resourceId ?? string.Empty;
            Priority = priority;
            Weight = Mathf.Clamp01(weight);
            NormalizedTime = Mathf.Clamp01(normalizedTime);
        }

        public string SourceId { get; }
        public string SourceName { get; }
        public string TrackAuthoringId { get; }
        public string ClipAuthoringId { get; }
        public TimelineCameraResourceKind Kind { get; }
        public string ResourceId { get; }
        public int Priority { get; }
        public float Weight { get; }
        public float NormalizedTime { get; }
    }

    [TrackGroup("Camera"), ScriptGuid("8b4e2c6a91d3f75e0a62c48b19d7e35f"), Ordered(7), Color(236, 190, 120)]
    public sealed class CameraEffectTrack : Track
    {
        public override string ContractKind => TimelineContractKinds.CameraEffectTrack;

        public void Sample(
            FixedScalar timelineTime,
            string sourceId,
            string sourceName,
            ICollection<TimelineCameraResourceSample> samples)
        {
            if (m_PersistentMuted || samples == null)
                return;

            for (int clipIndex = 0; clipIndex < Clips.Count; clipIndex++)
            {
                if (Clips[clipIndex] is not CameraEffectClip clip ||
                    timelineTime < clip.StartTime ||
                    timelineTime > clip.EndTime)
                    continue;

                CameraEffectAsset effect = clip.Effect;
                if (!effect)
                    continue;

                float duration = Mathf.Max(0.0001f, clip.DurationTime.ToSingle());
                float selfTime = FixedScalar.Clamp(timelineTime - clip.StartTime, FixedScalar.Zero, clip.DurationTime).ToSingle();
                float remainTime = FixedScalar.Max(FixedScalar.Zero, clip.EndTime - timelineTime).ToSingle();
                float normalizedTime = Mathf.Clamp01(selfTime / duration);
                float weight = CameraTimelineSampling.SampleWeight(
                    clip.WeightCurve,
                    clip.EaseInCurve,
                    clip.EaseOutCurve,
                    normalizedTime,
                    selfTime,
                    remainTime,
                    clip.EaseInTime.ToSingle(),
                    clip.EaseOutTime.ToSingle());
                if (weight <= 0f)
                    continue;

                TimelineCameraResourceKind kind = effect switch
                {
                    CameraOverrideTrackAsset => TimelineCameraResourceKind.Override,
                    CameraZoomAsset => TimelineCameraResourceKind.Zoom,
                    CameraStretchAsset => TimelineCameraResourceKind.Stretch,
                    CameraShotAsset => TimelineCameraResourceKind.Shot,
                    _ => throw new InvalidOperationException(
                        $"Camera effect clip '{clip.AuthoringId}' references unsupported effect asset '{effect.GetType().Name}'.")
                };

                samples.Add(new TimelineCameraResourceSample(
                    sourceId,
                    sourceName,
                    AuthoringId,
                    clip.AuthoringId,
                    kind,
                    effect.EffectId,
                    effect.EffectPriority,
                    weight,
                    normalizedTime));
            }
        }

#if UNITY_EDITOR
        public override Type ClipType => typeof(CameraEffectClip);

        public override Clip AddClip(UnityEngine.Object referenceObject, FixedScalar time)
        {
            if (referenceObject is not CameraEffectAsset effect)
                throw new ArgumentException("Camera effect clip requires a CameraEffectAsset.", nameof(referenceObject));
            var clip = new CameraEffectClip(this, time)
            {
                Effect = effect
            };
            clip.RegenerateAuthoringIdentity();
            m_Clips.Add(clip);
            return clip;
        }
#endif
    }

    [ScriptGuid("8b4e2c6a91d3f75e0a62c48b19d7e35f"), Color(236, 190, 120)]
    public sealed class CameraEffectClip : Clip, ITimelineContentClosureSource
    {
        public override string ContractKind => TimelineContractKinds.CameraEffectClip;

        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public CameraEffectAsset Effect;

        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public AnimationCurve WeightCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public AnimationCurve EaseInCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public AnimationCurve EaseOutCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        public void CollectContentClosure(TimelineContentClosureBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            string resourceId = Effect ? Effect.EffectId : string.Empty;
            if (string.IsNullOrWhiteSpace(resourceId))
            {
                builder.AddError("timeline_camera_resource_missing", AuthoringId, "Camera effect clip has no formal resource identity.");
                return;
            }
            UnityEngine.Object resource = Effect;
            builder.AddDependency(
                $"camera:{ContractKind}:{resourceId}",
                ContractKind,
                $"clip:{AuthoringId}",
                SourceContentHasher.Hash(JsonUtility.ToJson(resource)));
        }

#if UNITY_EDITOR
        public override ClipCapabilities Capabilities => ClipCapabilities.Resizable | ClipCapabilities.Mixable;
        public CameraEffectClip(Track track, FixedScalar time) : base(track, time) { }
#endif
    }
}
