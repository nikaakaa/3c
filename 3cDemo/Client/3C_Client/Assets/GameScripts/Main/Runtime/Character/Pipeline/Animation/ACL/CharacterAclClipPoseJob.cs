using Unity.Burst;
using Unity.Collections;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEngine;
using UnityEngine.Animations;

namespace ThirdPersonCharacter.Pipeline.Presentation.Animancer
{
    [BurstCompile]
    internal struct CharacterAclClipPoseJob : IAnimationJob
    {
        [ReadOnly]
        readonly NativeArray<TransformStreamHandle> m_Handles;
        [ReadOnly]
        readonly NativeArray<CharacterAclNativeTransformSample> m_Values;
        [ReadOnly]
        readonly NativeArray<int> m_TrackByPoseBone;

        internal CharacterAclClipPoseJob(
            NativeArray<TransformStreamHandle> handles,
            NativeArray<CharacterAclNativeTransformSample> values,
            NativeArray<int> trackByPoseBone)
        {
            if (!handles.IsCreated || handles.Length == 0 ||
                !values.IsCreated || values.Length == 0 ||
                !trackByPoseBone.IsCreated || trackByPoseBone.Length != handles.Length)
                throw new System.ArgumentException("ACL Clip Pose Job input is invalid.");
            m_Handles = handles;
            m_Values = values;
            m_TrackByPoseBone = trackByPoseBone;
        }

        public void ProcessAnimation(AnimationStream stream)
        {
            for (int boneIndex = 0; boneIndex < m_Handles.Length; boneIndex++)
            {
                TransformStreamHandle handle = m_Handles[boneIndex];
                if (!handle.IsValid(stream))
                    continue;
                int trackIndex = m_TrackByPoseBone[boneIndex];
                if (trackIndex < 0)
                    continue;
                CharacterAclNativeTransformSample value = m_Values[trackIndex];
                handle.SetLocalPosition(
                    stream,
                    new Vector3(value.PositionX, value.PositionY, value.PositionZ));
                handle.SetLocalRotation(
                    stream,
                    new Quaternion(
                        value.RotationX,
                        value.RotationY,
                        value.RotationZ,
                        value.RotationW).normalized);
                handle.SetLocalScale(
                    stream,
                    new Vector3(value.ScaleX, value.ScaleY, value.ScaleZ));
            }
        }

        public void ProcessRootMotion(AnimationStream stream)
        {
        }
    }
}
