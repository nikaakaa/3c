using System.Collections.Generic;
using ThirdPersonCamera;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonCharacter.Pipeline.Motion;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonGameplay.Tick;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Camera
{
    public sealed class CharacterCameraStage
    {
        const float DefaultFieldOfView = 60f;

        readonly CharacterGraphContext m_GraphContext;
        readonly ICameraRigAdapter m_RigAdapter;
        readonly Transform m_FollowAnchor;
        readonly Transform m_AimAnchor;
        readonly Vector3 m_FollowAnchorBindLocalPosition;
        readonly Vector3 m_AimAnchorBindLocalPosition;
        readonly bool m_HasFollowAnchorBinding;
        readonly bool m_HasAimAnchorBinding;
        readonly string m_LookInputValueId;
        readonly CameraStateResolver m_StateResolver = new CameraStateResolver();
        readonly CameraResponsePolicyResolver m_ResponseResolver = new CameraResponsePolicyResolver();
        readonly CameraModifierResolver m_ModifierResolver = new CameraModifierResolver();
        readonly List<CameraStateRequest> m_StateRequests = new List<CameraStateRequest>();
        readonly List<CameraCue> m_PendingCues = new List<CameraCue>();
        readonly List<CameraResponsePolicy> m_ResponsePolicies = new List<CameraResponsePolicy>();
        readonly List<CameraTargetRequest> m_TargetRequests = new List<CameraTargetRequest>();
        readonly HashSet<ulong> m_TerminalActionInstances = new HashSet<ulong>();
        readonly CameraDebugSnapshot m_DebugSnapshot = new CameraDebugSnapshot();

        Vector2 m_PendingLookDelta;
        bool m_MissingRigReported;
        bool m_MissingAnchorReported;
        bool m_MissingPresentationRootPoseReported;

        public CharacterCameraStage(
            CharacterGraphContext graphContext,
            ICameraRigAdapter rigAdapter,
            CharacterLogicBodyState initialLogicState,
            Transform followAnchor,
            Transform aimAnchor,
            string lookInputValueId)
        {
            m_GraphContext = graphContext;
            m_RigAdapter = rigAdapter;
            m_FollowAnchor = followAnchor;
            m_AimAnchor = aimAnchor;
            m_LookInputValueId = lookInputValueId ?? string.Empty;
            if (initialLogicState.IsValid && followAnchor)
            {
                Vector3 logicPosition = initialLogicState.Position.ToUnityVector();
                Quaternion logicRotation = initialLogicState.Rotation.ToUnityRotation();
                Quaternion inverseLogicRotation = Quaternion.Inverse(logicRotation);
                m_FollowAnchorBindLocalPosition = inverseLogicRotation * (followAnchor.position - logicPosition);
                m_HasFollowAnchorBinding = true;
            }
            if (initialLogicState.IsValid && aimAnchor)
            {
                Vector3 logicPosition = initialLogicState.Position.ToUnityVector();
                Quaternion logicRotation = initialLogicState.Rotation.ToUnityRotation();
                Quaternion inverseLogicRotation = Quaternion.Inverse(logicRotation);
                m_AimAnchorBindLocalPosition = inverseLogicRotation * (aimAnchor.position - logicPosition);
                m_HasAimAnchorBinding = true;
            }
        }

        public CameraDebugSnapshot DebugSnapshot => m_DebugSnapshot;

        public void Reset()
        {
            m_StateResolver.Reset();
            m_ModifierResolver.Reset();
            m_StateRequests.Clear();
            m_ResponsePolicies.Clear();
            m_TargetRequests.Clear();
            ClearPendingPresentationInputs();
            m_DebugSnapshot.Clear();
            m_MissingRigReported = false;
            m_MissingAnchorReported = false;
            m_MissingPresentationRootPoseReported = false;
            m_GraphContext?.SetCameraBasisSnapshot(CameraBasisSnapshot.Invalid);
        }

        public void CaptureRenderFrameInput(CharacterInputStage inputStage)
        {
            if (inputStage != null &&
                !string.IsNullOrEmpty(m_LookInputValueId) &&
                inputStage.TryGetLatchedVector2(m_LookInputValueId, out Vector2 look))
                m_PendingLookDelta += look;
        }

        public void CaptureLogicSample(CharacterPipelineFrame frame)
        {
            m_StateRequests.Clear();
            m_ResponsePolicies.Clear();
            m_TargetRequests.Clear();

            if (frame == null)
                return;

            Copy(frame.Output.Presentation.CameraStateRequests, m_StateRequests);
            Copy(frame.Output.Presentation.CameraCues, m_PendingCues);
            Copy(frame.Output.Presentation.CameraResponsePolicies, m_ResponsePolicies);
            Copy(frame.Output.Presentation.CameraTargetRequests, m_TargetRequests);
            CaptureTerminalActions(frame);
        }

        public void Update(GameplayPresentationFrameContext context, CharacterPipelineFrame frame)
        {
            if (m_RigAdapter == null)
            {
                ReportMissingRig();
                ClearFrameOutput(frame);
                return;
            }

            if (!m_FollowAnchor || !m_AimAnchor || !m_HasFollowAnchorBinding || !m_HasAimAnchorBinding)
            {
                ReportMissingAnchor();
                ClearFrameOutput(frame);
                return;
            }

            CameraStateRequest selectedState = m_StateResolver.Resolve(
                m_StateRequests,
                m_TerminalActionInstances,
                context.ScaledDeltaSeconds,
                out float blendProgress);
            CameraResponsePolicy responsePolicy = m_ResponseResolver.Resolve(
                selectedState,
                m_ResponsePolicies,
                m_TerminalActionInstances);
            CameraTargetRequest targetRequest = ResolveTargetRequest(selectedState);
            CharacterPresentationRootPose presentationRootPose = frame != null
                ? frame.Output.Presentation.PresentationRootPose
                : default;
            if (!TryBuildBasePlan(
                    selectedState,
                    responsePolicy,
                    targetRequest,
                    presentationRootPose,
                    blendProgress,
                    m_PendingLookDelta,
                    out CameraPosePlan basePlan))
            {
                ClearFrameOutput(frame);
                return;
            }
            CameraPosePlan modifiedPlan = m_ModifierResolver.Resolve(
                basePlan,
                m_PendingCues,
                m_TerminalActionInstances,
                context.ScaledDeltaSeconds);

            m_RigAdapter.Apply(modifiedPlan);
            CameraBasisSnapshot basis = m_RigAdapter.BasisSnapshot.Valid
                ? m_RigAdapter.BasisSnapshot
                : CameraBasisSnapshot.Invalid;
            m_GraphContext?.SetCameraBasisSnapshot(basis);
            WriteFrameOutput(frame, modifiedPlan, basis, TargetSource(selectedState, targetRequest));
            ClearPendingPresentationInputs();
        }

        bool TryBuildBasePlan(
            CameraStateRequest state,
            CameraResponsePolicy responsePolicy,
            CameraTargetRequest targetRequest,
            CharacterPresentationRootPose presentationRootPose,
            float blendProgress,
            Vector2 lookDelta,
            out CameraPosePlan plan)
        {
            if (!presentationRootPose.Valid)
            {
                ReportMissingPresentationRootPose();
                plan = default;
                return false;
            }
            Vector3 followPoint = presentationRootPose.TransformPoint(m_FollowAnchorBindLocalPosition);
            Vector3 defaultAimPoint = presentationRootPose.TransformPoint(m_AimAnchorBindLocalPosition);
            Vector3 aimPoint = ResolveAimPoint(state, targetRequest, defaultAimPoint);
            float fov = ResolveFieldOfView(state.Mode);
            Vector2 filteredLook = responsePolicy.Apply(lookDelta);

            plan = new CameraPosePlan(
                state.Mode,
                followPoint,
                aimPoint,
                fov,
                responsePolicy,
                filteredLook,
                state.SourceId,
                state.SourceActionInstanceId,
                blendProgress,
                true);
            return true;
        }

        CameraTargetRequest ResolveTargetRequest(CameraStateRequest selectedState)
        {
            CameraTargetRequest selected = default;
            for (int i = 0; i < m_TargetRequests.Count; i++)
            {
                CameraTargetRequest candidate = m_TargetRequests[i];
                if (!candidate.Active || IsTerminal(candidate.SourceActionInstanceId))
                    continue;

                if (!selected.Active ||
                    candidate.Priority > selected.Priority ||
                    candidate.Priority == selected.Priority && candidate.Weight > selected.Weight)
                    selected = candidate;
            }

            if (selected.Active || string.IsNullOrEmpty(selectedState.TargetKey))
                return selected;

            return new CameraTargetRequest(
                selectedState.TargetKey,
                string.Empty,
                selectedState.TargetKey,
                string.Empty,
                selectedState.Priority,
                selectedState.Weight,
                selectedState.SourceId,
                selectedState.SourceActionInstanceId);
        }

        Vector3 ResolveAimPoint(
            CameraStateRequest state,
            CameraTargetRequest targetRequest,
            Vector3 defaultAimPoint)
        {
            if (TryResolveTargetPoint(targetRequest.SourceActionInstanceId, out Vector3 aimPoint))
                return aimPoint;

            if (TryResolveTargetPoint(state.SourceActionInstanceId, out aimPoint))
                return aimPoint;

            return defaultAimPoint;
        }

        bool TryResolveTargetPoint(ulong actionInstanceId, out Vector3 point)
        {
            point = Vector3.zero;
            if (actionInstanceId == 0 || m_GraphContext == null)
                return false;

            if (m_GraphContext.TryGetActionInstanceHandle(actionInstanceId, out ActionInstanceHandle handle) &&
                handle.TargetSnapshot.HasTarget)
            {
                point = handle.TargetSnapshot.Position;
                return true;
            }

            return false;
        }

        static float ResolveFieldOfView(CameraMode mode)
        {
            switch (mode)
            {
                case CameraMode.Aim:
                    return 50f;
                case CameraMode.LockOn:
                    return 55f;
                case CameraMode.ActionFocus:
                    return 48f;
                case CameraMode.SkillCloseup:
                    return 42f;
                default:
                    return DefaultFieldOfView;
            }
        }

        void WriteFrameOutput(CharacterPipelineFrame frame, CameraPosePlan plan, CameraBasisSnapshot basis, string targetSource)
        {
            if (frame == null)
                return;

            frame.Output.Presentation.CameraPosePlan = plan;
            frame.Output.Presentation.CameraBasisSnapshot = basis;
            frame.Output.Presentation.CameraDebug.Set(
                plan,
                basis,
                targetSource,
                m_StateRequests,
                m_ModifierResolver.DebugCues);
            m_DebugSnapshot.Set(
                plan,
                basis,
                targetSource,
                m_StateRequests,
                m_ModifierResolver.DebugCues);
        }

        void ClearFrameOutput(CharacterPipelineFrame frame)
        {
            if (frame != null)
            {
                frame.Output.Presentation.CameraPosePlan = default;
                frame.Output.Presentation.CameraBasisSnapshot = default;
                frame.Output.Presentation.CameraDebug.Clear();
            }

            m_GraphContext?.SetCameraBasisSnapshot(CameraBasisSnapshot.Invalid);
            m_ModifierResolver.DiscardTerminal(m_TerminalActionInstances);
            ClearPendingPresentationInputs();
            m_DebugSnapshot.Clear();
        }

        void ClearPendingPresentationInputs()
        {
            m_PendingLookDelta = Vector2.zero;
            m_PendingCues.Clear();
            m_TerminalActionInstances.Clear();
        }

        void CaptureTerminalActions(CharacterPipelineFrame frame)
        {
            IReadOnlyList<ActionLifecycleTransition> transitions = frame.Output.SyncFacts.Action.LifecycleTransitions;
            for (int i = 0; i < transitions.Count; i++)
            {
                ActionLifecycleTransition transition = transitions[i];
                if (transition.IsTerminal)
                    m_TerminalActionInstances.Add(transition.ActionInstanceId);
            }
        }

        bool IsTerminal(ulong actionInstanceId)
        {
            return actionInstanceId != 0 && m_TerminalActionInstances.Contains(actionInstanceId);
        }

        static string TargetSource(CameraStateRequest state, CameraTargetRequest target)
        {
            if (target.Active)
                return !string.IsNullOrEmpty(target.TargetKey) ? target.TargetKey : target.AimPointKey;
            return state.TargetKey;
        }

        static void Copy<T>(IReadOnlyList<T> source, List<T> destination)
        {
            if (source == null)
                return;

            for (int i = 0; i < source.Count; i++)
                destination.Add(source[i]);
        }

        void ReportMissingRig()
        {
            if (m_MissingRigReported)
                return;

            m_MissingRigReported = true;
            Debug.LogError("CharacterCameraStage requires an explicit camera rig adapter.");
        }

        void ReportMissingAnchor()
        {
            if (m_MissingAnchorReported)
                return;

            m_MissingAnchorReported = true;
            Debug.LogError("CharacterCameraStage requires explicit camera follow and aim anchors bound relative to the logic root.");
        }

        void ReportMissingPresentationRootPose()
        {
            if (m_MissingPresentationRootPoseReported)
                return;

            m_MissingPresentationRootPoseReported = true;
            Debug.LogError("CharacterCameraStage requires a valid presentation root pose before applying the default camera follow binding.");
        }

    }
}
