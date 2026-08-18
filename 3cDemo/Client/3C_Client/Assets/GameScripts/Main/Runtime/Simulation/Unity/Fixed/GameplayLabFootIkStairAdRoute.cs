using System;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.Fixed
{
    public enum GameplayLabFootIkStairAdPhase : byte
    {
        Approach = 1,
        StrafeLeft = 2,
        StrafeRight = 3
    }

    public readonly struct GameplayLabFootIkStairAdPlan
    {
        public GameplayLabFootIkStairAdPlan(Vector3 center, Vector3 left, Vector3 right)
        {
            Center = center;
            Left = left;
            Right = right;
        }

        public Vector3 Center { get; }
        public Vector3 Left { get; }
        public Vector3 Right { get; }
    }

    public struct GameplayLabFootIkStairAdState
    {
        public GameplayLabFootIkStairAdPhase Phase;
        public int Lap;
    }

    public static class GameplayLabFootIkStairAdRoute
    {
        public static GameplayLabFootIkStairAdPlan Create(Vector3 start, Vector3 end)
        {
            Vector3 route = Vector3.ProjectOnPlane(end - start, Vector3.up);
            if (route.sqrMagnitude <= 0.000001f)
                throw new InvalidOperationException("GameplayLab Foot IK stair AD route is degenerate.");
            Vector3 forward = route.normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            Vector3 center = start + forward * (route.magnitude * GameplayLabFootIkRegressionCourse.TurnStressFirstFraction);
            float halfWidth = GameplayLabFootIkRegressionCourse.TurnStressHalfWidth;
            return new GameplayLabFootIkStairAdPlan(
                center,
                center - right * halfWidth,
                center + right * halfWidth);
        }

        public static GameplayLabFootIkStairAdState CreateState() =>
            new GameplayLabFootIkStairAdState
            {
                Phase = GameplayLabFootIkStairAdPhase.Approach,
                Lap = 1
            };

        public static Vector3 Tick(
            ref GameplayLabFootIkStairAdState state,
            in GameplayLabFootIkStairAdPlan plan,
            Vector3 position)
        {
            while (true)
            {
                switch (state.Phase)
                {
                    case GameplayLabFootIkStairAdPhase.Approach:
                        if (!Reached(position, plan.Center))
                            return plan.Center;
                        state.Phase = GameplayLabFootIkStairAdPhase.StrafeLeft;
                        break;
                    case GameplayLabFootIkStairAdPhase.StrafeLeft:
                        if (!Reached(position, plan.Left))
                            return plan.Left;
                        state.Phase = GameplayLabFootIkStairAdPhase.StrafeRight;
                        break;
                    case GameplayLabFootIkStairAdPhase.StrafeRight:
                        if (!Reached(position, plan.Right))
                            return plan.Right;
                        state.Lap++;
                        state.Phase = GameplayLabFootIkStairAdPhase.StrafeLeft;
                        break;
                    default:
                        throw new InvalidOperationException("GameplayLab Foot IK stair AD phase is invalid.");
                }
            }
        }

        public static Vector2 WorldDirection(Vector3 position, Vector3 target)
        {
            Vector2 direction = new Vector2(target.x - position.x, target.z - position.z);
            return direction.sqrMagnitude <= 0.000001f ? Vector2.zero : direction.normalized;
        }

        static bool Reached(Vector3 position, Vector3 target)
        {
            Vector2 delta = new Vector2(target.x - position.x, target.z - position.z);
            return delta.sqrMagnitude <=
                   GameplayLabFootIkRegressionCourse.ArrivalRadius *
                   GameplayLabFootIkRegressionCourse.ArrivalRadius;
        }
    }
}
