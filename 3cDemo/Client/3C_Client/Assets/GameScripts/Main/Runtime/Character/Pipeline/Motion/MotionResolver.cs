using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Motion
{
    public sealed class MotionResolver
    {
        public MotionIntent Resolve(
            IReadOnlyList<MotionContribution> contributions,
            Quaternion actorRotation,
            float deltaTime,
            MotionResolveDebugFrame debugFrame)
        {
            MotionIntent result = default;
            result = ResolveChannel(MotionChannel.Locomotion, result, contributions, actorRotation, deltaTime, debugFrame);
            result = ResolveChannel(MotionChannel.Action, result, contributions, actorRotation, deltaTime, debugFrame);
            result = ResolveChannel(MotionChannel.GameplayResult, result, contributions, actorRotation, deltaTime, debugFrame);
            debugFrame?.SetRawIntent(result);
            return result;
        }

        MotionIntent ResolveChannel(
            MotionChannel channel,
            MotionIntent accumulated,
            IReadOnlyList<MotionContribution> contributions,
            Quaternion actorRotation,
            float deltaTime,
            MotionResolveDebugFrame debugFrame)
        {
            if (contributions == null || contributions.Count == 0)
                return accumulated;

            Vector3 additiveDisplacement = Vector3.zero;
            float additiveYaw = 0f;
            Vector3 weightedDisplacement = Vector3.zero;
            float weightedYaw = 0f;
            float totalWeight = 0f;
            MotionContribution overrideWinner = default;
            Vector3 overrideDisplacement = Vector3.zero;
            float overrideYaw = 0f;
            bool hasAdditive = false;
            bool hasWeighted = false;
            bool hasOverride = false;

            for (int i = 0; i < contributions.Count; i++)
            {
                MotionContribution contribution = contributions[i];
                if (contribution.Channel != channel || !contribution.CanResolve)
                    continue;

                Vector3 resolvedDisplacement = ResolveDisplacement(contribution, actorRotation);
                float resolvedYaw = ResolveYaw(contribution, actorRotation, resolvedDisplacement);
                debugFrame?.AddContribution(contribution, resolvedDisplacement, resolvedYaw);

                switch (contribution.BlendMode)
                {
                    case MotionBlendMode.Additive:
                        additiveDisplacement += resolvedDisplacement * contribution.Weight;
                        additiveYaw += resolvedYaw * contribution.Weight;
                        hasAdditive = true;
                        break;
                    case MotionBlendMode.WeightedBlend:
                        weightedDisplacement += resolvedDisplacement * contribution.Weight;
                        weightedYaw += resolvedYaw * contribution.Weight;
                        totalWeight += contribution.Weight;
                        hasWeighted = true;
                        break;
                    case MotionBlendMode.Override:
                        if (!hasOverride || contribution.Priority > overrideWinner.Priority)
                        {
                            overrideWinner = contribution;
                            overrideDisplacement = resolvedDisplacement * contribution.Weight;
                            overrideYaw = resolvedYaw * contribution.Weight;
                            hasOverride = true;
                        }
                        break;
                }
            }

            if (!hasAdditive && !hasWeighted && !hasOverride)
                return accumulated;

            Vector3 channelDisplacement = additiveDisplacement;
            float channelYaw = additiveYaw;
            MotionContribution winner = default;
            bool consumedLower = false;

            if (hasOverride)
            {
                channelDisplacement += overrideDisplacement;
                channelYaw += overrideYaw;
                winner = overrideWinner;
                consumedLower = overrideWinner.ConsumeLowerChannels;
            }
            else if (hasWeighted && totalWeight > 0f)
            {
                channelDisplacement += weightedDisplacement / totalWeight;
                channelYaw += weightedYaw / totalWeight;
            }

            MotionIntent channelIntent = BuildIntent(channelDisplacement, channelYaw, deltaTime);
            debugFrame?.AddChannelResult(channel, channelIntent, consumedLower, winner);

            if (consumedLower)
                return channelIntent;

            return Combine(accumulated, channelIntent, deltaTime);
        }

        static Vector3 ResolveDisplacement(MotionContribution contribution, Quaternion actorRotation)
        {
            return contribution.Space == MotionContributionSpace.Local
                ? actorRotation * contribution.Displacement
                : contribution.Displacement;
        }

        static float ResolveYaw(MotionContribution contribution, Quaternion actorRotation, Vector3 resolvedDisplacement)
        {
            float yaw = contribution.YawDegrees;
            if (!contribution.FaceMovementDirection || contribution.MaxYawDegrees <= 0f)
                return yaw;

            Vector3 currentForward = actorRotation * Vector3.forward;
            currentForward.y = 0f;
            Vector3 targetForward = resolvedDisplacement;
            targetForward.y = 0f;
            if (currentForward.sqrMagnitude <= 0.000001f || targetForward.sqrMagnitude <= 0.000001f)
                return yaw;

            float desiredYaw = Vector3.SignedAngle(currentForward.normalized, targetForward.normalized, Vector3.up);
            return yaw + Mathf.Clamp(desiredYaw, -contribution.MaxYawDegrees, contribution.MaxYawDegrees);
        }

        static MotionIntent Combine(MotionIntent left, MotionIntent right, float deltaTime)
        {
            if (!left.HasMotion)
                return right;
            if (!right.HasMotion)
                return left;

            return BuildIntent(left.Displacement + right.Displacement, left.YawDegrees + right.YawDegrees, deltaTime);
        }

        static MotionIntent BuildIntent(Vector3 displacement, float yaw, float deltaTime)
        {
            if (displacement.sqrMagnitude <= 0.0000001f && Mathf.Abs(yaw) <= 0.0001f)
                return default;
            Vector3 velocity = deltaTime > 0f ? displacement / deltaTime : Vector3.zero;
            return new MotionIntent(displacement, velocity, yaw);
        }
    }
}
