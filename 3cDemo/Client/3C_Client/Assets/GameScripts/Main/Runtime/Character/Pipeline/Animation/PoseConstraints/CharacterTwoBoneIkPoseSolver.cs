using Unity.Collections;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public static class CharacterTwoBoneIkPoseSolver
    {
        public static CharacterTwoBoneIkResult Solve(
            CharacterPoseBoneCounts counts,
            NativeSlice<AnimationLocalBonePose> inputPose,
            NativeArray<int> poseParentIndices,
            CharacterTwoBoneIkDescriptor descriptor,
            NativeArray<CharacterComponentBonePose> componentScratch,
            NativeSlice<AnimationLocalBonePose> outputPose)
        {
            if (!counts.IsValid ||
                inputPose.Length != counts.PoseBoneCount ||
                poseParentIndices.Length != counts.PoseBoneCount ||
                componentScratch.Length < counts.PoseBoneCount ||
                outputPose.Length != counts.PoseBoneCount)
            {
                return CharacterTwoBoneIkResult.Fail(CharacterTwoBoneIkFailure.InvalidCounts);
            }
            if (!IsValidHierarchy(poseParentIndices))
                return CharacterTwoBoneIkResult.Fail(CharacterTwoBoneIkFailure.InvalidHierarchy);
            if (!IsValidDescriptor(descriptor, counts, poseParentIndices))
                return CharacterTwoBoneIkResult.Fail(CharacterTwoBoneIkFailure.InvalidDescriptor);
            if (HasNonFinitePose(inputPose))
                return CharacterTwoBoneIkResult.Fail(CharacterTwoBoneIkFailure.NonFiniteInput);
            if (!TryCopyAndBuildComponents(
                    inputPose,
                    poseParentIndices,
                    componentScratch,
                    outputPose))
            {
                return CharacterTwoBoneIkResult.Fail(CharacterTwoBoneIkFailure.InvalidPose);
            }

            CharacterComponentBonePose root =
                componentScratch[descriptor.RootPhysicalBoneIndex];
            CharacterComponentBonePose joint =
                componentScratch[descriptor.JointPhysicalBoneIndex];
            CharacterComponentBonePose end =
                componentScratch[descriptor.EndPhysicalBoneIndex];
            CharacterComponentBonePose effector =
                componentScratch[descriptor.EffectorPoseBoneIndex];
            CharacterComponentBonePose jointTargetReference =
                componentScratch[descriptor.JointTargetPoseBoneIndex];

            Vector3 targetPosition = CharacterPoseConstraintMath.TransformPoint(
                effector,
                descriptor.EffectorLocalPositionOffset);
            Quaternion targetRotation =
                (effector.Rotation * descriptor.EffectorLocalRotationOffset).normalized;
            Vector3 jointTargetPosition = CharacterPoseConstraintMath.TransformPoint(
                jointTargetReference,
                descriptor.JointTargetLocalOffset);
            if (!CharacterPoseConstraintMath.IsFinite(targetPosition) ||
                !CharacterPoseConstraintMath.IsFinite(targetRotation) ||
                !CharacterPoseConstraintMath.IsFinite(jointTargetPosition))
            {
                return CharacterTwoBoneIkResult.Fail(CharacterTwoBoneIkFailure.NonFiniteInput);
            }

            Vector3 currentUpper = joint.Position - root.Position;
            Vector3 currentLower = end.Position - joint.Position;
            float upperLength = currentUpper.magnitude;
            float lowerLength = currentLower.magnitude;
            if (!float.IsFinite(upperLength) ||
                !float.IsFinite(lowerLength) ||
                upperLength <= CharacterPoseConstraintMath.Epsilon ||
                lowerLength <= CharacterPoseConstraintMath.Epsilon)
            {
                return CharacterTwoBoneIkResult.Fail(CharacterTwoBoneIkFailure.ZeroLengthChain);
            }

            Vector3 targetDelta = targetPosition - root.Position;
            float targetDistance = targetDelta.magnitude;
            if (!float.IsFinite(targetDistance) ||
                targetDistance <= CharacterPoseConstraintMath.Epsilon)
            {
                return CharacterTwoBoneIkResult.Fail(CharacterTwoBoneIkFailure.DegenerateEffectorDirection);
            }

            float minimumReach = Mathf.Abs(upperLength - lowerLength);
            float maximumReach = upperLength + lowerLength;
            float solveDistance = Mathf.Clamp(targetDistance, minimumReach, maximumReach);
            CharacterTwoBoneIkReachState reachState =
                targetDistance < minimumReach
                    ? CharacterTwoBoneIkReachState.ClampedMinimum
                    : targetDistance > maximumReach
                        ? CharacterTwoBoneIkReachState.ClampedMaximum
                        : CharacterTwoBoneIkReachState.InRange;
            Vector3 targetDirection = targetDelta / targetDistance;

            if (descriptor.Weight <= 0f)
            {
                Vector3 passthroughResidual = targetPosition - end.Position;
                return CharacterTwoBoneIkResult.Success(
                    reachState,
                    passthroughResidual,
                    targetDistance,
                    solveDistance,
                    upperLength,
                    lowerLength);
            }

            Vector3 jointTargetDelta = jointTargetPosition - root.Position;
            Vector3 bendDirection = jointTargetDelta -
                                    targetDirection * Vector3.Dot(jointTargetDelta, targetDirection);
            float bendMagnitude = bendDirection.magnitude;
            if (!float.IsFinite(bendMagnitude) ||
                bendMagnitude <= CharacterPoseConstraintMath.Epsilon)
            {
                return CharacterTwoBoneIkResult.Fail(CharacterTwoBoneIkFailure.DegenerateJointTargetPlane);
            }
            bendDirection /= bendMagnitude;

            float alongTarget =
                (upperLength * upperLength +
                 solveDistance * solveDistance -
                 lowerLength * lowerLength) /
                (2f * solveDistance);
            float bendHeightSquared =
                upperLength * upperLength - alongTarget * alongTarget;
            if (!float.IsFinite(alongTarget) ||
                !float.IsFinite(bendHeightSquared) ||
                bendHeightSquared < -CharacterPoseConstraintMath.Epsilon)
            {
                return CharacterTwoBoneIkResult.Fail(CharacterTwoBoneIkFailure.NumericalFailure);
            }
            float bendHeight = Mathf.Sqrt(Mathf.Max(0f, bendHeightSquared));
            Vector3 solvedJointPosition =
                root.Position + targetDirection * alongTarget + bendDirection * bendHeight;
            Vector3 solvedEndPosition =
                root.Position + targetDirection * solveDistance;
            Vector3 solvedUpper = solvedJointPosition - root.Position;
            Vector3 solvedLower = solvedEndPosition - solvedJointPosition;
            if (!CharacterPoseConstraintMath.IsFinite(solvedJointPosition) ||
                !CharacterPoseConstraintMath.IsFinite(solvedEndPosition) ||
                solvedUpper.sqrMagnitude <= 0f ||
                solvedLower.sqrMagnitude <= 0f)
            {
                return CharacterTwoBoneIkResult.Fail(CharacterTwoBoneIkFailure.NumericalFailure);
            }

            Quaternion rootDelta =
                Quaternion.FromToRotation(currentUpper, solvedUpper);
            Quaternion solvedRootComponentRotation =
                (rootDelta * root.Rotation).normalized;
            Vector3 intermediateLower = rootDelta * currentLower;
            Quaternion intermediateJointRotation =
                (rootDelta * joint.Rotation).normalized;
            Quaternion jointDelta =
                Quaternion.FromToRotation(intermediateLower, solvedLower);
            Quaternion solvedJointComponentRotation =
                (jointDelta * intermediateJointRotation).normalized;

            int rootParentIndex = poseParentIndices[descriptor.RootPhysicalBoneIndex];
            Quaternion rootParentRotation =
                rootParentIndex >= 0
                    ? componentScratch[rootParentIndex].Rotation
                    : Quaternion.identity;
            Quaternion solvedRootLocalRotation =
                (Quaternion.Inverse(rootParentRotation) * solvedRootComponentRotation).normalized;
            Quaternion solvedJointLocalRotation =
                (Quaternion.Inverse(solvedRootComponentRotation) * solvedJointComponentRotation).normalized;
            Quaternion solvedEndComponentRotation =
                descriptor.EndRotationMode == CharacterTwoBoneIkEndRotationMode.MatchEffector
                    ? targetRotation
                    : end.Rotation;
            Quaternion solvedEndLocalRotation =
                (Quaternion.Inverse(solvedJointComponentRotation) * solvedEndComponentRotation).normalized;
            if (!IsUsableRotation(solvedRootLocalRotation) ||
                !IsUsableRotation(solvedJointLocalRotation) ||
                !IsUsableRotation(solvedEndLocalRotation))
            {
                return CharacterTwoBoneIkResult.Fail(CharacterTwoBoneIkFailure.NumericalFailure);
            }

            WriteWeightedRotation(
                descriptor.RootPhysicalBoneIndex,
                solvedRootLocalRotation,
                descriptor.Weight,
                inputPose,
                outputPose);
            WriteWeightedRotation(
                descriptor.JointPhysicalBoneIndex,
                solvedJointLocalRotation,
                descriptor.Weight,
                inputPose,
                outputPose);
            WriteWeightedRotation(
                descriptor.EndPhysicalBoneIndex,
                solvedEndLocalRotation,
                descriptor.Weight,
                inputPose,
                outputPose);

            if (!TryBuildComponents(outputPose, poseParentIndices, componentScratch))
                return CharacterTwoBoneIkResult.Fail(CharacterTwoBoneIkFailure.NumericalFailure);
            Vector3 positionResidual =
                targetPosition - componentScratch[descriptor.EndPhysicalBoneIndex].Position;
            if (!CharacterPoseConstraintMath.IsFinite(positionResidual))
                return CharacterTwoBoneIkResult.Fail(CharacterTwoBoneIkFailure.NumericalFailure);
            return CharacterTwoBoneIkResult.Success(
                reachState,
                positionResidual,
                targetDistance,
                solveDistance,
                upperLength,
                lowerLength);
        }

        static bool IsValidDescriptor(
            CharacterTwoBoneIkDescriptor descriptor,
            CharacterPoseBoneCounts counts,
            NativeArray<int> parentIndices) =>
            descriptor.IsValid &&
            descriptor.RootPhysicalBoneIndex < counts.PhysicalBoneCount &&
            descriptor.JointPhysicalBoneIndex < counts.PhysicalBoneCount &&
            descriptor.EndPhysicalBoneIndex < counts.PhysicalBoneCount &&
            descriptor.EffectorPoseBoneIndex < counts.PoseBoneCount &&
            descriptor.JointTargetPoseBoneIndex < counts.PoseBoneCount &&
            parentIndices[descriptor.JointPhysicalBoneIndex] == descriptor.RootPhysicalBoneIndex &&
            parentIndices[descriptor.EndPhysicalBoneIndex] == descriptor.JointPhysicalBoneIndex;

        static bool HasNonFinitePose(NativeSlice<AnimationLocalBonePose> pose)
        {
            for (int poseIndex = 0; poseIndex < pose.Length; poseIndex++)
            {
                AnimationLocalBonePose local = pose[poseIndex];
                if (!CharacterPoseConstraintMath.IsFinite(local.Position) ||
                    !CharacterPoseConstraintMath.IsFinite(local.Rotation) ||
                    !CharacterPoseConstraintMath.IsFinite(local.Scale))
                {
                    return true;
                }
            }
            return false;
        }

        static bool IsValidHierarchy(NativeArray<int> parentIndices)
        {
            int rootCount = 0;
            for (int poseIndex = 0; poseIndex < parentIndices.Length; poseIndex++)
            {
                int parentIndex = parentIndices[poseIndex];
                if (parentIndex < -1 || parentIndex >= poseIndex)
                    return false;
                if (parentIndex < 0)
                    rootCount++;
            }
            return rootCount == 1;
        }

        static bool TryCopyAndBuildComponents(
            NativeSlice<AnimationLocalBonePose> inputPose,
            NativeArray<int> parentIndices,
            NativeArray<CharacterComponentBonePose> componentScratch,
            NativeSlice<AnimationLocalBonePose> outputPose)
        {
            for (int poseIndex = 0; poseIndex < inputPose.Length; poseIndex++)
            {
                int parentIndex = parentIndices[poseIndex];
                AnimationLocalBonePose local = inputPose[poseIndex];
                if (!local.IsValid ||
                    !CharacterPoseConstraintMath.TryCreateComponent(
                        local,
                        parentIndex,
                        componentScratch,
                        out CharacterComponentBonePose component))
                {
                    return false;
                }
                outputPose[poseIndex] = local;
                componentScratch[poseIndex] = component;
            }
            return true;
        }

        static bool TryBuildComponents(
            NativeSlice<AnimationLocalBonePose> pose,
            NativeArray<int> parentIndices,
            NativeArray<CharacterComponentBonePose> componentScratch)
        {
            for (int poseIndex = 0; poseIndex < pose.Length; poseIndex++)
            {
                if (!CharacterPoseConstraintMath.TryCreateComponent(
                        pose[poseIndex],
                        parentIndices[poseIndex],
                        componentScratch,
                        out CharacterComponentBonePose component))
                {
                    return false;
                }
                componentScratch[poseIndex] = component;
            }
            return true;
        }

        static void WriteWeightedRotation(
            int poseIndex,
            Quaternion solvedRotation,
            float weight,
            NativeSlice<AnimationLocalBonePose> inputPose,
            NativeSlice<AnimationLocalBonePose> outputPose)
        {
            AnimationLocalBonePose input = inputPose[poseIndex];
            Quaternion rotation =
                Quaternion.Slerp(input.Rotation, solvedRotation, weight).normalized;
            outputPose[poseIndex] =
                new AnimationLocalBonePose(input.Position, rotation, input.Scale);
        }

        static bool IsUsableRotation(Quaternion value) =>
            CharacterPoseConstraintMath.IsFinite(value) &&
            Quaternion.Dot(value, value) > CharacterPoseConstraintMath.Epsilon;
    }
}
