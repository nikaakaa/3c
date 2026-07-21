using System;
using TreeDesigner;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Motion
{
    [Serializable]
    [NodeName("Locomotion Input Motion")]
    [NodePath("Base/Locomotion/Locomotion Input Motion")]
    [NodeAuthoringCapability(NodeAuthoringCapability.CharacterExecution)]
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

        public float MoveSpeed => m_MoveSpeed;
        public float TurnSpeedDegrees => m_TurnSpeedDegrees;
        public bool CameraRelative => m_CameraRelative;
        public bool Continuous => m_Continuous;

        public override State ReturnState => m_Continuous ? State.Running : State.Success;

        protected override void OnStart()
        {
            throw new InvalidOperationException($"{GetType().Name} must execute through CharacterSimulationProgram.");
        }

        protected override State OnUpdate()
        {
            throw new InvalidOperationException($"{GetType().Name} must execute through CharacterSimulationProgram.");
        }

        protected override void DoAction()
        {
            throw new InvalidOperationException($"{GetType().Name} must execute through CharacterSimulationProgram.");
        }
    }

    [Serializable]
    [NodeName("Character Move Facing Angle")]
    [NodePath("Base/Value/Locomotion/Move Facing Angle")]
    [NodeAuthoringCapability(NodeAuthoringCapability.CharacterExecution)]
    public sealed class CharacterMoveFacingAngleInfoNode : ValueNode
    {
        [SerializeField, PropertyPort(PortDirection.Input, "Move Input")]
        Vector2PropertyPort m_MoveInput = new Vector2PropertyPort();

        [SerializeField, PropertyPort(PortDirection.Output, "Angle"), ReadOnly]
        FloatPropertyPort m_Output = new FloatPropertyPort();

        protected override void OutputValue()
        {
            throw new InvalidOperationException($"{GetType().Name} must execute through CharacterSimulationProgram.");
        }
    }

}
