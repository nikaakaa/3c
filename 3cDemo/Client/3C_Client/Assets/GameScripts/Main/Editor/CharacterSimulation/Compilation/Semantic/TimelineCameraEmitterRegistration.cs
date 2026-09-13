using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Timeline;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal static class TimelineCameraEmitterRegistration
    {
        internal static void Register(TimelineSemanticEmitterRegistry registry)
        {
            registry.RegisterTrack<CameraStateTrack>(context => context.DeclareTrackCatalog());
            registry.RegisterTrack<CameraCueTrack>(context => context.DeclareTrackCatalog());
            registry.RegisterTrack<CameraResponseTrack>(context => context.DeclareTrackCatalog());
            registry.RegisterTrack<CameraOverrideTrack>(context => context.DeclareTrackCatalog());
            registry.RegisterTrack<CameraZoomTrack>(context => context.DeclareTrackCatalog());
            registry.RegisterTrack<CameraStretchTrack>(context => context.DeclareTrackCatalog());
            registry.RegisterTrack<CameraShotTrack>(context => context.DeclareTrackCatalog());
            registry.RegisterClip<CameraStateClip>((clip, context) =>
            {
                CharacterSimulationSourceLocation source = context.ClipSource(clip);
                return DeclarePresentationClip(
                    clip,
                    context,
                    SimulationOperationCode.TimelineCameraState,
                    new[]
                    {
                        context.Builder.ConstantField(source, "Mode", clip.Mode),
                        context.Builder.ConstantField(source, "SequenceId", clip.SequenceId),
                        context.Builder.ConstantField(source, "Priority", clip.Priority),
                        context.Builder.ConstantField(source, "BlendInSeconds", clip.BlendInSeconds),
                        context.Builder.ConstantField(source, "BlendOutSeconds", clip.BlendOutSeconds),
                        context.Builder.ConstantField(source, "TargetKey", clip.TargetKey),
                        context.Builder.ConstantField(source, "InterruptPolicy", clip.InterruptPolicy),
                        context.Builder.ConstantField(source, "WeightCurve", context.BakeCurve(clip, "WeightCurve", clip.WeightCurve)),
                        context.Builder.ConstantField(source, "EaseInCurve", context.BakeCurve(clip, "EaseInCurve", clip.EaseInCurve)),
                        context.Builder.ConstantField(source, "EaseOutCurve", context.BakeCurve(clip, "EaseOutCurve", clip.EaseOutCurve))
                    });
            });
            registry.RegisterClip<CameraCueClip>((clip, context) =>
            {
                if (string.IsNullOrWhiteSpace(clip.ResourceId))
                    throw new InvalidOperationException(
                        $"Camera cue clip '{context.ClipIdentity(clip)}' has no ResourceId.");
                CharacterSimulationSourceLocation source = context.ClipSource(clip);
                return DeclarePresentationClip(
                    clip,
                    context,
                    SimulationOperationCode.TimelineCameraCue,
                    new[]
                    {
                        context.Builder.ConstantField(source, "CueId", clip.CueId),
                        context.Builder.ConstantField(source, "CueKind", clip.CueKind),
                        context.Builder.ConstantField(source, "CueType", clip.CueType),
                        context.Builder.ConstantField(source, "ResourceId", clip.ResourceId),
                        context.Builder.ConstantField(source, "Intensity", clip.Intensity),
                        context.Builder.ConstantField(source, "DurationSeconds", clip.DurationSeconds),
                        context.Builder.ConstantField(source, "Priority", clip.Priority),
                        context.Builder.ConstantField(source, "WeightCurve", context.BakeCurve(clip, "WeightCurve", clip.WeightCurve)),
                        context.Builder.ConstantField(source, "EaseInCurve", context.BakeCurve(clip, "EaseInCurve", clip.EaseInCurve)),
                        context.Builder.ConstantField(source, "EaseOutCurve", context.BakeCurve(clip, "EaseOutCurve", clip.EaseOutCurve))
                    });
            });
            registry.RegisterClip<CameraResponseClip>((clip, context) =>
            {
                CharacterSimulationSourceLocation source = context.ClipSource(clip);
                return DeclarePresentationClip(
                    clip,
                    context,
                    SimulationOperationCode.TimelineCameraResponse,
                    new[]
                    {
                        context.Builder.ConstantField(source, "LookResponse", clip.LookResponse),
                        context.Builder.ConstantField(source, "ManualOrbitWeight", clip.ManualOrbitWeight),
                        context.Builder.ConstantField(source, "PitchResponseWeight", clip.PitchResponseWeight),
                        context.Builder.ConstantField(source, "YawResponseWeight", clip.YawResponseWeight),
                        context.Builder.ConstantField(source, "Priority", clip.Priority),
                        context.Builder.ConstantField(source, "WeightCurve", context.BakeCurve(clip, "WeightCurve", clip.WeightCurve)),
                        context.Builder.ConstantField(source, "EaseInCurve", context.BakeCurve(clip, "EaseInCurve", clip.EaseInCurve)),
                        context.Builder.ConstantField(source, "EaseOutCurve", context.BakeCurve(clip, "EaseOutCurve", clip.EaseOutCurve))
                    });
            });
            registry.RegisterClip<CameraOverrideClip>((clip, context) =>
                DeclareEffectClip(
                    clip,
                    context,
                    TimelineCameraCueKind.Override,
                    clip.OverrideTrack?.TrackId,
                    clip.OverrideTrack?.Priority ?? 0));
            registry.RegisterClip<CameraZoomClip>((clip, context) =>
                DeclareEffectClip(
                    clip,
                    context,
                    TimelineCameraCueKind.FovKick,
                    clip.Zoom?.ZoomId,
                    clip.Zoom?.DataPriority ?? 0));
            registry.RegisterClip<CameraStretchClip>((clip, context) =>
                DeclareEffectClip(
                    clip,
                    context,
                    TimelineCameraCueKind.Recoil,
                    clip.Stretch?.StretchId,
                    clip.Stretch?.DataPriority ?? 0));
            registry.RegisterClip<CameraShotClip>((clip, context) =>
                DeclareEffectClip(
                    clip,
                    context,
                    TimelineCameraCueKind.Shot,
                    clip.Shot?.ShotId,
                    clip.Shot?.Priority ?? 0));
        }

        static OperationHandle DeclareEffectClip(
            CameraResourceClip clip,
            TimelineSemanticEmitterContext context,
            TimelineCameraCueKind cueKind,
            string resourceId,
            int priority)
        {
            if (string.IsNullOrWhiteSpace(resourceId))
                throw new InvalidOperationException(
                    $"Camera resource clip '{context.ClipIdentity(clip)}' has no ResourceId.");
            CharacterSimulationSourceLocation source = context.ClipSource(clip);
            return DeclarePresentationClip(
                clip,
                context,
                SimulationOperationCode.TimelineCameraCue,
                new[]
                {
                    context.Builder.ConstantField(source, "CueId", resourceId),
                    context.Builder.ConstantField(source, "CueKind", cueKind),
                    context.Builder.ConstantField(source, "CueType", "Camera"),
                    context.Builder.ConstantField(source, "ResourceId", resourceId),
                    context.Builder.ConstantField(source, "Intensity", 1f),
                    context.Builder.ConstantField(source, "DurationSeconds", 0f),
                    context.Builder.ConstantField(source, "Priority", priority),
                    context.Builder.ConstantField(source, "WeightCurve", context.BakeCurve(clip, "WeightCurve", clip.WeightCurve)),
                    context.Builder.ConstantField(source, "EaseInCurve", context.BakeCurve(clip, "EaseInCurve", clip.EaseInCurve)),
                    context.Builder.ConstantField(source, "EaseOutCurve", context.BakeCurve(clip, "EaseOutCurve", clip.EaseOutCurve))
                });
        }

        static OperationHandle DeclarePresentationClip(
            Clip clip,
            TimelineSemanticEmitterContext context,
            SimulationOperationCode code,
            IEnumerable<ProgramCatalogField> fields)
        {
            CharacterSimulationSourceLocation source = context.ClipSource(clip);
            string producer = context.ProducerIdentity(clip);
            int producerIndex = context.Builder.DeclareProducer(
                producer,
                CameraProgramOperationSchema.ChannelId,
                context.ClipIdentity(clip),
                ProgramOutputChannelKind.Presentation,
                source);
            OperationHandle operation = context.DeclareClipOperation(
                clip,
                code,
                fields.Concat(new[] { context.Builder.IdentityField("Producer", producer) }),
                producer);
            if (producerIndex >= 0)
            {
                context.Builder.DeclareReference(
                    context.ReferenceIdentity($"{context.ClipIdentity(clip)}/producer"),
                    operation,
                    ProgramReferenceKind.Producer,
                    producerIndex,
                    producer,
                    source);
            }
            return operation;
        }

    }
}
