using System;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.Fixed
{
    public enum GameplayLabFootIkStairStraightPhase : byte
    {
        ToEnd = 1,
        ToStart = 2
    }

    public readonly struct GameplayLabFootIkStairStraightPlan
    {
        public GameplayLabFootIkStairStraightPlan(
            Vector3 start,
            Vector3 end,
            Vector2 forward,
            float length)
        {
            Start = start;
            End = end;
            Forward = forward;
            Length = length;
        }

        public Vector3 Start { get; }
        public Vector3 End { get; }
        public Vector2 Forward { get; }
        public float Length { get; }
    }

    public struct GameplayLabFootIkStairStraightState
    {
        public GameplayLabFootIkStairStraightPhase Phase;
        public int Lap;
    }

    public static class GameplayLabFootIkStairStraightRoute
    {
        public static GameplayLabFootIkStairStraightPlan Create(Vector3 start, Vector3 end)
        {
            Vector3 route = Vector3.ProjectOnPlane(end - start, Vector3.up);
            if (route.sqrMagnitude <= 0.000001f)
                throw new InvalidOperationException("GameplayLab Foot IK straight stair route is degenerate.");
            Vector3 forward = route.normalized;
            return new GameplayLabFootIkStairStraightPlan(
                start,
                end,
                new Vector2(forward.x, forward.z),
                route.magnitude);
        }

        public static GameplayLabFootIkStairStraightState CreateState() =>
            new GameplayLabFootIkStairStraightState
            {
                Phase = GameplayLabFootIkStairStraightPhase.ToEnd,
                Lap = 1
            };

        public static Vector2 Tick(
            ref GameplayLabFootIkStairStraightState state,
            in GameplayLabFootIkStairStraightPlan plan,
            Vector3 position)
        {
            float progress = AlongRoute(plan.Start, plan.Forward, position);
            switch (state.Phase)
            {
                case GameplayLabFootIkStairStraightPhase.ToEnd:
                    if (progress < plan.Length - GameplayLabFootIkRegressionCourse.ArrivalRadius)
                        return plan.Forward;
                    state.Phase = GameplayLabFootIkStairStraightPhase.ToStart;
                    return -plan.Forward;
                case GameplayLabFootIkStairStraightPhase.ToStart:
                    if (progress > GameplayLabFootIkRegressionCourse.ArrivalRadius)
                        return -plan.Forward;
                    state.Lap++;
                    state.Phase = GameplayLabFootIkStairStraightPhase.ToEnd;
                    return plan.Forward;
                default:
                    throw new InvalidOperationException("GameplayLab Foot IK straight stair phase is invalid.");
            }
        }

        static float AlongRoute(Vector3 start, Vector2 forward, Vector3 position)
        {
            Vector2 delta = new Vector2(position.x - start.x, position.z - start.z);
            return Vector2.Dot(delta, forward);
        }
    }
}
