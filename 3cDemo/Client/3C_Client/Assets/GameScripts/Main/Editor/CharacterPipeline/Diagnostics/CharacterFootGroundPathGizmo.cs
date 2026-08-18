using ThirdPersonCharacter.Pipeline.Presentation;
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

            Color lastColor = groundPath.Accepted ? Color.green : Color.red;
            Color nextColor = groundPath.Accepted ? Color.yellow : Color.red;
            DrawLandingMarker(groundPath.CurrentLanding, groundPath.ComponentUp, lastColor);
            DrawLandingMarker(groundPath.NextLanding, groundPath.ComponentUp, nextColor);
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
    }
}
