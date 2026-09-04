using System;
using BTSMTL.Timeline;
using ThirdPersonCamera;
using TreeDesigner;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Graph
{
    [Serializable]
    public abstract class CharacterSimulationOperationNode : ActionNode
    {
        protected sealed override void DoAction()
        {
        }
    }

    [Serializable]
    public abstract class CharacterSimulationValueNode : ValueNode
    {
        protected sealed override void OutputValue()
        {
        }
    }

    [Serializable]
    [NodeName("Request Camera Sequence")]
    [NodePath("Base/Action/Camera/Request Camera Sequence")]
    [NodeAuthoringCapability(NodeAuthoringCapability.CharacterExecution)]
    public sealed class RequestCameraStateNode : CharacterSimulationOperationNode
    {
        [SerializeField, ShowInPanel("Sequence")]
        CameraSequenceAsset m_Sequence;

        [SerializeField, ShowInPanel("Priority")]
        int m_Priority;

        [SerializeField, Range(0f, 1f), ShowInPanel("Weight")]
        float m_Weight = 1f;

        [SerializeField, Min(0f), ShowInPanel("Blend In Seconds")]
        float m_BlendInSeconds = 0.15f;

        [SerializeField, Min(0f), ShowInPanel("Blend Out Seconds")]
        float m_BlendOutSeconds = 0.2f;

        [SerializeField, ShowInPanel("Target Key")]
        string m_TargetKey;

        [SerializeField, ShowInPanel("Action Context")]
        ActionContextSlot m_ActionContext;

        [SerializeField, ShowInPanel("Interrupt Policy")]
        CameraSequenceInterruptPolicy m_InterruptPolicy = CameraSequenceInterruptPolicy.BlendOut;

        [SerializeField, PropertyPort(PortDirection.Output, "Submitted"), ReadOnly]
        BoolPropertyPort m_Submitted = new BoolPropertyPort();

        public CameraSequenceAsset Sequence => m_Sequence;
        public string SequenceId => m_Sequence ? m_Sequence.SequenceId : string.Empty;
        public int Priority => m_Priority;
        public float Weight => m_Weight;
        public float BlendInSeconds => m_BlendInSeconds;
        public float BlendOutSeconds => m_BlendOutSeconds;
        public string TargetKey => m_TargetKey;
        public ActionContextSlot ActionContext => m_ActionContext;
        public CameraSequenceInterruptPolicy InterruptPolicy => m_InterruptPolicy;
        public override State ReturnState => m_Submitted.Value ? State.Success : State.Failure;

    }

    [Serializable]
    [NodeName("Emit Camera Shake")]
    [NodePath("Base/Action/Camera/Emit Camera Shake")]
    [NodeAuthoringCapability(NodeAuthoringCapability.CharacterExecution)]
    public sealed class EmitCameraCueNode : CharacterSimulationOperationNode
    {
        [SerializeField, ShowInPanel("Shake")]
        CameraShakeAsset m_Shake;

        [SerializeField, Min(0f), ShowInPanel("Intensity")]
        float m_Intensity = 1f;

        [SerializeField, ShowInPanel("Priority")]
        int m_Priority;

        [SerializeField, ShowInPanel("Action Context")]
        ActionContextSlot m_ActionContext;

        [SerializeField, PropertyPort(PortDirection.Output, "Submitted"), ReadOnly]
        BoolPropertyPort m_Submitted = new BoolPropertyPort();

        public CameraShakeAsset Shake => m_Shake;
        public string ResourceId => m_Shake ? m_Shake.ShakeId : string.Empty;
        public float Intensity => m_Intensity;
        public int Priority => m_Priority;
        public ActionContextSlot ActionContext => m_ActionContext;
        public override State ReturnState => m_Submitted.Value ? State.Success : State.Failure;

    }

    [Serializable]
    [NodeName("Set Camera Response")]
    [NodePath("Base/Action/Camera/Set Camera Response")]
    [NodeAuthoringCapability(NodeAuthoringCapability.CharacterExecution)]
    public sealed class SetCameraResponseNode : CharacterSimulationOperationNode
    {
        [SerializeField, ShowInPanel("Look Response")]
        CameraResponseMode m_LookResponse = CameraResponseMode.Full;

        [SerializeField, Range(0f, 1f), ShowInPanel("Manual Orbit Weight")]
        float m_ManualOrbitWeight = 1f;

        [SerializeField, Range(0f, 1f), ShowInPanel("Pitch Response Weight")]
        float m_PitchResponseWeight = 1f;

        [SerializeField, Range(0f, 1f), ShowInPanel("Yaw Response Weight")]
        float m_YawResponseWeight = 1f;

        [SerializeField, ShowInPanel("Priority")]
        int m_Priority;

        [SerializeField, Range(0f, 1f), ShowInPanel("Weight")]
        float m_Weight = 1f;

        [SerializeField, ShowInPanel("Action Context")]
        ActionContextSlot m_ActionContext;

        [SerializeField, PropertyPort(PortDirection.Output, "Submitted"), ReadOnly]
        BoolPropertyPort m_Submitted = new BoolPropertyPort();

        public CameraResponseMode LookResponse => m_LookResponse;
        public float ManualOrbitWeight => m_ManualOrbitWeight;
        public float PitchResponseWeight => m_PitchResponseWeight;
        public float YawResponseWeight => m_YawResponseWeight;
        public int Priority => m_Priority;
        public float Weight => m_Weight;
        public ActionContextSlot ActionContext => m_ActionContext;
        public override State ReturnState => m_Submitted.Value ? State.Success : State.Failure;

    }

    [Serializable]
    [NodeName("Set Camera Target")]
    [NodePath("Base/Action/Camera/Set Camera Target")]
    [NodeAuthoringCapability(NodeAuthoringCapability.CharacterExecution)]
    public sealed class SetCameraTargetNode : CharacterSimulationOperationNode
    {
        [SerializeField, ShowInPanel("Target Key")]
        string m_TargetKey;

        [SerializeField, ShowInPanel("Anchor Key")]
        string m_AnchorKey;

        [SerializeField, ShowInPanel("Aim Point Key")]
        string m_AimPointKey;

        [SerializeField, ShowInPanel("Preferred Bone Key")]
        string m_PreferredBoneKey;

        [SerializeField, ShowInPanel("Priority")]
        int m_Priority;

        [SerializeField, Range(0f, 1f), ShowInPanel("Weight")]
        float m_Weight = 1f;

        [SerializeField, ShowInPanel("Action Context")]
        ActionContextSlot m_ActionContext;

        [SerializeField, PropertyPort(PortDirection.Output, "Submitted"), ReadOnly]
        BoolPropertyPort m_Submitted = new BoolPropertyPort();

        public string TargetKey => m_TargetKey;
        public string AnchorKey => m_AnchorKey;
        public string AimPointKey => m_AimPointKey;
        public string PreferredBoneKey => m_PreferredBoneKey;
        public int Priority => m_Priority;
        public float Weight => m_Weight;
        public ActionContextSlot ActionContext => m_ActionContext;
        public override State ReturnState => m_Submitted.Value ? State.Success : State.Failure;

    }

    [Serializable]
    [NodeName("Read Camera Basis")]
    [NodePath("Base/Value/Camera/Read Camera Basis")]
    [NodeAuthoringCapability(NodeAuthoringCapability.CharacterExecution)]
    public sealed class ReadCameraBasisNode : CharacterSimulationValueNode
    {
        [SerializeField, PropertyPort(PortDirection.Output, "Valid"), ReadOnly]
        BoolPropertyPort m_Valid = new BoolPropertyPort();

        [SerializeField, PropertyPort(PortDirection.Output, "Planar Forward"), ReadOnly]
        Vector3PropertyPort m_PlanarForward = new Vector3PropertyPort();

        [SerializeField, PropertyPort(PortDirection.Output, "Planar Right"), ReadOnly]
        Vector3PropertyPort m_PlanarRight = new Vector3PropertyPort();

        [SerializeField, PropertyPort(PortDirection.Output, "Look Direction"), ReadOnly]
        Vector3PropertyPort m_LookDirection = new Vector3PropertyPort();

        [SerializeField, PropertyPort(PortDirection.Output, "Aim Point"), ReadOnly]
        Vector3PropertyPort m_AimPoint = new Vector3PropertyPort();

        [SerializeField, PropertyPort(PortDirection.Output, "Yaw"), ReadOnly]
        FloatPropertyPort m_Yaw = new FloatPropertyPort();

        [SerializeField, PropertyPort(PortDirection.Output, "Pitch"), ReadOnly]
        FloatPropertyPort m_Pitch = new FloatPropertyPort();

    }
}
