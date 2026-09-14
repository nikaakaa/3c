using System;
using ThirdPersonCamera;
using UnityEngine;

namespace BTSMTL.Timeline
{
    public abstract class CameraResourceTrack : Track
    {
#if UNITY_EDITOR
        public override Clip AddClip(UnityEngine.Object referenceObject, int frame)
        {
            Clip clip = Activator.CreateInstance(ClipType, this, frame) as Clip;
            if (clip == null)
                throw new InvalidOperationException($"Camera track '{ContractKind}' could not create clip '{ClipType.Name}'.");
            switch (clip)
            {
                case CameraOverrideClip cameraOverride:
                    cameraOverride.OverrideTrack = referenceObject as CameraOverrideTrackAsset;
                    break;
                case CameraZoomClip cameraZoom:
                    cameraZoom.Zoom = referenceObject as CameraZoomAsset;
                    break;
                case CameraStretchClip cameraStretch:
                    cameraStretch.Stretch = referenceObject as CameraStretchAsset;
                    break;
                case CameraShotClip cameraShot:
                    cameraShot.Shot = referenceObject as CameraShotAsset;
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

    public abstract class CameraResourceClip : Clip
    {
        public AnimationCurve WeightCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
        public AnimationCurve EaseInCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        public AnimationCurve EaseOutCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

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

#if UNITY_EDITOR
        public override ClipCapabilities Capabilities => ClipCapabilities.Resizable | ClipCapabilities.Mixable;
        public CameraShotClip(Track track, int frame) : base(track, frame) { }
#endif
    }
}
