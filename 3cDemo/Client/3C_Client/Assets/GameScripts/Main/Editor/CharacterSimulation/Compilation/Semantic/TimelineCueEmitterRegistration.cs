using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Timeline;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal static class TimelineCueEmitterRegistration
    {
        internal static void Register(TimelineSemanticEmitterRegistry registry)
        {
            registry.RegisterTrack<ActionCueTrack>(context => context.DeclareTrackCatalog());
            registry.RegisterClip<ActionCueClip>((clip, context) =>
            {
                CharacterSimulationSourceLocation source = context.ClipSource(clip);
                string producer = context.ProducerIdentity(clip);
                int producerIndex = context.Builder.DeclareProducer(
                    producer,
                    new AnimationChannelId("Cue"),
                    context.ClipIdentity(clip),
                    ProgramOutputChannelKind.Presentation,
                    source);
                OperationHandle operation = context.DeclareClipOperation(
                    clip,
                    SimulationOperationCode.TimelineCue,
                    new[]
                    {
                        context.Builder.ConstantField(source, "CueId", clip.CueId),
                        context.Builder.ConstantField(source, "CueType", clip.CueType),
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
