using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonCharacter.Pipeline.Motion;
using ThirdPersonCharacter.Pipeline.Motion.RootMotion;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal static class CharacterSimulationMotionNodeEmitterRegistration
    {
        public static void Register(CharacterSimulationNodeEmitterRegistry registry)
        {
            registry.Register(CharacterSimulationNodeEmitterRegistry.Simple<LocomotionInputMotionNode>(Locomotion));
        }

        static LocomotionInputMotionExecutionMode RequireLocomotionExecution(LocomotionInputMotionNode node)
        {
            LocomotionInputMotionExecutionMode mode = node.ExecutionMode;
            if (!Enum.IsDefined(typeof(LocomotionInputMotionExecutionMode), mode))
                throw new InvalidOperationException($"Locomotion execution mode '{mode}' is invalid.");
            if (!float.IsFinite(node.DurationSeconds) || node.DurationSeconds < 0f)
                throw new InvalidOperationException("Locomotion duration must be a non-negative finite value.");
            if (mode == LocomotionInputMotionExecutionMode.Timed && node.DurationSeconds <= 0f)
                throw new InvalidOperationException("Timed Locomotion Input Motion requires a positive duration.");
            if (mode != LocomotionInputMotionExecutionMode.Timed && node.DurationSeconds != 0f)
                throw new InvalidOperationException("Only Timed Locomotion Input Motion may declare a duration.");
            return mode;
        }

        static CharacterSimulationNodeEmission Locomotion(LocomotionInputMotionNode node)
        {
            LocomotionInputMotionExecutionMode execution = RequireLocomotionExecution(node);
            LocomotionInputMotionDisplacementMode displacement = node.DisplacementMode;
            if (!Enum.IsDefined(typeof(LocomotionInputMotionDisplacementMode), displacement))
                throw new InvalidOperationException($"Locomotion displacement mode '{displacement}' is invalid.");
            if (!float.IsFinite(node.TurnSpeedDegrees) || node.TurnSpeedDegrees <= 0f)
                throw new InvalidOperationException("Locomotion turn speed must be a positive finite value.");

            var constants = new List<KeyValuePair<string, object>>
            {
                new KeyValuePair<string, object>("TurnSpeedDegrees", node.TurnSpeedDegrees),
                new KeyValuePair<string, object>("DurationSeconds", node.DurationSeconds)
            };
            if (displacement == LocomotionInputMotionDisplacementMode.ConstantSpeed)
            {
                if (!float.IsFinite(node.MoveSpeed) || node.MoveSpeed < 0f)
                    throw new InvalidOperationException("Constant Speed locomotion requires a non-negative finite Move Speed.");
                if (node.ActionMotionCurve)
                    throw new InvalidOperationException("Constant Speed locomotion cannot declare an Action Motion Curve.");
                constants.Add(new KeyValuePair<string, object>("MoveSpeed", node.MoveSpeed));
            }
            else
            {
                RootMotionCurveAsset curve = node.ActionMotionCurve;
                if (!curve || curve.EvaluationMode != RootMotionCurveEvaluationMode.FullLocalDelta)
                    throw new InvalidOperationException("Action Motion Curve locomotion requires a FullLocalDelta curve.");
                if (node.MoveSpeed != 0f)
                    throw new InvalidOperationException("Action Motion Curve locomotion cannot declare Move Speed.");
                if (!float.IsFinite(curve.Duration) || curve.Duration <= 0f)
                    throw new InvalidOperationException("Action Motion Curve locomotion requires a positive finite curve duration.");
                if (execution == LocomotionInputMotionExecutionMode.Timed && node.DurationSeconds > curve.Duration + 0.0001f)
                    throw new InvalidOperationException("Timed locomotion cannot exceed its Action Motion Curve duration.");
                constants.Add(new KeyValuePair<string, object>("ActionMotionPositionX", BakeCurve(curve.LocalPositionX, $"{node.GUID}/ActionMotionPositionX")));
                constants.Add(new KeyValuePair<string, object>("ActionMotionPositionZ", BakeCurve(curve.LocalPositionZ, $"{node.GUID}/ActionMotionPositionZ")));
                constants.Add(new KeyValuePair<string, object>("ActionMotionDuration", curve.Duration));
            }

            return new CharacterSimulationNodeEmission(
                SimulationOperationCode.LocomotionInputMotion,
                integer0: (int)execution,
                integer1: (int)displacement,
                flags: node.CameraRelative ? 1U : 0U,
                constants: constants);
        }

        static SemanticDataDocument BakeCurve(AnimationCurve curve, string identity)
        {
            if (curve == null || curve.length == 0)
                throw new InvalidOperationException($"Curve '{identity}' is empty.");
            var writer = new SemanticDataWriter();
            writer.WriteUInt32(0x56525543);
            writer.WriteInt32(1);
            writer.WriteInt32((int)curve.preWrapMode);
            writer.WriteInt32((int)curve.postWrapMode);
            writer.WriteInt32(curve.length);
            for (int i = 0; i < curve.length; i++)
            {
                Keyframe key = curve.keys[i];
                if (key.weightedMode != WeightedMode.None)
                    throw new InvalidOperationException($"Curve '{identity}' key #{i} uses unsupported weighted tangents.");
                writer.WriteNumber(key.time, $"{identity}[{i}].time");
                writer.WriteNumber(key.value, $"{identity}[{i}].value");
                writer.WriteNumber(key.inTangent, $"{identity}[{i}].inTangent");
                writer.WriteNumber(key.outTangent, $"{identity}[{i}].outTangent");
                writer.WriteNumber(key.inWeight, $"{identity}[{i}].inWeight");
                writer.WriteNumber(key.outWeight, $"{identity}[{i}].outWeight");
                writer.WriteInt32((int)key.weightedMode);
            }
            return writer.Build();
        }
    }
}
