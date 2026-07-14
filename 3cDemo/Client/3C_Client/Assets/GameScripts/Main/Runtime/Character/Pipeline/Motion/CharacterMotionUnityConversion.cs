using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Motion
{
    public static class CharacterMotionUnityConversion
    {
        public static CharacterMotionVector3 ToMotionVector(this Vector3 value)
        {
            return new CharacterMotionVector3(value.x, value.y, value.z);
        }

        public static Vector3 ToUnityVector(this CharacterMotionVector3 value)
        {
            return new Vector3(value.X, value.Y, value.Z);
        }

        public static CharacterMotionRotation ToMotionRotation(this Quaternion value)
        {
            return new CharacterMotionRotation(value.x, value.y, value.z, value.w);
        }

        public static Quaternion ToUnityRotation(this CharacterMotionRotation value)
        {
            return new Quaternion(value.X, value.Y, value.Z, value.W).normalized;
        }

        public static CharacterLogicPose ToLogicPose(this Vector3 position, Quaternion rotation)
        {
            return new CharacterLogicPose(position.ToMotionVector(), rotation.ToMotionRotation());
        }
    }
}
