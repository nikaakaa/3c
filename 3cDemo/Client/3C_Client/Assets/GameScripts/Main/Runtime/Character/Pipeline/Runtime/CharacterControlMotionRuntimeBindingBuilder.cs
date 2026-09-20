using System;
using ThirdPersonSimulation.Fixed;
using System.Collections.Generic;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Pipeline.Motion.RootMotion;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline
{
    public static class CharacterControlMotionRuntimeBindingBuilder
    {
        public static CharacterControlMotionBindingCatalog Build(
            CharacterPipelineDefinition definition,
            CharacterControlModuleContract controlModule)
        {
            if (!definition)
                throw new ArgumentNullException(nameof(definition));
            if (controlModule == null)
                throw new ArgumentNullException(nameof(controlModule));
            var bindings = new List<CharacterControlMotionBinding>();
            var identities = new HashSet<string>(StringComparer.Ordinal);
            IReadOnlyList<TimelineAsset> timelines = definition.ControlMotionTimelines;
            for (int timelineIndex = 0; timelineIndex < timelines.Count; timelineIndex++)
            {
                TimelineAsset timeline = timelines[timelineIndex]
                    ? timelines[timelineIndex]
                    : throw new InvalidOperationException($"Character Definition '{definition.name}' Control Motion timeline #{timelineIndex} is missing.");
                TimelineData data = timeline.Data
                    ?? throw new InvalidOperationException($"Control Motion timeline '{timeline.name}' has no TimelineData.");
                for (int trackIndex = 0; trackIndex < data.Tracks.Count; trackIndex++)
                {
                    if (!(data.Tracks[trackIndex] is MotionCurveTrack track))
                        continue;
                    for (int clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
                    {
                        if (!(track.Clips[clipIndex] is MotionCurveClip clip))
                            continue;
                        string sourceIdentity = $"timeline:{data.AuthoringId}/track:{track.AuthoringId}/clip:{clip.AuthoringId}";
                        if (!identities.Add(sourceIdentity))
                            throw new InvalidOperationException($"Character Definition '{definition.name}' has duplicate Control Motion source '{sourceIdentity}'.");
                        RootMotionCurveAsset source = clip.SourceCurve
                            ? clip.SourceCurve
                            : throw new InvalidOperationException($"MotionCurveClip '{sourceIdentity}' has no RootMotionCurveAsset source.");
                        if (!source.TryValidate(out string error))
                            throw new InvalidOperationException($"MotionCurveClip '{sourceIdentity}' source is invalid: {error}");
                        CharacterControlMotionCurve positionX = CreateCurve(source.LocalPositionX, $"{sourceIdentity}/PositionX");
                        CharacterControlMotionCurve positionY = CreateCurve(source.LocalPositionY, $"{sourceIdentity}/PositionY");
                        CharacterControlMotionCurve positionZ = CreateCurve(source.LocalPositionZ, $"{sourceIdentity}/PositionZ");
                        CharacterControlMotionCurve forwardDistance = CreateCurve(source.ForwardDistance, $"{sourceIdentity}/ForwardDistance");
                        CharacterControlMotionCurve yaw = CreateCurve(source.LocalYaw, $"{sourceIdentity}/Yaw");
                        CharacterControlMotionEvaluationMode evaluationMode = source.EvaluationMode switch
                        {
                            RootMotionCurveEvaluationMode.FullLocalDelta => CharacterControlMotionEvaluationMode.FullLocalDelta,
                            RootMotionCurveEvaluationMode.ForwardDistanceYaw => CharacterControlMotionEvaluationMode.ForwardDistanceYaw,
                            _ => throw new InvalidOperationException($"MotionCurveClip '{sourceIdentity}' source has an unsupported evaluation mode.")
                        };
                        var mapping = new CharacterControlMotionTimeMapping(
                            clip.StartTime,
                            clip.CurveEndTime,
                            clip.SourceStartTime,
                            clip.SourceEndTime);
                        StableHash sourceRevision = CharacterControlMotionBinding.ComputeSourceRevision(
                            source.Duration,
                            source.SampleRate,
                            evaluationMode,
                            positionX,
                            positionY,
                            positionZ,
                            forwardDistance,
                            yaw);
                        bindings.Add(new CharacterControlMotionBinding(
                            sourceIdentity,
                            clip.CurveId,
                            sourceRevision,
                            evaluationMode,
                            mapping,
                            positionX,
                            positionY,
                            positionZ,
                            forwardDistance,
                            yaw));
                    }
                }
            }
            var catalog = new CharacterControlMotionBindingCatalog(bindings);
            catalog.RequireContract(controlModule);
            return catalog;
        }

        static CharacterControlMotionCurve CreateCurve(AnimationCurve curve, string identity)
        {
            if (curve == null)
                throw new InvalidOperationException($"Control Motion source '{identity}' has no curve.");
            Keyframe[] keys = curve.keys;
            var result = new CharacterControlMotionCurveKey[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                Keyframe key = keys[i];
                result[i] = new CharacterControlMotionCurveKey(
                    key.time,
                    key.value,
                    key.inTangent,
                    key.outTangent,
                    key.inWeight,
                    key.outWeight,
                    (int)key.weightedMode);
            }
            return new CharacterControlMotionCurve(
                (int)curve.preWrapMode,
                (int)curve.postWrapMode,
                result);
        }
    }
}
