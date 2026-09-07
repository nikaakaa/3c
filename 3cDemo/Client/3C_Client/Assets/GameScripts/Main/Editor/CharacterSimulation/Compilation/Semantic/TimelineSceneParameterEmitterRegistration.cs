using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Timeline;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal static class TimelineSceneParameterEmitterRegistration
    {
        internal static void Register(TimelineSemanticEmitterRegistry registry)
        {
            registry.RegisterTrack<ScenePresentationParameterTrack>(context => context.DeclareTrackCatalog());
            registry.RegisterClip<ScenePresentationParameterCurveClip>((clip, context) =>
            {
                CharacterSimulationSourceLocation source = context.ClipSource(clip);
                context.Builder.RequireGameplayCapability("TimelineScenePresentationParameter");
                return context.DeclareClipOperation(
                    clip,
                    SimulationOperationCode.TimelineScenePresentationParameter,
                    new[]
                    {
                        context.Builder.IdentityField("TargetBinding", clip.TargetBindingId),
                        context.Builder.IdentityField("ParameterBinding", clip.ParameterBindingId),
                        context.Builder.ConstantField(source, "ValueCurve", context.BakeCurve(clip, "ValueCurve", clip.ValueCurve))
                    });
            });
        }
    }
}
