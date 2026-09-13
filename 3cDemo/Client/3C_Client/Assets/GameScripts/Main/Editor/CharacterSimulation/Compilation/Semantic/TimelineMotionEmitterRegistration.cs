using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Timeline;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal static class TimelineMotionEmitterRegistration
    {
        internal static void Register(TimelineSemanticEmitterRegistry registry)
        {
            registry.RegisterTrack<MotionCurveTrack>(context => context.DeclareTrackCatalog());
            registry.RegisterTrack<MotionWarpTrack>(context =>
            {
                context.ValidateMotionWarp();
                context.DeclareTrackCatalog();
            });
            registry.RegisterClip<MotionCurveClip>((clip, context) =>
            {
                CharacterSimulationSourceLocation source = context.ClipSource(clip);
                context.Builder.RequireGameplayCapability("TimelineMotionCurve");
                context.Builder.RequireWorldRequest("CharacterBodyMotion", WorldCapability.BodyMotion | WorldCapability.Grounding | WorldCapability.Collision);
                return context.DeclareClipOperation(
                    clip,
                    SimulationOperationCode.TimelineMotionCurve,
                    new[]
                    {
                        context.Builder.ConstantField(source, "CurveId", clip.CurveId),
                        context.Builder.ConstantField(source, "CurveEndFrame", clip.CurveEndFrame),
                        context.Builder.ConstantField(source, "Space", clip.Space),
                        context.Builder.ConstantField(source, "Channel", clip.Channel),
                        context.Builder.ConstantField(source, "BlendMode", clip.BlendMode),
                        context.Builder.ConstantField(source, "Priority", clip.Priority),
                        context.Builder.ConstantField(source, "ConsumeLowerChannels", clip.ConsumeLowerChannels),
                        context.Builder.ConstantField(source, "WeightCurve", context.BakeCurve(clip, "WeightCurve", clip.WeightCurve)),
                        context.Builder.ConstantField(source, "PositionX", context.BakeCurve(clip, "PositionX", clip.ProgramPositionX)),
                        context.Builder.ConstantField(source, "PositionY", context.BakeCurve(clip, "PositionY", clip.ProgramPositionY)),
                        context.Builder.ConstantField(source, "PositionZ", context.BakeCurve(clip, "PositionZ", clip.ProgramPositionZ)),
                        context.Builder.ConstantField(source, "Yaw", context.BakeCurve(clip, "Yaw", clip.ProgramYaw)),
                        context.Builder.ConstantField(source, "EaseInCurve", context.BakeCurve(clip, "EaseInCurve", clip.EaseInCurve)),
                        context.Builder.ConstantField(source, "EaseOutCurve", context.BakeCurve(clip, "EaseOutCurve", clip.EaseOutCurve))
                    },
                    clip.CurveId);
            });
            registry.RegisterClip<MotionWarpClip>((clip, context) =>
            {
                CharacterSimulationSourceLocation source = context.ClipSource(clip);
                if (string.IsNullOrEmpty(context.ActionContextIdentity))
                    throw new InvalidOperationException($"MotionWarp '{clip.AuthoringId}' requires an explicit Timeline Action Context.");
                context.Builder.RequireGameplayCapability("TimelineMotionWarp");
                MotionWarpTargetOffsetSpace offsetSpace = clip.HasPositionWarp
                    ? clip.TargetOffsetSpace
                    : MotionWarpTargetOffsetSpace.TargetLocal;
                MotionWarpRotationMethod rotationMethod = clip.HasYawWarp
                    ? clip.RotationMethod
                    : MotionWarpRotationMethod.ProgressCurve;
                var fields = new List<ProgramCatalogField>
                {
                    context.Builder.IdentityField("SourceMotionClip", $"timeline:{context.Timeline.AuthoringId}/clip:{clip.SourceMotionClipId}"),
                    context.Builder.IdentityField("TimelineOwner", $"timeline:{context.Timeline.AuthoringId}"),
                    context.Builder.IdentityField("ActionContext", context.ActionContextIdentity),
                    context.Builder.ConstantField(
                        context.InvocationConstantSource(clip, "TimelineOwnerOperation"),
                        "TimelineOwnerOperation",
                        context.TimelineOperation.Value),
                    context.Builder.ConstantField(source, "TranslationMode", clip.TranslationMode),
                    context.Builder.ConstantField(source, "TargetOffsetSpace", offsetSpace),
                    context.Builder.ConstantField(source, "RotationMode", clip.RotationMode),
                    context.Builder.ConstantField(source, "RotationMethod", rotationMethod),
                    context.Builder.ConstantField(source, "LimitPolicy", clip.LimitPolicy)
                };
                if (clip.HasPositionWarp)
                {
                    fields.Add(context.Builder.ConstantField(source, "TargetPlanarOffset", clip.TargetPlanarOffset));
                    fields.Add(context.Builder.ConstantField(source, "MaximumPlanarCorrection", clip.MaxTotalPositionCorrection));
                }
                if (clip.UsesPositionProgress)
                    fields.Add(context.Builder.ConstantField(source, "PositionProgressCurve", context.BakeCurve(clip, "PositionProgressCurve", clip.PositionProgressCurve)));
                if (clip.HasYawWarp)
                {
                    fields.Add(context.Builder.ConstantField(source, "TargetYawOffsetDegrees", clip.TargetYawOffsetDegrees));
                    fields.Add(context.Builder.ConstantField(source, "MaximumYawCorrectionDegrees", clip.MaxTotalYawCorrectionDegrees));
                }
                if (clip.UsesYawProgress)
                    fields.Add(context.Builder.ConstantField(source, "YawProgressCurve", context.BakeCurve(clip, "YawProgressCurve", clip.YawProgressCurve)));
                if (clip.UsesMaximumYawRate)
                    fields.Add(context.Builder.ConstantField(source, "MaximumYawRateDegreesPerSecond", clip.MaximumYawRateDegreesPerSecond));
                OperationHandle operation = context.DeclareClipOperation(
                    clip,
                    SimulationOperationCode.TimelineMotionWarp,
                    fields,
                    clip.SourceMotionClipId,
                    integer0: (int)clip.TranslationMode,
                    integer1: (int)clip.RotationMode);
                context.DeferMotionSourceReference(clip, operation);
                return operation;
            });
        }
    }
}
