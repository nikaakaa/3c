using ThirdPersonCharacter.Pipeline.Presentation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal static class CharacterFootLandingPredictionGizmo
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
            DrawFoot(diagnostics.Left);
            DrawFoot(diagnostics.Right);
        }

        static void DrawFoot(CharacterFootLandingPredictionFootDiagnostics foot)
        {
            const float pointRadius = 0.035f;
            Color footColor = FootColor(foot.Side);
            Gizmos.color = footColor;
            Gizmos.DrawWireSphere(foot.CurrentAnimatedSole, pointRadius);

            if (foot.Query.MaximumDistance <= 0f)
                return;

            Gizmos.color = footColor;
            Gizmos.DrawWireSphere(foot.RawLandingCandidate, pointRadius * 1.25f);

            Vector3 queryEnd = foot.Query.Origin +
                               foot.Query.Direction.normalized *
                               foot.Query.MaximumDistance;
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(foot.Query.Origin, queryEnd);
            Gizmos.DrawWireSphere(foot.Query.Origin, foot.Query.Radius);
            Gizmos.DrawWireSphere(queryEnd, foot.Query.Radius);

            if (!foot.Accepted)
            {
                Gizmos.color = Color.red;
                DrawCross(foot.RawLandingCandidate, pointRadius * 2f);
                return;
            }

            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(
                foot.LandingPoint,
                Vector3.one * pointRadius * 1.5f);
            Gizmos.DrawLine(
                foot.LandingPoint,
                foot.LandingPoint + foot.LandingNormal * 0.16f);
        }

        static Color FootColor(CharacterFootSide side) =>
            side == CharacterFootSide.Left
                ? new Color(0.1f, 0.8f, 1f)
                : new Color(1f, 0.35f, 0.75f);

        static void DrawCross(Vector3 center, float radius)
        {
            Gizmos.DrawLine(
                center + new Vector3(-radius, 0f, -radius),
                center + new Vector3(radius, 0f, radius));
            Gizmos.DrawLine(
                center + new Vector3(-radius, 0f, radius),
                center + new Vector3(radius, 0f, -radius));
        }
    }
}
