#if UNITY_EDITOR
using ParadoxNotion.Design;
using ThirdPersonCharacter.Pipeline.Motion;
using ThirdPersonCharacter.Pipeline.Motion.RootMotion;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Control.Authoring
{
    [Name("移动输入运动"), Category("BTSMTL/技能流程")]
    [BtsmtlSkillNodeKind("locomotion-input-motion")]
    [BtsmtlSkillAuthoringField(
        "moveSpeed",
        BTSMTL.Authoring.Graph.GraphAuthoringFieldValueKind.Float,
        HasMinimum = true,
        Minimum = 0d,
        Finite = true,
        HasDefaultValue = true,
        DefaultValue = LocomotionInputMotionAuthoringRules.DefaultMoveSpeedText)]
    [BtsmtlSkillAuthoringField(
        "displacementMode",
        typeof(LocomotionInputMotionDisplacementMode),
        HasDefaultValue = true,
        DefaultValue = LocomotionInputMotionAuthoringRules.DefaultDisplacementModeText)]
    [BtsmtlSkillAuthoringField(
        "turnSpeedDegrees",
        BTSMTL.Authoring.Graph.GraphAuthoringFieldValueKind.Float,
        HasMinimum = true,
        Minimum = 0d,
        Finite = true,
        HasDefaultValue = true,
        DefaultValue = LocomotionInputMotionAuthoringRules.DefaultTurnSpeedDegreesText)]
    [BtsmtlSkillAuthoringField(
        "cameraRelative",
        BTSMTL.Authoring.Graph.GraphAuthoringFieldValueKind.Boolean,
        HasDefaultValue = true,
        DefaultValue = LocomotionInputMotionAuthoringRules.DefaultCameraRelativeText)]
    [BtsmtlSkillAuthoringField(
        "executionMode",
        typeof(LocomotionInputMotionExecutionMode),
        HasDefaultValue = true,
        DefaultValue = LocomotionInputMotionAuthoringRules.DefaultExecutionModeText)]
    [BtsmtlSkillAuthoringField(
        "durationSeconds",
        BTSMTL.Authoring.Graph.GraphAuthoringFieldValueKind.Float,
        HasMinimum = true,
        Minimum = 0d,
        Finite = true,
        HasDefaultValue = true,
        DefaultValue = LocomotionInputMotionAuthoringRules.DefaultDurationSecondsText)]
    [BtsmtlSkillAuthoringField(
        "actionMotionCurve",
        BTSMTL.Authoring.Graph.GraphAuthoringFieldValueKind.AssetReference,
        Optional = true)]
    [BtsmtlSkillNodeAuthoringReference(
        "actionMotionCurve",
        BtsmtlSkillNodeAuthoringReferenceKind.Asset,
        "skill_root_motion_curve_unresolved",
        "Skill Locomotion节点的ActionMotionCurve引用无法解析。",
        typeof(RootMotionCurveAsset),
        Optional = true)]
    public sealed class BtsmtlSkillLocomotionFlowNode : BtsmtlSkillFlowNode, ILocomotionInputMotionAuthoring
    {
        [SerializeField] float m_MoveSpeed = LocomotionInputMotionAuthoringRules.DefaultMoveSpeed;
        [SerializeField] LocomotionInputMotionDisplacementMode m_DisplacementMode = LocomotionInputMotionAuthoringRules.DefaultDisplacementMode;
        [SerializeField] RootMotionCurveAsset m_ActionMotionCurve;
        [SerializeField] float m_TurnSpeedDegrees = LocomotionInputMotionAuthoringRules.DefaultTurnSpeedDegrees;
        [SerializeField] bool m_CameraRelative = LocomotionInputMotionAuthoringRules.DefaultCameraRelative;
        [SerializeField] LocomotionInputMotionExecutionMode m_ExecutionMode = LocomotionInputMotionAuthoringRules.DefaultExecutionMode;
        [SerializeField] float m_DurationSeconds = LocomotionInputMotionAuthoringRules.DefaultDurationSeconds;

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
            LocomotionInputMotionAuthoringRules.Validate(
                moveSpeed,
                displacementMode,
                actionMotionCurve,
                turnSpeedDegrees,
                executionMode,
                durationSeconds);
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
