using System;
using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal static class CharacterSimulationCameraNodeEmitterRegistration
    {
        public static void Register(CharacterSimulationNodeEmitterRegistry registry)
        {
            registry.Register(CharacterSimulationNodeEmitterRegistry.Camera<RequestCameraStateNode>(RequestCameraState));
            registry.Register(CharacterSimulationNodeEmitterRegistry.Camera<EmitCameraCueNode>(EmitCameraCue));
            registry.Register(CharacterSimulationNodeEmitterRegistry.Camera<SetCameraResponseNode>(SetCameraResponse));
            registry.Register(CharacterSimulationNodeEmitterRegistry.Camera<SetCameraTargetNode>(SetCameraTarget));
            registry.Register(new CameraBasisCharacterSimulationNodeEmitter());
        }

        static CharacterSimulationNodeEmission RequestCameraState(RequestCameraStateNode node)
        {
            RequireDefined(node.Mode, nameof(node.Mode));
            RequireIdentity(node.SequenceId, nameof(node.SequenceId));
            RequireDefined(node.InterruptPolicy, nameof(node.InterruptPolicy));
            RequireUnit(node.Weight, nameof(node.Weight));
            RequireNonNegative(node.BlendInSeconds, nameof(node.BlendInSeconds));
            RequireNonNegative(node.BlendOutSeconds, nameof(node.BlendOutSeconds));
            RequireOptionalIdentity(node.TargetKey, nameof(node.TargetKey));
            return new CharacterSimulationNodeEmission(
                SimulationOperationCode.CameraStateRequest,
                integer0: CameraProgramOperationSchema.PayloadVersion,
                integer1: (int)node.Mode,
                flags: (uint)node.InterruptPolicy,
                constants: CharacterSimulationNodeEmitterRegistry.Fields(
                    ("Priority", node.Priority),
                    ("Weight", node.Weight),
                    ("SequenceId", node.SequenceId),
                    ("BlendInSeconds", node.BlendInSeconds),
                    ("BlendOutSeconds", node.BlendOutSeconds),
                    ("TargetKey", node.TargetKey),
                    ("ActionContext", CharacterSimulationNodeEmitterContext.AssetIdentity(node.ActionContext))));
        }

        static CharacterSimulationNodeEmission EmitCameraCue(EmitCameraCueNode node)
        {
            RequireIdentity(node.CueId, nameof(node.CueId));
            RequireDefined(node.CueKind, nameof(node.CueKind));
            RequireIdentity(node.CueType, nameof(node.CueType));
            RequireOptionalIdentity(node.ResourceId, nameof(node.ResourceId));
            RequireNonNegative(node.Intensity, nameof(node.Intensity));
            RequireNonNegative(node.DurationSeconds, nameof(node.DurationSeconds));
            return new CharacterSimulationNodeEmission(
                SimulationOperationCode.CameraCue,
                integer0: CameraProgramOperationSchema.PayloadVersion,
                integer1: (int)node.CueKind,
                constants: CharacterSimulationNodeEmitterRegistry.Fields(
                    ("CueId", node.CueId),
                    ("CueType", node.CueType),
                    ("ResourceId", node.ResourceId),
                    ("Intensity", node.Intensity),
                    ("DurationSeconds", node.DurationSeconds),
                    ("Priority", node.Priority),
                    ("ActionContext", CharacterSimulationNodeEmitterContext.AssetIdentity(node.ActionContext))));
        }

        static CharacterSimulationNodeEmission SetCameraResponse(SetCameraResponseNode node)
        {
            RequireDefined(node.LookResponse, nameof(node.LookResponse));
            RequireUnit(node.ManualOrbitWeight, nameof(node.ManualOrbitWeight));
            RequireUnit(node.PitchResponseWeight, nameof(node.PitchResponseWeight));
            RequireUnit(node.YawResponseWeight, nameof(node.YawResponseWeight));
            RequireUnit(node.Weight, nameof(node.Weight));
            return new CharacterSimulationNodeEmission(
                SimulationOperationCode.CameraResponse,
                integer0: CameraProgramOperationSchema.PayloadVersion,
                integer1: (int)node.LookResponse,
                constants: CharacterSimulationNodeEmitterRegistry.Fields(
                    ("ManualOrbitWeight", node.ManualOrbitWeight),
                    ("PitchResponseWeight", node.PitchResponseWeight),
                    ("YawResponseWeight", node.YawResponseWeight),
                    ("Priority", node.Priority),
                    ("Weight", node.Weight),
                    ("ActionContext", CharacterSimulationNodeEmitterContext.AssetIdentity(node.ActionContext))));
        }

        static CharacterSimulationNodeEmission SetCameraTarget(SetCameraTargetNode node)
        {
            RequireOptionalIdentity(node.TargetKey, nameof(node.TargetKey));
            RequireOptionalIdentity(node.AnchorKey, nameof(node.AnchorKey));
            RequireOptionalIdentity(node.AimPointKey, nameof(node.AimPointKey));
            RequireOptionalIdentity(node.PreferredBoneKey, nameof(node.PreferredBoneKey));
            RequireUnit(node.Weight, nameof(node.Weight));
            int targetMask = (string.IsNullOrEmpty(node.TargetKey) ? 0 : CameraProgramOperationSchema.TargetKeyMask) |
                             (string.IsNullOrEmpty(node.AnchorKey) ? 0 : CameraProgramOperationSchema.AnchorKeyMask) |
                             (string.IsNullOrEmpty(node.AimPointKey) ? 0 : CameraProgramOperationSchema.AimPointKeyMask) |
                             (string.IsNullOrEmpty(node.PreferredBoneKey) ? 0 : CameraProgramOperationSchema.PreferredBoneKeyMask);
            if (targetMask == 0)
                throw new InvalidOperationException("SetCameraTarget requires at least one formal target identity.");
            return new CharacterSimulationNodeEmission(
                SimulationOperationCode.CameraTarget,
                integer0: CameraProgramOperationSchema.PayloadVersion,
                integer1: targetMask,
                constants: CharacterSimulationNodeEmitterRegistry.Fields(
                    ("TargetKey", node.TargetKey),
                    ("AnchorKey", node.AnchorKey),
                    ("AimPointKey", node.AimPointKey),
                    ("PreferredBoneKey", node.PreferredBoneKey),
                    ("Priority", node.Priority),
                    ("Weight", node.Weight),
                    ("ActionContext", CharacterSimulationNodeEmitterContext.AssetIdentity(node.ActionContext))));
        }

        static void RequireDefined<T>(T value, string field) where T : struct, Enum
        {
            if (!Enum.IsDefined(typeof(T), value))
                throw new InvalidOperationException($"Camera field '{field}' contains unknown enum value '{Convert.ToInt32(value)}'.");
        }

        static void RequireUnit(float value, string field)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f || value > 1f)
                throw new InvalidOperationException($"Camera field '{field}' must be finite and in [0, 1].");
        }

        static void RequireNonNegative(float value, string field)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
                throw new InvalidOperationException($"Camera field '{field}' must be finite and non-negative.");
        }

        static void RequireIdentity(string value, string field)
        {
            if (string.IsNullOrWhiteSpace(value) || !string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new InvalidOperationException($"Camera field '{field}' requires a trimmed identity.");
        }

        static void RequireOptionalIdentity(string value, string field)
        {
            if (value != null && !string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new InvalidOperationException($"Camera field '{field}' must not contain leading or trailing whitespace.");
        }
    }
}
