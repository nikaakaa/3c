using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Timeline;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal static class TimelineAnimationEmitterRegistration
    {
        internal static void Register(TimelineSemanticEmitterRegistry registry)
        {
            registry.RegisterTrack<AnimationTrack>(context =>
            {
                var track = (AnimationTrack)context.Track;
                context.DeclareTrackCatalog(context.Builder.ConstantField(context.TrackSource, "AnimationChannelId", track.AnimationChannelId));
                context.Builder.DeclareProducer(
                    context.AnimationProducerIdentity,
                    track.AnimationChannelId,
                    context.TrackIdentity,
                    ProgramOutputChannelKind.Presentation,
                    context.TrackSource);
            });
            registry.RegisterClip<BTSMTL.Timeline.AnimationClip>((clip, context) =>
            {
                CharacterSimulationSourceLocation source = context.ClipSource(clip);
                string producer = context.AnimationProducerIdentity;
                int producerIndex = context.Builder.DeclareProducer(
                    producer,
                    ((AnimationTrack)context.Track).AnimationChannelId,
                    context.TrackIdentity,
                    ProgramOutputChannelKind.Presentation,
                    source);
                OperationHandle operation = context.DeclareClipOperation(
                    clip,
                    SimulationOperationCode.TimelineAnimation,
                    new[]
                    {
                        context.Builder.ConstantField(source, "Extrapolation", clip.ExtraPolationMode),
                        context.Builder.ConstantField(source, "WeightCurve", context.BakeCurve(clip, "WeightCurve", clip.WeightCurve)),
                        context.Builder.ConstantField(source, "EaseInCurve", context.BakeCurve(clip, "EaseInCurve", clip.EaseInCurve)),
                        context.Builder.ConstantField(source, "EaseOutCurve", context.BakeCurve(clip, "EaseOutCurve", clip.EaseOutCurve)),
                        context.Builder.IdentityField("Producer", producer)
                    },
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
            });
        }
    }
}
