using System;
using System.Collections.Generic;
using System.Linq;

namespace ThirdPersonCamera
{
    public static class CharacterCameraProjectionBuilder
    {
        public static CharacterCameraProjectionPayload Build(CharacterCameraProfile profile)
        {
            if (!profile)
                throw new ArgumentNullException(nameof(profile));
            profile.RequireValid();
            var context = new CameraProjectionCompilationContext(profile);
            CameraSequencePayload defaultSequence = CameraSequenceProjectionCompiler.Compile(profile.DefaultSequence);
            var sequences = new Dictionary<string, CameraSequencePayload>(StringComparer.Ordinal)
            {
                { defaultSequence.SequenceId, defaultSequence }
            };
            for (int i = 0; i < profile.Sequences.Count; i++)
            {
                CameraSequencePayload sequence = CameraSequenceProjectionCompiler.Compile(profile.Sequences[i]);
                if (!sequences.TryAdd(sequence.SequenceId, sequence))
                    throw new InvalidOperationException($"Camera Profile '{profile.name}' contains duplicated Sequence identity '{sequence.SequenceId}'.");
            }

            var targetSlots = new CameraTargetSlotPayload[profile.TargetSlots.Count];
            for (int i = 0; i < targetSlots.Length; i++)
                targetSlots[i] = new CameraTargetSlotPayload(profile.TargetSlots[i]);

            var orbits = profile.DefaultOrbitGroup
                .Select(value => new CameraOrbitPayload(value.Height, value.Radius, value.ScreenY))
                .ToArray();
            return new CharacterCameraProjectionPayload(
                profile,
                defaultSequence,
                sequences.Values.OrderBy(value => value.SequenceId, StringComparer.Ordinal).ToArray(),
                profile.OverrideTracks.Select(value => CameraOverrideProjectionCompiler.Compile(value, context)).ToArray(),
                profile.Zooms.Select(value => CameraZoomProjectionCompiler.Compile(value, context)).ToArray(),
                profile.Stretches.Select(value => CameraStretchProjectionCompiler.Compile(value, context)).ToArray(),
                profile.Shakes.Select(value => CameraShakeProjectionCompiler.Compile(value, context)).ToArray(),
                profile.Shots.Select(value => CameraShotProjectionCompiler.Compile(value, context)).ToArray(),
                context.CompileCurves(profile.Curves),
                new CameraOrbitPayload(profile.DefaultSphere.Height, profile.DefaultSphere.Radius, profile.DefaultSphere.ScreenY),
                orbits,
                targetSlots);
        }
    }
}
