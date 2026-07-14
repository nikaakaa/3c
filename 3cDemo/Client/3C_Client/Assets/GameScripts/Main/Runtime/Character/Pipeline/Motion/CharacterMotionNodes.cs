using System;
using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonCamera;
using TreeDesigner;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Motion
{
    [Serializable]
    [NodeName("Locomotion Input Motion")]
    [NodePath("Base/Locomotion/Locomotion Input Motion")]
    public sealed class LocomotionInputMotionNode : ActionNode
    {
        [SerializeField, PropertyPort(PortDirection.Input, "Move Input")]
        Vector2PropertyPort m_MoveInput = new Vector2PropertyPort();

        [SerializeField, ShowInPanel("Move Speed")]
        float m_MoveSpeed = 4f;

        [SerializeField, ShowInPanel("Turn Speed Degrees")]
        float m_TurnSpeedDegrees = 720f;

        [SerializeField, ShowInPanel("Camera Relative")]
        bool m_CameraRelative = true;

        [SerializeField, ShowInPanel("Continuous")]
        bool m_Continuous;

        public override State ReturnState => m_Continuous ? State.Running : State.Success;

        protected override void OnStart()
        {
            if (!m_Continuous)
            {
                base.OnStart();
                return;
            }

            m_State = State.Running;
            OnStartCallback?.Invoke();
        }

        protected override State OnUpdate()
        {
            if (!m_Continuous)
                return base.OnUpdate();

            SubmitMotion(true);
            return State.Running;
        }

        protected override void DoAction()
        {
            SubmitMotion(true);
        }

        void SubmitMotion(bool readInput)
        {
            if (readInput)
                InputValue();

            if (!TryGetGraphContext(out CharacterGraphContext context))
                return;

            context.TryReadCameraBasisSnapshot(out CameraBasisSnapshot basis);
            Vector3 moveDirection = CharacterLocomotionDirectionResolver.Resolve(m_MoveInput.Value, m_CameraRelative, basis);
            Vector3 displacement = moveDirection * Mathf.Max(0f, m_MoveSpeed) * context.TickContext.FixedDeltaSeconds;
            float maxYawDegrees = Mathf.Max(0f, m_TurnSpeedDegrees) * context.TickContext.FixedDeltaSeconds;
            context.SubmitMotionContribution(MotionContribution.InputLocomotion(
                GUID,
                GetType().Name,
                displacement,
                maxYawDegrees,
                1f));
        }

        bool TryGetGraphContext(out CharacterGraphContext context)
        {
            context = null;
            if (Owner != null && Owner.TryGetUser(out context) && context != null)
                return true;

            Debug.LogError($"{GetType().Name}: CharacterGraphContext is missing from graph user.");
            return false;
        }
    }

    [Serializable]
    [NodeName("Character Move Facing Angle")]
    [NodePath("Base/Value/Locomotion/Move Facing Angle")]
    public sealed class CharacterMoveFacingAngleInfoNode : ValueNode
    {
        [SerializeField, PropertyPort(PortDirection.Input, "Move Input")]
        Vector2PropertyPort m_MoveInput = new Vector2PropertyPort();

        [SerializeField, PropertyPort(PortDirection.Output, "Angle"), ReadOnly]
        FloatPropertyPort m_Output = new FloatPropertyPort();

        protected override void OutputValue()
        {
            base.OutputValue();
            m_Output.Value = 0f;

            if (m_MoveInput.Value.sqrMagnitude <= 0.000001f ||
                !TryGetGraphContext(out CharacterGraphContext context) ||
                !context.TryReadActorPoseSnapshot(out CharacterActorPoseSnapshot pose))
                return;

            context.TryReadCameraBasisSnapshot(out CameraBasisSnapshot basis);
            Vector3 desiredDirection = CharacterLocomotionDirectionResolver.Resolve(m_MoveInput.Value, true, basis);
            desiredDirection.y = 0f;
            if (desiredDirection.sqrMagnitude <= 0.000001f)
                return;

            m_Output.Value = Mathf.Abs(Vector3.SignedAngle(pose.PlanarForward, desiredDirection.normalized, Vector3.up));
        }

        bool TryGetGraphContext(out CharacterGraphContext context)
        {
            context = null;
            if (Owner != null && Owner.TryGetUser(out context) && context != null)
                return true;

            Debug.LogError($"{GetType().Name}: CharacterGraphContext is missing from graph user.");
            return false;
        }
    }

    [Serializable]
    [NodeName("Submit Gameplay Result Motion")]
    [NodePath("Base/Action/Motion/Submit Gameplay Result Motion")]
    public sealed class SubmitGameplayResultMotionNode : ActionNode
    {
        [SerializeField, ShowInPanel("Source Id")]
        string m_SourceId;

        [SerializeField, ShowInPanel("Source Name")]
        string m_SourceName = "GameplayResultMotion";

        [SerializeField, PropertyPort(PortDirection.Input, "World Displacement")]
        Vector3PropertyPort m_WorldDisplacement = new Vector3PropertyPort();

        [SerializeField, PropertyPort(PortDirection.Input, "Yaw Degrees")]
        FloatPropertyPort m_YawDegrees = new FloatPropertyPort();

        [SerializeField, ShowInPanel("Priority")]
        int m_Priority = 100;

        [SerializeField, ShowInPanel("Consume Lower Channels")]
        bool m_ConsumeLowerChannels = true;

        [SerializeField, PropertyPort(PortDirection.Output, "Submitted"), ReadOnly]
        BoolPropertyPort m_Submitted = new BoolPropertyPort();

        public override State ReturnState => m_Submitted.Value ? State.Success : State.Failure;

        protected override void DoAction()
        {
            InputValue();
            m_Submitted.Value = false;

            if (!TryGetGraphContext(out CharacterGraphContext context))
                return;

            string sourceId = string.IsNullOrEmpty(m_SourceId) ? GUID : m_SourceId;
            m_Submitted.Value = context.SubmitMotionContribution(MotionContribution.GameplayResult(
                sourceId,
                string.IsNullOrEmpty(m_SourceName) ? GetType().Name : m_SourceName,
                m_WorldDisplacement.Value,
                m_YawDegrees.Value,
                1f,
                m_Priority,
                m_ConsumeLowerChannels));
        }

        bool TryGetGraphContext(out CharacterGraphContext context)
        {
            context = null;
            if (Owner != null && Owner.TryGetUser(out context) && context != null)
                return true;

            Debug.LogError($"{GetType().Name}: CharacterGraphContext is missing from graph user.");
            return false;
        }
    }
}
