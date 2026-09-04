using System;
using System.Collections.Generic;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCamera;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal static class CharacterCameraProducerBindingCompiler
    {
        public static CharacterPresentationCameraBinding Build(
            CharacterPresentationSemanticReader reader,
            ProgramProducer producer,
            ProgramSourceMapEntry source,
            IReadOnlyDictionary<string, TimelineData> timelines,
            CharacterCameraProjectionPayload cameraProjection,
            List<string> errors)
        {
            if (TryFindSourceClip(source, timelines, out Clip clip))
            {
                if (clip is CameraSequenceClip state)
                {
                    if (!state.Sequence)
                    {
                        errors?.Add($"Camera Sequence Clip '{state.AuthoringId}' has no Sequence resource.");
                        return null;
                    }
                    if (cameraProjection == null || !cameraProjection.TryGetSequence(state.Sequence.SequenceId, out _))
                    {
                        errors?.Add($"Camera Sequence '{state.Sequence.SequenceId}' is not registered by the Character Camera Profile.");
                        return null;
                    }
                    return CharacterPresentationCameraBinding.Sequence(
                        state.Sequence.SequenceId,
                        state.Priority,
                        state.BlendInSeconds,
                        state.BlendOutSeconds,
                        state.TargetKey,
                        state.InterruptPolicy);
                }
                if (clip is CameraShakeClip cue)
                {
                    if (!cue.Shake)
                    {
                        errors?.Add($"Camera Shake Clip '{cue.AuthoringId}' has no Shake resource.");
                        return null;
                    }
                    if (cameraProjection == null || !cameraProjection.TryGetShake(cue.Shake.ShakeId, out _))
                    {
                        errors?.Add($"Camera Shake '{cue.Shake.ShakeId}' is not registered by the Character Camera Profile.");
                        return null;
                    }
                    return CharacterPresentationCameraBinding.Effect(
                        CharacterPresentationCameraBindingKind.Shake,
                        cue.Shake.ShakeId,
                        cue.Priority);
                }
                if (clip is CameraOverrideClip overrideClip)
                {
                    if (!overrideClip.OverrideTrack || cameraProjection == null ||
                        !cameraProjection.TryGetOverride(overrideClip.OverrideTrack.TrackId, out _))
                    {
                        errors?.Add($"Camera Override Clip '{overrideClip.AuthoringId}' has no registered Override resource.");
                        return null;
                    }
                    return CharacterPresentationCameraBinding.Effect(
                        CharacterPresentationCameraBindingKind.Override,
                        overrideClip.OverrideTrack.TrackId,
                        overrideClip.OverrideTrack.Priority);
                }
                if (clip is CameraZoomClip zoomClip)
                {
                    if (!zoomClip.Zoom || cameraProjection == null ||
                        !cameraProjection.TryGetZoom(zoomClip.Zoom.ZoomId, out _))
                    {
                        errors?.Add($"Camera Zoom Clip '{zoomClip.AuthoringId}' has no registered Zoom resource.");
                        return null;
                    }
                    return CharacterPresentationCameraBinding.Effect(
                        CharacterPresentationCameraBindingKind.Zoom,
                        zoomClip.Zoom.ZoomId,
                        zoomClip.Zoom.DataPriority);
                }
                if (clip is CameraStretchClip stretchClip)
                {
                    if (!stretchClip.Stretch || cameraProjection == null ||
                        !cameraProjection.TryGetStretch(stretchClip.Stretch.StretchId, out _))
                    {
                        errors?.Add($"Camera Stretch Clip '{stretchClip.AuthoringId}' has no registered Stretch resource.");
                        return null;
                    }
                    return CharacterPresentationCameraBinding.Effect(
                        CharacterPresentationCameraBindingKind.Stretch,
                        stretchClip.Stretch.StretchId,
                        stretchClip.Stretch.DataPriority);
                }
                if (clip is CameraShotClip shotClip)
                {
                    if (!shotClip.Shot || cameraProjection == null ||
                        !cameraProjection.TryGetShot(shotClip.Shot.ShotId, out _))
                    {
                        errors?.Add($"Camera Shot Clip '{shotClip.AuthoringId}' has no registered Shot resource.");
                        return null;
                    }
                    return CharacterPresentationCameraBinding.Effect(
                        CharacterPresentationCameraBindingKind.Shot,
                        shotClip.Shot.ShotId,
                        shotClip.Shot.Priority);
                }
                if (clip is CameraResponseClip response)
                {
                    return CharacterPresentationCameraBinding.Response(
                        response.LookResponse,
                        response.ManualOrbitWeight,
                        response.PitchResponseWeight,
                        response.YawResponseWeight,
                        response.Priority);
                }
                errors?.Add($"Camera producer '{producer.Identity}' source clip type '{clip.GetType().Name}' is unsupported.");
                return null;
            }

            try
            {
                SemanticOperation operation = reader.RequireProducerOperation(producer);
                if (operation.Integer0 != CameraProgramOperationSchema.PayloadVersion)
                    throw new InvalidOperationException($"payload version '{operation.Integer0}' is unsupported");
                if (operation.Code == SimulationOperationCode.CameraSequenceRequest)
                {
                    string sequenceId = reader.RequireString(operation, "SequenceId");
                    if (cameraProjection == null || !cameraProjection.TryGetSequence(sequenceId, out _))
                        throw new InvalidOperationException($"Camera Sequence '{sequenceId}' is not registered by the Character Camera Profile.");
                }
                if (operation.Code == SimulationOperationCode.CameraShakeRequest)
                {
                    string resourceId = reader.RequireString(operation, "ResourceId");
                    if (cameraProjection == null || !cameraProjection.TryGetShake(resourceId, out _))
                        throw new InvalidOperationException($"Camera Shake '{resourceId}' is not registered by the Character Camera Profile.");
                }
                return operation.Code switch
                {
                    SimulationOperationCode.CameraSequenceRequest => CharacterPresentationCameraBinding.Sequence(
                        reader.RequireString(operation, "SequenceId"),
                        reader.RequireInt32(operation, "Priority"),
                        reader.RequireScalar(operation, "BlendInSeconds"),
                        reader.RequireScalar(operation, "BlendOutSeconds"),
                        reader.RequireString(operation, "TargetKey"),
                        (CameraSequenceInterruptPolicy)operation.Flags),
                    SimulationOperationCode.CameraShakeRequest => CharacterPresentationCameraBinding.Effect(
                        CharacterPresentationCameraBindingKind.Shake,
                        reader.RequireString(operation, "ResourceId"),
                        reader.RequireInt32(operation, "Priority")),
                    SimulationOperationCode.CameraResponse => CharacterPresentationCameraBinding.Response(
                        (CameraResponseMode)operation.Integer1,
                        reader.RequireScalar(operation, "ManualOrbitWeight"),
                        reader.RequireScalar(operation, "PitchResponseWeight"),
                        reader.RequireScalar(operation, "YawResponseWeight"),
                        reader.RequireInt32(operation, "Priority")),
                    SimulationOperationCode.CameraTarget => CharacterPresentationCameraBinding.Target(
                        reader.RequireString(operation, "TargetKey"),
                        reader.RequireString(operation, "AnchorKey"),
                        reader.RequireString(operation, "AimPointKey"),
                        reader.RequireString(operation, "PreferredBoneKey"),
                        reader.RequireInt32(operation, "Priority")),
                    _ => throw new InvalidOperationException($"operation '{operation.Code}' is unsupported")
                };
            }
            catch (Exception exception)
            {
                errors?.Add($"Camera producer '{producer.Identity}' Graph payload is invalid: {exception.Message}.");
                return null;
            }
        }

        static bool TryFindSourceClip(
            ProgramSourceMapEntry source,
            IReadOnlyDictionary<string, TimelineData> timelines,
            out Clip clip)
        {
            clip = null;
            if (source == null || string.IsNullOrEmpty(source.TimelineId) || string.IsNullOrEmpty(source.ClipId) ||
                !timelines.TryGetValue(source.TimelineId, out TimelineData timeline))
                return false;
            for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
            {
                Track track = timeline.Tracks[trackIndex];
                for (int clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
                {
                    if (string.Equals(track.Clips[clipIndex].AuthoringId, source.ClipId, StringComparison.Ordinal))
                    {
                        clip = track.Clips[clipIndex];
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
