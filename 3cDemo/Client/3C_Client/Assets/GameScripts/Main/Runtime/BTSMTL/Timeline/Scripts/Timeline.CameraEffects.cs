using System;
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

    public abstract class CameraResourceTrack : Track
    {
        public void Sample(
            float timelineTime,
            string sourceId,
            string sourceName,
            ICollection<TimelineCameraResourceSample> samples)
        {
            if (m_PersistentMuted || samples == null)
                return;

            for (int clipIndex = 0; clipIndex < Clips.Count; clipIndex++)
            {
                if (Clips[clipIndex] is not CameraResourceClip clip ||
                    timelineTime < clip.StartTime ||
                    timelineTime > clip.EndTime)
                    continue;

                float duration = Mathf.Max(0.0001f, clip.DurationTime);
                float selfTime = Mathf.Clamp(timelineTime - clip.StartTime, 0f, clip.DurationTime);
                float remainTime = Mathf.Max(0f, clip.EndTime - timelineTime);
                float normalizedTime = Mathf.Clamp01(selfTime / duration);
                float weight = CameraTimelineSampling.SampleWeight(
                    clip.WeightCurve,
                    clip.EaseInCurve,
                    clip.EaseOutCurve,
                    normalizedTime,
                    selfTime,
                    remainTime,
                    clip.EaseInTime,
                    clip.EaseOutTime);
                if (weight <= 0f)
                    continue;

                switch (clip)
                {
                    case CameraOverrideClip cameraOverride:
                        samples.Add(new TimelineCameraResourceSample(
                            sourceId,
                            sourceName,
                            AuthoringId,
                            clip.AuthoringId,
                            TimelineCameraResourceKind.Override,
                            cameraOverride.OverrideTrack ? cameraOverride.OverrideTrack.TrackId : string.Empty,
                            cameraOverride.OverrideTrack ? cameraOverride.OverrideTrack.Priority : 0,
                            weight,
                            normalizedTime));
                        break;
                    case CameraZoomClip cameraZoom:
                        samples.Add(new TimelineCameraResourceSample(
                            sourceId,
                            sourceName,
                            AuthoringId,
                            clip.AuthoringId,
                            TimelineCameraResourceKind.Zoom,
                            cameraZoom.Zoom ? cameraZoom.Zoom.ZoomId : string.Empty,
                            cameraZoom.Zoom ? cameraZoom.Zoom.DataPriority : 0,
                            weight,
                            normalizedTime));
                        break;
                    case CameraStretchClip cameraStretch:
                        samples.Add(new TimelineCameraResourceSample(
                            sourceId,
                            sourceName,
                            AuthoringId,
                            clip.AuthoringId,
                            TimelineCameraResourceKind.Stretch,
                            cameraStretch.Stretch ? cameraStretch.Stretch.StretchId : string.Empty,
                            cameraStretch.Stretch ? cameraStretch.Stretch.DataPriority : 0,
                            weight,
                            normalizedTime));
                        break;
                    case CameraShotClip cameraShot:
                        samples.Add(new TimelineCameraResourceSample(
                            sourceId,
                            sourceName,
                            AuthoringId,
                            clip.AuthoringId,
                            TimelineCameraResourceKind.Shot,
                            cameraShot.Shot ? cameraShot.Shot.ShotId : string.Empty,
                            cameraShot.Shot ? cameraShot.Shot.Priority : 0,
                            weight,
                            normalizedTime));
                        break;
                }
            }
        }

#if UNITY_EDITOR
        public override Clip AddClip(UnityEngine.Object referenceObject, int frame)
        {
            Clip clip = Activator.CreateInstance(ClipType, this, frame) as Clip;
            if (clip == null)
                throw new InvalidOperationException($"Camera track '{ContractKind}' could not create clip '{ClipType.Name}'.");
            switch (clip)
            {
                case CameraOverrideClip cameraOverride:
                    cameraOverride.OverrideTrack = referenceObject as CameraOverrideTrackAsset
                        ?? throw new ArgumentException("Camera Override clip requires a CameraOverrideTrackAsset.", nameof(referenceObject));
                    break;
                case CameraZoomClip cameraZoom:
                    cameraZoom.Zoom = referenceObject as CameraZoomAsset
                        ?? throw new ArgumentException("Camera Zoom clip requires a CameraZoomAsset.", nameof(referenceObject));
                    break;
                case CameraStretchClip cameraStretch:
                    cameraStretch.Stretch = referenceObject as CameraStretchAsset
                        ?? throw new ArgumentException("Camera Stretch clip requires a CameraStretchAsset.", nameof(referenceObject));
                    break;
                case CameraShotClip cameraShot:
                    cameraShot.Shot = referenceObject as CameraShotAsset
                        ?? throw new ArgumentException("Camera Shot clip requires a CameraShotAsset.", nameof(referenceObject));
                    break;
                default:
                    throw new InvalidOperationException($"Camera track '{ContractKind}' has an unsupported clip type '{clip.GetType().Name}'.");
            }
            clip.RegenerateAuthoringIdentity();
            m_Clips.Add(clip);
            return clip;
        }
#endif
    }

    [TrackGroup("Camera"), ScriptGuid("de0a9b796b3c4d1a8f5e02af91d63c74"), Ordered(7), Color(255, 196, 130)]
    public sealed class CameraOverrideTrack : CameraResourceTrack
    {
        public override string ContractKind => TimelineContractKinds.CameraOverrideTrack;

#if UNITY_EDITOR
        public override Type ClipType => typeof(CameraOverrideClip);
#endif
    }

    [TrackGroup("Camera"), ScriptGuid("5f5bb8b56d0d4a49b9d7b31db7dd2c10"), Ordered(8), Color(255, 210, 130)]
    public sealed class CameraZoomTrack : CameraResourceTrack
    {
        public override string ContractKind => TimelineContractKinds.CameraZoomTrack;

#if UNITY_EDITOR
        public override Type ClipType => typeof(CameraZoomClip);
#endif
    }

    [TrackGroup("Camera"), ScriptGuid("f0e09aaf6ec44896b63ac2ad7e2661e4"), Ordered(9), Color(255, 180, 130)]
    public sealed class CameraStretchTrack : CameraResourceTrack
    {
        public override string ContractKind => TimelineContractKinds.CameraStretchTrack;

#if UNITY_EDITOR
        public override Type ClipType => typeof(CameraStretchClip);
#endif
    }

    [TrackGroup("Camera"), ScriptGuid("a7dc7e5292c84b4584317a9f4f071f6d"), Ordered(10), Color(220, 180, 255)]
    public sealed class CameraShotTrack : CameraResourceTrack
    {
        public override string ContractKind => TimelineContractKinds.CameraShotTrack;

#if UNITY_EDITOR
        public override Type ClipType => typeof(CameraShotClip);
#endif
    }

    public abstract class CameraResourceClip : Clip, ITimelineContentClosureSource
    {
        public AnimationCurve WeightCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
        public AnimationCurve EaseInCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        public AnimationCurve EaseOutCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        public void CollectContentClosure(TimelineContentClosureBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            string resourceId = ResourceId;
            if (string.IsNullOrWhiteSpace(resourceId))
            {
                builder.AddError("timeline_camera_resource_missing", AuthoringId, "Camera resource clip has no formal resource identity.");
                return;
            }
            UnityEngine.Object resource = Resource;
            if (!resource)
            {
                builder.AddError("timeline_camera_resource_missing", AuthoringId, $"Camera resource '{resourceId}' is not assigned.");
                return;
            }
            builder.AddDependency(
                $"camera:{ContractKind}:{resourceId}",
                ContractKind,
                $"clip:{AuthoringId}",
                SourceContentHasher.Hash(JsonUtility.ToJson(resource)));
        }

        protected abstract string ResourceId { get; }
        protected abstract UnityEngine.Object Resource { get; }

#if UNITY_EDITOR
        protected CameraResourceClip(Track track, int frame) : base(track, frame)
        {
        }
#endif
    }

    [ScriptGuid("de0a9b796b3c4d1a8f5e02af91d63c74"), Color(255, 196, 130)]
    public sealed class CameraOverrideClip : CameraResourceClip
    {
        public override string ContractKind => TimelineContractKinds.CameraOverrideClip;

        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public CameraOverrideTrackAsset OverrideTrack;

        protected override string ResourceId => OverrideTrack ? OverrideTrack.TrackId : string.Empty;
        protected override UnityEngine.Object Resource => OverrideTrack;

#if UNITY_EDITOR
        public override ClipCapabilities Capabilities => ClipCapabilities.Resizable | ClipCapabilities.Mixable;
        public CameraOverrideClip(Track track, int frame) : base(track, frame) { }
#endif
    }

    [ScriptGuid("5f5bb8b56d0d4a49b9d7b31db7dd2c10"), Color(255, 210, 130)]
    public sealed class CameraZoomClip : CameraResourceClip
    {
        public override string ContractKind => TimelineContractKinds.CameraZoomClip;

        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public CameraZoomAsset Zoom;

        protected override string ResourceId => Zoom ? Zoom.ZoomId : string.Empty;
        protected override UnityEngine.Object Resource => Zoom;

#if UNITY_EDITOR
        public override ClipCapabilities Capabilities => ClipCapabilities.Resizable | ClipCapabilities.Mixable;
        public CameraZoomClip(Track track, int frame) : base(track, frame) { }
#endif
    }

    [ScriptGuid("f0e09aaf6ec44896b63ac2ad7e2661e4"), Color(255, 180, 130)]
    public sealed class CameraStretchClip : CameraResourceClip
    {
        public override string ContractKind => TimelineContractKinds.CameraStretchClip;

        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public CameraStretchAsset Stretch;

        protected override string ResourceId => Stretch ? Stretch.StretchId : string.Empty;
        protected override UnityEngine.Object Resource => Stretch;

#if UNITY_EDITOR
        public override ClipCapabilities Capabilities => ClipCapabilities.Resizable | ClipCapabilities.Mixable;
        public CameraStretchClip(Track track, int frame) : base(track, frame) { }
#endif
    }

    [ScriptGuid("a7dc7e5292c84b4584317a9f4f071f6d"), Color(220, 180, 255)]
    public sealed class CameraShotClip : CameraResourceClip
    {
        public override string ContractKind => TimelineContractKinds.CameraShotClip;

        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public CameraShotAsset Shot;

        protected override string ResourceId => Shot ? Shot.ShotId : string.Empty;
        protected override UnityEngine.Object Resource => Shot;

#if UNITY_EDITOR
        public override ClipCapabilities Capabilities => ClipCapabilities.Resizable | ClipCapabilities.Mixable;
        public CameraShotClip(Track track, int frame) : base(track, frame) { }
#endif
    }
}
