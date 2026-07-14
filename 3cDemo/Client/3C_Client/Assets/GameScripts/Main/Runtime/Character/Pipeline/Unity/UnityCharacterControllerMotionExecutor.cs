using ThirdPersonCharacter.Pipeline.Motion;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline
{
    [DisallowMultipleComponent]
    public sealed class UnityCharacterControllerMotionExecutor : MonoBehaviour,
        ICharacterMotionExecutor,
        ICharacterLogicPosePort
    {
        const float PositionTolerance = 0.0001f;
        const float RotationToleranceDegrees = 0.01f;

        [SerializeField] CharacterController m_CharacterController;
        [SerializeField] Transform m_LogicRoot;

        public string ImplementationId => "Unity.CharacterController";

        public bool TryExecute(
            CharacterMotionExecutionInput input,
            out CharacterMotionExecutionResult result,
            out string error)
        {
            result = default;
            if (!TryValidate(out error))
                return false;
            if (!input.IsValid)
            {
                error = "Motion execution input is invalid.";
                return false;
            }
            if (!TryReadState(out CharacterLogicBodyState current, out error))
                return false;
            if (!Matches(current, input.CurrentState))
            {
                error = "Motion execution input does not match the current logic body state.";
                return false;
            }

            Vector3 beforePosition = m_LogicRoot.position;
            Quaternion beforeRotation = m_LogicRoot.rotation;
            CollisionFlags flags = CollisionFlags.None;
            if (input.HasMotion)
            {
                flags = m_CharacterController.Move(input.RequestedDisplacement.ToUnityVector());
                if (Mathf.Abs(input.RequestedYawDegrees) > 0.0001f)
                {
                    m_LogicRoot.rotation =
                        Quaternion.AngleAxis(input.RequestedYawDegrees, Vector3.up) * m_LogicRoot.rotation;
                }
            }

            Vector3 appliedDisplacement = m_LogicRoot.position - beforePosition;
            float appliedYaw = SignedYawDelta(beforeRotation, m_LogicRoot.rotation);
            Vector3 velocity = input.DeltaSeconds > 0f
                ? appliedDisplacement / input.DeltaSeconds
                : Vector3.zero;
            var finalState = new CharacterLogicBodyState(
                m_LogicRoot.position.ToLogicPose(m_LogicRoot.rotation),
                velocity.ToMotionVector(),
                m_CharacterController.isGrounded);
            result = new CharacterMotionExecutionResult(
                input,
                finalState,
                appliedDisplacement.ToMotionVector(),
                appliedYaw,
                Convert(flags));
            if (!result.IsValid)
            {
                result = default;
                error = "Unity CharacterController produced an invalid motion execution result.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        public bool TryReadState(out CharacterLogicBodyState state, out string error)
        {
            state = default;
            if (!TryValidate(out error))
                return false;

            state = new CharacterLogicBodyState(
                m_LogicRoot.position.ToLogicPose(m_LogicRoot.rotation),
                m_CharacterController.velocity.ToMotionVector(),
                m_CharacterController.isGrounded);
            if (!state.IsValid)
            {
                state = default;
                error = "Unity CharacterController logic body state is invalid.";
                return false;
            }

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
            if (!m_CharacterController)
            {
                error = "Unity CharacterController motion executor requires an explicit CharacterController.";
                return false;
            }
            if (!m_LogicRoot)
            {
                error = "Unity CharacterController motion executor requires an explicit logic root.";
                return false;
            }
            if (m_CharacterController.transform != m_LogicRoot)
            {
                error = "Unity CharacterController motion executor requires its CharacterController and logic root to match.";
                return false;
            }
            if (!m_CharacterController.enabled || !m_CharacterController.gameObject.activeInHierarchy)
            {
                error = "Unity CharacterController motion executor requires an active, enabled CharacterController.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        static bool Matches(CharacterLogicBodyState left, CharacterLogicBodyState right)
        {
            return Vector3.Distance(left.Position.ToUnityVector(), right.Position.ToUnityVector()) <= PositionTolerance &&
                   Quaternion.Angle(left.Rotation.ToUnityRotation(), right.Rotation.ToUnityRotation()) <= RotationToleranceDegrees;
        }

        static CharacterMotionCollisionSummary Convert(CollisionFlags flags)
        {
            CharacterMotionCollisionSummary result = CharacterMotionCollisionSummary.None;
            if ((flags & CollisionFlags.Sides) != 0)
                result |= CharacterMotionCollisionSummary.Sides;
            if ((flags & CollisionFlags.Above) != 0)
                result |= CharacterMotionCollisionSummary.Above;
            if ((flags & CollisionFlags.Below) != 0)
                result |= CharacterMotionCollisionSummary.Below;
            return result;
        }

        static float SignedYawDelta(Quaternion from, Quaternion to)
        {
            Vector3 forward = Quaternion.Inverse(from) * (to * Vector3.forward);
            forward.y = 0f;
            if (forward.sqrMagnitude <= 0.0000001f)
                return 0f;

            return Vector3.SignedAngle(Vector3.forward, forward.normalized, Vector3.up);
        }
    }
}
