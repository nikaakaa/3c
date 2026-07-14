using ThirdPersonCharacter.Pipeline.Motion;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline
{
    [DisallowMultipleComponent]
    public sealed class UnityTransformLogicPoseAdapter : MonoBehaviour, ICharacterLogicPosePort
    {
        [SerializeField] Transform m_LogicRoot;

        public string ImplementationId => "Unity.Transform";

        public bool TryReadState(out CharacterLogicBodyState state, out string error)
        {
            state = default;
            if (!TryValidate(out error))
                return false;

            state = new CharacterLogicBodyState(
                m_LogicRoot.position.ToLogicPose(m_LogicRoot.rotation),
                Vector3.zero.ToMotionVector(),
                false);
            error = string.Empty;
            return true;
        }

        public bool TryApplyPose(
            CharacterLogicPose pose,
            out CharacterLogicBodyState state,
            out string error)
        {
            state = default;
            if (!TryValidate(out error))
                return false;
            if (!pose.IsValid)
            {
                error = "Requested logic pose is invalid.";
                return false;
            }

            m_LogicRoot.SetPositionAndRotation(
                pose.Position.ToUnityVector(),
                pose.Rotation.ToUnityRotation());
            return TryReadState(out state, out error);
        }

        bool TryValidate(out string error)
        {
            if (!m_LogicRoot)
            {
                error = "Unity Transform logic pose adapter requires an explicit logic root.";
                return false;
            }
            if (!m_LogicRoot.gameObject.activeInHierarchy)
            {
                error = "Unity Transform logic pose adapter requires an active logic root.";
                return false;
            }

            error = string.Empty;
            return true;
        }
    }
}
