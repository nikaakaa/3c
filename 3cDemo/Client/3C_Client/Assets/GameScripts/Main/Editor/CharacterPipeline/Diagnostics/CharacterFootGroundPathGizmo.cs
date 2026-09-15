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
        }

        static void DrawStrideHips(in CharacterFootStrideHipsDiagnostics stride)
        {
            if (!stride.Core.Accepted)
                return;
            Handles.color = new Color(1f, 0.85f, 0.2f);
            Handles.DrawLine(stride.Core.StrideStart, stride.Core.StrideEnd, 1.5f);
            Gizmos.color = new Color(1f, 0.7f, 0.1f);
            Gizmos.DrawSphere(stride.Observation.AnimatedPelvis + stride.Core.PelvisDelta, 0.04f);
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
