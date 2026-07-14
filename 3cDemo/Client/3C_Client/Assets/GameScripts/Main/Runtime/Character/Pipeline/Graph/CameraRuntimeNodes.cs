using System;
using BTSMTL.Timeline;
using ThirdPersonCamera;
using ThirdPersonCharacter.ActionSystem;
using TreeDesigner;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Graph
{
    [Serializable]
    [NodeName("Request Camera State")]
    [NodePath("Base/Action/Camera/Request Camera State")]
    public sealed class RequestCameraStateNode : ActionNode
    {
        [SerializeField, ShowInPanel("Mode")]
        CameraMode m_Mode = CameraMode.FreeLook;

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
        CameraInterruptPolicy m_InterruptPolicy = CameraInterruptPolicy.BlendOut;

        [SerializeField, PropertyPort(PortDirection.Output, "Submitted"), ReadOnly]
        BoolPropertyPort m_Submitted = new BoolPropertyPort();

        public override State ReturnState => m_Submitted.Value ? State.Success : State.Failure;

        protected override void DoAction()
        {
            m_Submitted.Value = false;
            if (!TryGetGraphContext(out CharacterGraphContext context))
                return;

            ulong actionInstanceId = ResolveActionInstanceId(context);
            m_Submitted.Value = context.SubmitCameraStateRequest(new CameraStateRequest(
                m_Mode,
                m_Priority,
                m_Weight,
                m_BlendInSeconds,
                m_BlendOutSeconds,
                m_TargetKey,
                GUID,
                GetType().Name,
                actionInstanceId,
                m_InterruptPolicy));
        }

        ulong ResolveActionInstanceId(CharacterGraphContext context)
        {
            return context.TryGetActionContextHandle(m_ActionContext, out ActionInstanceHandle handle)
                ? handle.ActionInstanceId
                : 0;
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
    [NodeName("Emit Camera Cue")]
    [NodePath("Base/Action/Camera/Emit Camera Cue")]
    public sealed class EmitCameraCueNode : ActionNode
    {
        [SerializeField, ShowInPanel("Cue Id")]
        string m_CueId = "CameraCue";

        [SerializeField, ShowInPanel("Cue Kind")]
        CameraCueKind m_CueKind = CameraCueKind.Shake;

        [SerializeField, ShowInPanel("Cue Type")]
        string m_CueType = "Camera";

        [SerializeField, Min(0f), ShowInPanel("Intensity")]
        float m_Intensity = 1f;

        [SerializeField, Min(0f), ShowInPanel("Duration Seconds")]
        float m_DurationSeconds = 0.2f;

        [SerializeField, ShowInPanel("Priority")]
        int m_Priority;

        [SerializeField, ShowInPanel("Action Context")]
        ActionContextSlot m_ActionContext;

        [SerializeField, PropertyPort(PortDirection.Output, "Submitted"), ReadOnly]
        BoolPropertyPort m_Submitted = new BoolPropertyPort();

        public override State ReturnState => m_Submitted.Value ? State.Success : State.Failure;

        protected override void DoAction()
        {
            m_Submitted.Value = false;
            if (!TryGetGraphContext(out CharacterGraphContext context))
                return;

            ulong actionInstanceId = ResolveActionInstanceId(context);
            m_Submitted.Value = context.SubmitCameraCue(new CameraCue(
                m_CueId,
                m_CueKind,
                m_CueType,
                m_Intensity,
                m_DurationSeconds,
                m_Priority,
                GUID,
                GetType().Name,
                actionInstanceId));
        }

        ulong ResolveActionInstanceId(CharacterGraphContext context)
        {
            return context.TryGetActionContextHandle(m_ActionContext, out ActionInstanceHandle handle)
                ? handle.ActionInstanceId
                : 0;
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
    [NodeName("Set Camera Response")]
    [NodePath("Base/Action/Camera/Set Camera Response")]
    public sealed class SetCameraResponseNode : ActionNode
    {
        [SerializeField, ShowInPanel("Look Response")]
        CameraLookResponseMode m_LookResponse = CameraLookResponseMode.Full;

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

        public override State ReturnState => m_Submitted.Value ? State.Success : State.Failure;

        protected override void DoAction()
        {
            m_Submitted.Value = false;
            if (!TryGetGraphContext(out CharacterGraphContext context))
                return;

            ulong actionInstanceId = ResolveActionInstanceId(context);
            m_Submitted.Value = context.SubmitCameraResponsePolicy(new CameraResponsePolicy(
                m_LookResponse,
                m_ManualOrbitWeight,
                m_PitchResponseWeight,
                m_YawResponseWeight,
                m_Priority,
                m_Weight,
                GUID,
                actionInstanceId));
        }

        ulong ResolveActionInstanceId(CharacterGraphContext context)
        {
            return context.TryGetActionContextHandle(m_ActionContext, out ActionInstanceHandle handle)
                ? handle.ActionInstanceId
                : 0;
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
    [NodeName("Set Camera Target")]
    [NodePath("Base/Action/Camera/Set Camera Target")]
    public sealed class SetCameraTargetNode : ActionNode
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

        public override State ReturnState => m_Submitted.Value ? State.Success : State.Failure;

        protected override void DoAction()
        {
            m_Submitted.Value = false;
            if (!TryGetGraphContext(out CharacterGraphContext context))
                return;

            ulong actionInstanceId = ResolveActionInstanceId(context);
            m_Submitted.Value = context.SubmitCameraTargetRequest(new CameraTargetRequest(
                m_TargetKey,
                m_AnchorKey,
                m_AimPointKey,
                m_PreferredBoneKey,
                m_Priority,
                m_Weight,
                GUID,
                actionInstanceId));
        }

        ulong ResolveActionInstanceId(CharacterGraphContext context)
        {
            return context.TryGetActionContextHandle(m_ActionContext, out ActionInstanceHandle handle)
                ? handle.ActionInstanceId
                : 0;
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
    [NodeName("Read Camera Basis")]
    [NodePath("Base/Value/Camera/Read Camera Basis")]
    public sealed class ReadCameraBasisNode : ValueNode
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

        protected override void OutputValue()
        {
            base.OutputValue();
            ClearOutputs();

            if (!TryGetGraphContext(out CharacterGraphContext context))
                return;

            if (!context.TryReadCameraBasisSnapshot(out CameraBasisSnapshot snapshot))
                return;

            m_Valid.Value = true;
            m_PlanarForward.Value = snapshot.PlanarForward;
            m_PlanarRight.Value = snapshot.PlanarRight;
            m_LookDirection.Value = snapshot.LookDirection;
            m_AimPoint.Value = snapshot.AimPoint;
            m_Yaw.Value = snapshot.Yaw;
            m_Pitch.Value = snapshot.Pitch;
        }

        void ClearOutputs()
        {
            m_Valid.Value = false;
            m_PlanarForward.Value = Vector3.zero;
            m_PlanarRight.Value = Vector3.zero;
            m_LookDirection.Value = Vector3.forward;
            m_AimPoint.Value = Vector3.zero;
            m_Yaw.Value = 0f;
            m_Pitch.Value = 0f;
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
