using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Motion.RootMotion;
using ThirdPersonSimulation;
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

        [SerializeField, ShowInPanel("Displacement Mode")]
        LocomotionInputMotionDisplacementMode m_DisplacementMode;

        [SerializeField, ShowInPanel("Action Motion Curve")]
        RootMotionCurveAsset m_ActionMotionCurve;

        [SerializeField, ShowInPanel("Turn Speed Degrees")]
        float m_TurnSpeedDegrees = 720f;

        [SerializeField, ShowInPanel("Camera Relative")]
        bool m_CameraRelative = true;

        [SerializeField, ShowInPanel("Execution Mode")]
        LocomotionInputMotionExecutionMode m_ExecutionMode;

        [SerializeField, ShowInPanel("Duration Seconds")]
        float m_DurationSeconds;

        public float MoveSpeed => m_MoveSpeed;
        public LocomotionInputMotionDisplacementMode DisplacementMode => m_DisplacementMode;
        public RootMotionCurveAsset ActionMotionCurve => m_ActionMotionCurve;
        public float TurnSpeedDegrees => m_TurnSpeedDegrees;
        public bool CameraRelative => m_CameraRelative;
        public LocomotionInputMotionExecutionMode ExecutionMode => m_ExecutionMode;
        public float DurationSeconds => m_DurationSeconds;

#if UNITY_EDITOR
        public void ConfigureAuthoring(
            float moveSpeed,
            LocomotionInputMotionDisplacementMode displacementMode,
            RootMotionCurveAsset actionMotionCurve,
            float turnSpeedDegrees,
            bool cameraRelative,
            LocomotionInputMotionExecutionMode executionMode,
            float durationSeconds)
        {
            if (float.IsNaN(moveSpeed) || float.IsInfinity(moveSpeed) || moveSpeed < 0f)
                throw new ArgumentOutOfRangeException(nameof(moveSpeed));
            if (!Enum.IsDefined(typeof(LocomotionInputMotionDisplacementMode), displacementMode))
                throw new ArgumentOutOfRangeException(nameof(displacementMode));
            if (displacementMode == LocomotionInputMotionDisplacementMode.ConstantSpeed && actionMotionCurve)
                throw new ArgumentException("Constant Speed locomotion cannot declare an Action Motion Curve.", nameof(actionMotionCurve));
            if (displacementMode == LocomotionInputMotionDisplacementMode.ActionMotionCurve)
            {
                if (!actionMotionCurve)
                    throw new ArgumentNullException(nameof(actionMotionCurve));
                if (moveSpeed != 0f)
                    throw new ArgumentOutOfRangeException(nameof(moveSpeed), "Action Motion Curve locomotion must not declare Move Speed.");
                if (actionMotionCurve.EvaluationMode != RootMotionCurveEvaluationMode.FullLocalDelta)
                    throw new ArgumentException("Action Motion Curve locomotion requires FullLocalDelta evaluation.", nameof(actionMotionCurve));
            }
            if (float.IsNaN(turnSpeedDegrees) || float.IsInfinity(turnSpeedDegrees) || turnSpeedDegrees <= 0f)
                throw new ArgumentOutOfRangeException(nameof(turnSpeedDegrees));
            if (!Enum.IsDefined(typeof(LocomotionInputMotionExecutionMode), executionMode))
                throw new ArgumentOutOfRangeException(nameof(executionMode));
            if (!float.IsFinite(durationSeconds) || durationSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(durationSeconds));
            if (executionMode == LocomotionInputMotionExecutionMode.Timed && durationSeconds <= 0f)
                throw new ArgumentOutOfRangeException(nameof(durationSeconds));
            if (executionMode != LocomotionInputMotionExecutionMode.Timed && durationSeconds != 0f)
                throw new ArgumentOutOfRangeException(nameof(durationSeconds));
            if (displacementMode == LocomotionInputMotionDisplacementMode.ActionMotionCurve &&
                executionMode == LocomotionInputMotionExecutionMode.Timed &&
                durationSeconds > actionMotionCurve.Duration + 0.0001f)
                throw new ArgumentOutOfRangeException(nameof(durationSeconds), "Timed locomotion cannot exceed its Action Motion Curve duration.");
            m_MoveSpeed = moveSpeed;
            m_DisplacementMode = displacementMode;
            m_ActionMotionCurve = actionMotionCurve;
            m_TurnSpeedDegrees = turnSpeedDegrees;
            m_CameraRelative = cameraRelative;
            m_ExecutionMode = executionMode;
            m_DurationSeconds = durationSeconds;
            OnNodeChangedCallback();
        }

        public override IEnumerable<NodeAssetReference> GetAssetReferences()
        {
            foreach (NodeAssetReference reference in base.GetAssetReferences())
                yield return reference;
            yield return new NodeAssetReference(this, "m_ActionMotionCurve", "Action Motion Curve", m_ActionMotionCurve, m_DisplacementMode == LocomotionInputMotionDisplacementMode.ActionMotionCurve);
        }
#endif

        public override State ReturnState => m_ExecutionMode == LocomotionInputMotionExecutionMode.Once
            ? State.Success
            : State.Running;

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
