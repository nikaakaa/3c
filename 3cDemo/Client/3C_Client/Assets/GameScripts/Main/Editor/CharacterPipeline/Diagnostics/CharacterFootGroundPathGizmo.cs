using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal static class CharacterFootGroundPathGizmo
    {
        [DrawGizmo(GizmoType.Active | GizmoType.NonSelected | GizmoType.Selected)]
        static void Draw(
            CharacterWorldAwarePresentationBinding binding,
            GizmoType gizmoType)
        {
            if (!Application.isPlaying || !binding || !binding.PresentationRoot ||
                !CharacterFootLandingPredictionDebugRegistry.TryGet(
                    binding.PresentationRoot.GetInstanceID(),
                    out CharacterFootLandingPredictionDiagnostics diagnostics))
            {
                return;
            }
            DrawGroundPath(diagnostics.Left);
            DrawGroundPath(diagnostics.Right);
            DrawFootMotion(diagnostics.Left);
            DrawFootMotion(diagnostics.Right);
            DrawStrideHips(diagnostics.StrideHips);
            DrawFinalPose(binding);
        }

        static void DrawStrideHips(in CharacterFootStrideHipsDiagnostics stride)
        {
            if (!stride.Accepted)
                return;
            Handles.color = new Color(1f, 0.85f, 0.2f);
            Handles.DrawLine(stride.StrideStart, stride.StrideEnd, 1.5f);
            Gizmos.color = new Color(1f, 0.7f, 0.1f);
            Gizmos.DrawSphere(stride.AnimatedPelvis + stride.PelvisDelta, 0.04f);
        }

        static void DrawGroundPath(
            CharacterFootLandingPredictionFootDiagnostics foot)
        {
            CharacterFootGroundPathDiagnostics groundPath = foot.GroundPath;
            if (groundPath.InputIdentity == 0)
                return;

            if (groundPath.Accepted && groundPath.EnvelopeVertexCount >= 2)
            {
                Handles.color = FootColor(foot.Side);
                Vector3 previous = groundPath.EnvelopeVertexAt(0).Position;
                for (int i = 1; i < groundPath.EnvelopeVertexCount; i++)
                {
                    Vector3 current = groundPath.EnvelopeVertexAt(i).Position;
                    Handles.DrawLine(previous, current, 2f);
                    previous = current;
                }
            }
            else if (groundPath.RejectReason ==
                     CharacterFootGroundPathRejectReason.UnreachableEdge &&
                     groundPath.HasInvalidSegment)
            {
                Handles.color = Color.red;
                Handles.DrawLine(
                    groundPath.FirstInvalidSegmentBottom,
                    groundPath.FirstInvalidSegmentTop,
                    1f);
            }

            DrawLandingMarker(
                groundPath.LastLanding,
                groundPath.ComponentUp,
                Color.green);
            if (groundPath.NextSwingLandingEventIdentity != 0 &&
                groundPath.NextSwingLandingEventIdentity == foot.LandingEventIdentity)
            {
                DrawLandingMarker(
                    groundPath.NextSwingLanding,
                    groundPath.ComponentUp,
                    Color.yellow);
            }
        }

        static void DrawFootMotion(
            CharacterFootLandingPredictionFootDiagnostics foot)
        {
            CharacterFootSwingMotionDiagnostics motion = foot.FootMotion;
            if (motion.Core.State == CharacterFootSwingMotionState.None)
                return;

            Gizmos.color = Color.white;
            Gizmos.DrawSphere(motion.Core.OriginalSole, 0.025f);
            if (motion.Accepted)
            {
                Color color = SupportColor(
                    motion.Core.ConstraintState,
                    motion.Core.LockResponse,
                    foot.Side);
                Gizmos.color = color;
                Gizmos.DrawSphere(motion.Core.CorrectedSole, 0.035f);
                Handles.color = color;
                Handles.DrawLine(
                    motion.Core.OriginalSole,
                    motion.Core.CorrectedSole,
                    1f);
                return;
            }
            if (motion.Core.RejectReason == CharacterFootSwingMotionRejectReason.StepUnavailable ||
                motion.Core.RejectReason == CharacterFootSwingMotionRejectReason.StepNotSwing)
            {
                return;
            }
            Handles.color = Color.red;
            Handles.DrawWireDisc(
                motion.Core.OriginalSole,
                Vector3.up,
                0.06f);
        }

        static void DrawFinalPose(CharacterWorldAwarePresentationBinding binding)
        {
            CharacterPipelineHost host = binding.GetComponentInParent<CharacterPipelineHost>();
            if (!host || !TryGetTarget(host.GetInstanceID(), out AnimationPresentationRuntimeTarget target) ||
                !target.TryGetDebugView(out AnimationPresentationDebugView debugView))
                return;
            Animator animator = binding.GetComponentInChildren<Animator>();
            if (!animator)
                return;
            AnimationFootPlacementRuntimeSnapshot foot = debugView.PosePlan.FootPlacement;
            if (!foot.IsAvailable || !foot.PhysicalWriteAvailable)
                return;
            DrawFinalEffector(animator.transform, foot.LeftGoal, foot.LeftFoot, foot.LeftPhysicalAnkleComponentPosition, Color.cyan);
            DrawFinalEffector(animator.transform, foot.RightGoal, foot.RightFoot, foot.RightPhysicalAnkleComponentPosition, Color.magenta);
            DrawFinalPelvis(animator.transform, foot.Pelvis, foot.PhysicalPelvisComponentPosition);
            CharacterAnimationRigBinding rigBinding = binding.GetComponent<CharacterAnimationRigBinding>();
            if (!rigBinding)
                return;
            IReadOnlyList<Transform> bones = rigBinding.PhysicalBones;
            for (int i = 0; i < bones.Count; i++)
            {
                Transform bone = bones[i];
                if (!bone || !bone.parent || !bone.parent.IsChildOf(animator.transform))
                    continue;
                Handles.color = Color.red;
                Handles.DrawLine(bone.parent.position, bone.position, 2f);
            }
        }

        static bool TryGetTarget(int hostInstanceId, out AnimationPresentationRuntimeTarget target)
        {
            IReadOnlyList<AnimationPresentationRuntimeTarget> targets =
                AnimationPresentationRuntimeTargetRegistry.Targets;
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i].HostInstanceId == hostInstanceId)
                {
                    target = targets[i];
                    return true;
                }
            }
            target = null;
            return false;
        }

        static void DrawFinalEffector(
            Transform componentRoot,
            CharacterFullBodyIkGoal goal,
            CharacterFullBodyIkEffectorDiagnostics solved,
            Vector3 physicalComponentPosition,
            Color color)
        {
            Vector3 goalPosition = componentRoot.TransformPoint(goal.ComponentPosition);
            Vector3 solvedPosition = componentRoot.TransformPoint(solved.SolvedComponentPosition);
            Vector3 physicalPosition = componentRoot.TransformPoint(physicalComponentPosition);
            Handles.color = Color.white;
            Handles.DrawWireDisc(goalPosition, componentRoot.up, 0.08f);
            Gizmos.color = color;
            Gizmos.DrawSphere(solvedPosition, 0.045f);
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(physicalPosition, 0.055f);
            Handles.color = color;
            Handles.DrawLine(goalPosition, solvedPosition, 2f);
            Handles.color = Color.red;
            Handles.DrawLine(solvedPosition, physicalPosition, 2f);
        }

        static void DrawFinalPelvis(
            Transform componentRoot,
            CharacterFullBodyIkEffectorDiagnostics pelvis,
            Vector3 physicalComponentPosition)
        {
            Vector3 solvedPosition = componentRoot.TransformPoint(pelvis.SolvedComponentPosition);
            Vector3 physicalPosition = componentRoot.TransformPoint(physicalComponentPosition);
            Gizmos.color = new Color(1f, 0.8f, 0.1f);
            Gizmos.DrawSphere(solvedPosition, 0.05f);
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(physicalPosition, 0.06f);
            Handles.color = Color.red;
            Handles.DrawLine(solvedPosition, physicalPosition, 2f);
        }

        static void DrawLandingMarker(Vector3 position, Vector3 componentUp, Color color)
        {
            Vector3 normal = componentUp.sqrMagnitude > 0.000001f
                ? componentUp.normalized
                : Vector3.up;
            Gizmos.color = color;
            Gizmos.DrawSphere(position, 0.05f);
            Handles.color = color;
            Handles.DrawWireDisc(position, normal, 0.12f);
        }

        static Color FootColor(CharacterFootSide side) =>
            side == CharacterFootSide.Left
                ? new Color(0.1f, 0.8f, 1f)
                : new Color(1f, 0.35f, 0.75f);

        static Color SupportColor(
            CharacterFootConstraintState state,
            CharacterFootLockResponse response,
            CharacterFootSide side) =>
            state switch
            {
                CharacterFootConstraintState.Landing => new Color(0.1f, 0.75f, 1f),
                CharacterFootConstraintState.Locked when response == CharacterFootLockResponse.Sliding =>
                    new Color(1f, 0.8f, 0.1f),
                CharacterFootConstraintState.Locked => new Color(0.2f, 1f, 0.25f),
                CharacterFootConstraintState.Releasing => new Color(0.85f, 0.35f, 1f),
                _ => FootColor(side)
            };
    }
}
