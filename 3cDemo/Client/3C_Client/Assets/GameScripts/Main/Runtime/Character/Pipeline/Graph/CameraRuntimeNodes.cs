#if UNITY_EDITOR
using System;
using BTSMTL.Timeline;
using ParadoxNotion.Design;
using ThirdPersonCamera;
using ThirdPersonSimulation;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Pipeline.Motion;
using UnityEngine;

namespace ThirdPersonCharacter.Control.Authoring
{
    [Name("请求相机状态"), Category("BTSMTL/相机")]
    [BtsmtlSkillNodeKind("camera-state-request")]
    [BtsmtlSkillNodeAuthoringReference(
        "actionContext",
        BtsmtlSkillNodeAuthoringReferenceKind.Asset,
        "skill_action_context_unresolved",
        "相机状态请求的Action Context引用无法解析。",
        typeof(ActionContextSlot),
        Optional = true)]
    [BtsmtlSkillAuthoringField(
        "mode",
        typeof(CameraMode),
        HasDefaultValue = true,
        DefaultValue = "FreeLook")]
    [BtsmtlSkillAuthoringField(
        "sequenceId",
        TreeDesigner.Authoring.GraphAuthoringFieldValueKind.IdentityReference,
        NonEmpty = true)]
    [BtsmtlSkillAuthoringField("priority", TreeDesigner.Authoring.GraphAuthoringFieldValueKind.Integer)]
    [BtsmtlSkillAuthoringField(
        "weight",
        TreeDesigner.Authoring.GraphAuthoringFieldValueKind.Float,
        HasMinimum = true,
        Minimum = 0d,
        HasMaximum = true,
        Maximum = 1d,
        Finite = true,
        HasDefaultValue = true,
        DefaultValue = "1")]
    [BtsmtlSkillAuthoringField(
        "blendInSeconds",
        TreeDesigner.Authoring.GraphAuthoringFieldValueKind.Float,
        HasMinimum = true,
        Minimum = 0d,
        Finite = true,
        HasDefaultValue = true,
        DefaultValue = "0.15")]
    [BtsmtlSkillAuthoringField(
        "blendOutSeconds",
        TreeDesigner.Authoring.GraphAuthoringFieldValueKind.Float,
        HasMinimum = true,
        Minimum = 0d,
        Finite = true,
        HasDefaultValue = true,
        DefaultValue = "0.2")]
    [BtsmtlSkillAuthoringField(
        "targetKey",
        TreeDesigner.Authoring.GraphAuthoringFieldValueKind.IdentityReference,
        Optional = true)]
    [BtsmtlSkillAuthoringField(
        "actionContext",
        TreeDesigner.Authoring.GraphAuthoringFieldValueKind.IdentityReference,
        Optional = true)]
    [BtsmtlSkillAuthoringField(
        "interruptPolicy",
        typeof(CameraInterruptPolicy),
        HasDefaultValue = true,
        DefaultValue = "BlendOut")]
    public sealed class RequestCameraStateNode : BtsmtlSkillFlowNode, IActionContextAuthoring
    {
        [SerializeField] CameraMode m_Mode = CameraMode.FreeLook;
        [SerializeField] string m_SequenceId;
        [SerializeField] int m_Priority;
        [SerializeField, Range(0f, 1f)] float m_Weight = 1f;
        [SerializeField, Min(0f)] float m_BlendInSeconds = 0.15f;
        [SerializeField, Min(0f)] float m_BlendOutSeconds = 0.2f;
        [SerializeField] string m_TargetKey;
        [SerializeField] ActionContextSlot m_ActionContext;
        [SerializeField] CameraInterruptPolicy m_InterruptPolicy = CameraInterruptPolicy.BlendOut;

        public CameraMode Mode => m_Mode;
        public string SequenceId => m_SequenceId ?? string.Empty;
        public int Priority => m_Priority;
        public float Weight => m_Weight;
        public float BlendInSeconds => m_BlendInSeconds;
        public float BlendOutSeconds => m_BlendOutSeconds;
        public string TargetKey => m_TargetKey ?? string.Empty;
        public ActionContextSlot ActionContext => m_ActionContext;
        public CameraInterruptPolicy InterruptPolicy => m_InterruptPolicy;

        public void Configure(
            CameraMode mode,
            string sequenceId,
            int priority,
            float weight,
            float blendInSeconds,
            float blendOutSeconds,
            string targetKey,
            ActionContextSlot actionContext,
            CameraInterruptPolicy interruptPolicy)
        {
            m_Mode = mode;
            m_SequenceId = sequenceId;
            m_Priority = priority;
            m_Weight = weight;
            m_BlendInSeconds = blendInSeconds;
            m_BlendOutSeconds = blendOutSeconds;
            m_TargetKey = targetKey;
            m_ActionContext = actionContext;
            m_InterruptPolicy = interruptPolicy;
        }

        protected override void RegisterPorts() =>
            AddFlowInput("执行", RejectAuthoringExecution, "Input");
    }

    [Name("请求相机效果"), Category("BTSMTL/相机")]
    [BtsmtlSkillNodeKind("camera-effect-request")]
    [BtsmtlSkillNodeAuthoringReference(
        "actionContext",
        BtsmtlSkillNodeAuthoringReferenceKind.Asset,
        "skill_action_context_unresolved",
        "相机效果请求的Action Context引用无法解析。",
        typeof(ActionContextSlot),
        Optional = true)]
    [BtsmtlSkillAuthoringField(
        "requestId",
        TreeDesigner.Authoring.GraphAuthoringFieldValueKind.IdentityReference,
        NonEmpty = true,
        HasDefaultValue = true,
        DefaultValue = "CameraEffect")]
    [BtsmtlSkillAuthoringField(
        "effectKind",
        typeof(CameraEffectKind),
        HasDefaultValue = true,
        DefaultValue = "Shake")]
    [BtsmtlSkillAuthoringField(
        "resourceId",
        TreeDesigner.Authoring.GraphAuthoringFieldValueKind.IdentityReference,
        NonEmpty = true)]
    [BtsmtlSkillAuthoringField(
        "weight",
        TreeDesigner.Authoring.GraphAuthoringFieldValueKind.Float,
        HasMinimum = true,
        Minimum = 0d,
        HasMaximum = true,
        Maximum = 1d,
        Finite = true,
        HasDefaultValue = true,
        DefaultValue = "1")]
    [BtsmtlSkillAuthoringField("priority", TreeDesigner.Authoring.GraphAuthoringFieldValueKind.Integer)]
    [BtsmtlSkillAuthoringField(
        "actionContext",
        TreeDesigner.Authoring.GraphAuthoringFieldValueKind.IdentityReference,
        Optional = true)]
    public sealed class RequestCameraEffectNode : BtsmtlSkillFlowNode, IActionContextAuthoring
    {
        [SerializeField] string m_RequestId = "CameraEffect";
        [SerializeField] CameraEffectKind m_EffectKind = CameraEffectKind.Shake;
        [SerializeField] string m_ResourceId;
        [SerializeField, Range(0f, 1f)] float m_Weight = 1f;
        [SerializeField] int m_Priority;
        [SerializeField] ActionContextSlot m_ActionContext;

        public string RequestId => m_RequestId ?? string.Empty;
        public CameraEffectKind EffectKind => m_EffectKind;
        public string ResourceId => m_ResourceId ?? string.Empty;
        public float Weight => m_Weight;
        public int Priority => m_Priority;
        public ActionContextSlot ActionContext => m_ActionContext;

        public void Configure(
            string requestId,
            CameraEffectKind effectKind,
            string resourceId,
            float weight,
            int priority,
            ActionContextSlot actionContext)
        {
            m_RequestId = requestId;
            m_EffectKind = effectKind;
            m_ResourceId = resourceId;
            m_Weight = weight;
            m_Priority = priority;
            m_ActionContext = actionContext;
        }

        protected override void RegisterPorts() =>
            AddFlowInput("执行", RejectAuthoringExecution, "Input");
    }

    [Name("设置相机响应"), Category("BTSMTL/相机")]
    [BtsmtlSkillNodeKind("camera-response")]
    [BtsmtlSkillNodeAuthoringReference(
        "actionContext",
        BtsmtlSkillNodeAuthoringReferenceKind.Asset,
        "skill_action_context_unresolved",
        "相机响应请求的Action Context引用无法解析。",
        typeof(ActionContextSlot),
        Optional = true)]
    [BtsmtlSkillAuthoringField("lookResponse", typeof(CameraLookResponseMode))]
    [BtsmtlSkillAuthoringField(
        "manualOrbitWeight",
        TreeDesigner.Authoring.GraphAuthoringFieldValueKind.Float,
        HasMinimum = true,
        Minimum = 0d,
        HasMaximum = true,
        Maximum = 1d,
        Finite = true,
        HasDefaultValue = true,
        DefaultValue = "1")]
    [BtsmtlSkillAuthoringField(
        "pitchResponseWeight",
        TreeDesigner.Authoring.GraphAuthoringFieldValueKind.Float,
        HasMinimum = true,
        Minimum = 0d,
        HasMaximum = true,
        Maximum = 1d,
        Finite = true,
        HasDefaultValue = true,
        DefaultValue = "1")]
    [BtsmtlSkillAuthoringField(
        "yawResponseWeight",
        TreeDesigner.Authoring.GraphAuthoringFieldValueKind.Float,
        HasMinimum = true,
        Minimum = 0d,
        HasMaximum = true,
        Maximum = 1d,
        Finite = true,
        HasDefaultValue = true,
        DefaultValue = "1")]
    [BtsmtlSkillAuthoringField("priority", TreeDesigner.Authoring.GraphAuthoringFieldValueKind.Integer)]
    [BtsmtlSkillAuthoringField(
        "weight",
        TreeDesigner.Authoring.GraphAuthoringFieldValueKind.Float,
        HasMinimum = true,
        Minimum = 0d,
        HasMaximum = true,
        Maximum = 1d,
        Finite = true,
        HasDefaultValue = true,
        DefaultValue = "1")]
    [BtsmtlSkillAuthoringField(
        "actionContext",
        TreeDesigner.Authoring.GraphAuthoringFieldValueKind.IdentityReference,
        Optional = true)]
    public sealed class SetCameraResponseNode : BtsmtlSkillFlowNode, IActionContextAuthoring
    {
        [SerializeField] CameraLookResponseMode m_LookResponse = CameraLookResponseMode.Full;
        [SerializeField, Range(0f, 1f)] float m_ManualOrbitWeight = 1f;
        [SerializeField, Range(0f, 1f)] float m_PitchResponseWeight = 1f;
        [SerializeField, Range(0f, 1f)] float m_YawResponseWeight = 1f;
        [SerializeField] int m_Priority;
        [SerializeField, Range(0f, 1f)] float m_Weight = 1f;
        [SerializeField] ActionContextSlot m_ActionContext;

        public CameraLookResponseMode LookResponse => m_LookResponse;
        public float ManualOrbitWeight => m_ManualOrbitWeight;
        public float PitchResponseWeight => m_PitchResponseWeight;
        public float YawResponseWeight => m_YawResponseWeight;
        public int Priority => m_Priority;
        public float Weight => m_Weight;
        public ActionContextSlot ActionContext => m_ActionContext;

        public void Configure(
            CameraLookResponseMode lookResponse,
            float manualOrbitWeight,
            float pitchResponseWeight,
            float yawResponseWeight,
            int priority,
            float weight,
            ActionContextSlot actionContext)
        {
            m_LookResponse = lookResponse;
            m_ManualOrbitWeight = manualOrbitWeight;
            m_PitchResponseWeight = pitchResponseWeight;
            m_YawResponseWeight = yawResponseWeight;
            m_Priority = priority;
            m_Weight = weight;
            m_ActionContext = actionContext;
        }

        protected override void RegisterPorts() =>
            AddFlowInput("执行", RejectAuthoringExecution, "Input");
    }

    [Name("设置相机目标"), Category("BTSMTL/相机")]
    [BtsmtlSkillNodeKind("camera-target")]
    [BtsmtlSkillNodeAuthoringReference(
        "actionContext",
        BtsmtlSkillNodeAuthoringReferenceKind.Asset,
        "skill_action_context_unresolved",
        "相机目标请求的Action Context引用无法解析。",
        typeof(ActionContextSlot),
        Optional = true)]
    [BtsmtlSkillAuthoringField(
        "targetKey",
        TreeDesigner.Authoring.GraphAuthoringFieldValueKind.IdentityReference,
        Optional = true)]
    [BtsmtlSkillAuthoringField(
        "anchorKey",
        TreeDesigner.Authoring.GraphAuthoringFieldValueKind.IdentityReference,
        Optional = true)]
    [BtsmtlSkillAuthoringField(
        "aimPointKey",
        TreeDesigner.Authoring.GraphAuthoringFieldValueKind.IdentityReference,
        Optional = true)]
    [BtsmtlSkillAuthoringField(
        "preferredBoneKey",
        TreeDesigner.Authoring.GraphAuthoringFieldValueKind.IdentityReference,
        Optional = true)]
    [BtsmtlSkillAuthoringField("priority", TreeDesigner.Authoring.GraphAuthoringFieldValueKind.Integer)]
    [BtsmtlSkillAuthoringField(
        "weight",
        TreeDesigner.Authoring.GraphAuthoringFieldValueKind.Float,
        HasMinimum = true,
        Minimum = 0d,
        HasMaximum = true,
        Maximum = 1d,
        Finite = true,
        HasDefaultValue = true,
        DefaultValue = "1")]
    [BtsmtlSkillAuthoringField(
        "actionContext",
        TreeDesigner.Authoring.GraphAuthoringFieldValueKind.IdentityReference,
        Optional = true)]
    public sealed class SetCameraTargetNode : BtsmtlSkillFlowNode, IActionContextAuthoring
    {
        [SerializeField] string m_TargetKey;
        [SerializeField] string m_AnchorKey;
        [SerializeField] string m_AimPointKey;
        [SerializeField] string m_PreferredBoneKey;
        [SerializeField] int m_Priority;
        [SerializeField, Range(0f, 1f)] float m_Weight = 1f;
        [SerializeField] ActionContextSlot m_ActionContext;

        public string TargetKey => m_TargetKey ?? string.Empty;
        public string AnchorKey => m_AnchorKey ?? string.Empty;
        public string AimPointKey => m_AimPointKey ?? string.Empty;
        public string PreferredBoneKey => m_PreferredBoneKey ?? string.Empty;
        public int Priority => m_Priority;
        public float Weight => m_Weight;
        public ActionContextSlot ActionContext => m_ActionContext;

        public void Configure(
            string targetKey,
            string anchorKey,
            string aimPointKey,
            string preferredBoneKey,
            int priority,
            float weight,
            ActionContextSlot actionContext)
        {
            m_TargetKey = targetKey;
            m_AnchorKey = anchorKey;
            m_AimPointKey = aimPointKey;
            m_PreferredBoneKey = preferredBoneKey;
            m_Priority = priority;
            m_Weight = weight;
            m_ActionContext = actionContext;
        }

        protected override void RegisterPorts() =>
            AddFlowInput("执行", RejectAuthoringExecution, "Input");
    }

    [Name("读取相机Basis"), Category("BTSMTL/相机")]
    [BtsmtlSkillNodeKind("camera-basis-read")]
    public sealed class ReadCameraBasisNode : BtsmtlSkillFlowNode, IBtsmtlSkillPureValueNode
    {
        protected override void RegisterPorts()
        {
            AddValueOutput<bool>("有效", RejectAuthoringValue<bool>, CameraProgramOperationSchema.BasisValidPortId);
            AddValueOutput<Vector3>("平面前方", RejectAuthoringValue<Vector3>, CameraProgramOperationSchema.BasisPlanarForwardPortId);
            AddValueOutput<Vector3>("平面右方", RejectAuthoringValue<Vector3>, CameraProgramOperationSchema.BasisPlanarRightPortId);
            AddValueOutput<Vector3>("观察方向", RejectAuthoringValue<Vector3>, CameraProgramOperationSchema.BasisLookDirectionPortId);
            AddValueOutput<Vector3>("瞄准点", RejectAuthoringValue<Vector3>, CameraProgramOperationSchema.BasisAimPointPortId);
            AddValueOutput<float>("Yaw", RejectAuthoringValue<float>, CameraProgramOperationSchema.BasisYawPortId);
            AddValueOutput<float>("Pitch", RejectAuthoringValue<float>, CameraProgramOperationSchema.BasisPitchPortId);
        }
    }
}
#endif
