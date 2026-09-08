#if UNITY_EDITOR
using ParadoxNotion.Design;
using ThirdPersonCharacter.Pipeline.Motion;
using ThirdPersonCharacter.Pipeline.Motion.RootMotion;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Control.Authoring
{
    [Name("移动输入运动"), Category("BTSMTL/技能流程")]
    public sealed class BtsmtlSkillLocomotionFlowNode : BtsmtlSkillFlowNode, ILocomotionInputMotionAuthoring
    {
        [SerializeField] float m_MoveSpeed = 4f;
        [SerializeField] LocomotionInputMotionDisplacementMode m_DisplacementMode;
        [SerializeField] RootMotionCurveAsset m_ActionMotionCurve;
        [SerializeField] float m_TurnSpeedDegrees = 720f;
        [SerializeField] bool m_CameraRelative = true;
        [SerializeField] LocomotionInputMotionExecutionMode m_ExecutionMode;
        [SerializeField] float m_DurationSeconds;

        public override string CapabilityId => "locomotion-input-motion";
        public float MoveSpeed => m_MoveSpeed;
        public LocomotionInputMotionDisplacementMode DisplacementMode => m_DisplacementMode;
        public RootMotionCurveAsset ActionMotionCurve => m_ActionMotionCurve;
        public float TurnSpeedDegrees => m_TurnSpeedDegrees;
        public bool CameraRelative => m_CameraRelative;
        public LocomotionInputMotionExecutionMode ExecutionMode => m_ExecutionMode;
        public float DurationSeconds => m_DurationSeconds;

        public void Configure(float moveSpeed, LocomotionInputMotionDisplacementMode displacementMode,
            RootMotionCurveAsset actionMotionCurve, float turnSpeedDegrees, bool cameraRelative,
            LocomotionInputMotionExecutionMode executionMode, float durationSeconds)
        {
            m_MoveSpeed = moveSpeed;
            m_DisplacementMode = displacementMode;
            m_ActionMotionCurve = actionMotionCurve;
            m_TurnSpeedDegrees = turnSpeedDegrees;
            m_CameraRelative = cameraRelative;
            m_ExecutionMode = executionMode;
            m_DurationSeconds = durationSeconds;
        }

        protected override void RegisterPorts()
        {
            AddFlowInput("执行", RejectAuthoringExecution, "Input");
            AddValueInput<Vector2>("移动输入", "m_MoveInput");
        }
    }
}
#endif
