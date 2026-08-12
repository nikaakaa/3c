using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonCharacter.Pipeline.Simulation.Fixed;
using ThirdPersonSimulation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    [InitializeOnLoad]
    static class CharacterPredictiveFootPlacementGizmo
    {
        const string MenuPath = "Tools/3C/Diagnostics/Predictive Foot Placement Gizmos";
        const string SessionKey = "3C.PredictiveFootPlacementGizmos.Enabled";
        static readonly Color s_LeftColor = new Color(0.1f, 0.8f, 1f, 1f);
        static readonly Color s_RightColor = new Color(1f, 0.35f, 0.85f, 1f);
        static readonly Color s_RouteColor = new Color(1f, 0.8f, 0.1f, 0.85f);
        static readonly Color s_LandingColor = new Color(0.25f, 1f, 0.35f, 1f);
        static readonly Color s_VirtualGroundSplitColor = new Color(1f, 0.55f, 0.1f, 1f);
        static readonly Color s_LiftColor = new Color(0.8f, 0.3f, 1f, 1f);
        static readonly Color s_AcceptColor = new Color(0.2f, 1f, 0.45f, 0.9f);
        static readonly Color s_RejectColor = new Color(1f, 0.2f, 0.1f, 0.9f);
        static readonly ConditionalWeakTable<CharacterPredictiveFootPlanGeometrySnapshot, PlanDrawCache> s_DrawCaches =
            new ConditionalWeakTable<CharacterPredictiveFootPlanGeometrySnapshot, PlanDrawCache>();
        static CharacterPredictiveFootPlacementGizmo()
        {
            Selection.selectionChanged += Repaint;
        }

        static bool Enabled
        {
            get => SessionState.GetBool(SessionKey, true);
            set => SessionState.SetBool(SessionKey, value);
        }

        [MenuItem(MenuPath)]
        static void Toggle()
        {
            Enabled = !Enabled;
            Repaint();
        }

        [MenuItem(MenuPath, true)]
        static bool ValidateToggle()
        {
            Menu.SetChecked(MenuPath, Enabled);
            return true;
        }

        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
        static void DrawFixedHost(FixedCharacterHost host, GizmoType gizmoType)
        {
            if (host)
                Draw(host.ActorId);
        }

        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
        static void DrawFloat32Host(CharacterPipelineHost host, GizmoType gizmoType)
        {
            if (host && !string.IsNullOrEmpty(host.ActorId))
                Draw(host.SimulationActorId);
        }

        static void Draw(ActorId actorId)
        {
            if (!Enabled || !EditorApplication.isPlaying ||
                !CharacterPredictiveFootPlacementDebugSnapshotRegistry.TryGet(
                    actorId,
                    out CharacterPredictiveFootFrameSnapshot snapshot))
                return;
            Matrix4x4 previousGizmosMatrix = Gizmos.matrix;
            Matrix4x4 previousHandlesMatrix = Handles.matrix;
            Color previousGizmosColor = Gizmos.color;
            Color previousHandlesColor = Handles.color;
            Gizmos.matrix = Matrix4x4.identity;
            Handles.matrix = Matrix4x4.identity;
            try
            {
                DrawLeg(snapshot.Left, s_LeftColor);
                DrawLeg(snapshot.Right, s_RightColor);
            }
            finally
            {
                Gizmos.matrix = previousGizmosMatrix;
                Handles.matrix = previousHandlesMatrix;
                Gizmos.color = previousGizmosColor;
                Handles.color = previousHandlesColor;
            }
        }

        static void DrawLeg(CharacterPredictiveFootLegFrameSnapshot leg, Color sideColor)
        {
            CharacterPredictiveFootPlanGeometrySnapshot plan = leg.Plan;
            if (plan == null)
                return;
            float markerSize = 0.11f;
            PlanDrawCache cache = s_DrawCaches.GetValue(plan, CreateDrawCache);
            if (leg.PlanState == CharacterPredictiveFootPlanState.Rejected)
            {
                DrawPath(cache.FootRoutePath, s_RouteColor, 2f);
                DrawLines(cache.QueryBoundaryLines, s_RouteColor);
                DrawLines(cache.AcceptedLines, s_AcceptColor);
                DrawLines(cache.RejectedLines, s_RejectColor);
                return;
            }
            if (leg.PlanState != CharacterPredictiveFootPlanState.Planned &&
                leg.PlanState != CharacterPredictiveFootPlanState.Executing)
                return;
            DrawPath(cache.FootRoutePath, s_RouteColor, 2f);
            DrawLines(cache.EnvelopeLines, sideColor);
            DrawPath(cache.ClearancePath, sideColor, 4f);
            if (plan.LandingValid)
                DrawMarker(plan.Landing, markerSize, s_LandingColor);
            if (plan.VirtualGroundSplitValid)
                DrawMarker(plan.VirtualGroundSplit, markerSize * 0.8f, s_VirtualGroundSplitColor);
            if (leg.ClearanceEvaluated)
                DrawMarker(leg.CurrentPath, markerSize * 0.55f, s_LiftColor);
            if (leg.Rewritten)
                DrawThickLine(leg.BaselineAnkle, leg.FinalAnkle, s_LiftColor, 4f);
        }

        static PlanDrawCache CreateDrawCache(CharacterPredictiveFootPlanGeometrySnapshot plan) =>
            new PlanDrawCache(plan);

        static void DrawLines(Vector3[] lines, Color color)
        {
            if (lines.Length == 0)
                return;
            Handles.color = color;
            Handles.DrawLines(lines);
        }

        static void DrawPath(Vector3[] path, Color color, float width)
        {
            if (path.Length < 2)
                return;
            Handles.color = color;
            Handles.DrawAAPolyLine(width, path);
        }

        static void DrawMarker(Vector3 position, float size, Color color)
        {
            Gizmos.color = new Color(color.r, color.g, color.b, 0.22f);
            Gizmos.DrawCube(position, Vector3.one * size * 0.55f);
            DrawWireMarker(position, size, color);
        }

        static void DrawWireMarker(Vector3 position, float size, Color color)
        {
            Gizmos.color = color;
            Gizmos.DrawWireCube(position, Vector3.one * size);
        }

        static void DrawThickLine(Vector3 from, Vector3 to, Color color, float width)
        {
            Gizmos.color = color;
            Gizmos.DrawLine(from, to);
        }

        static void Repaint()
        {
            SceneView.RepaintAll();
        }

        sealed class PlanDrawCache
        {
            const float MarkerSize = 0.11f;

            internal PlanDrawCache(CharacterPredictiveFootPlanGeometrySnapshot plan)
            {
                if (plan.Executable)
                {
                    FootRoutePath = BuildFootRoute(plan.FootRoute);
                    ClearancePath = BuildClearancePath(plan.ClearancePath);
                    EnvelopeLines = BuildEnvelope(plan.GroundEnvelope);
                    QueryBoundaryLines = System.Array.Empty<Vector3>();
                    AcceptedLines = System.Array.Empty<Vector3>();
                    RejectedLines = System.Array.Empty<Vector3>();
                    return;
                }
                FootRoutePath = BuildFootRoute(plan.FootRoute);
                ClearancePath = System.Array.Empty<Vector3>();
                EnvelopeLines = System.Array.Empty<Vector3>();
                QueryBoundaryLines = BuildQueryBoundaries(plan.QueryRequests, plan.RejectedGeometry);
                AcceptedLines = BuildTerminalAcceptedGeometry(plan.AcceptedSupports);
                RejectedLines = BuildTerminalRejectedGeometry(plan.RejectedGeometry);
            }

            internal Vector3[] FootRoutePath { get; }
            internal Vector3[] ClearancePath { get; }
            internal Vector3[] EnvelopeLines { get; }
            internal Vector3[] QueryBoundaryLines { get; }
            internal Vector3[] AcceptedLines { get; }
            internal Vector3[] RejectedLines { get; }

            static Vector3[] BuildFootRoute(IReadOnlyList<CharacterPredictiveFootRoutePointSnapshot> route)
            {
                var path = new Vector3[route.Count];
                for (int i = 0; i < route.Count; i++)
                    path[i] = route[i].Position;
                return path;
            }

            static Vector3[] BuildClearancePath(
                IReadOnlyList<CharacterPredictiveFootClearanceSegmentSnapshot> path)
            {
                if (path.Count == 0)
                    return System.Array.Empty<Vector3>();
                var result = new Vector3[path.Count + 1];
                result[0] = path[0].Start;
                for (int i = 0; i < path.Count; i++)
                    result[i + 1] = path[i].End;
                return result;
            }

            static Vector3[] BuildEnvelope(IReadOnlyList<CharacterPredictiveFootEnvelopeSegmentSnapshot> envelope)
            {
                var lines = new List<Vector3>(envelope.Count * 8);
                for (int i = 0; i < envelope.Count; i++)
                {
                    CharacterPredictiveFootEnvelopeSegmentSnapshot segment = envelope[i];
                    AddLine(lines, segment.EdgeStart, segment.EdgeEnd);
                    AddPoint(lines, segment.EdgeEnd, MarkerSize * 0.32f);
                }
                return lines.ToArray();
            }

            static Vector3[] BuildQueryBoundaries(
                IReadOnlyList<CharacterPredictiveFootQueryRequestSnapshot> queries,
                IReadOnlyList<CharacterPredictiveFootQueryGeometrySnapshot> rejected)
            {
                if (queries.Count == 0)
                    return System.Array.Empty<Vector3>();
                var lines = new List<Vector3>(12);
                AddQueryBoundary(lines, queries[0]);
                if (queries.Count > 1)
                    AddQueryBoundary(lines, queries[queries.Count - 1]);
                if (rejected.Count > 0)
                {
                    int terminalQueryIndex = rejected[rejected.Count - 1].QueryIndex;
                    if (terminalQueryIndex > 0 && terminalQueryIndex < queries.Count - 1)
                        AddQueryBoundary(lines, queries[terminalQueryIndex]);
                }
                return lines.ToArray();
            }

            static void AddQueryBoundary(
                List<Vector3> lines,
                CharacterPredictiveFootQueryRequestSnapshot query)
            {
                Vector3 endOffset = query.Direction * query.MaximumDistance;
                AddLine(lines, query.Origin, query.Origin + endOffset);
                if (query.Shape == "Capsule")
                    AddLine(lines, query.CapsuleEnd, query.CapsuleEnd + endOffset);
            }

            static Vector3[] BuildTerminalAcceptedGeometry(
                IReadOnlyList<CharacterPredictiveFootQueryGeometrySnapshot> geometry)
            {
                if (geometry.Count == 0)
                    return System.Array.Empty<Vector3>();
                CharacterPredictiveFootQueryGeometrySnapshot value = geometry[geometry.Count - 1];
                var lines = new List<Vector3>(8);
                AddPoint(lines, value.Position, MarkerSize * 0.55f);
                if (value.Normal.sqrMagnitude > 0.25f)
                    AddLine(lines, value.Position, value.Position + value.Normal.normalized * MarkerSize);
                return lines.ToArray();
            }

            static Vector3[] BuildTerminalRejectedGeometry(
                IReadOnlyList<CharacterPredictiveFootQueryGeometrySnapshot> geometry)
            {
                if (geometry.Count == 0)
                    return System.Array.Empty<Vector3>();
                int terminalQueryIndex = geometry[geometry.Count - 1].QueryIndex;
                var lines = new List<Vector3>();
                for (int i = 0; i < geometry.Count; i++)
                {
                    CharacterPredictiveFootQueryGeometrySnapshot value = geometry[i];
                    if (value.QueryIndex != terminalQueryIndex)
                        continue;
                    AddPoint(lines, value.Position, MarkerSize * 0.5f);
                    if (value.Normal.sqrMagnitude > 0.25f)
                        AddLine(lines, value.Position, value.Position + value.Normal.normalized * MarkerSize);
                }
                return lines.ToArray();
            }

            static void AddPoint(List<Vector3> lines, Vector3 position, float size)
            {
                float half = size * 0.5f;
                AddLine(lines, position - Vector3.right * half, position + Vector3.right * half);
                AddLine(lines, position - Vector3.up * half, position + Vector3.up * half);
                AddLine(lines, position - Vector3.forward * half, position + Vector3.forward * half);
            }

            static void AddLine(List<Vector3> lines, Vector3 from, Vector3 to)
            {
                lines.Add(from);
                lines.Add(to);
            }
        }
    }
}
