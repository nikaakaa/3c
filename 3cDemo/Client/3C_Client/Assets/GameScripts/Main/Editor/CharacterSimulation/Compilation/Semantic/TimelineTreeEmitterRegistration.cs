using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Timeline;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal static class TimelineTreeEmitterRegistration
    {
        internal static void Register(TimelineSemanticEmitterRegistry registry)
        {
            registry.RegisterTrack<TreeTrack>(context => context.DeclareTrackCatalog());
            registry.RegisterClip<TreeClip>((clip, context) =>
            {
                CharacterSimulationSourceLocation source = context.ClipSource(clip);
                return context.DeclareClipOperation(
                    clip,
                    SimulationOperationCode.TimelineTreeClip,
                    new[]
                    {
                        context.Builder.ConstantField(source, "ExecutionPhase", clip.ExecutionPhase),
                        context.Builder.ConstantField(source, "Ownership", clip.Ownership),
                        context.Builder.IdentityField("Graph", clip.ResolvedTree?.GraphAuthoringId)
                    },
                    clip.ResolvedTree?.GraphAuthoringId,
                    integer0: (int)clip.ExecutionPhase,
                    integer1: (int)clip.Ownership);
            });
        }
    }
}
