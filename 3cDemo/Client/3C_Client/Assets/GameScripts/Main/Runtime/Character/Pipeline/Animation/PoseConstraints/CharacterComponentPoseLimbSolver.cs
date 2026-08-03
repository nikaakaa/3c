using Unity.Collections;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public static class CharacterComponentPoseLimbSolver
    {
        public static CharacterTwoBoneIkResult Solve(
            CharacterPoseBoneCounts counts,
            NativeSlice<AnimationLocalBonePose> inputPose,
            NativeArray<int> parentIndices,
            int rootIndex,
            int jointIndex,
            int endIndex,
            Vector3 targetPosition,
            Quaternion targetRotation,
            Vector3 bendDirection,
            float positionWeight,
            float rotationWeight,
            NativeArray<CharacterComponentBonePose> inputScratch,
            NativeSlice<AnimationLocalBonePose> outputPose)
        {
            if (!counts.IsValid || inputPose.Length != counts.PoseBoneCount ||
                outputPose.Length != counts.PoseBoneCount || parentIndices.Length != counts.PoseBoneCount ||
                inputScratch.Length < counts.PoseBoneCount || rootIndex < 0 || jointIndex < 0 || endIndex < 0 ||
                rootIndex >= counts.PhysicalBoneCount || jointIndex >= counts.PhysicalBoneCount ||
                endIndex >= counts.PhysicalBoneCount || parentIndices[jointIndex] != rootIndex ||
                parentIndices[endIndex] != jointIndex || !CharacterPoseConstraintMath.IsFinite(targetPosition) ||
                !CharacterPoseConstraintMath.IsFinite(targetRotation) || !CharacterPoseConstraintMath.IsFinite(bendDirection) ||
                !float.IsFinite(positionWeight) || !float.IsFinite(rotationWeight))
                return CharacterTwoBoneIkResult.Fail(CharacterTwoBoneIkFailure.InvalidDescriptor);

            positionWeight = Mathf.Clamp01(positionWeight);
            rotationWeight = Mathf.Clamp01(rotationWeight);
            for (int i = 0; i < inputPose.Length; i++)
            {
                AnimationLocalBonePose value = inputPose[i];
                if (!value.IsValid)
                    return CharacterTwoBoneIkResult.Fail(CharacterTwoBoneIkFailure.NonFiniteInput);
                inputScratch[i] = new CharacterComponentBonePose(value.Position, value.Rotation, value.Scale);
                outputPose[i] = value;
            }

            CharacterComponentBonePose root = inputScratch[rootIndex];
            CharacterComponentBonePose joint = inputScratch[jointIndex];
            CharacterComponentBonePose end = inputScratch[endIndex];
            Vector3 currentUpper = joint.Position - root.Position;
            Vector3 currentLower = end.Position - joint.Position;
            float upperLength = currentUpper.magnitude;
            float lowerLength = currentLower.magnitude;
            if (!float.IsFinite(upperLength) || !float.IsFinite(lowerLength) ||
                upperLength <= CharacterPoseConstraintMath.Epsilon || lowerLength <= CharacterPoseConstraintMath.Epsilon)
                return CharacterTwoBoneIkResult.Fail(CharacterTwoBoneIkFailure.ZeroLengthChain);

            Vector3 targetDelta = targetPosition - root.Position;
            float targetDistance = targetDelta.magnitude;
            if (!float.IsFinite(targetDistance) || targetDistance <= CharacterPoseConstraintMath.Epsilon)
                return CharacterTwoBoneIkResult.Fail(CharacterTwoBoneIkFailure.DegenerateEffectorDirection);
            float minimumReach = Mathf.Abs(upperLength - lowerLength) + CharacterPoseConstraintMath.Epsilon;
            float maximumReach = upperLength + lowerLength - CharacterPoseConstraintMath.Epsilon;
            float solveDistance = Mathf.Clamp(targetDistance, minimumReach, maximumReach);
            CharacterTwoBoneIkReachState reach = targetDistance < minimumReach
                ? CharacterTwoBoneIkReachState.ClampedMinimum
                : targetDistance > maximumReach
                    ? CharacterTwoBoneIkReachState.ClampedMaximum
                    : CharacterTwoBoneIkReachState.InRange;
            Vector3 targetDirection = targetDelta / targetDistance;
            Vector3 projectedBend = Vector3.ProjectOnPlane(bendDirection, targetDirection);
            if (projectedBend.sqrMagnitude <= CharacterPoseConstraintMath.Epsilon)
                projectedBend = Vector3.ProjectOnPlane(joint.Position - root.Position, targetDirection);
            if (projectedBend.sqrMagnitude <= CharacterPoseConstraintMath.Epsilon)
                return CharacterTwoBoneIkResult.Fail(CharacterTwoBoneIkFailure.DegenerateJointTargetPlane);
            projectedBend.Normalize();

            float along = (upperLength * upperLength + solveDistance * solveDistance - lowerLength * lowerLength) /
                          (2f * solveDistance);
            float heightSquared = upperLength * upperLength - along * along;
            if (!float.IsFinite(along) || !float.IsFinite(heightSquared) || heightSquared < -CharacterPoseConstraintMath.Epsilon)
                return CharacterTwoBoneIkResult.Fail(CharacterTwoBoneIkFailure.NumericalFailure);
            float height = Mathf.Sqrt(Mathf.Max(0f, heightSquared));
            Vector3 solvedJoint = root.Position + targetDirection * along + projectedBend * height;
            Vector3 solvedEnd = root.Position + targetDirection * solveDistance;
            Quaternion rootDelta = Quaternion.FromToRotation(currentUpper, solvedJoint - root.Position);
            Quaternion solvedRootRotation = (rootDelta * root.Rotation).normalized;
            Quaternion intermediateJointRotation = (rootDelta * joint.Rotation).normalized;
            Quaternion jointDelta = Quaternion.FromToRotation(rootDelta * currentLower, solvedEnd - solvedJoint);
            Quaternion solvedJointRotation = (jointDelta * intermediateJointRotation).normalized;

            CharacterComponentBonePose outputRoot = new CharacterComponentBonePose(
                root.Position,
                Quaternion.Slerp(root.Rotation, solvedRootRotation, positionWeight),
                root.Scale);
            CharacterComponentBonePose outputJoint = new CharacterComponentBonePose(
                Vector3.Lerp(joint.Position, solvedJoint, positionWeight),
                Quaternion.Slerp(joint.Rotation, solvedJointRotation, positionWeight),
                joint.Scale);
            CharacterComponentBonePose outputEnd = new CharacterComponentBonePose(
                Vector3.Lerp(end.Position, solvedEnd, positionWeight),
                Quaternion.Slerp(end.Rotation, targetRotation, rotationWeight),
                end.Scale);
            outputPose[rootIndex] = ToPose(outputRoot);
            outputPose[jointIndex] = ToPose(outputJoint);
            outputPose[endIndex] = ToPose(outputEnd);
            if (!RebuildDescendants(rootIndex, jointIndex, endIndex, parentIndices, inputScratch, outputPose))
                return CharacterTwoBoneIkResult.Fail(CharacterTwoBoneIkFailure.NumericalFailure);
            Vector3 residual = targetPosition - outputPose[endIndex].Position;
            return CharacterTwoBoneIkResult.Success(
                reach,
                residual,
                targetDistance,
                solveDistance,
                upperLength,
                lowerLength);
        }

        public static bool TranslateSubtree(
            int rootIndex,
            Vector3 offset,
            NativeArray<int> parentIndices,
            NativeSlice<AnimationLocalBonePose> pose)
        {
            if (rootIndex < 0 || rootIndex >= pose.Length || parentIndices.Length != pose.Length ||
                !CharacterPoseConstraintMath.IsFinite(offset))
                return false;
            for (int i = rootIndex; i < pose.Length; i++)
            {
                if (i != rootIndex && !IsDescendant(i, rootIndex, parentIndices))
                    continue;
                AnimationLocalBonePose current = pose[i];
                if (!current.IsValid)
                    return false;
                pose[i] = new AnimationLocalBonePose(current.Position + offset, current.Rotation, current.Scale);
            }
            return true;
        }

        static bool RebuildDescendants(
            int rootIndex,
            int jointIndex,
            int endIndex,
            NativeArray<int> parentIndices,
            NativeArray<CharacterComponentBonePose> input,
            NativeSlice<AnimationLocalBonePose> output)
        {
            for (int i = rootIndex + 1; i < output.Length; i++)
            {
                if (i == jointIndex || i == endIndex || !IsDescendant(i, rootIndex, parentIndices))
                    continue;
                int parent = parentIndices[i];
                if (parent < 0 || !CharacterPoseConstraintMath.TryCreateLocal(input[i], input[parent], out AnimationLocalBonePose local))
                    return false;
                CharacterComponentBonePose outputParent = AsComponent(output[parent]);
                if (!CharacterPoseConstraintMath.TryCreateComponent(local, outputParent, out CharacterComponentBonePose rebuilt))
                    return false;
                output[i] = ToPose(rebuilt);
            }
            return true;
        }

        static bool IsDescendant(int value, int ancestor, NativeArray<int> parents)
        {
            int cursor = value;
            while (cursor >= 0)
            {
                if (cursor == ancestor)
                    return true;
                cursor = parents[cursor];
            }
            return false;
        }

        static CharacterComponentBonePose AsComponent(AnimationLocalBonePose value) =>
            new CharacterComponentBonePose(value.Position, value.Rotation, value.Scale);

        static AnimationLocalBonePose ToPose(CharacterComponentBonePose value) =>
            new AnimationLocalBonePose(value.Position, value.Rotation, value.Scale);
    }
}
