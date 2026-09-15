using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonCharacter.Pipeline.Motion;
using ThirdPersonCharacter.Pipeline.Motion.RootMotion;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal static class SimulationMotionNodeEmitterRegistration
    {
        public static void Register(SimulationNodeEmitterRegistry registry)
        {
            registry.Register(SimulationNodeEmitterRegistry.Simple<LocomotionInputMotionNode>(node => Locomotion(node, node.GUID)));
        }

        static LocomotionInputMotionExecutionMode RequireLocomotionExecution(ILocomotionInputMotionAuthoring node)
        {
            LocomotionInputMotionAuthoringRules.Validate(node);
            LocomotionInputMotionExecutionMode mode = node.ExecutionMode;
            return mode;
        }

        internal static SimulationNodeEmission Locomotion(ILocomotionInputMotionAuthoring node, string identity)
        {
            LocomotionInputMotionExecutionMode execution = RequireLocomotionExecution(node);
            LocomotionInputMotionDisplacementMode displacement = node.DisplacementMode;

            var constants = new List<KeyValuePair<string, object>>
            {
                new KeyValuePair<string, object>("TurnSpeedDegrees", node.TurnSpeedDegrees),
                new KeyValuePair<string, object>("DurationSeconds", node.DurationSeconds)
            };
            if (displacement == LocomotionInputMotionDisplacementMode.ConstantSpeed)
            {
                constants.Add(new KeyValuePair<string, object>("MoveSpeed", node.MoveSpeed));
            }
            else
            {
                RootMotionCurveAsset curve = node.ActionMotionCurve;
                constants.Add(new KeyValuePair<string, object>("ActionMotionPositionX", BakeCurve(curve.LocalPositionX, $"{identity}/ActionMotionPositionX")));
                constants.Add(new KeyValuePair<string, object>("ActionMotionPositionZ", BakeCurve(curve.LocalPositionZ, $"{identity}/ActionMotionPositionZ")));
                constants.Add(new KeyValuePair<string, object>("ActionMotionDuration", curve.Duration));
            }

            return new SimulationNodeEmission(
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
